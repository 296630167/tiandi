from pathlib import Path
import json, hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import label

root = Path(__file__).resolve().parents[1]
out = root/'output/敌人独立美术'
target = root/'Assets/天帝/Resources/敌人独立美术'
target.mkdir(parents=True, exist_ok=True)
maker = Path('C:/Users/123/Documents/taptap製造_一滴水的故事')
specs = json.loads((out/'设计映射.json').read_text(encoding='utf-8'))
records = []
for kind, resultfile in [('角色', '角色批量结果.json'), ('点缀', '点缀批量结果.json')]:
    if not (out/resultfile).exists(): continue
    results = json.loads((out/resultfile).read_text(encoding='utf-8'))['structuredContent']['results']
    for spec, r in zip(specs, results):
        if not r.get('success'): raise RuntimeError(r)
        p = maker/r['localPath']
        rgb = np.array(Image.open(p).convert('RGB')).astype(float)/255
        v = rgb.max(axis=2)
        if kind == '角色':
            # Only exterior-connected black background is removed; dark fur/armor remains opaque.
            groups, _ = label(v < .14)
            edge = np.unique(np.concatenate((groups[0], groups[-1], groups[:,0], groups[:,-1])))
            bg = np.isin(groups, edge[edge != 0])
            a = np.ones(v.shape)
            a[bg] = np.clip((v[bg]-.018)/.12,0,1)
            # Remove near-black enclosed negative space between legs, tails and feathers.
            a[v < .04] = np.clip((v[v < .04]-.012)/.028,0,1)
        else:
            a = np.clip((v-.015)/.22,0,1)
        clean = np.clip(rgb/np.maximum(a[:,:,None],1e-6),0,1)
        rgba = (np.dstack((clean,a))*255+.5).astype('uint8')
        ys,xs = np.where(a>.08)
        box = (max(0,int(xs.min())-8),max(0,int(ys.min())-8),min(1024,int(xs.max())+9),min(1024,int(ys.max())+9))
        code = spec['code'] if kind=='角色' else 'BFX'+str(spec['species'])
        Image.fromarray(rgba).crop(box).save(target/(code+'.png'))
        records.append(dict(code=code,name=spec['name'],kind=kind,source=str(p),cdn=r['previewUrl'],sha256=hashlib.sha256(p.read_bytes()).hexdigest(),bbox=box))
(out/'素材来源.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
sheet = Image.new('RGB',(1120,640),(58,73,65))
d = ImageDraw.Draw(sheet)
for i,s in enumerate(specs):
    im = Image.open(target/(s['code']+'.png')).convert('RGBA');im.thumbnail((270,260))
    x=(i%4)*280+(280-im.width)//2;y=(i//4)*320+35
    sheet.paste(im,(x,y),im)
    d.text(((i%4)*280+18,(i//4)*320+5),s['name'],font=font,fill='white')
sheet.save(out/'独立敌人检查.png')
print('processed',len(records))
