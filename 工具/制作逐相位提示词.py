from pathlib import Path
from math import sin,cos,pi
from PIL import Image,ImageDraw,ImageFont

根=Path(__file__).resolve().parents[1]/'output/imagegen/主角八方向'
名=['SOUTH front','SOUTHEAST front-right','EAST right profile','NORTHEAST back-right','NORTH back','NORTHWEST back-left','WEST left profile','SOUTHWEST front-left']
相位=['LEFT foot forward CONTACT, right leg behind','RIGHT leg PASSING the planted LEFT support leg, right knee bent and right foot LIFTED off the floor','RIGHT leg REACHING forward, left leg pushing off','RIGHT foot forward CONTACT, left leg behind','LEFT leg PASSING the planted RIGHT support leg, left knee bent and left foot LIFTED off the floor','LEFT leg REACHING forward, right leg pushing off']
for 帧 in range(6):
    图=Image.new('RGB',(1536,1024),(246,243,230));画=ImageDraw.Draw(图)
    字体=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
    for 序 in range(8):
        列,行=序%4,序//4;角=序*pi/4;相=帧*pi/3
        前=(sin(角),-cos(角));侧=(cos(角),sin(角))
        def 投影(p):return (round(列*384+192+p[0]*218),round(行*512+462-p[1]*110-p[2]*318))
        画.rectangle((列*384+2,行*512+2,列*384+381,行*512+509),outline=(183,177,160))
        画.text((列*384+8,行*512+6),名[序],font=字体,fill=(45,50,42))
        x,y=投影((0,0,1.18));画.ellipse((x-35,y-48,x+35,y+22),outline=(35,65,45),width=5)
        画.line([投影((0,0,.92)),投影((0,0,.55))],fill=(35,90,58),width=8)
        for s in [-1,1]:
            p=cos(相+(0 if s==-1 else pi));抬=max(0,sin(相+(0 if s==-1 else pi)))*.20
            髋=(侧[0]*s*.09,侧[1]*s*.09,.55)
            脚=(侧[0]*s*.095+前[0]*p*.33,侧[1]*s*.095+前[1]*p*.33,抬)
            膝=((髋[0]+脚[0])*.5+前[0]*.10,(髋[1]+脚[1])*.5+前[1]*.10,.27+抬*.7)
            肩=(侧[0]*s*.17,侧[1]*s*.17,.94)
            手=(侧[0]*s*.21-前[0]*p*.29,侧[1]*s*.21-前[1]*p*.29,.57)
            肘=((肩[0]+手[0])*.5,(肩[1]+手[1])*.5,.74)
            色=(182,57,40) if s==-1 else (37,100,180)
            画.line([投影(髋),投影(膝),投影(脚)],fill=色,width=8)
            画.line([投影(肩),投影(肘),投影(手)],fill=色,width=8)
            q=(脚[0]+前[0]*.15,脚[1]+前[1]*.15,脚[2]);画.line([投影(脚),投影(q)],fill=色,width=15)
            for 点 in [髋,膝,肩,肘,手]:
                x,y=投影(点);画.ellipse((x-5,y-5,x+5,y+5),fill=色)
    图.save(根/f'相位{帧+1:02d}_姿势参考.png')
    提示=f'''Use case: precise pose transfer, painted 2D GAME SPRITES.
INPUT 1 is a full sheet of EIGHT SKELETAL POSES. Treat it as the image to render: exactly preserve each head, shoulder, wrist, hip, knee and foot position and heading. RED is anatomical left limb and BLUE right limb. Turn every skeleton into the fully clothed character from INPUT 2, concealing all skeleton colors/lines, labels and cell borders. DO NOT replace the supplied poses with the standing pose of INPUT 2.
INPUT 2 is identity/clothing/painting reference ONLY: the same anime programmer boy with tousled short near-black hair, black glasses, loose forest-green hoodie with cream drawstrings/cuffs and offwhite collar, charcoal-grey straight pants, cream lace-up sneakers, no weapon.
INPUT 3 is the approved CONTACT sheet. Use it as a STRICT heading and character model anchor: copy EACH cell's exact head rotation, face/hood/back visibility, hairstyle silhouette, camera elevation, torso proportions, colors and clothes into the matching cell. Change the LIMB POSE to INPUT1's phase, not the body heading. In particular SOUTH stays perfectly frontal and EAST/WEST stay pure profiles. The front-facing SOUTH boy must NOT look right or turn diagonally. Keep the head and shoulders of input3's respective cell essentially unchanged.

Render exactly ONE 1536x1024 sprite sheet, EXACTLY FOUR columns and TWO rows, EIGHT complete separate full-body sprites. Each cell is384x512. Rows/columns are read left-to-right top-to-bottom. The EIGHT headings MUST remain in this precise order: SOUTH(front), SOUTHEAST(front-right), EAST(right profile), NORTHEAST(back-right), NORTH(straight back, no face), NORTHWEST(back-left), WEST(left profile), SOUTHWEST(front-left).
ALL EIGHT sprites depict ONE PARTICULAR WALK PHASE: {相位[帧]}. This is phase {帧+1} of a six-frame gait, NOT a neutral standing character sheet. Follow the matching skeleton support/knee bend and opposite arm swing EXACTLY. Keep the planted foot on the floor and the passing foot raised where specified. Body and shoe heading agree. Accurate occlusion of far limbs. Particularly reproduce the difference between extended leg contact and bent-knee passing: NO fixed wide split stance if the guide shows passing. No stiff zombie arms.
Fixed orthographic camera looking down about35 degrees below horizontal, matching input2. Full body, centered support point localx192, head near localy65, ground shoe contact near y464, approximately400px tall. Maintain same anatomy, head size, physique, hoodie shape and coloring in all headings. Soft painted pale-green anime aesthetic, young adult slim proportions, NOT chibi, NOT plastic3D. Gentle fixed upper-left light. No camera motion, no clipping or scene.
Background only perfectly uniform PURE MAGENTA RGB255,0,255. No painted floor, cast shadow, gradients, glow, text, labels, arrows, skeletons, guide grid, logos or watermark. Separate the eight sprites with generous empty space. All hair, hand and shoes inside their own cell. Use the skeletal image as precise pose target; use character image only for clothing, face, palette and texture.''' 
    (根/f'相位{帧+1:02d}_提示词.txt').write_text(提示,encoding='utf-8')
print('6个相位姿势与提示词完成')
