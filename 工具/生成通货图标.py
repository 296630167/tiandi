"""原创通货图标，透明底；生成源保留，图标不嵌入文字。"""
from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/天帝/Resources/通货图标'
OUT.mkdir(parents=True, exist_ok=True)
NAMES = ['启灵石','点玄石','紫蕴石','无瑕玉','凝华石','蕴玄石','琢天玉','通脉针','六通玉','添蕴砂','易纹砂','重铸石','问天石']
COLORS = ['#86d8b4','#70b8f4','#bd8aef','#ffd295','#75d8e4','#de97d9','#f2b660','#cedbd6','#91e0c7','#d9c080','#b3a1df','#d98968','#e4c477']
def rgb(c): return tuple(bytes.fromhex(c[1:]))
def shade(c, k): return tuple(min(255, int(v*k)) for v in rgb(c))
def regular(cx,cy,r,n,angle=0):
    return [(cx+math.cos(angle+i*math.tau/n)*r,cy+math.sin(angle+i*math.tau/n)*r) for i in range(n)]

for idx, (name, c) in enumerate(zip(NAMES, COLORS)):
    im = Image.new('RGBA', (256,256)); d = ImageDraw.Draw(im)
    d.ellipse((41,215,215,241), fill=(0,0,0,80))
    if idx < 7:
        polygons = [
            [(126,25),(196,73),(188,181),(128,224),(62,177),(56,74)],
            [(121,20),(177,45),(204,158),(162,211),(105,231),(57,176),(69,76)],
            [(88,43),(158,35),(206,91),(193,181),(132,225),(65,185),(50,105)],
            regular(128,130,94,8,math.pi/8),
            [(82,25),(144,36),(196,112),(180,207),(122,227),(57,161),(54,79)],
            [(128,21),(196,64),(210,154),(164,213),(90,227),(51,159),(57,78)],
            regular(128,126,101,6,math.pi/6),
        ]
        pts = polygons[idx]; center = (129,127)
        for k in range(len(pts)):
            d.polygon([center,pts[k],pts[(k+1)%len(pts)]], fill=shade(c,[.40,.62,.88,.54,.72,.95,.48,.75][k]))
        inner = [(128+(x-128)*.62,126+(y-126)*.62) for x,y in pts]
        d.polygon(inner, fill=shade(c,.73)); d.line(inner+[inner[0]],fill=shade(c,1.14),width=3)
        d.line(pts+[pts[0]],fill=shade(c,1.17),width=5)
        # 每件道具的雕纹布局不同。
        y = 100 + idx%3*6
        d.line([(106,y),(148,y),(128,y+23),(128,y+55)], fill=(245,247,220,235),width=5)
        d.line([(112,y+42),(144,y+42)], fill=(245,247,220,220),width=4)
        if idx in [3,6]: d.arc((74,72,182,183),20,320,fill=(255,235,170,240),width=5)
        d.line([pts[0],pts[1]],fill=(245,255,246,235),width=6)
    elif idx == 7:
        # 通脉针：金属针与玉柄。
        d.polygon([(65,226),(114,100),(132,111)], fill=(176,207,204),outline=(242,247,237))
        d.polygon([(106,107),(144,24),(170,38),(132,121)],fill=shade(c,.55))
        d.line([(114,102),(149,31)],fill=(246,255,240),width=6)
        d.line([(98,107),(139,125)],fill=(217,180,93),width=11)
        d.ellipse((140,22,176,55),fill=(108,195,172),outline=(232,224,163),width=5)
        d.line([(156,46),(183,87),(191,110)],fill=(209,169,82),width=4)
    elif idx == 8:
        # 六通玉：六向镶嵌玉环。
        pts=regular(128,128,83,6,math.pi/6)
        d.polygon(pts,fill=shade(c,.41),outline=shade(c,1.1),width=6)
        d.ellipse((75,75,181,181),fill=(16,43,45),outline=shade(c,1.0),width=13)
        for x,y in pts:
            d.line([(128,128),(x,y)],fill=shade(c,.85),width=7)
            d.ellipse((x-14,y-14,x+14,y+14),fill=shade(c,1.0),outline=(252,232,167),width=4)
        d.polygon(regular(128,128,25,6),fill=(221,201,134),outline=(250,237,201),width=3)
    elif idx in [9,10]:
        # 添蕴砂和易纹砂：沙囊与瓶。
        if idx==9:
            d.polygon([(91,67),(163,67),(179,99),(203,181),(177,219),(78,221),(52,186),(74,101)],fill=shade(c,.64))
            d.line([(79,105),(173,105)],fill=(237,215,153),width=7)
            d.polygon([(83,68),(72,29),(128,42),(178,25),(166,72)],fill=shade(c,.9))
            d.ellipse((88,133,168,199),outline=(245,219,165),width=4)
        else:
            d.rounded_rectangle((77,73,182,220),radius=29,fill=shade(c,.42),outline=shade(c,1.05),width=5)
            d.rectangle((100,27,157,76),fill=shade(c,.60),outline=(232,204,145),width=5)
            d.polygon([(83,144),(121,130),(171,146),(175,200),(96,209)],fill=shade(c,.89))
            d.line([(89,95),(89,157)],fill=(247,233,255),width=5)
        for k in range(9):
            x=100+(k*17)%56; y=145+(k*23)%45
            d.ellipse((x,y,x+5,y+5),fill=(255,241,178))
    elif idx == 11:
        # 重铸石：铸炉赤铁与回环刻纹。
        pts=[(80,42),(161,35),(205,94),(190,188),(137,224),(57,195),(42,105)]
        d.polygon(pts,fill=shade(c,.52),outline=shade(c,1.1),width=5)
        d.line([(80,42),(93,103),(66,157),(137,224)],fill=shade(c,.92),width=4)
        d.arc((79,75,177,174),25,295,fill=(255,222,167),width=9)
        d.polygon([(170,67),(181,106),(145,95)],fill=(255,222,167))
        d.line([(107,131),(145,131)],fill=(255,222,167),width=6)
    else:
        # 问天石：深色石盘和金色星轨。
        d.polygon(regular(128,128,103,10,-math.pi/2),fill=(43,56,71),outline=rgb(c),width=5)
        d.ellipse((55,55,201,201),outline=shade(c,.70),width=4)
        d.ellipse((72,72,184,184),outline=shade(c,1.0),width=3)
        d.polygon([(128,69),(143,111),(187,128),(143,144),(128,190),(113,144),(69,128),(113,111)],fill=rgb(c))
        d.polygon([(128,104),(152,128),(128,152),(104,128)],fill=(255,245,211))
        for x,y in regular(128,128,85,6): d.ellipse((x-4,y-4,x+4,y+4),fill=(255,236,181))
    im.save(OUT / (name + '.png'))

sheet = Image.new('RGB',(7*180,2*190),(15,29,34)); d=ImageDraw.Draw(sheet)
for idx,name in enumerate(NAMES):
    icon=Image.open(OUT/(name+'.png')).resize((150,150),Image.Resampling.LANCZOS)
    sheet.paste(icon,(idx%7*180+15,idx//7*190+10),icon)
    d.text((idx%7*180+70,idx//7*190+165),str(idx+1),fill=(235,227,185))
preview=ROOT/'生成/验证/通货图标一览.png'; preview.parent.mkdir(exist_ok=True,parents=True);sheet.save(preview)
print('已生成13枚原创图标：'+str(OUT))
