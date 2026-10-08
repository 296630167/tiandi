"""处理Tap原画：抠除黑底、裁切、对齐、打包，不绘制特效内容。"""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image,ImageOps,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1]
out=root/'output/战斗生成美术'
maker=Path('C:/Users/123/Documents/taptap製造_一滴水的故事')
target=root/'Assets/天帝/Resources/战斗特效'
target.mkdir(parents=True,exist_ok=True)
tiles=out/'透明成品';tiles.mkdir(exist_ok=True)
bullet=json.loads((out/'正式弹体结果.json').read_text(encoding='utf-8'))['structuredContent']
effects=json.loads((out/'正式预警批量结果.json').read_text(encoding='utf-8'))['structuredContent']['results']
names=['金弹','木弹','水弹','火弹','土弹','灵弹','预警环','危险区','冲锋框','尾迹','冲击环','爆闪']
atlas=Image.new('RGBA',(2048,1536));preview=Image.new('RGB',(1536,640),(32,48,43));font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
records=[];metrics=[]
sheet=Image.open(maker/bullet['localPath']).convert('RGB')
for n,name in enumerate(names):
    result=bullet if n<6 else effects[n-6]
    if not result['success'] or not result['download']['success']:raise ValueError(name+'未下载成功')
    path=maker/result['localPath']
    im=sheet.crop((round(n%2*sheet.width/2),round(n//2*sheet.height/3),round((n%2+1)*sheet.width/2),round((n//2+1)*sheet.height/3))) if n<6 else Image.open(path).convert('RGB')
    rgb=np.array(im).astype(np.float32)/255
    if max(rgb[0,0].max(),rgb[-1,-1].max(),rgb[0,-1].max(),rgb[-1,0].max())>.1:raise ValueError(name+'背景不是黑底')
    # 把黑底发光颜色分解为直通alpha，防止RGB溢出导致红绿杂边。
    alpha=np.maximum(0,(rgb.max(axis=2)-2/255)/(1-2/255))
    clean=np.clip(rgb/np.maximum(alpha[:,:,None],1e-6),0,1)
    rgba=np.dstack((clean,alpha));rgba=(rgba*255+.5).astype(np.uint8)
    ys,xs=np.where(alpha>12/255)
    if len(xs)<100:raise ValueError(name+'素材空白')
    box=(int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1))
    if n in (6,7):
        # 以原图明亮圆环的径向平均亮度标定边界，去掉圈外装饰留白。
        cx=(box[0]+box[2]-1)/2;cy=(box[1]+box[3]-1)/2
        y,x=np.mgrid[:im.height,:im.width];r=np.sqrt((x-cx)**2+(y-cy)**2).astype(int)
        sums=np.bincount(r.ravel(),weights=rgb.max(axis=2).ravel());counts=np.bincount(r.ravel())
        profile=sums/np.maximum(counts,1);low=int(min(im.size)*.22);high=int(min(im.size)*.45)
        peak=low+int(np.argmax(profile[low:high]));outer=peak
        while outer+1<high and profile[outer+1]>profile[peak]*.35:outer+=1
        box=(round(cx-outer),round(cy-outer),round(cx+outer),round(cy+outer))
    if n==8:
        # 对齐直边，排除边框外飘带；框的UV边界就是真实宽度和长度。
        xprofile=rgb[int(im.height*.25):int(im.height*.75)].max(axis=2).mean(axis=0)
        yprofile=rgb[:,int(im.width*.4):int(im.width*.6)].max(axis=2).mean(axis=1)
        xx=np.where(xprofile>xprofile.max()*.6)[0];yy=np.where(yprofile>yprofile.max()*.5)[0]
        box=(int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1))
    crop=Image.fromarray(rgba).crop(box)
    aspect=crop.width/crop.height
    anchor=.5
    if n<6:
        lum=np.array(im.crop(box)).max(axis=2).astype(float)
        lum[:,:int(lum.shape[1]*.5)]=0
        yy,xx=np.where(lum>=lum.max()*.92)
        anchor=float(np.median(xx)/max(1,lum.shape[1]-1))
        crop=crop.transpose(Image.Transpose.ROTATE_90)
    # UV格内直接规范化；弹体保留原始宽高比，长条由真实范围定宽和长。
    crop=crop.resize((496,496),Image.Resampling.LANCZOS)
    if n in (6,7):
        # 将生成原画裁在正圆内，装饰不越过实际攻击圆；不添加新图案。
        data=np.array(crop);y,x=np.mgrid[:496,:496];rad=((x-247.5)**2+(y-247.5)**2)**.5
        data[:,:,3]=(data[:,:,3]*(np.clip(248-rad,0,1))).astype(np.uint8);crop=Image.fromarray(data)
    tile=Image.new('RGBA',(512,512));tile.alpha_composite(crop,(8,8))
    tile.save(tiles/(name+'.png'));atlas.alpha_composite(tile,((n%4)*512,(n//4)*512))
    thumb=tile.resize((240,240),Image.Resampling.LANCZOS);preview.paste(thumb,((n%6)*256,(n//6)*320),thumb)
    ImageDraw.Draw(preview).text(((n%6)*256+12,(n//6)*320+250),name,fill='white',font=font)
    metrics.append(dict(长宽比=aspect if n<6 else 1,锚点=anchor))
    records.append(dict(id=n,name=name,source=str(path),cdn=result['previewUrl'],sha256=hashlib.sha256(path.read_bytes()).hexdigest(),bbox=box,alpha='black_glow_unpremultiply_clamped',aspect=aspect,anchor=anchor))
atlas.save(target/'战斗特效图集.png')
(target/'战斗特效尺寸.json').write_text(json.dumps(dict(条目=metrics),ensure_ascii=False,indent=2),encoding='utf-8')
(out/'正式素材来源.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
preview.save(out/'正式素材检查.png')
print(json.dumps(dict(count=len(records),atlas=str(target/'战斗特效图集.png')),ensure_ascii=True))
