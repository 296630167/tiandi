from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
def edit(path, old, new):
    p = ROOT / path
    s = p.read_text(encoding='utf-8-sig')
    assert old in s, (path, old)
    p.write_text(s.replace(old,new),encoding='utf-8')

path = ROOT/'Assets/天帝/脚本/核心/天帝道纹.cs'
s = path.read_text(encoding='utf-8-sig')
start = s.index('        var 随机 = new System.Random(种子);')
end = s.index('        重算();', start)
s = s[:start] + '''        道纹.Add(new 道纹实例 { 编号 = 1, 分类 = 道纹分类.技能, 品阶 = 道纹品阶.普通,
            技能名称 = "普攻", 接口 = 1 << 3, 介绍 = "开局技能；接通起点后激活普攻，攻击方式与伤害待设计" });
''' + s[end:]
s = s.replace('public int 技能点 { get; private set; }', 'public int 技能点 { get; private set; } = 1;')
s = s.replace('技能点 = 天帝天赋效果.开局技能点(天赋);', '技能点 = 1 + 天帝天赋效果.开局技能点(天赋);')
s = s.replace('public string 技能效果 => 技能力量倍率.HasValue ?', 'public string 技能效果 => 技能名称 == "普攻" ? "解锁普攻；攻击方式与伤害待设计" : 技能力量倍率.HasValue ?')
path.write_text(s,encoding='utf-8')

# 既有复杂系统回归明确使用Editor夹具，实际开局由新专项验证。
for name in ['天帝道纹验证.cs','天帝道纹品阶验证.cs','天帝主角属性验证.cs','天帝天赋验证.cs','天帝通货验证.cs']:
    p = ROOT/'Assets/天帝/Editor'/name
    s = p.read_text(encoding='utf-8-sig').replace('new 天帝道纹(', '天帝道纹夹具.创建(')
    p.write_text(s,encoding='utf-8')

edit('Assets/天帝/脚本/核心/天帝游戏.cs', '游戏阶段 { 标题, 序章, 主页, 道纹, 源道纹选择 }', '游戏阶段 { 标题, 序章, 主页, 道纹, 源道纹选择, 道纹改造 }')
edit('Assets/天帝/脚本/核心/天帝游戏.cs', '''                if (界面.道纹页 != null && 界面.道纹页.通货已打开) 界面.道纹页.关闭通货();
                else if (界面.道纹页 != null && 界面.道纹页.拖动中) 界面.道纹页.取消拖动();''', '''                if (界面.道纹页 != null && 界面.道纹页.拖动中) 界面.道纹页.取消拖动();''')
edit('Assets/天帝/脚本/核心/天帝游戏.cs', '            else if (阶段 == 游戏阶段.道纹)', '            else if (阶段 == 游戏阶段.道纹改造) 返回主页();\n            else if (阶段 == 游戏阶段.道纹)')
edit('Assets/天帝/脚本/核心/天帝游戏.cs', '    public void 返回主页()\n', '''    public void 打开道纹改造()
    {
        if (阶段 != 游戏阶段.主页 || 界面.设置已打开 || 道纹数据 == null || 通货数据 == null) return;
        阶段 = 游戏阶段.道纹改造; 界面.显示道纹改造(道纹数据, 通货数据);
    }
    public void 返回主页()
''')
edit('Assets/天帝/脚本/核心/天帝游戏.cs', '        if (阶段 != 游戏阶段.道纹) return;', '        if (阶段 != 游戏阶段.道纹 && 阶段 != 游戏阶段.道纹改造) return;')

edit('Assets/天帝/脚本/界面/天帝界面.cs', '    public 天帝道纹界面 道纹页 { get; private set; }', '    public 天帝道纹界面 道纹页 { get; private set; }\n    public 天帝通货界面 改造页 { get; private set; }')
edit('Assets/天帝/脚本/界面/天帝界面.cs', '        道纹页 = null;', '        道纹页 = null; 改造页 = null;')
edit('Assets/天帝/脚本/界面/天帝界面.cs', '        按钮(页面, "道纹", 46, 40, 166, 60, 游戏.打开道纹);', '        按钮(页面, "道纹", 46, 40, 166, 60, 游戏.打开道纹);\n        按钮(页面, "道纹改造", 46, 118, 166, 60, 游戏.打开道纹改造);')
edit('Assets/天帝/脚本/界面/天帝界面.cs', '道纹页.初始化(数据, 游戏.默认字体, 游戏.返回主页, 游戏.通货数据);', '道纹页.初始化(数据, 游戏.默认字体, 游戏.返回主页);')
edit('Assets/天帝/脚本/界面/天帝界面.cs', '    public void 显示源道纹选择()', '''    public void 显示道纹改造(天帝道纹 数据, 天帝通货 通货)
    {
        换页(); var 区 = 区块(页面, "道纹改造页面", 0, 0, 1600, 900);
        改造页 = 区.gameObject.AddComponent<天帝通货界面>(); 改造页.初始化(数据, 通货, 游戏.默认字体, 游戏.返回主页);
    }
    public void 显示源道纹选择()''')

p=ROOT/'Assets/天帝/脚本/界面/天帝道纹界面.cs'
s=p.read_text(encoding='utf-8-sig')
start=s.index('    天帝通货 通货数据;'); end=s.index('    public void 初始化',start)
s=s[:start]+s[end:]
s=s.replace('Action 返回, 天帝通货 通货 = null)', 'Action 返回)').replace('; 通货数据 = 通货;', ';')
s=s.replace('        if (通货数据 != null) 按钮(根, "通货改造", 493, 27, 170, 46, 打开通货);\n','')
s=s.replace('float 间距 = 1536f / 数据.道纹.Count, 宽 = 间距 - 8, x = i * 间距;', 'float 间距 = Mathf.Min(1536f / 数据.道纹.Count, 170), 宽 = 间距 - 8, x = i * 间距;')
s=s.replace('纹.是技能 ? " · 效果待设计"', '纹.是技能 ? " · " + (纹.技能名称 == "待设计" ? "效果待设计" : 纹.技能名称)')
s=s.replace('通货已打开 || ', '')
start=s.index('    public void 打开通货()'); end=s.index('    void OnDisable()',start)
s=s[:start]+s[end:]
p.write_text(s,encoding='utf-8')
edit('Assets/天帝/脚本/界面/天帝通货界面.cs','"关闭", 1120, 24, 128, 44','"返回主页", 1080, 24, 168, 44')
edit('Assets/天帝/脚本/界面/天帝通货界面.cs','if (纹.是技能) 文.Append("技能效果：待设计\\n");','if (纹.是技能) 文.Append("技能效果：").Append(纹.技能效果).Append("\\n");')
edit('Assets/天帝/Editor/天帝构建工具.cs','"0.10.0"','"0.10.1"')
edit('ProjectSettings/ProjectSettings.asset','bundleVersion: 0.10.0','bundleVersion: 0.10.1')
print('开局候选、技能点与主页独立道纹改造入口已更新')
