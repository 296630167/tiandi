"""只处理背景与共同坐标，不以逐帧非等比缩放掩盖体型漂移。"""
from pathlib import Path
import argparse
import json
import math
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont
from Tap素材代理 import 制造项目

根=Path(__file__).resolve().parents[1]
交付=根/'output/imagegen/主角十组动画'
制造=制造项目
源目录=制造/'assets/image/天帝主角动画_20261005'


def 透明(图):
    a=np.asarray(图.convert('RGBA')).copy()
    rgb=a[:,:,:3].astype(float)
    色差=np.minimum(rgb[:,:,0],rgb[:,:,2])-rgb[:,:,1]
    背景=(色差>70)&(rgb[:,:,0]>140)&(rgb[:,:,2]>130)
    掩码=(~背景)&(a[:,:,3]>128)
    a[:,:,3]=np.where(掩码,255,0).astype('uint8')
    内部=np.asarray(Image.fromarray((掩码*255).astype('uint8')).filter(ImageFilter.MinFilter(5)))>128
    外圈=掩码&~内部
    污染=外圈&(色差>15)
    a[:,:,0][污染]=np.minimum(a[:,:,0][污染],a[:,:,1][污染]+15)
    a[:,:,2][污染]=np.minimum(a[:,:,2][污染],a[:,:,1][污染]+15)
    return Image.fromarray(a)


def 母版(来源,输出):
    图=透明(Image.open(来源));框=图.getbbox()
    if not 框:raise ValueError('母版没有人物')
    倍率=800/(框[3]-框[1])
    # 每个方向母版只建立一次基准，不将此操作逐帧执行。
    调整=图.resize((round(图.width*倍率),round(图.height*倍率)),Image.Resampling.LANCZOS)
    左=round(512-(框[0]+框[2])*.5*倍率);上=round(928-框[3]*倍率)
    完成=Image.new('RGBA',(1024,1024));完成.alpha_composite(调整,(左,上))
    输出.parent.mkdir(parents=True,exist_ok=True);完成.save(输出)
    带底=Image.new('RGBA',(1024,1024),(255,0,255,255));带底.alpha_composite(完成)
    带底.save(输出.with_name(输出.stem+'_生图参考.png'))
    报告={'source':str(来源),'source_bounds':框,'baseline_scale':倍率,'offset':[左,上],
         'output':str(输出),'root':[512,928]}
    输出.with_suffix('.json').write_text(json.dumps(报告,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(报告,ensure_ascii=False))


def 头部测量(图):
    a=np.asarray(图);框=图.getbbox()
    if not 框:raise ValueError('空白帧')
    m=a[:,:,3]>160
    # 上方135像素只包括头发轮廓，不使用整个全身包围盒推断尺度。
    起=框[1];行=[];中心=[]
    for y in range(起+18,min(起+145,1024)):
        xx=np.where(m[y])[0]
        if len(xx)>35:行.append(xx[-1]-xx[0]+1);中心.append((xx[-1]+xx[0])*.5)
    if not 行:raise ValueError('未识别头部轮廓')
    return {'top':起,'width':float(np.percentile(行,80)),'x':float(np.median(中心))}


def 羽化掩码(上,下,反=False):
    值=np.clip((下-np.arange(1024))/(下-上),0,1)
    if 反:值=1-值
    a=np.tile((值[:,None]*255).astype('uint8'),(1,1024))
    return Image.fromarray(a)


def 处理(项,原图,基准):
    图=透明(原图);原测=头部测量(图);基测=头部测量(基准)
    # 校准生成器意外的镜头缩放：整张图统一等比调整，绝不分开拉伸宽高。
    比例=基测['width']/原测['width']
    相位=(项['frame']-1)/16*2*math.pi
    偏移=-math.sin(相位*2)*4.9 if 项['action']=='移动' else -math.sin(相位)*1.6 if 项['action']=='待机' else 0
    左=round(基测['x']-原测['x']*比例);上=round(128+偏移-原测['top']*比例)
    放大=图.resize((round(1024*比例),round(1024*比例)),Image.Resampling.LANCZOS)
    完成=Image.new('RGBA',(1024,1024));完成.alpha_composite(放大,(左,上))
    # 局部编辑中约定不变的头脸从母版恢复，消除AI换脸/发型轮廓闪烁。
    头=Image.new('RGBA',(1024,1024));头.alpha_composite(基准,(0,round(偏移)))
    头遮=羽化掩码(322+偏移,347+偏移)
    if 项['action']=='待机' and 项['frame'] in [12,13,14]:
        # 仅保留本帧生成的眼睑，眼镜外框/眉毛/其他脸部使用相同母版。
        d=ImageDraw.Draw(头遮)
        for x0,x1 in [(451,497),(521,566)]:d.rounded_rectangle((x0,257+round(偏移),x1,276+round(偏移)),radius=5,fill=0)
    完成=Image.composite(头,完成,头遮)
    if 项['action'] in ['待机','射击']:
        # 定点动作双腿不动；恢复未编辑的站立双腿，保持鞋底逐像素稳定。
        完成=Image.composite(基准,完成,羽化掩码(594,617,True))
    # 仅把共同1024画布统一缩至512，所有成品共用(256,464)根节点。
    成品=完成.resize((512,512),Image.Resampling.LANCZOS)
    数=np.asarray(成品);实底=数[464:,:,:3]
    框=成品.getbbox()
    检查=[]
    if not .92<=比例<=1.08:检查.append('生成镜头尺度偏移超过8%，需重画')
    if 框 and (框[0]<16 or 框[2]>496 or 框[1]<40 or 框[3]>503):检查.append('角色过近成品边缘，需检查裁切')
    报告={'name':项['name'],'raw_head':原测,'master_head':基测,'camera_scale_correction':round(比例,5),
          'translation':[左,上],'intentional_head_y':round(偏移/2,2),'bounds':框,'review_flags':检查}
    return 成品,报告


def 制作预览(组,目录):
    字体=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
    for 名,帧组 in 组.items():
        if len(帧组)!=16:continue
        页=Image.new('RGB',(1536,1664),(235,234,223));画=ImageDraw.Draw(页);动=[]
        for 序,图 in enumerate(帧组):
            小=图.resize((384,384),Image.Resampling.LANCZOS);x,y=序%4*384,序//4*416
            页.paste(小,(x,y+28),小);画.text((x+173,y+3),f'{序+1:02d}',font=字体,fill=(38,68,52))
            单=Image.new('RGB',(512,512),(235,234,223));单.paste(图,(0,0),图);动.append(单)
        页.save(目录/f'{名}_逐帧对照.png')
        秒=125 if 名.startswith('待机') else 63 if 名.startswith('移动') else 70
        动[0].save(目录/f'{名}_循环预览.gif',save_all=True,append_images=动[1:],duration=秒,loop=0,disposal=2)
        动[0].save(目录/f'{名}_半速检查.gif',save_all=True,append_images=动[1:],duration=秒*2,loop=0,disposal=2)


def 全部(只组='all'):
    清单=json.loads((交付/'生成清单.json').read_text(encoding='utf-8'))
    输出=交付/'处理后素材';输出.mkdir(exist_ok=True)
    预览=交付/'动画预览';预览.mkdir(exist_ok=True)
    报告=[];未完=[];组={}
    for 项 in 清单['frames']:
        if 项['master']:continue
        名=项['name'];组名=名.rsplit('_',1)[0]
        if 只组!='all' and 组名!=只组:continue
        记录=交付/'逐帧生成结果'/f'{名}.json'
        if not 记录.exists():未完.append(名);continue
        r=json.loads(记录.read_text(encoding='utf-8'));s=r.get('result',{}).get('structuredContent',{})
        if not s.get('success') or not s.get('absolutePath'):未完.append(名);continue
        基准=Image.open(源目录/'母版'/f'基准_{项["direction"]}.png').convert('RGBA')
        图,测=处理(项,Image.open(s['absolutePath']),基准);图.save(输出/f'主角_{名}.png')
        报告.append(测);组.setdefault(组名,[]).append(图)
    制作预览(组,预览)
    记录={'completed':len(报告),'pending':未完,'flags':[r for r in 报告 if r['review_flags']],
          'frame_size':[512,512],'root':[256,464],'frames':报告}
    (交付/f'尺寸检查_{只组}.json').write_text(json.dumps(记录,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'completed':len(报告),'pending':len(未完),'flagged':len(记录['flags'])},ensure_ascii=False))


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('action',choices=['master','process']);p.add_argument('source',type=Path,nargs='?');p.add_argument('output',type=Path,nargs='?');p.add_argument('--group',default='all')
    a=p.parse_args()
    if a.action=='master':母版(a.source,a.output)
    else:全部(a.group)
