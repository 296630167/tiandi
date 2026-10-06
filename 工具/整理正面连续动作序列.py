"""固定格切分，不按逐帧包围盒缩放；生成可暂停、逐帧检查的预览。"""
from pathlib import Path
import json
import urllib.request
from PIL import Image, ImageDraw, ImageFont
from 整理主角动画帧 import 透明
from Tap素材代理 import 制造项目

根=Path(__file__).resolve().parents[1]
输出=根/'output/imagegen/主角十组动画/正面连续序列_v4'


def main():
    record=输出/'原始返回.json'
    data=json.loads(record.read_text(encoding='utf-8'))
    result=data.get('result',{})
    info=result.get('structuredContent',{})
    if not info.get('success') or result.get('isError'):
        raise RuntimeError('远程未成功返回，不能生成成品预览。')
    source=Path(info['absolutePath']) if info.get('absolutePath') else None
    if source is None:
        source=制造项目/'assets/image/天帝_正面连续走路16帧_v4_下载恢复.png'
        if not source.exists():
            with urllib.request.urlopen(info['previewUrl'],timeout=60) as r:
                source.write_bytes(r.read())
        with Image.open(source) as im:im.verify()
        info.update({'absolutePath':str(source),'localPath':str(source.relative_to(制造项目)).replace('\\','/'),
                     'download':{'success':True,'recovered_existing_url':True}})
        data['local_delivery_recovery']='Recovered existing previewUrl; no second generation request.'
        result['content']=[{'type':'text','text':json.dumps(info,ensure_ascii=False,indent=2)}]
        record.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
        mapfile=制造项目/'.maker/assets/generated-assets.json'
        mapping=json.loads(mapfile.read_text(encoding='utf-8-sig'))
        mapping[info['localPath']]={'tool':'edit_image','name':info['name'],'prompt':info['prompt'],'previewUrl':info['previewUrl'],
                                 'localPath':info['localPath'],'absolutePath':str(source),'localDeliveryReconciled':True}
        mapfile.write_text(json.dumps(mapping,ensure_ascii=False,indent=2),encoding='utf-8')
    sheet=Image.open(source).convert('RGBA')
    if sheet.size!=(3072,3072):raise ValueError(f'返回尺寸{sheet.size}不符合3072×3072；不猜测切分坐标。')
    (输出/'处理后素材').mkdir(exist_ok=True)
    frames=[];report=[]
    bg=(235,234,223)
    font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
    contact=Image.new('RGB',(1536,1664),bg);draw=ImageDraw.Draw(contact)
    for i in range(16):
        x=i%4*768;y=i//4*768
        im=透明(sheet.crop((x,y,x+768,y+768))).resize((512,512),Image.Resampling.LANCZOS)
        im.save(输出/'处理后素材'/f'主角_移动_南_{i+1:02}.png')
        frame=Image.new('RGB',(512,512),bg);frame.paste(im,(0,0),im);frames.append(frame)
        cx=i%4*384;cy=i//4*416
        contact.paste(frame.resize((384,384),Image.Resampling.LANCZOS),(cx,cy+28))
        draw.text((cx+173,cy+3),f'{i+1:02}',font=font,fill=(38,68,52))
        bounds=im.getbbox()
        report.append({'frame':i+1,'source_cell':[x,y,768,768],'bounds':bounds,'height':bounds[3]-bounds[1] if bounds else 0,
                       'edge_flag':bool(bounds and (bounds[0]<12 or bounds[2]>500 or bounds[1]<20 or bounds[3]>505))})
    contact.save(输出/'正面走路_逐帧对照.png')
    frames[0].save(输出/'正面走路_原速.gif',save_all=True,append_images=frames[1:],duration=[70,80]*8,loop=0,disposal=2)
    frames[0].save(输出/'正面走路_半速.gif',save_all=True,append_images=frames[1:],duration=150,loop=0,disposal=2)
    preview='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>天帝 · 正面步态返工预览</title>
<style>body{margin:0;background:#171e1d;color:#e8eadf;font:16px 'Microsoft YaHei',sans-serif}main{max-width:960px;margin:32px auto;padding:24px}h1{font-size:24px;margin:0 0 12px}.note{color:#c3cbbf;line-height:1.7}.stage{display:flex;justify-content:center;background:#ebeadd;border-radius:12px;margin:22px 0;position:relative}.stage img{width:512px;height:512px;max-width:100%;object-fit:contain}.controls{display:flex;align-items:center;gap:14px;flex-wrap:wrap}button,select{padding:10px 16px;border:1px solid #779684;border-radius:6px;background:#2b3c34;color:#f1f3e9;font:inherit}input{flex:1;min-width:180px}a{color:#a5d4ad}.counter{font-variant-numeric:tabular-nums;min-width:90px}.links{display:flex;gap:24px;margin-top:24px}p{line-height:1.7}</style>
<main><h1>正面走路 · 连续序列候选</h1><p class="note">16帧 / 每圈1.2秒 · 同一画布固定格切分，全部使用相同尺寸和根位置。当前为返工预览，尚未接入Unity。</p>
<div class="stage"><img id="pose" alt="正面走路动画"></div><div class="controls"><button id="play">暂停</button><button id="previous">上一帧</button><button id="next">下一帧</button><select id="speed"><option value="1">原速</option><option value="0.5">半速</option></select><input id="seek" type="range" min="0" max="15" value="0"><span id="counter" class="counter"></span></div>
<p class="note">用暂停和逐帧按钮检查左右脚支撑顺序、摆臂、袖口与裤腿变化，以及16→01循环接缝。左右方向键也可以切换帧。</p>
<div class="links"><a href="正面走路_原速.gif">原速GIF</a><a href="正面走路_半速.gif">半速GIF</a><a href="正面走路_逐帧对照.png">逐帧对照图</a></div></main>
<script>const images=Array.from({length:16},(_,i)=>{let im=new Image();im.src=`处理后素材/主角_移动_南_${String(i+1).padStart(2,'0')}.png`;return im});let f=0,playing=true,rate=1,last=0,elapsed=0;const pose=document.getElementById('pose'),seek=document.getElementById('seek'),counter=document.getElementById('counter'),play=document.getElementById('play');function show(){pose.src=images[f].src;seek.value=f;counter.textContent=`${String(f+1).padStart(2,'0')} / 16`}function pause(){playing=false;play.textContent='播放'}function move(n){pause();f=(f+n+16)%16;show()}play.onclick=()=>{playing=!playing;play.textContent=playing?'暂停':'播放';elapsed=0};document.getElementById('previous').onclick=()=>move(-1);document.getElementById('next').onclick=()=>move(1);seek.oninput=()=>{pause();f=Number(seek.value);show()};document.getElementById('speed').onchange=e=>{rate=Number(e.target.value);elapsed=0};document.addEventListener('keydown',e=>{if(e.key==='ArrowRight'){e.preventDefault();move(1)}if(e.key==='ArrowLeft'){e.preventDefault();move(-1)}});function tick(t){if(last&&playing){elapsed+=(t-last)*rate;while(elapsed>=75){elapsed-=75;f=(f+1)%16;show()}}last=t;requestAnimationFrame(tick)}show();requestAnimationFrame(tick);</script></html>'''
    (输出/'正面步态预览.html').write_text(preview,encoding='utf-8')
    (输出/'切帧记录.json').write_text(json.dumps({'source':str(source),'frames':report,'frame_size':[512,512],'root':[256,464],
        'method':'Uniform fixed-cell crop and common 2/3 downsampling. No per-frame resizing, mirroring, duplication, optical-flow or head/torso replacement.',
        'visual_approved':False,'unity_imported':False},ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'processed':len(frames),'edge_flags':[r['frame'] for r in report if r['edge_flag']]},ensure_ascii=False))


if __name__=='__main__':main()
