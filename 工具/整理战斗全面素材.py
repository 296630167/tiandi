from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from scipy.ndimage import label
root=Path(__file__).resolve().parents[1];out=root/'output/战斗全面优化';maker=Path('C:/Users/123/Documents/taptap製造_一滴水的故事')
records=[]
def result(file):
    r=json.loads(file.read_text(encoding='utf-8'))['structuredContent']
    if r.get('failed',0):raise RuntimeError('生成存在失败，请逐项核对：'+str(file))
    return r['results']
def cut(im,glow=False):
    rgb=np.asarray(im.convert('RGB')).astype(float)/255;v=rgb.max(2)
    if glow:a=np.clip((v-.015)/.22,0,1)
    else:
        labs,_=label(v<.14);edge=np.unique(np.r_[labs[0],labs[-1],labs[:,0],labs[:,-1]])
        bg=np.isin(labs,edge[edge>0]);a=np.ones(v.shape);a[bg]=np.clip((v[bg]-.018)/.12,0,1);a[v<.04]=np.clip((v[v<.04]-.012)/.028,0,1)
    rgb=np.clip(rgb/np.maximum(a[:,:,None],1e-6),0,1)
    rgba=Image.fromarray((np.dstack((rgb,a))*255+.5).astype('uint8'))
    ys,xs=np.where(a>.1);box=(int(xs.min()),int(ys.min()),int(xs.max())+1,int(ys.max())+1)
    return rgba.crop(box)
def provenance(r,code):
    p=maker/r['localPath'];records.append(dict(code=code,source=str(p),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),cdn=r.get('previewUrl')));return p
motion=root/'Assets/天帝/Resources/敌人动作';motion.mkdir(exist_ok=True)
mapping=json.loads((out/'动作映射.json').read_text(encoding='utf-8'));lookup={x['code']:x for x in mapping}
for batch in range(1,4):
    f=out/f'动作批次{batch}结果.json'
    if not f.exists():continue
    for r in result(f):
        code=r['name'].split('动作_')[1].split('_v')[0];p=provenance(r,code);sheet=Image.open(p);w,h=sheet.size
        pieces=[cut(sheet.crop((i%2*w//2,i//2*h//2,(i%2+1)*w//2,(i//2+1)*h//2))) for i in range(4)]
        idle=Image.open(lookup[code]['source']).convert('RGBA');idle=idle.crop(idle.getbbox())
        pieces.append(idle)
        # All keys share one canvas, fixed foot pivot and body scale; original idle remains available.
        idleWidth=idle.width;idleHeight=idle.height
        base=min(400/idleWidth,400/idleHeight)
        for i,im in enumerate(pieces):
            scale=base if i==4 else base*idleWidth/((pieces[0].width+pieces[1].width)/2)
            scale=min(scale,450/im.width,430/im.height)
            im=im.resize((max(1,round(im.width*scale)),max(1,round(im.height*scale))),Image.Resampling.LANCZOS)
            canvas=Image.new('RGBA',(512,512));canvas.alpha_composite(im,((512-im.width)//2,451-im.height));canvas.save(motion/f'{code}_{i}.png')
    # representative combined contact sheet
    rs=result(f);inspect=Image.new('RGB',(1200,((len(rs)+2)//3)*400),(45,60,51));draw=ImageDraw.Draw(inspect)
    for j,r in enumerate(rs):
        p=maker/r['localPath'];im=Image.open(p).convert('RGB');im.thumbnail((390,370));inspect.paste(im,((j%3)*400,(j//3)*400+25));draw.text(((j%3)*400+8,(j//3)*400+4),r['name'].split('动作_')[1],fill='white')
    inspect.save(out/f'动作批次{batch}检查.jpg')
f=out/'形态结果.json'
if f.exists():
    rs=result(f);dir=root/'Assets/天帝/Resources/独立攻击形态';dir.mkdir(exist_ok=True);atlas=Image.new('RGBA',(2048,1536));preview=Image.new('RGB',(1200,600),(45,60,51));font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',20)
    for i,r in enumerate(rs):
        p=provenance(r,r['name']);im=cut(Image.open(p),True);im=im.resize((496,496),Image.Resampling.LANCZOS);atlas.alpha_composite(im,(i%4*512+8,i//4*512+8));pr=im.resize((200,200));preview.paste(pr,(i%6*200,i//6*300+30),pr);ImageDraw.Draw(preview).text((i%6*200+8,i//6*300+3),r['name'].split('形态_')[1].split('_v')[0],font=font,fill='white')
    atlas.save(dir/'形态图集.png');preview.save(out/'形态检查.jpg')
(out/'美术来源.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print('已整理来源',len(records))
