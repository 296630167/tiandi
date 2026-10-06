from pathlib import Path
import subprocess, json
ROOT = Path(__file__).resolve().parents[1]
MAIN = ROOT/'生成/开发日志二期采集-20261005-185151'
BOX = ROOT/'生成/开发日志二期采集-20261005-190600'
OUT = ROOT/'开发日志第二期/视频'
FFMPEG = ROOT/'生成/录制工具/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
OUT.mkdir(exist_ok=True)
spec = []
def add(source, start, end, name, battle=False):
    spec.append(dict(source=source,start=start,end=end,name=name,battle=battle))
report=json.loads((MAIN/'录制报告.json').read_text(encoding='utf-8'))
for i,s in enumerate(report['片段']):
    if i == 14:
        add(0,2911,3091,'递增波次与精英',True)
        add(0,3450,3660,'头目与清剿进度',True)
        add(0,3966,4176,'狼王出现条件',True)
    elif i == 15:
        add(0,4176,4406,'狼王进入视野',True)
        add(0,4670,4880,'狼王半血阶段',True)
        add(0,5025,5265,'击败狼王',True)
    elif i == 17:
        add(1,30,360,'宝盒界面 实际抽取与悬停详情')
    else:
        add(0,s['开始帧'],s['结束帧'],s['名'],i in (10,11,12))

lines=[]
for src in range(2):
    ids=[i for i,x in enumerate(spec) if x['source']==src]
    lines.append(f'[{src}:v]split={len(ids)}'+''.join(f'[raw{i}]' for i in ids)+';')
for i,x in enumerate(spec):
    filters=f"[raw{i}]trim=start_frame={x['start']}:end_frame={x['end']},setpts=PTS-STARTPTS,setsar=1"
    if x['battle']:
        filters+=",drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':text='战斗原速选段':fontsize=19:fontcolor=white:box=1:boxcolor=black@0.55:boxborderw=5:x=(w-text_w)/2:y=55"
    if x['source'] == 1:
        filters+=",drawtext=fontfile='C\\:/Windows/Fonts/msyh.ttc':text='同版本宝盒补拍':fontsize=19:fontcolor=white:box=1:boxcolor=black@0.55:boxborderw=5:x=(w-text_w)/2:y=55"
    lines.append(filters+f'[clip{i}];')
lines.append(''.join(f'[clip{i}]' for i in range(len(spec)))+f'concat=n={len(spec)}:v=1:a=0[video];')
frames=sum(x['end']-x['start'] for x in spec)
duration=frames/30
lines.append(f'[2:a]atrim=duration={duration},asetpts=PTS-STARTPTS,volume=0.16,afade=t=in:st=0:d=1.2,afade=t=out:st={duration-2}:d=2[audio]')
(MAIN/'剪辑滤镜.txt').write_text('\n'.join(lines),encoding='utf-8')
cmd=[str(FFMPEG),'-hide_banner','-loglevel','error','-y','-i',str(MAIN/'原始录制.mp4'),'-i',str(BOX/'原始录制.mp4'),'-stream_loop','-1','-i',str(ROOT/'Assets/天帝/音频/music-0.wav'),'-filter_complex_script',str(MAIN/'剪辑滤镜.txt'),'-map','[video]','-map','[audio]','-r','30','-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart','-metadata','title=第二期开发日志 最新版介绍','-t',str(duration),str(OUT/'第二期最新版介绍.mp4')]
subprocess.run(cmd,check=True)
time=0
chapters=[]
for x in spec:
    d=(x['end']-x['start'])/30
    chapters.append({'开始秒':round(time,3),'结束秒':round(time+d,3),'内容':x['name'],'画面速度':1})
    time+=d
(MAIN/'视频剪辑记录.json').write_text(json.dumps({'frames':frames,'duration':duration,'clips':chapters},ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'视频章节.md').write_text('# 介绍视频章节\n\n1920×1080，30fps，中文字幕和游戏配乐。战斗为原速选段，跳切省略重复清怪等待；宝盒画面来自同版本的独立补拍。\n\n'+'\n'.join(f"- {int(x['开始秒'])//60:02d}:{int(x['开始秒'])%60:02d} {x['内容']}" for x in chapters)+'\n',encoding='utf-8')
print(json.dumps({'video':str(OUT/'第二期最新版介绍.mp4'),'duration':duration,'frames':frames},ensure_ascii=False))
