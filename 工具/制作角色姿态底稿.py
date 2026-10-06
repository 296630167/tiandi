"""用已生成的2D母版制作保持头脸尺度的姿态编辑底稿；不是最终游戏帧。

最终帧仍需经Tap逐帧绘制与检查，此底稿帮助生图模型锁定构图。
"""
from pathlib import Path
import json
import math
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
from Tap素材代理 import 制造项目

根=Path(__file__).resolve().parents[1]
制造=制造项目
素材=制造/'assets/image/天帝主角动画_20261005'
交付=根/'output/imagegen/主角十组动画'


def 变形(图,组):
    # 对2D精灵的规则采样格进行局部软变形，头部/中心躯干不参与缩放。
    步长=32
    细网=[]
    for y in range(0,1024,步长):
        for x in range(0,1024,步长):
            角点=[]
            for px,py in [(x,y),(x,y+步长),(x+步长,y+步长),(x+步长,y)]:
                dx=dy=0
                for (cx,cy),(mx,my),rx,ry in 组:
                    权=math.exp(-.5*((px-cx)/rx)**2-.5*((py-cy)/ry)**2)
                    dx+=mx*权;dy+=my*权
                # 头和大半躯干锁定，只作用于腕、腿和下摆附近。
                锁=max(0,min(1,(py-315)/80))
                角点.extend((px-dx*锁,py-dy*锁))
            细网.append(((x,y,x+步长,y+步长),tuple(角点)))
    return 图.transform((1024,1024),Image.Transform.MESH,细网,Image.Resampling.BICUBIC)


def main():
    清单=json.loads((交付/'生成清单.json').read_text(encoding='utf-8'))
    输出=素材/'姿态底稿';输出.mkdir(parents=True,exist_ok=True)
    for 项 in 清单['frames']:
        if 项['master']:continue
        图=Image.open(素材/'母版'/f'基准_{项["direction"]}.png').convert('RGBA')
        点=项['guide_points'];中性=next(x for x in 清单['frames'] if x['master'] and x['direction']==项['direction'])['guide_points']
        组=[]
        for 键 in ['左膝','右膝','左脚','右脚','左肘','右肘','左腕','右腕']:
            a=np.array(中性[键]);b=np.array(点[键]);差=b-a
            if '膝' in 键:rx,ry=40,85
            elif '脚' in 键:rx,ry=48,55
            elif '肘' in 键:rx,ry=43,60
            else:rx,ry=42,45
            组.append((a.tolist(),差.tolist(),rx,ry))
        完成=变形(图,组)
        底=Image.new('RGBA',(1024,1024),(255,0,255,255));底.alpha_composite(完成)
        位置=输出/f'{项["name"]}_底稿.png';底.save(位置)
        原=项['request']
        # 避免在一次请求中夹入前后帧动作或多个不同参考编号，引起错脚/复原站姿。
        from 准备主角十组动画 import 步态,射击,方向说明,方向
        序=项['frame']-1
        描述=步态[序] if 项['action']=='移动' else 射击[序] if 项['action']=='射击' else '平静正面待机，双脚原位，呼吸只使胸肩移动最多2px。'
        提示=f'''请对原图做连续角色动画的单帧局部姿态编辑，最终1024×1024。
原图是本帧姿态底稿，已经定好人物大小和画布位置。保留原图的头发、脸、眼镜、肩宽、胸腹宽、骨盆尺寸和衣服配色；头顶y128附近，不能变大、变小、变胖或变瘦，不能把人物撑满图片。固定正交俯视镜头，禁止重新取景。骨段长度不变。
本帧为{项['name']}，第{项['frame']:02d}/16帧。指定方向：{方向说明[方向.index(项['direction'])]}。
只实现这一个当前姿势：{描述}
唯一参考图为本帧关节姿态参考，不是人物母版：用同一个完整穿衣人物覆盖骨架，不能把红蓝线画出来。红线是人物左侧肢体，蓝线是人物右侧肢体。严格遵守本帧脚和手的位置，不能自行换脚。
脚部目标：左膝{点['左膝']}，左脚{点['左脚']}；右膝{点['右膝']}，右脚{点['右脚']}。手部目标：左肘{点['左肘']}、左腕{点['左腕']}；右肘{点['右肘']}、右腕{点['右腕']}。
地面世界根节点固定(512,928)，真实上下轻微起伏允许，但必须属于指定的关节动作。只修正手臂、裤腿关节与必要的衣褶；原图头脸发型眼镜的结构和位置保持不动。保持柔和淡彩手绘、轻轮廓、暖光冷影，光源不变。纯洋红#FF00FF背景，不要文字、网格、投影、光效、弹体或水印。
'''
        if 项['direction']=='南':
            提示+='正面图中，人物左腿在画面右侧、人物右腿在画面左侧。'
            if 项['action']=='移动':
                支撑='画面右侧的腿' if 序<8 else '画面左侧的腿'
                另一='画面左侧的腿' if 序<8 else '画面右侧的腿'
                提示+=f'本半周期承重支撑必须是{支撑}，{另一}是向前经过的另一条腿，不允许画成相反的支撑脚。'
                if 序 in [0,8]:提示+='这是脚跟接触地面的双脚接触帧，两只鞋都在地面上、一前一后；两脚鞋底只是透视位置不同，不能把后脚高抬成经过帧。'
        if 项['action']=='待机':
            提示+='双脚固定，禁止移动脚底、全身缩放或在画布中飘动；手指放松。'
            if 序==11:提示+='这帧眼睑向下约三分之一，开始眨眼。'
            elif 序==12:提示+='这帧自然闭眼一次，眉毛眼镜和脸型不变。'
            elif 序==13:提示+='这帧眼睑睁开约三分之二。'
            else:提示+='这帧保持双眼正常睁开。'
        请求={k:v for k,v in 原.items() if k not in ['reference_images','prompt']}
        请求.update({'target_dir':str(制造),'image':str(位置.relative_to(制造)).replace('\\','/'),
                     'name':'天帝动画_底稿锁定_'+项['name'],'prompt':提示,
                     'reference_images':[原['reference_images'][1]]})
        (交付/'请求'/f'底稿_{项["name"]}.json').write_text(json.dumps(请求,ensure_ascii=False,indent=2),encoding='utf-8')
        (交付/'逐帧提示词'/f'{项["name"]}.txt').write_text(提示,encoding='utf-8')
    print('已制作160张同尺度姿态编辑底稿与完整逐帧编辑请求，均待Tap绘制和验收。')


if __name__=='__main__':main()
