"""有界生成正面走路返工帧；保留请求与未知状态，不自动重试。"""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
import argparse
import json
import hashlib
import threading
from Tap素材代理 import 制造代理

根=Path(__file__).resolve().parents[1]
输出=根/'output/imagegen/主角十组动画/正面走路返工_v3'


def main():
    p=argparse.ArgumentParser()
    p.add_argument('--frames',default='1,7,13,19')
    p.add_argument('--workers',type=int,default=4)
    a=p.parse_args();ids=range(1,25) if a.frames=='all' else [int(v) for v in a.frames.split(',')]
    stop=threading.Event()
    def one(i):
        name=f'移动_南_{i:02}';q=输出/'请求'/f'{name}.json';dest=输出/'原始返回'/f'{name}.json'
        if stop.is_set():return {'name':name,'state':'not_dispatched'}
        fingerprint=hashlib.sha256(q.read_bytes()).hexdigest()
        if dest.exists():
            old=json.loads(dest.read_text(encoding='utf-8'))
            if old.get('request_sha256')==fingerprint:return {'name':name,'state':'saved','success':old.get('result',{}).get('structuredContent',{}).get('success',False)}
            return {'name':name,'state':'request_changed_requires_new_record'}
        args=json.loads(q.read_text(encoding='utf-8'))
        record={'request_file':str(q),'request_sha256':fingerprint,'status':'dispatched_unknown'}
        dest.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
        proxy=None
        try:
            proxy=制造代理()
            r=proxy.请求('tools/call',{'name':'edit_image','arguments':args},1200)
            record.update({'status':'returned','result':r})
            dest.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
            s=r.get('structuredContent',{})
            ok=s.get('success',False) and not r.get('isError')
            if not ok:stop.set()
            return {'name':name,'success':ok,'absolutePath':s.get('absolutePath'),'state':'returned' if ok else 'stopped_error'}
        except Exception as e:
            stop.set();record['error']=str(e)
            dest.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
            return {'name':name,'state':'unknown','error':str(e)}
        finally:
            if proxy:proxy.关闭()
    with ThreadPoolExecutor(max_workers=max(1,min(a.workers,4))) as pool:
        pending=[pool.submit(one,i) for i in ids]
        for f in as_completed(pending):print(json.dumps(f.result(),ensure_ascii=False),flush=True)


if __name__=='__main__':main()
