from pathlib import Path
import argparse, json, time, uuid

ROOT = Path(__file__).resolve().parents[1]

def issue(method, timeout=0):
    request = ROOT / 'Temp' / '天帝请求.json'
    if request.exists():
        raise RuntimeError('编辑器已有待处理请求，不能覆盖。')
    task_id = uuid.uuid4().hex[:12]
    result = ROOT / '生成' / '验证' / f'response-{task_id}.json'
    request.parent.mkdir(exist_ok=True)
    request.write_text(json.dumps({'编号': task_id, '方法': method, '参数': ''}, ensure_ascii=False), encoding='utf-8')
    deadline = time.monotonic() + min(timeout, 55)
    while time.monotonic() < deadline:
        if result.exists():
            data = json.loads(result.read_text(encoding='utf-8'))
            print(json.dumps(data, ensure_ascii=False))
            return data
        time.sleep(.5)
    print(json.dumps({'任务': task_id, '状态': '已发送，请检查结果文件', '结果文件': str(result)}, ensure_ascii=False))
    return {'taskId': task_id}

def ready():
    source = max(path.stat().st_mtime for path in (ROOT / 'Assets' / '天帝').rglob('*.cs'))
    # Runtime和Editor分别比较源文件时间，避免把刷新前的旧程序集当作最终结果。
    deadline = time.monotonic() + 45
    while time.monotonic() < deadline:
        runtime_source = max(path.stat().st_mtime for path in (ROOT / 'Assets' / '天帝' / '脚本').rglob('*.cs'))
        editor_source = max(path.stat().st_mtime for path in (ROOT / 'Assets' / '天帝' / 'Editor').rglob('*.cs'))
        runtime = ROOT / 'Library' / 'ScriptAssemblies' / 'Assembly-CSharp.dll'
        editor = ROOT / 'Library' / 'ScriptAssemblies' / 'Assembly-CSharp-Editor.dll'
        if runtime.exists() and editor.exists() and runtime.stat().st_mtime >= runtime_source and editor.stat().st_mtime >= editor_source:
            return
        time.sleep(.5)
    raise RuntimeError('最终源码尚未编译完成，请检查Unity Console。')

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('method')
    parser.add_argument('--wait', type=float, default=0)
    args = parser.parse_args()
    if args.method not in ('刷新', '停止', '状态'):
        ready()
    issue(args.method, args.wait)
