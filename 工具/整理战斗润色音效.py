from pathlib import Path
import json,hashlib,subprocess,wave
import imageio_ffmpeg
import numpy as np
root=Path(__file__).resolve().parents[1];out=root/'output/战斗全面优化';maker=Path('C:/Users/123/Documents/taptap製造_一滴水的故事')
data=json.loads((out/'声音结果.json').read_text(encoding='utf-8'))['structuredContent']
if data.get('failed'):raise RuntimeError('有失败的音效，不重复派发，请先核对')
target=root/'Assets/天帝/Resources/战斗润色音效';target.mkdir(exist_ok=True)
records=[]
for r in data['audio_files']:
    p=maker/r['localPath'];name=r['name'].split('战斗_')[1].split('_v')[0]
    raw=subprocess.check_output([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-i',str(p),'-f','f32le','-ar','22050','-ac','1','pipe:1'])
    samples=np.frombuffer(raw,dtype=np.float32).copy();peak=float(np.max(np.abs(samples)))
    if peak<.001 or not np.all(np.isfinite(samples)):raise RuntimeError('无效音效：'+name)
    active=np.where(np.abs(samples)>peak*.015)[0];start=max(0,int(active[0])-220);end=min(len(samples),int(active[-1])+660)
    samples=samples[start:end];samples*=min(3,.32/peak) if name!='跑步' else min(2,.12/peak)
    n=min(110,len(samples)//4);samples[:n]*=np.linspace(0,1,n);samples[-n:]*=np.linspace(1,0,n)
    with wave.open(str(target/(name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(22050);w.writeframes((np.clip(samples,-1,1)*32767).astype('int16').tobytes())
    records.append(dict(name=name,source=str(p),cdn=r['audioUrl'],sha256=hashlib.sha256(p.read_bytes()).hexdigest(),seconds=len(samples)/22050,source_peak=peak,final_peak=float(np.max(np.abs(samples)))))
(out/'音效来源.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8');print('处理音效',len(records))
