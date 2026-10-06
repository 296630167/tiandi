"""整理返工候选：补下载已有结果、固定头脸/躯干、按共同画布生成预览。"""
from pathlib import Path
import json
import math
import urllib.request
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from Tap素材代理 import 制造项目
from 整理主角动画帧 import 透明, 头部测量

根=Path(__file__).resolve().parents[1]
输出=根/'output/imagegen/主角十组动画/正面走路返工_v3'
母版=制造项目/'assets/image/天帝主角动画_20261005/母版/基准_南.png'


def 取图(i):
    p=输出/'原始返回'/f'移动_南_{i:02}.json'
    if not p.exists():return None
    d=json.loads(p.read_text(encoding='utf-8'));s=d.get('result',{}).get('structuredContent',{})
    if not s.get('success'):return None
    if not s.get('absolutePath'):
        dest=制造项目/'assets/image'/f'天帝正面步态修正_v3_移动_南_{i:02}_下载恢复.png'
        if not dest.exists():
            with urllib.request.urlopen(s['previewUrl'],timeout=60) as r:dest.write_bytes(r.read())
        with Image.open(dest) as im:im.verify()
        s.update({'absolutePath':str(dest),'localPath':str(dest.relative_to(制造项目)).replace('\\','/'),
                  'download':{'success':True,'recovered_existing_url':True}})
        d['result']['content']=[{'type':'text','text':json.dumps(s,ensure_ascii=False,indent=2)}]
        d['local_delivery_recovery']='existing previewUrl download only; no regeneration'
        p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
        mapfile=制造项目/'.maker/assets/generated-assets.json'
        mapping=json.loads(mapfile.read_text(encoding='utf-8-sig'))
        mapping[s['localPath']]={'tool':'edit_image','name':s['name'],'prompt':s['prompt'],'previewUrl':s['previewUrl'],
                               'localPath':s['localPath'],'absolutePath':str(dest),'localDeliveryReconciled':True}
        mapfile.write_text(json.dumps(mapping,ensure_ascii=False,indent=2),encoding='utf-8')
    return 透明(Image.open(s['absolutePath']))


def 校正(i,图):
    基准=Image.open(母版).convert('RGBA');a=头部测量(图);b=头部测量(基准)
    scale=b['width']/a['width'];bob=-math.sin((i-1)/24*math.tau*2)*3
    x=round(b['x']-a['x']*scale);y=round(b['top']+bob-a['top']*scale)
    aligned=Image.new('RGBA',(1024,1024));aligned.alpha_composite(图.resize((round(1024*scale),round(1024*scale)),Image.Resampling.LANCZOS),(x,y))
    ref=Image.new('RGBA',(1024,1024));ref.alpha_composite(基准,(0,round(bob)))
    mask=Image.new('L',(1024,1024));d=ImageDraw.Draw(mask)
    d.rectangle((0,0,1023,324+bob),fill=255)
    # 躯干中心纹理保持原画；保留肩部及两臂的姿态。
    d.polygon([(450,321+bob),(576,321+bob),(583,405+bob),(583,507+bob),
               (589,565+bob),(439,565+bob),(443,475+bob),(446,394+bob)],fill=255)
    mask=mask.filter(ImageFilter.GaussianBlur(4))
    done=Image.composite(ref,aligned,mask)
    return done,{'frame':i,'raw_head':a,'head_scale_correction':scale,'translation':[x,y],'bob':bob}


def main():
    frames=[];report=[];missing=[]
    out=输出/'处理后素材';out.mkdir(exist_ok=True)
    for i in range(1,25):
        im=取图(i)
        if im is None:missing.append(i);continue
        im,info=校正(i,im);report.append(info)
        im=im.resize((512,512),Image.Resampling.LANCZOS);im.save(out/f'主角_移动_南_{i:02}.png')
        bg=Image.new('RGB',(512,512),(235,234,223));bg.paste(im,(0,0),im);frames.append((i,bg))
    font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',20)
    sheet=Image.new('RGB',(2048,1680),(235,234,223));d=ImageDraw.Draw(sheet)
    for j,(i,im) in enumerate(frames):
        x=j%6*341;y=j//6*420
        sheet.paste(im.resize((341,341)),(x,y+26));d.text((x+155,y+2),f'{i:02}',font=font,fill=(38,68,52))
    sheet.save(输出/'正面走路_逐帧对照.png')
    if len(frames)==24:
        images=[im for _,im in frames]
        for name,ms in [('正面走路_原速.gif',50),('正面走路_半速.gif',100)]:
            images[0].save(输出/name,save_all=True,append_images=images[1:],duration=ms,loop=0,disposal=2)
    (输出/'尺寸校正记录.json').write_text(json.dumps({'generated':len(frames),'pending':missing,'visual_approved':False,'unity_imported':False,'method':'uniform camera correction plus master head/central torso compositing, not per-axis stretch','frames':report},ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'processed':len(frames),'pending':missing},ensure_ascii=False))


if __name__=='__main__':main()
