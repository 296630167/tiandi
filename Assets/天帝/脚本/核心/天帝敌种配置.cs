using System;
using System.Collections.Generic;
using UnityEngine;

// 物种与品质独立。参数、名称、波次与形态阈值只读正式JSON导出。
public static class 天帝敌种配置
{
    public static float 取(string 键) => (float)天帝数值.取("battle_content." + 键);
    public static float 物种(int 种, string 键) => 取("species." + 种 + "." + 键);
    public static int 角色(int 种) => (int)物种(种, "role");
    public static bool 已解锁(int 种, int 地图等级) => 地图等级 >= 物种(种, "unlock");
    public static string 名称(int 种) => 天帝数值配置.取文本("battle_content.species." + 种 + ".name");
    public static string 形态名(int 形) => 天帝数值配置.取文本("battle_content.boss_forms." + (形 - 1));
    public static int 形态数(int 等级)
    { int n = 1; for (int i = 1; i < 5; i++) if (等级 >= 取("boss.unlock." + i)) n++; return n; }
    public static int 形态(int 等级, float 生命比)
    {
        int n = 形态数(等级), 形 = 1;
        for (int i = 0; i < n - 1; i++) if (生命比 <= 取("boss.thresholds." + (n - 1) + "." + i)) 形++;
        return 形;
    }
    public static string 美术编号(战斗敌人 敌)
        => 敌.物种 == 20 ? "BTN04" : 敌.物种 == 21 ? "BTN09" : 敌.物种 >= 15 ? "BTN01" : 敌.物种 == 14 ? "BTB" + 敌.形态.ToString("00") : 敌.物种 < 12 ? "BTN" + (敌.物种 + 1).ToString("00") : "BTE" + (敌.物种 - 11).ToString("00");
    public static bool 属性狼(int 种) => 种 >= 15 && 种 <= 19;
    public static Color 立绘颜色(int 种)
    {
        switch (种)
        {
            case 15: return new Color(1, .86f, .54f);
            case 16: return new Color(.6f, 1, .65f);
            case 17: return new Color(.55f, .86f, 1);
            case 18: return new Color(1, .55f, .4f);
            case 19: return new Color(.88f, .74f, .52f);
            case 20: return new Color(.9f, .7f, 1);
            case 21: return new Color(.65f, .7f, 1);
            default: return Color.white;
        }
    }
    public static int[] 区域队列(int 等级, int 种子, int 数量)
    {
        var r = new System.Random(unchecked(种子 ^ 0x781C));
        var 属 = new List<int>(); var 其余 = new List<int>();
        for (int i = 1; i < 22; i++) if ((i < 12 || i >= 15) && 已解锁(i, 等级))
        { if (属性狼(i)) 属.Add(i); else 其余.Add(i); }
        var 队 = new int[数量]; int 属序 = 0, 余序 = 0;
        int 普狼 = (int)Math.Round(数量 * 取("growth.wolf_weight"));
        int 属狼 = 属.Count == 0 ? 0 : (int)Math.Round(数量 * 取("growth.element_wolf_weight"));
        for (int i = 0; i < 数量; i++)
            队[i] = i < 普狼 ? 0 : i < 普狼 + 属狼 ? 属[属序++ % 属.Count] : 其余.Count == 0 ? 属.Count == 0 ? 0 : 属[属序++ % 属.Count] : 其余[余序++ % 其余.Count];
        for (int i = 队.Length - 1; i > 0; i--) { int j = r.Next(i + 1); int a = 队[i]; 队[i] = 队[j]; 队[j] = a; }
        return 队;
    }
    public static int 分组(int 种)
    { int 角 = 角色(种); return 角 == 1 ? 0 : 角 == 2 ? 1 : 角 == 3 ? 2 : 角 == 4 ? 3 : -1; }
    static readonly int[][] 分组候选 = { new[] { 0, 1 }, new[] { 2, 3 }, new[] { 4, 5, 11 }, new[] { 8, 9 }, new[] { 10 }, new[] { 6, 7 } };
    public static int[] 普通队列(int 等级, int 种子)
    {
        var r = new System.Random(unchecked(种子 ^ 0x514C));
        var 队 = new List<int>(60);
        for (int 波 = 0; 波 < 5; 波++)
        {
            var 本波 = new List<int>();
            for (int 组 = 0; 组 < 6; 组++)
            {
                int 数 = (int)取("waves." + 波 + "." + 组);
                var 可用 = new List<int>();
                foreach (int 种 in 分组候选[组])
                    if (已解锁(种, 等级) && (组 != 5 || 种 == (波 % 2 == 1 ? 6 : 7))) 可用.Add(种);
                int 偏移 = 可用.Count == 0 ? 0 : r.Next(可用.Count);
                for (int i = 0; i < 数; i++) 本波.Add(可用.Count == 0 || 等级 < 5 && 波 == 0 ? 0 : 可用[(偏移 + i) % 可用.Count]);
            }
            // 在同组名额里轮换各物种，避免高档一种怪长期不出现。
            for (int i = 本波.Count - 1; i > 0; i--) { int j = r.Next(i + 1); int a = 本波[i]; 本波[i] = 本波[j]; 本波[j] = a; }
            队.AddRange(本波);
        }
        return 队.ToArray();
    }
    public static 战斗伤害包 伤害包(战斗敌人 敌, int 技, double 倍率 = -1)
    {
        var s = 敌技能.读取(技); double u = 倍率 < 0 ? s.倍率 : 倍率;
        double x = s.属性比; int 属 = s.元素;
        if (敌.物种 == 14 && 敌.形态 >= 3 && 技 != 1)
        { x = Math.Max(x, 取("boss.element_fraction_min")); 属 = 敌.形态 == 3 ? 1 : 敌.形态 == 4 ? 2 : 敌.已攻击次数 % 5; }
        // 同档标准玩家归一化，不读取真实玩家构筑；避免物理改属性造成免费增伤。
        int 品质 = (int)敌.布点.级别;
        double ad = 天帝数值.防御留存(天帝数值配置.敌人(敌.地图档位, 品质, 5), 敌.等级);
        double ar = 天帝数值配置.敌人(敌.地图档位, 品质, 6);
        double total = u * 敌.攻击力 * ad / ((1 - x) * ad + x * (1 - ar));
        double e = total * x;
        var f = new 五行伤害分量(属 == 0 ? e : 0, 属 == 1 ? e : 0, 属 == 2 ? e : 0, 属 == 3 ? e : 0, 属 == 4 ? e : 0);
        return new 战斗伤害包(total - e, e, 敌.等级, 1, 1, 1, 1, f, 敌);
    }
}
public enum 敌技能类型 { 单击, 扇面, 飞弹, 投掷, 直线, 冲锋, 跳跃, 自周, 地面, 治疗, 护盾, 归潮 }
public sealed class 敌技能
{
    static readonly Dictionary<int, 敌技能> 缓存 = new Dictionary<int, 敌技能>();
    public int 编号, 元素, 数量, 效果;
    public 敌技能类型 类型;
    public float 倍率, 属性比, 距离, 宽度, 前摇, 后摇, 周期, 速度;
    public bool 辅助 => 类型 == 敌技能类型.治疗 || 类型 == 敌技能类型.护盾;
    public bool 大招 => 类型 == 敌技能类型.冲锋 || 类型 == 敌技能类型.跳跃 || 类型 == 敌技能类型.自周 || 类型 == 敌技能类型.归潮 || 类型 == 敌技能类型.投掷;
    public string 名称 => 天帝数值配置.取文本("battle_content.skills." + (编号 - 1) + ".name");
    public static 敌技能 读取(int id)
    {
        if (缓存.TryGetValue(id, out var s)) return s;
        float P(string k) => 天帝敌种配置.取("skills." + (id - 1) + "." + k);
        s = new 敌技能 { 编号 = id, 类型 = (敌技能类型)P("type"), 元素 = (int)P("element"), 数量 = (int)P("count"), 效果 = (int)P("effect"),
            倍率 = P("damage"), 属性比 = P("element_fraction"), 距离 = P("range"), 宽度 = P("width"), 前摇 = P("windup"), 后摇 = P("recovery"), 周期 = P("period"), 速度 = P("speed") };
        缓存.Add(id, s); return s;
    }
}
