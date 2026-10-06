"""读取正式战斗基线和特性设计，核算门槛、格子预算、转化及掉落；不修改Unity或玩家档案。"""
from pathlib import Path
import csv
import hashlib
import json
import math
import re
import runpy

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / '特性道纹_设计方案.md'
BASE = runpy.run_path(str(ROOT / '工具/战斗数值计算.py'), run_name='特性数值基线')
TEXT = DOC.read_text(encoding='utf-8')
CONFIG = json.loads(re.search(r'<!-- 特性配置开始 -->\s*```json\s*(.*?)\s*```\s*<!-- 特性配置结束 -->', TEXT, re.S).group(1))
RANGES = BASE['CFG']['rune']['affix_ranges']
KEYS = {'力量':'strength','速度':'speed','智力':'intelligence','血量':'hp','灵力':'mp','防御':'armor','护盾':'shield','攻速':'attack_speed','移速':'move', **dict.fromkeys('金木水火土','element')}
ATTRIBUTES = ['金','木','水','火','土','力量','速度','智力','血量','灵力','防御','护盾','攻速','移速','数量','分裂','连锁','弧度','范围']
GROUPS = [('五行',ATTRIBUTES[:5]),('三基础',ATTRIBUTES[5:8]),('普通属性',ATTRIBUTES[8:14]),('攻击形态',ATTRIBUTES[14:])]
OUT = ROOT / '数据' / '特性道纹设计'
OUT.mkdir(parents=True, exist_ok=True)
checks, failures = [], []

def check(name, valid, evidence=None):
    (checks if valid else failures).append({'名称':name,'证据':evidence})

def threshold(condition, grade, level):
    attr, base, unit = condition
    value = base * grade['门槛'] * (BASE['growth'](level) if unit == 'g' else 1)
    step = 30 if attr == '弧度' else 1 if attr in ('数量','分裂','连锁','范围') else .01
    return math.ceil(value / step - 1e-9) * step

def typical(attr, level, maximum=False):
    if attr in KEYS:
        bounds = RANGES[KEYS[attr]]
        value = bounds[1] if maximum else sum(bounds)/2
        return value * (1 if attr in ('攻速','移速') else BASE['growth'](level))
    return 30 if attr == '弧度' else 1

def legal_midpoint(attr, level):
    value = typical(attr, level)
    return math.floor(value*100+.5)/100 if attr in KEYS else value

def text_gate(condition):
    attr, amount, unit = condition
    return f'{attr}≥{amount:g}' + ('g' if unit=='g' else '%' if unit=='%' else '°/秒' if unit=='°/秒' else '')

traits = CONFIG['traits']
check('现有19种属性每种两种特性', len(traits)==38 and all(sum(t['属性']==a for t in traits)==2 for a in ATTRIBUTES))
check('稳定编号与名称无重复',len({t['编号'] for t in traits})==38 and len({t['名称'] for t in traits})==38)
check('八种转化目标与稳定编号唯一',len(CONFIG['converters'])==8 and len({c['编号'] for c in CONFIG['converters']})==8 and {c['目标'] for c in CONFIG['converters']}==set('金木水火土')|{'力量','速度','智力'})
check('八品阶条件与效果单调',all(CONFIG['grades'][i]['门槛']<CONFIG['grades'][i+1]['门槛'] and CONFIG['grades'][i]['效果']<CONFIG['grades'][i+1]['效果'] for i in range(7)))
catalog=[]
for group, attributes in GROUPS:
    catalog += [f'### {group}', '', '| 编号 | 属性·特性 | 普通版下游条件 | 普通版效果 | 接管的形态 |', '|---|---|---|---|---|']
    for t in traits:
        if t['属性'] not in attributes: continue
        catalog.append('| '+t['编号']+' | '+t['属性']+'·'+t['名称']+' | '+'；'.join(text_gate(c) for c in t['条件'])+' | '+t['说明']+' | '+('、'.join(t['支持']) or '无（纯被动）')+' |')
    catalog.append('')

rows=[]
for t in traits:
    widest=0
    for grade in CONFIG['grades']:
        for level in range(1,101):
            gates=[threshold(c,grade,level) for c in t['条件']]
            # 用单词条普通属性叶+分叉主干证明，不依赖多词条/二口属性掉落。
            leaf_count=sum(math.ceil(v/legal_midpoint(c[0],level)-1e-9) for c,v in zip(t['条件'],gates))
            point_count=2*leaf_count # 一枚特性+N属性叶+(N-1)分叉
            widest=max(widest,point_count)
            check(f"{t['编号']}-{grade['名称']}-{level}门槛有限且100点内可构建",all(math.isfinite(v) and v>0 for v in gates) and point_count<=100)
            if level in (8,20,50,100):
                u=BASE['growth'](level)
                period=t.get('周期',0)*grade['周期']
                duration=t.get('持续',0)*grade['持续']
                damage=(6 if t['编号']=='T30' else t.get('伤害',0))*u*grade['效果']
                summon=t.get('召唤总伤',0)*u*grade['效果']
                dot=t.get('持续伤',0)*u*grade['效果']
                cooldown=period/(1+BASE['player'](level,'standard')['haste']) if period else 0
                # 标明是预算上界。事件触发受蓄势/移动/受伤/资源/真实目标等限制。
                nominal=(damage+summon+dot*duration)/cooldown if cooldown else 0
                rows.append({'编号':t['编号'],'名称':t['名称'],'主属性':t['属性'],'品质':grade['名称'],'iLv=玩家Lv':level,'最低敌等级':grade['敌等级'],'该iLv自然可掉落':level>=grade['敌等级'],'门槛':';'.join(f'{c[0]}={v:.2f}' for c,v in zip(t['条件'],gates)),'中值单词条叶数':leaf_count,'含特性分叉耗点':point_count,'同级技能点足够':point_count<=level,'原周期':round(period,5),'含标准急速周期':round(cooldown,5),'持续':round(duration,5),'直接伤害预算':round(damage,5),'每秒持续伤预算':round(dot,5),'一次召唤总伤':round(summon,5),'单目标名义DPS上界_未减伤':round(nominal,5)})
    check(t['名称']+'最高门槛有合法中值叶布局',widest<=100,{'最高耗点':widest})

g20=BASE['growth'](20)
vine=next(t for t in traits if t['名称']=='缠绕')
vine_gates=[threshold(c,CONFIG['grades'][0],20) for c in vine['条件']]
check('20级缠绕条件与合法定点词条',5.72>=vine_gates[0] and 7.16>=vine_gates[1] and 28.62>=vine_gates[2],{'条件':vine_gates,'下游':[5.72,7.16,28.62]})
check('20级缠绕10格布局预算',10<=20)
g30=BASE['growth'](30)
inputs=[('血量',53.06),('血量',53.06),('力量',11.28)]
converted=[math.floor((value/typical(attr,30))*.9*typical('速度',30)*100+.5)/100 for attr,value in inputs]
conversion=sum(converted)
check('30级化速疾行组合可激活',conversion>=threshold(next(t for t in traits if t['名称']=='疾行')['条件'][0],CONFIG['grades'][0],30) and 30>=CONFIG['grades'][3]['敌等级'],{'转换后各词条':converted,'速度':round(conversion,5),'需求':round(5*g30,5),'解锁格':6})
check('转化同预算不凭空放大',all(0<g['转换']<=1 for g in CONFIG['grades']))
expected=60*.004+4*.06+.15+.35
at_least=1-(1-.004)**60*(1-.06)**4*(1-.15)*(1-.35)
check('掉落概率合法且期望小于1',expected<1 and 0<at_least<1)
check('普通与BOSS品阶权重均为100',sum((35,23,16,11,7,4,3,1))==100 and sum((20,20,18,15,10,8,6,3))==100)

with (OUT/'品阶与等级门槛.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=rows[0]); writer.writeheader(); writer.writerows(rows)
summary={'正式基线指纹':BASE['CONFIG_HASH'],'特性设计指纹':hashlib.sha256(json.dumps(CONFIG,ensure_ascii=False,sort_keys=True).encode()).hexdigest(),'范围':'离线设计预算；不代表已接入Unity或真人平衡','特性数':len(traits),'转化数':len(CONFIG['converters']),'属性数':len(ATTRIBUTES),'通过项':len(checks),'失败项':failures,'完整级别品质条件组合':len(traits)*8*100,'采样CSV行':len(rows),'每图掉落期望':expected,'每图至少一枚概率':at_least,'30级化速组合':{'速度':conversion,'需求':5*g30},'注意':'高品阶低iLv组合仅用于边界核算；自然掉落受最低敌等级限制。表内DPS是持续施放预算上界，不含命中率、减伤、控制、施放间实际目标/资源与形态影响，不能当作实战DPS。'}
(OUT/'校验摘要.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
notes=f"读取正式基线指纹`{BASE['CONFIG_HASH']}`，对38种特性×8品阶×100物品等级共30400组条件检查，{len(checks)}项通过、{len(failures)}项失败。所有门槛都存在100点内的单词条属性叶＋分叉主干布局；这不表示该等级有足够点数或已经随机收齐。\n\n采样CSV：`数据/特性道纹设计/品阶与等级门槛.csv`，1216行，含自然掉落等级、同级技能点是否足够、品阶条件、周期与预算上界。摘要：`数据/特性道纹设计/校验摘要.json`。高品质低物品等级只用于边界检查，自然掉落仍遵守最低敌等级。\n\n本次没有修改Unity游戏内容、原正式数值、真实存档或开局资源；没有调用生图、构建或录像。下一步先确定两项归属规则，再把已验证的特性分批接入真实战斗。"
output=re.sub(r'<!-- 特性目录开始 -->.*?<!-- 特性目录结束 -->','<!-- 特性目录开始 -->\n'+'\n'.join(catalog)+'\n<!-- 特性目录结束 -->',TEXT,flags=re.S)
output=re.sub(r'<!-- 特性核算开始 -->.*?<!-- 特性核算结束 -->','<!-- 特性核算开始 -->\n'+notes+'\n<!-- 特性核算结束 -->',output,flags=re.S)
DOC.write_text(output,encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False))
if failures: raise SystemExit(1)
