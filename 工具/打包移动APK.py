"""使用英文临时工程打包Android，避免Unity安卓工具链拒绝中文项目路径。"""
from pathlib import Path
import datetime
import json
import re
import shutil
import subprocess
import sys
import tempfile

PROJECT_ROOT = Path(__file__).resolve().parents[1]


def main():
    output = PROJECT_ROOT / '移动端打包'
    output.mkdir(exist_ok=True)
    version = re.search(r'm_EditorVersion: (.+)', (PROJECT_ROOT / 'ProjectSettings/ProjectVersion.txt').read_text()).group(1).strip()
    editors = list(Path('C:/Program Files/Unity/Hub/Editor').glob(version + '*/Editor/Unity.exe'))
    if not editors:
        raise RuntimeError(f'找不到Unity {version}。')
    if not str(Path(tempfile.gettempdir()).resolve()).isascii():
        raise RuntimeError('Android临时构建目录必须是纯英文路径。')
    clone = Path(tempfile.mkdtemp(prefix='TiandiAndroidBuild-')).resolve()
    stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
    log = output / f'安卓打包日志-{stamp}.log'
    print(f'正在准备临时打包工程：{clone}', flush=True)
    for name in ('Assets', 'Packages', 'ProjectSettings'):
        shutil.copytree(PROJECT_ROOT / name, clone / name)
    shutil.copy2(PROJECT_ROOT / '游戏数值配置.md', clone / '游戏数值配置.md')
    print(f'开始Android ARM64打包，日志：{log}', flush=True)
    result = subprocess.run([
        str(editors[0]), '-batchmode', '-nographics', '-buildTarget', 'Android',
        '-projectPath', str(clone), '-executeMethod', '天帝构建工具.构建移动APK',
        '-quit', '-logFile', str(log),
    ], creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == 'win32' else 0)
    apks = list((clone / 'MobileBuild').glob('*.apk'))
    if result.returncode != 0 or len(apks) != 1:
        raise RuntimeError(f'APK打包失败，请查看日志。临时工程保留于：{clone}')
    source = apks[0]
    report_source = source.with_suffix('.构建报告.json')
    report = json.loads(report_source.read_text(encoding='utf-8'))
    if report.get('结果') != 'Succeeded':
        raise RuntimeError(f'APK构建报告未确认成功，临时工程保留于：{clone}')
    target = output / f'天帝-移动试玩-{stamp}.apk'
    shutil.copy2(source, target)
    report['APK'] = str(target)
    report['APK字节数'] = target.stat().st_size
    target.with_suffix('.构建报告.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    for folder in source.parent.iterdir():
        if folder.is_dir() and ('_BackUpThisFolder_' in folder.name or folder.name.endswith('_BurstDebugInformation_DoNotShip')):
            backup = output / '调试备份' / stamp / folder.name
            shutil.copytree(folder, backup)
    temp_root = Path(tempfile.gettempdir()).resolve()
    if clone.parent == temp_root and clone.name.startswith('TiandiAndroidBuild-'):
        shutil.rmtree(clone)
    print(f'APK已生成：{target}', flush=True)


if __name__ == '__main__':
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8')
    main()
