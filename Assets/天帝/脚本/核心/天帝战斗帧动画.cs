using System;
using UnityEngine;

public enum 战斗帧动作 { 待机, 移动, 奔跑, 攻击 }

[CreateAssetMenu(menuName = "天帝/战斗角色帧动画")]
public sealed class 天帝战斗帧动画 : ScriptableObject
{
    [Serializable]
    public sealed class 方向动作
    {
        public 角色朝向 朝向;
        public 战斗帧动作 动作;
        public Sprite[] 帧 = Array.Empty<Sprite>();
        public float[] 每帧秒 = Array.Empty<float>();
        public bool 循环 = true;
        [Tooltip("攻击的真实命中/发射相位；前摇不会提前播放这一帧。")]
        public int 释放帧;

        public float 总秒
        {
            get { float 秒 = 0; if (每帧秒 != null) foreach (float 项 in 每帧秒) 秒 += 项; return 秒; }
        }
        public float 释放秒
        {
            get { float 秒 = 0; for (int i = 0; i < 释放帧 && i < 每帧秒.Length; i++) 秒 += 每帧秒[i]; return 秒; }
        }
        public int 帧号(float 秒)
        {
            if (帧 == null || 帧.Length == 0) return -1;
            float 时间 = 循环 ? Mathf.Repeat(Mathf.Max(0, 秒), 总秒) : Mathf.Max(0, 秒);
            for (int i = 0; i < 每帧秒.Length; i++)
            { if (时间 < 每帧秒[i]) return i; 时间 -= 每帧秒[i]; }
            return 帧.Length - 1;
        }
        public bool 有效(Vector2Int 画布, Vector2 根点, float 每米像素)
        {
            if (帧 == null || 每帧秒 == null || 帧.Length == 0 || 每帧秒.Length != 帧.Length || !天帝战斗帧动画.正数(总秒)) return false;
            if (动作 == 战斗帧动作.攻击 && (循环 || 释放帧 < 1 || 释放帧 >= 帧.Length)) return false;
            if ((int)朝向 < 0 || (int)朝向 >= 8 || (int)动作 < 0 || (int)动作 > 3) return false;
            for (int i = 0; i < 帧.Length; i++)
            {
                if (!天帝战斗帧动画.正数(每帧秒[i]) || 帧[i] == null || 帧[i].rect.size != (Vector2)画布 ||
                    Vector2.Distance(帧[i].pivot, 根点) > .01f || Mathf.Abs(帧[i].pixelsPerUnit - 每米像素) > .01f) return false;
            }
            return true;
        }
    }

    public string 编号;
    public bool 已验收;
    public string 验收清单指纹;
    public string 来源清单指纹;
    public Vector2Int 共同画布 = new Vector2Int(256, 256);
    [Tooltip("左下原点的像素坐标；全部帧共用脚下根点，不按单帧包围盒缩放。")]
    public Vector2 共同根点 = new Vector2(128, 20);
    [Min(.01f)] public float 每米像素 = 100;
    [Min(.01f)] public float 展示高度 = 3.5f;
    [Min(.01f)] public float 参考移速 = 6;
    public 方向动作[] 动作 = Array.Empty<方向动作>();
    public float 固定缩放 => 展示高度 * 每米像素 / 共同画布.y;
    public 方向动作 获取(角色朝向 朝向, 战斗帧动作 动作名)
    {
        if (动作 != null) foreach (var 项 in 动作) if (项 != null && 项.朝向 == 朝向 && 项.动作 == 动作名) return 项;
        return null;
    }
    public bool 可用
    {
        get
        {
            if (!已验收 || string.IsNullOrEmpty(编号) || 共同画布.x <= 0 || 共同画布.y <= 0 ||
                !正数(每米像素) || !正数(展示高度) || !正数(参考移速) || !正数(固定缩放) ||
                float.IsNaN(共同根点.x) || float.IsNaN(共同根点.y) || 共同根点.x < 0 || 共同根点.y < 0 ||
                共同根点.x > 共同画布.x || 共同根点.y > 共同画布.y || 动作 == null || 动作.Length == 0) return false;
            int 掩码 = 0;
            foreach (var 项 in 动作)
            {
                if (项 == null || !项.有效(共同画布, 共同根点, 每米像素)) return false;
                int 位 = 1 << ((int)项.朝向 * 4 + (int)项.动作);
                if ((掩码 & 位) != 0) return false;
                掩码 |= 位;
            }
            return true;
        }
    }
    internal static bool 正数(float 值) => 值 > 0 && !float.IsNaN(值) && !float.IsInfinity(值);
}

// 只推进表现时钟；战斗位移、伤害和攻击事件仍由现有战斗系统决定。
public sealed class 天帝战斗帧播放
{
    readonly 天帝战斗帧动画 配置;
    天帝战斗帧动画.方向动作 当前动作;
    float 动作秒;
    public 角色朝向 朝向 { get; private set; } = 角色朝向.南;
    public Sprite 当前精灵 { get; private set; }
    public bool 使用新帧 { get; private set; }

    public 天帝战斗帧播放(天帝战斗帧动画 配置) { this.配置 = 配置 != null && 配置.可用 ? 配置 : null; }

    public void 回退() { 使用新帧 = false; 当前动作 = null; 动作秒 = 0; }

    bool 选动作(战斗帧动作 动作, Vector2 方向, bool 保持周期)
    {
        if (配置 == null) return 使用新帧 = false;
        if (方向.sqrMagnitude > .00000001f)
        {
            float 角 = Mathf.Atan2(方向.x, -方向.y) * Mathf.Rad2Deg;
            if (动作 == 战斗帧动作.攻击 || Mathf.Abs(Mathf.DeltaAngle((int)朝向 * 45, 角)) > 26.5f)
                朝向 = 天帝八方向播放.方向转朝向(方向);
        }
        var 新动作 = 配置.获取(朝向, 动作);
        if (新动作 == null) { 当前动作 = null; 动作秒 = 0; return 使用新帧 = false; }
        if (新动作 != 当前动作)
        {
            float 相位 = 保持周期 && 当前动作 != null && 当前动作.循环 && 新动作.循环
                ? Mathf.Repeat(动作秒, 当前动作.总秒) / 当前动作.总秒 : 0;
            动作秒 = 新动作.总秒 * 相位;
            当前动作 = 新动作;
        }
        return 使用新帧 = true;
    }

    public bool 尝试推进(战斗帧动作 动作, Vector2 方向, float 秒, out Sprite 图, float 速度倍率 = 1)
    {
        图 = null;
        if (!有效输入(方向, 秒) || !天帝战斗帧动画.正数(速度倍率)) { 回退(); return false; }
        float 步进秒 = 秒 * 速度倍率;
        if (float.IsNaN(步进秒) || float.IsInfinity(步进秒)) { 回退(); return false; }
        bool 保持周期 = 当前动作 != null && 当前动作.动作 == 动作 && 动作 != 战斗帧动作.攻击;
        if (!选动作(动作, 方向, 保持周期)) return false;
        double 时间 = (double)动作秒 + 步进秒;
        动作秒 = 当前动作.循环 ? (float)(时间 % 当前动作.总秒) : (float)Math.Min(时间, 当前动作.总秒);
        图 = 当前精灵 = 当前动作.帧[当前动作.帧号(动作秒)];
        return true;
    }

    public bool 尝试攻击(Vector2 方向, float 阶段进度, bool 已释放, out Sprite 图)
    {
        图 = null;
        if (!有效输入(方向, 阶段进度)) { 回退(); return false; }
        if (!选动作(战斗帧动作.攻击, 方向, false)) return false;
        float 进度 = Mathf.Clamp01(阶段进度);
        动作秒 = 已释放 ? Mathf.Lerp(当前动作.释放秒, 当前动作.总秒, 进度)
            : 当前动作.释放秒 * 进度;
        int 帧 = 当前动作.帧号(动作秒);
        帧 = 已释放 ? Mathf.Max(当前动作.释放帧, 帧) : Mathf.Min(当前动作.释放帧 - 1, 帧);
        图 = 当前精灵 = 当前动作.帧[帧];
        return true;
    }

    static bool 有效输入(Vector2 方向, float 秒) => 秒 >= 0 && !float.IsNaN(秒) && !float.IsInfinity(秒) &&
        !float.IsNaN(方向.x) && !float.IsNaN(方向.y) && !float.IsInfinity(方向.x) && !float.IsInfinity(方向.y);
}
