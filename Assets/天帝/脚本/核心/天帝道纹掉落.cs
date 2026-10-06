using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class 战斗道纹掉落
{
    public Vector2 位置 { get; internal set; }
    public 道纹实例 道纹 { get; internal set; }
    public bool 已拾取 { get; internal set; }
    public 天帝掉落吸附 吸附 { get; internal set; }
    public Vector2 显示位置 => 吸附?.位置 ?? 位置;
    public bool 演出结束 => 已拾取 && (吸附 == null || 吸附.完成);
}

// 死亡掉落全图自动入库；地面实例只保留吸附演出，不发经验。
public sealed class 天帝道纹掉落
{
    public const float 拾取半径 = 1.8f; // 旧工具API兼容，实际拾取不再使用距离。
    public static IReadOnlyList<int> 普通品阶权重 { get; } = Array.AsReadOnly(天帝数值.整数表("loot.rune_grade_weights.0", 8));
    public static IReadOnlyList<int> 困难品阶权重 { get; } = Array.AsReadOnly(天帝数值.整数表("loot.rune_grade_weights.1", 8));
    public static float 基础概率(战斗敌人级别 级别, 战斗难度 难度)
    {
        return (float)天帝数值.取("loot.rune_probability." + (难度 == 战斗难度.困难 ? 1 : 0) + "." + (int)级别);
    }
    public static 道纹品阶 品阶结果(战斗难度 难度, int 权重点)
    {
        if (权重点 < 0 || 权重点 >= 10000) throw new ArgumentOutOfRangeException(nameof(权重点));
        var 权重 = 难度 == 战斗难度.困难 ? 困难品阶权重 : 普通品阶权重; int 累计 = 0;
        for (int i = 0; i < 权重.Count; i++) { 累计 += 权重[i]; if (权重点 < 累计) return (道纹品阶)i; }
        throw new InvalidOperationException("掉落品阶权重总和必须为10000");
    }
    public static 道纹品阶 BOSS保底品阶(int 等级)
    {
        if (等级 < 1) throw new ArgumentOutOfRangeException(nameof(等级));
        var 品阶 = 道纹品阶.稀有;
        for (int 序 = 0; 序 <= (int)道纹品阶.传说 - (int)道纹品阶.稀有; 序++)
        {
            if (等级 < 天帝数值.取("loot.boss_grade_levels." + 序)) break;
            品阶 = (道纹品阶)((int)道纹品阶.稀有 + 序);
        }
        return 品阶;
    }
    readonly List<战斗道纹掉落> 地面数据 = new List<战斗道纹掉落>(66);
    readonly HashSet<战斗敌人> 已结算 = new HashSet<战斗敌人>();
    readonly 天帝战斗地图 地图;
    readonly 天帝战斗寻路 寻路;
    readonly 天帝道纹 库存;
    readonly 战斗难度 难度;
    readonly System.Random 随机;
    readonly System.Random 特性随机;
    readonly Func<道纹品阶> 抽品阶;
    bool 正在拾取;
    public IReadOnlyList<战斗道纹掉落> 地面 => 地面数据;
    public int 拾取数 { get; private set; }
    public 道纹实例 最近拾取 { get; private set; }
    public event Action<道纹实例> 获得道纹;
    public 天帝道纹掉落(天帝战斗地图 地图, 天帝道纹 库存, 战斗难度 难度)
    {
        this.地图 = 地图; this.库存 = 库存; this.难度 = 难度;
        寻路 = new 天帝战斗寻路(地图); 随机 = new System.Random(unchecked(地图.种子 ^ 0x571A93));
        特性随机 = new System.Random(unchecked(地图.种子 ^ 0x371B94));
        抽品阶 = () => 品阶结果(难度, 随机.Next(10000));
    }
    internal bool 敌人死亡(战斗敌人 敌)
    {
        if (敌 == null || 敌.存活 || !已结算.Add(敌)) return false;
        bool 特性掉了 = 生成特性掉落(敌);
        if (随机.NextDouble() >= 天帝天赋效果.掉落概率(库存.天赋, 基础概率(敌.布点.级别, 难度))) return 特性掉了;
        var 品阶 = 天帝天赋效果.掉落品阶(库存.天赋, 抽品阶);
        // 非BOSS的少量独立分叉只提供接口；BOSS固定一枚属性道纹。
        bool 分叉 = 随机.Next(100) < 天帝数值.取("rune.branch_drop_percent." + (难度 == 战斗难度.困难 ? 1 : 0));
        if (敌.布点.级别 == 战斗敌人级别.王级)
        {
            var 保底 = BOSS保底品阶(敌.等级);
            if (品阶 < 保底) 品阶 = 保底;
            分叉 = false;
        }
        // BOSS保底仍给属性道纹，避免高等级保底与功能固定稀有冲突。
        道纹属性分组? 分组 = 敌.布点.级别 == 战斗敌人级别.王级 ?
            天帝道纹属性.分组(天帝道纹属性.非功能属性[随机.Next(天帝道纹属性.非功能属性.Length)]) : (道纹属性分组?)null;
        var 纹 = 天帝道纹生成.创建(1, 分叉 ? 道纹分类.分叉 : 道纹分类.属性, 品阶, 随机, 分组, 敌.等级);
        Vector2 点 = 地图.可站立(敌.位置) ? 敌.位置 : 敌.布点.位置;
        地面数据.Add(new 战斗道纹掉落 { 位置 = 点, 道纹 = 纹 }); 自动拾取(); return true;
    }
    bool 生成特性掉落(战斗敌人 敌)
    {
        if(敌.等级<天帝特性道纹.取("grades.0.敌等级") || 特性随机.NextDouble()>=天帝天赋效果.掉落概率(库存.天赋,(float)天帝特性道纹.取("loot.probabilities."+(int)敌.布点.级别)))return false;
        string key=敌.布点.级别==战斗敌人级别.王级?"boss_weights":"weights";int total=0;
        for(int i=0;i<8;i++)if(敌.等级>=天帝特性道纹.取("grades."+i+".敌等级"))total+=(int)天帝特性道纹.取("loot."+key+"."+i);
        int roll=特性随机.Next(total),grade=0;
        for(int i=0;i<8;i++)if(敌.等级>=天帝特性道纹.取("grades."+i+".敌等级")){roll-=(int)天帝特性道纹.取("loot."+key+"."+i);if(roll<0){grade=i;break;}}
        var kind=特性随机.NextDouble()<天帝特性道纹.取("loot.trait_share")?道纹分类.特性:道纹分类.转化;
        var r=天帝道纹生成.创建(1,kind,(道纹品阶)grade,特性随机,物品等级:敌.等级);
        地面数据.Add(new 战斗道纹掉落 { 位置=地图.可站立(敌.位置)?敌.位置:敌.布点.位置,道纹=r });自动拾取();return true;
    }
    public int 拾取附近(Vector2 玩家, bool 玩家死亡) => 自动拾取();
    public int 自动拾取()
    {
        if (正在拾取) return 0;
        int 数 = 0;
        正在拾取 = true;
        try
        {
            int 上限 = 地面数据.Count;
            for (int i = 0; i < 上限; i++)
            {
                var 物 = 地面数据[i]; if (物.已拾取 || !库存.获得道纹(物.道纹)) continue;
                物.已拾取 = true; 物.吸附 = new 天帝掉落吸附(物.位置);
                最近拾取 = 物.道纹; 拾取数++; 数++; 获得道纹?.Invoke(物.道纹);
            }
        }
        finally { 正在拾取 = false; }
        return 数;
    }
    public void 推进吸附(Vector2 玩家, float 秒)
    {
        自动拾取(); foreach (var 物 in 地面数据) 物.吸附?.推进(玩家, 秒);
    }
    public 战斗道纹掉落 附近详情(Vector2 玩家)
    {
        战斗道纹掉落 最近 = null; float 距 = 36;
        foreach (var 物 in 地面数据)
        {
            float d = (物.位置 - 玩家).sqrMagnitude;
            if (物.已拾取 || d > 距 || !寻路.无遮挡(玩家, 物.位置)) continue;
            最近 = 物; 距 = d;
        }
        return 最近;
    }
}
