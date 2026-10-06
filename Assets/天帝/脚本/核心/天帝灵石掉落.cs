using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class 战斗灵石掉落
{
    public Vector2 位置 { get; internal set; }
    public int 数量 { get; internal set; }
    public bool 已拾取 { get; internal set; }
    public 天帝掉落吸附 吸附 { get; internal set; }
    public Vector2 显示位置 => 吸附?.位置 ?? 位置;
    public bool 演出结束 => 已拾取 && (吸附 == null || 吸附.完成);
}

// 独立随机流按正式概率判定，一次死亡最多一堆；先入余额，再播放吸附。
public sealed class 天帝灵石掉落
{
    readonly 天帝宝盒 库存;
    readonly List<战斗灵石掉落> 地面数据 = new List<战斗灵石掉落>(66);
    readonly HashSet<战斗敌人> 已结算 = new HashSet<战斗敌人>();
    readonly System.Random 随机;
    readonly 天赋定义 天赋;
    bool 正在拾取;
    public IReadOnlyList<战斗灵石掉落> 地面 => 地面数据;
    public int 拾取总量 { get; private set; }
    public event Action<int> 获得灵石;

    public static float 基础概率(战斗敌人级别 级别) => (float)天帝数值.取("loot.stone_probability." + (int)级别);
    public 天帝灵石掉落(天帝宝盒 库存, int 地图种子 = 42, 天赋定义 天赋 = null)
    {
        this.库存 = 库存; this.天赋 = 天赋;
        随机 = new System.Random(unchecked(地图种子 ^ 0x397B21));
    }
    internal bool 敌人死亡(战斗敌人 敌)
    {
        if (敌 == null || !敌.已生成 || 敌.存活 || !已结算.Add(敌)) return false;
        if (随机.NextDouble() >= 天帝天赋效果.掉落概率(天赋, 基础概率(敌.布点.级别))) return false;
        int 数量 = 天帝宝盒.击败奖励(敌.布点.级别);
        if (数量 <= 0) return false;
        地面数据.Add(new 战斗灵石掉落 { 位置 = 敌.位置, 数量 = 数量 });
        自动拾取(); return true;
    }
    public int 自动拾取()
    {
        if (库存 == null || 正在拾取) return 0;
        int 数 = 0; 正在拾取 = true;
        try
        {
            int 上限 = 地面数据.Count;
            for (int i = 0; i < 上限; i++)
            {
                var 物 = 地面数据[i];
                if (物.已拾取 || !库存.获得灵石(物.数量)) continue;
                物.已拾取 = true; 物.吸附 = new 天帝掉落吸附(物.位置);
                拾取总量 += 物.数量; 数++; 获得灵石?.Invoke(物.数量);
            }
        }
        finally { 正在拾取 = false; }
        return 数;
    }
    public void 推进吸附(Vector2 玩家, float 秒)
    {
        自动拾取(); foreach (var 物 in 地面数据) 物.吸附?.推进(玩家, 秒);
    }
}
