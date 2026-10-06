using System.Collections.Generic;
using UnityEngine;

// 只平滑显示轮廓，不改变地图碰撞；闭合线始终来自相邻通行/阻挡采样格。
public static class 天帝通行轮廓
{
    public static List<List<Vector2>> 获取(天帝战斗地图 地图)
    {
        var 边 = new Dictionary<Vector2Int, List<Vector2Int>>();
        void 添(Vector2Int 起, Vector2Int 终)
        {
            if (!边.TryGetValue(起, out var 后续)) 边.Add(起, 后续 = new List<Vector2Int>(2));
            后续.Add(终);
        }
        for (int y = 0; y < 地图.区域高; y++) for (int x = 0; x < 地图.区域宽; x++)
        {
            if (!地图.区域格可通行(x, y)) continue;
            if (!地图.区域格可通行(x, y - 1)) 添(new Vector2Int(x,y),new Vector2Int(x+1,y));
            if (!地图.区域格可通行(x + 1, y)) 添(new Vector2Int(x+1,y),new Vector2Int(x+1,y+1));
            if (!地图.区域格可通行(x, y + 1)) 添(new Vector2Int(x+1,y+1),new Vector2Int(x,y+1));
            if (!地图.区域格可通行(x - 1, y)) 添(new Vector2Int(x,y+1),new Vector2Int(x,y));
        }
        var 结果 = new List<List<Vector2>>();
        while (边.Count > 0)
        {
            Vector2Int 起 = default;
            foreach (var 项 in 边) { 起 = 项.Key; break; }
            var 格线 = new List<Vector2Int>(); var 当前 = 起; var 前向 = Vector2Int.zero;
            do
            {
                格线.Add(当前);
                if (!边.TryGetValue(当前, out var 后续)) throw new System.InvalidOperationException("通行轮廓未闭合。");
                int 最佳 = 0;
                // 对角格在同一角点相遇时优先左转，分别闭合，避免两条轮廓交叉。
                if (后续.Count > 1 && 前向 != Vector2Int.zero)
                    for (int i = 1; i < 后续.Count; i++)
                        if (转向(前向, 后续[i] - 当前) > 转向(前向, 后续[最佳] - 当前)) 最佳 = i;
                var 下个 = 后续[最佳]; 后续.RemoveAt(最佳); if (后续.Count == 0) 边.Remove(当前);
                前向 = 下个 - 当前; 当前 = 下个;
            } while (当前 != 起);
            var 点 = new List<Vector2>();
            foreach (var 格 in 格线) 点.Add(new Vector2(格.x * 天帝战斗地图.区域格边长 - 地图.半宽, 格.y * 天帝战斗地图.区域格边长 - 地图.半高));
            if (点.Count >= 4) 结果.Add(圆角(简化闭合(点, .65f)));
        }
        return 结果;
    }
    static int 转向(Vector2Int 前, Vector2Int 后)
    {
        int 叉 = 前.x * 后.y - 前.y * 后.x;
        return 叉 > 0 ? 3 : 前.x * 后.x + 前.y * 后.y > 0 ? 2 : 叉 < 0 ? 1 : 0;
    }
    static List<Vector2> 简化闭合(List<Vector2> 点, float 误差)
    {
        int 远 = 1;
        for (int i = 2; i < 点.Count; i++) if ((点[i]-点[0]).sqrMagnitude > (点[远]-点[0]).sqrMagnitude) 远 = i;
        var 线 = new List<Vector2>(点); 线.Add(点[0]); var 保留 = new bool[线.Count];
        保留[0] = 保留[远] = 保留[线.Count-1] = true;
        简化(线,0,远,误差*误差,保留); 简化(线,远,线.Count-1,误差*误差,保留);
        var 结果 = new List<Vector2>();
        for (int i=0;i<点.Count;i++) if(保留[i]) 结果.Add(点[i]);
        return 结果.Count >= 3 ? 结果 : 点;
    }
    static void 简化(List<Vector2> 点,int 起,int 终,float 误差平方,bool[] 保留)
    {
        int 最远=-1; float 最大=误差平方; var 向=点[终]-点[起]; float 长方=向.sqrMagnitude;
        for(int i=起+1;i<终;i++)
        {
            float t=长方>0 ? Mathf.Clamp01(Vector2.Dot(点[i]-点[起],向)/长方) : 0;
            float 距=(点[i]-点[起]-向*t).sqrMagnitude;
            if(距>最大){最大=距;最远=i;}
        }
        if(最远<0)return; 保留[最远]=true;
        简化(点,起,最远,误差平方,保留); 简化(点,最远,终,误差平方,保留);
    }
    static List<Vector2> 圆角(List<Vector2> 点)
    {
        var 结果=new List<Vector2>();
        for(int i=0;i<点.Count;i++)
        {
            var 前=点[(i+点.Count-1)%点.Count];var 中=点[i];var 后=点[(i+1)%点.Count];
            float 切=Mathf.Min(.35f,Mathf.Min((前-中).magnitude,(后-中).magnitude)*.25f);
            var 起=中+(前-中).normalized*切;var 终=中+(后-中).normalized*切;
            for(int j=0;j<=4;j++)
            {
                float t=j/4f;结果.Add((1-t)*(1-t)*起+2*(1-t)*t*中+t*t*终);
            }
        }
        return 结果;
    }
}
