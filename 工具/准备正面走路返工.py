"""针对正面走路的准确母版坐标准备24份独立Tap局部编辑提示词。"""
from pathlib import Path
import json
import math
import shutil
from PIL import Image, ImageDraw
from Tap素材代理 import 制造项目

根=Path(__file__).resolve().parents[1]
输出=根/'output/imagegen/主角十组动画/正面走路返工_v3'
素材=制造项目/'assets/image/天帝主角动画_20261005/正面走路返工_v3'
母版=制造项目/'assets/image/天帝主角动画_20261005/母版/基准_南.png'
阶段=[
 '人物左脚（画面右）前方脚跟刚触地，人物右脚（画面左）在后方以脚尖推蹬，两只鞋都触地。右臂轻微前摆、左臂轻微后摆。',
 '左脚由脚跟向全掌滚动，左膝开始轻屈；右脚跟刚抬离地面。双臂从接触姿态缓慢回收。',
 '左脚全掌落地开始承重，左膝轻微弯曲；右脚仍以脚尖支撑、即将离地。',
 '左脚稳定承重，髋部处低位；右脚尖刚离地，右膝开始向前收拢。',
 '左腿开始伸直，右腿往身体下方摆进，右膝稍屈，脚尖自然朝下。',
 '右膝即将经过左膝旁，右脚已经明显离地；左腿稳定承重。',
 '经过姿态：左脚在髋下稳定支撑，右膝向前微屈，右鞋距地面约25px；双手接近身体两侧。不能画双脚落地。',
 '右膝越过身体中心，右小腿开始往前打开；左脚仍稳在髋下。左臂开始前摆、右臂开始后摆。',
 '左脚跟开始抬起进行推蹬；右膝往前，右小腿继续展开、右脚降低。',
 '左脚只剩前掌支撑，右鞋跟继续前伸，髋部处轻微高位。',
 '右脚跟接近前方地面，右膝放松；左脚在后推蹬，双臂靠近下一次摆幅极点。',
 '右脚跟距离地面仅数px，准备接触；左脚在后方脚尖支撑。自然衔接13帧。',
 '人物右脚（画面左）前方脚跟刚触地，人物左脚（画面右）在后方以脚尖推蹬，两只鞋都触地。左臂轻微前摆、右臂轻微后摆。',
 '右脚由脚跟向全掌滚动，右膝开始轻屈；左脚跟刚抬离地面。双臂缓慢回收。',
 '右脚全掌落地开始承重，右膝轻微弯曲；左脚仍以脚尖支撑、即将离地。',
 '右脚稳定承重，髋部处低位；左脚尖刚离地，左膝开始向前收拢。',
 '右腿开始伸直，左腿往身体下方摆进，左膝稍屈，脚尖自然朝下。',
 '左膝即将经过右膝旁，左脚已经明显离地；右腿稳定承重。',
 '经过姿态：右脚在髋下稳定支撑，左膝向前微屈，左鞋距地面约25px；双手接近身体两侧。不能画双脚落地。',
 '左膝越过身体中心，左小腿开始往前打开；右脚仍稳在髋下。右臂开始前摆、左臂开始后摆。',
 '右脚跟开始抬起进行推蹬；左膝往前，左小腿继续展开、左脚降低。',
 '右脚只剩前掌支撑，左鞋跟继续前伸，髋部处轻微高位。',
 '左脚跟接近前方地面，左膝放松；右脚在后推蹬，双臂靠近下一次摆幅极点。',
 '左脚跟距离地面仅数px，准备接触；右脚在后方脚尖支撑。与01帧之间只差很小动作，不回到站姿。',
]


def main():
    输出.mkdir(parents=True,exist_ok=True)
    for d in ['参考','提示词','请求','原始返回']: (输出/d).mkdir(exist_ok=True)
    素材.mkdir(parents=True,exist_ok=True)
    站立=Image.open(母版).convert('RGBA')
    底=Image.new('RGBA',(1024,1024),(255,0,255,255));底.alpha_composite(站立)
    底.save(素材/'锁定母版.png')
    rows=[]
    for i in range(24):
        t=i/24*math.tau
        shift=math.sin(t)*3
        bob=-math.sin(t*2)*3
        pts={'头':[511,230+bob],'左肩':[605+shift,378+bob],'右肩':[414+shift,378+bob],
             '左髋':[557+shift,631+bob],'右髋':[455+shift,631+bob]}
        for side,s in [('左',t),('右',t+math.pi)]:
            screen=1 if side=='左' else -1
            # 前半步为承重脚从前方移动到后方，抬脚发生于后半步。
            # 抬脚相位不能与本帧“承重/经过”文本相反。
            f=math.cos(s);lift=max(0,-math.sin(s))
            hx=506+screen*51+shift
            pts[side+'膝']=[hx+screen*13,751+f*11-lift*15]
            pts[side+'脚踝']=[hx+screen*31,864+f*23-lift*28]
            pts[side+'脚底']=[hx+screen*48,926+f*23-lift*28]
            af=-f
            pts[side+'肘']=[506+screen*119+shift-screen*max(0,af)*4,491-max(0,af)*8]
            pts[side+'腕']=[506+screen*122+shift-screen*max(0,af)*10,607-max(0,af)*26+min(0,af)*-6]
        guide=Image.new('RGB',(1024,1024),(243,241,231));draw=ImageDraw.Draw(guide)
        draw.line((160,926,865,926),fill=(170,170,170),width=2)
        draw.ellipse((435,128+bob,587,329+bob),outline=(65,65,65),width=4)
        for side,c in [('左',(190,74,66)),('右',(57,108,176))]:
            for chain in [[side+'肩',side+'肘',side+'腕'],[side+'髋',side+'膝',side+'脚踝',side+'脚底']]:
                draw.line([tuple(pts[k]) for k in chain],fill=c,width=8)
                for k in chain:
                    x,y=pts[k];draw.ellipse((x-6,y-6,x+6,y+6),fill=c)
        name=f'移动_南_{i+1:02}'
        guide.save(素材/f'{name}_姿态.png')
        guide.save(输出/'参考'/f'{name}_姿态.png')
        prompt=f'''用途：纯2D游戏青年主角的正面慢走循环，局部编辑一张1024×1024动画帧。当前第{i+1:02}/24帧。
图1是精确锁定的母版，图2是本帧的肢体姿态。只改变两臂与两腿的姿态和必要关节褶皱，不重新设计整个人。头发、眼镜、五官、帽子、胸腹袋口、抽绳、卫衣主体宽度和色块全部沿用图1，保持位置，不重新渲染衣服纹理。全程严格正面，淡彩青绿手绘与原图一致，不是侧面也不是跑步。
当前动作：{阶段[i]}
普通青年以1.2秒完成左右两个正常步子，步幅克制、手臂放松，绝不是跑步、原地高抬腿、跳跃、踏步操或军步。手腕摆幅25px内，鞋的前后投影变化约46px，离地高度约28px。双膝自然放松而不是大幅抬到腰部。
本帧坐标（与原画重新测量对齐）：{json.dumps({k:[round(v[0],1),round(v[1],1)] for k,v in pts.items()},ensure_ascii=False)}。坐标是衣服内的关节，鞋底坐标不是脚踝；衣服应包住骨架，禁止把线条画进成品。人物左侧位于画面右、蓝线右侧位于画面左，不能互换承重脚。
固定母版头部尺寸：头顶128附近、头宽155px、下巴330附近；肩部378、髋部631、自然站立地面926。人物绝不能变高、矮、胖、瘦，镜头不推拉，四周留白不变。允许整体自然起伏约3px，不许另一次取景。腿部长度、手脚尺寸都不变；不能把一帧画成瘦腿下一帧画成宽裤。
严格只画当前帧，纯洋红#FF00FF背景；不要动作残影、多手多腿、粒子、弹体、投影、线条、文字、边框或水印。原图中旧手旧脚必须完全移除，不能保留在新姿态旁边。保持本角色平静表情。
'''
        (输出/'提示词'/f'{name}.txt').write_text(prompt,encoding='utf-8')
        req={'image':str((素材/'锁定母版.png').relative_to(制造项目)).replace('\\','/'),'reference_images':[str((素材/f'{name}_姿态.png').relative_to(制造项目)).replace('\\','/')],
             'prompt':prompt,'name':'天帝正面步态修正_v3_'+name,'target_size':'1024x1024','aspect_ratio':'1:1','transparent':False,'model':'gpt','quality':'high','resolution':'2K','thinking_level':'high','target_dir':str(制造项目)}
        (输出/'请求'/f'{name}.json').write_text(json.dumps(req,ensure_ascii=False,indent=2),encoding='utf-8')
        shutil.copy2(输出/'提示词'/f'{name}.txt',素材/f'{name}_提示词.txt')
        rows.append({'name':name,'phase':i/24,'guide_points':pts,'action':阶段[i],'request':req})
    (输出/'生成清单.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    print('prepared 24 frame prompts; no generation yet')


if __name__=='__main__':main()
