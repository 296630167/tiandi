"""通过本机已有的 TapTap Maker MCP 运行时调用绑定项目，保留其资源工作流。

不读取或使用 OpenAI API 配置；身份验证、远程下载及资源映射交给 Maker。
"""
from pathlib import Path
import argparse
import json
import os
import queue
import subprocess
import threading
import time
import tomllib

根 = Path(__file__).resolve().parents[1]
制造项目 = Path(r'C:\Users\123\Documents\taptap製造_一滴水的故事')
制造项目ID = '9a4dd81a-f6cc-4948-b3b9-47df257e033c'
旧素材项目 = Path(r'C:\Users\123\Documents\taptap製造')


class 制造代理:
    def __init__(self):
        绑定路径 = 制造项目 / '.maker-mcp/config.json'
        绑定 = json.loads(绑定路径.read_text(encoding='utf-8-sig'))
        if 绑定.get('project_id') != 制造项目ID:
            raise RuntimeError('素材目录未绑定指定的“一滴水的故事”项目；停止调用，避免使用错误积分入口。')
        配置 = tomllib.loads((Path.home() / '.codex/config.toml').read_text(encoding='utf-8-sig'))
        服务 = 配置['mcp_servers']['taptap-maker']
        环境 = os.environ.copy()
        环境.update(服务.get('env', {}))
        环境['TAPTAP_MCP_WORKSPACE_ROOT'] = str(制造项目)
        self.进程 = subprocess.Popen(
            [服务['command'], *服务.get('args', [])], cwd=制造项目, env=环境,
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
            text=True, encoding='utf-8', creationflags=subprocess.CREATE_NO_WINDOW,
        )
        self.队列 = queue.Queue()
        self.编号 = 0
        threading.Thread(target=self._收取, daemon=True).start()
        self.请求('initialize', {'protocolVersion': '2024-11-05', 'capabilities': {},
                  'clientInfo': {'name': 'tiandi-asset-client', 'version': '1.0'}}, 60)
        self._发送({'jsonrpc': '2.0', 'method': 'notifications/initialized'})

    def _收取(self):
        for 行 in self.进程.stdout:
            try:
                self.队列.put(json.loads(行))
            except json.JSONDecodeError:
                pass
        self.队列.put({'closed': True})

    def _发送(self, 消息):
        self.进程.stdin.write(json.dumps(消息, ensure_ascii=False) + '\n')
        self.进程.stdin.flush()

    def 请求(self, 方法, 参数, 超时=300):
        if 方法 == 'tools/call':
            目标 = 参数.get('arguments', {}).get('target_dir')
            if 目标 and Path(目标).resolve() != 制造项目.resolve():
                raise ValueError('Maker请求target_dir与已核验项目不同；请修正请求目录后再调用。')
        self.编号 += 1
        编号 = self.编号
        self._发送({'jsonrpc': '2.0', 'id': 编号, 'method': 方法, 'params': 参数})
        期限 = time.monotonic() + 超时
        while time.monotonic() < 期限:
            try:
                消息 = self.队列.get(timeout=max(.01, 期限 - time.monotonic()))
            except queue.Empty:
                break
            if 消息.get('closed'):
                raise RuntimeError('Tap Maker MCP 运行时已退出；未改用其他生图入口。')
            if 消息.get('id') == 编号:
                if 'error' in 消息:
                    raise RuntimeError('Maker MCP 请求失败：' + str(消息['error'].get('code')))
                return 消息.get('result', {})
            if 'id' in 消息 and 'method' in 消息:
                self._发送({'jsonrpc': '2.0', 'id': 消息['id'],
                           'error': {'code': -32601, 'message': 'Client method unavailable'}})
        raise TimeoutError('Tap Maker MCP 请求超时；请检查远程连接。')

    def 关闭(self):
        if self.进程.poll() is None:
            self.进程.terminate()
            try:
                self.进程.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self.进程.kill()


def main():
    命令 = argparse.ArgumentParser()
    命令.add_argument('action', choices=['list', 'call'])
    命令.add_argument('--tool')
    命令.add_argument('--args-file', type=Path)
    命令.add_argument('--out', type=Path, required=True)
    命令.add_argument('--timeout', type=float, default=300)
    参数 = 命令.parse_args()
    代理 = 制造代理()
    try:
        if 参数.action == 'list':
            结果 = 代理.请求('tools/list', {}, 60)
            参数.out.parent.mkdir(parents=True, exist_ok=True)
            参数.out.write_text(json.dumps(结果, ensure_ascii=False, indent=2), encoding='utf-8')
            for 工具 in 结果.get('tools', []):
                print(json.dumps({'name': 工具['name'], 'description': 工具.get('description', '')[:240]}, ensure_ascii=False))
        else:
            if not 参数.tool or not 参数.args_file:
                命令.error('call 必须提供 --tool 与 --args-file')
            输入 = json.loads(参数.args_file.read_text(encoding='utf-8-sig'))
            结果 = 代理.请求('tools/call', {'name': 参数.tool, 'arguments': 输入}, 参数.timeout)
            参数.out.parent.mkdir(parents=True, exist_ok=True)
            参数.out.write_text(json.dumps(结果, ensure_ascii=False, indent=2), encoding='utf-8')
            print(json.dumps({'saved': str(参数.out), 'isError': 结果.get('isError', False),
                              'contentTypes': [项.get('type') for 项 in 结果.get('content', [])]}, ensure_ascii=False))
    finally:
        代理.关闭()


if __name__ == '__main__':
    main()
