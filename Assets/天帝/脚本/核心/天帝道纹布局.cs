using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class 道纹布局项
{
    public int 编号, 接口;
    public int 入口方向 = -1;
    public Vector2Int 格子;
    public 道纹布局项 副本() => new 道纹布局项 { 编号 = 编号, 接口 = 接口, 格子 = 格子, 入口方向 = 入口方向 };
}

[Serializable]
public sealed class 道纹布局方案
{
    public string 名称 = "";
    public List<道纹布局项> 摆放 = new List<道纹布局项>();
    public bool 已保存 => !string.IsNullOrWhiteSpace(名称);
    public 道纹布局方案 副本()
    {
        var 新 = new 道纹布局方案 { 名称 = 名称 ?? "" };
        if (摆放 != null) foreach (var 项 in 摆放) if (项 != null) 新.摆放.Add(项.副本());
        return 新;
    }
}

public sealed partial class 天帝道纹
{
    public const int 方案槽数 = 3;
    public readonly List<道纹布局方案> 布局方案 = new List<道纹布局方案>();
    public static int 顺时针接口(int 口) => ((口 >> 1) | ((口 & 1) << 5)) & 63;
    public static bool 同形接口(int 甲, int 乙)
    {
        if (甲 < 1 || 甲 > 63 || 乙 < 1 || 乙 > 63) return false;
        for (int i = 0; i < 6; i++, 甲 = 顺时针接口(甲)) if (甲 == 乙) return true;
        return false;
    }
    public int 通路掩码(道纹实例 纹)
    {
        if (纹 == null || 纹.是源纹) return 0;
        int 口 = 0; for (int d = 0; d < 6; d++) if (弹槽道纹[d].Contains(纹.编号)) 口 |= 1 << d;
        return 口;
    }
    public static string 通路名称(int 方向)
    {
        if (方向 < 0 || 方向 > 5) return "无通路";
        return "通路" + (((6 - 方向) % 6) + 1) + " · " + 方向名[方向];
    }
    public static int 通路解封等级(int 方向) => ((6 - 方向) % 6) * 10;
    public bool 通路开放(int 方向) => 方向 >= 0 && 方向 < 6 && 已放置[Vector2Int.zero].有接口(方向);
    public int 开放通路数 { get { int 数 = 0; for (int d = 0; d < 6; d++) if (通路开放(d)) 数++; return 数; } }
    public bool 通路参与射击(int 方向)
    {
        if (!通路开放(方向)) return false;
        if (弹槽生效数[方向] > 0) return true;
        // 新局没有下游道纹时保留第一路基础灵力弹；一旦玩家接通任意链路，空接口不再占用技能槽。
        for (int d = 0; d < 6; d++) if (弹槽生效数[d] > 0) return false;
        return 方向 == 0;
    }
    public int 射击通路数 { get { int 数 = 0; for (int d = 0; d < 6; d++) if (通路参与射击(d)) 数++; return 数; } }
    public List<道纹布局项> 当前布局()
    {
        var 项 = new List<道纹布局项>();
        foreach (var 纹 in 道纹) if (纹.格子.HasValue) 项.Add(new 道纹布局项 { 编号 = 纹.编号, 格子 = 纹.格子.Value, 接口 = 纹.接口, 入口方向 = 纹.是顺序功能 ? 纹.入口方向 : -1 });
        return 项;
    }
    internal void 读取布局方案(List<道纹布局方案> 存)
    {
        if (存 == null) return;
        for (int i = 0; i < Math.Min(方案槽数, 存.Count); i++)
        {
            // 旧布局原样保留；越界布局不可载入，玩家可查看后重新摆放并覆盖保存。
            var 方案 = 存[i]; if (方案 == null || !方案.已保存 || 方案.名称.Length > 20 || 方案.摆放 == null || 方案.摆放.Count >= 10000) continue;
            var 编号 = new HashSet<int>(); var 格 = new HashSet<Vector2Int>(); bool 有效 = true;
            foreach (var 项 in 方案.摆放)
                if (项 == null || 项.编号 < 1 || 项.接口 < 1 || 项.接口 > 63 || !在旧画布范围(项.格子) || 项.格子 == Vector2Int.zero || !编号.Add(项.编号) || !格.Add(项.格子)) { 有效 = false; break; }
            if (有效) 布局方案[i] = 方案.副本();
        }
    }
    public bool 保存布局方案(int 槽, string 名称)
    {
        if (槽 < 0 || 槽 >= 方案槽数 || string.IsNullOrWhiteSpace(名称)) return false;
        名称 = 名称.Trim(); if (名称.Length > 20) return false;
        布局方案[槽] = new 道纹布局方案 { 名称 = 名称, 摆放 = 当前布局() }; 状态改变?.Invoke(); return true;
    }
    bool 校验布局(IReadOnlyList<道纹布局项> 项, ISet<Vector2Int> 锁格, out string 原因)
    {
        原因 = ""; if (项 == null) { 原因 = "方案数据不完整"; return false; }
        var 库存 = new Dictionary<int, 道纹实例>(); foreach (var 纹 in 道纹) 库存[纹.编号] = 纹;
        var 编号 = new HashSet<int>(); var 位置 = new HashSet<Vector2Int>();
        foreach (var 条 in 项)
        {
            if (条 == null || !编号.Add(条.编号) || !位置.Add(条.格子)) { 原因 = "编号或位置重复"; return false; }
            if (!库存.TryGetValue(条.编号, out var 纹)) { 原因 = "缺少道纹 #" + 条.编号 + "，请先获得原实例"; return false; }
            if (!在范围(条.格子)) { 原因 = "目标格 " + 条.格子 + " 超出31×31画布，请重新摆放并保存方案"; return false; }
            if (条.格子 == Vector2Int.zero || !锁格.Contains(条.格子)) { 原因 = "目标格 " + 条.格子 + " 尚未解锁或不可用"; return false; }
            if (!同形接口(纹.接口, 条.接口)) { 原因 = "道纹 #" + 条.编号 + " 的接口形状已改变"; return false; }
            if (纹.是顺序功能 && !天帝顺序道纹.定义有效(纹.功能, 条.接口))
            { 原因 = "功能道纹的接口数量不匹配"; return false; }
        }
        return true;
    }
    public bool 检查布局方案(int 槽, out string 原因)
    {
        原因 = "尚未保存方案";
        return 槽 >= 0 && 槽 < 方案槽数 && 布局方案[槽].已保存 && 校验布局(布局方案[槽].摆放, 解锁格, out 原因);
    }
    public bool 应用布局(IReadOnlyList<道纹布局项> 项, out string 原因)
    {
        if (!校验布局(项, 解锁格, out 原因)) return false;
        写入布局(项); 重算(); return true;
    }
    void 写入布局(IReadOnlyList<道纹布局项> 项)
    {
        var 源 = 已放置[Vector2Int.zero]; 已放置.Clear(); 已放置[Vector2Int.zero] = 源;
        var 库存 = new Dictionary<int, 道纹实例>();
        foreach (var 纹 in 道纹) { 纹.格子 = null; 库存[纹.编号] = 纹; }
        foreach (var 条 in 项)
        {
            var 纹 = 库存[条.编号];
            纹.接口 = 条.接口; 纹.格子 = 条.格子; 已放置.Add(条.格子, 纹);
        }
    }
    public bool 载入布局方案(int 槽, out string 原因)
    {
        if (!检查布局方案(槽, out 原因)) return false;
        return 应用布局(布局方案[槽].摆放, out 原因);
    }
    internal static string 快照标识(道纹存档数据 快照)
    {
        // 方案的保存不影响撤销；进度、库存、词条等任何外部变更都会使旧撤销失效。
        var 原 = 快照.布局方案; 快照.布局方案 = null;
        try { return JsonUtility.ToJson(快照); } finally { 快照.布局方案 = 原; }
    }
    internal bool 恢复画布(道纹存档数据 前, string 预期状态, out string 原因)
    {
        原因 = "画布或库存已改变，无法撤销旧操作";
        if (前 == null || 快照标识(导出存档()) != 预期状态 || 前.玩家等级 != 玩家等级 || 前.道纹.Count != 道纹.Count ||
            前.技能点 + 前.解锁格.Count != 技能点 + 解锁格.Count) return false;
        var 格 = new HashSet<Vector2Int>(前.解锁格);
        if (格.Count != 前.解锁格.Count || !格.Contains(Vector2Int.zero)) return false;
        var 布局 = new List<道纹布局项>();
        foreach (var 条 in 前.道纹)
        {
            var 纹 = 道纹.Find(x => x.编号 == 条.编号);
            if (纹 == null || !同形接口(纹.接口, 条.接口)) return false;
            if (条.已放置) 布局.Add(new 道纹布局项 { 编号 = 条.编号, 接口 = 条.接口, 格子 = 条.格子, 入口方向 = 纹.是顺序功能 ? 条.入口方向 : -1 });
        }
        if (!校验布局(布局, 格, out 原因)) return false;
        解锁格.Clear(); foreach (var 点 in 格) 解锁格.Add(点); 技能点 = 前.技能点;
        foreach (var 条 in 前.道纹) { var 纹 = 道纹.Find(x => x.编号 == 条.编号); 纹.接口 = 条.接口; 纹.入口方向 = 条.入口方向; }
        写入布局(布局); 重算(); 原因 = ""; return true;
    }
}
