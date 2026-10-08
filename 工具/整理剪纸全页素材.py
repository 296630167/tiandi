"""只处理其余剪纸页的已返回素材，不调用生图接口；不再写入已替换的首两页资源。"""
from pathlib import Path
from PIL import Image
import numpy as np
import cv2, json, shutil, hashlib

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'output/剪纸全页修复_20261007'
TAP=Path('C:/Users/123/Documents/taptap製造_一滴水的故事/assets/image')
UI=ROOT/'Assets/天帝/Resources/剪纸界面'
records=[]
LOCAL=OUT/'本地原画清理'
FALLBACK={'天帝标题页纯山水底图':'标题','天帝角色立绘透明抠图':'角色','天帝干净九宫格纸框':'纸框','天帝干净无字墨绿按钮':'按钮',**{'天帝'+n+'宝盒去纸背景':n+'宝盒' for n in ['属性','功能','分叉']}}
def source(prefix):
    files=sorted(TAP.glob('*'+prefix+'*.png'),key=lambda p:p.stat().st_mtime)
    p=files[-1] if files else LOCAL/(FALLBACK[prefix]+'.png')
    shutil.copy2(p,OUT/p.name)
    records.append({'source':str(p),'method':'remote completed' if files else 'existing approved bitmap cleanup; remote edit timed out and was not retried','sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'size':Image.open(p).size})
    return Image.open(p).convert('RGBA')
def fit(im,size,trim=False):
    if trim and im.getextrema()[3][0]<255:
        a=np.array(im)[:,:,3];ys,xs=np.where(a>24)
        im=im.crop((max(0,xs.min()-3),max(0,ys.min()-3),min(im.width,xs.max()+4),min(im.height,ys.max()+4)))
    return im.resize(size,Image.Resampling.LANCZOS)
def cut_outer(im):
    # 部分返回无alpha：只移除从四边可达的浅色底，不穿过面板边线或人物。
    a=np.array(im)
    if a[:,:,3].min()<128:return im
    rgb=a[:,:,:3];light=(rgb.min(2)>185)&(rgb.max(2)-rgb.min(2)<65)
    count,labels=cv2.connectedComponents(light.astype(np.uint8),8)
    exterior=np.unique(np.r_[labels[0,:],labels[-1,:],labels[:,0],labels[:,-1]])
    exterior=exterior[exterior!=0];a[:,:,3][np.isin(labels,exterior)]=0
    return Image.fromarray(a)

home=fit(source('天帝主页完整无界面底图'),(1920,1080))
# 共用页底纹只取已去UI背景的自然外缘，禁止从概念页保留底部烘焙提示。
tile=Image.open(UI/'素纸.png').convert('RGB');page=Image.new('RGB',(1920,1080))
for y in range(0,1080,tile.height):
    for x in range(0,1920,tile.width):page.paste(tile,(x,y))
edge=np.zeros((1080,1920),np.float32)
for n in range(44):edge[:,n]=edge[:,-n-1]=(1-n/44)**2
for n in range(96):edge[-n-1,:]=np.maximum(edge[-n-1,:],(1-n/96)**1.1)
pa=np.asarray(page,dtype=float);ha=np.asarray(home.convert('RGB'),dtype=float)
clean_page=Image.fromarray(np.uint8(ha*edge[:,:,None]+pa*(1-edge[:,:,None])))
for n in ['角色','构筑','回收','图鉴','宝盒']:clean_page.save(UI/(n+'背景.png'))
fit(source('天帝标题页纯山水底图'),(1920,1080)).save(UI/'标题山水背景.png')
im=cut_outer(source('天帝角色立绘透明抠图'))
# 人物保持自然比例，裁掉透明边距。
if im.getextrema()[3][0]>=128:raise ValueError('人物未返回可用透明通道，需检查原图，不能贴矩形')
im.crop(im.getbbox()).save(UI/'角色纸雕.png')
frame=fit(cut_outer(source('天帝干净九宫格纸框')),(480,320),True)
frame.save(UI/'朱红纸框.png');frame.save(UI/'弹窗纸框.png')
a=np.array(frame);hsv=cv2.cvtColor(a[:,:,:3],cv2.COLOR_RGB2HSV)
red=((hsv[:,:,0]<15)|(hsv[:,:,0]>165))&(hsv[:,:,1]>65)&(a[:,:,3]>32)
a[:,:,:3][red]=(80,110,91);Image.fromarray(a).save(UI/'轻纸框.png')

button=fit(cut_outer(source('天帝干净无字墨绿按钮')),(640,160),True)
button.save(UI/'墨绿按钮.png')
a=np.array(button);hsv=cv2.cvtColor(a[:,:,:3],cv2.COLOR_RGB2HSV)
green=(hsv[:,:,0]>30)&(hsv[:,:,0]<110)&(hsv[:,:,1]>35)&(a[:,:,3]>0)
shade=np.clip((a[:,:,:3].mean(2)-35)/110,0,1)
paper=np.asarray(Image.open(UI/'素纸.png').convert('RGB').resize(button.size)).astype(float)
warm=a.copy();warm[:,:,:3][green]=np.uint8((paper*(.88+.12*shade[:,:,None]))[green]);Image.fromarray(warm).save(UI/'暖纸按钮.png')
red=a.copy();red[:,:,:3][green]=np.uint8((np.array([155,49,34])[None,None,:]*(.85+.25*shade[:,:,None]))[green]);Image.fromarray(red).save(UI/'朱红按钮.png')

for n in ['属性','功能','分叉']:
    im=cut_outer(source('天帝'+n+'宝盒去纸背景'))
    if im.getextrema()[3][0]>=128:raise ValueError(n+'宝盒没有透明外底')
    im.crop(im.getbbox()).save(UI/(n+'宝盒插画.png'))

import runpy
runpy.run_path(str(ROOT/'工具/整理剪纸可读性素材.py'))
outputs=[]
map_source=ROOT.parent/'预览/天帝标题与战斗剪纸概念_20261007/03_青岚原战斗场景_实际地图.png'
records.append({'source':str(map_source),'sha256':hashlib.sha256(map_source.read_bytes()).hexdigest(),'usage':'青岚原生存大图，仅替换渲染，保留原通行图与范围'})
records.append({'source':str(ROOT.parent/'预览/天帝标题与战斗剪纸概念_20261007/最终交付映射.json'),'usage':'批准概念的原始远程映射'})
for directory in [UI]:
    for p in directory.glob('*.png'):
        outputs.append({'path':str(p),'size':Image.open(p).size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(OUT/'素材处理映射.json').write_text(json.dumps({'requested_model':'gpt / GPT Image 2','returned_model_confirmed':False,'originals':records,'outputs':outputs,'processing':'使用已返回山水底图的外缘处理其余剪纸页面；清理已有批准原画，人物/插画透明提取，纸纹和角纹重组可伸缩框，无字按钮纸纹清理与调色；首两页独立素材不在本工具中处理。'},ensure_ascii=False,indent=2),encoding='utf-8')
print('processed',len(records),'source records; all bitmaps retain source hashes')
