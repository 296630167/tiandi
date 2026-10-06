using System.Collections.Generic;
using UnityEngine;

// 大图使用1米导航节点，旧地图沿用4米节点。节点和相邻边都由实际碰撞区域检查。
public sealed class 天帝战斗寻路
{
    readonly 天帝战斗地图 地图;
    readonly int[] 前驱;
    readonly int[] 队列;
    readonly byte[] 连通方向;
    readonly bool[] 导航可走;
    readonly int 导航宽, 导航高;
    readonly float 导航边长;
    static readonly Vector2Int[] 四向 = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
    public 天帝战斗寻路(天帝战斗地图 地图)
    {
        this.地图 = 地图; 导航边长 = 地图.精细碰撞 ? 1 : 天帝战斗地图.格边长;
        导航宽 = Mathf.RoundToInt(地图.半宽 * 2 / 导航边长); 导航高 = Mathf.RoundToInt(地图.半高 * 2 / 导航边长);
        前驱 = new int[导航宽 * 导航高]; 队列 = new int[前驱.Length];
        if (!地图.精细碰撞) return;
        导航可走 = new bool[前驱.Length]; 连通方向 = new byte[前驱.Length];
        for (int i = 0; i < 前驱.Length; i++) 导航可走[i] = 地图.可站立(中心(坐标(i)));
        for (int i = 0; i < 前驱.Length; i++)
        {
            if (!导航可走[i]) continue; var 格 = 坐标(i);
            for (int d = 0; d < 2; d++)
            {
                var 邻 = 格 + 四向[d]; if (!允许(邻, false, false)) continue;
                int 号 = 编号(邻);
                if (!无遮挡(中心(格), 中心(邻), .45f)) continue;
                连通方向[i] |= (byte)(1 << d); 连通方向[号] |= (byte)(1 << (d + 2));
            }
        }
    }
    int 编号(Vector2Int 格) => 格.y * 导航宽 + 格.x;
    Vector2Int 坐标(int 号) => new Vector2Int(号 % 导航宽, 号 / 导航宽);
    Vector2 中心(Vector2Int 格) => new Vector2((格.x + .5f) * 导航边长 - 地图.半宽, (格.y + .5f) * 导航边长 - 地图.半高);
    public Vector2Int 目标格(Vector2 点) => new Vector2Int(Mathf.FloorToInt((点.x + 地图.半宽) / 导航边长), Mathf.FloorToInt((点.y + 地图.半高) / 导航边长));
    bool 找节点(Vector2 点, out Vector2Int 格)
    {
        格 = 目标格(点);
        if (!地图.精细碰撞) return true;
        if (!地图.可站立(点)) return false;
        var 原格 = 格; float 最小 = float.MaxValue; bool 找到 = false;
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++)
        {
            var 候选 = 原格 + new Vector2Int(x, y); if (!允许(候选, false, false)) continue;
            float 距 = (中心(候选) - 点).sqrMagnitude;
            if (距 >= 最小 || !无遮挡(点, 中心(候选), .45f)) continue;
            格 = 候选; 最小 = 距; 找到 = true;
        }
        return 找到;
    }
    public bool 无遮挡(Vector2 起, Vector2 终, float 半径 = 0)
    {
        if (!地图.可站立(起, 半径) || !地图.可站立(终, 半径)) return false;
        int 步 = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(起, 终) / 0.2f));
        for (int i = 1; i < 步; i++) if (!地图.可站立(Vector2.Lerp(起, 终, i / (float)步), 半径)) return false;
        return true;
    }
    // 刷新点只预计算一次出生点所在的连通区域，避免为每个候选点重复寻路。
    public void 取得连通节点(Vector2 起, List<Vector2> 输出)
    {
        输出.Clear();
        if (!找节点(起, out var 起格) || !允许(起格, false, false)) return;
        for (int i = 0; i < 前驱.Length; i++) 前驱[i] = -1;
        int 起号 = 编号(起格), 头 = 0, 尾 = 0;
        队列[尾++] = 起号; 前驱[起号] = 起号;
        while (头 < 尾)
        {
            int 当前 = 队列[头++]; var 格 = 坐标(当前); 输出.Add(中心(格));
            for (int d = 0; d < 四向.Length; d++)
            {
                if (地图.精细碰撞 && (连通方向[当前] & (1 << d)) == 0) continue;
                var 新 = 格 + 四向[d]; if (!允许(新, false, false)) continue;
                int 新号 = 编号(新); if (前驱[新号] >= 0) continue;
                前驱[新号] = 当前; 队列[尾++] = 新号;
            }
        }
    }
    public bool 路径(Vector2 起, Vector2 终, List<Vector2> 输出, bool 限王房, bool 禁王房, System.Func<Vector2,Vector2,bool> 阻挡=null)
    {
        输出.Clear();
        if (!找节点(起, out var 起格) || !找节点(终, out var 终格)) return false;
        if (!允许(起格, 限王房, 禁王房) || !允许(终格, 限王房, 禁王房)) return false;
        for (int i = 0; i < 前驱.Length; i++) 前驱[i] = -1;
        int 起号 = 编号(起格), 终号 = 编号(终格), 头 = 0, 尾 = 0;
        队列[尾++] = 起号; 前驱[起号] = 起号;
        while (头 < 尾 && 前驱[终号] < 0)
        {
            int 当前 = 队列[头++]; var 格 = 坐标(当前);
            for (int d = 0; d < 四向.Length; d++)
            {
                if (地图.精细碰撞 && (连通方向[当前] & (1 << d)) == 0) continue;
                var 新 = 格 + 四向[d]; if (!允许(新, 限王房, 禁王房)) continue;
                int 新号 = 编号(新); if (前驱[新号] >= 0 || 阻挡!=null && 阻挡(中心(格),中心(新))) continue;
                前驱[新号] = 当前; 队列[尾++] = 新号;
            }
        }
        if (前驱[终号] < 0) return false;
        for (int 当前 = 终号; 当前 != 起号; 当前 = 前驱[当前])
        { var 格 = 坐标(当前); 输出.Add(中心(格)); }
        输出.Reverse();
        // 起点位于格边缘时先回到所在格中心，避免转角切入障碍。
        if (输出.Count > 0 && !无遮挡(起, 输出[0], 0.45f)) 输出.Insert(0, 中心(起格));
        if (地图.精细碰撞 && !无遮挡(输出.Count > 0 ? 输出[输出.Count - 1] : 起, 终, .45f)) 输出.Add(中心(终格));
        输出.Add(终); return true;
    }
    bool 允许(Vector2Int 格, bool 限王房, bool 禁王房)
    {
        if (地图.精细碰撞)
        {
            if (格.x < 0 || 格.y < 0 || 格.x >= 导航宽 || 格.y >= 导航高 || !导航可走[编号(格)]) return false;
            if (!地图.横向区域) return true;
            bool 王区 = 地图.王房范围.Contains(地图.所在格(中心(格)));
            return (!限王房 || 王区) && (!禁王房 || !王区);
        }
        if (!地图.可通行格(格)) return false;
        bool 在王房 = 地图.王房范围.Contains(格);
        return (!限王房 || 在王房) && (!禁王房 || !在王房);
    }
}
