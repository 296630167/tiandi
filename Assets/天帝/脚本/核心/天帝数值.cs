using System;

// 所有成长、软上限、减伤都从唯一配置取参数；场景和表现不再拥有数值曲线。
public sealed class 主角战斗数值
{
    public double 力量, 速度, 智力, 血量, 灵力, 防御, 护盾, 攻击, 攻速, 暴击率, 暴击倍率, 抗性, 急速, 移速, 跑速, 闪避率;
}

public readonly struct 战斗伤害包
{
    public readonly double 普通, 元素, 技能倍率, 形态倍率, 天赋倍率, 暴击倍率;
    public readonly int 攻方等级;
    public readonly 五行伤害分量 五行来源;
    public readonly 战斗敌人 来源;
    public 战斗伤害包(double 普通, double 元素, int 攻方等级, double 技能倍率 = 1, double 形态倍率 = 1, double 天赋倍率 = 1, double 暴击倍率 = 1)
        : this(普通, 元素, 攻方等级, 技能倍率, 形态倍率, 天赋倍率, 暴击倍率, default) { }
    public 战斗伤害包(double 普通, double 元素, int 攻方等级, double 技能倍率, double 形态倍率, double 天赋倍率, double 暴击倍率, 五行伤害分量 五行来源, 战斗敌人 来源 = null)
    { this.普通 = 普通; this.元素 = 元素; this.攻方等级 = 攻方等级; this.技能倍率 = 技能倍率; this.形态倍率 = 形态倍率; this.天赋倍率 = 天赋倍率; this.暴击倍率 = 暴击倍率; this.五行来源 = 五行来源; this.来源=来源; }
}

public static class 天帝数值
{
    static readonly string[] 敌类型键 = { "normal", "elite", "leader", "boss" };
    public static double 取(string 路径) => 天帝数值配置.取(路径);
    public static double 地图进度(int 等级) => 夹(等级 - 1, 0, 99) / 99;
    public static double 大图成长(int 等级, string 键)
        => 取("map.arena." + 键 + "_start") + (取("map.arena." + 键 + "_end") - 取("map.arena." + 键 + "_start")) * 地图进度(等级);
    public static double 敌战术成长(int 等级, string 键, double 初值 = 1)
        => 初值 + (取("battle_content.growth." + 键) - 初值) * 地图进度(等级);
    public static int 区域敌人数(int 等级, int 品质)
    {
        double 进度 = Math.Pow(地图进度(等级), 取("map.long_region.growth.exponent"));
        int 数(string 键) => (int)Math.Floor(取("map.long_region.growth." + 键 + "_start") +
            (取("map.long_region.growth." + 键 + "_end") - 取("map.long_region.growth." + 键 + "_start")) * 进度 + .5);
        int 总 = 数("total"), 精 = 数("elite"), 头 = 数("leader");
        return 品质 == 0 ? 总 - 精 - 头 - 1 : 品质 == 1 ? 精 : 品质 == 2 ? 头 : 1;
    }
    public static int 道纹回收价(道纹实例 纹)
    {
        if(纹==null||纹.是源纹||纹.是天赋)return 0;
        if(纹.可改造词条 || 纹.是特性道纹)
            return 纹.品阶>=道纹品阶.普通&&纹.品阶<=道纹品阶.传说?(int)取("economy.recycle_grade_prices."+(int)纹.品阶):0;
        if(纹.分类!=道纹分类.分叉||纹.接口<1||纹.接口>63)return 0;
        int 口=0;for(int i=0;i<6;i++)if(纹.有接口(i))口++;
        return 口>=3&&口<=6?(int)取("economy.recycle_branch_prices."+(口-3)):0;
    }
    public static int[] 整数表(string 路径, int 数量)
    { var 结果 = new int[数量]; for (int 序 = 0; 序 < 数量; 序++) 结果[序] = (int)取(路径 + "." + 序); return 结果; }
    public static int 玩家上限 => (int)取("levels.player_max");
    public static int 弹体上限 => (int)取("shape.projectiles_max");
    public static double 夹(double 值, double 最小, double 最大) => Math.Max(最小, Math.Min(最大, 值));
    public static bool 有限(double 值) => !double.IsNaN(值) && !double.IsInfinity(值);
    public static double 正值(double 值) => 有限(值) ? Math.Max(0, 值) : 0;
    public static double 成长(int 等级)
    { double 差 = 夹(等级, 1, 取("levels.enemy_max")) - 1; return 1 + 取("growth.linear") * 差 + 取("growth.quadratic") * 差 * 差; }
    public static double 软上限(double 增益, double 拐点)
    { 增益 = 正值(增益); if (增益 <= 拐点) return 增益; double 超额 = 增益 - 拐点; return 拐点 + 超额 / (1 + 超额 / 拐点); }
    public static 主角战斗数值 计算主角(int 等级, double[] 增益, 天赋定义 天赋)
    {
        double 加(道纹属性 属性) => 增益 == null ? 0 : 正值(增益[(int)属性]);
        bool 是(天赋种类 种类) => 天赋 != null && 天赋.种类 == 种类;
        double 成 = 成长((int)夹(等级, 1, 玩家上限)), 初始 = 取("player.initial_attribute");
        double 软(道纹属性 属性, string 参数) => 软上限(加(属性), 取("soft." + 参数) * 成);
        var 结果 = new 主角战斗数值();
        结果.力量 = 初始 + 取("growth.strength") * (成 - 1) + 软上限(加(道纹属性.力量) * (是(天赋种类.万钧) ? 取("talents.strength_multiplier") : 1), 取("soft.strength_g") * 成);
        结果.速度 = 初始 + 取("growth.speed") * (成 - 1) + 软(道纹属性.速度, "speed_g");
        结果.智力 = 初始 + 取("growth.intelligence") * (成 - 1) + 软(道纹属性.智力, "intelligence_g");
        double 力增 = 结果.力量 - 初始, 速增 = 结果.速度 - 初始, 智增 = 结果.智力 - 初始;
        结果.血量 = 取("player.hp_base") + 取("player.hp_level_g") * (成 - 1) + 取("player.hp_per_strength") * 结果.力量 + 软(道纹属性.血量, "hp_g");
        结果.灵力 = (取("player.mp_base") + 取("player.mp_per_intelligence") * 结果.智力 + 软(道纹属性.灵力, "mp_g")) * (是(天赋种类.灵海) ? 取("talents.mp_multiplier") : 1);
        结果.防御 = (取("player.armor_base") + 取("player.armor_per_strength_bonus") * 力增 + 软(道纹属性.防御, "armor_g")) * (是(天赋种类.铁骨) ? 取("talents.armor_multiplier") : 1);
        结果.护盾 = 取("player.shield_per_intelligence_bonus") * 智增 + 软(道纹属性.护盾, "shield_g") + (是(天赋种类.凝光) ? 结果.智力 * 取("talents.shield_per_intelligence") : 0);
        结果.攻击 = 取("player.attack_base") + 取("player.attack_per_strength_bonus") * 力增 + 取("player.attack_per_speed_bonus") * 速增 + 取("player.attack_per_intelligence_bonus") * 智增;
        结果.攻速 = Math.Min(取("player.aps_max"), 取("player.aps_base") * (1 + 取("player.aps_speed_gain") * 速增 / (取("player.aps_speed_k_g") * 成 + 速增)) * (1 + 软上限(加(道纹属性.攻速), 取("soft.attack_speed_percent")) / 100));
        结果.暴击率 = 夹(取("player.crit_base") + 取("player.crit_speed_gain") * 速增 / (取("player.crit_speed_k_g") * 成 + 速增), 0, 取("player.crit_max"));
        结果.暴击倍率 = 夹(取("player.crit_damage_base") + 取("player.crit_int_gain") * 智增 / (取("player.crit_int_k") + 智增), 1, 取("player.crit_damage_max"));
        结果.抗性 = 夹(取("player.resist_int_gain") * 智增 / (取("player.resist_int_k") + 智增), 0, 取("player.resist_max"));
        结果.急速 = 夹(取("player.haste_int_gain") * 智增 / (取("player.haste_int_k") + 智增), 0, 取("player.haste_max"));
        结果.移速 = Math.Min(取("player.move_max"), (取("player.move_base") + 取("player.move_speed_gain") * 速增 / (取("player.move_speed_k_g") * 成 + 速增)) * (1 + 软上限(加(道纹属性.移速), 取("soft.move_percent")) / 100) * (是(天赋种类.逐风) ? 取("talents.move_multiplier") : 1));
        结果.跑速 = Math.Min(取("player.run_max"), 取("player.run_factor") * 结果.移速);
        结果.闪避率 = 夹(取("player.evasion_speed_gain") * 速增 / (取("player.evasion_speed_k_g") * 成 + 速增), 0, 取("player.evasion_max"));
        return 结果;
    }
    public static double 五行伤害(double[] 增益, int 等级)
    { double 总 = 0; for (int 索引 = (int)道纹属性.金; 索引 <= (int)道纹属性.土; 索引++) 总 += 正值(增益[索引]); return 软上限(总, 取("soft.element_g") * 成长(等级)); }
    // 满命中纸面预算，不参与实际伤害；多通路按现有全局间隔轮转取平均。
    public static double 输出预算(天帝道纹 网, 天帝主角属性 人, int 目标数)
    {
        if (网 == null || 人 == null) return 0;
        double 总 = 0; int 通路数 = 0;
        for (int 通路 = 0; 通路 < 6; 通路++)
        {
            if (!网.通路参与射击(通路)) continue;
            var 攻 = 普攻参数.读取通路(网, 人, 通路); int 剩余 = 目标数;
            if (攻.顺序计划 != null)
            {
                总 += 天帝顺序道纹.伤害预算(攻.顺序计划, 目标数, 天帝天赋效果.射击数量(网.天赋, 1, true), true) * 人.攻击速度;
                通路数++; continue;
            }
            double 权重 = 0;
            void 分配(int 次数, double 比例) { int 数 = Math.Min(剩余, 次数); 权重 += 数 * 比例; 剩余 -= 数; }
            分配(攻.数量, 1);
            for (int 次 = 1; 次 <= 攻.连锁; 次++) 分配(攻.数量, Math.Pow(取("shape.chain_factor"), 次));
            分配(攻.数量 * 攻.分裂, 取("shape.split_factor"));
            if (攻.溅射半径 > 0) 分配(攻.数量 * (int)取("shape.splash_targets_max"), 取("shape.splash_factor"));
            总 += 攻.伤害 * 人.攻击速度 * (1 + 人.暴击率 * (人.暴击倍率 - 1)) * 权重; 通路数++;
        }
        double 回响 = 网.天赋?.种类 == 天赋种类.余响 ? 1 + 1 / 取("talents.echo_every") : 1;
        return 通路数 == 0 ? 0 : 总 / 通路数 * 回响;
    }
    public static double 战斗力(天帝道纹 网, 天帝主角属性 人)
    {
        double 单体 = 输出预算(网, 人, 1), 清群 = 输出预算(网, 人, (int)取("shape.targets_test"));
        double 输出 = Math.Pow(单体, 取("power.single_weight")) * Math.Pow(清群, 取("power.crowd_weight"));
        double 生存 = (人.血量 + 人.灵气护盾) / 防御留存(人.防御, 人.等级);
        double 初始输出 = 取("player.attack_base") * 取("player.aps_base") * (1 + 取("player.crit_base") * (取("player.crit_damage_base") - 1));
        double 初始生存 = (取("player.hp_base") + 取("player.hp_per_strength") * 取("player.initial_attribute")) / 防御留存(取("player.armor_base"), 1);
        return 取("power.base_score") * Math.Pow(输出 / 初始输出, 取("power.output_weight")) * Math.Pow(生存 / 初始生存, 取("power.ehp_weight")) * Math.Pow(人.移动速度 / 取("player.move_base"), 取("power.move_weight"));
    }
    public static double 防御留存(double 防御, int 攻方等级, double 百分比穿透 = 0, double 固定穿透 = 0)
    { double 有效 = Math.Max(0, 正值(防御) * (1 - 夹(百分比穿透, 0, 取("damage.penetration_percent_max"))) - 正值(固定穿透)); double 基数 = 取("damage.armor_k_g") * 成长(攻方等级); return Math.Max(取("damage.armor_retention_min"), 基数 / (基数 + 有效)); }
    public static double 等级倍率(int 攻方, int 守方) => Math.Exp(取("damage.level_log") * 夹(攻方 - 守方, -取("damage.level_difference_max"), 取("damage.level_difference_max")));
    public static double 结算伤害(战斗伤害包 包, int 守方等级, double 防御, double 抗性, double 百分比穿透 = 0, double 固定穿透 = 0, double 抗性穿透 = 0)
    {
        if (!有限(包.普通) || !有限(包.元素) || 包.普通 < 0 || 包.元素 < 0 || !有限(包.技能倍率) || !有限(包.形态倍率) || !有限(包.天赋倍率) || !有限(包.暴击倍率) || 包.技能倍率 < 0 || 包.形态倍率 < 0 || 包.天赋倍率 < 0 || 包.暴击倍率 < 0) return 0;
        double 伤 = (包.普通 * 防御留存(防御, 包.攻方等级, 百分比穿透, 固定穿透) + 包.元素 * (1 - 夹(抗性 - 抗性穿透, 0, 取("player.resist_max")))) * 包.技能倍率 * 包.形态倍率 * 包.天赋倍率 * 包.暴击倍率 * 等级倍率(包.攻方等级, 守方等级);
        return 伤 > 0 && 有限(伤) ? 夹(伤, 取("damage.hit_min"), 取("damage.technical_hit_max")) : 0;
    }
    public static 战斗伤害明细 拆分伤害(战斗伤害包 包, double 命中总伤, double 防御, double 抗性)
    {
        if (!有限(命中总伤) || 命中总伤 <= 0) return default;
        double 普 = 包.普通 * 防御留存(防御, 包.攻方等级);
        double 属 = 包.元素 * (1 - 夹(抗性, 0, 取("player.resist_max")));
        double 基 = 普 + 属;
        if (!有限(基) || 基 <= 0) return default;
        double 普伤 = 属 <= 0 ? 命中总伤 : 命中总伤 * (普 / 基), 属伤 = 命中总伤 - 普伤;
        double 来源和 = 包.五行来源.总和;
        if (属伤 <= 0) return new 战斗伤害明细(普伤);
        if (!有限(来源和) || 来源和 <= 0) return new 战斗伤害明细(普伤, 属性: 属伤);
        // 最后一项接收浮点余数，使分项与原有一次命中的最终值一致。
        int 最后 = 4; while (最后 > 0 && 包.五行来源.读取(最后) <= 0) 最后--;
        double 已分配 = 0;
        double 分量(int 序)
        {
            double 值 = 序 == 最后 ? Math.Max(0, 属伤 - 已分配) : 序 < 最后 ? 属伤 * (包.五行来源.读取(序) / 来源和) : 0;
            已分配 += 值; return 值;
        }
        return new 战斗伤害明细(普伤, 分量(0), 分量(1), 分量(2), 分量(3), 分量(4));
    }
    public static string 敌人键(战斗敌人级别 类) => 敌类型键[(int)类];
    public static double 敌参数(战斗敌人级别 类, string 参数) => 取("enemies." + 敌人键(类) + "." + 参数);
    public static int 升级经验(int 等级) => 等级 >= 玩家上限 ? 0 : (int)Math.Ceiling(取("experience.base") + 取("experience.linear") * (等级 - 1) + 取("experience.power_gain") * Math.Pow(等级 - 1, 取("experience.power_exponent")));
    public static int 击杀经验(int 玩家等级, int 敌等级, 战斗敌人级别 类) => 玩家等级 >= 玩家上限 ? 0 : (int)Math.Ceiling(取("experience.enemy_base") * (1 + 取("experience.enemy_growth") * (敌等级 - 1)) * 取("experience.rank_multipliers." + 敌人键(类)) * 夹(1 + 取("experience.difference_gain") * (敌等级 - 玩家等级), 取("experience.difference_min"), 取("experience.difference_max")));
    public static 道纹词条 抽取词条(道纹属性 属性, int 物品等级, Random 随机)
    {
        if (天帝道纹属性.分组(属性) == 道纹属性分组.形态) return new 道纹词条(属性, 属性 == 道纹属性.弧度 ? (int)取("rune.arc_per_affix") : 1);
        词条定点范围(属性, 物品等级, out int 下, out int 上);
        return 道纹词条.从定点(属性, 随机.Next(下, 上 + 1));
    }
    // 图鉴与实际掉落共用端点和舍入，品阶仅决定词条容量。
    public static void 词条定点范围(道纹属性 属性, int 物品等级, out int 下, out int 上)
    {
        int 尺度 = (int)取("rune.storage_scale");
        if (天帝道纹属性.分组(属性) == 道纹属性分组.形态)
        { 下 = 上 = (属性 == 道纹属性.弧度 ? (int)取("rune.arc_per_affix") : 1) * 尺度; return; }
        string 键;
        switch (属性)
        {
            case 道纹属性.力量: 键 = "strength"; break; case 道纹属性.速度: 键 = "speed"; break; case 道纹属性.智力: 键 = "intelligence"; break;
            case 道纹属性.血量: 键 = "hp"; break; case 道纹属性.灵力: 键 = "mp"; break; case 道纹属性.防御: 键 = "armor"; break;
            case 道纹属性.护盾: 键 = "shield"; break; case 道纹属性.攻速: 键 = "attack_speed"; break; case 道纹属性.移速: 键 = "move"; break;
            default: 键 = "element"; break;
        }
        double 成 = 属性 == 道纹属性.攻速 || 属性 == 道纹属性.移速 ? 1 : 成长((int)夹(物品等级, 1, 玩家上限));
        下 = (int)Math.Ceiling(取("rune.affix_ranges." + 键 + ".0") * 成 * 尺度 - 1e-9);
        上 = (int)Math.Floor(取("rune.affix_ranges." + 键 + ".1") * 成 * 尺度 + 1e-9);
    }
}
