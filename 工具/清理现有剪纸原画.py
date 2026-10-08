"""远程编辑超时后的原画清理：只编辑已拥有的位图，不派发新请求。"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import numpy as np
import cv2, json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'output/剪纸全页修复_20261007'
BACK=OUT/'修改前/Assets/天帝/Resources/剪纸界面'
DEST=OUT/'本地原画清理';DEST.mkdir(exist_ok=True)

# 人物粗轮廓仅用于前景/背景种子，实际边缘由原图GrabCut分割。
im=Image.open(BACK/'角色纸雕.png').convert('RGB');a=np.array(im)
points=[(140,0),(172,3),(190,20),(193,54),(188,70),(188,84),(199,89),(211,111),(227,128),(242,171),(251,210),(250,231),(240,243),(222,246),(224,279),(215,299),(209,301),(210,329),(216,383),(225,464),(234,552),(240,592),(240,607),(232,625),(236,642),(246,673),(246,692),(228,699),(205,692),(196,681),(193,662),(192,644),(180,634),(184,615),(175,563),(159,522),(149,468),(142,419),(141,408),(139,460),(139,513),(150,576),(156,600),(154,616),(150,633),(137,647),(137,661),(117,670),(91,676),(73,674),(69,663),(77,648),(96,631),(106,619),(106,601),(100,555),(92,500),(92,450),(90,399),(87,350),(85,304),(82,291),(82,272),(85,239),(70,246),(55,238),(47,219),(53,196),(62,173),(68,145),(81,123),(91,106),(103,98),(118,84),(126,84),(124,75),(119,62),(115,43),(115,22)]
poly=np.zeros(a.shape[:2],np.uint8);cv2.fillPoly(poly,[np.array(points,np.int32)],255)
mask=np.full(poly.shape,cv2.GC_BGD,np.uint8)
mask[cv2.dilate(poly,np.ones((17,17),np.uint8))>0]=cv2.GC_PR_BGD;mask[poly>0]=cv2.GC_PR_FGD
mask[cv2.erode(poly,np.ones((7,7),np.uint8))>0]=cv2.GC_FGD
cv2.grabCut(a,mask,None,np.zeros((1,65),np.float64),np.zeros((1,65),np.float64),8,cv2.GC_INIT_WITH_MASK)
alpha=np.uint8((mask==cv2.GC_FGD)|(mask==cv2.GC_PR_FGD))*255
Image.fromarray(np.dstack((a,alpha))).save(DEST/'角色.png')

# 从已批准标题概念去除字形和按钮；标题天际是纸纹，操作位置是湖面。
src=ROOT.parent/'预览/天帝标题与战斗剪纸概念_20261007/01_开始游戏标题页_实际内容.png'
a=np.array(Image.open(src).convert('RGB'));hsv=cv2.cvtColor(a,cv2.COLOR_RGB2HSV);mask=np.zeros(a.shape[:2],np.uint8)
roi=np.zeros_like(mask);roi[120:446,625:1355]=255
ink=((hsv[:,:,0]>30)&(hsv[:,:,0]<110)&(hsv[:,:,1]>65)&(hsv[:,:,2]<175))
mask[(roi>0)&ink]=255;mask=cv2.dilate(mask,np.ones((5,5),np.uint8))
for box in [(1620,20,1900,116),(690,672,1220,801),(1600,930,1908,1034),(25,991,229,1053)]:
    x0,y0,x1,y1=box;mask[y0:y1,x0:x1]=255
a=cv2.inpaint(a,mask,9,cv2.INPAINT_TELEA)
# 大块文字不能仅靠小半径补洞：纸纹天空重新铺展，柔和收口到原山体。
original=Image.open(src).convert('RGB');sky=original.crop((820,20,1080,85))
tile=Image.new('RGB',(520,130));tile.paste(sky,(0,0));tile.paste(sky.transpose(Image.Transpose.FLIP_LEFT_RIGHT),(260,0));tile.paste(tile.crop((0,0,520,65)).transpose(Image.Transpose.FLIP_TOP_BOTTOM),(0,65))
canvas=Image.new('RGB',original.size)
for y in range(0,1080,130):
    for x in range(0,1920,520):canvas.paste(tile,(x,y))
result=Image.fromarray(a)
yy,xx=np.mgrid[:1080,:1920];dist=np.minimum.reduce([xx-505,1460-xx,yy+20,560-yy])
t=np.clip(dist/105,0,1);m=t*t*(3-2*t)
setting=np.minimum.reduce([xx-1520,1960-xx,yy+70,210-yy]);s=np.clip(setting/80,0,1);m=np.maximum(m,s*s*(3-2*s))
result=Image.composite(canvas,result,Image.fromarray(np.uint8(m*255)))
# 主按钮下的湖面以相邻同层水纹修补，避免大片直线模糊。
water=original.crop((650,558,1290,681)).resize((640,123))
layer=result.copy();layer.paste(water,(650,681));wm=Image.new('L',original.size);ImageDraw.Draw(wm).rectangle((670,687,1270,788),fill=255)
result=Image.composite(layer,result,wm.filter(ImageFilter.GaussianBlur(18)))
foliage=original.crop((1690,775,1920,945)).resize((300,170));layer=result.copy();layer.paste(foliage,(1600,905));fm=Image.new('L',original.size);ImageDraw.Draw(fm).rectangle((1610,936,1878,1032),fill=255)
result=Image.composite(layer,result,fm.filter(ImageFilter.GaussianBlur(20)))
result.save(DEST/'标题.png')

# 真正可伸缩纸框：已有纸材+提取的朱红角纹，没有任何内容截图。
paper=Image.open(BACK/'素纸.png').convert('RGBA').resize((480,320))
frame=Image.new('RGBA',(480,320));shape=Image.new('L',frame.size);ImageDraw.Draw(shape).rounded_rectangle((6,6,473,313),radius=7,fill=255)
frame.paste(paper,(0,0),shape);d=ImageDraw.Draw(frame);d.rectangle((10,10,469,309),outline=(158,58,42,255),width=1);d.rectangle((13,13,466,306),outline=(158,58,42,120),width=1)
old=Image.open(BACK/'朱红纸框.png').convert('RGB');w,h=old.size
for box,pos in [((0,0,60,60),(8,8)),((w-60,0,w,60),(456,8)),((0,h-60,60,h),(8,296)),((w-60,h-60,w,h),(456,296))]:
    corner=old.crop(box);v=np.array(corner);red=(v[:,:,0]>v[:,:,1]*1.3)&(v[:,:,0]>v[:,:,2]*1.4)&(v[:,:,1]<150)
    part=Image.fromarray(np.dstack((v,np.uint8(red)*255))).resize((16,16),Image.Resampling.LANCZOS);frame.alpha_composite(part,pos)
frame.save(DEST/'纸框.png')

button=Image.open(BACK/'墨绿按钮.png').convert('RGBA').resize((640,160),Image.Resampling.LANCZOS);a=np.array(button)
hsv=cv2.cvtColor(a[:,:,:3],cv2.COLOR_RGB2HSV);green=(hsv[:,:,0]>30)&(hsv[:,:,0]<110)&(hsv[:,:,1]>35)
texture=np.asarray(paper.resize(button.size))[:,:,:3].mean(2);shade=.96+.08*(texture-texture.mean())/255
core=np.zeros(a.shape[:2],bool);core[21:140,78:563]=True
color=np.median(a[:,:,:3][green],axis=0)
a[:,:,:3][core&green]=np.uint8((color[None,None,:]*shade[:,:,None])[core&green])
# 去除旧自动抠图残留的孤立脏点，保留连通的正式板形与小花纹。
count,labels,stats,_=cv2.connectedComponentsWithStats(np.uint8(a[:,:,3]>32),8)
keep=np.where(stats[:,cv2.CC_STAT_AREA]>45)[0];keep=keep[keep!=0];a[:,:,3][~np.isin(labels,keep)]=0
Image.fromarray(a).save(DEST/'按钮.png')

for name in ['属性','功能','分叉']:
    a=np.array(Image.open(BACK/(name+'宝盒插画.png')).convert('RGB'));hsv=cv2.cvtColor(a,cv2.COLOR_RGB2HSV)
    light=(hsv[:,:,1]<75)&(hsv[:,:,2]>150)&(hsv[:,:,0]<40)
    _,labels=cv2.connectedComponents(np.uint8(light),8);edge=np.unique(np.r_[labels[0,:],labels[-1,:],labels[:,0],labels[:,-1]]);edge=edge[edge!=0]
    alpha=np.uint8(~np.isin(labels,edge))*255
    # 保留外轮廓，恢复宝盒内部的米色图案；过滤从纸纹误留下的碎点。
    contours,_=cv2.findContours(alpha,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE);clean=np.zeros(alpha.shape,np.uint8)
    cv2.drawContours(clean,[c for c in contours if cv2.contourArea(c)>=50],-1,255,cv2.FILLED);alpha=clean
    box=[(217,0),(280,19),(313,81),(279,155),(217,179),(153,157),(126,81),(156,19)]
    if name=='属性':box=[(216,5),(279,28),(305,96),(271,163),(215,186),(152,163),(117,98),(151,31)]
    if name=='分叉':box=[(218,6),(282,30),(314,99),(278,159),(216,184),(153,162),(122,101),(156,33)]
    cv2.fillPoly(alpha,[np.array(box,np.int32)],255)
    for cloud in [[(52,124),(76,119),(111,114),(122,121),(108,129),(98,131),(129,136),(139,144),(128,152),(100,151),(85,145),(62,146),(49,140)],[(306,120),(335,115),(358,113),(376,120),(388,132),(379,149),(343,154),(320,149),(320,140),(343,135),(327,128)]]:
        cv2.fillPoly(alpha,[np.array(cloud,np.int32)],255)
    Image.fromarray(np.dstack((a,alpha))).save(DEST/(name+'宝盒.png'))

(DEST/'处理说明.json').write_text(json.dumps({'source_ui':str(BACK),'source_title':str(src),'method':'现有原画人物GrabCut、插画连通外底去除、标题文字与按钮修补、原纸纹与角纹重组可伸缩框、原无字按钮纸纹清理；没有调用其它生图接口。'},ensure_ascii=False,indent=2),encoding='utf-8')
print('cleaned existing approved originals; no new API requests')
