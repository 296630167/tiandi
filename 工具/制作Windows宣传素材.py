from pathlib import Path
import json
import shutil

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '生成/宣传素材/Windows素材-20261004'
SOURCE = OUT / '源图'
RECORD = json.loads((SOURCE / '生成记录.json').read_text(encoding='utf-8'))
RESAMPLE = Image.Resampling.LANCZOS
FILES = []


def source(name):
    item = next(x for x in RECORD['images'] if x['name'] == name)
    origin = Path(item['source'])
    local = SOURCE / (name + '.png')
    if origin.resolve() != local.resolve():
        shutil.copy2(origin, local)
    return Image.open(local)


def save(image, filename, size, transparent=False):
    assert image.size == size, (filename, image.size)
    path = OUT / filename
    if transparent:
        assert image.mode == 'RGBA'
        assert image.getchannel('A').getextrema() == (0, 255)
        image.save(path, optimize=True)
    else:
        image.convert('RGB').save(path, quality=97, subsampling=0, optimize=True)
    assert path.stat().st_size < 6 * 1024 * 1024, filename
    with Image.open(path) as check:
        check.verify()
    FILES.append({'文件': filename, '尺寸': list(size), '字节': path.stat().st_size, '透明': transparent})


def cover(art, size, logo, max_logo, margin, align_right=False):
    result = ImageOps.fit(art.convert('RGB'), size, method=RESAMPLE, centering=(0.5, 0.4)).convert('RGBA')
    title = logo.copy()
    title.thumbnail(max_logo, RESAMPLE)
    x = size[0] - title.width - margin if align_right else (size[0] - title.width) // 2
    y = size[1] - title.height - margin
    result.alpha_composite(title, (x, y))
    return result


logo = source('Windows_透明游戏Logo').convert('RGBA')
pixels = np.array(logo)
red, green, blue = (pixels[:, :, i].astype(np.int16) for i in range(3))
red_fringe = (red > 160) & (green < 80) & (blue < 80) & (red > green * 2) & (red > blue * 2)
# 抠图残留的红底改成字标暗描边色，保留原Alpha与笔画。
pixels[red_fringe, :3] = (64, 49, 33)
logo = Image.fromarray(pixels)
horizontal = source('Windows_无Logo横版宣传母图').convert('RGB')
vertical = source('Windows_无Logo竖版宣传母图').convert('RGB')
library = source('Windows_游戏库超宽背景').convert('RGB')
save(logo, '01_游戏Logo_1280x720.png', (1280, 720), True)
# 超宽画幅只裁切，不拉伸人物；偏上保留主角脸部与巨神目光。
library = ImageOps.fit(library, (3840, 1240), method=RESAMPLE, centering=(0.5, 0.08))
save(library, '02_游戏库背景_3840x1240.jpg', (3840, 1240))
title = logo.crop(logo.getchannel('A').getbbox())
save(cover(horizontal, (920, 430), title, (440, 182), 18, True), '03_横版封面_920x430.jpg', (920, 430))
save(cover(vertical, (800, 1200), title, (730, 266), 32), '04_竖版封面_800x1200.jpg', (800, 1200))
save(horizontal, '05_无Logo横版宣传图_1920x1080.jpg', (1920, 1080))
save(vertical, '06_无Logo竖版宣传图_1080x1620.jpg', (1080, 1620))
(SOURCE / '尺寸校验.json').write_text(json.dumps(FILES, ensure_ascii=False, indent=2), encoding='utf-8')

preview_dir = ROOT.parent / '预览/天帝_Windows素材'
preview_dir.mkdir(parents=True, exist_ok=True)
preview = Image.new('RGB', (1480, 1690), '#f2f4f4')
draw = ImageDraw.Draw(preview)
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 24)
labels = ['游戏 Logo · 透明 PNG', '游戏库背景 · 无文字', '横版游戏封面', '竖版游戏封面', '无 Logo 横版宣传图', '无 Logo 竖版宣传图']
for index, item in enumerate(FILES):
    column, row = index % 2, index // 2
    x, y = 30 + column * 730, 24 + row * 553
    draw.text((x, y), f'{index + 1:02d}  {labels[index]}', font=font, fill='#192d2c')
    draw.text((x, y + 34), f'{item["尺寸"][0]} x {item["尺寸"][1]}', font=font, fill='#536663')
    panel = Image.new('RGB', (700, 460), '#e5eaea')
    with Image.open(OUT / item['文件']) as image:
        picture = image.convert('RGBA')
        picture.thumbnail((700, 460), RESAMPLE)
        if item['透明']:
            panel.paste('#172927', (0, 0, 700, 460))
        px, py = (700 - picture.width) // 2, (460 - picture.height) // 2
        panel.paste(picture, (px, py), picture)
    preview.paste(panel, (x, y + 78))
preview.save(preview_dir / 'Windows素材总览.jpg', quality=94, subsampling=0, optimize=True)
print(json.dumps(FILES, ensure_ascii=False, indent=2))
