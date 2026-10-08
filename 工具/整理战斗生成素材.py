"""只裁切与整理远程生成素材，不绘制特效内容。原图留在Maker项目。"""
from pathlib import Path
import argparse,json
import numpy as np
from PIL import Image,ImageOps,ImageDraw,ImageFont

root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('projectiles',type=Path)
parser.add_argument('effects',type=Path)
args=parser.parse_args()
target=root/'Assets/天帝/Resources/战斗特效'
target.mkdir(parents=True,exist_ok=True)
output=root/'output/战斗生成美术'
output.mkdir(parents=True,exist_ok=True)
names=['金弹','木弹','水弹','火弹','土弹','灵弹','预警环','危险区','冲锋框','尾迹','冲击环','爆闪']
atlas=Image.new('RGBA',(2048,1536))
preview=Image.new('RGB',(1536,1024),(30,42,42))
preview_font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',24)
records=[]
for source_index,path in enumerate([args.projectiles,args.effects]):
    image=Image.open(path).convert('RGBA')
    for index in range(6):
        col,row=index%2,index//2
        cell=image.crop((round(col*image.width/2),round(row*image.height/3),round((col+1)*image.width/2),round((row+1)*image.height/3)))
        data=np.array(cell)
        if data[:,:,3].min()==255:
            # 仅允许黑底光效抠底，拒绝白底/伪透明背景冒充透明成品。
            corners=np.array([data[0,0,:3],data[0,-1,:3],data[-1,0,:3],data[-1,-1,:3]])
            if corners.max()>35:raise ValueError(f'{path.name} cell{index}: needs transparent output or manual background inspection')
            luminance=data[:,:,:3].max(axis=2).astype(float)
            alpha=np.clip((luminance-8)/247,0,1)
            rgb=np.divide(data[:,:,:3],np.maximum(alpha[:,:,None],.001))
            data[:,:,:3]=np.clip(rgb,0,255).astype('uint8');data[:,:,3]=(alpha*255).astype('uint8');cell=Image.fromarray(data)
        mask=np.array(cell.getchannel('A'))>12
        ys,xs=np.where(mask)
        if len(xs)<100:raise ValueError(f'Empty source cell {index}')
        box=(int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1))
        crop=cell.crop(box)
        n=source_index*6+index
        if source_index==0:crop=crop.transpose(Image.Transpose.ROTATE_90)
        # 圆形成品按外接矩形归一到正方形；长条按原始长宽填充其格。
        if n in (6,7,10):crop=crop.resize((496,496),Image.Resampling.LANCZOS)
        else:crop=ImageOps.contain(crop,(496,496),Image.Resampling.LANCZOS)
        tile=Image.new('RGBA',(512,512))
        tile.alpha_composite(crop,((512-crop.width)//2,(512-crop.height)//2))
        tile.save(target/(names[n]+'.png'))
        atlas.alpha_composite(tile,((n%4)*512,(n//4)*512))
        p=tile.resize((256,256),Image.Resampling.LANCZOS)
        preview.paste(p,((n%6)*256,(n//6)*512),p)
        ImageDraw.Draw(preview).text(((n%6)*256+8,(n//6)*512+270),f'{n:02} {names[n]}',fill='white',font=preview_font)
        records.append({'id':n,'name':names[n],'source':str(path),'cell':index,'source_bbox':box,'alpha_min':int(np.array(tile.getchannel('A')).min()),'alpha_max':int(np.array(tile.getchannel('A')).max())})
atlas.save(target/'战斗特效图集.png')
preview.save(output/'素材检查.png')
(output/'素材来源.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'count':len(records),'atlas':str(target/'战斗特效图集.png')},ensure_ascii=False))
