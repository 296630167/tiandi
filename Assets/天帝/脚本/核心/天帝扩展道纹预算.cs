using System;
using System.Collections.Generic;

// 密集目标纸面对照：蓄势按6米、拖尾按1处完整4跳，弹墙假设成功反弹。
// 这不是自动命中承诺；实战仍按轨迹、预警、障碍、控制和存活目标结算。
public static class 天帝扩展道纹预算
{
    public static double 计算(道纹执行计划 计划, int 目标数, int 起始数, bool 暴击)
    {
        var 队 = new Queue<(道纹执行段 段, int 余, double 倍, bool 已命中)>(); int 生成 = 0; double 总 = 0;
        double 取(string k) => 天帝顺序道纹.扩展数值(k);
        double 形(string k) => 天帝顺序道纹.形态数值(k);
        double 伤(道纹执行段 s)
        {
            var p = s.参数;
            return p.伤害 * (暴击 ? 1 + p.暴击率 * (p.暴击倍率 - 1) : 1) *
                Math.Min(取("charge_max"), 1 + (s.功能==道纹功能.剑雨||s.功能==道纹功能.旋刃||s.功能==道纹功能.灵鞭||s.功能==道纹功能.地雷?0:取("paper_travel_distance")) * 取("charge_per_meter") * 天帝顺序道纹.运动层(p.运动功能, 道纹功能.蓄势));
        }
        void 发(道纹执行段 s, int 余, double 倍)
        {
            if (生成 >= 天帝数值.取("rune.ordered_functions.projectiles_per_release")) return;
            if (天帝顺序道纹.即时功能(s.功能))
            {
                int n = 天帝顺序道纹.多弹功能(s.功能) ? 天帝顺序道纹.多弹数量(s.功能) : 1;
                for (int i=0;i<n;i++) foreach (var 下 in s.后续) 发(下,余,倍*(天帝顺序道纹.多弹功能(s.功能)?天帝顺序道纹.多弹倍率(s.功能):1));
                return;
            }
            for (int i=0;i<s.参数.数量 && 生成<天帝数值.取("rune.ordered_functions.projectiles_per_release");i++)
            { 生成++; 队.Enqueue((s,余,倍,false)); }
        }
        void 续(道纹执行段 s, int 余, double 倍, bool 新飞行)
        {
            if(s.后续.Count==0)return;
            if(天帝顺序道纹.攻击形态(s.功能) && s.后续[0].空出口)return;
            var 下=s.后续[0];
            while(天帝顺序道纹.即时功能(下.功能)&&下.功能!=道纹功能.齐射&&!天帝顺序道纹.多弹功能(下.功能))
            { if(下.后续.Count==0)return; 下=下.后续[0]; }
            bool 新 = 新飞行 || 下.功能==道纹功能.齐射 || 天帝顺序道纹.多弹功能(下.功能) ||
                下.功能==道纹功能.延时 || 下.功能==道纹功能.跃迁 || 下.功能==道纹功能.陨落 || 下.功能==道纹功能.折返 || 下.功能==道纹功能.弹墙 || 天帝顺序道纹.攻击形态(下.功能);
            if(下.功能==道纹功能.齐射 || 天帝顺序道纹.多弹功能(下.功能)) 发(下,余,倍);
            else if(新 || 下.功能==道纹功能.穿透) 队.Enqueue((下,余,倍,false));
            else if(下.功能!=道纹功能.旧版) 队.Enqueue((下,余,倍,true));
        }
        for(int i=0;i<起始数;i++)发(计划.起点,Math.Max(0,目标数),1);
        while(队.Count>0)
        {
            var x=队.Dequeue(); var s=x.段; var f=s.功能; int 余=x.余; double 单=伤(s)*x.倍;
            if(天帝顺序道纹.攻击形态(f))
            {
                string key=f==道纹功能.光束?"beam":f==道纹功能.刃波?"blade":f==道纹功能.地刺?"spike":f==道纹功能.剑雨?"rain":f==道纹功能.旋刃?"orbit":f==道纹功能.灵鞭?"whip":f==道纹功能.飞轮?"wheel":f==道纹功能.游龙?"dragon":f==道纹功能.灵网?"net":"mine";
                int n=Math.Min(余,(int)取("hits_per_area"));
                double ticks=f==道纹功能.剑雨?形("rain_count"):f==道纹功能.飞轮?形("paper_wheel_hits"):1;
                总+=n*单*形(key+"_factor")*ticks;
                续(s,余-n,x.倍,true); continue;
            }
            if(f==道纹功能.延时||f==道纹功能.跃迁||f==道纹功能.弹墙) { 续(s,余,x.倍,true); continue; }
            if(f==道纹功能.陨落) { int n=Math.Min(余,(int)取("hits_per_area")); 总+=n*单*取("fall_factor"); 续(s,余-n,x.倍,false); continue; }
            if(!x.已命中)
            {
                if(余<=0)continue;
                总+=单; 余--;
                int 拖尾=天帝顺序道纹.运动层(s.参数.运动功能,道纹功能.拖尾);
                总+=单*拖尾*取("trail_factor")*Math.Floor(取("trail_seconds")/取("trail_tick")+.00001)*取("paper_trail_zones");
            }
            if(f==道纹功能.分裂||f==道纹功能.连锁)
            { foreach(var 下 in s.后续)发(下,余,x.倍*天帝数值.取(f==道纹功能.分裂?"rune.ordered_functions.split_factor":"rune.ordered_functions.chain_factor")); }
            else if(f==道纹功能.穿透)
            { int n=Math.Min(余,(int)天帝数值.取("rune.ordered_functions.pierce_count")); 总+=n*单; if(n==天帝数值.取("rune.ordered_functions.pierce_count"))续(s,余-n,x.倍,false); }
            else if(f==道纹功能.折返) { 总+=余*单; 续(s,0,x.倍,true); }
            else if(f==道纹功能.停驻)续(s,余,x.倍,true);
            else if(f==道纹功能.爆破||f==道纹功能.震荡||f==道纹功能.烙印)
            {
                bool 重击=f!=道纹功能.爆破; int n=Math.Min(余,(int)取("hits_per_area")-(重击?1:0));
                总+=(n+(重击?1:0))*单*取(f==道纹功能.爆破?"blast_factor":f==道纹功能.震荡?"pulse_factor":"mark_factor");
                续(s,余-n,x.倍,false);
            }
            else if(f==道纹功能.击退||f==道纹功能.牵引||f==道纹功能.束缚)续(s,余,x.倍,false);
            else
            {
                for(int i=1;i<=Math.Min(余,s.参数.连锁);i++)总+=单*Math.Pow(天帝数值.取("shape.chain_factor"),i);
                for(int i=0;i<s.参数.分裂;i++)总+=单*天帝数值.取("shape.split_factor");
            }
        }
        return 总;
    }
}
