"""正面步态修正候选：使用Tap母版拆层、统一二维关节轨迹，再烘焙透明帧。

不会覆盖旧帧、不会自动接入Unity；生成的是逐帧重绘之外的二维拆层方案候选。
"""
from pathlib import Path
import math
import json
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.interpolate import RBFInterpolator
from Tap素材代理 import 制造项目

根 = Path(__file__).resolve().parents[1]
输出 = 根 / 'output/imagegen/主角十组动画/正面步态修正_v2'
源 = 制造项目 / 'assets/image/天帝主角动画_20261005/母版/基准_南.png'
帧数 = 24


def 分层(图, 多边形):
    遮 = Image.new('L', 图.size)
    ImageDraw.Draw(遮).polygon(多边形, fill=255)
    a = np.asarray(图).copy()
    a[:, :, 3] = (a[:, :, 3].astype(float) * np.asarray(遮) / 255).astype('uint8')
    return a


def 关节变形(图, 来源, 目标):
    # 固定采样点控制形变，采用逆向采样避免透明边缘空洞。
    边 = [[0, 0], [1023, 0], [0, 1023], [1023, 1023]]
    p = np.array([*来源, *边], dtype=float)
    q = np.array([*目标, *边], dtype=float)
    逆 = RBFInterpolator(q, p-q, kernel='thin_plate_spline', smoothing=0)
    格 = np.mgrid[0:1024:8, 0:1024:8].transpose(1, 2, 0)[:, :, ::-1].astype(float)
    差 = 逆(格.reshape(-1, 2)).reshape(128, 128, 2)
    差 = cv2.resize(差.astype('float32'), (1024, 1024), interpolation=cv2.INTER_CUBIC)
    y, x = np.mgrid[:1024, :1024].astype('float32')
    # 先预乘alpha，避免透明边缘插值产生黑边。
    a = 图.astype('float32') / 255
    a[:, :, :3] *= a[:, :, 3:]
    b = cv2.remap(a, x+差[:, :, 0], y+差[:, :, 1], cv2.INTER_CUBIC,
                  borderMode=cv2.BORDER_CONSTANT)
    b = np.clip(b, 0, 1)
    b[:, :, :3] /= np.maximum(b[:, :, 3:], 1e-5)
    return Image.fromarray(np.clip(b*255, 0, 255).astype('uint8'))


def 平移(图, x, y):
    out = Image.new('RGBA', (1024, 1024))
    out.alpha_composite(Image.fromarray(图), (round(x), round(y)))
    return out


def main():
    输出.mkdir(parents=True, exist_ok=True)
    原 = Image.open(源).convert('RGBA')
    # 左/右按画面坐标，保留原母版的不对称光照与服装细节，不镜像。
    左臂 = 分层(原, [(402,345),(448,378),(437,458),(423,493),(419,552),
                        (419,635),(353,635),(348,465),(380,387)])
    右臂 = 分层(原, [(576,344),(620,357),(650,401),(677,486),(669,550),
                        (659,567),(659,635),(605,635),(602,539),(590,484),(581,420)])
    左腿 = 分层(原, [(426,615),(514,615),(505,696),(476,792),(471,843),
                        (482,920),(465,953),(362,953),(368,864),(391,783),(410,690)])
    右腿 = 分层(原, [(515,615),(595,615),(613,692),(639,785),(645,843),
                        (663,953),(551,953),(539,884),(535,826),(525,781),(513,703)])
    # 正向选取躯干，不使用减法留下母版原手脚的半透明边缘。
    身体 = 分层(原, [(0,0),(1023,0),(1023,330),(610,330),(584,413),
                        (582,502),(599,566),(597,605),(414,605),(423,539),
                        (443,458),(440,385),(405,330),(0,330)])
    # 骨盆覆盖连接处，避免腿根在摆动过程中裂开。
    骨盆 = 分层(原, [(417,570),(595,570),(605,643),(527,662),(512,615),(495,662),(414,643)])
    for 名,图 in [('躯干',身体),('骨盆',骨盆),('画面左臂',左臂),('画面右臂',右臂),('画面左腿',左腿),('画面右腿',右腿)]:
        Image.fromarray(图).save(输出/f'拆层_{名}.png')
    臂点左 = [[413,385],[384,487],[426,487],[372,555],[415,555],[384,603],[410,603]]
    臂点右 = [[607,385],[596,487],[653,487],[609,555],[652,555],[613,603],[642,603]]
    腿点左 = [[456,632],[415,752],[469,752],[397,842],[460,842],[393,906],[455,906]]
    腿点右 = [[556,632],[552,752],[610,752],[560,842],[626,842],[565,906],[637,906]]
    图们=[];轨迹=[]
    for i in range(帧数):
        t=i/帧数*math.tau
        # 步行循环：触地→承重→经过→抬起→另一脚触地。
        重心x=math.sin(t)*2.5
        重心y=-math.cos(t*2)*2.2
        完成=Image.new('RGBA',(1024,1024))
        腿们=[];臂们=[]
        for side,相位,臂,腿,臂点,腿点 in [
            ('画面左',t,左臂,左腿,臂点左,腿点左),
            ('画面右',t+math.pi,右臂,右腿,臂点右,腿点右)]:
            前=math.cos(相位)
            抬=max(0,math.sin(相位))
            足y=前*31-抬*38
            膝y=前*13-抬*23
            足x=(-1 if side=='画面左' else 1)*前*4+重心x
            新腿=[]
            for j,(x,y) in enumerate(腿点):
                if j==0:dx,dy=重心x,重心y
                elif j<3:dx,dy=重心x*.5,膝y
                else:dx,dy=足x,足y
                新腿.append([x+dx,y+dy])
            腿图=关节变形(腿,腿点,新腿)
            腿们.append((前,腿图))
            # 手臂与同侧腿相反，前摆会自然屈肘，不伸直甩动。
            臂前=-前
            腕y=-max(0,臂前)*58+min(0,臂前)*-10
            腕x=(1 if side=='画面左' else -1)*max(0,臂前)*19+重心x
            新臂=[]
            for j,(x,y) in enumerate(臂点):
                if j==0:dx,dy=重心x,重心y
                elif j<3:dx,dy=腕x*.35,腕y*.45+重心y
                else:dx,dy=腕x,腕y+重心y
                新臂.append([x+dx,y+dy])
            臂图=关节变形(臂,臂点,新臂)
            臂们.append((臂前,臂图))
            轨迹.append({'frame':i+1,'side':side,'leg':新腿,'arm':新臂})
        # 前后遮挡在中心经过时切换，人物原本处于左右分离位置。
        for _,图 in sorted(腿们,key=lambda x:x[0]):完成.alpha_composite(图)
        for 前,图 in 臂们:
            if 前<0:完成.alpha_composite(图)
        完成.alpha_composite(平移(身体,重心x,重心y))
        完成.alpha_composite(平移(骨盆,重心x,重心y))
        for 前,图 in 臂们:
            if 前>=0:完成.alpha_composite(图)
        完成=完成.resize((512,512),Image.Resampling.LANCZOS)
        完成.save(输出/f'主角_移动_南_{i+1:02}.png')
        底=Image.new('RGB',(512,512),(235,234,223));底.paste(完成,(0,0),完成)
        图们.append(底)
    # 1.2秒完整左右步循环，24帧；慢速严格同序列只改时间。
    图们[0].save(输出/'正面走路_修正版.gif',save_all=True,append_images=图们[1:],duration=50,loop=0,disposal=2)
    图们[0].save(输出/'正面走路_慢速.gif',save_all=True,append_images=图们[1:],duration=100,loop=0,disposal=2)
    sheet=Image.new('RGB',(1536,1680),(235,234,223));draw=ImageDraw.Draw(sheet)
    font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
    for i,f in enumerate(图们):
        x=i%6*256;y=i//6*420
        sheet.paste(f.resize((256,256)),(x,y+24))
        draw.text((x+108,y+2),f'{i+1:02}',font=font,fill=(38,68,52))
    sheet.save(输出/'正面走路_24帧对照.png')
    (输出/'处理说明.json').write_text(json.dumps({'source':str(源),'method':'Tap原图二维拆层+同一关节轨迹烘焙；不是重新生成24张AI画','frame_size':[512,512],'root':[256,464],'frame_count':帧数,'duration_seconds':1.2,'unity_imported':False,'review':'候选，待视觉检查','tracks':轨迹},ensure_ascii=False,indent=2),encoding='utf-8')
    print(输出)


if __name__=='__main__':main()
