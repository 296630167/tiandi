"""把Image 2.5的两张6×4走路表整理为透明、统一脚底的八方向精灵帧。"""
from pathlib import Path
from collections import deque
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from PIL import ImageFilter

根 = Path(__file__).resolve().parents[1]
来源 = 根 / 'output/imagegen/主角八方向'
目标 = 根 / 'Assets/天帝/美术/角色/主角八方向'
方向 = ['南', '东南', '东', '东北', '北', '西北', '西', '西南']

def 取角色(图):
    像素 = np.asarray(图.convert('RGBA')).copy()
    颜色 = 像素[:, :, :3].astype(float)
    # Magenta only exists in the key background. Keep the white cuffs, shoes and eye highlights.
    色差 = np.minimum(颜色[:, :, 0], 颜色[:, :, 2]) - 颜色[:, :, 1]
    底色 = (色差 > 40) & (颜色[:, :, 0] > 100) & (颜色[:, :, 2] > 90)
    像素[:, :, 3][底色] = 0
    掩码 = 像素[:, :, 3] > 128
    已查 = np.zeros(掩码.shape, bool)
    组 = []
    for y, x in zip(*np.where(掩码)):
        if 已查[y, x]:
            continue
        队 = deque([(int(x), int(y))]); 已查[y, x] = True; 点 = []
        while 队:
            px, py = 队.popleft(); 点.append((px, py))
            for nx, ny in ((px-1,py),(px+1,py),(px,py-1),(px,py+1)):
                if 0 <= nx < 图.width and 0 <= ny < 图.height and 掩码[ny,nx] and not 已查[ny,nx]:
                    已查[ny,nx] = True; 队.append((nx,ny))
        组.append(点)
    if not 组:
        raise RuntimeError('帧内未找到角色')
    最大 = max(组, key=len)
    if len(最大) < 800:
        raise RuntimeError('帧主体过小或切分位置错误')
    # Remove keying speckles but retain limbs/hair with their own small islands.
    有效 = np.zeros(掩码.shape, bool)
    主x,主y=zip(*最大)
    主边=(min(主x),min(主y),max(主x),max(主y))
    for 点 in 组:
        if len(点) >= 12 and all(主边[0]-8<=x<=主边[2]+8 and 主边[1]-8<=y<=主边[3]+8 for x,y in 点):
            for x, y in 点: 有效[y,x] = True
    像素[:, :, 3][~有效] = 0
    y, x = np.where(有效)
    边界 = (int(x.min()), int(y.min()), int(x.max())+1, int(y.max())+1)
    if 边界[0] <= 1 or 边界[1] <= 1 or 边界[2] >= 图.width-1 or 边界[3] >= 图.height-1:
        raise RuntimeError('角色触及单元格边缘，可能有截断，需要重新检查素材')
    # De-spill only magenta contamination at transparent edges.
    外沿 = np.asarray(Image.fromarray((有效*255).astype('uint8')).filter(ImageFilter.MinFilter(3))) == 0
    污边 = 外沿 & 有效 & (色差 > 12)
    像素[:,:,0][污边] = np.minimum(像素[:,:,0][污边], 像素[:,:,1][污边]+15)
    像素[:,:,2][污边] = np.minimum(像素[:,:,2][污边], 像素[:,:,1][污边]+15)
    return Image.fromarray(像素), 边界

def main():
    目标.mkdir(parents=True, exist_ok=True)
    所有 = [[] for _ in 方向]; 测量 = []
    for 相位 in range(1,7):
        文件=f'相位{相位:02d}_八方向.png'
        if 相位==2 and (来源/'相位02_八方向_朝向修正.png').exists(): 文件='相位02_八方向_朝向修正.png'
        表 = Image.open(来源 / 文件)
        if 表.size != (1536,1024): raise RuntimeError('生成表尺寸不符合1536×1024')
        for 序 in range(8):
            列,行=序%4,序//4
            所有[序].append(取角色(表.crop((列*384,行*512,(列+1)*384,(行+1)*512))))
    待机=Image.open(来源/'待机_八方向.png')
    for 序 in range(8):
        列,行=序%4,序//4
        所有[序].append(取角色(待机.crop((列*384,行*512,(列+1)*384,(行+1)*512))))
    # ONE scale per direction, not per frame. This preserves each walk's real body compression.
    成品 = []; 汇总 = Image.new('RGBA',(1536,2048),(232,231,217,255))
    for 编号, 帧组 in enumerate(所有):
        参考高 = float(np.median([框[3]-框[1] for _,框 in 帧组[:6]])); 基础比例 = 210 / 参考高
        头宽=[]
        for 图,框 in 帧组:
            x0,y0,x1,y1=框; m=np.asarray(图)[:,:,3]>128
            行宽=[]
            for 行 in range(y0,y0+round((y1-y0)*.18)):
                xx=np.where(m[行])[0]
                if len(xx):行宽.append(xx.max()-xx.min()+1)
            头宽.append(float(np.percentile(行宽,90)))
        参考头宽=float(np.median(头宽[:6]))
        行成品 = []
        for 列,(图,框) in enumerate(帧组):
            比例=基础比例*参考头宽/头宽[列]
            x0,y0,x1,y1 = 框
            掩码 = np.asarray(图)[:,:,3] > 128
            顶 = max(y0+1, y0+round((y1-y0)*.22))
            yy,xx = np.where(掩码[y0:顶]); 头中心 = float(np.median(xx))
            图 = 图.resize((round(图.width*比例),round(图.height*比例)),Image.Resampling.LANCZOS)
            左 = round(128-头中心*比例); 上 = round(236-y1*比例)
            帧 = Image.new('RGBA',(256,256)); 帧.alpha_composite(图,(左,上))
            if 编号 in (0,4) and 3<=列<=5:
                # 正背视图衣裤对称，纠正模型漏画的另一半步；保留原头部和头发方向。
                对称=帧.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
                蒙版=Image.new('L',(256,256),0);md=ImageDraw.Draw(蒙版)
                for 行 in range(62,256):md.line((0,行,255,行),fill=round(min(1,(行-62)/10)*255))
                帧=Image.composite(对称,帧,蒙版)
            文件 = f'主角_{方向[编号]}_移动_{列+1:02d}.png' if 列<6 else f'主角_{方向[编号]}_待机.png'
            帧.save(目标/文件); 行成品.append(帧)
            if 列<6: 汇总.alpha_composite(帧,(列*256,编号*256))
            测量.append({'文件':文件,'来源边界':框,'统一比例':round(比例,4),'对齐后边界':帧.getbbox()})
        成品.append(行成品)
    汇总.save(来源/'八方向移动帧_对照.png')
    待机表=Image.new('RGBA',(1024,512),(232,231,217,255))
    for 序 in range(8):待机表.alpha_composite(成品[序][6],((序%4)*256,(序//4)*256))
    待机表.save(来源/'八方向站立帧_对照.png')
    动图 = []
    字体 = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',20)
    for 帧序 in range(6):
        页 = Image.new('RGB',(1024,568),(234,235,224)); 画 = ImageDraw.Draw(页)
        for 序 in range(8):
            x,y=(序%4)*256,(序//4)*284
            页.paste(成品[序][帧序],(x,y+20),成品[序][帧序])
            画.text((x+110,y+3),方向[序],font=字体,fill=(37,75,58))
        动图.append(页)
    动图[0].save(来源/'八方向走路_循环预览.gif',save_all=True,append_images=动图[1:],duration=100,loop=0,disposal=2)
    (来源/'切帧检查.json').write_text(json.dumps(测量,ensure_ascii=False,indent=2),encoding='utf-8')
    print('已整理48张透明移动帧与8张待机，生成对照表和循环预览。')

if __name__ == '__main__': main()
