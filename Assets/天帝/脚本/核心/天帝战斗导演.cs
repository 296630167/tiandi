using System;
using System.Collections.Generic;
using UnityEngine;

public enum 战斗危险类型 { 火焰裂隙, 冰霜潮汐, 雷击标记, 毒雾扩散, 能量风暴, 范围收缩 }

// 战斗内唯一的主要危险事件。预警阶段只展示边界，生效后才造成伤害。
public sealed class 战斗危险事件
{
    public 战斗危险类型 类型;
    public Vector2 中心;
    public float 半径;
    public float 总秒;
    public float 预警秒;
    public float 已过秒;
    public int 种子;

    public float 剩余秒 => Mathf.Max(0, 总秒 - 已过秒);
    public float 预警进度 => Mathf.Clamp01(已过秒 / Mathf.Max(.01f, 预警秒));
    public bool 已生效 => 已过秒 >= 预警秒;
    public bool 已结束 => 已过秒 >= 总秒;
    public float 有效半径
    {
        get
        {
            float t = Mathf.Clamp01((已过秒 - 预警秒) / Mathf.Max(.01f, 总秒 - 预警秒));
            if (类型 == 战斗危险类型.毒雾扩散) return 半径 * (.78f + t * .22f);
            if (类型 == 战斗危险类型.范围收缩) return 半径 * (1f - t * .28f);
            return 半径;
        }
    }
    public bool 包含(Vector2 点)
    {
        float d = Vector2.Distance(点, 中心);
        return 类型 == 战斗危险类型.范围收缩 ? d >= 有效半径 : d <= 有效半径;
    }
}

// 负责地图等级驱动的内容节奏和可读的关键演出，不接管敌人寻路或伤害公式。
public sealed class 天帝战斗导演
{
    readonly 天帝战斗系统 宿主;
    readonly 天帝战斗地图 地图;
    readonly int 地图等级;
    readonly System.Random 随机;
    readonly Func<float, bool> 造成伤害;
    float 时钟;
    float 下次事件秒;
    float 危险伤害冷却;
    int 上次释放数;
    int 上次完整轮次;
    int 上次BOSS形态 = -1;
    int 上次登场数;
    float 链路剩余秒;
    float 共鸣剩余秒;
    int 共鸣通路 = -1;
    int 当前链路槽 = -1;
    float 结束反馈剩余;
    Vector2 最后事件中心;
    float 最后事件半径;
    战斗危险类型 最后事件类型;
    float BOSS阶段提示剩余;
    int 事件序号;
    public 战斗危险事件 当前事件 { get; private set; }
    public int 事件次数 { get; private set; }
    public int 事件结束次数 { get; private set; }
    public int 事件同时峰值 { get; private set; }
    public int BOSS阶段变化次数 { get; private set; }
    public float 环境强度 { get; private set; }
    public float 镜头拉近剩余秒 { get; private set; }
    public float 宽镜头剩余秒 { get; private set; }
    public float 镜头拉近倍率 => 镜头拉近剩余秒 > 0 ? .88f : 宽镜头剩余秒 > 0 ? 1.13f : 1f;
    public float 链路演出进度 => Mathf.Clamp01(1f - 链路剩余秒 / .72f);
    public float 完整共鸣剩余秒 => Mathf.Max(0, 共鸣剩余秒);
    public float 事件结束反馈剩余秒 => Mathf.Max(0, 结束反馈剩余);
    public float 事件结束反馈进度 => Mathf.Clamp01(结束反馈剩余 / .46f);
    public Vector2 最后事件位置 => 最后事件中心;
    public float 最后事件范围 => 最后事件半径;
    public 战斗危险类型 最后事件 => 最后事件类型;
    public float BOSS阶段提示剩余秒 => Mathf.Max(0, BOSS阶段提示剩余);
    public int 当前链路槽位 => 链路剩余秒 > .0001f ? 当前链路槽 : -1;
    public int 内容档位 => 地图等级 < 20 ? 1 : 地图等级 < 60 ? 2 : 3;
    public string 内容标签 => 内容档位 == 1 ? "低阶 · 认知节奏" : 内容档位 == 2 ? "中阶 · 复合事件" : "高阶 · 多向协同";
    public Color 环境颜色
    {
        get
        {
            if (当前事件 == null) return 内容档位 == 1 ? new Color(.18f, .34f, .28f) : 内容档位 == 2 ? new Color(.24f, .28f, .36f) : new Color(.34f, .20f, .30f);
            switch (当前事件.类型)
            {
                case 战斗危险类型.火焰裂隙: return new Color(.55f, .18f, .08f);
                case 战斗危险类型.冰霜潮汐: return new Color(.12f, .38f, .55f);
                case 战斗危险类型.雷击标记: return new Color(.36f, .28f, .58f);
                case 战斗危险类型.毒雾扩散: return new Color(.20f, .46f, .22f);
                case 战斗危险类型.能量风暴: return new Color(.48f, .22f, .54f);
                default: return new Color(.55f, .30f, .10f);
            }
        }
    }

    public 天帝战斗导演(天帝战斗系统 宿主, 天帝战斗地图 地图, int 地图等级, Func<float, bool> 造成伤害)
    {
        this.宿主 = 宿主; this.地图 = 地图; this.地图等级 = Mathf.Clamp(地图等级, 1, 100); this.造成伤害 = 造成伤害;
        随机 = new System.Random(unchecked(地图.种子 ^ 0x4D4150 ^ 地图等级 * 7919));
        下次事件秒 = 内容档位 == 1 ? 18f : 内容档位 == 2 ? 12f : 8f;
    }

    public bool 危险包含(Vector2 点) => 当前事件 != null && 当前事件.已生效 && 当前事件.包含(点);
    public Vector2 危险逃离方向(Vector2 点)
    {
        if (当前事件 == null) return Vector2.zero;
        Vector2 差 = 点 - 当前事件.中心;
        if (当前事件.类型 == 战斗危险类型.范围收缩) 差 = 当前事件.中心 - 点;
        return 差.sqrMagnitude > .0001f ? 差.normalized : Vector2.right;
    }

    public void 推进(float 秒)
    {
        if (秒 <= 0 || float.IsNaN(秒) || float.IsInfinity(秒) || 宿主.玩家死亡) return;
        秒 = Mathf.Min(.25f, 秒); 时钟 += 秒;
        镜头拉近剩余秒 = Mathf.Max(0, 镜头拉近剩余秒 - 秒);
        宽镜头剩余秒 = Mathf.Max(0, 宽镜头剩余秒 - 秒);
        结束反馈剩余 = Mathf.Max(0, 结束反馈剩余 - 秒);
        BOSS阶段提示剩余 = Mathf.Max(0, BOSS阶段提示剩余 - 秒);
        if (链路剩余秒 > 0)
        {
            链路剩余秒 = Mathf.Max(0, 链路剩余秒 - 秒);
            if (链路剩余秒 <= .0001f) 当前链路槽 = -1;
        }
        if (共鸣剩余秒 > 0) 共鸣剩余秒 = Mathf.Max(0, 共鸣剩余秒 - 秒);
        if (危险伤害冷却 > 0) 危险伤害冷却 = Mathf.Max(0, 危险伤害冷却 - 秒);

        读取释放与BOSS阶段();
        if (当前事件 != null)
        {
            当前事件.已过秒 += 秒;
            if (当前事件.类型 == 战斗危险类型.能量风暴)
            {
                float a = 时钟 * .55f + 当前事件.种子;
                Vector2 新中心 = 当前事件.中心 + new Vector2(Mathf.Cos(a), Mathf.Sin(a * .83f)) * 秒 * .22f;
                // 移动事件只在可站立区域内更新，不能因为持续漂移越过地图边界或障碍。
                if (地图.可站立(新中心, .65f)) 当前事件.中心 = 新中心;
            }
            if (当前事件.已生效 && 危险包含(宿主.当前玩家位置) && 危险伤害冷却 <= 0)
            {
                float 比例 = 内容档位 == 1 ? .018f : 内容档位 == 2 ? .028f : .036f;
                造成伤害?.Invoke(Mathf.Max(1f, 宿主.主角生命上限 * 比例));
                危险伤害冷却 = .55f;
            }
            if (当前事件.已结束)
            {
                最后事件中心 = 当前事件.中心;
                最后事件半径 = 当前事件.有效半径;
                最后事件类型 = 当前事件.类型;
                结束当前事件();
                下次事件秒 = 时钟 + (内容档位 == 1 ? 18f : 内容档位 == 2 ? 11f : 7.5f);
            }
        }
        else if (时钟 >= 下次事件秒 && 宿主.场上敌人数量 > 0 && !宿主.玩家死亡)
            开始事件();
        float 事件强度 = 当前事件 == null ? 0 : Mathf.Lerp(.25f, 1f, 当前事件.预警进度);
        环境强度 = Mathf.Clamp01((地图等级 - 1) / 99f * .55f + 事件强度 * .45f + (宿主.BOSS已出现 ? .12f : 0));
    }

    void 读取释放与BOSS阶段()
    {
        if (宿主.普通释放次数 != 上次释放数)
        {
            上次释放数 = 宿主.普通释放次数; 当前链路槽 = (6 - 宿主.当前通路) % 6; 链路剩余秒 = .72f;
            if (链路剩余秒 > .45f) 共鸣通路 = 宿主.当前通路;
        }
        if (宿主.自动展示轮次完成次数 != 上次完整轮次)
        {
            上次完整轮次 = 宿主.自动展示轮次完成次数;
            共鸣通路 = 宿主.当前通路;
            共鸣剩余秒 = .3f;
        }
        var 王 = 宿主.敌人 == null ? null : 查找BOSS();
        if (王 == null) return;
        if (上次BOSS形态 < 0) { 上次BOSS形态 = 王.形态; return; }
        if (王.形态 != 上次BOSS形态)
        {
            上次BOSS形态 = 王.形态; BOSS阶段变化次数++; BOSS阶段提示剩余 = 1.1f;
            镜头拉近剩余秒 = .72f; 宽镜头剩余秒 = .48f; 共鸣剩余秒 = .3f;
            结束当前事件();
            下次事件秒 = 时钟 + (内容档位 == 3 ? 2.2f : 3.5f);
            宿主.请求命中停顿(.045f);
        }
    }
    战斗敌人 查找BOSS()
    {
        foreach (var e in 宿主.敌人) if (e.布点.级别 == 战斗敌人级别.王级 && e.已生成) return e;
        return null;
    }
    void 开始事件()
    {
        int 序 = ++事件序号;
        // 明确的事件池保证每个等级档位都展示该档位的核心机制：
        // 低阶先认识火焰，中阶覆盖火/冰/雷/毒，高阶再加入风暴与范围收缩。
        战斗危险类型[] 事件池 = 内容档位 == 1
            ? new[] { 战斗危险类型.火焰裂隙 }
            : 内容档位 == 2
                ? new[] { 战斗危险类型.火焰裂隙, 战斗危险类型.冰霜潮汐, 战斗危险类型.雷击标记, 战斗危险类型.毒雾扩散 }
                : new[] { 战斗危险类型.火焰裂隙, 战斗危险类型.冰霜潮汐, 战斗危险类型.雷击标记, 战斗危险类型.毒雾扩散, 战斗危险类型.能量风暴, 战斗危险类型.范围收缩 };
        战斗危险类型 类型 = 事件池[(序 - 1) % 事件池.Length];
        float 预警 = 内容档位 == 1 ? 1.75f : 内容档位 == 2 ? 1.35f : 1.05f;
        float 半径 = 内容档位 == 1 ? 2.1f : 内容档位 == 2 ? 2.8f : 3.5f;
        Vector2 中心;
        if (类型 == 战斗危险类型.范围收缩)
        {
            中心 = Vector2.zero; 半径 = 内容档位 == 3 ? 15f : 17f;
        }
        else
        {
            Vector2? 选点 = 选事件中心(类型, 半径);
            if (!选点.HasValue)
            {
                // 找不到与玩家保持安全距离的可站立点时延后事件，禁止落到玩家脚下。
                下次事件秒 = 时钟 + .9f;
                return;
            }
            中心 = 选点.Value;
        }
        当前事件 = new 战斗危险事件 { 类型 = 类型, 中心 = 中心, 半径 = 半径, 预警秒 = 预警, 总秒 = 预警 + (内容档位 == 1 ? 3.2f : 内容档位 == 2 ? 4.1f : 5f), 种子 = 序 };
        事件次数++;
        事件同时峰值 = Mathf.Max(事件同时峰值, 当前事件 == null ? 0 : 1);
    }
    Vector2? 选事件中心(战斗危险类型 类型, float 半径)
    {
        Vector2 玩家 = 宿主.当前玩家位置;
        for (int i = 0; i < 8; i++)
        {
            float a = (随机.Next(360) + i * 45) * Mathf.Deg2Rad;
            float 距 = Mathf.Max(4.5f, 半径 + 1.25f) + (float)随机.NextDouble() * 2.5f;
            Vector2 p = 玩家 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 距;
            if (地图.可站立(p, .65f) && Vector2.Distance(p, 玩家) > 半径 + 1.2f) return p;
        }
        // 径向采样失败时遍历地图内的稀疏网格，选离玩家最远的安全点。
        Vector2 最佳 = default; float 最远 = -1f;
        for (int y = 1; y < 12; y++) for (int x = 1; x < 16; x++)
        {
            Vector2 p = new Vector2(Mathf.Lerp(-地图.半宽 + 1.2f, 地图.半宽 - 1.2f, x / 16f), Mathf.Lerp(-地图.半高 + 1.2f, 地图.半高 - 1.2f, y / 12f));
            float d = Vector2.Distance(p, 玩家);
            if (d > 半径 + 1.2f && d > 最远 && 地图.可站立(p, .65f)) { 最远 = d; 最佳 = p; }
        }
        return 最远 >= 0 ? 最佳 : (Vector2?)null;
    }

    void 结束当前事件()
    {
        if (当前事件 == null) return;
        最后事件中心 = 当前事件.中心;
        最后事件半径 = 当前事件.有效半径;
        最后事件类型 = 当前事件.类型;
        当前事件 = null;
        事件结束次数++;
        结束反馈剩余 = .46f;
    }
}

public sealed partial class 天帝战斗系统
{
    public 天帝战斗导演 导演 { get; private set; }
    public Vector2 当前玩家位置 => 玩家;
    public 战斗危险事件 当前危险事件 => 导演?.当前事件;
    public float 环境强度 => 导演?.环境强度 ?? 0;
    public int 战斗事件次数 => 导演?.事件次数 ?? 0;
    public int BOSS阶段变化次数 => 导演?.BOSS阶段变化次数 ?? 0;
    public int 当前链路演出槽 => 导演?.当前链路槽位 ?? -1;
    public float 链路演出进度 => 导演?.链路演出进度 ?? 0;
    public float 完整链路共鸣剩余秒 => 导演?.完整共鸣剩余秒 ?? 0;
    public float 事件结束反馈剩余秒 => 导演?.事件结束反馈剩余秒 ?? 0;
    public int 主角生命上限 => Mathf.Max(1, Mathf.RoundToInt((float)主角.血量));
    public string 地图内容标签 => 导演?.内容标签 ?? "低阶 · 认知节奏";
}
