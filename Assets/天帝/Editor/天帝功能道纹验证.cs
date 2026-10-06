#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 只运行独立模型与隔离存档，不激活游戏宿主，不写玩家存档。
public static class 天帝功能道纹验证
{
    [Serializable] sealed class 报告
    { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static int 口数(道纹实例 纹) => Enumerable.Range(0, 6).Count(纹.有接口);
    static bool 合法(道纹实例 纹) => 纹.是功能道纹 && 纹.品阶 == 道纹品阶.稀有 && 纹.词条.Count == 1 &&
        纹.词条上限 == 1 && 天帝道纹属性.是功能(纹.属性) && 口数(纹) >= 1 && 口数(纹) <= 3 &&
        纹.实际数值 == 1 && 天帝顺序道纹.定义有效(纹.功能, 纹.入口方向, 纹.接口);
    static 天帝道纹 新网() => new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
    public static string 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式验证。");
        天帝数值同步检查.校验();
        var r = new 报告(); void 查(string 名, bool 对) => (对 ? r.通过 : r.失败).Add(名);
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/功能道纹-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        Application.LogCallback 日志 = (文, 栈, 类) => { if (类 == LogType.Error || 类 == LogType.Exception || 类 == LogType.Assert) r.错误.Add(文 + "\n" + 栈); };
        Application.logMessageReceived += 日志;
        try
        {
            查("旧分类序号保持不变", (int)道纹分类.属性 == 0 && (int)道纹分类.天赋 == 1 && (int)道纹分类.分叉 == 2);
            int[] 接口计数 = new int[3]; var 功能覆盖 = new HashSet<道纹功能>();
            var 形状 = new Dictionary<道纹功能, HashSet<int>>();
            foreach (var f in Enumerable.Range(1, 天帝顺序道纹.功能数量).Select(i => (道纹功能)i)) 形状[f] = new HashSet<int>();
            foreach (int 级 in new[] { 1, 50, 100 }) for (int 阶 = 0; 阶 < 8; 阶++)
            {
                bool 正确 = true, 普通正确 = true;
                for (int seed = 0; seed < 600; seed++)
                {
                    var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, (道纹品阶)阶, new System.Random(seed + 阶 * 300 + 级 * 10000), 道纹属性分组.形态, 级);
                    正确 &= 合法(纹) && 纹.物品等级 == 级; 功能覆盖.Add(纹.功能); 接口计数[口数(纹) - 1]++; 形状[纹.功能].Add(纹.接口);
                    var 普 = 天帝道纹生成.创建(1, 道纹分类.属性, (道纹品阶)阶, new System.Random(seed + 1101), 物品等级: 级);
                    普通正确 &= 普.是功能道纹 ? 合法(普) : 普.词条.All(x => !天帝道纹属性.是功能(x.属性));
                }
                查("限定功能无视输入品阶且固定单条-" + 阶 + "@" + 级, 正确);
                查("常规生成功能独立且不混入属性-" + 阶 + "@" + 级, 普通正确);
            }
            var 全功能 = Enumerable.Range(1, 天帝顺序道纹.功能数量).Select(i => (道纹功能)i).ToArray();
            查(天帝顺序道纹.功能数量 + "功能均可生成", 功能覆盖.SetEquals(全功能));
            foreach (var kv in 形状) 查("随机方向覆盖全部物理形状-" + kv.Key,
                kv.Value.Count == (kv.Key <= 道纹功能.分裂 ? 20 : kv.Key == 道纹功能.连锁 ? 15 : 21));
            double 样本 = 接口计数.Sum(), 单口预期 = 样本 * (天帝顺序道纹.功能数量 - 3) / 天帝顺序道纹.功能数量 / 2;
            double 三口预期 = 样本 * 2 / 天帝顺序道纹.功能数量;
            查("新增功能1口2口近似各半且原三种保持", Math.Abs(接口计数[0] - 单口预期) < 400 && Math.Abs(接口计数[2] - 三口预期) < 300);
            var 网 = 新网(); 网.设置玩家等级(20);
            var 功 = 天帝道纹生成.创建(1, 道纹分类.功能, 道纹品阶.传说, new System.Random(5), 物品等级: 20);
            typeof(道纹实例).GetProperty("入口方向").SetValue(功, 3); 功.接口 = 天帝顺序道纹.固定接口(功.功能, 3);
            网.获得道纹(功); 网.解锁格子(new Vector2Int(1, 0)); 网.放置(功, new Vector2Int(1, 0));
            网.设置回收锁定(功, true); 网.保存布局方案(0, "功能布局");
            var 钱 = new 天帝通货(网, 50, 400); int 通知 = 0; 网.状态改变 += () => 通知++;
            foreach (var 种 in 天帝通货.可用种类.Where(x => x != 通货种类.易纹砂 && x != 通货种类.重铸石))
            {
                string 前 = JsonUtility.ToJson(网.导出存档()); int 余 = 钱.数量(种);
                查("禁止材料不修改不扣费-" + 种, !钱.可使用(种, 功, 0, out _) && !钱.使用(种, 功, 0, out _) &&
                    前 == JsonUtility.ToJson(网.导出存档()) && 钱.数量(种) == 余 && 通知 == 0);
            }
            string 索前 = JsonUtility.ToJson(网.导出存档()); int 索余 = 钱.数量(通货种类.易纹砂);
            查("非法词条索引不扣费不改动", !钱.使用(通货种类.易纹砂, 功, -1, out _) && !钱.使用(通货种类.易纹砂, 功, 1, out _) &&
                索前 == JsonUtility.ToJson(网.导出存档()) && 钱.数量(通货种类.易纹砂) == 索余);
            var 洗覆盖 = new HashSet<道纹功能>();
            using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                for (int i = 0; i < 天帝顺序道纹.功能数量 * 20; i++)
                {
                    var 种 = i % 2 == 0 ? 通货种类.易纹砂 : 通货种类.重铸石; int 余 = 钱.数量(种), 旧通知 = 通知, 旧口 = 功.接口, 旧数 = 口数(功);
                    查("重抽保品阶条数位置及保护-" + i, 钱.使用(种, 功, 0, out _) && 合法(功) &&
                        功.格子 == new Vector2Int(1, 0) && 功.回收锁定 && 功.物品等级 == 20 && 钱.数量(种) == 余 - 1 && 通知 == 旧通知 + 1);
                    查("重抽同数量保方向异数量只增减一口-" + i, 旧数 == 口数(功) ? 旧口 == 功.接口 :
                        旧数 > 口数(功) ? (旧口 & 功.接口) == 功.接口 : (旧口 & 功.接口) == 旧口);
                    洗覆盖.Add(功.功能);
                    bool 加成正确 = 天帝道纹属性.形态属性.All(x => 网.生效加成[(int)x] == 0);
                    var p = 普攻参数.读取通路(网, 人, 0);
                    查("重抽撤回旧功能并同步实际通路-" + i, 加成正确 && 功.生效 == 功.有接口(3) &&
                        (功.生效 ? p.顺序计划 != null && p.顺序计划.起点.功能 == 功.功能 && p.顺序计划.起点.实际来路 == 3 : p.顺序计划 == null));
                }
            }
            查("两种通货可切换到全部" + 天帝顺序道纹.功能数量 + "功能", 洗覆盖.SetEquals(全功能));
            网.收回(功); 查("功能卸下撤回全部形态", 天帝道纹属性.形态属性.All(x => 网.生效加成[(int)x] == 0));
            var 存 = new 天帝存档(Path.Combine(目录, "隔离存档"));
            查("功能可正式保存", 存.保存(new 天帝存档数据 { 序章已完成 = true, 主角 = 天帝普攻.主角配置(), 画布 = 网.导出存档(), 通货 = 钱.导出库存(), 灵石 = 500 }));
            var 回 = 天帝道纹.读取存档(存.读取().画布); var 回功 = 回.道纹.Single();
            网.保存布局方案(0, "新功能布局");
            回 = 天帝道纹.读取存档(网.导出存档()); 回功 = 回.道纹.Single();
            查("回读保留功能保护入口接口数值及布局", 合法(回功) && 回功.接口 == 功.接口 && 回功.入口方向 == 功.入口方向 && 回功.回收锁定 && 回功.物品等级 == 20 &&
                回功.功能 == 功.功能 && 回.检查布局方案(0, out _));
            void 拒档(string 名, Action<道纹存档实例> 改)
            {
                var d = 网.导出存档(); 改(d.道纹[0]); bool 拒 = false;
                try { 天帝道纹.读取存档(d); } catch (InvalidDataException) { 拒 = true; } 查(名, 拒);
            }
            拒档("功能其它品阶存档拒绝", x => x.品阶 = 道纹品阶.完美);
            拒档("功能多词条存档拒绝", x => x.词条.Add(new 道纹词条(道纹属性.连锁, 1)));
            拒档("功能零词条存档拒绝", x => x.词条.Clear());
            拒档("功能普通属性存档拒绝", x => x.词条[0].属性 = 道纹属性.力量);
            拒档("功能超3接口存档拒绝", x => x.接口 = 15);
            var 旧 = 新网(); var 混 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.传说, new System.Random(3), 道纹属性分组.基础);
            混.词条.Clear(); foreach (var a in new[] { 道纹属性.力量, 道纹属性.数量, 道纹属性.连锁, 道纹属性.范围, 道纹属性.弧度 }) 混.词条.Add(new 道纹词条(a, 1));
            旧.获得道纹(混); var 回旧 = 天帝道纹.读取存档(旧.导出存档()); var 回混 = 回旧.道纹.Single();
            查("旧混合属性原样兼容不重抽", 回混.分类 == 混.分类 && 回混.品阶 == 混.品阶 && 回混.接口 == 混.接口 &&
                回混.编号 == 混.编号 && 回混.物品等级 == 混.物品等级 && 回混.词条.Count == 混.词条.Count &&
                回混.词条.Zip(混.词条, (a, b) => a.属性 == b.属性 && a.实际数值 == b.实际数值).All(x => x));
            var 范围 = new 天帝回收范围 { 类型 = 3, 品阶掩码 = 1 << (int)道纹品阶.稀有 };
            查("功能类型回收筛选不混入旧属性", 范围.匹配(功) && !范围.匹配(混));
            查("功能回收按稀有55且锁定保护", 天帝数值.道纹回收价(功) == 55 && !网.可回收(功, out _));
            var 盒网 = 新网(); var 盒 = new 天帝宝盒(盒网, 421, 500000); bool 盒对 = true;
            for (int i = 0; i < 300; i++) { int 前 = 盒.灵石; 盒对 &= 盒.抽取(宝盒种类.功能, out var x, out _) && 合法(x) && 盒.灵石 == 前 - 500; }
            查("连续300功能盒固定稀有单条机制接口并扣费", 盒对);
            foreach (var 难 in new[] { 战斗难度.普通, 战斗难度.困难 })
            {
                var 掉网 = 新网(); var 地图 = new 天帝战斗地图(381); var 掉 = new 天帝道纹掉落(地图, 掉网, 难);
                for (int i = 0; i < 400; i++)
                {
                    var 敌 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.头目, 地图.出生位置), 难, 50);
                    typeof(战斗敌人).GetProperty("血量").SetValue(敌, 0f); typeof(天帝道纹掉落).GetMethod("敌人死亡", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(掉, new object[] { 敌 });
                }
                查("实际战斗掉落能获得合法功能-" + 难, 掉网.道纹.Any(x => x.是功能道纹) && 掉网.道纹.Where(x => x.是功能道纹).All(合法));
                foreach (int 级 in new[] { 1, 50, 70, 85, 100 })
                {
                    var 敌 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.王级, 地图.出生位置), 难, 级);
                    typeof(战斗敌人).GetProperty("血量").SetValue(敌, 0f); typeof(天帝道纹掉落).GetMethod("敌人死亡", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(掉, new object[] { 敌 });
                    var x = 掉网.道纹.Last(); 查("BOSS保持属性保底-" + 难 + "@" + 级, x.分类 == 道纹分类.属性 && x.品阶 >= 天帝道纹掉落.BOSS保底品阶(敌.等级));
                }
            }
        }
        catch (Exception ex) { r.错误.Add(ex.ToString()); }
        finally { Application.logMessageReceived -= 日志; File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(r, true)); }
        return 目录;
    }
}
#endif
