#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class 天帝第三轮战斗验证
{
    static 天帝真实数值验证.报告 结果;
    static readonly BindingFlags 私有实例 = BindingFlags.Instance | BindingFlags.NonPublic;
    static void 检查(string 名称, bool 通过) => (通过 ? 结果.通过 : 结果.失败).Add(名称);
    public static string 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式验证隔离战斗模型。");
        结果 = new 天帝真实数值验证.报告();
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/第三轮战斗-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(目录);
        try { 自动落点(8); 自动落点(9); 余响时序(); 技能名称顺序(); }
        catch (Exception 异常) { 结果.错误.Add(异常.ToString()); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        Debug.Log("第三轮战斗验证：" + 目录 + "；通过 " + 结果.通过.Count + "，失败 " + 结果.失败.Count + "，错误 " + 结果.错误.Count);
        return 目录;
    }
    static void 自动落点(int 特性)
    {
        var 构筑 = typeof(天帝剩余概念验收).GetMethod("主动特性构筑", BindingFlags.Static | BindingFlags.NonPublic);
        if (构筑 == null) throw new MissingMethodException("主动特性构筑夹具不存在");
        var 网 = (天帝道纹)构筑.Invoke(null, new object[] { 特性 });
        var 地 = new 天帝战斗地图(42, true);
        string 名称 = 天帝特性道纹.名称(道纹分类.特性, 特性);
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通) { 自动攻击启用 = false };
            try
            {
                战.设置演示靶(new[] { Vector2.right * 6, Vector2.right * 8 });
                天帝战斗扩展验证.设(战, "演示模式", false);
                foreach (var 敌 in 战.敌人) 天帝战斗扩展验证.设(敌, "登场剩余秒", 100f);
                for (int 步 = 0; 步 < 1200; 步++) 战.推进(地.出生位置, .025f);
                检查(名称 + "-等待期间无自动射击或特性施放", 战.普通释放次数 == 0 && 战.特性施放次数 == 0);
                检查(名称 + "-夹具保留六米和八米固定目标",
                    战.敌人.Count == 2 && Vector2.Distance(战.敌人[0].位置, 地.出生位置 + Vector2.right * 6) < .001f
                    && Vector2.Distance(战.敌人[1].位置, 地.出生位置 + Vector2.right * 8) < .001f);
                战.自动攻击启用 = true;
                for (int 步 = 0; 步 < 40 && 战.普通释放次数 == 0; 步++) 战.推进(地.出生位置, .025f);
                var 效果 = 战.特性效果列表.Where(项 => 项.特性 == 特性).ToArray();
                检查(名称 + "-真实自动前摇完成并触发特性", 战.普通释放次数 == 1 && 战.特性施放次数 > 0 && 效果.Length > 0);
                检查(名称 + "-至少一枚落在最近六米目标脚下",
                    效果.Any(项 => Vector2.Distance(项.位置, 战.敌人[0].位置) < .001f));
                检查(名称 + "-落点不受手动四米限制",
                    效果.Length > 0 && 效果.All(项 => Vector2.Distance(项.位置, 地.出生位置) > 5.9f
                        && 战.敌人.Any(敌 => Vector2.Distance(项.位置, 敌.位置) < .001f)));
                var 手动字段 = typeof(天帝战斗系统.特性战斗效果).GetField("手动瞄准", 私有实例);
                检查(名称 + "-自动特性保留自动索敌标记",
                    手动字段 != null && 效果.Length > 0 && 效果.All(项 => !(bool)手动字段.GetValue(项)));
            }
            finally { 战.清理特性战斗(); 战.战术.清理(); }
        }
    }
    static void 余响时序()
    {
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.余响));
        网.设置玩家等级(50);
        var 地 = new 天帝战斗地图(42, true);
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通) { 自动攻击启用 = false };
            try
            {
                战.设置演示靶(Array.Empty<Vector2>());
                double 时钟 = 0;
                var 发射时间 = new List<double>();
                var 发射方向 = new List<Vector2>();
                战.射击释放 += 方向 => { 发射时间.Add(时钟); 发射方向.Add(方向); };
                void 推进一步() { 时钟 += .025; 战.推进(地.出生位置, .025f); }
                bool 五次成功 = true;
                for (int 次 = 0; 次 < 5; 次++)
                {
                    人.设置当前资源(人.血量, 人.灵力, 人.灵气护盾);
                    五次成功 &= 战.尝试释放技能(0, Vector2.up, 地.出生位置) == 战斗操作结果.成功;
                    for (int 步 = 0; 步 < 40 && 战.普通释放次数 <= 次; 步++) 推进一步();
                    if (次 < 4)
                        for (int 步 = 0; 步 < 100 && 战.技能冷却剩余 > .00001f; 步++) 推进一步();
                }
                检查("余响-五次真实前摇射击成功且尚无回响", 五次成功 && 战.普通释放次数 == 5 && 战.回响次数 == 0 && 发射时间.Count == 5);
                double 第五次 = 发射时间.Count >= 5 ? 发射时间[4] : double.NaN;
                for (int 步 = 0; 步 < 7; 步++) 推进一步();
                检查("余响-发射后0.175秒不提前回响", 战.回响次数 == 0 && 发射时间.Count == 5 && Math.Abs(时钟 - 第五次 - .175) < .00001);
                推进一步();
                检查("余响-发射后0.200秒追加一次", 战.回响次数 == 1 && 发射时间.Count == 6
                    && Math.Abs(发射时间[5] - 第五次 - 天帝数值.取("talents.echo_delay")) < .00001);
                检查("余响-额外发射不推进根计数并继承方向通路",
                    战.普通释放次数 == 5 && 战.通路释放次数[0] == 5 && 战.当前通路 == 0
                    && 发射方向.Count == 6 && 发射方向.All(向 => Vector2.Dot(向, Vector2.up) > .999f));
                int 回响 = 战.回响次数;
                for (int 步 = 0; 步 < 8; 步++) 推进一步();
                检查("余响-同一请求不重复释放", 战.回响次数 == 回响 && 发射时间.Count == 6);
            }
            finally { 战.清理特性战斗(); 战.战术.清理(); }
        }
    }
    static void 技能名称顺序()
    {
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人)); 网.设置玩家等级(50);
        var 后 = 天帝道纹生成.创建(100, 道纹分类.功能, 道纹品阶.稀有, new System.Random(42));
        var 前 = 天帝道纹生成.创建(101, 道纹分类.功能, 道纹品阶.稀有, new System.Random(43));
        typeof(道纹实例).GetProperty("功能").SetValue(后, 道纹功能.穿透);
        typeof(道纹实例).GetProperty("功能").SetValue(前, 道纹功能.加速);
        后.接口 = 前.接口 = (1 << 0) | (1 << 3);
        网.获得道纹(后); 网.获得道纹(前);
        网.解锁格子(new Vector2Int(1, 0)); 网.解锁格子(new Vector2Int(2, 0));
        网.放置(前, new Vector2Int(1, 0)); 网.放置(后, new Vector2Int(2, 0));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 战 = new 天帝战斗系统(new 天帝战斗地图(42, true), 网, 人, 战斗难度.普通);
            try
            {
                检查("技能名称-库存先获得末段仍显示真实起始功能", 战.技能名称(0) == "加速");
                网.道纹.Reverse();
                检查("技能名称-库存排序不改变链路身份", 战.技能名称(0) == "加速");
            }
            finally { 战.清理特性战斗(); 战.战术.清理(); }
        }
    }
}
#endif
