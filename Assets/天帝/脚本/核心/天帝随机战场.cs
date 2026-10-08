using System;
using System.Collections.Generic;
using UnityEngine;

public enum 随机场景障碍种类 { 岩石, 古树, 松树, 断墙 }
public sealed class 随机场景障碍
{
    public 随机场景障碍种类 种类;
    public Vector2 位置,半径,尺寸;
    public float 角度;
}
public sealed class 战场浅滩 { public Vector2 位置; public float 半径; }

// 原图通行边界+本局布点共同写入唯一碰撞位图，寻路/弹体/移动全部复用。
public sealed partial class 天帝战斗地图
{
    readonly List<随机场景障碍> 随机障碍数据=new List<随机场景障碍>();
    readonly List<战场浅滩> 浅滩数据=new List<战场浅滩>();
    public IReadOnlyList<随机场景障碍> 随机障碍=>随机障碍数据;
    public IReadOnlyList<战场浅滩> 浅滩=>浅滩数据;
    public int 战场布局 { get; private set; }
    public string 战场布局名=>战场布局==0?"岩林交错":战场布局==1?"断碑回廊":"疏林石阵";
    static float 地形参数(string k)=>(float)天帝数值.取("map.arena.terrain."+k);
    public float 地形移速(Vector2 点)
    {
        foreach(var 水 in 浅滩数据)if((水.位置-点).sqrMagnitude<水.半径*水.半径)return 地形参数("shallow_speed");
        return 1;
    }
    bool 保留通道(Vector2 点,float 转角)
    {
        if(点.magnitude<地形参数("spawn_safe"))return true;
        if(Mathf.Abs(点.x)>半宽-地形参数("edge_clear")||Mathf.Abs(点.y)>半高-地形参数("edge_clear"))return true;
        float x=点.x*Mathf.Cos(转角)+点.y*Mathf.Sin(转角),y=-点.x*Mathf.Sin(转角)+点.y*Mathf.Cos(转角);
        return Mathf.Min(Mathf.Abs(x),Mathf.Abs(y))<地形参数("corridor_half_width");
    }
    void 生成随机战场(System.Random rng)
    {
        战场布局=rng.Next(3);float 转角=(float)rng.NextDouble()*Mathf.PI*.5f;
        var 基础连通=new List<Vector2>();new 天帝战斗寻路(this).取得连通节点(Vector2.zero,基础连通);
        var 可达=new HashSet<Vector2>();var 节点=new List<Vector2>();
        var 中心列=new List<Vector2>();int 目标=rng.Next((int)地形参数("cluster_min"),(int)地形参数("cluster_max")+1),占地=0;
        for(int 尝试=0;尝试<地形参数("attempts")&&中心列.Count<目标;尝试++)
        {
            Vector2 中=new Vector2(((float)rng.NextDouble()*2-1)*(半宽-地形参数("edge_clear")),((float)rng.NextDouble()*2-1)*(半高-地形参数("edge_clear")));
            if(保留通道(中,转角))continue;
            bool 近=false;foreach(var c in 中心列)if(Vector2.Distance(c,中)<地形参数("cluster_gap")){近=true;break;}if(近)continue;
            int 数=rng.Next((int)地形参数("members_min"),(int)地形参数("members_max")+1);
            float 朝=(float)rng.NextDouble()*Mathf.PI*2;var 候选=new List<随机场景障碍>(数);var 变化=new List<Vector2Int>();bool 有效=true;
            for(int i=0;i<数;i++)
            {
                float a=朝+(战场布局==2?i*Mathf.PI*2/数:0);
                Vector2 点=中+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(战场布局==2?地形参数("member_step"):(i-(数-1)*.5f)*地形参数("member_step"));
                float 缩=地形参数("scale_min")+(float)rng.NextDouble()*(地形参数("scale_max")-地形参数("scale_min"));
                int 抽=rng.Next(10);var 类=战场布局==1&&抽<6?随机场景障碍种类.断墙:抽<4?随机场景障碍种类.岩石:抽<7?随机场景障碍种类.松树:随机场景障碍种类.古树;
                var b=new 随机场景障碍{种类=类,位置=点,角度=类==随机场景障碍种类.断墙?朝*Mathf.Rad2Deg:0};
                float r=地形参数(类==随机场景障碍种类.岩石?"rock_radius":"tree_radius");
                b.半径=类==随机场景障碍种类.断墙?new Vector2(地形参数("ruin_half_length"),地形参数("ruin_half_width"))*缩:new Vector2(r,r*.8f)*缩;
                b.尺寸=(类==随机场景障碍种类.岩石?new Vector2(3.5f,2.8f):类==随机场景障碍种类.断墙?new Vector2(3.2f,1.15f):new Vector2(3.6f,4.2f))*缩;
                if(!可站立(点,Mathf.Max(b.半径.x,b.半径.y))||保留通道(点,转角)){有效=false;break;}
                int x0=Mathf.Clamp(Mathf.FloorToInt((点.x-2+半宽)/区域格边长),0,区域宽-1),x1=Mathf.Clamp(Mathf.CeilToInt((点.x+2+半宽)/区域格边长),0,区域宽-1);
                int y0=Mathf.Clamp(Mathf.FloorToInt((点.y-2+半高)/区域格边长),0,区域高-1),y1=Mathf.Clamp(Mathf.CeilToInt((点.y+2+半高)/区域格边长),0,区域高-1);
                float cos=Mathf.Cos(b.角度*Mathf.Deg2Rad),sin=Mathf.Sin(b.角度*Mathf.Deg2Rad);
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                {
                    Vector2 p=区域格中心(x,y),d=p-点;float u=d.x*cos+d.y*sin,v=-d.x*sin+d.y*cos;
                    bool 内=类==随机场景障碍种类.断墙?Mathf.Abs(u)<=b.半径.x&&Mathf.Abs(v)<=b.半径.y:u*u/(b.半径.x*b.半径.x)+v*v/(b.半径.y*b.半径.y)<=1;
                    if(!内)continue;if(保留通道(p,转角)){有效=false;continue;}
                    if(通行区域[x,y]){通行区域[x,y]=false;变化.Add(new Vector2Int(x,y));}
                }
                候选.Add(b);if(!有效)break;
            }
            if(有效&&占地+变化.Count<=区域宽*区域高*地形参数("cover_fraction"))
            {
                new 天帝战斗寻路(this).取得连通节点(Vector2.zero,节点);可达.Clear();foreach(var p in 节点)可达.Add(p);
                foreach(var p in 基础连通)if(可站立(p)&&!可达.Contains(p)){有效=false;break;}
            }
            else 有效=false;
            if(!有效){foreach(var p in 变化)通行区域[p.x,p.y]=true;continue;}
            随机障碍数据.AddRange(候选);中心列.Add(中);占地+=变化.Count;
        }
        int 水数=rng.Next((int)地形参数("shallows_min"),(int)地形参数("shallows_max")+1);
        for(int i=0;i<地形参数("attempts")&&浅滩数据.Count<水数;i++)
        {
            Vector2 p=new Vector2(((float)rng.NextDouble()*2-1)*(半宽-7),((float)rng.NextDouble()*2-1)*(半高-7));
            float r=地形参数("shallow_radius_min")+(float)rng.NextDouble()*(地形参数("shallow_radius_max")-地形参数("shallow_radius_min"));
            if(p.magnitude<地形参数("spawn_safe")+r||!可站立(p,r))continue;
            bool 近=false;foreach(var 水 in 浅滩数据)if(Vector2.Distance(水.位置,p)<水.半径+r){近=true;break;}
            if(!近)浅滩数据.Add(new 战场浅滩{位置=p,半径=r});
        }
    }
}
