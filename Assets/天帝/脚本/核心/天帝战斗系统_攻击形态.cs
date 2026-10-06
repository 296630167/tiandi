using System;
using System.Collections.Generic;
using UnityEngine;

// 形态节点执行自己的几何命中，在完整结束时续接；不把每次接触当成新功能触发。
public sealed partial class 天帝战斗系统
{
    public sealed class 攻击形态演出
    {
        public 道纹功能 功能;
        public Vector2 起, 终;
        public float 半径, 剩余秒;
        public bool 预警;
    }
    readonly List<攻击形态演出> 形态演出 = new List<攻击形态演出>(256);
    public IReadOnlyList<攻击形态演出> 攻击形态效果 => 形态演出;
    static float 形(string k) => 天帝顺序道纹.形态数值(k);
    void 推进形态演出(float dt)
    { for (int i = 形态演出.Count - 1; i >= 0; i--) { 形态演出[i].剩余秒 -= dt; if (形态演出[i].剩余秒 <= 0) 形态演出.RemoveAt(i); } }
    void 留形态(道纹功能 f, Vector2 a, Vector2 b, float r, bool warning = false)
    {
        if (形态演出.Count >= 形("visuals_alive")) return;
        形态演出.Add(new 攻击形态演出 { 功能 = f, 起 = a, 终 = b, 半径 = r, 预警 = warning, 剩余秒 = 形("visual_seconds") });
    }
    void 初始化攻击形态(战斗灵矢 v)
    {
        var f = v.执行段.功能;
        v.自动连锁 = false; v.形态计时 = 0; v.形态步数 = 0;
        v.形态锚点 = v.位置; v.形态进入历史 = new HashSet<int>(v.命中过);
        v.形态命中时刻 = f == 道纹功能.飞轮 ? new Dictionary<int, float>() : null;
        if (f == 道纹功能.剑雨)
        {
            int t = 找目标(v.位置, 普攻参数.索敌距离, v.命中过);
            v.形态锚点 = t >= 0 ? 敌人数据[t].位置 : v.位置 + v.方向 * 4;
        }
        if (f == 道纹功能.旋刃)
        { v.形态锚点 = 玩家; v.位置 = 玩家 + v.方向 * 形("orbit_radius") * v.参数.体型倍率; }
        v.形态上一中心 = v.位置;
        if (f == 道纹功能.光束) v.剩余距离 = Mathf.Min(v.剩余距离, 形("beam_range"));
        if (f == 道纹功能.刃波) v.剩余距离 = Mathf.Min(v.剩余距离, 形("blade_range"));
        if (f == 道纹功能.飞轮) v.剩余距离 = Mathf.Min(v.剩余距离, 形("wheel_range"));
        if (f == 道纹功能.游龙) v.剩余距离 = Mathf.Min(v.剩余距离, 形("dragon_range") + 形("dragon_length") * v.参数.体型倍率);
        if (f == 道纹功能.灵网) v.剩余距离 = Mathf.Min(v.剩余距离, 形("net_range"));
    }
    Vector2 形态可达(Vector2 a, Vector2 b)
    {
        if (寻路.无遮挡(a, b)) return b;
        float lo = 0, hi = 1;
        for (int i = 0; i < 12; i++) { float m = (lo + hi) * .5f; if (寻路.无遮挡(a, Vector2.Lerp(a,b,m))) lo = m; else hi = m; }
        return Vector2.Lerp(a,b,Mathf.Max(0,lo-.001f));
    }
    static float 线段距离(Vector2 p, Vector2 a, Vector2 b)
    { var d=b-a; return Vector2.Distance(p, a + d * (d.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude) : 0)); }
    // repeat只用于剑雨分批和飞轮接触；祖先命中过的敌人始终排除。
    void 形态命中段(战斗灵矢 v, Vector2 a, Vector2 b, float radius, float factor, bool repeat = false)
    {
        int count=0;
        for(int i=0;i<敌人数据.Count && count<扩("hits_per_area");i++)
        {
            var e=敌人数据[i];
            if(!e.存活 || (repeat ? v.形态进入历史.Contains(i) : v.命中过.Contains(i))) continue;
            float er=e.布点.级别==战斗敌人级别.王级?1.25f:.6f;
            if(线段距离(e.位置,a,b)>radius+er || !寻路.无遮挡(a,e.位置))continue;
            if(v.形态命中时刻!=null && v.形态命中时刻.TryGetValue(i,out var last) && v.形态计时-last < 形("wheel_tick")-.00001f)continue;
            命中伤害(e,v,v.形态倍率*factor); v.命中过.Add(i); count++;
            if(v.形态命中时刻!=null)v.形态命中时刻[i]=v.形态计时;
        }
    }
    bool 结束攻击形态(战斗灵矢 v)
    {
        扩展功能触发次数++;
        // 单口或未连接的物理出口只结束形态，不凭空再射一枚普通弹。
        if(v.执行段.后续.Count==0 || v.执行段.后续[0].空出口)return false;
        return 扩展续接(v,-1,v.位置,true);
    }
    bool 推进攻击形态(战斗灵矢 v,float dt)
    {
        if(dt<=0)return true;
        var f=v.执行段.功能; float scale=v.参数.体型倍率;
        float oldtime=v.形态计时; v.形态计时+=dt;
        if(f==道纹功能.光束)
        {
            Vector2 end=形态可达(v.位置,v.位置+v.方向*v.剩余距离);
            v.已飞距离+=Vector2.Distance(v.位置,end);
            形态命中段(v,v.位置,end,形("beam_radius")*scale,形("beam_factor"));
            留形态(f,v.位置,end,形("beam_radius")*scale); v.位置=end;
            return 结束攻击形态(v);
        }
        if(f==道纹功能.地刺)
        {
            while(v.形态步数<形("spike_count") && v.形态计时+.00001f >= (v.形态步数+1)*形("spike_interval"))
            {
                Vector2 end=v.形态锚点+v.方向*(v.形态步数+1)*形("spike_spacing");
                if(!寻路.无遮挡(v.位置,end))return 结束攻击形态(v);
                v.形态步数++; v.位置=end; v.已飞距离=Vector2.Distance(v.形态锚点,end);
                形态命中段(v,end,end,形("spike_radius")*scale,形("spike_factor")); 留形态(f,end,end,形("spike_radius")*scale);
            }
            return v.形态步数<形("spike_count") || 结束攻击形态(v);
        }
        if(f==道纹功能.剑雨)
        {
            int n=(int)形("rain_count");
            for(int i=v.形态步数;i<n;i++)
            {
                float due=形("rain_delay")+i*形("rain_interval");
                Vector2 p=v.形态锚点+(i==0?Vector2.zero:转向(Vector2.right,(i-1)*360f/(n-1))*形("rain_spread"));
                留形态(f,p,p,形("rain_radius")*scale,true);
                if(v.形态计时+.00001f<due)break;
                v.形态步数++; v.位置=p;
                形态命中段(v,p,p,形("rain_radius")*scale,形("rain_factor"),true); 留形态(f,p,p,形("rain_radius")*scale);
            }
            if(v.形态步数<n)return true;
            return 结束攻击形态(v);
        }
        if(f==道纹功能.地雷)
        {
            留形态(f,v.位置,v.位置,形("mine_trigger_radius")*scale,v.形态计时<形("mine_arm_seconds"));
            // 超时先于触发，不允许长帧让已消散地雷重新生效。
            if(v.形态计时>=形("mine_life_seconds"))return false;
            if(v.形态计时<形("mine_arm_seconds"))return true;
            for(int i=0;i<敌人数据.Count;i++)
                if(敌人数据[i].存活 && !v.形态进入历史.Contains(i) && Vector2.Distance(敌人数据[i].位置,v.位置)<=形("mine_trigger_radius")*scale && 寻路.无遮挡(v.位置,敌人数据[i].位置))
                { 形态命中段(v,v.位置,v.位置,形("mine_blast_radius")*scale,形("mine_factor")); 留形态(f,v.位置,v.位置,形("mine_blast_radius")*scale); return 结束攻击形态(v); }
            return true;
        }
        if(f==道纹功能.旋刃 || f==道纹功能.灵鞭)
        {
            bool orbit=f==道纹功能.旋刃; float seconds=形(orbit?"orbit_seconds":"whip_seconds");
            float span=orbit?360:形("whip_degrees"), begin=orbit?0:-span*.5f;
            float until=Mathf.Min(seconds,v.形态计时);
            // 限制角度子步，避免低帧率漏过扫过的敌人；位置采样同时覆盖玩家移动。
            int steps=Mathf.Clamp(Mathf.CeilToInt((until-oldtime)/seconds*span/8),1,(int)形("visual_segments_max"));
            Vector2 center=orbit?玩家:v.形态锚点, last=v.形态上一中心;
            for(int i=1;i<=steps;i++)
            {
                float t=Mathf.Lerp(oldtime,until,i/(float)steps);
                Vector2 c=Vector2.Lerp(last,center,i/(float)steps);
                Vector2 tip=c+转向(v.方向,begin+span*t/seconds)*形(orbit?"orbit_radius":"whip_range")*scale;
                if(orbit) { 形态命中段(v,v.位置,tip,形("orbit_blade_radius")*scale,形("orbit_factor")); 留形态(f,v.位置,tip,形("orbit_blade_radius")*scale); }
                else { Vector2 valid=形态可达(c,tip); 形态命中段(v,c,valid,形("whip_radius")*scale,形("whip_factor")); 留形态(f,c,valid,形("whip_radius")*scale); tip=valid; }
                v.位置=tip;
            }
            v.形态上一中心=center;
            return v.形态计时<seconds-.00001f || 结束攻击形态(v);
        }
        // 刃波、飞轮、游龙和灵网持续推进，同敌去重或按飞轮接触间隔结算。
        Vector2 old=v.位置; float speed=v.参数.弹速*(f==道纹功能.飞轮?形("wheel_speed_factor"):1);
        float dist=Mathf.Min(speed*dt,v.剩余距离); Vector2 want=扩展飞行位置(v,dist,dt), next=形态可达(old,want);
        bool wall=Vector2.Distance(next,want)>.001f;
        v.位置=next; v.剩余距离-=dist; 登记扩展飞行(v,old);
        Vector2 side=new Vector2(-v.方向.y,v.方向.x);
        if(f==道纹功能.飞轮)
        { 形态命中段(v,old,next,形("wheel_radius")*scale,形("wheel_factor"),true); 留形态(f,next,next,形("wheel_radius")*scale); }
        else if(f==道纹功能.游龙)
        {
            Vector2 tail=形态可达(next,next-v.方向*Mathf.Min(v.已飞距离,形("dragon_length")*scale));
            形态命中段(v,old,next,形("dragon_radius")*scale,形("dragon_factor")); 形态命中段(v,tail,next,形("dragon_radius")*scale,形("dragon_factor"));
            留形态(f,tail,next,形("dragon_radius")*scale);
        }
        else
        {
            float half=scale*(f==道纹功能.刃波?形("blade_half_width"):形("net_half_width")*Mathf.Clamp01(v.已飞距离/形("net_range")));
            int slices=Mathf.Clamp(Mathf.CeilToInt(dist/.2f),1,(int)形("visual_segments_max"));
            for(int i=0;i<=slices;i++)
            { Vector2 p=Vector2.Lerp(old,next,i/(float)slices); Vector2 a=形态可达(p,p-side*half),b=形态可达(p,p+side*half); 形态命中段(v,a,b,.12f*scale,形(f==道纹功能.刃波?"blade_factor":"net_factor")); }
            留形态(f,形态可达(next,next-side*half),形态可达(next,next+side*half),.12f*scale);
        }
        return !wall && v.剩余距离>.00001f || 结束攻击形态(v);
    }
}
