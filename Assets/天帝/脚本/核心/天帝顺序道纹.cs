using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 放置时的图编译结果；一段属性只属于其投掷物，功能节点决定下一段的触发时机。
public sealed class 道纹执行段
{
    public int 节点编号, 实际来路 = -1;
    public 道纹功能 功能;
    public 普攻参数 参数;
    public readonly List<道纹执行段> 后续 = new List<道纹执行段>(2);
    public readonly List<int> 来源 = new List<int>();
    public string 路径;
    public bool 空出口;
}

public sealed class 道纹执行计划
{
    public 道纹执行段 起点;
    public int 段数, 功能数, 根弹数, 最多弹体, 起始链路数;
    public readonly List<string> 提示 = new List<string>();
    public readonly List<道纹执行段> 各段 = new List<道纹执行段>();
    public string 摘要
    {
        get
        {
            var b = new StringBuilder("首发 ").Append(根弹数).Append(" 颗 · 顺序功能 ").Append(功能数).Append(" 个");
            if (提示.Count > 0) b.Append("\n链路有提示 · 请查看加成来源");
            else b.Append("\n出口独立属性 · 按触发时机逐段执行");
            return b.ToString();
        }
    }
    public string 详情()
    {
        var b = new StringBuilder(摘要).Append("\n单次释放最多生成 ").Append(天帝数值.取("rune.ordered_functions.projectiles_per_release")).Append(" 个弹体（含后续阶段）。\n");
        foreach (var 段 in 各段)
        {
            b.Append("\n").Append(段.路径).Append("：普通 ").Append(段.参数.普通伤害.ToString("0.##"))
                .Append(" ＋ 五行 ").Append(段.参数.五行额外伤害.ToString("0.##"));
            if (段.功能 != 道纹功能.旧版) b.Append(" → ").Append(段.功能).Append(" #").Append(段.节点编号)
                .Append(" · 本段来路：").Append(天帝道纹.方向名[段.实际来路]);
            else b.Append(" → 投掷物末段");
            b.Append("\n来源 #").Append(string.Join("、#", 段.来源));
        }
        foreach (var t in 提示) b.Append("\n提示：").Append(t);
        return b.ToString();
    }
}

public static class 天帝顺序道纹
{
    public static int 功能数量 => (int)天帝数值.取("rune.ordered_functions.function_count");
    public static float 扩展数值(string 键) => (float)天帝数值.取("rune.ordered_functions.extended." + 键);
    public static float 形态数值(string 键) => (float)天帝数值.取("rune.ordered_functions.attack_shapes." + 键);
    public static bool 攻击形态(道纹功能 f) => f >= 道纹功能.光束 && f <= 道纹功能.地雷;
    public static bool 多弹功能(道纹功能 f) => f >= 道纹功能.扇射 && f <= 道纹功能.背射;
    public static bool 运动修饰(道纹功能 f) => f == 道纹功能.回旋 || f == 道纹功能.波动 || f == 道纹功能.蓄势 || f == 道纹功能.拖尾;
    public static int 运动位(道纹功能 f) => f == 道纹功能.回旋 ? 0 : f == 道纹功能.波动 ? 3 : f == 道纹功能.蓄势 ? 6 : 9;
    public static int 运动层(int 运动, 道纹功能 f) => (运动 >> 运动位(f)) & 7;
    public static int 多弹数量(道纹功能 f) => (int)扩展数值(f == 道纹功能.扇射 ? "fan_count" : f == 道纹功能.环射 ? "ring_count" : f == 道纹功能.十字 ? "cross_count" : "back_count");
    public static float 多弹倍率(道纹功能 f) => 扩展数值(f == 道纹功能.扇射 ? "fan_factor" : f == 道纹功能.环射 ? "ring_factor" : f == 道纹功能.十字 ? "cross_factor" : "back_factor");
    public static float 多弹角度(道纹功能 f, int i) => f == 道纹功能.扇射 ? (i - (多弹数量(f) - 1) * .5f) * 扩展数值("fan_angle") : 360f * i / 多弹数量(f);
    public static int 最少接口数(道纹功能 f) => (int)天帝数值.取("rune.ordered_functions.port_counts_min." + ((int)f - 1));
    public static bool 即时功能(道纹功能 f) => f == 道纹功能.齐射 || f >= 道纹功能.增大 && f <= 道纹功能.减速 || 多弹功能(f) || 运动修饰(f);
    public struct 射击规格 { public 道纹执行段 段; public float 角度; public double 倍率; public bool 并排; }
    public static List<射击规格> 展开射击(道纹执行段 段, Action 齐射 = null, Action 截断 = null)
    {
        var 结果 = new List<射击规格>(32);
        void 走(道纹执行段 s, float 角, double 倍, bool 并排)
        {
            if (结果.Count >= 天帝数值.取("rune.ordered_functions.projectiles_per_release")) { 截断?.Invoke(); return; }
            if (!即时功能(s.功能)) { 结果.Add(new 射击规格 { 段 = s, 角度 = 角, 倍率 = 倍, 并排 = 并排 }); return; }
            if (s.功能 == 道纹功能.齐射) 齐射?.Invoke();
            int n = 多弹功能(s.功能) ? 多弹数量(s.功能) : 1;
            for (int i = 0; i < n; i++) foreach (var 下 in s.后续)
                走(下, 角 + (多弹功能(s.功能) ? 多弹角度(s.功能, i) : 0), 倍 * (多弹功能(s.功能) ? 多弹倍率(s.功能) : 1), 并排 || s.功能 == 道纹功能.齐射);
        }
        走(段, 0, 1, false); return 结果;
    }
    public static IEnumerable<道纹执行段> 展开齐射(道纹执行段 段, Action 触发 = null)
    {
        foreach (var 规格 in 展开射击(段, 触发)) yield return 规格.段;
    }
    public static int 接口数(道纹功能 功能)
    {
        if (功能 < 道纹功能.齐射 || (int)功能 > 功能数量) throw new ArgumentOutOfRangeException(nameof(功能));
        return (int)天帝数值.取("rune.ordered_functions.port_counts." + ((int)功能 - 1));
    }
    public static bool 定义有效(道纹功能 功能, int 接口)
    {
        if (功能 < 道纹功能.齐射 || (int)功能 > 功能数量 || 接口 < 1 || 接口 > 63) return false;
        int 数 = 0; for (int d = 0; d < 6; d++) if ((接口 & (1 << d)) != 0) 数++;
        return 数 >= 最少接口数(功能) && 数 <= 接口数(功能);
    }
    // 旧调用兼容，已保存的入口字段不再参与规则判断。
    public static bool 定义有效(道纹功能 功能, int 旧入口, int 接口) => 定义有效(功能, 接口);
    public static int 调整接口数量(int 口, 道纹功能 功能, System.Random 随机)
    {
        int 数 = 0; for (int d = 0; d < 6; d++) if ((口 & (1 << d)) != 0) 数++;
        int 最少 = 最少接口数(功能), 最多 = 接口数(功能);
        int 目标 = 数 >= 最少 && 数 <= 最多 ? 数 : 随机.Next(最少, 最多 + 1);
        while (数 != 目标)
        {
            var 池 = new List<int>();
            for (int d = 0; d < 6; d++) if (((口 & (1 << d)) != 0) == (数 > 目标)) 池.Add(d);
            int 选 = 池[随机.Next(池.Count)];
            if (数 > 目标) { 口 &= ~(1 << 选); 数--; } else { 口 |= 1 << 选; 数++; }
        }
        return 口;
    }
    // 图鉴示例与既有测试的标准形状，正式生成使用随机方向。
    public static int 固定接口(道纹功能 功能, int 入口)
    {
        if (功能 < 道纹功能.齐射 || (int)功能 > 功能数量 || 入口 < 0 || 入口 > 5) throw new ArgumentOutOfRangeException();
        int 口 = (1 << 入口) | (1 << ((入口 + 3) % 6));
        if (接口数(功能) == 3) 口 |= 1 << ((入口 + 4) % 6);
        return 口;
    }
    public static 道纹属性 兼容属性(道纹功能 功能) => 功能 == 道纹功能.齐射 ? 道纹属性.数量 :
        功能 == 道纹功能.分裂 ? 道纹属性.分裂 : 道纹属性.连锁;
    public static string 功能摘要(道纹功能 功能)
    {
        switch (功能)
        {
            case 道纹功能.齐射: return "3个随机方向接口 · 增加1颗并排投掷物，实际来路之外两口分别计算";
            case 道纹功能.分裂: return "3个随机方向接口 · 命中后两颗子弹各继承50%倍率，分别执行剩余两口链路";
            case 道纹功能.连锁: return "2个随机方向接口 · 命中后自动攻击附近新目标，继承80%倍率并执行另一口链路";
            case 道纹功能.增大: return "随机1～2口 · 投掷物体型与命中半径×1.5，立即执行后续链路";
            case 道纹功能.缩小: return "随机1～2口 · 投掷物体型与命中半径×2/3，立即执行后续链路";
            case 道纹功能.加速: return "随机1～2口 · 投掷物飞行速度×1.5，立即执行后续链路";
            case 道纹功能.减速: return "随机1～2口 · 投掷物飞行速度×2/3，立即执行后续链路";
            case 道纹功能.穿透: return "随机1～2口 · 穿过1名敌人，命中第2名后执行后续；未命中第2名则不触发";
            case 道纹功能.扇射: return "随机1～2口 · 3弹扇形，各相差25°、伤害65%，分别复制后续链路";
            case 道纹功能.环射: return "随机1～2口 · 8弹环形，每弹伤害25%，分别复制后续链路";
            case 道纹功能.十字: return "随机1～2口 · 4弹十字，每弹伤害45%，分别复制后续链路";
            case 道纹功能.背射: return "随机1～2口 · 前后各1弹，每弹伤害80%，分别复制后续链路";
            case 道纹功能.折返: return "随机1～2口 · 去程穿过敌人，飞到尽头沿原路折返一次，返回后执行后续；同敌不重复命中";
            case 道纹功能.回旋: return "随机1～2口 · 后续投掷物每秒转60°，最多转180°，不追踪";
            case 道纹功能.波动: return "随机1～2口 · 后续弹左右摆动，幅度0.6米、波长4米，不追踪";
            case 道纹功能.弹墙: return "随机1～2口 · 遇障碍反弹一次，反弹后继续后续链路；反弹前命中也继续飞行";
            case 道纹功能.跃迁: return "随机1～2口 · 向前跃迁4米，途中不命中；落点必须可通行，随后执行后续";
            case 道纹功能.延时: return "随机1～2口 · 等待0.4秒，再执行后续链路";
            case 道纹功能.停驻: return "随机1～2口 · 首次命中后停留0.6秒，再向前执行后续";
            case 道纹功能.蓄势: return "随机1～2口 · 后续弹每飞行1米增加8%伤害，最多180%";
            case 道纹功能.爆破: return "随机1～2口 · 命中后2米爆炸，对其它敌人造成50%伤害，再执行后续";
            case 道纹功能.震荡: return "随机1～2口 · 命中0.5秒后在原地释放2.5米脉冲、60%伤害，再执行后续";
            case 道纹功能.拖尾: return "随机1～2口 · 后续弹每1米留下0.7米伤害区，持续1.2秒，每0.3秒造成15%伤害";
            case 道纹功能.击退: return "随机1～2口 · 命中后向前击退1.5米，再执行后续；强敌与BOSS减弱";
            case 道纹功能.牵引: return "随机1～2口 · 命中后将3米内敌人拉近1.5米，再执行后续；不穿障碍";
            case 道纹功能.束缚: return "随机1～2口 · 命中敌人定身0.8秒，可继续攻击；强敌与BOSS减弱";
            case 道纹功能.烙印: return "随机1～2口 · 命中后留下跟随目标的标记，1秒后1.8米爆炸、60%伤害，再执行后续";
            case 道纹功能.陨落: return "随机1～2口 · 锁定目标当前落点，预警0.5秒后1.5米范围落击，再执行后续；移动可躲";
            case 道纹功能.光束: return "随机1～2口 · 10米直线光束、120%伤害，沿途每敌一次，终点续接";
            case 道纹功能.刃波: return "随机1～2口 · 2.4米宽月牙刀气推进8米、90%伤害，每敌一次，结束续接";
            case 道纹功能.地刺: return "随机1～2口 · 每0.12秒向前冒出一根地刺，共6根、每敌80%伤害一次，最后一根续接";
            case 道纹功能.剑雨: return "随机1～2口 · 固定落点预警0.4秒，6剑每0.15秒落下，每剑35%伤害、半径0.6米，最后一剑续接";
            case 道纹功能.旋刃: return "随机1～2口 · 跟随玩家在2米半径旋转一圈、1.2秒、100%伤害，每敌一次，结束续接";
            case 道纹功能.灵鞭: return "随机1～2口 · 4米长鞭在0.45秒横扫120°、110%伤害，每敌一次，鞭梢续接";
            case 道纹功能.飞轮: return "随机1～2口 · 半径0.75米锯轮推进8米、70%弹速，同敌每0.2秒可受35%伤害，结束续接";
            case 道纹功能.游龙: return "随机1～2口 · 3米长灵龙固定朝向推进10米、120%伤害，每敌一次，龙尾到终点续接";
            case 道纹功能.灵网: return "随机1～2口 · 前进6米逐渐展开到4米宽、80%伤害，每敌一次，展开完从中心续接";
            case 道纹功能.地雷: return "随机1～2口 · 布置0.4秒后敌人进入1.4米触发2米爆炸、160%伤害，触发才续接；4秒无人触发消散";
            default: return "旧版形态词条";
        }
    }
    public static bool 本路使用顺序(天帝道纹 网, int 通路)
    {
        foreach (var 纹 in 网.道纹) if (纹.是顺序功能 && 网.弹槽道纹[通路].Contains(纹.编号)) return true;
        return false;
    }
    // 无防御密集目标的纸面预算，兄弟弹允许同目标，祖先命中过的目标从该分支扣除。
    public static double 伤害预算(道纹执行计划 计划, int 目标数, int 起始数, bool 计入暴击 = false)
    {
        if (计划.各段.Exists(s => s.功能 > 道纹功能.穿透)) return 天帝扩展道纹预算.计算(计划, 目标数, 起始数, 计入暴击);
        var 队 = new Queue<(道纹执行段 段, int 剩余, double 倍率, bool 旧子)>(); int 数 = 0; double 总 = 0;
        void 发(道纹执行段 段, int 剩余, double 倍率, bool 旧子 = false)
        {
            if (即时功能(段.功能)) { foreach (var 下 in 段.后续) 发(下, 剩余, 倍率); return; }
            for (int i = 0; i < 段.参数.数量 && 数 < 天帝数值.取("rune.ordered_functions.projectiles_per_release"); i++)
            { 数++; 队.Enqueue((段, 剩余, 倍率, 旧子)); }
        }
        void 穿透后续(道纹执行段 段, int 剩余, double 倍率)
        {
            while (即时功能(段.功能) && 段.功能 != 道纹功能.齐射)
            { if (段.后续.Count == 0) return; 段 = 段.后续[0]; }
            if (段.功能 == 道纹功能.齐射 || 段.功能 == 道纹功能.穿透) { 发(段, 剩余, 倍率); return; }
            if (段.功能 == 道纹功能.分裂 || 段.功能 == 道纹功能.连锁)
            {
                double f = 天帝数值.取(段.功能 == 道纹功能.分裂 ? "rune.ordered_functions.split_factor" : "rune.ordered_functions.chain_factor");
                foreach (var 下 in 段.后续) 发(下, 剩余, 倍率 * f);
            }
            else for (int i = 1; i <= Math.Min(剩余, 段.参数.连锁); i++)
                总 += 段.参数.伤害 * (计入暴击 ? 1 + 段.参数.暴击率 * (段.参数.暴击倍率 - 1) : 1) * 倍率 * Math.Pow(天帝数值.取("shape.chain_factor"), i);
        }
        for (int i = 0; i < 起始数; i++) 发(计划.起点, Math.Max(0, 目标数), 1);
        while (队.Count > 0)
        {
            var x = 队.Dequeue(); if (x.剩余 <= 0) continue;
            var 参数 = x.段.参数;
            double 单弹 = 参数.伤害 * (计入暴击 ? 1 + 参数.暴击率 * (参数.暴击倍率 - 1) : 1);
            总 += 单弹 * x.倍率;
            if (x.旧子) continue;
            if (x.段.功能 == 道纹功能.穿透)
            {
                int 命中数 = Math.Min(x.剩余, 1 + (int)天帝数值.取("rune.ordered_functions.pierce_count"));
                总 += 单弹 * x.倍率 * (命中数 - 1);
                if (命中数 > 天帝数值.取("rune.ordered_functions.pierce_count") && x.段.后续.Count > 0)
                    穿透后续(x.段.后续[0], x.剩余 - 命中数, x.倍率);
                continue;
            }
            int 余 = x.剩余 - 1;
            if (参数.溅射半径 > 0)
            {
                int 溅 = Math.Min(余, (int)天帝数值.取("shape.splash_targets_max"));
                总 += 单弹 * x.倍率 * 溅 * 天帝数值.取("shape.splash_factor"); 余 -= 溅;
            }
            if (余 > 0) for (int i = 0; i < 参数.分裂; i++)
                发(new 道纹执行段 { 参数 = 参数 }, 余, x.倍率 * 天帝数值.取("shape.split_factor"), true);
            if (x.段.功能 == 道纹功能.分裂 || x.段.功能 == 道纹功能.连锁)
            {
                double 倍 = 天帝数值.取(x.段.功能 == 道纹功能.分裂 ? "rune.ordered_functions.split_factor" : "rune.ordered_functions.chain_factor");
                foreach (var 下 in x.段.后续) 发(下, 余, x.倍率 * 倍);
            }
            else for (int i = 1; i <= Math.Min(余, 参数.连锁); i++) 总 += 单弹 * x.倍率 * Math.Pow(天帝数值.取("shape.chain_factor"), i);
        }
        return 总;
    }
    public static 道纹执行计划 编译(天帝道纹 网, 天帝主角属性 人, int 通路)
    {
        var p = new 道纹执行计划(); int 上限 = (int)天帝数值.取("rune.ordered_functions.max_compiled_segments"), 待编译 = 1;
        var 源 = 网.已放置[Vector2Int.zero]; var 初值 = new double[天帝道纹属性.数量];
        if (!源.是天赋) 初值[(int)源.属性] = 源.实际数值;
        Vector2Int? 对接(Vector2Int 格, int d)
        {
            if (!网.已放置.TryGetValue(格, out var 纹) || !纹.允许传出(d)) return null;
            var 下 = 格 + 天帝道纹.邻向[d];
            return 天帝道纹.可传导(格, 下) && 网.已放置.TryGetValue(下, out var 邻) && 邻.允许接入((d + 3) % 6) ? 下 : (Vector2Int?)null;
        }
        道纹执行段 走(Vector2Int? 起格, int 起入口, double[] 继承, HashSet<Vector2Int> 上游, List<int> 上游来源, double 体型, double 弹速, int 运动, int 深度, string 路)
        {
            待编译--;
            var 段 = new 道纹执行段 { 路径 = 路, 空出口 = !起格.HasValue }; var 加 = (double[])继承.Clone(); 段.来源.AddRange(上游来源);
            // 继承本投掷物已经走过的节点，跨功能阶段也不清空；同圈闭环不能重复加成或再次触发功能。
            // 兄弟投掷物独立计算自己的链路，均只继承共同上游的访问记录。
            var 见 = new HashSet<Vector2Int>(上游); var 候选 = new Dictionary<int, (道纹实例 纹, int 来路)>();
            var 队 = new Queue<(Vector2Int 格, int 来路)>(); if (起格.HasValue && !见.Contains(起格.Value)) 队.Enqueue((起格.Value, 起入口));
            while (队.Count > 0)
            {
                var 项 = 队.Dequeue(); var 格 = 项.格;
                if (!见.Add(格) || !网.已放置.TryGetValue(格, out var 纹)) continue;
                if (纹.是顺序功能) { 候选[纹.编号] = (纹, 项.来路); continue; }
                if (!纹.是源纹)
                {
                    段.来源.Add(纹.编号);
                    foreach (var 词 in 网.读取有效词条(纹)) if ((int)词.属性 >= 0 && (int)词.属性 < 加.Length) 加[(int)词.属性] += 天帝数值.正值(词.实际数值);
                }
                for (int d = 0; d < 6; d++) { var 下 = 对接(格, d); if (下.HasValue && !见.Contains(下.Value)) 队.Enqueue((下.Value, (d + 3) % 6)); }
            }
            段.参数 = 普攻参数.从加成(网, 人, 通路, 加, true);
            段.参数.体型倍率 = (float)体型; 段.参数.弹速倍率 = (float)弹速; 段.参数.弹速 *= (float)弹速;
            段.参数.运动功能 = 运动;
            p.各段.Add(段); p.段数++;
            if (候选.Count > 1) { p.提示.Add("同一投掷物同时接到多个下一功能，请用齐射或分裂明确分支；该段只发基础弹。"); return 段; }
            if (候选.Count == 0) return 段;
            道纹实例 功 = null; int 来路 = -1; foreach (var v in 候选.Values) { 功 = v.纹; 来路 = v.来路; }
            var 出口 = new List<int>(2);
            // 不指定物品入口，按本次真实来路排除一口，剩余物理接口分别承载弹体链路。
            for (int i = 0; i < 6; i++) { int d = (来路 + 3 + i) % 6; if (d != 来路 && 功.有接口(d)) 出口.Add(d); }
            bool 末端修饰 = 出口.Count == 0 && (即时功能(功.功能) && 功.功能 != 道纹功能.齐射 || 功.功能 >= 道纹功能.折返);
            int 口数 = 出口.Count + (末端修饰 ? 1 : 0);
            if (深度 >= 天帝数值.取("rune.ordered_functions.max_function_depth") || p.段数 + 待编译 + 口数 > 上限)
            { p.提示.Add("后续功能超过编译预算，已在此停止扩展。"); return 段; }
            段.功能 = 功.功能; 段.节点编号 = 功.编号; 段.实际来路 = 来路; p.功能数++;
            if (功.功能 == 道纹功能.增大 || 功.功能 == 道纹功能.缩小)
                体型 = 天帝数值.夹(体型 * 天帝数值.取(功.功能 == 道纹功能.增大 ? "rune.ordered_functions.grow_factor" : "rune.ordered_functions.shrink_factor"),
                    天帝数值.取("rune.ordered_functions.scale_min"), 天帝数值.取("rune.ordered_functions.scale_max"));
            if (功.功能 == 道纹功能.加速 || 功.功能 == 道纹功能.减速)
                弹速 = 天帝数值.夹(弹速 * 天帝数值.取(功.功能 == 道纹功能.加速 ? "rune.ordered_functions.speed_up_factor" : "rune.ordered_functions.speed_down_factor"),
                    天帝数值.取("rune.ordered_functions.speed_factor_min"), 天帝数值.取("rune.ordered_functions.speed_factor_max"));
            待编译 += 口数;
            if (运动修饰(功.功能) && 运动层(运动, 功.功能) < 扩展数值("motion_layers_max")) 运动 += 1 << 运动位(功.功能);
            for (int i = 0; i < 出口.Count; i++)
            {
                int d = 出口[i]; var 下 = 对接(功.格子.Value, d);
                if (下.HasValue && 见.Contains(下.Value)) { p.提示.Add("回接已经执行的节点，后续已截断。"); 下 = null; }
                段.后续.Add(走(下, (d + 3) % 6, 加, 见, 段.来源, 体型, 弹速, 运动, 深度 + 1, 路 + "/" + 天帝道纹.方向名[d]));
            }
            if (末端修饰) 段.后续.Add(走(null, -1, 加, 见, 段.来源, 体型, 弹速, 运动, 深度 + 1, 路 + "/末端投掷物"));
            return 段;
        }
        p.起点 = 走(对接(Vector2Int.zero, 通路), (通路 + 3) % 6, 初值, new HashSet<Vector2Int> { Vector2Int.zero }, new List<int>(), 1, 1, 0, 0, "本路");
        int 数(道纹执行段 段, bool 首发)
        {
            if (首发 && !即时功能(段.功能)) return 段.参数.数量;
            int n = 即时功能(段.功能) ? 0 : 1;
            foreach (var 下 in 段.后续) n = Math.Min(100000, n + 数(下, 首发));
            return Math.Min(100000, 即时功能(段.功能) ? n * (多弹功能(段.功能) ? 多弹数量(段.功能) : 1) : n * 段.参数.数量);
        }
        int 天赋数 = 天帝天赋效果.射击数量(网.天赋, 1, true);
        p.起始链路数 = 天赋数;
        p.根弹数 = Math.Min((int)天帝数值.取("rune.ordered_functions.projectiles_per_release"), 数(p.起点, true) * 天赋数);
        p.最多弹体 = 数(p.起点, false) * 天赋数;
        if (p.最多弹体 > 天帝数值.取("rune.ordered_functions.projectiles_per_release")) p.提示.Add("弹体总量超过单次32预算；按出口顺序保留可执行阶段。");
        return p;
    }
}
