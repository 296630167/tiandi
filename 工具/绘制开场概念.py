from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageEnhance

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT.parent / '预览' / '天帝_开场与主页概念' / '20261004'
ART = ROOT / 'Assets/天帝/美术/生成素材'
FONT = ROOT / 'Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf'
SIZE = (1920, 1080)
INK, PAPER, GREEN, GOLD = '#173e39', '#f3f5ed', '#326e5f', '#b39a59'
OUT.mkdir(parents=True, exist_ok=True)


def font(size):
    return ImageFont.truetype(str(FONT), size)


def text(img, pos, value, size=28, fill=INK, anchor='mm', shadow=False):
    draw = ImageDraw.Draw(img)
    if shadow:
        draw.text((pos[0] + 2, pos[1] + 3), value, font=font(size), fill=(12, 29, 29, 220), anchor=anchor, stroke_width=2)
    draw.text(pos, value, font=font(size), fill=fill, anchor=anchor)


def band(img, rect, fill):
    layer = Image.new('RGBA', SIZE)
    ImageDraw.Draw(layer).rectangle(rect, fill=fill)
    img.alpha_composite(layer)


def button(img, rect, label, primary=False):
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle(rect, radius=6, fill=GREEN if primary else PAPER, outline=GOLD if primary else '#98afa2', width=2)
    text(img, ((rect[0] + rect[2]) / 2, (rect[1] + rect[3]) / 2), label, 29, PAPER if primary else INK)


def gear(img, center):
    draw = ImageDraw.Draw(img)
    x, y = center
    draw.ellipse((x-26, y-26, x+26, y+26), fill=PAPER, outline='#98afa2', width=2)
    draw.ellipse((x-12, y-12, x+12, y+12), outline=INK, width=3)
    draw.ellipse((x-4, y-4, x+4, y+4), outline=INK, width=2)
    for i in range(8):
        a = i * math.pi / 4
        draw.line((x+12*math.cos(a), y+12*math.sin(a), x+19*math.cos(a), y+19*math.sin(a)), fill=INK, width=3)


def title():
    img = Image.open(ROOT / '生成/宣传素材/Windows素材-20261004/05_无Logo横版宣传图_1920x1080.jpg').convert('RGBA')
    img = ImageEnhance.Color(img).enhance(.78)
    band(img, (0, 690, 1920, 1080), (17, 43, 42, 136))
    text(img, (960, 742), '从参加聚光灯比赛', 46, PAPER, shadow=True)
    text(img, (960, 819), '到我为天帝镇压世间一切', 65, PAPER, shadow=True)
    button(img, (715, 900, 1205, 978), '继续游戏', True)
    text(img, (960, 1022), '新游戏', 27, PAPER)
    gear(img, (1830, 80))
    text(img, (76, 1032), '版本 0.17.0', 20, PAPER, anchor='lm')
    text(img, (1838, 1032), '退出', 24, PAPER, anchor='rm')
    return img


def prologue():
    img = Image.open(ART / 'SC06_两名修仙者飞过.png').convert('RGBA').resize(SIZE, Image.Resampling.LANCZOS)
    text(img, (66, 58), '序章 · 初到异世', 27, PAPER, anchor='lm', shadow=True)
    button(img, (1654, 28, 1854, 88), '跳过')
    band(img, (0, 848, 1920, 1080), (15, 34, 35, 208))
    text(img, (960, 899), '“给我干哪来了，这还是国内吗？”', 35, PAPER)
    text(img, (960, 951), '两名修仙者御剑掠过云海。我仰着头，眼睛都看直了。', 27, '#c4d5cb')
    draw = ImageDraw.Draw(img)
    draw.rectangle((80, 1002, 104, 1026), fill=PAPER)
    draw.rectangle((89, 1000, 95, 1028), fill='#102223')
    text(img, (145, 1015), '字幕：开', 22, PAPER, anchor='lm')
    text(img, (1840, 1015), '00:20 / 00:28', 22, PAPER, anchor='rm')
    draw.line((80, 1050, 1840, 1050), fill='#697d79', width=5)
    draw.line((80, 1050, 1337, 1050), fill='#d4b771', width=5)
    draw.ellipse((1330, 1043, 1344, 1057), fill=PAPER)
    return img


def home():
    img = Image.open(ART / 'BG01_标题与主页共用背景.png').convert('RGBA').resize(SIZE, Image.Resampling.LANCZOS)
    model = Image.open(ROOT / '生成/验证/主角骨骼待机-20261004-055722/01-主角透明渲染.png').convert('RGBA')
    model = model.crop(model.getchannel('A').getbbox())
    model = model.resize((round(model.width * 748 / model.height), 748), Image.Resampling.LANCZOS)
    shadow = Image.new('RGBA', SIZE)
    ImageDraw.Draw(shadow).ellipse((818, 817, 1102, 850), fill=(20, 54, 41, 82))
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(7)))
    img.alpha_composite(model, ((1920-model.width)//2, 92))
    button(img, (62, 62, 302, 132), '道纹')
    button(img, (62, 154, 302, 224), '道纹改造')
    band(img, (1370, 72, 1860, 353), (245, 249, 239, 210))
    text(img, (1405, 115), '我  ·  等级 1', 31, INK, anchor='lm')
    text(img, (1405, 178), '源道纹：穿越者', 27, INK, anchor='lm')
    text(img, (1405, 235), '剩余技能点  4', 27, GREEN, anchor='lm')
    ImageDraw.Draw(img).line((1405, 275, 1825, 275), fill='#9cae9c', width=1)
    text(img, (1405, 311), '已接通道纹  0', 24, '#526c60', anchor='lm')
    band(img, (594, 862, 1326, 920), (241, 247, 235, 224))
    text(img, (960, 891), '普通人，只能勉强射出一丝灵力', 28, INK)
    button(img, (740, 947, 1180, 1025), '开始', True)
    button(img, (1270, 956, 1732, 1016), '青岚原  ·  普通  ›')
    gear(img, (1830, 990))
    return img


pages = [title(), prologue(), home()]
names = ['01_开始页面概念', '02_序章页面概念', '03_游戏主页概念']
for name, page in zip(names, pages):
    sheet = Image.new('RGB', (1920, 1144), '#edf1eb')
    sheet.paste(page.convert('RGB'), (0, 64))
    text(sheet, (32, 32), name.split('_', 1)[1] + '  /  布局设计稿，非游戏实拍', 24, INK, anchor='lm')
    sheet.save(OUT / (name + '.jpg'), quality=94)

overview = Image.new('RGB', (1040, 1940), '#edf1eb')
text(overview, (38, 42), '开场与主页 · 视觉方向', 32, INK, anchor='lm')
text(overview, (38, 84), '概念图，未实施到游戏；开始页暂借现有宣传插画。', 21, '#526c60', anchor='lm')
for i, (name, page) in enumerate(zip(names, pages)):
    y = 142 + i * 596
    text(overview, (40, y), name.split('_', 1)[1], 25, INK, anchor='lm')
    overview.paste(page.convert('RGB').resize((960, 540), Image.Resampling.LANCZOS), (40, y + 26))
overview.save(OUT / '三页概念总览.jpg', quality=94)
print(OUT)
