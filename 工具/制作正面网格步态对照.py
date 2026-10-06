"""同一幅Tap原画的连续二维网格关节试验，不冒充逐帧AI生成成果。"""
from pathlib import Path
import json
import math
import cv2
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from scipy.interpolate import RBFInterpolator
from Tap素材代理 import 制造项目

根=Path(__file__).resolve().parents[1]
输出=根/'output/imagegen/主角十组动画/正面网格步态对照_v5'
源=制造项目/'assets/image/天帝主角动画_20261005/母版/基准_南.png'


def main():
    输出.mkdir(parents=True,exist_ok=True)
    (输出/'处理后素材').mkdir(exist_ok=True)
    original=np.asarray(Image.open(源).convert('RGBA')).astype('float32')/255
    original[:,:,:3]*=original[:,:,3:]
    sample=np.mgrid[0:1024:8,0:1024:8].transpose(1,2,0)[:,:,::-1].astype(float)
    yy,xx=np.mgrid[:1024,:1024].astype('float32')
    frames=[];tracks=[];count=24
    for i in range(count):
        t=i/count*math.tau
        sway=math.sin(t)*1.2;bob=-math.cos(t*2)*1.6
        p=[];q=[]
        def anchor(x,y,dx,dy):p.append((x,y));q.append((x+dx,y+dy))
        # 头、肩、躯干使用同一固定原画，锁定形状及纹理，只随重心轻移。
        for y in [140,220,300,360,430,505,575]:
            for x in [455,512,568]:anchor(x,y,sway,bob)
        for x,y in [(0,0),(1023,0),(0,1023),(1023,1023),(512,1000)]:anchor(x,y,0,0)
        phase_data=[]
        for screen_side,phase,hip,knee,ankle,foot,shoulder,elbow,hand in [
            ('画面右/人物左',t,556,583,595,604,606,628,628),
            ('画面左/人物右',t+math.pi,456,432,425,419,414,395,392)]:
            depth=math.cos(phase);lift=max(0,-math.sin(phase))
            foot_y=depth*20-lift*17
            foot_x=sway*.5+(1 if screen_side.startswith('画面右') else -1)*depth*1.2
            knee_y=depth*7-lift*9
            # 原画是双脚外张的站姿；行走时落脚应靠近髋下，不能原地开合腿。
            inward=-1 if screen_side.startswith('画面右') else 1
            stance=inward*36
            knee_x=sway*.5+stance*.55
            foot_x+=stance
            for x in [hip-30,hip,hip+30]:anchor(x,632,sway,bob)
            for x in [knee-28,knee,knee+28]:anchor(x,751,knee_x,knee_y)
            for x in [ankle-30,ankle,ankle+30]:anchor(x,830,foot_x,foot_y*.88)
            # 每只鞋的多个采样点整体平移，保持鞋型而非每帧重绘/缩放。
            angle=math.radians(-13 if inward==1 else 13)
            c,s=math.cos(angle),math.sin(angle)
            for y in [858,889,920]:
                for x in [foot-28,foot,foot+28]:
                    dx=x-ankle;dy=y-855
                    anchor(x,y,(c*dx-s*dy)-dx+foot_x,(s*dx+c*dy)-dy+foot_y)
            arm=-depth
            hand_y=-max(0,arm)*22+max(0,-arm)*5+bob
            hand_x=sway+(1 if screen_side.startswith('画面左') else -1)*max(0,arm)*5
            for x in [shoulder-13,shoulder+13]:anchor(x,380,sway,bob)
            for x in [elbow-22,elbow,elbow+22]:anchor(x,490,hand_x*.4,hand_y*.4)
            for y in [558,585,610,620]:
                for x in [hand-14,hand+14]:anchor(x,y,hand_x,hand_y)
            phase_data.append({'side':screen_side,'phase':phase,'foot_offset':[foot_x,foot_y],
                               'knee_offset':[knee_x,knee_y],'hand_offset':[hand_x,hand_y]})
        source=np.asarray(p,dtype=float);target=np.asarray(q,dtype=float)
        inverse=RBFInterpolator(target,source-target,kernel='thin_plate_spline',smoothing=.02)
        delta=inverse(sample.reshape(-1,2)).reshape(128,128,2)
        delta=cv2.resize(delta.astype('float32'),(1024,1024),interpolation=cv2.INTER_CUBIC)
        warped=cv2.remap(original,xx+delta[:,:,0],yy+delta[:,:,1],cv2.INTER_CUBIC,borderMode=cv2.BORDER_CONSTANT)
        warped=np.clip(warped,0,1)
        warped[:,:,:3]/=np.maximum(warped[:,:,3:],1e-5)
        im=Image.fromarray(np.clip(warped*255,0,255).astype('uint8')).resize((512,512),Image.Resampling.LANCZOS)
        im.save(输出/'处理后素材'/f'主角_移动_南_{i+1:02}.png')
        bg=Image.new('RGB',(512,512),(235,234,223));bg.paste(im,(0,0),im);frames.append(bg)
        tracks.append({'frame':i+1,'body_offset':[sway,bob],'joints':phase_data,'bounds':im.getbbox()})
    for name,ms in [('正面走路_原速.gif',50),('正面走路_半速.gif',100)]:
        frames[0].save(输出/name,save_all=True,append_images=frames[1:],duration=ms,loop=0,disposal=2)
    page=Image.new('RGB',(1536,1120),(235,234,223));draw=ImageDraw.Draw(page)
    font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',16)
    for i,im in enumerate(frames):
        x=i%6*256;y=i//6*280
        page.paste(im.resize((256,256),Image.Resampling.LANCZOS),(x,y+24))
        draw.text((x+117,y+2),f'{i+1:02}',font=font,fill=(38,68,52))
    page.save(输出/'正面走路_逐帧对照.png')
    (输出/'处理说明.json').write_text(json.dumps({'source':str(源),'method':'One connected 2D image mesh warped by continuous joint trajectories. Not 24 separately generated images; no disjoint limb masks.',
        'frame_count':24,'frame_size':[512,512],'root':[256,464],'duration_seconds':1.2,'unity_imported':False,'visual_approved':False,'tracks':tracks},ensure_ascii=False,indent=2),encoding='utf-8')
    print('Prepared a connected-mesh comparison only; no Unity import.')


if __name__=='__main__':main()
