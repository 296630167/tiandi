"""核对用户指定的24个MP3，备份旧旁白，再按原VO编号替换；不修改meta或字幕。"""
from pathlib import Path
from datetime import datetime
import argparse
import hashlib
import json
import re
import shutil
import subprocess

import imageio_ffmpeg
import numpy as np
from scipy.io import wavfile

ROOT = Path(__file__).resolve().parents[1]
DOWNLOADS = Path('C:/Users/123/Downloads')
NAMES = [
    '午后三点我给自己报了个游戏开发比赛.mp3',
    '报名成功 文件夹建好了名字也起好了.mp3',
    '咖啡从烫嘴放到了温热.mp3',
    '主题终于出现涌现️03s.mp3',
    '很多简单的东西凑在一起竟会长出谁都没预料.mp3',
    '我闭上眼睛手指在头顶画圈️03s.mp3',
    '纸上落下几个点 我把它们连起来️03s规.mp3',
    '头顶忽然传来一声轻响.mp3',
    '屋里的线条开始弯曲纸张奔向半空.mp3',
    '桌沿从指尖滑走电脑留在原地.mp3',
    '一阵天旋地转 我想起比赛截止日期️03s.mp3',
    '砰 我摔进草丛腰被石头硌得生疼️03s.mp3',
    '我撑着膝盖坐起来 山是真的风是真的️03.mp3',
    '手机黑着屏口袋里也没有充电线.mp3',
    '头顶有人掠过 我刚想喊救命却看见他们站在.mp3',
    '两位这是什么地方️03s我挥手挥得像个人.mp3',
    '风里送来一句青岚原️03s.mp3',
    '我低头 脚边一块断碑刚好亮了️05s.mp3',
    '碑上没有字只有细线和六角纹️03s.mp3',
    '光沿连接往外走断开的地方就停住.mp3',
    '我还没看懂云层里已经亮起五道光️03s.mp3',
    '每枚纹都不一样 有的厚重有的轻灵有的安静.mp3',
    '我伸出手又停住️05s.mp3',
    '远处山路还长 我深吸一口气比赛先欠着吧.mp3',
]

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def write_json(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    sources = [DOWNLOADS / name for name in NAMES]
    missing = [str(p) for p in sources if not p.is_file()]
    if len(sources) != 24 or len(set(sources)) != 24 or missing:
        raise RuntimeError('配音文件缺失或重复：' + str(missing))
    script = ROOT / 'Assets/天帝/脚本/核心/天帝序章.cs'
    text_block = re.search(r'叙述 = \{([\s\S]*?)\n    \};', script.read_text(encoding='utf-8')).group(1)
    lines = re.findall(r'"([^"]*)"', text_block)
    if len(lines) != 24:
        raise RuntimeError('序章台词数量变化，请重新核对映射')
    out = ROOT / 'output/用户序章配音' / datetime.now().strftime('%Y%m%d-%H%M%S')
    out.mkdir(parents=True, exist_ok=False)
    stage = out / '转换后'
    stage.mkdir()
    audio_root = ROOT / 'Assets/天帝/Resources/天帝音频'
    baseline_paths = [*ROOT.glob('Assets/天帝/场景/*.unity'), ROOT / '游戏数值配置.md', script,
        Path('C:/Users/123/AppData/LocalLow/TiandiStudio/从参加聚光灯比赛到我为天帝镇压世间一切/天帝进度.json'),
        *audio_root.glob('音乐/*'), *audio_root.glob('音效/*'), *audio_root.glob('配音/*.meta')]
    write_json(out / '保护文件指纹.json', {str(p): digest(p) for p in baseline_paths if p.is_file()})
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    report = []
    for i, source in enumerate(sources, 1):
        name = f'VO{i:02d}'
        raw = stage / (name + '-raw.wav')
        subprocess.run([ffmpeg, '-v', 'error', '-y', '-i', str(source), '-ar', '48000', '-ac', '1',
            '-c:a', 'pcm_f32le', str(raw)], check=True, capture_output=True)
        rate, original = wavfile.read(raw)
        seconds = len(original) / rate
        if not np.isfinite(original).all() or seconds < 2 or np.max(np.abs(original)) < .001:
            raise RuntimeError(name + '不是有效完整语音片段')
        peak = float(np.max(np.abs(original)))
        rms = float(np.sqrt(np.mean(original * original)))
        active = np.flatnonzero(np.abs(original) > .01)
        ending_silence = (len(original) - active[-1] - 1) / rate if len(active) else seconds
        target = stage / (name + '.wav')
        subprocess.run([ffmpeg, '-v', 'error', '-y', '-i', str(source),
            '-af', 'loudnorm=I=-18:TP=-3:LRA=7,afade=t=in:d=0.005,afade=t=out:st=' + str(max(0, seconds-.005)) + ':d=0.005',
            '-ar', '48000', '-ac', '1', '-c:a', 'pcm_s16le', str(target)], check=True, capture_output=True)
        new_rate, converted = wavfile.read(target)
        if new_rate != 48000 or converted.dtype != np.int16 or abs(len(converted) / new_rate - seconds) > .02:
            raise RuntimeError(name + '转换改变时长或格式')
        raw.unlink()
        report.append({'编号': name, '原文件': str(source), '原SHA256': digest(source), '对应字幕': lines[i-1],
            '语音秒': round(seconds, 4), '画面秒': round(max(6.5, seconds+.8), 4), '原峰值': round(peak, 5),
            '原RMS': round(rms, 5), '尾部低音量秒': round(ending_silence, 3), '转换文件': str(target),
            '成品SHA256': digest(target), '目标资源': str(audio_root / '配音' / target.name)})
    manifest = {'文件齐全': True, '段数': len(report), '映射依据': '用户提供文件名对应现有24格台词',
        '台词逐字听校': '未执行人工逐字听校；文件名映射与解码、波形、时长检查通过',
        '已安装': False, '配音总秒': round(sum(p['语音秒'] for p in report), 4),
        '画面总秒': round(sum(p['画面秒'] for p in report), 4), '片段': report}
    write_json(out / '接入清单.json', manifest)
    if args.install:
        backup = out / '旧配音'
        originals = out / '用户原始MP3'
        backup.mkdir()
        originals.mkdir()
        # 所有片段转换通过之后才安装，先备份全部旧资源与GUID元数据。
        for entry in report:
            destination = Path(entry['目标资源'])
            for p in [destination, Path(str(destination) + '.meta')]:
                if p.is_file():
                    shutil.copy2(p, backup / p.name)
            shutil.copy2(entry['原文件'], originals / (entry['编号'] + '.mp3'))
        changed = []
        try:
            for entry in report:
                destination = Path(entry['目标资源'])
                temporary = destination.with_suffix('.importing')
                shutil.copyfile(entry['转换文件'], temporary)
                temporary.replace(destination)
                changed.append(destination)
                if digest(destination) != entry['成品SHA256']:
                    raise RuntimeError('安装指纹不匹配：' + entry['编号'])
        except Exception:
            for destination in changed:
                shutil.copy2(backup / destination.name, destination)
            raise
        manifest['已安装'] = True
        write_json(out / '接入清单.json', manifest)
    print(json.dumps({'目录': str(out), '齐全': len(report), '安装': manifest['已安装'],
        '配音总秒': manifest['配音总秒'], '画面总秒': manifest['画面总秒'],
        '单段最短秒': min(p['语音秒'] for p in report), '单段最长秒': max(p['语音秒'] for p in report),
        '低于2秒': [p['编号'] for p in report if p['语音秒'] < 2]}, ensure_ascii=False))

if __name__ == '__main__':
    main()
