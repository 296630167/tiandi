using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class 敌术命中
{
    public bool 已命中;
    public readonly HashSet<int> 地面次数 = new HashSet<int>();
}
public sealed class 战斗敌术
{
    public 战斗敌人 来源;
    public 敌技能 技能;
    public Vector2 起点, 位置, 终点, 方向;
    public float 已过, 寿命, 延迟, 次伤, 半径;
    public int 已跳数;
    public bool 位移, 跃击, 地面, 二段, 冲击演出;
    public 敌术命中 命中;
    public 战斗伤害包 包;
}
public sealed class 辅助光线
{
    public Vector2 起, 终;
    public bool 盾;
    public float 剩余 = .5f;
}

// 单一战斗生命周期拥有全部敌方弹体、位移、持续区与辅助预算。
public sealed class 天帝敌人战术
{
    readonly 天帝战斗地图 地图;
    readonly 天帝战斗寻路 寻路;
    readonly List<战斗敌人> 敌人;
    readonly Func<战斗伤害包, bool> 伤害;
    readonly Func<战斗伤害包, bool, bool> 可闪伤害;
    readonly Func<Rect> 视野;
    readonly float 初始生命和;
    readonly List<战斗敌术> 效果 = new List<战斗敌术>(48);
    readonly List<战斗敌术> 冲击 = new List<战斗敌术>(48);
    readonly List<辅助光线> 光线 = new List<辅助光线>(16);
    public IReadOnlyList<战斗敌术> 敌术 => 效果;
    public IReadOnlyList<战斗敌术> 冲击演出 => 冲击;
    public IReadOnlyList<辅助光线> 辅助线 => 光线;
    public float 实际治疗 { get; private set; }
    public float 实际授盾 { get; private set; }
    public int 技能释放数 { get; private set; }
    public int 引导打断数 { get; private set; }
    public readonly int[] 技能统计 = new int[25];
    int 蓄力数, 大招数;
    public string 王台词 { get; private set; } = "";
    float 台词秒;
    public float 震屏秒 { get; private set; }
    float 突进协作等待;
    public event Action<战斗敌人,敌技能> 技能演出释放;
    public Vector2 玩家速度 { get; set; }
    static float A(string 键) => (float)天帝数值.取("map.arena." + 键);
    public Vector2 预判位置(Vector2 玩家, float 秒)
    {
        if (!地图.生存大图) return 玩家;
        var 终 = 玩家 + Vector2.ClampMagnitude(玩家速度 * Mathf.Min(秒, A("aim_lead_max")), A("aim_shift_max"));
        return 有效落点(玩家, 终) ? 终 : 玩家;
    }
    public Vector2 追击目标(战斗敌人 敌, Vector2 玩家)
    {
        if (!地图.生存大图 || 玩家速度.sqrMagnitude < .25f || Vector2.Distance(敌.位置, 玩家) > A("close_pursuit_range") ||
            敌.序号 % 10 >= Mathf.RoundToInt(A("flank_fraction") * 10) || 天帝敌种配置.角色(敌.物种) >= 3) return 玩家;
        var 向 = 玩家速度.normalized;
        var 终 = 玩家 + 玩家速度 * A("flank_lead_seconds") + new Vector2(-向.y, 向.x) * (敌.序号 % 2 == 0 ? 1 : -1) * A("flank_offset");
        return 有效落点(玩家, 终) ? 终 : 玩家;
    }
    int 地图等级 => 敌人.Count == 0 ? 1 : 敌人[0].地图档位;
    float 成长上限(string 键) => Mathf.Floor((float)天帝数值.敌战术成长(地图等级, 键 + "_end", P("limits." + 键)));
    public void 开始敌人推进()
    {
        蓄力数 = 大招数 = 0;
        foreach (var e in 敌人) if (e.存活 && e.技能编号 > 0 && e.行动 == 敌人行动.蓄力)
        { var s = 敌技能.读取(e.技能编号); if (s.类型 != 敌技能类型.单击) 蓄力数++; if (s.大招) 大招数++; }
        foreach (var a in 效果) if (a.位移 || a.二段 && a.延迟 > 0) 大招数++;
    }
    float 减速剩余, 束缚剩余, 控制免疫;
    public Func<bool> 特性免疫 {get;set;}
    public Func<战斗敌人,Vector2,Vector2> 特性目标 {get;set;}
    public Func<战斗伤害包,Vector2,bool,bool> 特性受击 {get;set;}
    bool 命中目标(战斗伤害包 包,Vector2 点,bool 闪避=false) => 特性受击!=null ? 特性受击(包,点,闪避) : 闪避?可闪伤害(包,true):伤害(包);
    public float 玩家移速倍率 => 束缚剩余 > 0 ? 0 : 减速剩余 > 0 ? 1 - P("control.slow_fraction") : 1;
    static float P(string k) => 天帝敌种配置.取(k);
    static readonly string[] 角色键 = { "ranged", "mobile", "healer", "shielder" };
    public 天帝敌人战术(天帝战斗地图 地图, 天帝战斗寻路 寻路, List<战斗敌人> 敌人,
        Func<战斗伤害包, bool> 伤害, Func<战斗伤害包, bool, bool> 可闪伤害, float 初始生命和, Func<Rect> 视野)
    { this.地图 = 地图; this.寻路 = 寻路; this.敌人 = 敌人; this.伤害 = 伤害; this.可闪伤害 = 可闪伤害; this.初始生命和 = 初始生命和; this.视野 = 视野; }
    public bool 可投放(战斗敌人 敌)
    {
        int 类 = 天帝敌种配置.分组(敌.物种); if (类 < 0) return true;
        int n = 0; foreach (var e in 敌人) if (e.存活 && 天帝敌种配置.分组(e.物种) == 类) n++;
        return n < P("limits." + 角色键[类]);
    }
    bool 视内(战斗敌人 e, Vector2 玩家)
        => 视野 == null ? Vector2.Distance(e.位置, 玩家) <= 14 : 视野().Contains(e.位置);
    bool 辅助合法(战斗敌人 施, 战斗敌人 受, 敌技能 s)
    {
        if (受 == 施 || !受.存活 || 受.登场剩余秒 > 0 || (int)受.布点.级别 >= 2 || 天帝敌种配置.角色(受.物种) >= 3) return false;
        if (Vector2.Distance(施.位置, 受.位置) > s.距离 || !寻路.无遮挡(施.位置, 受.位置)) return false;
        if (s.类型 == 敌技能类型.治疗)
            return 受.血量 / 受.最大血量 < P("support.heal_hp_threshold") && 受.累计恢复 < 受.最大血量 * P("support.heal_target_cap") && 实际治疗 < 初始生命和 * P("support.heal_global_cap");
        return 受.护盾量 <= 0 && 受.盾禁用 <= 0 && 受.累计授盾 < 受.最大血量 * P("support.shield_target_cap") && 实际授盾 < 初始生命和 * P("support.shield_global_cap");
    }
    public void 受伤(战斗敌人 敌)
    {
        if (敌.行动 != 敌人行动.蓄力 || 敌.技能编号 <= 0 || !敌技能.读取(敌.技能编号).辅助) return;
        引导打断数++; 敌.行动 = 敌人行动.后摇; 敌.后摇秒 = P("support.interrupt_lockout"); 敌.战术决策秒 = 0;
        敌.冷却 = Mathf.Max(敌.冷却, 敌技能.读取(敌.技能编号).周期); 敌.蓄力 = 0; 敌.辅助目标.Clear();
    }
    int 下一技(战斗敌人 e, Vector2 玩家)
    {
        bool 精 = e.布点.级别 == 战斗敌人级别.精英;
        if (e.物种 == 14)
        {
            int n = e.已攻击次数 % 3;
            // 保留形态轮换；每组三击中的第二击按距离选择，避免近身仍向远处空放。
            if (n == 1)
            {
                float d = Vector2.Distance(e.位置, 玩家);
                if (d < P("movement.boss_close")) return e.形态 == 2 ? 2 : e.形态 == 3 ? 11 : 16;
                if (d > P("movement.boss_far")) return e.形态 <= 2 ? 15 : e.形态 == 3 ? 5 : 18;
            }
            switch (e.形态)
            {
                case 1: return n == 0 ? 1 : n == 1 ? 15 : 16;
                case 2: return n == 0 ? 1 : n == 1 ? 17 : 2;
                case 3: return n == 0 ? 11 : n == 1 ? 8 : 5;
                case 4: return n == 0 ? 7 : n == 1 ? 18 : 15;
                default: return n == 0 ? 1 : n == 1 ? 五行技能[(e.已攻击次数 / 3) % 5] : 19;
            }
        }
        if (e.物种 == 12) return e.已攻击次数 % 3 == 0 ? 2 : e.已攻击次数 % 3 == 1 ? 11 : 16;
        if (e.物种 == 13) return e.已攻击次数 % 3 == 0 ? 7 : e.已攻击次数 % 3 == 1 ? 13 : 5;
        if (e.物种 == 20) return e.已攻击次数 % 3 == 2 ? 18 : 20 + e.已攻击次数 % 5;
        if (e.物种 == 21) return e.已攻击次数 % 3 == 0 ? 9 : e.已攻击次数 % 3 == 1 ? 10 : 1;
        if (e.物种 == 0 && 精 && e.已攻击次数 % 3 == 2) return 9;
        if (e.物种 == 10 && 精 && e.已攻击次数 % 3 == 2) return 8;
        return (int)天帝敌种配置.物种(e.物种, "skill");
    }
    bool 预警许可(战斗敌人 e, 敌技能 s)
    {
        if((s.类型==敌技能类型.冲锋||s.类型==敌技能类型.跳跃)&&突进协作等待>0)return false;
        if (s.类型 == 敌技能类型.单击) return true;
        return 蓄力数 < 成长上限("casts") && (!s.大招 || 大招数 < 成长上限("heavy"));
    }
    static readonly int[] 五行技能 = { 17, 8, 18, 12, 16 };
    bool 有效落点(Vector2 起, Vector2 终) => 地图.可站立(终, P("movement.body_radius")) && 寻路.无遮挡(起, 终, P("movement.body_radius"));
    public void 推进敌(战斗敌人 e, Vector2 玩家, float dt, Action<战斗敌人, Vector2, float> 移动)
    {
        if (dt <= 0 || !e.存活) return;
        e.战术决策秒 = Mathf.Max(0, e.战术决策秒 - dt);
        e.突袭冷却 = Mathf.Max(0, e.突袭冷却 - dt);
        bool 位移中 = false; foreach (var a in 效果) if (a.来源 == e && a.位移) { 位移中 = true; break; }
        if (e.物种 == 14 && e.显示形态 != e.形态 && !位移中)
        {
            e.显示形态 = e.形态; e.形态提示秒 = P("boss.switch_effect");
            if (e.地图档位 >= P("growth.dialogue_unlock")) { 王台词 = 天帝数值配置.取文本("battle_content.boss_lines." + (e.形态 - 1)); 台词秒 = 3; }
            e.行动 = 敌人行动.后摇; e.后摇秒 = P("boss.switch_pause");
            e.冷却 = Mathf.Max(e.冷却, e.后摇秒); e.蓄力 = 0;
            for (int i = 效果.Count - 1; i >= 0; i--)
                if (效果[i].来源 == e && 效果[i].二段 && 效果[i].延迟 > 0) 效果.RemoveAt(i);
                else if (效果[i].来源 == e && 效果[i].地面) 效果[i].寿命 = Mathf.Min(效果[i].寿命, 效果[i].已过 + P("limits.death_zone_seconds"));
        }
        if (e.行动 == 敌人行动.蓄力)
        {
            if (地图.生存大图 && e.技能编号 == 1 && e.蓄力 > e.蓄力总秒 * (1 - A("bite_follow_fraction")))
            { 移动(e, 玩家, dt); e.攻击落点 = 玩家; }
            e.蓄力 -= dt;
            if (e.蓄力 <= 0)
            {
                var s = 敌技能.读取(e.技能编号);
                if (视内(e, 玩家)) 释放(e, 玩家);
                e.行动 = 敌人行动.后摇; e.后摇秒 = s.后摇 * (float)天帝数值.敌战术成长(e.地图档位, "recovery_end") + (s.类型 == 敌技能类型.冲锋 || s.类型 == 敌技能类型.跳跃 ? s.速度 : 0);
                if(s.大招&&(e.物种==14||e.布点.级别!=战斗敌人级别.普通))
                    e.后摇秒+=P(e.物种==14?"boss.opening_seconds":"boss.elite_opening_seconds");
                e.已攻击次数++;
            }
            return;
        }
        if (e.行动 == 敌人行动.后摇)
        { e.后摇秒 -= dt; if (e.后摇秒 <= 0) e.行动 = 敌人行动.追击; return; }
        int id = 下一技(e, 玩家);
        float 当前距 = Vector2.Distance(e.位置, 玩家);
        bool 反风筝突袭 = 地图.生存大图 && e.地图档位 >= A("charge_unlock") && (e.物种 == 0 || 天帝敌种配置.角色(e.物种) == 2) &&
            e.突袭冷却 <= 0 && 当前距 > e.攻击范围 && 当前距 <= 敌技能.读取(9).距离 - P("movement.range_margin");
        if (反风筝突袭) id = 9;
        var 技 = 敌技能.读取(id);
        if (天帝敌种配置.属性狼(e.物种) && Vector2.Distance(e.位置, 玩家) <= e.攻击范围)
        { id = 1; 技 = 敌技能.读取(id); }
        if (技.辅助)
        {
            if (e.战术决策秒 <= 0 || (e.冷却 <= 0 && e.辅助目标.Count == 0))
            {
                e.辅助目标.Clear(); e.掩护队友 = null; e.战术站位 = e.位置;
                float 最近 = float.MaxValue;
                foreach (var a in 敌人)
                {
                    if (辅助合法(e, a, 技)) e.辅助目标.Add(a);
                    if (a == e || !a.存活 || a.登场剩余秒 > 0 || 天帝敌种配置.角色(a.物种) != 0) continue;
                    float d = (a.位置 - e.位置).sqrMagnitude;
                    if (d < 最近) { 最近 = d; e.掩护队友 = a; }
                }
                e.辅助目标.Sort((a,b) => {
                    // 治疗优先濒危者；授盾优先正在顶线的近战/机动与精英。
                    float 分(战斗敌人 x)=>技.类型==敌技能类型.治疗?x.血量/x.最大血量:
                        (天帝敌种配置.角色(x.物种)==0||天帝敌种配置.角色(x.物种)==2?-.5f:0)+
                        (x.布点.级别==战斗敌人级别.精英?-.25f:0)+(x.血量/x.最大血量<天帝战斗润色.取("support_focus_health")?-.2f:0);
                    int n=分(a).CompareTo(分(b));return n!=0?n:a.序号.CompareTo(b.序号);
                });
                if (e.辅助目标.Count > 技.数量) e.辅助目标.RemoveRange(技.数量, e.辅助目标.Count - 技.数量);
                e.战术决策秒 = P("movement.decision_seconds");
            }
            for (int i = e.辅助目标.Count - 1; i >= 0; i--) if (!辅助合法(e, e.辅助目标[i], 技)) e.辅助目标.RemoveAt(i);
            if (e.辅助目标.Count == 0) { id = e.物种 == 7 ? 1 : 7; 技 = 敌技能.读取(id); }
        }
        float 距 = Vector2.Distance(e.位置, 玩家);
        bool 远程 = 技.类型 == 敌技能类型.飞弹 || 技.类型 == 敌技能类型.投掷 || 技.辅助;
        float 起手距离 = id == 1 ? e.攻击范围 : e.物种 == 14 && 技.类型 == 敌技能类型.扇面 ? P("boss.fan_range") : 技.距离;
        bool 可攻击 = (技.辅助 || 距 <= 起手距离 - P("movement.range_margin")) && (技.辅助 || 寻路.无遮挡(e.位置, 玩家)) && 视内(e, 玩家);
        if (可攻击 && e.冷却 <= 0)
        {
            if (!预警许可(e, 技))
            {
                e.技能等待 += dt;
                if (e.技能等待 >= P("limits.role_wait") && 距 <= e.攻击范围) { id = 1; 技 = 敌技能.读取(id); }
                else { e.冷却 = 地图.生存大图 ? A("casting_retry") : 0; 移动(e, 追击目标(e, 玩家), dt); return; }
            }
            e.技能等待 = 0; e.技能编号 = id;
            Vector2 瞄准 = 技.类型 == 敌技能类型.单击 || 技.辅助 ? 玩家 : 预判位置(玩家,
                技.前摇 + (技.类型 == 敌技能类型.飞弹 ? 距 / Mathf.Max(.1f, 技.速度) : 0));
            e.锁定方向 = (瞄准 - e.位置).normalized;
            if (e.锁定方向.sqrMagnitude < .1f) e.锁定方向 = Vector2.right;
            e.攻击落点 = 技.类型 == 敌技能类型.自周 ? e.位置 : 瞄准;
            if (技.类型 == 敌技能类型.冲锋 || 技.类型 == 敌技能类型.跳跃)
            {
                float 长 = Mathf.Min(Vector2.Distance(e.位置, 瞄准), 技.距离);
                // 不跨墙，逻辑位置在可通行路径上；落地仍可受攻击。
                e.攻击落点 = 地图.移动(e.位置, e.锁定方向, 长);
                if (!有效落点(e.位置, e.攻击落点)) { e.冷却 = P("movement.retry"); return; }
            }
            if (技.类型 == 敌技能类型.投掷 && (距 < P("limits.zone_player_min_distance") || !地图.可站立(e.攻击落点, P("movement.body_radius")))) { e.冷却 = P("movement.retry"); return; }
            e.本次突进 = 技.类型 == 敌技能类型.冲锋; e.本次范围技 = 技.大招 || 技.类型 == 敌技能类型.扇面 || 技.类型 == 敌技能类型.直线;
            e.本次攻击范围 = 技.类型 == 敌技能类型.投掷 || 技.类型 == 敌技能类型.跳跃 ? 技.宽度 : 技.距离;
            e.本次技能倍率 = 技.倍率; e.行动 = 敌人行动.蓄力;
            e.蓄力 = 技.前摇; if (id == 1) e.蓄力 = (float)天帝数值.敌参数(e.布点.级别, "windup");
            if (id != 1) e.蓄力 = Mathf.Max(P(e.物种 == 14 ? "growth.boss_windup_min" : "growth.windup_min"), e.蓄力 * (float)天帝数值.敌战术成长(e.地图档位, "windup_end"));
            e.蓄力总秒 = e.蓄力;
            if(技.类型==敌技能类型.冲锋||技.类型==敌技能类型.跳跃)突进协作等待=天帝战斗润色.取("charge_stagger");
            if (反风筝突袭) e.突袭冷却 = A("charge_period");
            e.冷却 = Mathf.Max(Mathf.Max(技.周期, e.攻击间隔) * e.战术周期倍率, e.蓄力 + 技.后摇);
            if (技.类型 != 敌技能类型.单击) 蓄力数++; if (技.大招) 大招数++;
            return;
        }
        if (远程 && 视内(e, 玩家) && 距 < P("movement.preferred_range") && !技.辅助)
        {
            e.退步秒 += dt;
            if (e.退步秒 < P("movement.retreat_seconds"))
            {
                Vector2 退 = e.位置 + (e.位置 - 玩家).normalized * P("movement.retreat_step");
                if (有效落点(e.位置, 退)) { 移动(e, 退, dt); return; }
            }
            e.停步秒 += dt; if (e.停步秒 < P("movement.stand_seconds")) return;
            e.停步秒 = e.退步秒 = 0;
        }
        // 冷却期间移动而非原地排队；所有站位仍由原寻路、分离和控制模块落实。
        int 角色 = 天帝敌种配置.角色(e.物种);
        if (视内(e, 玩家) && (角色 >= 3 || (可攻击 && (远程 || 角色 == 2))))
        {
            if (角色 >= 3 && e.掩护队友 != null && e.掩护队友.存活)
            {
                Vector2 后 = (e.掩护队友.位置 - 玩家).normalized;
                Vector2 点 = e.掩护队友.位置 + 后 * P("movement.support_cover");
                if (有效落点(e.位置, 点)) { 移动(e, 点, dt); return; }
            }
            if (e.战术决策秒 <= 0)
            {
                Vector2 径 = (e.位置 - 玩家).normalized;
                if (径.sqrMagnitude < .1f) 径 = Vector2.right;
                Vector2 侧 = new Vector2(-径.y, 径.x) * ((e.序号 + e.已攻击次数) % 2 == 0 ? 1 : -1);
                float 半径 = Mathf.Min(P("movement.preferred_range"), Mathf.Max(1, 起手距离 - P("movement.approach_margin")));
                Vector2 点 = 玩家 + 径 * 半径 + 侧 * P("movement.orbit_step");
                if (!有效落点(e.位置, 点)) 点 = 玩家 + 径 * 半径 - 侧 * P("movement.orbit_step");
                e.战术站位 = 有效落点(e.位置, 点) ? 点 : e.位置;
                e.战术决策秒 = P("movement.decision_seconds");
            }
            移动(e, e.战术站位, dt); return;
        }
        if (可攻击 && !地图.生存大图) return;
        float 站距 = 技.辅助 ? P("movement.assist_range") : 远程 ? P("movement.preferred_range") : Mathf.Min(e.攻击范围 - P("movement.approach_margin"), P("movement.melee_stand"));
        Vector2 终 = 远程 ? 玩家 : 追击目标(e, 玩家);
        if(地图.生存大图&&!可攻击)
        {
            if(e.战术决策秒<=0)
            {
                e.战术站位=选择地形站位(e,玩家,远程,起手距离,站距);
                e.战术决策秒=P("movement.decision_seconds")*(1+((uint)(地图.种子^e.序号)&7)/7f*P("movement.decision_jitter"));
            }
            终=地图.可站立(e.战术站位)?e.战术站位:玩家;
        }
        if (!地图.生存大图 && 距 < P("movement.ring_distance"))
        {
            float 角 = (e.序号 % 8) * Mathf.PI / 4;
            var 点 = 玩家 + new Vector2(Mathf.Cos(角), Mathf.Sin(角)) * 站距;
            if (有效落点(玩家, 点)) 终 = 点;
        }
        移动(e, 终, dt);
    }
    Vector2 选择地形站位(战斗敌人 e,Vector2 玩家,bool 远程,float 射程,float 站距)
    {
        int 角色=天帝敌种配置.角色(e.物种);
        if(角色>=3&&e.掩护队友!=null&&e.掩护队友.存活)
        {
            Vector2 后=(e.掩护队友.位置-玩家).normalized;
            Vector2 掩护=e.掩护队友.位置+后*P("movement.support_cover");
            if(地图.可站立(掩护))return 掩护;
        }
        Vector2 最佳=玩家;float 分=float.MaxValue;
        if(角色>=3)
        {
            foreach(var b in 地图.随机障碍)
            {
                if((b.位置-e.位置).sqrMagnitude>P("movement.cover_search")*P("movement.cover_search"))continue;
                Vector2 后=(b.位置-玩家).normalized;
                Vector2 点=b.位置+后*(Mathf.Max(b.半径.x,b.半径.y)+P("movement.cover_gap"));
                if(!地图.可站立(点)||寻路.无遮挡(玩家,点))continue;
                float 值=(点-e.位置).sqrMagnitude;if(值<分){最佳=点;分=值;}
            }
            if(分<float.MaxValue)return 最佳;
        }
        if(!远程)
        {
            int 格=(int)P("movement.ring_slots");
            float 角=(e.序号%格)*Mathf.PI*2/格+((uint)地图.种子%360)*Mathf.Deg2Rad;
            float r=Mathf.Max(.5f,Mathf.Min(e.攻击范围-P("movement.range_margin")-.05f,站距+e.序号%3*P("movement.ring_variation")));
            for(int i=0;i<4;i++)
            {
                float a=角+(i==0?0:i%2==0?1:-1)*(i+1)*Mathf.PI/格;
                Vector2 p=玩家+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                if(地图.可站立(p)&&寻路.无遮挡(玩家,p))return p;
            }
            return 追击目标(e,玩家);
        }
        Vector2 径=(e.位置-玩家).normalized;if(径.sqrMagnitude<.01f)径=Vector2.right;
        float 朝=Mathf.Atan2(径.y,径.x)+(e.序号%2==0?1:-1)*天帝战斗润色.取("crossfire_angle")*Mathf.Deg2Rad,
            半径=Mathf.Min(P("movement.preferred_range"),Mathf.Max(1,射程-P("movement.approach_margin")));
        int 样本=(int)P("movement.peek_samples");
        for(int i=0;i<样本;i++)
        {
            float a=朝+(i%2==0?1:-1)*((i+1)/2)*Mathf.PI*2/样本;
            Vector2 p=玩家+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*半径;
            if(!地图.可站立(p)||!寻路.无遮挡(p,玩家))continue;
            float 值=(p-e.位置).sqrMagnitude+(有效落点(e.位置,p)?0:25);
            if(值<分){分=值;最佳=p;}
        }
        if(分<float.MaxValue)return 最佳;
        // 没有射击角度时先向侧翼出口走，避免一直抵住掩体。
        for(int i=0;i<2;i++)
        {
            Vector2 p=e.位置+new Vector2(-径.y,径.x)*P("movement.peek_step")*(i==0?1:-1);
            if(有效落点(e.位置,p))return p;
        }
        return 玩家;
    }
    double 技倍率(战斗敌人 e, int id)
    {
        if (天帝敌种配置.角色(e.物种) >= 3 && e.物种 != 14 && !敌技能.读取(id).辅助) return P("support.fallback_damage");
        if (id == 9 && e.布点.级别 == 战斗敌人级别.精英) return P("elite.charge_damage");
        if (id == 16 && e.物种 == 12) return 天帝数值.敌参数(战斗敌人级别.头目, "skills.2");
        return 敌技能.读取(id).倍率;
    }
    战斗敌术 建效果(战斗敌人 e, 敌技能 s, 敌术命中 命中, Vector2 起, Vector2 终)
        => new 战斗敌术 { 来源 = e, 技能 = s, 起点 = 起, 位置 = 起, 终点 = 终, 方向 = (终 - 起).normalized,
            命中 = 命中, 包 = 天帝敌种配置.伤害包(e, s.编号, 技倍率(e, s.编号)), 半径 = s.宽度 };
    void 留冲击(战斗敌人 e, 敌技能 s, Vector2 点, float 半径)
    {
        if (冲击.Count >= P("presentation.glow_visible_max")) return;
        冲击.Add(new 战斗敌术 { 来源 = e, 技能 = s, 位置 = 点, 终点 = 点, 方向 = e.锁定方向, 半径 = 半径, 寿命 = P("presentation.impact_seconds"), 冲击演出 = true });
    }
    public void 释放(战斗敌人 e, Vector2 玩家)
    {
        var s = 敌技能.读取(e.技能编号); 技能释放数++; 技能统计[s.编号]++;
        技能演出释放?.Invoke(e,s);
        if (e.物种 == 14 && e.地图档位 >= P("growth.shake_unlock") && s.大招) 震屏秒 = P("growth.shake_seconds");
        if (s.辅助) { 完成辅助(e, s); return; }
        if (效果.Count >= 成长上限("projectiles") && (s.类型 == 敌技能类型.飞弹 || s.类型 == 敌技能类型.投掷 || s.类型 == 敌技能类型.冲锋 || s.类型 == 敌技能类型.跳跃)) return;
        var 命中 = new 敌术命中(); var 包 = 天帝敌种配置.伤害包(e, s.编号, 技倍率(e, s.编号));
        if (s.类型 == 敌技能类型.单击 || s.类型 == 敌技能类型.扇面 || s.类型 == 敌技能类型.直线 || s.类型 == 敌技能类型.自周 || s.类型 == 敌技能类型.归潮)
            留冲击(e, s, s.类型 == 敌技能类型.单击 ? e.攻击落点 : e.位置, s.类型 == 敌技能类型.单击 ? e.攻击范围 : e.物种 == 12 && s.类型 == 敌技能类型.自周 ? (float)天帝数值.敌参数(战斗敌人级别.头目, "skill_range") : s.距离);
        switch (s.类型)
        {
            case 敌技能类型.单击:
                if (Vector2.Distance(e.位置, 玩家) <= e.攻击范围 && Vector2.Distance(e.攻击落点, 玩家) <= e.攻击范围 && 寻路.无遮挡(e.位置, 玩家)) 命中目标(包, 玩家, true);
                break;
            case 敌技能类型.扇面:
                if (在扇面(e.位置, e.锁定方向, 玩家, e.物种 == 14 ? P("boss.fan_range") : s.距离, e.物种 == 14 ? P("boss.fan_angle") : s.宽度) && 寻路.无遮挡(e.位置, 玩家)) 命中目标(包, 玩家);
                break;
            case 敌技能类型.直线:
                if (线距(玩家, e.位置, e.位置 + e.锁定方向 * s.距离) <= s.宽度 * .5f && 寻路.无遮挡(e.位置, 玩家))
                { bool hit = 命中目标(包, 玩家); if (hit && e.布点.级别 == 战斗敌人级别.精英 && e.地图档位 >= P("control.root_unlock")) 控制(true); }
                break;
            case 敌技能类型.自周:
                float 半径 = e.物种 == 12 ? (float)天帝数值.敌参数(战斗敌人级别.头目, "skill_range") : s.距离;
                if (Vector2.Distance(e.位置, 玩家) <= 半径 && 寻路.无遮挡(e.位置, 玩家)) 命中目标(包, 玩家);
                break;
            case 敌技能类型.飞弹:
                int 数 = s.数量;
                float 角 = s.宽度;
                if (s.编号 == 4 && e.布点.级别 == 战斗敌人级别.精英) { 数 = (int)P("elite.fan_count"); 角 = P("elite.fan_angle"); }
                for (int i = 0; i < 数 && 效果.Count < 成长上限("projectiles"); i++)
                {
                    Vector2 起 = e.位置;
                    if (s.编号 == 18) 起 += new Vector2(-e.锁定方向.y, e.锁定方向.x) * (i - 1) * P("boss.mirror_offset");
                    if (!有效落点(e.位置, 起)) 起 = e.位置;
                    Vector2 向 = 转(e.锁定方向, (i - (数 - 1) * .5f) * 角);
                    var a = 建效果(e, s, 命中, 起, 起 + 向 * s.距离); a.寿命 = s.距离 / s.速度; 效果.Add(a);
                }
                break;
            case 敌技能类型.冲锋: case 敌技能类型.跳跃:
                var 动 = 建效果(e, s, 命中, e.位置, e.攻击落点); 动.位移 = true; 动.跃击 = s.类型 == 敌技能类型.跳跃;
                动.寿命 = s.速度; 效果.Add(动); break;
            case 敌技能类型.投掷:
                var 球 = 建效果(e, s, 命中, e.位置, e.攻击落点); 球.寿命 = s.速度; 效果.Add(球); break;
            case 敌技能类型.归潮:
                if (在扇面(e.位置, e.锁定方向, 玩家, s.距离, s.宽度) && 寻路.无遮挡(e.位置, 玩家)) 命中目标(包, 玩家);
                var 二段命中 = new 敌术命中();
                for (int i = 0; i < s.数量; i++)
                {
                    var 垂 = new Vector2(-e.锁定方向.y, e.锁定方向.x);
                    var 落 = e.攻击落点 + 垂 * (i == 0 ? -1 : 1) * P("boss.second_offset");
                    if (!地图.可站立(落, P("movement.body_radius"))) continue;
                    var a = 建效果(e, s, 二段命中, e.位置, 落); a.二段 = true; a.延迟 = P("boss.second_delay");
                    a.寿命 = 0; a.半径 = P("boss.second_radius"); a.包 = 天帝敌种配置.伤害包(e, 19, P("boss.second_damage")); 效果.Add(a);
                }
                break;
        }
    }
    public void 完成辅助(战斗敌人 e, 敌技能 s)
    {
        foreach (var a in e.辅助目标)
        {
            if (!辅助合法(e, a, s)) continue;
            bool 治 = s.类型 == 敌技能类型.治疗;
            float 剩 = 初始生命和 * P(治 ? "support.heal_global_cap" : "support.shield_global_cap") - (治 ? 实际治疗 : 实际授盾);
            float 上限 = a.最大血量 * P(治 ? "support.heal_target_cap" : "support.shield_target_cap") - (治 ? a.累计恢复 : a.累计授盾);
            float 量 = Mathf.Min(剩, 上限, a.最大血量 * P(治 ? "support.heal_fraction" : "support.shield_fraction"));
            if (治) { 量 = Mathf.Min(量, a.最大血量 - a.血量); a.血量 += Mathf.Max(0, 量); a.累计恢复 += Mathf.Max(0, 量); 实际治疗 += Mathf.Max(0, 量); }
            else { a.护盾量 = Mathf.Max(0, 量); a.盾剩余 = P("support.shield_duration"); a.累计授盾 += Mathf.Max(0, 量); 实际授盾 += Mathf.Max(0, 量); }
            if (量 > 0) 光线.Add(new 辅助光线 { 起 = e.位置, 终 = a.位置, 盾 = !治 });
        }
        e.辅助目标.Clear(); e.战术决策秒 = 0;
    }
    void 控制(bool 根)
    {
        if (!根&&特性免疫?.Invoke()==true || 控制免疫 > 0 || 减速剩余 > 0 || 束缚剩余 > 0) return;
        if (根) 束缚剩余 = P("control.root_duration"); else 减速剩余 = P("control.slow_duration");
    }
    public void 取消功能位移(战斗敌人 敌)
    {
        for (int i = 效果.Count - 1; i >= 0; i--) if (效果[i].来源 == 敌 && 效果[i].位移)
        { 效果.RemoveAt(i); 敌.跳跃高度 = 0; 敌.行动 = 敌人行动.后摇; 敌.后摇秒 = P("movement.retry"); }
    }
    public void 推进效果(Vector2 主目标, float dt)
    {
        突进协作等待=Mathf.Max(0,突进协作等待-dt);
        if(特性免疫?.Invoke()==true)减速剩余=0;
        bool 受控 = 减速剩余 > 0 || 束缚剩余 > 0;
        台词秒 = Mathf.Max(0, 台词秒 - dt); if (台词秒 <= 0) 王台词 = "";
        震屏秒 = Mathf.Max(0, 震屏秒 - dt);
        减速剩余 = Mathf.Max(0, 减速剩余 - dt); 束缚剩余 = Mathf.Max(0, 束缚剩余 - dt); 控制免疫 = Mathf.Max(0, 控制免疫 - dt);
        if (受控 && 减速剩余 <= 0 && 束缚剩余 <= 0) 控制免疫 = P("control.immunity");
        foreach (var e in 敌人)
        {
            e.形态提示秒 = Mathf.Max(0, e.形态提示秒 - dt); e.盾禁用 = Mathf.Max(0, e.盾禁用 - dt);
            if (e.护盾量 > 0) { e.盾剩余 -= dt; if (e.盾剩余 <= 0) { e.护盾量 = 0; e.盾禁用 = P("support.shield_lockout"); } }
        }
        for (int i = 光线.Count - 1; i >= 0; i--) { 光线[i].剩余 -= dt; if (光线[i].剩余 <= 0) 光线.RemoveAt(i); }
        for (int i = 冲击.Count - 1; i >= 0; i--) { 冲击[i].已过 += dt; if (冲击[i].已过 >= 冲击[i].寿命) 冲击.RemoveAt(i); }
        for (int i = 效果.Count - 1; i >= 0; i--)
        {
            var a = 效果[i]; var s = a.技能;
            Vector2 玩家=特性目标?.Invoke(a.来源,主目标)??主目标;
            if (地图.横向区域 && a.位移 && a.来源.行动 == 敌人行动.归巢)
            { a.来源.跳跃高度 = 0; 效果.RemoveAt(i); continue; }
            if (!a.来源.存活 && a.位移) { a.来源.跳跃高度 = 0; 效果.RemoveAt(i); continue; }
            if (!a.来源.存活 && a.地面) a.寿命 = Mathf.Min(a.寿命, a.已过 + P("limits.death_zone_seconds"));
            if (a.延迟 > 0) { a.延迟 -= dt; continue; }
            a.已过 += dt;
            if (a.冲击演出) { if (a.已过 >= a.寿命) 效果.RemoveAt(i); continue; }
            if (a.地面)
            {
                int tick = Mathf.FloorToInt(a.已过 / P("limits.tick_interval"));
                if (tick > a.已跳数)
                {
                    a.已跳数 = tick;
                    if (Vector2.Distance(a.终点, 玩家) <= a.半径 && !a.命中.地面次数.Contains(tick) && 寻路.无遮挡(a.终点, 玩家))
                    { a.命中.地面次数.Add(tick); 命中目标(a.包, 玩家); }
                }
                if (a.已过 >= a.寿命) 效果.RemoveAt(i);
                continue;
            }
            if (a.二段)
            {
                if (!a.命中.已命中 && Vector2.Distance(a.终点, 玩家) <= a.半径 && 寻路.无遮挡(a.终点, 玩家))
                { a.命中.已命中 = true; 命中目标(a.包, 玩家); }
                留冲击(a.来源, a.技能, a.终点, a.半径);
                效果.RemoveAt(i); continue;
            }
            if (a.位移)
            {
                if (a.来源.束缚剩余秒 > 0) { a.已过 -= dt; continue; }
                Vector2 前 = a.来源.位置;
                Vector2 点 = Vector2.Lerp(a.起点, a.终点, Mathf.Clamp01(a.已过 / a.寿命));
                if (地图.横向区域 && 地图.王房范围.Contains(地图.所在格(点)) != (a.来源.布点.级别 == 战斗敌人级别.王级))
                { a.来源.跳跃高度 = 0; 效果.RemoveAt(i); continue; }
                a.来源.位置 = 地图.移动(前, (点 - 前).normalized, Vector2.Distance(前, 点)); a.位置 = a.来源.位置;
                a.来源.跳跃高度 = a.跃击 ? Mathf.Sin(Mathf.Clamp01(a.已过 / a.寿命) * Mathf.PI) * 1.5f : 0;
                if (!a.跃击 && !a.命中.已命中 && 线距(玩家, 前, a.位置) <= a.半径 * .5f && 寻路.无遮挡(a.位置, 玩家))
                { a.命中.已命中 = true; 命中目标(a.包, 玩家, true); }
                if (a.已过 >= a.寿命)
                {
                    a.来源.跳跃高度 = 0;
                    if (a.跃击 && !a.命中.已命中 && Vector2.Distance(a.位置, 玩家) <= a.半径 && 寻路.无遮挡(a.位置, 玩家)) { a.命中.已命中 = true; 命中目标(a.包, 玩家); }
                    if (a.跃击) 留冲击(a.来源, s, a.位置, a.半径);
                    效果.RemoveAt(i);
                }
                continue;
            }
            if (s.类型 == 敌技能类型.投掷)
            {
                a.位置 = Vector2.Lerp(a.起点, a.终点, Mathf.Clamp01(a.已过 / a.寿命));
                if (a.已过 < a.寿命) continue;
                if (!a.命中.已命中 && Vector2.Distance(a.终点, 玩家) <= a.半径 && 寻路.无遮挡(a.终点, 玩家)) { a.命中.已命中 = true; 命中目标(a.包, 玩家); }
                int 地数 = 0; foreach (var 区 in 效果) if (区.地面) 地数++;
                a.半径 = Mathf.Min(a.半径, P("limits.zone_radius_max"));
                if (地数 < P("limits.zones") && 有逃路(a.终点, 玩家, a.半径))
                {
                    a.地面 = true; a.已过 = 0; a.寿命 = P("limits.ground_seconds"); a.位置 = a.终点;
                    a.技能 = 敌技能.读取(6); 技能统计[6]++;
                    a.包 = 天帝敌种配置.伤害包(a.来源, 6, P("limits.ground_damage")); a.命中 = new 敌术命中();
                }
                else 效果.RemoveAt(i);
                continue;
            }
            Vector2 原 = a.位置, 新 = 原 + a.方向 * s.速度 * dt;
            if (!有效落点(原, 新)) { 效果.RemoveAt(i); continue; }
            a.位置 = 新;
            if (!a.命中.已命中 && 线距(玩家, 原, 新) <= P("movement.body_radius"))
            {
                a.命中.已命中 = true; bool 中 = 命中目标(a.包, 玩家, true); if (中 && s.效果 == 1 && a.来源.物种 != 14) 控制(false);
                效果.RemoveAt(i); continue;
            }
            if (a.已过 >= a.寿命) 效果.RemoveAt(i);
        }
    }
    bool 有逃路(Vector2 落, Vector2 玩家, float 半径)
    {
        int 安全 = 0;
        for (int i = 0; i < 8; i++)
        {
            float 角 = i * Mathf.PI / 4; Vector2 终 = 玩家 + new Vector2(Mathf.Cos(角), Mathf.Sin(角)) * (半径 + P("limits.escape_margin"));
            if (!有效落点(玩家, 终) || Vector2.Distance(终, 落) < 半径 + P("limits.escape_padding")) continue;
            bool 重 = false; foreach (var a in 效果) if (a.地面 && Vector2.Distance(终, a.终点) < a.半径 + P("limits.escape_padding")) 重 = true;
            if (!重) 安全++;
        }
        return 安全 >= P("limits.escape_count");
    }
    public void 清理()
    {
        效果.Clear(); 冲击.Clear(); 光线.Clear(); 减速剩余 = 束缚剩余 = 控制免疫 = 0;
        王台词 = ""; 台词秒 = 震屏秒 = 0;
        foreach (var e in 敌人) { e.辅助目标.Clear(); e.跳跃高度 = 0; e.战术决策秒 = 0; e.掩护队友 = null; e.战术站位 = e.位置; }
    }
    public static float 线距(Vector2 点, Vector2 起, Vector2 终)
    { var d = 终 - 起; float t = d.sqrMagnitude <= .00001f ? 0 : Mathf.Clamp01(Vector2.Dot(点 - 起, d) / d.sqrMagnitude); return Vector2.Distance(点, 起 + d * t); }
    static bool 在扇面(Vector2 起, Vector2 向, Vector2 点, float 距, float 角)
        => Vector2.Distance(起, 点) <= 距 && Vector2.Angle(向, 点 - 起) <= 角 * .5f;
    static Vector2 转(Vector2 向, float 角)
    { float a = 角 * Mathf.Deg2Rad; return new Vector2(向.x * Mathf.Cos(a) - 向.y * Mathf.Sin(a), 向.x * Mathf.Sin(a) + 向.y * Mathf.Cos(a)); }
}
