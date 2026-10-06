"""逐帧调用官方Maker edit_image，保存检查点；不自动重试已派发请求。"""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
import argparse
import hashlib
import json
import threading
import time
from Tap素材代理 import 制造代理, 制造项目, 旧素材项目

根=Path(__file__).resolve().parents[1]
交付=根/'output/imagegen/主角十组动画'


def main():
    p=argparse.ArgumentParser()
    p.add_argument('--group',default='移动_南')
    p.add_argument('--workers',type=int,default=4)
    p.add_argument('--resume-after-balance',action='store_true',help='仅在积分或项目入口问题已解决后使用；保留失败记录并检查Maker资源映射。')
    a=p.parse_args()
    档=json.loads((交付/'生成清单.json').read_text(encoding='utf-8'))
    待做=[f for f in 档['frames'] if not f['master'] and (a.group=='all' or f['name'].startswith(a.group+'_'))]
    本轮=交付/'逐帧生成结果';本轮.mkdir(exist_ok=True)
    线程本地=threading.local();代理们=[];锁=threading.Lock();停止=threading.Event()
    def 一个(项):
        名=项['name'];输入=交付/'请求'/f'底稿_{名}.json';结果文件=本轮/f'{名}.json'
        if 停止.is_set():return {'name':名,'status':'not_dispatched'}
        参数=json.loads(输入.read_text(encoding='utf-8'))
        指纹=hashlib.sha256(输入.read_bytes()).hexdigest()
        if 结果文件.exists():
            已有=json.loads(结果文件.read_text(encoding='utf-8'))
            if 已有.get('request_sha256')==指纹 and 已有.get('result',{}).get('structuredContent',{}).get('success'):
                return {'name':名,'status':'existing'}
            # 已知失败保留，不循环扣费；输入变化是一次明确的新版本请求。
            if 已有.get('request_sha256')==指纹:
                内容=json.dumps(已有.get('result',{}),ensure_ascii=False)
                if not a.resume_after_balance or 'INSUFFICIENT_BALANCE' not in 内容:
                    return {'name':名,'status':'previous_failure','path':str(结果文件)}
                已存路径=[]
                for 素材项目 in (制造项目, 旧素材项目):
                    映射路径=素材项目/'.maker/assets/generated-assets.json'
                    映射=json.loads(映射路径.read_text(encoding='utf-8-sig')) if 映射路径.exists() else {}
                    已存路径.extend(str(素材项目/k) for k in 映射
                                    if 参数['name']+'_' in k and (素材项目/k).exists())
                if 已存路径:
                    return {'name':名,'status':'needs_reconcile','existing_assets':已存路径}
                历史=本轮/'历史';历史.mkdir(exist_ok=True)
                (历史/f'{名}_{time.time_ns()}.json').write_bytes(结果文件.read_bytes())
        if not hasattr(线程本地,'代理'):
            线程本地.代理=制造代理()
            with 锁:代理们.append(线程本地.代理)
        参数['target_dir'] = str(制造项目)
        记录={'name':名,'request_sha256':指纹,'started_at_utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),
              'request_file':str(输入),'effective_target_dir':str(制造项目),'status':'dispatched_unknown'}
        结果文件.write_text(json.dumps(记录,ensure_ascii=False,indent=2),encoding='utf-8')
        try:
            结果=线程本地.代理.请求('tools/call',{'name':'edit_image','arguments':参数},1200)
            记录.update({'result':结果,'status':'returned'})
            结果文件.write_text(json.dumps(记录,ensure_ascii=False,indent=2),encoding='utf-8')
            结构=结果.get('structuredContent',{})
            if not 结构:
                for b in 结果.get('content',[]):
                    if b.get('type')=='text':
                        try:结构=json.loads(b['text'])
                        except json.JSONDecodeError:pass
                        if isinstance(结构,dict):break
            成功=结构.get('success',False) and not 结果.get('isError',False)
            错误文本='\n'.join(b.get('text','') for b in 结果.get('content',[]) if b.get('type')=='text') if not 成功 else ''
            if 'INSUFFICIENT_BALANCE' in 错误文本 or 'execution_state=unknown' in 错误文本 or 结果.get('isError'):
                停止.set()
            return {'name':名,'status':'success' if 成功 else 'failed','absolutePath':结构.get('absolutePath'),'error':结构.get('error') or 错误文本[:2000]}
        except Exception as e:
            停止.set()
            记录['client_error']=str(e)
            结果文件.write_text(json.dumps(记录,ensure_ascii=False,indent=2),encoding='utf-8')
            return {'name':名,'status':'unknown','path':str(结果文件)}
    汇总=[]
    try:
        with ThreadPoolExecutor(max_workers=max(1,min(4,a.workers))) as 池:
            任务={池.submit(一个,项):项 for 项 in 待做}
            for 完成 in as_completed(任务):
                结果=完成.result();汇总.append(结果)
                print(json.dumps({'progress':len(汇总),'total':len(待做),**结果},ensure_ascii=False),flush=True)
                (本轮/f'进度_{a.group}.json').write_text(json.dumps(汇总,ensure_ascii=False,indent=2),encoding='utf-8')
        # 独立生产记录放在Maker资产工作流内，远程结果/路径保留，未构建提交Maker游戏。
        记录目录=制造项目/'assets/image/天帝主角动画_20261005/生成记录';记录目录.mkdir(exist_ok=True)
        for 项 in 待做:
            文件=本轮/f'{项["name"]}.json'
            if 文件.exists():
                (记录目录/文件.name).write_bytes(文件.read_bytes())
    finally:
        for 代理 in 代理们:代理.关闭()


if __name__=='__main__':main()
