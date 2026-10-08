from pathlib import Path
import subprocess, json, shutil, re
from PIL import Image, ImageDraw, ImageFont, ImageOps

OUT = Path(__file__).resolve().parent
ROOT = OUT.parent
SRC = OUT / '录制源'
FF = ROOT / '生成/录制工具/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
VIDEO = OUT / '视频'
FIG = OUT / '截图'
VIDEO.mkdir(exist_ok=True)
FIG.mkdir(exist_ok=True)
report = json.loads((SRC / '录制报告.json').read_text(encoding='utf-8-sig'))
if not report['完成'] or report['错误']:
    raise RuntimeError('本轮采集未成功完成')
duration = report['帧数'] / 30
segments = report['片段']

def stamp(seconds):
    ms = round(seconds * 1000)
    return f'{ms//3600000:01d}:{ms//60000%60:02d}:{ms//1000%60:02d}.{ms%1000//10:02d}'

ass = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 1080
WrapStyle: 2
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Main,Microsoft YaHei,30,&H00E9F3F3,&H000000FF,&H00302316,&H00302316,0,0,0,0,100,100,0,0,1,0,0,2,24,24,13,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
for seg in segments:
    ass += f"Dialogue: 0,{stamp(seg['开始帧']/30)},{stamp(seg['结束帧']/30)},Main,,0,0,0,,{seg['名']}\n"
(SRC/'章节字幕.ass').write_text(ass, encoding='utf-8-sig')
# 保留完整游戏画面，字幕放在另留的底栏，不覆盖任何游戏控件。
filters = 'scale=1818:1022:flags=lanczos,pad=1920:1080:51:0:color=0x163023,ass=章节字幕.ass'
cmd = [str(FF), '-hide_banner', '-loglevel', 'error', '-y', '-i', str(SRC/'当前版本原始实录.mp4'),
       '-stream_loop', '-1', '-i', str(ROOT/'Assets/天帝/音频/music-0.wav'),
       '-vf', filters, '-af', f'volume=0.20,afade=t=in:st=0:d=0.7,afade=t=out:st={duration-1.5}:d=1.5',
       '-map', '0:v:0', '-map', '1:a:0', '-t', str(duration), '-r', '30', '-c:v', 'libx264',
       '-preset', 'fast', '-crf', '19', '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-b:a', '192k',
       '-movflags', '+faststart', '-metadata', 'title=第三期开发日志 当前版本展示',
       str(VIDEO/'第三期当前版本展示_32秒.mp4')]
subprocess.run(cmd, cwd=SRC, check=True)

for source, name in [(4,'01_主页'), (7,'02_道纹构筑'), (14,'03_青岚原实战'), (8,'04_道纹改造'),
                     (9,'05_道纹回收'), (1,'06_标题'), (10,'07_图鉴'), (6,'08_宝盒'), (5,'09_角色'), (11,'10_设置')]:
    shutil.copy2(SRC/f'画面-{source:02d}.jpg', FIG/f'{name}.jpg')

# 提取最终编码的代表帧，确认字幕、场景和页面切换。
times = [0.6, 2, 3, 4.5, 6, 7.5, 9.5, 12, 14.3, 16, 18.5, 19.5, 22, 27, 31.4]
contact = Image.new('RGB', (1280, 5*210), '#163023')
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 17)
for i, t in enumerate(times):
    dest = SRC / f'成片帧-{i:02d}.jpg'
    subprocess.run([str(FF), '-hide_banner', '-loglevel', 'error', '-y', '-ss', str(t),
                    '-i', str(VIDEO/'第三期当前版本展示_32秒.mp4'), '-frames:v', '1', '-q:v', '3', str(dest)], check=True)
    tile = ImageOps.contain(Image.open(dest), (420, 184))
    contact.paste(tile, ((i%3)*426, (i//3)*210))
    ImageDraw.Draw(contact).text(((i%3)*426+4, (i//3)*210+186), f'{t:04.1f}s', fill='white', font=font)
contact.save(SRC/'最终成片总览.jpg', quality=90)

# 完整解码检查，同时读取帧率、分辨率、时长与音轨。
decoded = subprocess.run([str(FF), '-hide_banner', '-v', 'info', '-i', str(VIDEO/'第三期当前版本展示_32秒.mp4'),
                          '-f', 'null', '-'], capture_output=True, text=True, encoding='utf-8', errors='replace')
if decoded.returncode:
    raise RuntimeError(decoded.stderr)
(SRC/'视频解码检查.log').write_text(decoded.stderr, encoding='utf-8')
(VIDEO/'视频章节.md').write_text(
    '# 当前版本展示\n\n32秒，1920×1080，30fps，H.264视频、AAC音轨。游戏画面原速，省去加载与开场空等；配乐为项目现有音乐，后期配入，不是实时音效录音。\n\n'
    + '\n'.join(f"- {seg['开始帧']/30:05.1f}秒 {seg['名']}" for seg in segments)
    + '\n\n使用现有1级进度的隔离副本、正式一级地图；没有添加测试道纹、资源、无敌或伤害。图鉴展示其它功能分类，实战使用当前实际拥有的构筑。\n', encoding='utf-8')
(SRC/'成片信息.json').write_text(json.dumps({'时长秒':duration, '帧数':report['帧数'], '宽':1920, '高':1080,
    '帧率':30, '完整解码成功':True, '战斗击败数':report['击败数'], '玩家死亡':report['玩家死亡'],
    '配乐':'Assets/天帝/音频/music-0.wav（后期配入）'}, ensure_ascii=False, indent=2), encoding='utf-8')

md = (OUT/'第三期开发日志.md').read_text(encoding='utf-8')
plain = re.sub(r'!\[([^\]]+)\]\([^)]+\)', r'【配图：\1】', md)
plain = re.sub(r'^#{1,3} ', '', plain, flags=re.M)
plain = plain.replace('视频：[第三期当前版本展示](视频/第三期当前版本展示_32秒.mp4)', '【附视频：第三期当前版本展示_32秒.mp4】')
(OUT/'第三期开发日志_可复制正文.txt').write_text(plain, encoding='utf-8-sig')
print(json.dumps({'视频':str(VIDEO/'第三期当前版本展示_32秒.mp4'),'秒':duration,'截图':len(list(FIG.glob('*.jpg')))},ensure_ascii=False))
