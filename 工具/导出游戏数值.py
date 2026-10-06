"""只从权威配置生成Unity参数、四敌曲线及对照输入，不操作场景或存档。"""
from pathlib import Path
import hashlib
import json
import re
import runpy

ROOT = Path(__file__).resolve().parents[1]
model = runpy.run_path(str(ROOT / '工具' / '战斗数值计算.py'), run_name='数值导出模块')
cfg = model['CFG']
block = re.search(r'<!-- 数值配置开始 -->\s*```json\s*(.*?)\s*```\s*<!-- 数值配置结束 -->', (ROOT / '游戏数值配置.md').read_text(encoding='utf-8'), re.S).group(1).strip()
block_hash = hashlib.sha256(block.encode()).hexdigest()

def flatten(value, prefix=''):
    if isinstance(value, dict):
        for key, child in value.items():
            yield from flatten(child, f'{prefix}.{key}' if prefix else key)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from flatten(child, f'{prefix}.{index}')
    elif isinstance(value, (int, float)):
        yield prefix, int(value) if isinstance(value, bool) else value

lines = ['// 自动生成自 游戏数值配置.md；修改基准后重新导出，禁止手工改这里。', 'using System.Collections.Generic;',
         'public static class 天帝数值配置', '{', f'    public const string 版本 = "{cfg["version"]}";',
         f'    public const string 配置指纹 = "{model["CONFIG_HASH"]}";', f'    public const string 原文指纹 = "{block_hash}";',
         '    static readonly Dictionary<string, double> 参数 = new Dictionary<string, double>', '    {']
for key, value in flatten({k: v for k, v in cfg.items() if k not in ('simulation', 'recommendation')}):
    lines.append(f'        {{"{key}", {repr(value)}d}},')
def strings(value, prefix=''):
    if isinstance(value, dict):
        for key, child in value.items():
            yield from strings(child, f'{prefix}.{key}' if prefix else key)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from strings(child, f'{prefix}.{index}')
    elif isinstance(value, str):
        yield prefix, value

lines += ['    };', '    static readonly Dictionary<string, string> 文本 = new Dictionary<string, string>', '    {']
for key, value in strings({k: cfg[k] for k in ('battle_content', 'trait_runes') if k in cfg}):
    lines.append('        {' + json.dumps(key, ensure_ascii=False) + ', ' + json.dumps(value, ensure_ascii=False) + '},')
lines += ['    };', '    public static string 取文本(string 路径) => 文本[路径];', '    public static double 取(string 路径) => 参数[路径];',
          '    // 类型顺序：普通、精英、头目、BOSS；列：生命、攻击、防御、抗性、移速、同档标准玩家防御、同档标准玩家抗性。',
          '    static readonly double[,] 敌表 = new double[,]', '    {']
enemy_rows = []
for kind in model['KINDS']:
    for level in range(1, cfg['levels']['map_max'] + 1):
        e = model['enemy'](level, kind)
        enemy_rows.append(e)
        reference = model['player'](level, 'standard')
        values = [e[k] for k in ('hp', 'attack', 'armor', 'resist', 'move')] + [reference['armor'], reference['resist']]
        lines.append('        {' + ', '.join(repr(v) + 'd' for v in values) + '},')
lines += ['    };', '    public static double 敌人(int 地图等级, int 类别, int 列) => 敌表[类别 * (int)取("levels.map_max") + 地图等级 - 1, 列];', '}']
output = ROOT / 'Assets' / '天帝' / '脚本' / '核心' / '天帝数值配置.cs'
content = '\n'.join(lines) + '\n'
if not output.exists() or output.read_text(encoding='utf-8') != content:
    output.write_text(content, encoding='utf-8')
players = [model['player'](level, profile) for profile in model['PROFILES'] for level in range(1, 101)]
talents = [dict(model['player'](level, 'standard', talent=talent, missing_hp=.5), talent=talent) for level in (1, 10, 50, 100)
           for talent in ('普通人', '万钧', '双生矢', '续雷', '广域', '灵海', '铁骨', '凝光', '逐风', '逆命', '余响', '穿越者', '天命之子')]
inputs = []
for p in players + talents:
    keys = {'strength':'力量', 'speed':'速度', 'intelligence':'智力', 'hp':'血量', 'mp':'灵力', 'armor':'防御', 'shield':'护盾',
            'element':'土', 'attack_speed':'攻速', 'move':'移速', 'quantity':'数量', 'chain':'连锁', 'split':'分裂', 'radius':'范围', 'arc':'弧度'}
    b = model['bonuses'](p['level'], p['profile'])
    inputs.append({'等级': p['level'], '构筑':p['profile'], '天赋':p.get('talent', '普通人'), '增益':[{'属性':keys[k], '值':v} for k,v in b.items()],
                   '力量':p['strength'], '速度':p['speed'], '智力':p['intelligence'], '血量':p['hp'], '灵力':p['mp'], '护盾':p['shield'],
                   '防御':p['armor'], '攻击':p['attack'], '攻速':p['aps'], '暴击':p['crit'], '暴伤':p['crit_damage'], '抗性':p['resist'],
                   '急速':p['haste'], '移速':p['move'], '跑速':p['run'], '闪避':p['evasion'], '元素':p['element'],
                   '数量':p['quantity'], '连锁':p['chain'], '分裂':p['split'], '范围':p['radius'], '弧度':p['arc'],
                   '经验':model['next_exp'](p['level']), '单体DPS':p['ds'], '清群DPS':p['d8'], '战斗力':p['cp']})
out = ROOT / '数据' / '战斗数值_v1' / 'Unity对照输入.json'
out.write_text(json.dumps({'配置指纹':model['CONFIG_HASH'], '玩家':inputs,
              '敌人':[{'地图等级':e['map_level'], '类型':e['kind'], '等级':e['level'], '生命':e['hp'], '攻击':e['attack'],
                        '防御':e['armor'], '抗性':e['resist'], '速度':e['move'], '间隔':e['period'], '目标秒':e['ttk_target']} for e in enemy_rows]}, ensure_ascii=False), encoding='utf-8')
print(json.dumps({'状态':'已导出', '配置指纹':model['CONFIG_HASH'], '玩家对照':len(inputs), '敌人对照':len(enemy_rows)}, ensure_ascii=False))
