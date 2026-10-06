using System;

public static class 天帝普攻
{
    public static int 开局技能点 => (int)天帝数值.取("progression.start_skill_points");
    public static float 基础伤害 => (float)天帝数值.取("player.attack_base");
    // 兼容旧序列化结构，正常数值公式由天帝数值拥有。
    public static 主角属性配置 主角配置() => new 主角属性配置
    {
        力量 = (float)天帝数值.取("player.initial_attribute"), 速度 = (float)天帝数值.取("player.initial_attribute"), 智力 = (float)天帝数值.取("player.initial_attribute"),
        血量 = new 主角衍生属性配置 { 基础值 = (float)天帝数值.取("player.hp_base"), 力量系数 = (float)天帝数值.取("player.hp_per_strength") },
        灵力 = new 主角衍生属性配置 { 基础值 = (float)天帝数值.取("player.mp_base"), 智力系数 = (float)天帝数值.取("player.mp_per_intelligence") },
        防御 = new 主角衍生属性配置 { 基础值 = (float)天帝数值.取("player.armor_base") },
        移动速度 = new 主角衍生属性配置 { 基础值 = (float)天帝数值.取("player.move_base") },
        跑步速度 = new 主角衍生属性配置 { 基础值 = (float)(天帝数值.取("player.move_base") * 天帝数值.取("player.run_factor")) },
        攻击力 = new 主角衍生属性配置 { 基础值 = (float)天帝数值.取("player.attack_base") }
    };
}

public sealed class 普攻参数
{
    public int 通路, 等级;
    public bool 已激活;
    public int 数量, 分裂, 连锁;
    public float 伤害, 普通伤害, 五行额外伤害, 间隔, 转向角度, 溅射半径, 弹速;
    public float 暴击率, 暴击倍率, 天赋倍率;
    public 五行伤害分量 五行来源;
    public 道纹执行计划 顺序计划;
    public float 体型倍率 = 1, 弹速倍率 = 1;
    // 即时轨迹修饰沿后续分支复制；不改变普通弹的索敌方向。
    public int 运动功能;
    public float 弹体半径 => (float)天帝数值.取("rune.ordered_functions.projectile_radius") * 体型倍率;
    public static float 索敌距离 => (float)天帝数值.取("player.range_base");
    public static float 飞行距离 => 索敌距离 + (float)天帝数值.取("player.travel_extra");
    public static 普攻参数 读取(天帝道纹 网, 天帝主角属性 人) => 读取通路(网, 人, 0);
    public static 普攻参数 读取通路(天帝道纹 网, 天帝主角属性 人, int 通路)
    {
        if (网 == null || 人 == null || !网.通路开放(通路)) return new 普攻参数 { 通路 = 通路 };
        if (!天帝顺序道纹.本路使用顺序(网, 通路)) return 从加成(网, 人, 通路, 网.弹槽加成[通路], false);
        var 计划 = 天帝顺序道纹.编译(网, 人, 通路);
        var 参数 = 从加成(网, 人, 通路, 网.弹槽加成[通路], false);
        参数.顺序计划 = 计划; 参数.数量 = 计划.根弹数;
        参数.伤害 = 计划.起点.参数.伤害; 参数.普通伤害 = 计划.起点.参数.普通伤害; 参数.五行额外伤害 = 计划.起点.参数.五行额外伤害;
        return 参数;
    }
    public static 普攻参数 从加成(天帝道纹 网, 天帝主角属性 人, int 通路, double[] 加, bool 分段)
    {
        var 参数 = new 普攻参数 { 通路 = 通路 };
        if (网 == null || 人 == null || !网.通路开放(通路)) return 参数;
        参数.已激活 = true; 参数.等级 = 人.等级;
        int 形态(道纹属性 属性, string 上限) => (int)天帝数值.夹(加[(int)属性], 0, 天帝数值.取("shape." + 上限));
        参数.数量 = Math.Min((int)天帝数值.取("shape.quantity_max"), 分段 ? 1 + 形态(道纹属性.数量, "quantity_max") : 天帝天赋效果.射击数量(网.天赋, 1 + 形态(道纹属性.数量, "quantity_max"), true));
        参数.分裂 = 形态(道纹属性.分裂, "split_max");
        参数.连锁 = Math.Min((int)天帝数值.取("shape.chain_max"), 天帝天赋效果.连锁次数(网.天赋, 形态(道纹属性.连锁, "chain_max"), true));
        参数.转向角度 = (float)天帝数值.夹(加[(int)道纹属性.弧度], 0, 天帝数值.取("shape.arc_max"));
        参数.溅射半径 = (float)Math.Min(天帝数值.取("shape.radius_max"), 天帝天赋效果.作用半径(网.天赋, (float)(天帝数值.正值(加[(int)道纹属性.范围]) * 天帝数值.取("shape.radius_per_point")), true));
        参数.间隔 = 1 / 人.攻击速度;
        var 分段数值 = 分段 ? 天帝数值.计算主角(人.等级, 加, 网.天赋) : default;
        参数.弹速 = (float)Math.Min(天帝数值.取("player.projectile_max"), 天帝数值.取("player.projectile_base") + (分段 ? 分段数值.灵力 : 人.灵力) * 天帝数值.取("player.projectile_mp"));
        参数.天赋倍率 = 天帝天赋效果.技能伤害倍率(网.天赋, 人.当前血量, 人.血量);
        参数.普通伤害 = 分段 ? (float)分段数值.攻击 : 人.攻击力;
        参数.五行额外伤害 = (float)天帝数值.五行伤害(加, 人.等级);
        参数.五行来源 = new 五行伤害分量(加[(int)道纹属性.金], 加[(int)道纹属性.木], 加[(int)道纹属性.水], 加[(int)道纹属性.火], 加[(int)道纹属性.土]);
        参数.伤害 = (参数.普通伤害 + 参数.五行额外伤害) * 参数.天赋倍率;
        参数.暴击率 = 分段 ? (float)分段数值.暴击率 : 人.暴击率; 参数.暴击倍率 = 分段 ? (float)分段数值.暴击倍率 : 人.暴击倍率;
        return 参数;
    }
}
