"""整理其余剪纸页的山水底、纸框与画布材质；首两页改用独立山水资源。"""
from pathlib import Path
from PIL import Image, ImageFilter
import numpy as np
import json, hashlib

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / 'Assets/天帝/Resources/剪纸界面'
OUT = ROOT / 'output/剪纸可读性修复_20261007'
OUT.mkdir(parents=True, exist_ok=True)
sources, outputs = [], []

def record(path, purpose):
    sources.append({'source': str(path), 'purpose': purpose,
                    'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})

tile_path = UI / '素纸.png'
scene_path = UI / '标题山水背景.png'
frame_path = UI / '朱红纸框.png'
for p, purpose in [(tile_path, '纸纹材质'), (scene_path, '已批准无UI山水原画'), (frame_path, '已清理的纸框及角纹')]:
    record(p, purpose)
tile = Image.open(tile_path).convert('RGB')
paper = Image.new('RGB', (1920, 1080))
for y in range(0, 1080, tile.height):
    for x in range(0, 1920, tile.width):
        paper.paste(tile, (x, y))
p = np.asarray(paper, dtype=float)
grain = np.clip(p.mean(2) / p.mean(), .90, 1.08)
scene = np.asarray(Image.open(scene_path).convert('RGB').resize((1920,1080)).filter(ImageFilter.GaussianBlur(2.2)), dtype=float)
yy, xx = np.mgrid[0:1080, 0:1920]
edge = np.maximum(np.abs(xx-960)/960, (yy/1080)**2)
header = np.clip((yy - 80) / 220, 0, 1)
weight = ((.12 + .42 * edge) * (.3 + .7 * header))[:, :, None]
sage_paper = grain[:, :, None] * np.array([211, 222, 205])[None, None, :]
page = Image.fromarray(np.uint8(np.clip(scene * weight + sage_paper * (1-weight), 0, 255)))
for name in ['角色', '构筑', '回收', '图鉴', '宝盒']:
    page.save(UI / (name+'背景.png'))

# 此材质只作为真实六边网格的背景，网格仍由原绘图组件绘制。
dark = np.uint8(np.clip(grain[::3, ::3, None] * np.array([39, 70, 65]), 0, 255))
Image.fromarray(dark).save(UI / '墨青画布.png')
frame = np.array(Image.open(frame_path).convert('RGBA'))
rgb = frame[:, :, :3].astype(float)
ink = (rgb[:, :, 0] > rgb[:, :, 1] * 1.22) & (rgb[:, :, 0] > rgb[:, :, 2] * 1.3)
bright = rgb.mean(2) > 155
frame[:, :, :3][ink] = (70, 101, 86)
frame[:, :, :3][bright] = np.uint8(np.clip(rgb[bright] * np.array([.95, .98, 1.02]), 0, 255))
Image.fromarray(frame).save(UI / '卡片纸框.png')

for directory in [UI]:
    for path in directory.glob('*.png'):
        outputs.append({'path': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
(OUT/'素材处理映射.json').write_text(json.dumps({'new_remote_requests': 0, 'processing': '已有原画复用、透明图标原色恢复、山水与纸纹合成、材质明暗分层；不生成新玩法或道具', 'sources': sources, 'outputs': outputs}, ensure_ascii=False, indent=2), encoding='utf-8')
print('Layered parchment/scenery materials prepared for the remaining pages.')
