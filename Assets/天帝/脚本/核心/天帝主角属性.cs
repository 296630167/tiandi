using System;
using UnityEngine;

// 每项衍生属性单独配置。默认系数为零，等待数值设计，不把建议当作正式规则。
[Serializable]
public sealed class 主角衍生属性配置
{
    [Min(0)] public float 基础值;
    [Min(0)] public float 力量系数;
    [Min(0)] public float 速度系数;
    [Min(0)] public float 智力系数;

    internal 主角衍生属性配置 副本() => new 主角衍生属性配置
    {
        基础值 = 有效值(基础值), 力量系数 = 有效值(力量系数),
        速度系数 = 有效值(速度系数), 智力系数 = 有效值(智力系数)
    };
    internal float 计算(float 力量, float 速度, float 智力, float 直接加成 = 0)
        => 截断((double)基础值 + (double)力量 * 力量系数 + (double)速度 * 速度系数 + (double)智力 * 智力系数 + 直接加成);
    internal static float 有效值(float 值) => float.IsNaN(值) || float.IsInfinity(值) || 值 < 0 ? 0 : 值;
    internal static float 截断(double 值) => (float)Math.Min(float.MaxValue, Math.Max(0, 值));
}

[Serializable]
public sealed class 主角属性配置
{
    // 正常流程始终为false；仅明确要求的测试夹具可启用自定义参数。
    public bool 使用测试数据;
    public string 名字 = "我";
    [Min(0)] public float 力量;
    [Min(0)] public float 速度;
    [Min(0)] public float 智力;
    public 主角衍生属性配置 血量 = new 主角衍生属性配置();
    public 主角衍生属性配置 灵力 = new 主角衍生属性配置();
    public 主角衍生属性配置 防御 = new 主角衍生属性配置();
    public 主角衍生属性配置 灵气护盾 = new 主角衍生属性配置();
    public 主角衍生属性配置 移动速度 = new 主角衍生属性配置();
    public 主角衍生属性配置 跑步速度 = new 主角衍生属性配置();
    public 主角衍生属性配置 攻击力 = new 主角衍生属性配置();
    public bool 攻击仅计算增益;

    internal 主角属性配置 副本() => new 主角属性配置
    {
        使用测试数据 = 使用测试数据,
        名字 = string.IsNullOrWhiteSpace(名字) ? "我" : 名字.Trim(),
        力量 = 主角衍生属性配置.有效值(力量), 速度 = 主角衍生属性配置.有效值(速度), 智力 = 主角衍生属性配置.有效值(智力),
        血量 = (血量 ?? new 主角衍生属性配置()).副本(), 灵力 = (灵力 ?? new 主角衍生属性配置()).副本(),
        防御 = (防御 ?? new 主角衍生属性配置()).副本(), 灵气护盾 = (灵气护盾 ?? new 主角衍生属性配置()).副本(),
        移动速度 = (移动速度 ?? new 主角衍生属性配置()).副本(), 跑步速度 = (跑步速度 ?? new 主角衍生属性配置()).副本(),
        攻击力 = (攻击力 ?? new 主角衍生属性配置()).副本(), 攻击仅计算增益 = 攻击仅计算增益
    };
}

// 会话数据，不依赖MonoBehaviour。血量/灵力/灵气护盾表示上限，当前资源另存。
public sealed class 天帝主角属性 : IDisposable
{
    public string 名字 => 配置.名字;
    public 天赋定义 天赋 => 道纹 != null ? 道纹.天赋 : null;
    public int 等级 => 道纹 != null ? 道纹.玩家等级 : 1;
    public float 力量 { get; private set; }
    public float 速度 { get; private set; }
    public float 智力 { get; private set; }
    public float 血量 { get; private set; }
    public float 灵力 { get; private set; }
    public float 防御 { get; private set; }
    public float 灵气护盾 { get; private set; }
    public float 移动速度 { get; private set; }
    public float 跑步速度 { get; private set; }
    public float 攻击力 { get; private set; }
    public float 攻击速度 { get; private set; }
    public float 暴击率 { get; private set; }
    public float 暴击倍率 { get; private set; }
    public float 抗性 { get; private set; }
    public float 技能急速 { get; private set; }
    public float 闪避率 { get; private set; }
    public double 单体期望DPS => 天帝数值.输出预算(道纹, this, 1);
    public double 八目标期望DPS => 天帝数值.输出预算(道纹, this, (int)天帝数值.取("shape.targets_test"));
    public double 综合战斗力 => 天帝数值.战斗力(道纹, this);
    public float 最大血量 => 血量;
    public float 最大灵力 => 灵力;
    public float 最大灵气护盾 => 灵气护盾;
    public float 当前血量 { get; private set; }
    public float 当前灵力 { get; private set; }
    public float 当前灵气护盾 { get; private set; }
    float 特性移速,特性急速,特性攻速,特性闪避;
    public void 设置特性增益(float 移,float 急,float 攻,float 闪)
    {
        if(特性移速==移&&特性急速==急&&特性攻速==攻&&特性闪避==闪)return;
        特性移速=移;特性急速=急;特性攻速=攻;特性闪避=闪;重算();
    }
    public event Action 属性改变;
    public event Action 基础配置改变;
    public 主角属性配置 导出配置() => 配置.副本();
    主角属性配置 配置;
    天帝道纹 道纹;
    bool 已释放;

    public 天帝主角属性(主角属性配置 初始配置 = null, 天帝道纹 道纹数据 = null)
    {
        配置 = (初始配置 ?? new 主角属性配置()).副本();
        道纹 = 道纹数据;
        if (道纹 != null) 道纹.状态改变 += 重算;
        重算();
        当前血量 = 血量; 当前灵力 = 灵力; 当前灵气护盾 = 灵气护盾;
    }
    public bool 设置名字(string 新名字)
    {
        if (已释放 || string.IsNullOrWhiteSpace(新名字)) return false;
        新名字 = 新名字.Trim();
        if (配置.名字 == 新名字) return true;
        配置.名字 = 新名字; 属性改变?.Invoke(); 基础配置改变?.Invoke(); return true;
    }
    public bool 更新配置(主角属性配置 新配置)
    {
        if (已释放 || 新配置 == null) return false;
        配置 = 新配置.副本(); 重算(); 基础配置改变?.Invoke(); return true;
    }
    public bool 设置当前资源(float 血, float 灵, float 盾)
    {
        if (已释放 || !有限(血) || !有限(灵) || !有限(盾)) return false;
        血 = Mathf.Clamp(血, 0, 血量); 灵 = Mathf.Clamp(灵, 0, 灵力); 盾 = Mathf.Clamp(盾, 0, 灵气护盾);
        if (血 == 当前血量 && 灵 == 当前灵力 && 盾 == 当前灵气护盾) return true;
        当前血量 = 血; 当前灵力 = 灵; 当前灵气护盾 = 盾; 属性改变?.Invoke(); return true;
    }
    static bool 有限(float 值) => !float.IsNaN(值) && !float.IsInfinity(值);
    public bool 尝试消耗灵力(float 消耗)
    {
        if (已释放 || 当前血量 <= 0 || !有限(消耗) || 消耗 < 0 || 当前灵力 < 消耗) return false;
        return 设置当前资源(当前血量, 当前灵力 - 消耗, 当前灵气护盾);
    }
    public void 回复灵力(float 数量)
    {
        if (!已释放 && 当前血量 > 0 && 有限(数量) && 数量 > 0)
            设置当前资源(当前血量, 当前灵力 + 数量, 当前灵气护盾);
    }
    float 加成(道纹属性 属性) => 道纹 != null ? (float)道纹.生效加成[(int)属性] : 0;
    public void 重算()
    {
        if (已释放) return;
        // 从配置及本次有效道纹结果重新构造，不能在上次结果上继续累加。
        if (!配置.使用测试数据)
        {
            var 值 = 天帝数值.计算主角(等级, 道纹?.生效加成, 天赋);
            力量 = (float)值.力量; 速度 = (float)值.速度; 智力 = (float)值.智力;
            血量 = (float)值.血量; 灵力 = (float)值.灵力; 防御 = (float)值.防御; 灵气护盾 = (float)值.护盾;
            移动速度 = (float)值.移速; 跑步速度 = (float)值.跑速; 攻击力 = (float)值.攻击;
            攻击速度 = (float)值.攻速; 暴击率 = (float)值.暴击率; 暴击倍率 = (float)值.暴击倍率;
            抗性 = (float)值.抗性; 技能急速 = (float)值.急速; 闪避率 = (float)值.闪避率;
            float 常驻移速=特性移速,常驻急速=特性急速;
            if(道纹!=null)foreach(var t in 道纹.特性视图.生效)
            {
                if(t.道纹.特性编号==13)常驻移速+=(float)天帝特性道纹.品阶值(t.道纹,"疾行")/100;
                if(t.道纹.特性编号==15)常驻急速+=(float)天帝特性道纹.取("rules.haste")*(1+((float)天帝特性道纹.品阶值(t.道纹,"效果")-1)*.5f);
            }
            if(常驻移速>0){float f=1+(float)天帝数值.软上限(常驻移速,天帝特性道纹.取("limits.move_knee"));移动速度=Mathf.Min((float)天帝特性道纹.取("limits.walk_max"),移动速度*f);跑步速度=Mathf.Min((float)天帝特性道纹.取("limits.run_max"),跑步速度*f);}
            技能急速=Mathf.Min((float)天帝数值.取("player.haste_max"),技能急速+常驻急速);
            攻击速度=Mathf.Min((float)天帝数值.取("player.aps_max"),攻击速度*(1+特性攻速));
            闪避率=Mathf.Min((float)天帝数值.取("player.evasion_max"),闪避率+特性闪避);
            截断资源(); 属性改变?.Invoke(); return;
        }
        力量 = 主角衍生属性配置.截断((double)配置.力量 + 天帝天赋效果.力量加成(天赋, 加成(道纹属性.力量)));
        速度 = 主角衍生属性配置.截断((double)配置.速度 + 加成(道纹属性.速度));
        智力 = 主角衍生属性配置.截断((double)配置.智力 + 加成(道纹属性.智力));
        血量 = 配置.血量.计算(力量, 速度, 智力, 加成(道纹属性.血量));
        灵力 = 配置.灵力.计算(力量, 速度, 智力, 加成(道纹属性.灵力));
        防御 = 配置.防御.计算(力量, 速度, 智力, 加成(道纹属性.防御));
        灵气护盾 = 配置.灵气护盾.计算(力量, 速度, 智力, 加成(道纹属性.护盾));
        移动速度 = 配置.移动速度.计算(力量, 速度, 智力);
        跑步速度 = 配置.跑步速度.计算(力量, 速度, 智力);
        攻击力 = 配置.攻击仅计算增益 ? 配置.攻击力.计算(Mathf.Max(0, 力量 - 配置.力量), Mathf.Max(0, 速度 - 配置.速度), Mathf.Max(0, 智力 - 配置.智力)) : 配置.攻击力.计算(力量, 速度, 智力);
        灵力 = 天帝天赋效果.灵力上限(天赋, 灵力);
        防御 = 天帝天赋效果.防御(天赋, 防御);
        灵气护盾 = 天帝天赋效果.护盾上限(天赋, 灵气护盾, 智力);
        float 移速倍率 = 1 + 加成(道纹属性.移速) / 100f;
        移动速度 = 主角衍生属性配置.截断((double)天帝天赋效果.移动速度(天赋, 移动速度) * 移速倍率);
        跑步速度 = 主角衍生属性配置.截断((double)天帝天赋效果.移动速度(天赋, 跑步速度) * 移速倍率);
        // 上限增加不自动回复；上限降低只截断，防止插拔道纹免费恢复资源。
        攻击速度 = (float)天帝数值.取("player.aps_base"); 暴击率 = (float)天帝数值.取("player.crit_base"); 暴击倍率 = (float)天帝数值.取("player.crit_damage_base");
        截断资源();
        属性改变?.Invoke();
    }
    void 截断资源()
    { 当前血量 = Mathf.Min(当前血量, 血量); 当前灵力 = Mathf.Min(当前灵力, 灵力); 当前灵气护盾 = Mathf.Min(当前灵气护盾, 灵气护盾); }
    public void 阶段恢复(double 比例)
    {
        if (已释放 || 当前血量 <= 0 || 比例 <= 0) return;
        比例 = 天帝数值.夹(比例, 0, 1);
        设置当前资源(当前血量 + (float)(血量 * 比例), 当前灵力, 当前灵气护盾 + (float)(灵气护盾 * 比例));
    }
    public void Dispose()
    {
        if (已释放) return;
        已释放 = true;
        if (道纹 != null) 道纹.状态改变 -= 重算;
        属性改变 = null;
    }
}
