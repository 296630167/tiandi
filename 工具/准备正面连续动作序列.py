"""在一张画布内约束连续姿态；只准备请求，不调用远程生成。"""
from pathlib import Path
import json
import math
from PIL import Image, ImageDraw
from Tap素材代理 import 制造项目

根 = Path(__file__).resolve().parents[1]
输出 = 根 / 'output/imagegen/主角十组动画/正面连续序列_v4'
素材 = 制造项目 / 'assets/image/天帝主角动画_20261005/正面连续序列_v4'
母版 = 制造项目 / 'assets/image/天帝主角动画_20261005/母版/基准_南.png'
动作 = [
    '左脚前伸以脚跟接触，右脚在后以脚尖推蹬；右手在前、左手在后，摆幅克制。',
    '左脚由脚跟滚向全掌承重，左膝轻屈；右脚跟刚抬起，右手开始回收。',
    '左脚全掌承重，右脚尖刚离地，右膝开始收拢；躯干略低，不能换支撑脚。',
    '左腿承重开始伸直，右膝从后往前收至身体下方，右鞋离地；双臂靠近中位。',
    '经过姿态：左脚在髋下支撑，右脚在身体下方离地约20像素，双手接近躯干两侧。',
    '右膝越过身体中位，右小腿开始展开，右鞋仍离地；左手开始前摆、右手后摆。',
    '左脚跟抬起进行推蹬，右小腿前伸且鞋跟接近地面；左手继续前摆。',
    '左脚在后以脚尖推蹬，右脚跟在前接近地面，距接触只差很小的动作。',
    '右脚前伸以脚跟接触，左脚在后以脚尖推蹬；左手在前、右手在后。这是01的另一半步。',
    '右脚由脚跟滚向全掌承重，右膝轻屈；左脚跟刚抬起，左手开始回收。',
    '右脚全掌承重，左脚尖刚离地，左膝开始收拢；躯干略低，不能换支撑脚。',
    '右腿承重开始伸直，左膝从后往前收至身体下方，左鞋离地；双臂靠近中位。',
    '经过姿态：右脚在髋下支撑，左脚在身体下方离地约20像素，双手接近躯干两侧。',
    '左膝越过身体中位，左小腿开始展开，左鞋仍离地；右手开始前摆、左手后摆。',
    '右脚跟抬起进行推蹬，左小腿前伸且鞋跟接近地面；右手继续前摆。',
    '右脚在后以脚尖推蹬，左脚跟在前接近地面，距01接触只差很小的动作；不要复制01或回站姿。',
]


def main():
    输出.mkdir(parents=True, exist_ok=True)
    素材.mkdir(parents=True, exist_ok=True)
    (输出 / '逐帧提示词').mkdir(exist_ok=True)
    size = 768
    master = Image.open(母版).convert('RGBA').resize((size,size),Image.Resampling.LANCZOS)
    layout = Image.new('RGB',(3072,3072),(255,0,255))
    skeleton = Image.new('RGB',(3072,3072),(255,0,255))
    draw = ImageDraw.Draw(skeleton)
    rows = []
    for i, action in enumerate(动作):
        ox,oy = i%4*size,i//4*size
        layout.paste(master,(ox,oy),master)
        t=i/16*math.tau
        points={'左肩':(454,284),'右肩':(311,284),'左髋':(418,473),'右髋':(341,473)}
        draw.ellipse((ox+326,oy+96,ox+440,oy+247),outline=(244,230,214),width=3)
        for side,s,color in [('左',t,(229,105,54)),('右',t+math.pi,(68,175,232))]:
            sign=1 if side=='左' else -1
            f=math.cos(s);lift=max(0,-math.sin(s))
            hx=380+sign*38
            points[side+'膝']=(hx+sign*9,563+f*8-lift*11)
            points[side+'踝']=(hx+sign*23,648+f*17-lift*21)
            points[side+'脚底']=(hx+sign*36,696+f*17-lift*21)
            af=-f
            points[side+'肘']=(380+sign*89,368-max(0,af)*6)
            points[side+'腕']=(380+sign*91-sign*max(0,af)*7,455-max(0,af)*19)
            for chain in [[side+'肩',side+'肘',side+'腕'],[side+'髋',side+'膝',side+'踝',side+'脚底']]:
                draw.line([(ox+points[k][0],oy+points[k][1]) for k in chain],fill=color,width=5)
        text=f'第{i+1:02}帧，格子内局部坐标，解剖左=画面右，解剖右=画面左。动作：{action}\n保持同一头脸、躯干尺寸、衣服宽度、肢体长度与鞋的真实尺寸。头顶y=96附近，脚底参考y=696，头宽约116px，人物高度约600px；不因前后摆腿改变鞋的大小。两臂与同侧腿反向；全程严格正面、平静表情、原地慢走，禁止抬膝到腰部。当前关节导引：{json.dumps(points,ensure_ascii=False)}。只画本帧自然姿态，不描绘关节线。'
        (输出/'逐帧提示词'/f'移动_南_{i+1:02}.txt').write_text(text,encoding='utf-8')
        rows.append({'frame':i+1,'action':action,'prompt':text,'local_points':points,'cell':[ox,oy,size,size]})
    layout.save(素材/'固定人物布局.png')
    skeleton.save(素材/'连续姿态导引.png')
    prompt='''Use case: identity-preserve. Asset type: production 2D sprite walk cycle, sixteen distinct sequential poses on one sheet.
将图1编辑成同一个青年角色的16帧正面慢走连续动画。原有4×4格布局必须保持，每格768×768，输出3072×3072。读图顺序为从左到右再从上到下。所有16个人物是同一个人同一时间序列，绝不是16种设计。图1只规定原画身份、淡彩手绘风格、颜色和统一尺寸，站姿必须改为下列各帧走路动作。图2规定每格姿态，橙色是人物左侧（画面右），蓝色是人物右侧（画面左），线条不能出现在结果里。
近黑碎发、黑色矩形眼镜、森林绿宽松卫衣、深灰长裤、米白运动鞋，平静的成年青年。保持原脸、发型、眼镜、领口、帽子、胸腹、抽绳、袋口和相同衣褶，不生成另一种画风或脸。全程面向镜头，不转头，不出现侧面。
1.2秒完成左右两个自然小步。与站姿相比只改变步态需要的手脚、膝肘和轻微肩髋动作。身体根节点不平移，人物大小不变；正常的最大起伏不超过3像素。脚步是前后摆动而非左右开合，支撑脚与抬脚明确交替；手臂与同侧腿反向，手腕最大摆幅20像素。不能变跑步、高抬腿、机械操或夸张摇摆。
每格人物头顶96附近，头宽116附近、自然站立鞋底696附近、中心x384，身高600左右。各帧衣服、裤子和鞋的体积保持一致，避免帧间裤子突然变宽、袖子忽长忽短、鞋被任意缩放。鞋的前后位置可以变化，鞋本身不能放大。
逐帧动作要求：
'''+ '\n\n'.join(row['prompt'] for row in rows)+'''
每帧都必须是不同且相邻的小步变化，禁止复制整段站姿或镜像凑帧。01→05→09→13→01是同一完整循环，16→01无突然跳变。全部16帧头脸和不活动的服装中心纹理一致，不在每格重新取景或改变画法。
纯#FF00FF洋红背景，没有格线、文字、帧编号、姿态线、阴影、武器、攻击特效、水印或其他元素。每个人严格留在自己的格子内，人物不跨格、不裁切。只输出这张连续帧图，不输出布局说明。
'''
    request={'image':str((素材/'固定人物布局.png').relative_to(制造项目)).replace('\\','/'),
             'reference_images':[str((素材/'连续姿态导引.png').relative_to(制造项目)).replace('\\','/')],
             'prompt':prompt,'name':'天帝_正面连续走路16帧_v4','target_size':'3072x3072','aspect_ratio':'1:1',
             'transparent':False,'model':'gpt','quality':'high','resolution':'4K','thinking_level':'high','target_dir':str(制造项目)}
    (输出/'完整提示词.txt').write_text(prompt,encoding='utf-8')
    (素材/'完整提示词.txt').write_text(prompt,encoding='utf-8')
    (输出/'请求.json').write_text(json.dumps(request,ensure_ascii=False,indent=2),encoding='utf-8')
    (输出/'逐帧清单.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Prepared 16 individual prompts and one continuous-sheet request; no generation dispatched.')


if __name__=='__main__':main()
