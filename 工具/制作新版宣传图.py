from pathlib import Path
import argparse
import json
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '生成/宣传素材/宣传图重制-20261004'
TITLE = '从参加聚光灯比赛到我为天帝镇压世间一切'
OUT.mkdir(parents=True, exist_ok=True)


def prepare():
    sources = {
        '仙帝_干净参考.jpg': ROOT / '生成/宣传素材/游戏图标-20261004/游戏图标_仙帝道纹_1024.png',
        '对峙_干净参考.jpg': ROOT / '生成/宣传素材/Windows素材-20261004/05_无Logo横版宣传图_1920x1080.jpg',
    }
    for name, source in sources.items():
        image = Image.open(source).convert('RGB')
        image.thumbnail((900, 900), Image.Resampling.LANCZOS)
        image.save(OUT / name, quality=77, optimize=True)
    print(OUT)


def compose(source):
    image = Image.open(source).convert('RGB')
    if image.size != (1920, 1080):
        raise ValueError(f'API output dimensions differ: {image.size}')
    image.save(OUT / '02_无字宣传图_1920x1080.jpg', quality=94, optimize=True)
    image.save(OUT / '生成原图.png', optimize=True)
    image = image.convert('RGBA')
    shadow = Image.new('RGBA', image.size)
    draw = ImageDraw.Draw(shadow)
    title_font = ImageFont.truetype('C:/Windows/Fonts/simkai.ttf', 83)
    bounds = draw.textbbox((0, 0), TITLE, font=title_font)
    while bounds[2] - bounds[0] > 1740:
        title_font = ImageFont.truetype('C:/Windows/Fonts/simkai.ttf', title_font.size - 1)
        bounds = draw.textbbox((0, 0), TITLE, font=title_font)
    x = (1920 - (bounds[2] - bounds[0])) / 2 - bounds[0]
    y = 53 - bounds[1]
    draw.text((x, y + 6), TITLE, font=title_font, fill=(12, 27, 28, 240), stroke_width=8, stroke_fill=(12, 27, 28, 240))
    image.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(3)))
    draw = ImageDraw.Draw(image)
    draw.text((x, y), TITLE, font=title_font, fill='#fbf7e8', stroke_width=4, stroke_fill='#766139')
    image.convert('RGB').save(OUT / '01_宣传图_含完整游戏名_1920x1080.jpg', quality=94, optimize=True)
    report = []
    for file in OUT.glob('*1920x1080.jpg'):
        with Image.open(file) as output:
            report.append({'file': file.name, 'size': list(output.size), 'bytes': file.stat().st_size})
    (OUT / '尺寸校验.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--source')
    args = parser.parse_args()
    compose(Path(args.source)) if args.source else prepare()
