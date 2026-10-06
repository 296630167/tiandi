using System;
using System.Collections.Generic;
using UnityEngine;

// 只读诊断与沙盘计算。预览操作在独立副本执行，不改真实道纹、技能点或存档。
public sealed class 天帝道纹连接诊断
{
    readonly 天帝道纹 数据;
    readonly Dictionary<Vector2Int, Vector2Int> 来路 = new Dictionary<Vector2Int, Vector2Int>();
    public 天帝道纹连接诊断(天帝道纹 数据)
    {
        this.数据 = 数据;
        遍历(数据, true, 来路);
    }
    static void 遍历(天帝道纹 网, bool 限方向, Dictionary<Vector2Int, Vector2Int> 父, int 源方向 = -1)
    {
        var 队 = new Queue<Vector2Int>(); 父[Vector2Int.zero] = Vector2Int.zero; 队.Enqueue(Vector2Int.zero);
        while (队.Count > 0)
        {
            var 格 = 队.Dequeue(); var 纹 = 网.已放置[格];
            for (int d = 0; d < 6; d++)
            {
                var 邻格 = 格 + 天帝道纹.邻向[d];
                if (格 == Vector2Int.zero && 源方向 >= 0 && d != 源方向 || 父.ContainsKey(邻格) || !纹.允许传出(d) || (限方向 && !天帝道纹.可传导(格, 邻格)) ||
                    !网.已放置.TryGetValue(邻格, out var 邻) || !邻.允许接入((d + 3) % 6)) continue;
                父[邻格] = 格; 队.Enqueue(邻格);
            }
        }
    }
    public List<Vector2Int> 路径(道纹实例 纹)
        => 路径(纹, -1);
    public List<Vector2Int> 路径(道纹实例 纹, int 通路)
    {
        var 路 = new List<Vector2Int>();
        var 父 = 来路;
        if (通路 >= 0) { 父 = new Dictionary<Vector2Int, Vector2Int>(); 遍历(数据, true, 父, 通路); }
        if (纹 == null || !纹.格子.HasValue || !父.ContainsKey(纹.格子.Value)) return 路;
        var 格 = 纹.格子.Value;
        while (true) { 路.Add(格); if (格 == Vector2Int.zero) break; 格 = 父[格]; }
        路.Reverse(); return 路;
    }
    public string 说明(道纹实例 纹)
    {
        if (纹 == null) return "悬停道纹查看通路；拖动时查看放置后的变化。";
        if (纹.是源纹) return "万物起点 · 金色路径指向接通的道纹。";
        if (!纹.格子.HasValue) return "待放置 · 接口相互对接，且沿同圈或外圈连到源纹后生效。";
        var 格 = 纹.格子.Value;
        if (来路.ContainsKey(格)) return "已生效 · 金色显示一条从源纹到此处的有效通路。";
        var 无方向 = new Dictionary<Vector2Int, Vector2Int>(); 遍历(数据, false, 无方向);
        if (无方向.ContainsKey(格))
        {
            var 路 = new List<Vector2Int>(); var 点 = 格;
            while (点 != Vector2Int.zero) { 路.Add(点); 点 = 无方向[点]; } 路.Add(Vector2Int.zero); 路.Reverse();
            for (int i = 1; i < 路.Count; i++) if (!天帝道纹.可传导(路[i - 1], 路[i]))
                return "传导受阻 · 此路线需从第" + 天帝道纹.格权重(路[i - 1]) + "圈返回第" + 天帝道纹.格权重(路[i]) + "圈；只允许同圈或向外。";
        }
        bool 有邻纹 = false, 已对接 = false;
        for (int d = 0; d < 6; d++)
        {
            if (!数据.已放置.TryGetValue(格 + 天帝道纹.邻向[d], out var 邻)) continue;
            有邻纹 = true; int 对口 = (d + 3) % 6;
            if (邻.是源纹 && !邻.有接口(对口))
            {
                int 解封等级 = ((6 - 对口) % 6) * 10;
                return "源纹接口封印 · " + 天帝道纹.方向名[对口] + "口需达到" + 解封等级 + "级；请接到已开放的接口。";
            }
            if (邻.生效 && !纹.有接口(d)) return "接口未对接 · 本纹的" + 天帝道纹.方向名[d] + "口未启用，可尝试右键旋转。";
            if (邻.生效 && !邻.有接口(对口)) return "接口未对接 · 邻纹的" + 天帝道纹.方向名[对口] + "口未启用。";
            已对接 |= 纹.有接口(d) && 邻.有接口(对口);
        }
        return 已对接 ? "分支未接源纹 · 已与邻纹对接，但上游还没有有效通路。" :
            有邻纹 ? "接口未对接 · 相邻两枚道纹必须在对应方向同时有接口。" : "独立道纹 · 周围没有相邻道纹，尚未连接到源纹。";
    }
    public static 天帝道纹 副本(天帝道纹 原)
    {
        var 存 = 原.导出存档(); bool 旧源 = 存.天赋编号 < 0;
        if (旧源) 存.天赋编号 = (int)天赋种类.普通人;
        var 网 = 天帝道纹.读取存档(存);
        if (旧源)
        {
            var 源 = 原.已放置[Vector2Int.zero];
            var 新源 = 天帝道纹.创建源纹((int)源.属性);
            新源.接口 = 源.接口; 新源.数值 = 源.数值; 新源.等级 = 源.等级;
            网.已放置[Vector2Int.zero] = 新源; 网.重算();
        }
        return 网;
    }
    public 天帝道纹 预览(道纹实例 纹, Vector2Int? 放置格, bool 旋转, int? 接口覆盖 = null, int? 入口覆盖 = null)
    {
        if (纹 == null || 纹.是源纹 || !数据.道纹.Contains(纹)) return null;
        if (放置格.HasValue && !数据.可放置(纹, 放置格.Value)) return null;
        if (旋转 && !纹.格子.HasValue) return null;
        var 网 = 副本(数据); var 新纹 = 网.道纹.Find(x => x.编号 == 纹.编号);
        if (接口覆盖.HasValue)
        {
            if (!天帝道纹.同形接口(新纹.接口, 接口覆盖.Value)) return null;
            新纹.接口 = 接口覆盖.Value;
        }
        if (旋转) 网.旋转(新纹);
        else if (放置格.HasValue) 网.放置(新纹, 放置格.Value);
        else 网.收回(新纹);
        return 网;
    }
}
