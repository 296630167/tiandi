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
    int 上次自动槽 = -1;
    int 自动展示轮次掩码;
    public int 自动展示轮次完成次数 { get; private set; }
    readonly double[] 自动技能节拍结束 = new double[6];
    int 缓冲技能槽 = -1;
    Vector2 缓冲技能方向;
    float 缓冲技能剩余;
    double 战斗时钟, 无敌结束, 闪避冷却结束;
    public float 技能冷却剩余 => 发射冷却;
    public float 技能冷却总时长 { get; private set; }
    public float 闪避冷却总时长 { get; private set; }
    // 战斗场景会显式开启纯AI；模型默认保持兼容，便于旧版隔离验收直接调用手动接口。
    public bool 纯AI模式 { get; set; } = false;
    public bool 自动攻击启用 { get; set; } = false;
    public float 闪避冷却剩余 => (float)System.Math.Max(0, 闪避冷却结束 - 战斗时钟);
    public float 闪避无敌剩余 => (float)System.Math.Max(0, 无敌结束 - 战斗时钟);
    public bool 闪避无敌中 => 无敌结束 - 战斗时钟 > .000001;
    public float 技能输入缓冲剩余 => Mathf.Max(0, 缓冲技能剩余);
    public int 技能输入缓冲槽 => 缓冲技能剩余 > .00001f ? 缓冲技能槽 : -1;
    public float 自动技能节拍剩余(int 槽) => 槽 < 0 || 槽 >= 自动技能节拍结束.Length
        ? 0 : (float)System.Math.Max(0, 自动技能节拍结束[槽] - 战斗时钟);
    public float 技能预警剩余 => 前摇剩余 > 0 ? 前摇剩余 : 0;
    public Vector2 技能预警方向 => 前摇方向;
    public int 技能预警槽 => 前摇剩余 >= 0 && 前摇参数 != null ? ((6 - 前摇参数.通路) % 6) : -1;
    public event Action<战斗操作结果, int, bool> 自动操作反馈;
    public static float 技能灵力消耗 => (float)天帝数值.取("player.skill_mana_cost");
    public static int 技能通路(int 槽) => (6 - 槽) % 6;
    public bool 技能已解封(int 槽) => 槽 >= 0 && 槽 < 6 && 道纹.通路开放(技能通路(槽));
    public bool 技能有链路(int 槽) => 槽 >= 0 && 槽 < 6 && 道纹.通路参与射击(技能通路(槽));
    public string 技能名称(int 槽)
    {
        if (!技能有链路(槽)) return 技能已解封(槽) ? "无技能链路" : "未解封";
        int 路 = 技能通路(槽);
        var 起点 = 读取通路参数(路).顺序计划?.起点;
        if (起点 != null && 起点.功能 != 道纹功能.旧版) return 起点.功能.ToString();
        return "灵力弹";
    }
    static bool 方向有效(Vector2 向) => !float.IsNaN(向.x) && !float.IsNaN(向.y)
        && !float.IsInfinity(向.x) && !float.IsInfinity(向.y) && 向.sqrMagnitude > .000001f;

    static float 有效间隔(普攻参数 参数)
    {
        float 间隔 = 参数 == null ? 0 : 参数.间隔;
        return float.IsNaN(间隔) || float.IsInfinity(间隔) ? .05f : Mathf.Max(.02f,间隔);
    }
    float 计算前摇(float 间隔)
    {
        float 比例 = (float)天帝数值.取("player.attack_windup_fraction");
        if(float.IsNaN(比例)||float.IsInfinity(比例)) 比例=.35f;
        float 前摇 = 射击前摇;
        if(float.IsNaN(前摇)) 前摇=0;
        if(float.IsInfinity(前摇)) 前摇=间隔;
        return Mathf.Min(Mathf.Max(0,前摇),间隔*Mathf.Clamp01(比例));
    }
    void 设置主动节奏(普攻参数 参数)
    {
        float 间隔 = 有效间隔(参数);
        发射冷却 = 技能冷却总时长 = 间隔;
        前摇剩余 = 计算前摇(间隔);
    }

    public 战斗操作结果 尝试释放技能(int 槽, Vector2 方向, Vector2 位置)
    {
        if (玩家死亡 || !地图.可站立(位置)) return 战斗操作结果.无法操作;
        if (!技能已解封(槽)) return 战斗操作结果.未解封;
        if (!技能有链路(槽)) return 战斗操作结果.无技能链路;
        if (!方向有效(方向)) return 战斗操作结果.无效方向;
        if (发射冷却 > .00001f || 前摇剩余 >= 0)
        {
            // 只记住最后一次有效按键，窗口很短，避免连点堆积成不可预期的自动连发。
            if (发射冷却 > .00001f && 方向有效(方向))
            {
                // 缓冲只保留玩家的瞄准意图；真正释放时取当前站位，避免移动后从旧坐标发射。
                缓冲技能槽 = 槽; 缓冲技能方向 = 方向.normalized;
                缓冲技能剩余 = 天帝敌种配置.取("presentation.polish.skill_input_buffer_seconds");
            }
            return 战斗操作结果.冷却中;
        }
        // 冷却结束后的直接输入优先级最高，避免旧缓冲在下一细步再次触发。
        缓冲技能槽 = -1; 缓冲技能剩余 = 0;
        var 参数 = 读取通路参数(技能通路(槽));
        if (!参数.已激活) return 战斗操作结果.未解封;
        if (!主角.尝试消耗灵力(技能灵力消耗)) return 战斗操作结果.灵力不足;
        玩家 = 位置; 当前通路 = 参数.通路; 前摇通路 = 参数.通路;
        前摇参数 = 参数; 前摇方向 = 方向.normalized; 前摇自动释放 = false;
        设置主动节奏(参数);
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
            if (自动)
            {
                // 目标在前摇期间失效时，这次没有完成展示；退还资源并立即允许下一条链路重试。
                主角.回复灵力(技能灵力消耗);
                发射冷却 = 0;
                for (int i = 0; i < 自动技能节拍结束.Length; i++)
                    if (技能通路(i) == 参数.通路) 自动技能节拍结束[i] = 战斗时钟;
            }
            return;
        }
        普通释放次数++; 特性根释放(参数.通路, 自动 ? (Vector2?)null : 前摇方向);
        当前通路 = 参数.通路; 通路释放次数[当前通路]++;
        if (自动) 记录自动链路成功(参数.通路);
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
        缓冲技能剩余 = Mathf.Max(0, 缓冲技能剩余 - 秒);
        主角.回复灵力(主角.灵力 * (float)天帝数值.取("player.mana_regen_fraction") * 秒);
        // 只推进本步开始前已有的回响，避免新射击的回响提前一个细步。
        if (回响剩余 >= 0)
        {
            回响剩余 -= 秒;
            if (回响剩余 <= .000001f)
            {
                if (回响参数 != null && 发射根(回响参数, false, 回响自动释放 ? (Vector2?)null : 回响方向)) 回响次数++;
                回响剩余 = -1; 回响参数 = null; 回响自动释放 = false;
            }
        }
        if (前摇剩余 >= 0)
        {
            前摇剩余 -= 秒;
            if (前摇剩余 <= .000001f) { 前摇剩余 = -1; 完成主动释放(); }
        }
        if (前摇剩余 < 0 && 发射冷却 <= .00001f)
        {
            bool 已触发缓冲 = false;
            if (缓冲技能剩余 > .00001f && 缓冲技能槽 >= 0)
            {
                int 槽 = 缓冲技能槽; var 向 = 缓冲技能方向; var 位 = 玩家;
                缓冲技能槽 = -1; 缓冲技能剩余 = 0;
                var 结果 = 尝试释放技能(槽, 向, 位);
                已触发缓冲 = 结果 == 战斗操作结果.成功;
                if (!已触发缓冲) 自动操作反馈?.Invoke(结果, 槽, false);
            }
            else 缓冲技能槽 = -1;
            if (!已触发缓冲) 尝试开始自动释放();
        }
    }

    static 道纹功能 自动技能功能(普攻参数 参数)
    {
        return 参数?.顺序计划?.起点?.功能 ?? 道纹功能.旧版;
    }

    float 自动技能权重(普攻参数 参数, int 目标, int 附近目标数, float 目标距离)
    {
        float 权重 = 1f;
        var 功能 = 自动技能功能(参数);
        bool 多目标 = 附近目标数 >= 3;
        bool 近战圈 = 目标距离 <= 4.5f;
        bool 远距离 = 目标距离 >= 8f;
        bool 王 = 目标 >= 0 && 目标 < 敌人数据.Count && 敌人数据[目标].布点.级别 == 战斗敌人级别.王级;
        switch (功能)
        {
            case 道纹功能.齐射: case 道纹功能.分裂: case 道纹功能.连锁:
            case 道纹功能.扇射: case 道纹功能.环射: case 道纹功能.十字: case 道纹功能.爆破:
            case 道纹功能.震荡: case 道纹功能.刃波: case 道纹功能.剑雨: case 道纹功能.灵网:
                if (多目标) 权重 *= 2.35f;
                else if (!王) 权重 *= .88f;
                break;
            case 道纹功能.穿透: case 道纹功能.光束: case 道纹功能.游龙: case 道纹功能.飞轮:
            case 道纹功能.蓄势: case 道纹功能.加速:
                if (远距离 || 王) 权重 *= 1.75f;
                break;
            case 道纹功能.旋刃: case 道纹功能.灵鞭: case 道纹功能.地雷:
            case 道纹功能.击退: case 道纹功能.束缚:
                if (近战圈) 权重 *= 1.65f;
                break;
            case 道纹功能.陨落: case 道纹功能.烙印: case 道纹功能.延时:
                if (王 || 目标距离 > 6f) 权重 *= 1.35f;
                break;
        }
        if (参数 != null && 参数.数量 > 1) 权重 *= 多目标 ? 1.18f : .96f;
        if (上次自动槽 >= 0 && 参数 != null && 参数.通路 == 技能通路(上次自动槽)) 权重 *= .58f;
        return Mathf.Max(.05f, 权重);
    }

    int 战术自动技能槽(int 目标)
    {
        int 附近目标数 = 0;
        float 目标距离 = 0;
        if (目标 >= 0 && 目标 < 敌人数据.Count) 目标距离 = Vector2.Distance(玩家, 敌人数据[目标].位置);
        for (int i = 0; i < 敌人数据.Count; i++)
        {
            var 敌 = 敌人数据[i];
            if (!敌.存活 || Vector2.Distance(玩家, 敌.位置) > 6f || !寻路.无遮挡(玩家, 敌.位置)) continue;
            附近目标数++;
        }
        int 有效掩码 = 0;
        for (int 槽 = 0; 槽 < 6; 槽++) if (技能有链路(槽) && 读取通路参数(技能通路(槽)).已激活) 有效掩码 |= 1 << 槽;
        自动展示轮次掩码 &= 有效掩码;
        int 未展示掩码 = 有效掩码 & ~自动展示轮次掩码;
        int 选中 = -1; float 总权重 = 0;
        bool 有轮换候选 = 未展示掩码 != 0;
        for (int 槽 = 0; 槽 < 6; 槽++)
        {
            if (槽 == 上次自动槽 || !技能已解封(槽) || !技能有链路(槽)) continue;
            var 候选参数 = 读取通路参数(技能通路(槽));
            if (候选参数.已激活 && 自动技能节拍剩余(槽) <= .00001f) { 有轮换候选 = true; break; }
        }
        for (int 槽 = 0; 槽 < 6; 槽++)
        {
            if (!技能已解封(槽) || !技能有链路(槽)) continue;
            var 参数 = 读取通路参数(技能通路(槽));
            if (!参数.已激活) continue;
            // 共享冷却之外再给每条链路一个很短的节拍；有替代链路时轮换，只有一条链路时不额外拖慢。
            if (有轮换候选 && (未展示掩码 & (1 << 槽)) == 0) continue;
            if (!有轮换候选 && 槽 == 上次自动槽 && 有效掩码 != (1 << 槽)) continue;
            if (自动技能节拍剩余(槽) > .00001f && 有轮换候选) continue;
            float 权重 = 自动技能权重(参数, 目标, 附近目标数, 目标距离);
            总权重 += 权重;
            if (战斗随机.NextDouble() * 总权重 < 权重) 选中 = 槽;
        }
        return 选中;
    }

    void 记录自动链路成功(int 通路)
    {
        for (int 槽 = 0; 槽 < 6; 槽++)
            if (技能通路(槽) == 通路 && 技能有链路(槽)) { 自动展示轮次掩码 |= 1 << 槽; break; }
        int 有效掩码 = 0;
        for (int 槽 = 0; 槽 < 6; 槽++) if (技能有链路(槽) && 读取通路参数(技能通路(槽)).已激活) 有效掩码 |= 1 << 槽;
        if (有效掩码 != 0 && (自动展示轮次掩码 & 有效掩码) == 有效掩码)
        {
            自动展示轮次完成次数++;
            自动展示轮次掩码 = 0;
        }
    }

    void 尝试开始自动释放()
    {
        if ((!自动攻击启用 && !纯AI模式) || 玩家死亡) return;
        int 目标 = 找目标(玩家, 普攻参数.索敌距离, null, true);
        if (目标 < 0) return;
        int 槽 = 战术自动技能槽(目标);
        if (槽 < 0) return;
        if (!主角.尝试消耗灵力(技能灵力消耗))
        {
            if (!自动缺灵力已提示) { 自动缺灵力已提示 = true; 自动操作反馈?.Invoke(战斗操作结果.灵力不足, 槽, false); }
            return;
        }
        自动缺灵力已提示 = false;
        上次自动槽 = 槽;
        自动技能节拍结束[槽] = 战斗时钟 + Mathf.Max(.16f, 有效间隔(读取通路参数(技能通路(槽))) * 1.25f);
        var 参数 = 读取通路参数(技能通路(槽));
        当前通路 = 参数.通路; 前摇通路 = 参数.通路; 前摇参数 = 参数;
        前摇方向 = (敌人数据[目标].位置 - 玩家).normalized; 前摇自动释放 = true;
        设置主动节奏(参数);
        准备射击?.Invoke(前摇方向);
        if (前摇剩余 <= 0) { 前摇剩余 = -1; 完成主动释放(); }
    }
}
