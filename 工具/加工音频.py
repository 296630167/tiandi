from pathlib import Path
import json,subprocess,hashlib
import numpy as np
from scipy.io import wavfile
import imageio_ffmpeg
root=Path(__file__).resolve().parents[1]
maker=Path('C:/Users/123/Documents/taptap製造_一滴水的故事')
out=root/'output/音频接入'
data=json.loads((out/'完整生成记录.json').read_text(encoding='utf-8'))
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
items=[]
for m in data['music']:
    name=m['music']['title']; items.append((name,'音乐',maker/'assets/audio/music'/f'{name}.mp3',m['music']['audioUrl']))
for batch in data['sfx']:
    for a in batch['audio_files']: items.append((a['name'],'音效',Path(a['absolutePath']),a['audioUrl']))
for v in data['voice']:
    a=v['result']['audio_files'][0]; items.append((v['name'],'配音',Path(a['absolutePath']),a['audioUrl']))
report=[]
for name,kind,source,url in items:
    assert source.exists(), str(source)
    tmp=out/(name+'_decoded.wav')
    filters='loudnorm=I=-20:TP=-3:LRA=9' if kind=='音乐' else 'loudnorm=I=-18:TP=-3:LRA=7' if kind=='配音' else 'anull'
    subprocess.run([ffmpeg,'-v','error','-y','-i',str(source),'-af',filters,'-ar','48000','-ac','2' if kind=='音乐' else '1','-c:a','pcm_f32le',str(tmp)],check=True)
    rate,x=wavfile.read(tmp); peak=float(np.max(np.abs(x))); assert peak>.0001, name+' silent'
    if kind=='音乐':
        # 末尾与开头线性重叠，循环接缝处波形连续；两端不另加长静音。
        overlap=rate
        t=np.linspace(0,1,overlap,dtype=np.float32)[:,None]
        seam=x[-overlap:]*(1-t)+x[:overlap]*t
        x=np.concatenate([seam,x[overlap:-overlap]])
    if kind=='音效': x*=.50/peak
    else: x*=min(1,.70/float(np.max(np.abs(x))))
    # 短淡入淡出避免音效/语音的截断爆音。
    if kind!='音乐':
        n=min(int(rate*.008),len(x)//4); ramp=np.linspace(0,1,n)
        x[:n]*=ramp; x[-n:]*=ramp[::-1]
    target=root/'Assets/天帝/Resources/天帝音频'/kind/(name+('.ogg' if kind=='音乐' else '.wav'))
    target.parent.mkdir(parents=True,exist_ok=True)
    if kind=='音乐':
        wavfile.write(tmp,rate,x.astype(np.float32))
        subprocess.run([ffmpeg,'-v','error','-y','-i',str(tmp),'-c:a','libvorbis','-q:a','5',str(target)],check=True)
    else: wavfile.write(target,rate,(x*32767).astype(np.int16))
    report.append({'名称':name,'分类':kind,'原文件':str(source),'来源':url,'原始SHA256':hashlib.sha256(source.read_bytes()).hexdigest(),'成品':str(target),'秒':round(len(x)/rate,3),'峰值':round(float(np.max(np.abs(x))),4),'RMS':round(float(np.sqrt(np.mean(x*x))),4),'字节':target.stat().st_size})
    tmp.unlink()
(out/'素材验收.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
# 为自动下载失败的音乐保留本次补齐记录，原始生成映射不覆盖。
(maker/'assets/audio/music/天帝素材下载记录.json').write_text(json.dumps(data['music'],ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'音乐':sum(x['分类']=='音乐' for x in report),'音效':sum(x['分类']=='音效' for x in report),'配音':sum(x['分类']=='配音' for x in report),'总字节':sum(x['字节'] for x in report)},ensure_ascii=False))
