from pathlib import Path
import hashlib, json, shutil
root = Path(__file__).resolve().parents[1]
out = root / 'output/音频接入'
out.mkdir(parents=True, exist_ok=True)
paths = [*root.glob('Assets/天帝/场景/*.unity'), root / '游戏数值配置.md', Path('C:/Users/123/AppData/LocalLow/TiandiStudio/从参加聚光灯比赛到我为天帝镇压世间一切/天帝进度.json')]
baseline = out / '修改前指纹.json'
current = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths if p.exists()}
if not baseline.exists():
    baseline.write_text(json.dumps(current, ensure_ascii=False, indent=2), encoding='utf-8')
    for p in root.glob('Assets/天帝/脚本/**/*.cs'):
        target = out / '修改前' / p.relative_to(root)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(p, target)
    print('baseline created')
else:
    expected = json.loads(baseline.read_text(encoding='utf-8'))
    print(json.dumps({'保护文件数':len(expected), '变化':[p for p,v in expected.items() if current.get(p)!=v]},ensure_ascii=False))
