from pathlib import Path
import re
ROOT = Path(__file__).resolve().parents[1]
core = ROOT / 'Assets/天帝/脚本/核心'
f = core / '天帝道纹品阶.cs'
s = f.read_text(encoding='utf-8').replace('最少词条, 最多词条, 最少接口, 最多接口', '最少词条, 最多词条')
s = s.replace('int 少词, int 多词, int 少口, int 多口, Color 色', 'int 少词, int 多词, Color 色')
s = s.replace(' 最少接口 = 少口; 最多接口 = 多口;', '')
s = re.sub(r'new 道纹品阶约定\((\d), (\d), \d, \d, ', r'new 道纹品阶约定(\1, \2, ', s)
f.write_text(s, encoding='utf-8')
f = core / '天帝通货.cs'
s = f.read_text(encoding='utf-8').replace('            接口 = 补接口(接口, 规则.最少接口);\n', '')
f.write_text(s, encoding='utf-8')
f = ROOT / 'Assets/天帝/脚本/界面/天帝通货界面.cs'
s = f.read_text(encoding='utf-8').replace('接口改造可突破初始品阶接口数，最多六个。', '接口独立于品阶；升阶不加接口，最多六个。')
f.write_text(s, encoding='utf-8')
f = ROOT / 'Assets/天帝/Editor/天帝道纹夹具.cs'
s = f.read_text(encoding='utf-8')
start = s.index('            int[] 方向')
end = s.index('            var 纹 =', start)
s = s[:start] + '            int 掩码 = 天帝道纹生成.随机接口(随机);\n' + s[end:]
f.write_text(s, encoding='utf-8')
f = ROOT / 'Assets/天帝/Editor/天帝通货验证.cs'
s = f.read_text(encoding='utf-8').replace('口数(纹) >= 天帝道纹品阶.获取(定.目标.Value).最少接口', '纹.接口 == 8')
s = s.replace('精准升阶保词保口补最低并扣1', '精准升阶保词保口仅补词条并扣1')
s = s.replace('低品阶突破为六口保词', '任意品阶补足为六口保词')
s = s.replace('品阶 <= 道纹品阶.史诗 && 纹1.有接口(3)', '品阶 <= 道纹品阶.史诗 && 纹1.接口 == 8')
f.write_text(s, encoding='utf-8')
for rel in ['ProjectSettings/ProjectSettings.asset', 'Assets/天帝/Editor/天帝构建工具.cs']:
    f = ROOT / rel
    s = f.read_text(encoding='utf-8').replace('0.10.1', '0.11.0')
    f.write_text(s, encoding='utf-8')
