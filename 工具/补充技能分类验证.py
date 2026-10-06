from pathlib import Path
root = Path(__file__).resolve().parents[1]
p = root / 'Assets/天帝/Editor/天帝道纹验证.cs'
s = p.read_text(encoding='utf-8-sig')
s = s.replace('随机数据.道纹.Count == 10', '随机数据.道纹.Count == 11').replace('(纹.类型 == "属性" || 纹.类型 == "功能")', '(纹.类型 == "属性" || 纹.类型 == "功能" || 纹.类型 == "技能")')
s = s.replace('页.数据.道纹.Count == 10', '页.数据.道纹.Count == 11').replace('画布与10候选初始化', '画布与11候选初始化').replace('Length == 11', 'Length == 12').replace('接通 0 / 10', '接通 0 / 11')
needle = '检查("功能孤立不计入汇总", !链.生效 && 分类数据.生效加成[(int)道纹属性.连锁] == 0 && 分类数据.生效加成[0] == 10);'
assert needle in s
s = s.replace(needle, needle + '''
        var 技能数据 = new 天帝道纹(31); 技能数据.设置玩家等级(10);
        var 技 = 技能数据.道纹.Single(纹 => 纹.是技能); var 桥 = 技能数据.道纹[0]; 桥.接口 = 9; 技.接口 = 8;
        检查("技能独立分类且纯效果无属性加成", 技.类型 == "技能" && 技.数值 == 0 && 技.技能效果 == "待设计" && 技.详情文字().Contains("基础属性：无\\n技能效果：待设计") && !技.已激活 && 技能数据.已激活技能.Count == 0);
        foreach (var 格 in new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(10, 10), new Vector2Int(11, 10), new Vector2Int(10, 11) }) 技能数据.解锁格子(格);
        技能数据.放置(技, new Vector2Int(2, 0));
        检查("未接源纹技能不能激活", !技.已激活 && 技能数据.已激活技能.Count == 0);
        技能数据.放置(桥, new Vector2Int(1, 0));
        检查("通过普通道纹连接可激活且技能不加属性", 技.已激活 && 技能数据.已激活技能.Count == 1 && 技能数据.生效加成.Sum() == 10 + 桥.数值 && 技.详情文字().Contains("已接源纹 · 已激活"));
        技.数值 = 999; 技能数据.重算();
        检查("技能数值字段不参与属性或功能汇总", 技能数据.生效加成.Sum() == 10 + 桥.数值 && 技能数据.已激活技能.Count == 1);
        技能数据.旋转(桥);
        检查("上游旋转断开即时停用技能", !技.已激活 && 技能数据.已激活技能.Count == 0);
        for (int i = 0; i < 5; i++) 技能数据.旋转(桥);
        检查("重接恢复技能并且重复重算无重复激活", 技.已激活 && 技能数据.已激活技能.Count == 1);
        技能数据.收回(技); 检查("技能卸载即停用", !技.已激活 && 技能数据.已激活技能.Count == 0);
        var 环 = 技能数据.道纹[1]; 技.接口 = 桥.接口 = 环.接口 = 63;
        技能数据.放置(技, new Vector2Int(10, 10)); 技能数据.放置(桥, new Vector2Int(11, 10)); 技能数据.放置(环, new Vector2Int(10, 11));
        检查("孤立技能闭环不能自激活", !技.已激活 && 技能数据.已激活技能.Count == 0);
        技能数据.放置(技, new Vector2Int(1, 0)); 技能数据.放置(桥, new Vector2Int(2, 0));
        检查("技能能作为连接中继且无附带属性", 技.已激活 && 桥.生效 && 技能数据.已激活技能.Count == 1 && 技能数据.生效加成.Sum() == 10 + 桥.数值);''')
needle = '点击(游戏, "关闭"); 完成(); return;'
assert needle in s
s = s.replace(needle, '''点击(游戏, "关闭"); break;
                case 23:
                    点击(游戏, "道纹"); 页 = 游戏.界面.道纹页;
                    页.数据.设置玩家等级(5); 点击道纹(页.格屏幕位置(new Vector2Int(3, 0)), PointerEventData.InputButton.Left);
                    页.数据.道纹[1].接口 = 9; 页.数据.道纹[10].接口 = 1; 页.数据.重算();
                    开始拖(页.候选屏幕位置(10), 页.格屏幕位置(new Vector2Int(3, 0)));
                    检查("技能候选拖入已解锁格绿色可放", 页.放置反馈 == true); break;
                case 24:
                    结束拖(); 检查("技能接口错位暗掉且未激活", 页.数据.道纹[10].格子 == new Vector2Int(3, 0) && !页.数据.道纹[10].已激活 && 页.数据.已激活技能.Count == 0);
                    截图("13-skill-inactive"); break;
                case 25:
                    for (int i = 0; i < 3; i++) 点击道纹(页.格屏幕位置(new Vector2Int(3, 0)), PointerEventData.InputButton.Right);
                    检查("技能真实右键接通激活并显示纯效果详情", 页.数据.道纹[10].已激活 && 页.数据.已激活技能.Count == 1 && 页.浮窗显示 && 页.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith("技能道纹\\n基础属性：无\\n技能效果：待设计") && t.text.Contains("道纹类型：技能") && t.text.Contains("已接源纹 · 已激活")));
                    截图("14-skill-active"); break;
                case 26:
                    点击道纹(页.格屏幕位置(new Vector2Int(1, 0)), PointerEventData.InputButton.Right);
                    检查("实际旋转上游停用技能", !页.数据.道纹[10].已激活 && 页.数据.已激活技能.Count == 0);
                    for (int i = 0; i < 5; i++) 点击道纹(页.格屏幕位置(new Vector2Int(1, 0)), PointerEventData.InputButton.Right);
                    检查("实际重接恢复技能且点数不变", 页.数据.道纹[10].已激活 && 页.数据.技能点 == 0 && 页.数据.已解锁格数 == 5);
                    点击道纹(页.格屏幕位置(new Vector2Int(3, 0)), PointerEventData.InputButton.Left);
                    点击道纹(页.格屏幕位置(new Vector2Int(3, 0)), PointerEventData.InputButton.Left, 2);
                    检查("实际双击技能卸载并保留解锁格", !页.数据.道纹[10].格子.HasValue && !页.数据.道纹[10].已激活 && 页.数据.已激活技能.Count == 0 && 页.数据.格已解锁(new Vector2Int(3, 0)));
                    完成(); return;''')
p.write_text(s, encoding='utf-8')
