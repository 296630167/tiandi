using System;
using System.Collections.Generic;
using UnityEngine;

// 刷新只负责选点；正式敌人强度、波次与AI仍由原系统驱动。
public sealed class 天帝敌人刷新
{
    readonly 天帝战斗地图 地图;
    readonly Func<Rect> 读取视野;
    readonly IReadOnlyList<战斗敌人> 敌人;
    readonly List<Vector2> 候选 = new List<Vector2>();
    readonly List<Vector2>[] 边缘 = { new List<Vector2>(), new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };
    readonly float 屏外余量, 间距平方;
    readonly Vector2 北端位置;
    readonly bool 有北端位置;
    int 序号;

    public 天帝敌人刷新(天帝战斗地图 地图, 天帝战斗寻路 寻路, IReadOnlyList<战斗敌人> 敌人, Func<Rect> 读取视野 = null)
    {
        this.地图 = 地图; this.敌人 = 敌人; this.读取视野 = 读取视野;
        屏外余量 = (float)天帝数值.取("map.spawn_offscreen_margin");
        float 间距 = (float)天帝数值.取("map.spawn_clearance"); 间距平方 = 间距 * 间距;
        寻路.取得连通节点(地图.出生位置, 候选);
        float 普通半径 = (float)天帝数值.取("map.spawn_radius");
        float 王半径 = (float)天帝数值.取("map.boss_spawn_radius");
        float 最北 = float.NegativeInfinity, 中距 = float.MaxValue;
        for (int i = 候选.Count - 1; i >= 0; i--)
        {
            var 点 = 候选[i];
            if (地图.可站立(点, 王半径) && (点.y > 最北 || 点.y == 最北 && Mathf.Abs(点.x) < 中距))
            { 北端位置 = 点; 有北端位置 = true; 最北 = 点.y; 中距 = Mathf.Abs(点.x); }
            if (!地图.可站立(点, 普通半径)) 候选.RemoveAt(i);
            else if (地图.生存大图)
            {
                float 垫 = (float)天帝数值.取("map.arena.edge_padding"), 深 = (float)天帝数值.取("map.arena.edge_depth");
                float 横距 = 地图.半宽 - Mathf.Abs(点.x), 纵距 = 地图.半高 - Mathf.Abs(点.y);
                if (横距 >= 垫 && 纵距 >= 垫 && Mathf.Min(横距, 纵距) <= 深)
                    边缘[横距 < 纵距 ? (点.x < 0 ? 0 : 1) : (点.y < 0 ? 2 : 3)].Add(点);
            }
        }
    }
    bool 拥挤(Vector2 点)
    {
        foreach (var 敌 in 敌人) if (敌.存活 && (敌.位置 - 点).sqrMagnitude < 间距平方) return true;
        return false;
    }
    public bool 尝试选点(战斗敌人级别 类, Vector2 玩家, out Vector2 位置)
    {
        位置 = default;
        if (类 == 战斗敌人级别.王级 && !地图.生存大图)
        {
            if (!有北端位置 || 拥挤(北端位置)) return false;
            位置 = 北端位置; return true;
        }
        // 无相机的离线调用沿用正式场景的默认正交视野；运行时始终读取实际相机。
        float 半高 = 地图.生存大图 ? (float)天帝数值.取("map.arena.camera_half_height") : 14;
        Rect 视野 = 读取视野 != null ? 读取视野() : new Rect(玩家.x - 半高 * 16f / 9, 玩家.y - 半高, 2 * 半高 * 16f / 9, 2 * 半高);
        if (视野.width <= 0 || 视野.height <= 0 || float.IsNaN(视野.width) || float.IsNaN(视野.height)) return false;
        var 安全视野 = Rect.MinMaxRect(视野.xMin - 屏外余量, 视野.yMin - 屏外余量, 视野.xMax + 屏外余量, 视野.yMax + 屏外余量);
        if (地图.生存大图)
        {
            int 起边 = 序号++ % 4;
            float 最小距 = (float)天帝数值.取("map.arena.spawn_player_distance");
            float 半径 = (float)天帝数值.取(类 == 战斗敌人级别.王级 ? "map.boss_spawn_radius" : "map.spawn_radius");
            for (int k = 0; k < 4; k++)
            {
                var 列 = 边缘[(起边 + k) % 4]; if (列.Count == 0) continue;
                int 起 = (int)((uint)(序号 * 7919 + 地图.种子) % (uint)列.Count);
                for (int i = 0; i < 列.Count; i++)
                {
                    var 点 = 列[(起 + i) % 列.Count];
                    if (安全视野.Contains(点) || (点 - 玩家).sqrMagnitude < 最小距 * 最小距 || !地图.可站立(点, 半径) || 拥挤(点)) continue;
                    位置 = 点; return true;
                }
            }
            return false;
        }
        float 角 = (++序号 * 137.508f + 地图.种子 % 360) * Mathf.Deg2Rad;
        var 方向 = new Vector2(Mathf.Cos(角), Mathf.Sin(角));
        float 距边 = Mathf.Min(安全视野.width * .5f / Mathf.Max(.001f, Mathf.Abs(方向.x)), 安全视野.height * .5f / Mathf.Max(.001f, Mathf.Abs(方向.y)));
        var 期望 = 安全视野.center + 方向 * (距边 + 1);
        float 最佳 = float.MaxValue; bool 找到 = false;
        foreach (var 点 in 候选)
        {
            if (安全视野.Contains(点)) continue;
            float 分 = (点 - 期望).sqrMagnitude;
            if (分 >= 最佳 || 拥挤(点)) continue;
            最佳 = 分; 位置 = 点; 找到 = true;
        }
        // 不把失败退化为屏内硬刷；预留敌人留在队列中，玩家移动后重试。
        return 找到;
    }
}
