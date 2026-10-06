using System;
using System.Collections.Generic;

public enum 天赋种类 { 万钧, 双生矢, 续雷, 广域, 灵海, 铁骨, 凝光, 逐风, 逆命, 余响, 普通人, 穿越者, 天命之子 }

public sealed class 天赋定义
{
    public 天赋种类 种类 { get; }
    public int 编号 => (int)种类;
    public string 名称 => 种类.ToString();
    public string 效果 { get; }
    public string 说明 { get; }
    public string 倾向 { get; }
    internal 天赋定义(天赋种类 种类, string 效果, string 说明, string 倾向)
    { this.种类 = 种类; this.效果 = 效果; this.说明 = 说明; this.倾向 = 倾向; }
    public string 详情文字() => 名称 + " · 天赋道纹\n" + 效果 + "\n" + 说明 + "\n构筑倾向：" + 倾向 +
        "\n开局只能选择一枚，本局不可替换。\n固定中心，不进入普通候选与品阶抽取。";
}

public static class 天帝天赋
{
    static readonly 天赋定义[] 定义 =
    {
        new 天赋定义(天赋种类.万钧, "全局力量加成翻倍", "初始基础力量不翻倍，所有来源的正向力量加成×2。", "力量成长"),
        new 天赋定义(天赋种类.双生矢, "射击基础数量 +1", "每次正常射击额外一枚灵力弹；分裂或其他衍生弹体不再增加。", "射击与数量"),
        new 天赋定义(天赋种类.续雷, "灵力弹连锁次数 +1", "为灵力弹增加一次目标连锁；名称不代表已解锁雷元素效果。", "连锁清群"),
        new 天赋定义(天赋种类.广域, "范围属性作用半径 +30%", "接通范围属性后扩大半径，不凭空增加范围或伤害。", "范围覆盖"),
        new 天赋定义(天赋种类.灵海, "最大灵力 +50%", "在属性计算完成后提高灵力上限，不附带回复。", "灵力储备"),
        new 天赋定义(天赋种类.铁骨, "总防御翻倍", "属性计算后的防御×2；不改变伤害减免公式。", "防御成长"),
        new 天赋定义(天赋种类.凝光, "额外护盾上限 = 智力×3", "使用最终智力增加最大灵气护盾，不附带自动回复。", "智力与护盾"),
        new 天赋定义(天赋种类.逐风, "移动与跑步速度 +20%", "两种移动速度×1.2，不增加攻击速度或弹体速度。", "走位与穿梭"),
        new 天赋定义(天赋种类.逆命, "缺失血量增伤，最多 +60%", "增伤=缺失血量比例×60%；半血+30%，满血无加成。", "低血量输出"),
        new 天赋定义(天赋种类.余响, "每第5次射击追加一次回响", "0.2秒后额外射出灵力弹，不消耗灵力；回响与衍生攻击不推进计数。", "高频射击"),
        new 天赋定义(天赋种类.普通人, "只能勉强射出一丝灵力", "没有额外天赋加成；默认灵力弹基础伤害1，接通道纹后获得加成。", "从零修行"),
        new 天赋定义(天赋种类.穿越者, "开局额外获得3技能点", "异界来客，先人三步。确认后一次性获得3点，不提高等级或提前解封接口。", "提前铺设路线"),
        new 天赋定义(天赋种类.天命之子, "爆率×1.5，道纹品阶幸运", "道纹、材料和灵石爆率×1.5（最高100%），数量不翻倍；道纹品阶抽两次取高，只得一枚。不影响天赋刷新。", "幸运寻宝")
    };
    public static IReadOnlyList<天赋定义> 全部 { get; } = Array.AsReadOnly(定义);
    public static 天赋定义 获取(int 编号) => 编号 >= 0 && 编号 < 定义.Length ? 定义[编号] : null;
}

// 均匀抽取五枚不重复天赋；已确认后拒绝刷新及重选。
public sealed class 天帝天赋池
{
    readonly Random 随机;
    readonly List<天赋定义> 当前 = new List<天赋定义>();
    public IReadOnlyList<天赋定义> 候选 { get; }
    public 天赋定义 已选天赋 { get; private set; }
    public int 轮次 { get; private set; }
    public int 刷新次数 => Math.Max(0, 轮次 - 1);
    public 天帝天赋池(int 种子) { 随机 = new Random(种子); 候选 = 当前.AsReadOnly(); 刷新(); }
    public static 天帝天赋池 从已选天赋恢复(int 编号)
    {
        var 天赋 = 天帝天赋.获取(编号) ?? throw new ArgumentOutOfRangeException(nameof(编号));
        return new 天帝天赋池(Environment.TickCount) { 已选天赋 = 天赋 };
    }
    public bool 刷新()
    {
        if (已选天赋 != null) return false;
        var 编号 = new int[天帝天赋.全部.Count]; for (int i = 0; i < 编号.Length; i++) 编号[i] = i;
        for (int i = 编号.Length - 1; i > 0; i--) { int j = 随机.Next(i + 1); int t = 编号[i]; 编号[i] = 编号[j]; 编号[j] = t; }
        当前.Clear(); for (int i = 0; i < 5; i++) 当前.Add(天帝天赋.获取(编号[i]));
        轮次++; return true;
    }
    public bool 确认(int 编号, int 候选轮次)
    {
        if (已选天赋 != null || 候选轮次 != 轮次) return false;
        var 天赋 = 天帝天赋.获取(编号);
        if (天赋 == null || !当前.Contains(天赋)) return false;
        已选天赋 = 天赋; return true;
    }
}

// 技能尚未接入战斗；调用方按技能能力声明使用这些效果入口。
public static class 天帝天赋效果
{
    public static int 开局技能点(天赋定义 天赋) => 是(天赋, 天赋种类.穿越者) ? (int)天帝数值.取("talents.extra_skill_points") : 0;
    // 概率使用0至1；只用于未来实际掉落判定，不用于开局天赋和演示候选。
    public static float 掉落概率(天赋定义 天赋, float 原概率)
        => (float)Math.Min(1, (double)Math.Min(1, 有效(原概率)) * (是(天赋, 天赋种类.天命之子) ? 天帝数值.取("talents.drop_multiplier") : 1));
    public static 道纹品阶 掉落品阶(天赋定义 天赋, Func<道纹品阶> 单次抽取)
    {
        if (单次抽取 == null) throw new ArgumentNullException(nameof(单次抽取));
        var 首次 = 抽取有效品阶(单次抽取);
        if (!是(天赋, 天赋种类.天命之子)) return 首次;
        var 再次 = 抽取有效品阶(单次抽取);
        return (int)首次 >= (int)再次 ? 首次 : 再次;
    }
    static 道纹品阶 抽取有效品阶(Func<道纹品阶> 抽取)
    {
        var 品阶 = 抽取();
        if (品阶 < 道纹品阶.普通 || 品阶 > 道纹品阶.传说) throw new ArgumentOutOfRangeException(nameof(品阶));
        return 品阶;
    }
    public static float 力量加成(天赋定义 天赋, float 原加成) => 值((double)有效(原加成) * (是(天赋, 天赋种类.万钧) ? 天帝数值.取("talents.strength_multiplier") : 1));
    public static float 灵力上限(天赋定义 天赋, float 原上限) => 值((double)有效(原上限) * (是(天赋, 天赋种类.灵海) ? 天帝数值.取("talents.mp_multiplier") : 1));
    public static float 防御(天赋定义 天赋, float 原值) => 值((double)有效(原值) * (是(天赋, 天赋种类.铁骨) ? 天帝数值.取("talents.armor_multiplier") : 1));
    public static float 护盾上限(天赋定义 天赋, float 原上限, float 最终智力) => 值((double)有效(原上限) + (是(天赋, 天赋种类.凝光) ? (double)有效(最终智力) * 天帝数值.取("talents.shield_per_intelligence") : 0));
    public static float 移动速度(天赋定义 天赋, float 原值) => 值((double)有效(原值) * (是(天赋, 天赋种类.逐风) ? 天帝数值.取("talents.move_multiplier") : 1));
    public static int 射击数量(天赋定义 天赋, int 原数量, bool 是射击, bool 是衍生攻击 = false)
        => (int)Math.Min(int.MaxValue, (long)Math.Max(0, 原数量) + (是(天赋, 天赋种类.双生矢) && 是射击 && !是衍生攻击 ? (int)天帝数值.取("talents.quantity_bonus") : 0));
    public static int 连锁次数(天赋定义 天赋, int 原次数, bool 支持连锁)
        => (int)Math.Min(int.MaxValue, (long)Math.Max(0, 原次数) + (是(天赋, 天赋种类.续雷) && 支持连锁 ? (int)天帝数值.取("talents.chain_bonus") : 0));
    public static float 作用半径(天赋定义 天赋, float 原半径, bool 是范围技能)
        => 值((double)有效(原半径) * (是(天赋, 天赋种类.广域) && 是范围技能 ? 天帝数值.取("talents.radius_multiplier") : 1));
    public static float 技能伤害倍率(天赋定义 天赋, float 当前血量, float 最大血量)
    {
        if (!是(天赋, 天赋种类.逆命) || !有限(当前血量) || !有限(最大血量) || 最大血量 <= 0) return 1;
        double 缺失 = 1 - Math.Min(最大血量, Math.Max(0, 当前血量)) / (double)最大血量;
        return (float)(1 + 缺失 * 天帝数值.取("talents.missing_hp_damage"));
    }
    static bool 是(天赋定义 天赋, 天赋种类 类型) => 天赋 != null && 天赋.种类 == 类型;
    static bool 有限(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    static float 有效(float v) => 有限(v) && v > 0 ? v : 0;
    static float 值(double v) => 主角衍生属性配置.截断(v);
}

public sealed class 余响请求
{
    public string 技能编号 { get; }
    public float 延迟秒 => (float)天帝数值.取("talents.echo_delay");
    public bool 消耗灵力 => false;
    public bool 推进计数 => false;
    internal 余响请求(string 技能编号) { this.技能编号 = 技能编号; }
}

// 各技能共享一个计数器。只在成功的正常释放后调用；不在这里创建技能或计时器。
public sealed class 天帝余响计数
{
    readonly 天赋定义 天赋;
    public int 当前计数 { get; private set; }
    public 天帝余响计数(天赋定义 天赋) { this.天赋 = 天赋; }
    public 余响请求 记录释放(string 技能编号, bool 释放成功 = true, bool 是衍生释放 = false)
    {
        if (天赋 == null || 天赋.种类 != 天赋种类.余响 || !释放成功 || 是衍生释放 || string.IsNullOrWhiteSpace(技能编号)) return null;
        当前计数 = (当前计数 + 1) % (int)天帝数值.取("talents.echo_every");
        return 当前计数 == 0 ? new 余响请求(技能编号) : null;
    }
}

