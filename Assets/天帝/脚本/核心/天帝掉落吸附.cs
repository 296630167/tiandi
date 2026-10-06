using UnityEngine;

// 奖励先入库，飞行动画只负责反馈；穿墙、不寻路、不影响结算。
public sealed class 天帝掉落吸附
{
    readonly Vector2 起点;
    float 累计秒;
    public Vector2 位置 { get; private set; }
    public bool 完成 => 累计秒 >= .65f;
    public float 缩放 => 1 - Mathf.Clamp01((累计秒 - .1f) / .55f) * .65f;
    public 天帝掉落吸附(Vector2 起点) { this.起点 = 起点; 位置 = 起点; }
    public void 推进(Vector2 玩家, float 秒)
    {
        if (完成 || 秒 <= 0 || float.IsNaN(秒) || float.IsInfinity(秒) ||
            float.IsNaN(玩家.x) || float.IsNaN(玩家.y) || float.IsInfinity(玩家.x) || float.IsInfinity(玩家.y)) return;
        累计秒 = Mathf.Min(.65f, 累计秒 + Mathf.Min(秒, .25f));
        float t = Mathf.Clamp01((累计秒 - .1f) / .55f);
        位置 = Vector2.Lerp(起点, 玩家, t * t) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * .5f);
        if (完成) 位置 = 玩家;
    }
}
