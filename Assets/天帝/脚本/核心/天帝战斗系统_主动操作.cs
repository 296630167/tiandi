using System;
using UnityEngine;

public enum 战斗操作结果 { 成功, 未解封, 灵力不足, 冷却中, 无效方向, 无法操作, 无技能链路 }

public sealed partial class 天帝战斗系统
{
    普攻参数 前摇参数;
    Vector2 前摇方向, 回响方向;
    bool 前摇自动释放;
    bool 回响自动释放;
    bool 自动缺灵力已提示;
    double 战斗时钟, 无敌结束, 闪避冷却结束;
    public float 技能冷却剩余 => 发射冷却;
    public float 技能冷却总时长 { get; private set; }
    public float 闪避冷却总时长 { get; private set; }
    // 正式战斗默认自动攻击；编辑器手动操作验收可关闭自动驱动而复用同一套施法逻辑。
    public bool 自动攻击启用 { get; set; } = true;
    public float 闪避冷却剩余 => (float)System.Math.Max(0, 闪避冷却结束 - 战斗时钟);
    public float 闪避无敌剩余 => (float)System.Math.Max(0, 无敌结束 - 战斗时钟);
    public bool 闪避无敌中 => 无敌结束 - 战斗时钟 > .000001;
    public event Action<战斗操作结果, int, bool> 自动操作反馈;
    public static float 技能灵力消耗 => (float)天帝数值.取("player.skill_mana_cost");
    public static int 技能通路(int 槽) => (6 - 槽) % 6;
    public bool 技能已解封(int 槽) => 槽 >= 0 && 槽 < 6 && 道纹.通路开放(技能通路(槽));
    public bool 技能有链路(int 槽) => 槽 >= 0 && 槽 < 6 && 道纹.通路参与射击(技能通路(槽));
    public string 技能名称(int 槽)
    {
        if (!技能有链路(槽)) return 技能已解封(槽) ? "无技能链路" : "未解封";
        int 路 = 技能通路(槽);
        foreach (var 纹 in 道纹.道纹)
            if (纹.格子.HasValue && 道纹.弹槽道纹[路].Contains(纹.编号) && 纹.是顺序功能)
                return 纹.功能.ToString();
        return "灵力弹";
    }
    static bool 方向有效(Vector2 向) => !float.IsNaN(向.x) && !float.IsNaN(向.y)
        && !float.IsInfinity(向.x) && !float.IsInfinity(向.y) && 向.sqrMagnitude > .000001f;

    public 战斗操作结果 尝试释放技能(int 槽, Vector2 方向, Vector2 位置)
    {
        if (玩家死亡 || !地图.可站立(位置)) return 战斗操作结果.无法操作;
        if (!技能已解封(槽)) return 战斗操作结果.未解封;
        if (!技能有链路(槽)) return 战斗操作结果.无技能链路;
        if (!方向有效(方向)) return 战斗操作结果.无效方向;
        if (发射冷却 > .00001f || 前摇剩余 >= 0) return 战斗操作结果.冷却中;
        var 参数 = 读取通路参数(技能通路(槽));
        if (!参数.已激活) return 战斗操作结果.未解封;
        if (!主角.尝试消耗灵力(技能灵力消耗)) return 战斗操作结果.灵力不足;
        玩家 = 位置; 当前通路 = 参数.通路; 前摇通路 = 参数.通路;
        前摇参数 = 参数; 前摇方向 = 方向.normalized; 前摇自动释放 = false;
        发射冷却 = 技能冷却总时长 = 参数.间隔;
        前摇剩余 = Mathf.Min(射击前摇, 参数.间隔 * (float)天帝数值.取("player.attack_windup_fraction"));
        准备射击?.Invoke(前摇方向);
        if (前摇剩余 <= 0) { 前摇剩余 = -1; 完成主动释放(); }
        return 战斗操作结果.成功;
    }
    void 完成主动释放()
    {
        var 参数 = 前摇参数; 前摇参数 = null;
        bool 自动 = 前摇自动释放; 前摇自动释放 = false;
        if (玩家死亡 || 参数 == null) return;
        if (!发射根(参数, true, 自动 ? (Vector2?)null : 前摇方向))
        {
            if (自动) { 主角.回复灵力(技能灵力消耗); 发射冷却 = 0; }
            return;
        }
        普通释放次数++; 特性根释放(参数.通路);
        当前通路 = 参数.通路; 通路释放次数[当前通路]++;
        if (余响.记录释放("普攻") != null)
        { 回响剩余 = (float)天帝数值.取("talents.echo_delay"); 回响参数 = 参数; 回响方向 = 前摇方向; 回响自动释放 = 自动; }
    }
    public 战斗操作结果 尝试闪避()
    {
        if (玩家死亡) return 战斗操作结果.无法操作;
        if (闪避冷却剩余 > .00001f) return 战斗操作结果.冷却中;
        无敌结束 = 战斗时钟 + 天帝数值.取("player.dodge_invulnerability");
        double 时长 = 天帝数值.取("player.dodge_cooldown") / (1 + 主角.技能急速);
        闪避冷却总时长 = (float)时长;
        闪避冷却结束 = 战斗时钟 + 时长;
        return 战斗操作结果.成功;
    }
    void 推进主动计时(float 秒)
    {
        发射冷却 = Mathf.Max(0, 发射冷却 - 秒);
        主角.回复灵力(主角.灵力 * (float)天帝数值.取("player.mana_regen_fraction") * 秒);
        if (前摇剩余 >= 0)
        {
            前摇剩余 -= 秒;
            if (前摇剩余 <= .000001f) { 前摇剩余 = -1; 完成主动释放(); }
        }
        if (前摇剩余 < 0 && 发射冷却 <= .00001f)
            尝试开始自动释放();
        if (回响剩余 >= 0)
        {
            回响剩余 -= 秒;
            if (回响剩余 <= 0)
            {
                if (回响参数 != null && 发射根(回响参数, false, 回响自动释放 ? (Vector2?)null : 回响方向)) 回响次数++;
                回响剩余 = -1; 回响参数 = null; 回响自动释放 = false;
            }
        }
    }

    int 随机自动技能槽()
    {
        int 选中 = -1, 候选数 = 0;
        for (int 槽 = 0; 槽 < 6; 槽++)
        {
            if (!技能已解封(槽) || !技能有链路(槽)) continue;
            var 参数 = 读取通路参数(技能通路(槽));
            if (!参数.已激活) continue;
            候选数++;
            if (战斗随机.Next(候选数) == 0) 选中 = 槽;
        }
        return 选中;
    }

    void 尝试开始自动释放()
    {
        if (!自动攻击启用 || 玩家死亡) return;
        int 目标 = 找目标(玩家, 普攻参数.索敌距离, null);
        if (目标 < 0) return;
        int 槽 = 随机自动技能槽();
        if (槽 < 0) return;
        if (!主角.尝试消耗灵力(技能灵力消耗))
        {
            if (!自动缺灵力已提示) { 自动缺灵力已提示 = true; 自动操作反馈?.Invoke(战斗操作结果.灵力不足, 槽, false); }
            return;
        }
        自动缺灵力已提示 = false;
        var 参数 = 读取通路参数(技能通路(槽));
        当前通路 = 参数.通路; 前摇通路 = 参数.通路; 前摇参数 = 参数;
        前摇方向 = (敌人数据[目标].位置 - 玩家).normalized; 前摇自动释放 = true;
        发射冷却 = 技能冷却总时长 = 参数.间隔;
        前摇剩余 = Mathf.Min(射击前摇, 参数.间隔 * (float)天帝数值.取("player.attack_windup_fraction"));
        准备射击?.Invoke(前摇方向);
        if (前摇剩余 <= 0) { 前摇剩余 = -1; 完成主动释放(); }
    }
}
