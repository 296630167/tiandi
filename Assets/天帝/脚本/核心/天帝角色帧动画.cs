using System;
using UnityEngine;

// 世界Vector2.y向上对应屏幕北；所有动作共用画布尺寸与脚下根节点。
public enum 角色朝向 { 南, 东南, 东, 东北, 北, 西北, 西, 西南 }

[CreateAssetMenu(menuName = "天帝/角色八方向帧动画")]
public sealed class 天帝角色帧动画 : ScriptableObject
{
    [Serializable] public sealed class 方向动作
    {
        public 角色朝向 朝向;
        public Sprite 待机;
        public Sprite[] 移动帧 = Array.Empty<Sprite>();
        [Tooltip("0沿用统一帧率；不同方向样例可以按各自完整步行周期设置。")]
        [Min(0)] public float 移动帧率;
    }
    [Min(.1f)] public float 展示高度 = 3.5f;
    [Min(1)] public float 走路帧率 = 10;
    [Min(.1f)] public float 参考移动速度 = 6;
    public Sprite[] 正面待机帧 = Array.Empty<Sprite>();
    public Sprite[] 正面射击帧 = Array.Empty<Sprite>();
    [Min(1)] public float 待机帧率 = 8;
    [Min(0)] public int 射击释放帧 = 7;
    [Min(.01f)] public float 射击回收秒 = .28f;
    public 方向动作[] 动作 = Array.Empty<方向动作>();
    public 方向动作 获取(角色朝向 朝向)
    {
        foreach (var 项 in 动作) if (项 != null && 项.朝向 == 朝向) return 项;
        return null;
    }
    public bool 完整
    {
        get
        {
            for (int i = 0; i < 8; i++)
            {
                var 项 = 获取((角色朝向)i);
                if (项 == null || 项.待机 == null || 项.移动帧 == null || 项.移动帧.Length < 2) return false;
                foreach (var 帧 in 项.移动帧) if (帧 == null) return false;
            }
            return true;
        }
    }
    public bool 十组完整
    {
        get
        {
            if (!完整 || 获取(角色朝向.南).移动帧.Length != 16 || 正面待机帧.Length != 16 || 正面射击帧.Length != 16 || 射击释放帧 < 1 || 射击释放帧 >= 正面射击帧.Length) return false;
            foreach (var 帧 in 正面待机帧) if (帧 == null) return false;
            foreach (var 帧 in 正面射击帧) if (帧 == null) return false;
            return true;
        }
    }
    public Sprite 正面待机(float 秒)
        => 正面待机帧 != null && 正面待机帧.Length > 0
            ? 正面待机帧[(int)(Mathf.Max(0, 秒) * 待机帧率) % 正面待机帧.Length] : 获取(角色朝向.南)?.待机;
}

// 只选择精灵，不驱动位移/攻击。按真实位移推进帧，墙边与待机不会原地踩步。
public sealed class 天帝八方向播放
{
    readonly 天帝角色帧动画 配置;
    float 当前帧;
    float 待机秒, 射击秒, 前摇秒;
    bool 正面射击中, 已释放;
    public 角色朝向 朝向 { get; private set; } = 角色朝向.南;
    public bool 移动中 { get; private set; }
    public Sprite 当前精灵 { get; private set; }
    public 天帝八方向播放(天帝角色帧动画 配置)
    {
        this.配置 = 配置;
        当前精灵 = 配置 != null ? 配置.获取(朝向)?.待机 : null;
    }
    public static 角色朝向 方向转朝向(Vector2 方向)
    {
        float 角 = Mathf.Atan2(方向.x, -方向.y) * Mathf.Rad2Deg;
        return (角色朝向)((Mathf.RoundToInt(角 / 45) + 8) % 8);
    }
    // 当前只绘制正面射击；其他方向保留对应朝向，不强行转成正面。
    public void 准备射击(Vector2 目标方向, float 前摇)
    {
        正面射击中 = 配置 != null && 配置.十组完整 && !移动中 && 目标方向.sqrMagnitude > .0001f && 方向转朝向(目标方向) == 角色朝向.南;
        if (!正面射击中) return;
        朝向 = 角色朝向.南; 射击秒 = 0; 前摇秒 = Mathf.Max(.001f, 前摇); 已释放 = false;
    }
    public void 释放射击()
    {
        if (!正面射击中) return;
        已释放 = true; 射击秒 = 0;
    }
    public Sprite 推进(Vector2 实际位移, float 秒, bool 存活)
    {
        if (配置 == null || float.IsNaN(秒) || float.IsInfinity(秒) || 秒 < 0 ||
            float.IsNaN(实际位移.x) || float.IsNaN(实际位移.y) || float.IsInfinity(实际位移.x) || float.IsInfinity(实际位移.y)) return 当前精灵;
        if (!存活) { 移动中 = 正面射击中 = false; return 当前精灵; }
        移动中 = 秒 > 0 && 实际位移.sqrMagnitude > .00000001f;
        var 原动作 = 配置.获取(朝向);
        if (移动中)
        {
            // 分界线附近保留4度余量，移动摇杆微小抖动时不会来回翻转朝向。
            float 角 = Mathf.Atan2(实际位移.x, -实际位移.y) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle((int)朝向 * 45, 角)) > 26.5f) 朝向 = 方向转朝向(实际位移);
        }
        var 项 = 配置.获取(朝向); if (项 == null) return 当前精灵;
        // 样例方向的帧数可以不同；转向保留步行相位，而不是沿用原方向的整数帧号。
        if (项 != 原动作 && 原动作?.移动帧 != null && 原动作.移动帧.Length > 0 && 项.移动帧 != null)
            当前帧 = 当前帧 / 原动作.移动帧.Length * 项.移动帧.Length;
        if (!移动中)
        {
            当前帧 = 0; 待机秒 += Mathf.Min(秒, .25f);
            if (正面射击中)
            {
                射击秒 += Mathf.Min(秒, .25f);
                int 释放帧 = 配置.射击释放帧;
                int 帧 = 已释放 ? 释放帧 + Mathf.FloorToInt(射击秒 / 配置.射击回收秒 * (配置.正面射击帧.Length - 释放帧))
                    : Mathf.FloorToInt(Mathf.Clamp01(射击秒 / 前摇秒) * 释放帧);
                // 未收到真实释放事件时停在释放前一帧；目标失效会超时归位。
                if (!已释放 && 射击秒 <= 前摇秒 + .15f) return 当前精灵 = 配置.正面射击帧[Mathf.Min(帧, 释放帧 - 1)];
                if (已释放 && 帧 < 配置.正面射击帧.Length) return 当前精灵 = 配置.正面射击帧[帧];
                正面射击中 = false; 待机秒 = 0;
            }
            return 当前精灵 = 朝向 == 角色朝向.南 ? 配置.正面待机(待机秒) : 项.待机;
        }
        待机秒 = 0; 正面射击中 = false;
        if (项.移动帧 == null || 项.移动帧.Length == 0) return 当前精灵 = 项.待机;
        // 位移驱动帧率：跑步自然加速；最大倍率限制避免低帧率瞬移使动画疯转。
        float 速度倍率 = Mathf.Clamp(实际位移.magnitude / Mathf.Max(.0001f, 秒 * 配置.参考移动速度), .35f, 1.8f);
        float 帧率 = 项.移动帧率 > 0 ? 项.移动帧率 : 配置.走路帧率;
        当前帧 = Mathf.Repeat(当前帧 + Mathf.Min(秒, .25f) * 帧率 * 速度倍率, 项.移动帧.Length);
        return 当前精灵 = 项.移动帧[Mathf.FloorToInt(当前帧)];
    }
}
