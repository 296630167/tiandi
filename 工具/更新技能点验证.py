from pathlib import Path

root = Path(__file__).resolve().parents[1]
p = root / 'Assets/天帝/Editor/天帝道纹验证.cs'
s = p.read_text(encoding='utf-8-sig')
s = s.replace('static bool 选择详情已检查;', 'static bool 选择详情已检查;\n    static bool 锁格已检查;')
s = s.replace('选择详情已检查 = false;', '选择详情已检查 = false; 锁格已检查 = false;')
s = s.replace('var 数据 = new 天帝道纹(7);', '''var 点数 = new 天帝道纹(7); var 测格 = new Vector2Int(1, 0);
        检查("初始1级0点且仅中心默认解锁", 点数.玩家等级 == 1 && 点数.技能点 == 0 && 点数.已解锁格数 == 1 && 点数.格已解锁(Vector2Int.zero) && !点数.格已解锁(测格));
        检查("无点数拒绝解锁且锁格拒绝放置", !点数.解锁格子(测格) && !点数.可放置(点数.道纹[0], 测格) && !点数.放置(点数.道纹[0], 测格));
        检查("升一级给1点", 点数.设置玩家等级(2) && 点数.技能点 == 1 && 点数.已放置[Vector2Int.zero].等级 == 2);
        检查("重复及降级非法更新不刷点", 点数.设置玩家等级(2) && !点数.设置玩家等级(1) && !点数.设置玩家等级(0) && !点数.设置玩家等级(-1) && 点数.技能点 == 1 && 点数.玩家等级 == 2);
        检查("中心及越界不扣点", !点数.解锁格子(Vector2Int.zero) && !点数.解锁格子(new Vector2Int(50, 0)) && 点数.技能点 == 1);
        检查("解锁扣1点且重复不扣", 点数.解锁格子(测格) && !点数.解锁格子(测格) && 点数.技能点 == 0 && 点数.已解锁格数 == 2 && 点数.可放置(点数.道纹[0], 测格));
        点数.设置玩家等级(10);
        检查("跨多级累计发点并联动源纹接口", 点数.技能点 == 8 && 点数.玩家等级 == 10 && 点数.已放置[Vector2Int.zero].有接口(5));
        点数.放置(点数.道纹[0], 测格); 点数.旋转(点数.道纹[0]); 点数.收回(点数.道纹[0]);
        检查("放置旋转卸载不退点不锁格", 点数.技能点 == 8 && 点数.格已解锁(测格) && 点数.已解锁格数 == 2);
        var 远格 = new Vector2Int(49, -50);
        检查("无邻接限制可解锁边界格", 点数.解锁格子(远格) && 点数.放置(点数.道纹[0], 远格) && !点数.道纹[0].生效 && 点数.技能点 == 7);
        检查("新会话重置等级点数和锁格", new 天帝道纹(7).玩家等级 == 1 && new 天帝道纹(7).技能点 == 0 && new 天帝道纹(7).已解锁格数 == 1);
        var 数据 = new 天帝道纹(7);
        数据.设置玩家等级(80);
        foreach (var 格 in new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(10, 10), new Vector2Int(11, 10), new Vector2Int(10, 11), new Vector2Int(0, 1), new Vector2Int(-1, 1) }) 数据.解锁格子(格);''', 1)
s = s.replace('var 邻 = 升级.道纹[0];', '升级.设置玩家等级(2); 升级.解锁格子(天帝道纹.邻向[5]);\n        var 邻 = 升级.道纹[0];')
s = s.replace('检查("非法等级拒绝且等级回设重算封印", !升级.设置源纹等级(0) && 升级.设置源纹等级(1) && 起点.接口 == 1 && !邻.生效 && 升级.生效加成.Sum() == 10);', '检查("非法及降级拒绝且保持等级接口点数", !升级.设置源纹等级(0) && !升级.设置源纹等级(1) && 起点.等级 == 50 && 起点.接口 == 63 && 邻.生效 && 升级.技能点 == 48);\n        起点 = 天帝道纹.创建源纹(0);')
s = s.replace('检查("画布与8候选初始化", 页 != null && 页.数据.道纹.Count == 8 && 页.数据.已放置.Count == 1);', '''检查("画布与8候选初始化", 页 != null && 页.数据.道纹.Count == 8 && 页.数据.已放置.Count == 1);
                    点击道纹(页.格屏幕位置(new Vector2Int(1, 0)), PointerEventData.InputButton.Left);
                    检查("无点点击不解锁并提示不足", 页.数据.技能点 == 0 && !页.数据.格已解锁(new Vector2Int(1, 0)) && 页.GetComponentsInChildren<Text>().Any(t => t.text.Contains("技能点不足")));''')
s = s.replace('检查("移出隐藏属性", !页.浮窗显示); 悬停点 = null;', '''检查("移出隐藏属性", !页.浮窗显示); 悬停点 = null;
                    if (!锁格已检查)
                    {
                        开始拖(页.候选屏幕位置(0), 页.格屏幕位置(new Vector2Int(1, 0)));
                        检查("锁格红色预览且有解锁提示", 页.放置反馈 == false && 页.GetComponentsInChildren<Text>().Any(t => t.text.Contains("此格锁定")));
                        截图("12-locked-preview"); 锁格已检查 = true; 下一步 = EditorApplication.timeSinceStartup + 0.7; return;
                    }
                    结束拖(); 检查("锁格释放不放置不扣点", !页.数据.道纹[0].格子.HasValue && 页.数据.技能点 == 0);
                    页.数据.设置玩家等级(4);
                    检查("外部升级即时刷新等级技能点", 页.GetComponentsInChildren<Text>().Any(t => t.text.Contains("等级 4   ·   技能点 3")));
                    点击道纹(页.格屏幕位置(new Vector2Int(1, 0)), PointerEventData.InputButton.Right);
                    检查("右键锁格不解锁", 页.数据.技能点 == 3 && !页.数据.格已解锁(new Vector2Int(1, 0)));
                    点击道纹(页.格屏幕位置(new Vector2Int(1, 0)), PointerEventData.InputButton.Left);
                    点击道纹(页.格屏幕位置(new Vector2Int(1, 0)), PointerEventData.InputButton.Left, 2);
                    检查("真实点击解锁连点只扣一次", 页.数据.技能点 == 2 && 页.数据.格已解锁(new Vector2Int(1, 0)));
                    点击道纹(页.格屏幕位置(new Vector2Int(2, 0)), PointerEventData.InputButton.Left);
                    点击道纹(页.格屏幕位置(new Vector2Int(-1, 0)), PointerEventData.InputButton.Left);
                    检查("三个格解锁后点数归零", 页.数据.技能点 == 0 && 页.数据.已解锁格数 == 4);''')
s = s.replace('检查("空白处平移", 页.画布.平移.sqrMagnitude > 1);', '检查("空白处平移", 页.画布.平移.sqrMagnitude > 1);\n                    检查("平移不解锁不扣点", 页.数据.已解锁格数 == 4 && 页.数据.技能点 == 0);')
s = s.replace('游戏.道纹数据.道纹[1].格子 == new Vector2Int(2, 0));', '游戏.道纹数据.道纹[1].格子 == new Vector2Int(2, 0));\n                    检查("重入保留等级点数已解锁格", 游戏.道纹数据.玩家等级 == 4 && 游戏.道纹数据.技能点 == 0 && 游戏.道纹数据.已解锁格数 == 4);')
p.write_text(s, encoding='utf-8')
for name in ['Assets/天帝/Editor/天帝构建工具.cs', 'ProjectSettings/ProjectSettings.asset']:
    p = root / name
    p.write_text(p.read_text(encoding='utf-8-sig').replace('"0.5.0"', '"0.6.0"').replace('bundleVersion: 0.5.0', 'bundleVersion: 0.6.0'), encoding='utf-8')
