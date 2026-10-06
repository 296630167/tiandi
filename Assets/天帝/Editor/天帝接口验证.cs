#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class 天帝接口验证
{
    [Serializable] sealed class 报告
    {
        public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>();
        public int 抽样次数 = 100000;
        public int[] 实际接口分布 = new int[2];
        public float 理论平均接口数 = 1.3f;
    }
    static int 数量(int 掩码) => Enumerable.Range(0, 6).Count(d => (掩码 & (1 << d)) != 0);
    public static string 运行()
    {
        var 结果 = new 报告();
        void 检查(string 名, bool 成立) => (成立 ? 结果.通过 : 结果.失败).Add(名);
        bool 拒绝(Action 操作) { try { 操作(); return false; } catch (ArgumentException) { return true; } }
        try
        {
            int[] 分布 = new int[2];
            for (int i = 0; i < 100; i++) 分布[天帝道纹生成.接口数量结果(i) - 1]++;
            检查("普通道纹接口70/30分布", 分布.SequenceEqual(new[] { 70, 30 }) && 天帝道纹生成.接口权重.Sum() == 100);
            检查("普通接口边界与非法参数", new[] { 0, 69, 70, 99 }.Select(天帝道纹生成.接口数量结果).SequenceEqual(new[] { 1, 1, 2, 2 }) &&
                拒绝(() => 天帝道纹生成.接口数量结果(-1)) && 拒绝(() => 天帝道纹生成.接口数量结果(100)) &&
                拒绝(() => 天帝道纹生成.随机接口(null)) && 拒绝(() => 天帝道纹生成.随机分叉接口(null)) &&
                拒绝(() => 天帝道纹生成.随机方向(new System.Random(1), 0)) && 拒绝(() => 天帝道纹生成.随机方向(new System.Random(1), 7)));
            for (int 数 = 1; 数 <= 6; 数++)
            {
                var 组合 = new HashSet<int>(); var 随机 = new System.Random(数);
                bool 有效 = true;
                for (int i = 0; i < 2000; i++) { int 口 = 天帝道纹生成.随机方向(随机, 数); 有效 &= 口 > 0 && 口 < 64 && 数量(口) == 数; 组合.Add(口); }
                int[] 组合数 = { 6, 15, 20, 15, 6, 1 };
                检查(数 + "接口无重复越界且覆盖全部方向组合", 有效 && 组合.Count == 组合数[数 - 1]);
            }
            var a = new System.Random(703); var b = new System.Random(703); bool 相同 = true;
            for (int i = 0; i < 结果.抽样次数; i++)
            {
                int 口 = 天帝道纹生成.随机接口(a); 结果.实际接口分布[数量(口) - 1]++;
                相同 &= 口 == 天帝道纹生成.随机接口(b);
            }
            检查("10万次随机同种子可复现", 相同);
            double[] 概率 = { .7, .3 };
            检查("10万次普通接口落在五倍标准差内", Enumerable.Range(0, 2).All(i => Math.Abs(结果.实际接口分布[i] - 结果.抽样次数 * 概率[i]) <= 5 * Math.Sqrt(结果.抽样次数 * 概率[i] * (1 - 概率[i]))));
            foreach (var 类 in new[] { 道纹分类.属性, 道纹分类.功能 })
            {
                for (int 阶 = 0; 阶 < 8; 阶++)
                {
                    bool 一致 = true, 词合法 = true; var 口数 = new HashSet<int>();
                    for (int seed = 0; seed < 2000; seed++)
                    {
                        道纹属性分组? 组 = 类 == 道纹分类.属性 ? 道纹属性分组.基础 : (道纹属性分组?)null;
                        var 纹 = 天帝道纹生成.创建(1, 类, (道纹品阶)阶, new System.Random(seed), 组);
                        var 对照 = 天帝道纹生成.创建(1, 类, 道纹品阶.普通, new System.Random(seed), 组);
                        一致 &= 纹.接口 == 对照.接口; 口数.Add(数量(纹.接口));
                        var 定 = 天帝道纹品阶.获取(纹.品阶);
                        词合法 &= (纹.是功能道纹 ? 纹.品阶 == 道纹品阶.稀有 && 纹.词条.Count == 1 : 纹.是五行道纹 ? 纹.词条.Count == 1 : 纹.词条.Count >= 定.最少词条 && 纹.词条.Count <= 定.最多词条) &&
                            纹.词条.All(x => (int)x.属性 >= 0 && (int)x.属性 < 天帝道纹属性.数量);
                    }
                    检查(类 + "/" + (道纹品阶)阶 + "接口抽样独立于品阶", 一致 && 口数.SetEquals(类 == 道纹分类.功能 ? new[] { 1, 2, 3 } : new[] { 1, 2 }) && 词合法);
                }
            }
            var 分叉口 = new HashSet<int>(); bool 无词 = true;
            for (int seed = 0; seed < 2000; seed++)
            {
                var 纹 = 天帝道纹生成.创建(1, 道纹分类.分叉, 道纹品阶.传说, new System.Random(seed));
                分叉口.Add(数量(纹.接口)); 无词 &= 纹.词条.Count == 0 && 纹.品阶 == 道纹品阶.普通;
            }
            检查("分叉道纹单独掉落3至6口且无属性", 分叉口.SetEquals(new[] { 3, 4, 5, 6 }) && 无词);
            检查("六边格权重与单向传导", 天帝道纹.格权重(Vector2Int.zero) == 0 &&
                天帝道纹.格权重(new Vector2Int(2, -1)) == 2 &&
                天帝道纹.可传导(new Vector2Int(1, 0), new Vector2Int(0, 1)) &&
                天帝道纹.可传导(new Vector2Int(1, 0), new Vector2Int(2, 0)) &&
                !天帝道纹.可传导(new Vector2Int(2, 0), new Vector2Int(1, 0)));
            var 链 = new 天帝道纹(1, 天帝天赋.获取((int)天赋种类.普通人));
            链.设置玩家等级(5);
            var 格子 = new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1), new Vector2Int(0, 1) };
            int[] 接口 = { 9, 12, 40, 1 };
            var 链纹 = new List<道纹实例>();
            for (int i = 0; i < 格子.Length; i++)
            {
                链.解锁格子(格子[i]); var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, new System.Random(i), 道纹属性分组.基础);
                纹.接口 = 接口[i]; 链.获得道纹(纹); 链.放置(纹, 格子[i]); 链纹.Add(纹);
            }
            检查("同圈可继续、外圈回内圈不生效", 链纹[0].生效 && 链纹[1].生效 && 链纹[2].生效 && !链纹[3].生效 && 链.生效数 == 3 &&
                链.弹槽生效数[0] == 3 && 链.弹槽生效数.Skip(1).All(x => x == 0));
            var 分叉 = 天帝道纹生成.创建(1, 道纹分类.分叉, 道纹品阶.普通, new System.Random(9));
            var 钱袋 = new 天帝通货(链, 1, 2); 链.获得道纹(分叉);
            检查("接口不可改造、分叉不可改造", !钱袋.使用(通货种类.通脉针, 链纹[0], 0, out _) &&
                !钱袋.使用(通货种类.六通玉, 链纹[0], 0, out _) &&
                !钱袋.使用(通货种类.启灵石, 分叉, 0, out _));
            var 恢复 = 天帝道纹.读取存档(链.导出存档());
            检查("分叉分类与固定接口存档往返", 恢复.道纹.Last().分类 == 道纹分类.分叉 &&
                恢复.道纹.Last().接口 == 分叉.接口 && 恢复.道纹.Last().词条.Count == 0);
            var 双槽 = new 天帝道纹(2, 天帝天赋.获取((int)天赋种类.普通人));
            双槽.设置玩家等级(10);
            var 双格 = new[] { new Vector2Int(1, 0), new Vector2Int(1, -1) };
            int[] 双口 = { 24, 6 };
            for (int i = 0; i < 2; i++)
            {
                双槽.解锁格子(双格[i]);
                var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, new System.Random(i + 30), 道纹属性分组.基础);
                纹.接口 = 双口[i]; 纹.属性 = 道纹属性.力量; 纹.数值 = i + 2;
                双槽.获得道纹(纹); 双槽.放置(纹, 双格[i]);
            }
            检查("同圈汇合共享道纹但每弹槽只计一次", 双槽.生效数 == 2 &&
                双槽.弹槽生效数[0] == 2 && 双槽.弹槽生效数[5] == 2 &&
                双槽.生效加成[(int)道纹属性.力量] == 5 &&
                双槽.弹槽加成[0][(int)道纹属性.力量] == 5 && 双槽.弹槽加成[5][(int)道纹属性.力量] == 5);
            foreach (var 定 in 天帝通货.定义.Take(7).Concat(天帝通货.定义.Where(x => x.种类 == 通货种类.问天石)))
            {
                var 图 = 天帝道纹夹具.创建(1); var 纹 = 图.道纹.First(x => x.分类 == 道纹分类.属性);
                typeof(道纹实例).GetProperty("品阶").SetValue(纹, 定.来源.Value); 纹.接口 = 8; 纹.词条.Clear(); 纹.词条.Add(new 道纹词条(道纹属性.力量, 2));
                var 钱 = new 天帝通货(图, 3, 2);
                检查(定.名称 + "升阶赌阶完全保留单接口", 钱.使用(定.种类, 纹, 0, out _) && 纹.接口 == 8 && 钱.数量(定.种类) == 1);
            }
            var 开局 = new 天帝道纹(1, 天帝天赋.获取((int)天赋种类.普通人));
            检查("开局无技能道纹且天赋起点固定右口", 开局.道纹.Count == 0 && 开局.已放置[Vector2Int.zero].接口 == 1 && 开局.技能点 == 1);
            开局.设置玩家等级(50); 检查("源纹按等级解封仍有效", 开局.已放置[Vector2Int.zero].接口 == 63);
        }
        catch (Exception ex) { 结果.错误.Add(ex.ToString()); }
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/接口-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(目录); string 路径 = Path.Combine(目录, "ports-smoke.json");
        File.WriteAllText(路径, JsonUtility.ToJson(结果, true));
        if (结果.失败.Count > 0 || 结果.错误.Count > 0) throw new InvalidOperationException("接口验证失败：" + 路径);
        return 路径;
    }
}
#endif
