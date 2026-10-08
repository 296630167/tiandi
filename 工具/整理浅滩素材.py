from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image
root=Path(__file__).resolve().parents[1];out=root/'output/随机战场强化'
r=json.loads((out/'浅滩素材结果.json').read_text(encoding='utf-8'))['structuredContent']
p=Path('C:/Users/123/Documents/taptap製造_一滴水的故事')/r['localPath']
im=Image.open(p).convert('RGB');rgb=np.array(im).astype(float)/255
# 非发光地表只去黑底，内部保留原画不变；软透明外沿去掉黑色污染。
a=np.clip((rgb.max(axis=2)-.015)/.12,0,1)
clean=np.clip(rgb/np.maximum(a[:,:,None],1e-6),0,1)
rgba=(np.dstack((clean,a))*255+.5).astype('uint8')
ys,xs=np.where(a>.05);box=(int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1))
im=Image.fromarray(rgba).crop(box)
target=root/'Assets/天帝/Resources/随机战场';target.mkdir(parents=True,exist_ok=True)
im.save(target/'浅滩.png')
(out/'浅滩素材来源.json').write_text(json.dumps(dict(source=str(p),cdn=r['previewUrl'],sha256=hashlib.sha256(p.read_bytes()).hexdigest(),bbox=box),ensure_ascii=False,indent=2),encoding='utf-8')
