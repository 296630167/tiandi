"""真实2D骨架、绑定矩阵及线性蒙皮。只输出正面步态审查样例，不改游戏绑定。"""
from pathlib import Path
import json
import math
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from Tap素材代理 import 制造项目

根=Path(__file__).resolve().parents[1]
输出=根/'output/imagegen/主角十组动画/正面骨骼样例_v6'
源=制造项目/'assets/image/天帝主角动画_20261005/母版/基准_南.png'
帧数=30
周期=1.2

# 原画坐标为像素，y向下；左/右始终以人物解剖方向命名。
定义=[
 ('B_Root',-1,(512,928),None),
 ('B_Pelvis',0,(512,631),None),
 ('B_Torso',1,(512,578),None),
 ('B_Head',2,(512,330),None),
 ('B_UpperArm_L',2,(605,378),(625,491)),
 ('B_Forearm_L',4,(625,491),(628,607)),
 ('B_Hand_L',5,(628,607),None),
 ('B_UpperArm_R',2,(414,378),(396,491)),
 ('B_Forearm_R',7,(396,491),(391,607)),
 ('B_Hand_R',8,(391,607),None),
 ('B_Thigh_L',1,(557,631),(583,751)),
 ('B_Shin_L',10,(583,751),(595,856)),
 ('B_Foot_L',11,(595,856),None),
 ('B_Thigh_R',1,(455,631),(432,751)),
 ('B_Shin_R',13,(432,751),(424,856)),
 ('B_Foot_R',14,(424,856),None),
]


def matrix(origin,angle=0,length_scale=1):
    c,s=math.cos(angle),math.sin(angle)
    return np.array([[c*length_scale,-s,origin[0]],[s*length_scale,c,origin[1]],[0,0,1]],float)


def bone_matrix(start,end,definition):
    a=np.asarray(start,float);b=np.asarray(end,float)
    rest=np.linalg.norm(np.asarray(definition[3])-np.asarray(definition[2]))
    v=b-a
    return matrix(a,math.atan2(v[1],v[0]),np.linalg.norm(v)/rest)


def smoothstep(a,b,x):
    t=np.clip((x-a)/(b-a),0,1)
    return t*t*(3-2*t)


def weights(vertices):
    out=np.zeros((len(vertices),len(定义)),float)
    for j,(x,y) in enumerate(vertices):
        if y<352:
            head=1-smoothstep(322,352,y)
            out[j,3]=head;out[j,2]=1-head
        elif y<605:
            # 同一连通网格：肩部混合衣身与上臂，不切开腋下/肩部贴图。
            right=x>512
            upper,lower,hand=(4,5,6) if right else (7,8,9)
            arm_fraction=(1-smoothstep(428,450,x)) if not right else smoothstep(575,597,x)
            if y<390:arm_fraction*=smoothstep(346,390,y)
            lower_mix=smoothstep(463,516,y)
            hand_mix=smoothstep(556,592,y)
            out[j,2]=1-arm_fraction
            out[j,upper]=arm_fraction*(1-lower_mix)
            out[j,lower]=arm_fraction*lower_mix*(1-hand_mix)
            out[j,hand]=arm_fraction*lower_mix*hand_mix
        else:
            # 指尖在裤腰高度仍归属手部；腿根平滑过渡，左右脚不互相借权重。
            if y<633 and (x<417 or x>603):
                out[j,6 if x>512 else 9]=1
                continue
            leg=smoothstep(609,647,y)
            shin_mix=smoothstep(718,781,y)
            foot_mix=smoothstep(830,870,y)
            out[j,1]=1-leg
            # 中央透明裤缝也需要连续权重，不能以x=512硬切导致三角形翻转、锯齿裂口。
            left_mix=smoothstep(486,538,x)
            for fraction,thigh,shin,foot in [(left_mix,10,11,12),(1-left_mix,13,14,15)]:
                out[j,thigh]=fraction*leg*(1-shin_mix)
                out[j,shin]=fraction*leg*shin_mix*(1-foot_mix)
                out[j,foot]=fraction*leg*shin_mix*foot_mix
            # Unity标准蒙皮至多四影响，极小的第三段权重归并到保留的影响。
            keep=np.argsort(out[j])[-4:]
            out[j,np.setdiff1d(np.arange(len(定义)),keep)]=0
            out[j]/=out[j].sum()
    if not np.allclose(out.sum(axis=1),1):raise ValueError('蒙皮权重未归一化。')
    if np.max((out>1e-8).sum(axis=1))>4:raise ValueError('单顶点影响超过4根骨骼。')
    return out


def rest_matrices():
    result=[]
    for bone in 定义:
        if bone[3] is None:result.append(matrix(bone[2]))
        else:
            v=np.asarray(bone[3])-np.asarray(bone[2])
            result.append(matrix(bone[2],math.atan2(v[1],v[0])))
    return np.asarray(result)


def pose(t):
    # 两步同一连续曲线：支撑脚在前半周期从前移后，摆动脚在后半周期抬起。
    lateral=math.sin(t)*2.0
    bob=math.sin(t*2)*2.5
    lean=math.sin(t)*math.radians(.45)
    trunk=matrix((512+lateral,631+bob),lean)@matrix((-512,-631))
    def body(p):return (trunk@np.array([p[0],p[1],1]))[:2]
    result=[matrix((512,928)),matrix(body((512,631))),matrix(body((512,578)),lean),matrix(body((512,330)))]
    joints=[]
    # 手臂与同侧腿反相，前摆时自然屈肘；固定手与袖口体积。
    for phase,shoulder,elbow,hand,upper_i,lower_i,inward in [
        (t,(605,378),(625,491),(628,607),4,5,-1),
        (t+math.pi,(414,378),(396,491),(391,607),7,8,1)]:
        arm=-math.cos(phase)
        s=body(shoulder)
        e=body(elbow)+np.array([inward*max(0,arm)*2,-max(0,arm)*7])
        h=body(hand)+np.array([inward*max(0,arm)*6,-max(0,arm)*25+max(0,-arm)*4])
        result.extend([bone_matrix(s,e,定义[upper_i]),bone_matrix(e,h,定义[lower_i]),matrix(h,lean+arm*math.radians(1.2))])
        joints.append({'side':'L' if inward==-1 else 'R','shoulder':s.tolist(),'elbow':e.tolist(),'hand':h.tolist()})
    # 矢状面双段腿IK，保持股骨/胫骨的物理长度；再投影到正面二维骨架。
    # 这里没有角色3D模型、3D场景或骨骼资源，投影只是落脚轨迹的数学约束。
    tilt=math.radians(18)
    physical_height=218
    projection_scale=225/(physical_height*math.cos(tilt))
    femur,tibia=126.0,108.0
    for side,phase,hip,thigh_i,shin_i,inward in [
        ('L',t,(557,631),10,11,-1),
        ('R',t+math.pi,(455,631),13,14,1)]:
        depth=math.cos(phase)
        lift=max(0,-math.sin(phase))**1.25*21
        depth_z=depth*55
        height=physical_height-bob/(projection_scale*math.cos(tilt))
        endpoint=np.array([depth_z,-height+lift])
        distance=np.linalg.norm(endpoint)
        if not abs(femur-tibia)<distance<femur+tibia:raise ValueError('膝部IK目标越界。')
        u=endpoint/distance
        along=(femur*femur-tibia*tibia+distance*distance)/(2*distance)
        forward=math.sqrt(max(0,femur*femur-along*along))
        knee=along*u+forward*np.array([-u[1],u[0]])
        h=body(hip)
        k=np.array([hip[0]-inward*17+lateral*.65,
                    h[1]+projection_scale*(-knee[1]*math.cos(tilt)+knee[0]*math.sin(tilt))])
        a=np.array([hip[0]-inward*18+lateral*.2,
                    h[1]+projection_scale*(-endpoint[1]*math.cos(tilt)+endpoint[0]*math.sin(tilt))])
        # 原画鞋头略朝外，行走时向前收拢；推进阶段略提踵，过脚时自然松踝。
        shoe_angle=math.radians(12 if side=='L' else -12)+math.sin(phase)*math.radians(2.8)
        result.extend([bone_matrix(h,k,定义[thigh_i]),bone_matrix(k,a,定义[shin_i]),matrix(a,shoe_angle)])
        joints.append({'side':side,'hip':h.tolist(),'knee':k.tolist(),'ankle':a.tolist(),
                       'forward_depth':depth_z,'lift_height':lift,'support':lift<.01})
    return np.asarray(result),joints


def mesh(alpha):
    coverage=cv2.dilate((alpha>0).astype('uint8'),np.ones((17,17),np.uint8))
    xs=np.arange(320,706,16);ys=np.arange(112,962,16)
    vertices=np.array([(x,y) for y in ys for x in xs],float)
    triangles=[]
    nx=len(xs)
    for y in range(len(ys)-1):
        for x in range(nx-1):
            # 轮廓留8px透明余量，舍弃完全不覆盖贴图的区域，不连接两只鞋之间的空白网格。
            left,top=int(xs[x]),int(ys[y])
            if not coverage[top:top+17,left:left+17].max():continue
            a=y*nx+x;b=a+1;c=a+nx;d=c+1
            triangles.extend([(a,b,c),(b,d,c)])
    used=sorted(set(v for t in triangles for v in t));index={v:i for i,v in enumerate(used)}
    return vertices[used],np.array([[index[v] for v in t] for t in triangles],int)


def render(original,vertices,posed,triangles):
    map_x=np.full((1024,1024),-1,np.float32);map_y=map_x.copy()
    for triangle in triangles:
        p=posed[triangle];s=vertices[triangle]
        xmin=max(0,int(np.floor(p[:,0].min())));xmax=min(1023,int(np.ceil(p[:,0].max())))
        ymin=max(0,int(np.floor(p[:,1].min())));ymax=min(1023,int(np.ceil(p[:,1].max())))
        if xmin>xmax or ymin>ymax:continue
        yy,xx=np.mgrid[ymin:ymax+1,xmin:xmax+1]
        denom=(p[1,1]-p[2,1])*(p[0,0]-p[2,0])+(p[2,0]-p[1,0])*(p[0,1]-p[2,1])
        if abs(denom)<1e-6:continue
        a=((p[1,1]-p[2,1])*(xx-p[2,0])+(p[2,0]-p[1,0])*(yy-p[2,1]))/denom
        b=((p[2,1]-p[0,1])*(xx-p[2,0])+(p[0,0]-p[2,0])*(yy-p[2,1]))/denom
        c=1-a-b
        valid=(a>=-1e-5)&(b>=-1e-5)&(c>=-1e-5)
        mx=a*s[0,0]+b*s[1,0]+c*s[2,0];my=a*s[0,1]+b*s[1,1]+c*s[2,1]
        map_x[ymin:ymax+1,xmin:xmax+1][valid]=mx[valid]
        map_y[ymin:ymax+1,xmin:xmax+1][valid]=my[valid]
    sampled=cv2.remap(original,map_x,map_y,cv2.INTER_CUBIC,borderMode=cv2.BORDER_CONSTANT)
    sampled=np.clip(sampled,0,1);sampled[:,:,:3]/=np.maximum(sampled[:,:,3:],1e-5)
    return Image.fromarray(np.clip(sampled*255,0,255).astype('uint8'))


def main():
    输出.mkdir(parents=True,exist_ok=True)
    for folder in ['处理后素材','骨架叠加帧']:(输出/folder).mkdir(exist_ok=True)
    reference=Image.open(源).convert('RGBA');reference.save(输出/'主角_正面绑定原画.png')
    original=np.asarray(reference).astype('float32')/255
    vertices,triangles=mesh(original[:,:,3]);influences=weights(vertices)
    bind=rest_matrices();inverse=np.linalg.inv(bind)
    homogeneous=np.column_stack([vertices,np.ones(len(vertices))])
    original[:,:,:3]*=original[:,:,3:]
    frames=[];overlays=[];tracks=[];metrics=[];last_vertices=None;max_frame_delta=0
    font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
    for i in range(帧数):
        phase=i/帧数*math.tau
        bones,joints=pose(phase)
        skin=bones@inverse
        transformed=np.einsum('bij,vj->bvi',skin,homogeneous)[:,:,:2]
        positioned=np.einsum('vb,bvi->vi',influences,transformed)
        tri=positioned[triangles];ab=tri[:,1]-tri[:,0];ac=tri[:,2]-tri[:,0]
        area=ab[:,0]*ac[:,1]-ab[:,1]*ac[:,0]
        inverted=int((area<=0).sum())
        if inverted:raise ValueError(f'第{i+1}帧存在{inverted}个翻折三角形，需修正权重/骨骼。')
        if last_vertices is not None:max_frame_delta=max(max_frame_delta,float(np.linalg.norm(positioned-last_vertices,axis=1).max()))
        last_vertices=positioned
        im=render(original,vertices,positioned,triangles).resize((512,512),Image.Resampling.LANCZOS)
        im.save(输出/'处理后素材'/f'主角_移动_南_{i+1:02}.png')
        frame=Image.new('RGB',(512,512),(235,234,223));frame.paste(im,(0,0),im);frames.append(frame)
        overlay=frame.copy();draw=ImageDraw.Draw(overlay)
        for j in joints:
            points=[j[k] for k in ('shoulder','elbow','hand')] if 'shoulder'in j else [j[k] for k in ('hip','knee','ankle')]
            color=(214,104,71) if j['side']=='L' else (66,142,188)
            points=[(p[0]*.5,p[1]*.5) for p in points]
            draw.line(points,fill=color,width=2)
            for x,y in points:draw.ellipse((x-3,y-3,x+3,y+3),fill=color)
        draw.text((16,16),f'{i+1:02}/{帧数}  2D骨架 / 蒙皮',font=font,fill=(38,68,52))
        overlay.save(输出/'骨架叠加帧'/f'主角_移动_南_{i+1:02}.png');overlays.append(overlay)
        local=[bones[0]]+[np.linalg.inv(bones[b[1]])@bones[k] for k,b in enumerate(定义) if k]
        tracks.append({'frame':i+1,'time_seconds':round(i/帧数*周期,6),'world_matrices':bones.tolist(),
                       'local_matrices':np.asarray(local).tolist(),'joints':joints})
        metrics.append({'frame':i+1,'bounds':im.getbbox(),'root':[256,464],
                        'inverted_triangles':inverted,
                        'left_lift':joints[2]['lift_height'],'right_lift':joints[3]['lift_height']})
    for name,images,ms in [('正面走路_原速.gif',frames,40),('正面走路_半速.gif',frames,80),('正面走路_骨架.gif',overlays,80)]:
        images[0].save(输出/name,save_all=True,append_images=images[1:],duration=ms,loop=0,disposal=2)
    contact=Image.new('RGB',(1536,1390),(235,234,223));draw=ImageDraw.Draw(contact)
    for i,frame in enumerate(frames):
        x=i%6*256;y=i//6*278;contact.paste(frame.resize((256,256),Image.Resampling.LANCZOS),(x,y+22))
        draw.text((x+113,y+1),f'{i+1:02}',font=font,fill=(38,68,52))
    contact.save(输出/'正面走路_逐帧对照.png')
    # 静态检查展示权重分区和真正的骨骼层级；动画预览由同一组蒙皮数据采样。
    bones_export=[]
    for i,b in enumerate(定义):
        local=bind[i] if b[1]<0 else np.linalg.inv(bind[b[1]])@bind[i]
        bones_export.append({'name':b[0],'parent':b[1],'origin_pixels':list(b[2]),'endpoint_pixels':list(b[3]) if b[3]else None,
                             'bind_world_matrix':bind[i].tolist(),'bind_local_matrix':local.tolist(),'inverse_bind_matrix':inverse[i].tolist()})
    (输出/'主角_正面二维骨架.json').write_text(json.dumps({'format':'tiandi-2d-skinning-v1','source':'主角_正面绑定原画.png','coordinate_system':'1024x1024 pixels, x right, y down; root (512,928)',
        'bones':bones_export,'vertices':vertices.tolist(),'triangles':triangles.tolist(),'weights':influences.tolist(),
        'max_influences':4,'method':'linear blend skinning: posed_vertex = sum(weight * world_pose * inverse_bind * bind_vertex)'},ensure_ascii=False),encoding='utf-8')
    (输出/'主角_正面步态动画.json').write_text(json.dumps({'duration_seconds':周期,'loop':True,'sample_count':帧数,'samples':tracks,
        'continuous_curve':'left phase=t, right=t+pi; two-segment leg IK with fixed physical femur/tibia, counter-swing arms; root fixed'},ensure_ascii=False),encoding='utf-8')
    closure=float(np.max(np.abs(pose(0)[0]-pose(math.tau)[0])))
    if closure>1e-8:raise ValueError('动画循环首尾骨骼姿态不相同。')
    (输出/'样例检查记录.json').write_text(json.dumps({'bone_count':len(定义),'vertex_count':len(vertices),'triangle_count':len(triangles),
        'weight_sum_error':float(np.max(np.abs(influences.sum(axis=1)-1))),
        'loop_closure_matrix_error':closure,
        'max_adjacent_vertex_displacement_pixels':max_frame_delta,
        'source':str(源),'frame_count':帧数,'frame_size':[512,512],'duration_seconds':周期,
        'texture_repainted':False,'head_scale_animated':False,'root_animated':False,'unity_imported':False,'visual_approved':False,'frames':metrics},ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'bones':len(定义),'vertices':len(vertices),'triangles':len(triangles),'frames':帧数,'unity_imported':False},ensure_ascii=False))


if __name__=='__main__':main()
