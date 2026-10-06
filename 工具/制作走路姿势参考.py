from pathlib import Path
from math import sin,cos,pi
from PIL import Image,ImageDraw,ImageFont

根=Path(__file__).resolve().parents[1]/'output/imagegen/主角八方向'
根.mkdir(parents=True,exist_ok=True)
字体=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',13)
方向名=['S','SE','E','NE','N','NW','W','SW']
for 页 in range(2):
    图=Image.new('RGB',(1536,1024),(248,244,229));画=ImageDraw.Draw(图)
    for 行 in range(4):
        角=(页*4+行)*pi/4
        前=(sin(角),-cos(角));侧=(cos(角),sin(角))
        def 投影(p,列): return (round(列*256+128+p[0]*123),round(行*256+227-p[1]*62-p[2]*148))
        for 列 in range(6):
            相=列*pi/3
            画.rectangle((列*256+2,行*256+2,列*256+253,行*256+253),outline=(186,181,166))
            画.text((列*256+8,行*256+7),f'{方向名[页*4+行]} / phase {列+1}',font=字体,fill=(50,50,50))
            头=(0,0,1.16);顶=投影(头,列)
            画.ellipse((顶[0]-19,顶[1]-26,顶[0]+19,顶[1]+15),outline=(45,62,49),width=3)
            胸=(0,0,.84);腰=(0,0,.53)
            画.line([投影(胸,列),投影(腰,列)],fill=(40,92,64),width=5)
            for s in [-1,1]:
                位=cos(相+(0 if s==-1 else pi));抬=max(0,sin(相+(0 if s==-1 else pi)))*.12
                髋=(侧[0]*s*.085,侧[1]*s*.085,.53)
                脚=(侧[0]*s*.09+前[0]*位*.30,侧[1]*s*.09+前[1]*位*.30,抬)
                膝=((髋[0]+脚[0])*.5+前[0]*.07,(髋[1]+脚[1])*.5+前[1]*.07,.25+抬*.6)
                肩=(侧[0]*s*.16,侧[1]*s*.16,.91)
                手=(侧[0]*s*.20-前[0]*位*.25,侧[1]*s*.20-前[1]*位*.25,.55)
                肘=((肩[0]+手[0])*.5,(肩[1]+手[1])*.5,.70)
                色=(180,58,40) if s==-1 else (35,94,175)
                画.line([投影(髋,列),投影(膝,列),投影(脚,列)],fill=色,width=5)
                画.line([投影(肩,列),投影(肘,列),投影(手,列)],fill=色,width=5)
                p=投影(脚,列);q=投影((脚[0]+前[0]*.13,脚[1]+前[1]*.13,脚[2]),列)
                画.line([p,q],fill=色,width=8)
                for 点 in [髋,膝,肩,肘,手]:
                    x,y=投影(点,列);画.ellipse((x-3,y-3,x+3,y+3),fill=色)
    图.save(根/f'步态参考_{页+1:02d}.png')
print('步态参考完成')
