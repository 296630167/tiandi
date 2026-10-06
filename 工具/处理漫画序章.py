"""保留Maker原图；按实际漫画留白切三格，统一游戏使用的50%分界。"""
from pathlib import Path
import json
import shutil
import numpy as np
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/天帝/美术/漫画序章_v1'
PLAN = ROOT / 'output/imagegen/漫画序章_v1'
MAKER = Path(r'C:\Users\123\Documents\taptap製造_一滴水的故事')

def materialize():
    data = json.loads(json.loads((PLAN / '生成结果.json').read_text(encoding='utf-8'))['content'][0]['text'])
    edited = json.loads(json.loads((PLAN / '定点删除结果.json').read_text(encoding='utf-8'))['content'][0]['text'])
    # 修图响应与批量响应的字段保持原样，绝不对生成动作自动重试。
    edit_path = edited.get('absolutePath') or edited.get('localPath')
    if not edit_path:
        raise RuntimeError('修图没有可用本地结果，请先核对响应。')
    edit_path = Path(edit_path)
    if not edit_path.is_absolute():
        edit_path = MAKER / edit_path
    report = []
    for n, item in enumerate(data['results']):
        source = Path(item.get('absolutePath') or MAKER / 'assets/image/天帝漫画序章_P07_20261005123132.png')
        if n == 7:
            source = edit_path
        name = f'CM{n+1:02d}' if n < 8 else 'CB01'
        image = Image.open(source).convert('RGB')
        if n == 8:
            image.save(OUT / (name + '.png'))
            continue
        a = np.asarray(image)
        light = (a.min(2) > 210) & ((a.max(2).astype(int)-a.min(2)) < 40)
        score = light.mean(1)
        lo, hi = int(a.shape[0]*.35), int(a.shape[0]*.65)
        y = lo + int(score[lo:hi].argmax())
        xs = light[y+15:int(a.shape[0]*.95)].mean(0)
        l, h = int(a.shape[1]*.35), int(a.shape[1]*.65)
        x = l + int(xs[l:h].argmax())
        if score[y] < .90 or xs[x] < .85:
            raise RuntimeError(f'{name}无法可靠定位分格：{y}/{x}')
        canvas = Image.new('RGB', (1920,1080), (230,228,213))
        cuts = [(0,0,1920,y-4),(0,y+5,x-4,1080),(x+5,y+5,1920,1080)]
        boxes = [(0,0,1920,540),(0,540,960,1080),(960,540,1920,1080)]
        for cut, box in zip(cuts, boxes):
            w,h = box[2]-box[0],box[3]-box[1]
            panel = ImageOps.fit(image.crop(cut),(w,h),method=Image.Resampling.LANCZOS)
            canvas.paste(panel,box[:2])
        canvas.save(OUT / (name+'.png'))
        report.append({'id':name,'source':str(source),'divider':[x,y],'confidence':[float(xs[x]),float(score[y])]})
    (PLAN / '切格记录.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    # 同步保留远程映射，包含手动补全下载与修图版本；不修改Maker游戏代码。
    ledger = {'generation': data, 'edit': edited, 'processing': report}
    (MAKER / 'assets/image/天帝漫画序章_资源映射.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2),encoding='utf-8')
    thumbs = Image.new('RGB',(960,540),(230,228,213))
    for n,f in enumerate(sorted(OUT.glob('*.png'))):
        thumbs.paste(Image.open(f).convert('RGB').resize((320,180)),((n%3)*320,(n//3)*180))
    thumbs.save(PLAN / '成品总览.jpg')
    print(json.dumps(report,ensure_ascii=False))

if __name__ == '__main__':
    materialize()
