from pathlib import Path

root = Path(__file__).resolve().parents[1]
p = root / 'Assets/天帝/Editor/天帝道纹验证.cs'
s = p.read_text(encoding='utf-8-sig')
s = s.replace('随机数据.道纹.Count == 8', '随机数据.道纹.Count == 10').replace('Distinct().Count() == 7', 'Distinct().Count() == 10')
s = s.replace('纹.类型 == "待设计"', '(纹.类型 == "属性" || 纹.类型 == "功能")')
s = s.replace('百种子随机候选覆盖七属性且占位完整', '百种子候选覆盖五属性五功能且占位完整')
s = s.replace('特殊效果：无\\n道纹类型：待设计\\n道纹介绍：待补充', '特殊效果：无\\n道纹类型：属性\\n道纹介绍：待补充')
s = s.replace('画布与8候选初始化', '画布与10候选初始化').replace('页.数据.道纹.Count == 8', '页.数据.道纹.Count == 10')
s = s.replace('Length == 9', 'Length == 11').replace('已放置 · 可从画布拖回', '已放置 · 可拖回')
needle = '检查("百种子候选覆盖五属性五功能且占位完整", 随机正确);'
assert needle in s
s = s.replace(needle, needle + '''
        var 分类数据 = new 天帝道纹(19);
        检查("五属性五功能分类准确", 分类数据.道纹.Count(纹 => 纹.分类 == 道纹分类.属性) == 5 && 分类数据.道纹.Count(纹 => 纹.分类 == 道纹分类.功能) == 5 && 分类数据.道纹.Where(纹 => 纹.分类 == 道纹分类.属性).Select(纹 => 纹.属性).OrderBy(v => v).SequenceEqual(new[] { 道纹属性.力量, 道纹属性.速度, 道纹属性.智力, 道纹属性.血量, 道纹属性.灵力 }));
        检查("源纹归属性类且保留10点", 分类数据.已放置[Vector2Int.zero].类型 == "属性" && 分类数据.生效加成[0] == 10);
        检查("分类详情区分属性与形态构建", 分类数据.道纹.All(纹 => 纹.详情文字().Contains("道纹类型：" + 纹.类型) && 纹.详情文字().Contains(纹.是功能 ? "形态构建：" : "基础属性：")));
        分类数据.设置玩家等级(20);
        var 血 = 分类数据.道纹.First(纹 => 纹.属性 == 道纹属性.血量); var 链 = 分类数据.道纹.First(纹 => 纹.属性 == 道纹属性.连锁);
        血.接口 = 9; 链.接口 = 8;
        分类数据.解锁格子(new Vector2Int(1, 0)); 分类数据.解锁格子(new Vector2Int(2, 0));
        分类数据.放置(血, new Vector2Int(1, 0)); 分类数据.放置(链, new Vector2Int(2, 0));
        检查("血量与连锁分别汇总且断链取消", 血.生效 && 链.生效 && 分类数据.生效加成[(int)道纹属性.血量] == 血.数值 && 分类数据.生效加成[(int)道纹属性.连锁] == 1);
        分类数据.收回(血);
        检查("功能孤立不计入汇总", !链.生效 && 分类数据.生效加成[(int)道纹属性.连锁] == 0 && 分类数据.生效加成[0] == 10);''')
needle = '检查("画布与10候选初始化", 页 != null && 页.数据.道纹.Count == 10 && 页.数据.已放置.Count == 1);'
s = s.replace(needle, needle + '''
                    检查("候选可见分类并覆盖血量灵力连锁弧度范围", 页.GetComponentsInChildren<Text>().Count(t => t.text == "属性 · 特殊：无") == 5 && 页.GetComponentsInChildren<Text>().Count(t => t.text == "功能 · 特殊：无") == 5 && 页.数据.道纹.Select(纹 => 纹.属性).Distinct().Count() == 10);
                    检查("汇总分开属性与功能且不超出区域", 页.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith("接通 0 / 10") && t.text.Contains("\\n属性  力量 +10") && t.text.Contains("\\n功能  等待接入源纹") && t.preferredHeight <= t.rectTransform.rect.height));''')
p.write_text(s, encoding='utf-8')
for name in ['Assets/天帝/Editor/天帝构建工具.cs', 'ProjectSettings/ProjectSettings.asset']:
    p = root / name
    p.write_text(p.read_text(encoding='utf-8-sig').replace('"0.6.0"', '"0.7.0"').replace('bundleVersion: 0.6.0', 'bundleVersion: 0.7.0'), encoding='utf-8')
