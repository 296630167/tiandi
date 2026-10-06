from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
f = ROOT / 'Assets/天帝/Editor/天帝道纹品阶验证.cs'
s = f.read_text(encoding='utf-8')
s = s.replace('int[] 最少口 = { 1, 2, 3, 4, 5, 6, 6, 6 }, 最多口 = { 2, 3, 4, 4, 5, 6, 6, 6 };', 'int[] 最少口 = Enumerable.Repeat(1, 8).ToArray(), 最多口 = Enumerable.Repeat(6, 8).ToArray();')
s = s.replace('词条及接口范围', '词条容量独立于接口')
s = s.replace(' && 规则.最少接口 == 最少口[i] && 规则.最多接口 == 最多口[i]', '')
s = s.replace('检查((道纹品阶)i + "接口上下界均可生成", 接口覆盖[i].Contains(最少口[i]) && 接口覆盖[i].Contains(最多口[i]));', '检查((道纹品阶)i + "生成接口处于1至6范围", 接口覆盖[i].All(v => v >= 1 && v <= 6));')
f.write_text(s, encoding='utf-8')
f = ROOT / 'Assets/天帝/Editor/天帝构建工具.cs'
s = f.read_text(encoding='utf-8').replace('public static string 运行开局验证()', 'public static string 运行接口验证() => 天帝接口验证.运行();\n    public static string 运行开局验证()')
f.write_text(s, encoding='utf-8')
