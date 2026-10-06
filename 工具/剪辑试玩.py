from pathlib import Path
import argparse, json, subprocess

ROOT = Path(__file__).resolve().parents[1]
FFMPEG = ROOT / '生成/录制工具/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'

def run(args):
    subprocess.run([str(FFMPEG), '-hide_banner', '-loglevel', 'error', '-y', *map(str, args)], check=True)

def main(folder, seconds=60):
    folder = Path(folder)
    report = json.loads((folder / '录制报告.json').read_text(encoding='utf-8'))
    assert not report['错误'] and report['王已击败'] and report['玩家存活']
    if seconds == 30:
        assert report['实际改造'] and report['通货拾取数'] > 0
    else:
        assert report['升阶成功'] and report['加词成功'] and report['开口成功']
    assert report['连锁数'] > 0 and report['拾取数'] > 0
    # 全部来源于同一遍实际GameView录制；只调整片段播放速度，顺序不变。
    lengths = [1.2, 1.5, .8, 4, 1, 7, .7, 4, 4, .8, 4, 1] if seconds == 30 else [3, 3, 1.5, 10, 3, 18, 2.5, 5, 4, 2, 7, 1]
    assert sum(lengths) == seconds and len(report['片段']) == len(lengths)
    edit = folder / '剪辑'; edit.mkdir(exist_ok=True)
    clips = []; timeline = []; now = 0
    for i, (part, length) in enumerate(zip(report['片段'], lengths)):
        start, end = part['开始帧'] / 30, part['结束帧'] / 30
        raw_length = end - start
        target = edit / f'{i:02}.mp4'
        run(['-ss', start, '-t', raw_length, '-i', folder / '原始试玩.mp4', '-an',
             '-vf', f'setpts=(PTS-STARTPTS)*{length/raw_length:.9f},fps=30,tpad=stop_mode=clone:stop_duration=0.2',
             '-frames:v', int(length*30), '-c:v', 'libx264', '-crf', 18, '-preset', 'fast', '-pix_fmt', 'yuv420p', target])
        clips.append(target)
        timeline.append({'阶段': part['名'], '成片开始秒': now, '成片结束秒': now+length,
                         '原始开始秒': start, '原始结束秒': end, '播放速度': raw_length/length})
        now += length
    # 文件名只包含程序生成的数字，concat清单无需拼入用户文本。
    (edit / 'concat.txt').write_text('\n'.join(f"file '{p.name}'" for p in clips), encoding='utf-8')
    prefix = f'天帝_新版美术与通货_{seconds}秒试玩' if seconds == 30 else '天帝_闪电连锁_60秒试玩'
    out = folder.parent / (prefix + '.mp4')
    run(['-f', 'concat', '-safe', 0, '-i', edit / 'concat.txt', '-stream_loop', -1, '-i', ROOT / 'Assets/天帝/音频/music-0.wav',
         '-map', '0:v', '-map', '1:a', '-c:v', 'copy', '-af', f'volume=0.32,afade=t=in:d=0.7,afade=t=out:st={seconds-2}:d=2',
         '-c:a', 'aac', '-b:a', '192k', '-t', seconds, '-movflags', '+faststart', out])
    run(['-ss', 12 if seconds == 30 else 28.5, '-i', out, '-frames:v', 1, folder.parent / (prefix + '_封面.jpg')])
    (folder.parent / (prefix + '_时间线.json')).write_text(json.dumps(timeline, ensure_ascii=False, indent=2), encoding='utf-8')
    # 完整解码验收；FFmpeg progress给出实际1800帧与60秒，不以文件名当作时长证据。
    check = subprocess.run([str(FFMPEG), '-hide_banner', '-v', 'error', '-i', str(out), '-progress', 'pipe:1', '-f', 'null', '-'],
                           check=True, capture_output=True, text=True)
    fields = dict(line.split('=', 1) for line in check.stdout.splitlines() if '=' in line)
    assert int(fields['frame']) == seconds * 30, fields
    assert abs(int(fields['out_time_us'])/1_000_000 - seconds) < .05, fields
    (folder.parent / (prefix + '_验收.json')).write_text(json.dumps({'帧数': seconds * 30, '秒数': seconds, '帧率': 30,
        '宽': report['宽'], '高': report['高'], '完整解码通过': True, '录制目录': str(folder), '游戏报告': report},
        ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'视频': str(out), '秒数': seconds, '帧数': seconds*30}, ensure_ascii=False))

if __name__ == '__main__':
    parser = argparse.ArgumentParser(); parser.add_argument('folder'); parser.add_argument('--seconds', type=int, choices=[30, 60], default=60)
    args = parser.parse_args(); main(args.folder, args.seconds)
