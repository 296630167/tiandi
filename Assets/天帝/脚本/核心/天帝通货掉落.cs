using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class 战斗通货掉落
{
    public Vector2 位置 { get; internal set; }
    public 通货种类 种类 { get; internal set; }
    public int 数量 { get; internal set; }
    public bool 已拾取 { get; internal set; }
    public 天帝掉落吸附 吸附 { get; internal set; }
    public Vector2 显示位置 => 吸附?.位置 ?? 位置;
    public bool 演出结束 => 已拾取 && (吸附 == null || 吸附.完成);
}

// 与道纹独立的随机流；死亡只结算一次，成功入库后播放全图吸附。
public sealed class 天帝通货掉落
{
    public static IReadOnlyList<int> 种类权重 { get; } = Array.AsReadOnly(天帝数值.整数表("loot.currency_weights", 13));
    public static float 基础概率(战斗敌人级别 级别, 战斗难度 难度)
    {
        return (float)天帝数值.取("loot.currency_probability." + (难度 == 战斗难度.困难 ? 1 : 0) + "." + (int)级别);
    }
    public static int 堆叠数量(战斗敌人级别 级别, 战斗难度 难度)
    {
        return (int)天帝数值.取("loot.currency_stacks." + (难度 == 战斗难度.困难 ? 1 : 0) + "." + (int)级别);
    }
    public static 通货种类 种类结果(int 权重点)
    {
        if (权重点 < 0 || 权重点 >= 10000) throw new ArgumentOutOfRangeException(nameof(权重点));
        int 累计 = 0;
        for (int i = 0; i < 种类权重.Count; i++) { 累计 += 种类权重[i]; if (权重点 < 累计) return (通货种类)i; }
        throw new InvalidOperationException("通货权重总和必须为10000");
    }
    readonly List<战斗通货掉落> 地面数据 = new List<战斗通货掉落>(66);
    readonly HashSet<战斗敌人> 已结算 = new HashSet<战斗敌人>();
    readonly 天帝战斗地图 地图;
    readonly 天帝战斗寻路 寻路;
    readonly 天帝通货 库存;
    readonly 天帝道纹 道纹;
    readonly 战斗难度 难度;
    readonly System.Random 随机;
    bool 正在拾取;
    public IReadOnlyList<战斗通货掉落> 地面 => 地面数据;
    public int 拾取数 { get; private set; } // 堆数
    public int 拾取总量 { get; private set; }
    public event Action<通货种类, int> 获得通货;
    public 天帝通货掉落(天帝战斗地图 地图, 天帝道纹 道纹, 天帝通货 库存, 战斗难度 难度)
    {
        this.地图 = 地图; this.道纹 = 道纹; this.库存 = 库存; this.难度 = 难度;
        寻路 = new 天帝战斗寻路(地图); 随机 = new System.Random(unchecked(地图.种子 ^ 0x6C091B));
    }
    internal bool 敌人死亡(战斗敌人 敌)
    {
        if (敌 == null || 敌.存活 || !已结算.Add(敌)) return false;
        if (随机.NextDouble() >= 天帝天赋效果.掉落概率(道纹.天赋, 基础概率(敌.布点.级别, 难度))) return false;
        Vector2 点 = 地图.可站立(敌.位置) ? 敌.位置 : 敌.布点.位置;
        var 偏 = 点 + Vector2.right * .8f;
        if (地图.可站立(偏) && 寻路.无遮挡(点, 偏)) 点 = 偏;
        地面数据.Add(new 战斗通货掉落 { 位置 = 点, 种类 = 种类结果(随机.Next(10000)), 数量 = 堆叠数量(敌.布点.级别, 难度) });
        自动拾取();
        return true;
    }
    public int 拾取附近(Vector2 玩家, bool 玩家死亡) => 自动拾取();
    public int 自动拾取()
    {
        if (库存 == null || 正在拾取) return 0;
        int 数 = 0;
        正在拾取 = true;
        try
        {
            int 上限 = 地面数据.Count;
            for (int i = 0; i < 上限; i++)
            {
                var 物 = 地面数据[i]; if (物.已拾取 || !库存.获得(物.种类, 物.数量)) continue;
                物.已拾取 = true; 物.吸附 = new 天帝掉落吸附(物.位置);
                拾取数++; 拾取总量 += 物.数量; 数++; 获得通货?.Invoke(物.种类, 物.数量);
            }
        }
        finally { 正在拾取 = false; }
        return 数;
    }
    public void 推进吸附(Vector2 玩家, float 秒)
    {
        自动拾取(); foreach (var 物 in 地面数据) 物.吸附?.推进(玩家, 秒);
    }
    public 战斗通货掉落 附近详情(Vector2 玩家)
    {
        战斗通货掉落 最近 = null; float 距 = 36;
        foreach (var 物 in 地面数据)
        {
            float d = (物.位置 - 玩家).sqrMagnitude;
            if (物.已拾取 || d > 距 || !寻路.无遮挡(玩家, 物.位置)) continue;
            最近 = 物; 距 = d;
        }
        return 最近;
    }
}

