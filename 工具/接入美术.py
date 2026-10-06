from pathlib import Path
from PIL import Image
import hashlib
import json
import shutil

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r'C:\Users\123\Documents\2026聚光灯参赛项目\assets\image\天帝_75张整理版_20261003')
DEST = ROOT / 'Assets/天帝/美术/生成素材'
REPORT = ROOT / '生成/验证/美术接入'
DEST.mkdir(parents=True, exist_ok=True)
REPORT.mkdir(parents=True, exist_ok=True)
items = []
for path in sorted(SOURCE.glob('*.png')):
    with Image.open(path) as im:
        rgba = im.convert('RGBA')
        alpha = rgba.getchannel('A')
        items.append({'id': path.name.split('_')[0], 'file': path.name, 'size': list(im.size),
                      'alpha': list(alpha.getextrema()), 'bbox': alpha.getbbox(),
                      'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    shutil.copy2(path, DEST / path.name)
assert len(items) == 75 and len({i['id'] for i in items}) == 75

# One atlas keeps the visible hex canvas in one UI draw call.
ids = ['DW01', 'DW02'] + [f'AT{i:02}' for i in range(1, 6)] + [f'FN{i:02}' for i in range(1, 6)] + ['SK01'] + [f'TF{i:02}' for i in range(1, 14)]
atlas = Image.new('RGBA', (2048, 1024))
for index, ident in enumerate(ids):
    item = next(i for i in items if i['id'] == ident)
    with Image.open(DEST / item['file']) as raw:
        im = raw.convert('RGBA')
        im = im.crop(im.getchannel('A').getbbox())
        if ident == 'DW01':
            im = im.transpose(Image.Transpose.ROTATE_90)
        im.thumbnail((232, 232), Image.Resampling.LANCZOS)
        x, y = (index % 8) * 256, (index // 8) * 256
        atlas.alpha_composite(im, (x + (256 - im.width) // 2, y + (256 - im.height) // 2))
atlas.paste((255, 255, 255, 255), (0, 1020, 4, 1024))
atlas.save(DEST / 'UI_道纹图集.png')
(REPORT / '素材导入清单.json').write_text(json.dumps({'source': str(SOURCE), 'total': len(items), 'atlas': ids, 'items': items}, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'Copied {len(items)} images. Built 26-entry UI atlas. Audit saved to {REPORT}.')
