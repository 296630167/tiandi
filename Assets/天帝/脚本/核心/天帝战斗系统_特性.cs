using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 集中推进38种特性；施放、命中历史和资源预算独立于普通攻击，特性不触发自身。
public sealed partial class 天帝战斗系统
{
    internal sealed class 特性运行
    {
        public 特性激活结果 定义;
        public float 冷却,持续,移动,站稳,蓄伤,反震,回复秒;
        public int 根次数;
        public bool 蓄满;
    }
    public sealed class 特性战斗效果
    {
        public int 特性,实例;
        public Vector2 位置,起点,方向;
        public float 半径,剩余秒,血量;
        internal float 召唤盾,召唤盾到期;
        internal int 召唤盾所属;
        public bool 召唤,诱饵,土垒,退化,护体;
        public float 覆盖角度;
        internal bool 灼印附着;
        internal float 回转开始;
        internal Vector2 发射方向;
        internal float 已过,下次,总时长;
        internal int 跳数,目标=-1;
        internal double 伤害,暴击=1;
        internal 特性运行 运行;
        internal HashSet<int> 命中=new HashSet<int>();
        internal 特性施放记录 释放=new 特性施放记录();
        internal bool 仅演出;
        internal bool 次级,已衍生;
        internal int 分段数量=1;
        internal 特性战斗效果 复制()=> (特性战斗效果)MemberwiseClone();
        internal Dictionary<int,bool> 环内=new Dictionary<int,bool>();
    }
    internal sealed class 特性施放记录
    {
        public bool 已恢复;
        public readonly Dictionary<int,HashSet<int>> 跳历史=new Dictionary<int,HashSet<int>>();
    }
    readonly Dictionary<int,特性运行> 特性运行表=new Dictionary<int,特性运行>();
    readonly List<特性战斗效果> 特性效果=new List<特性战斗效果>(128);
    readonly List<(float 量,float 到期,int 所属)> 特性盾=new List<(float,float,int)>();
    public IReadOnlyList<特性战斗效果> 特性效果列表=>特性效果;
    public float 特性临时护盾=>特性盾.Sum(x=>x.量);
    float 特性盾剩余容量=>Mathf.Max(0,主角.最大血量*限("shield_capacity")-特性临时护盾-特性效果.Sum(e=>e.召唤盾));
    public int 特性施放次数 {get;private set;}
    float 特性时钟,特性静止秒,特性未伤秒,治疗额度,灵力额度,护盾额度;
    Vector2 特性上次玩家;
    int 特性修订=-1;
    static float 特(string k)=>(float)天帝特性道纹.取("rules."+k);
    static float 限(string k)=>(float)天帝特性道纹.取("limits."+k);
    double u=>天帝数值.成长(主角.等级);
    static int 种(特性运行 s)=>s.定义.道纹.特性编号;
    static float 强(特性运行 s)=>(float)天帝特性道纹.品阶值(s.定义.道纹,"效果");
    static float 久(特性运行 s)=>(float)天帝特性道纹.品阶值(s.定义.道纹,"持续");
    static float 百分(特性运行 s)=>1+(强(s)-1)*.5f;
    float 周期(特性运行 s)=>(float)天帝特性道纹.数值(种(s),"周期")*(float)天帝特性道纹.品阶值(s.定义.道纹,"周期")/(1+主角.技能急速);
    static float 基持续(特性运行 s)=>(float)天帝特性道纹.数值(种(s),"持续")*久(s);
    static double 下(特性运行 s,道纹属性 a)=>s.定义.下游[(int)a];
    float 半径(特性运行 s,float 基)=>Mathf.Min(特("shape_radius_max"),基+(float)Math.Min(特("shape_radius_max"),下(s,道纹属性.范围))*.35f);
    int 数量(特性运行 s)=>Mathf.Min((int)特("shape_quantity_max"),1+(int)下(s,道纹属性.数量));
    static bool 支持(特性运行 s,string a)
    {int id=种(s);for(int i=0;i<天帝特性道纹.数值(id,"support_count");i++)if(天帝特性道纹.文本(id,"支持."+i)==a)return true;return false;}
    bool 特性免控=>特性运行表.Values.Any(s=>种(s)==28&&s.持续>0);
    void 同步特性()
    {
        if(特性修订==道纹.修订号)return;
        特性修订=道纹.修订号;var live=new HashSet<int>();
        foreach(var a in 道纹.特性视图.生效)
        {
            live.Add(a.道纹.编号);
            if(!特性运行表.TryGetValue(a.道纹.编号,out var state))
                特性运行表[a.道纹.编号]=state=new 特性运行{定义=a,冷却=(float)天帝特性道纹.数值(a.道纹.特性编号,"周期")};
            state.定义=a;
        }
        foreach(var key in 特性运行表.Keys.Where(k=>!live.Contains(k)).ToArray())特性运行表.Remove(key);
        特性效果.RemoveAll(x=>!live.Contains(x.实例));
        特性盾.RemoveAll(x=>!live.Contains(x.所属));
        foreach(var e in 特性效果)if(!live.Contains(e.召唤盾所属))e.召唤盾=0;
    }
    void 推进特性战斗(float dt)
    {
        同步特性();if(dt<=0)return;
        float moved=Vector2.Distance(玩家,特性上次玩家);特性上次玩家=玩家;
        特性时钟+=dt;特性未伤秒+=dt;特性静止秒=moved>.001f?0:特性静止秒+dt;
        治疗额度=Mathf.Min(主角.最大血量*限("heal_hp_per_second"),治疗额度+主角.最大血量*限("heal_hp_per_second")*dt);
        灵力额度=Mathf.Min(主角.最大灵力*限("heal_mp_per_second"),灵力额度+主角.最大灵力*限("heal_mp_per_second")*dt);
        护盾额度=Mathf.Min(主角.最大血量*限("shield_per_second"),护盾额度+主角.最大血量*限("shield_per_second")*dt);
        特性盾.RemoveAll(x=>x.到期<=特性时钟||x.量<=0);
        foreach(var e in 敌人数据)
        {
            e.特性减速秒=Mathf.Max(0,e.特性减速秒-dt);e.特性弱化秒=Mathf.Max(0,e.特性弱化秒-dt);e.特性易伤秒=Mathf.Max(0,e.特性易伤秒-dt);
            e.特性控制窗口+=dt;if(e.特性控制窗口>=限("control_window")){e.特性控制窗口=0;e.特性控制累计=0;}
        }
        float move=0,haste=0,aps=0,evasion=0;
        foreach(var s in 特性运行表.Values.ToArray())
        {
            int id=种(s);s.冷却=Mathf.Max(0,s.冷却-dt);s.持续=Mathf.Max(0,s.持续-dt);s.移动+=moved;
            if(id==12){if(moved>.001f){s.站稳=0;s.蓄满=false;}else if(s.冷却<=0){s.站稳+=dt;if(s.站稳>=特("steady_seconds"))s.蓄满=true;}}
            if(id==21){if(moved>.001f){s.站稳=0;s.持续=0;}else if(s.持续<=0){s.站稳+=dt;if(s.站稳>=特("steady_seconds")){s.持续=基持续(s);s.站稳=0;}}}
            if(id==18)
            {if(特性未伤秒<特("recover_wait")){s.回复秒=0;}else if(s.回复秒<特("recover_seconds")*久(s)){float secs=Mathf.Min(dt,特("recover_seconds")*久(s)-s.回复秒);治疗(主角.最大血量*特("recover_rate")*百分(s)*secs,0);s.回复秒+=secs;}}
            if(id==25 && s.持续>0)aps+=特("rapid_aps")*百分(s);
            if(id==28 && s.持续>0)evasion+=特("immunity_evasion")*百分(s);
            if(id==14 && s.移动>=特("decoy_distance")&&s.冷却<=0){s.移动=0;施放特性(s);}
            if(id==27 && s.移动>=特("water_distance")&&s.冷却<=0){s.移动=0;施放特性(s);}
            if(id==28 && s.移动>=特("immunity_distance")&&s.冷却<=0){s.移动=0;施放特性(s);}
            if(id==22 && s.反震>0 && s.冷却<=0){施放特性(s);}
            if(new[]{1,2,3,4,5,6,7,8,9,10,11,16,19,20,23,25,29,30,31,32,33,34,35,36,37,38}.Contains(id)&&s.冷却<=0)施放特性(s);
        }
        主角.设置特性增益(move,haste,aps,evasion);
        推进特性效果(dt);
    }
    void 治疗(float hp,float mp)
    {
        if(玩家死亡)return;
        float h=Mathf.Min(Mathf.Max(0,hp),治疗额度,主角.最大血量-主角.当前血量),m=Mathf.Min(Mathf.Max(0,mp),灵力额度,主角.最大灵力-主角.当前灵力);
        治疗额度-=h;灵力额度-=m;主角.设置当前资源(主角.当前血量+h,主角.当前灵力+m,主角.当前灵气护盾);
    }
    void 授盾(float amount,float seconds,int owner)
    {
        if(玩家死亡)return;
        float value=Mathf.Min(Mathf.Max(0,amount),护盾额度,特性盾剩余容量);
        if(value<=0)return;护盾额度-=value;特性盾.Add((value,特性时钟+seconds,owner));
    }
    float 特性受伤前(float damage,bool direct,战斗敌人 source)
    {
        同步特性();float reduction=0;
        foreach(var s in 特性运行表.Values)
        {
            int id=种(s);
            if(id==21&&s.持续>0)reduction+=特("armor_mitigation")*百分(s);
            if(id==2&&s.持续>0&&direct)
            {reduction+=特("guard_mitigation")*百分(s);s.持续=0;if(source!=null)特性伤害(s,source,天帝特性道纹.数值(id,"伤害")*u*强(s),1);}
            if(id==36&&s.持续>0&&direct&&source!=null)
            {float turn=特性时钟*(float)Math.Min(特("shape_arc_max"),下(s,道纹属性.弧度));int n=数量(s);for(int i=0;i<n;i++)if(Vector2.Angle(转向(Vector2.right,turn+i*360f/n),source.位置-玩家)<=特("spin_degrees")/n*.5f){reduction+=特("spin_mitigation")*百分(s);s.持续=0;break;}}
        }
        if(特性效果.Any(e=>e.特性==37&&Vector2.Distance(e.位置,玩家)<=e.半径))reduction+=特("shelter_mitigation");
        damage*=1-Mathf.Min(限("mitigation"),reduction);
        for(int i=0;i<特性盾.Count&&damage>0;i++)
        {var x=特性盾[i];float cost=Mathf.Min(damage,x.量);damage-=cost;特性盾[i]=(x.量-cost,x.到期,x.所属);}
        return damage;
    }
    void 特性受伤后(float hpLoss,float shieldLoss,bool broken)
    {
        if(hpLoss+shieldLoss<=0)return;特性未伤秒=0;
        foreach(var s in 特性运行表.Values.ToArray())
        {
            int id=种(s);
            if(id==17){s.蓄伤+=hpLoss;if(s.蓄伤>=主角.最大血量*特("blood_threshold")&&s.冷却<=0){s.蓄伤=0;施放特性(s);}}
            if(id==22)s.反震=Mathf.Min((float)(特("reflect_max_g")*u*强(s)),s.反震+(hpLoss+shieldLoss)*特("reflect_store"));
            if(id==24&&broken&&s.冷却<=0)施放特性(s);
        }
    }
    void 特性根释放(int route)
    {
        同步特性();foreach(var s in 特性运行表.Values)if(种(s)==26&&s.定义.通路==route)
        {s.根次数++;if(s.根次数>=特("shield_cast_count")&&s.冷却<=0){s.根次数=0;授盾((float)(特("shield_on_cast_g")*u*强(s)),基持续(s),s.定义.道纹.编号);s.冷却=特("shield_cast_period");特性施放次数++;}}
    }
    void 特性根命中(战斗敌人 enemy,int route)
    {
        foreach(var s in 特性运行表.Values.ToArray())if(种(s)==12&&s.定义.通路==route&&s.蓄满)
        {s.蓄满=false;s.站稳=0;s.冷却=周期(s);特性伤害(s,enemy,特("rest_bonus_g")*u*强(s),1);特性施放次数++;}
    }
    bool 施放特性(特性运行 s)
    {
        int id=种(s),target=找目标(玩家,特("attack_range"),null);
        bool offensive=new[]{1,3,4,5,7,8,9,10,11,16,20,29,30,31,32,34,35,38}.Contains(id);
        if(offensive&&target<0)return false;
        if(id==20 && 主角.当前灵力<主角.最大灵力*特("burst_mp_cost"))return false;
        if(id==20)主角.设置当前资源(主角.当前血量,主角.当前灵力-主角.最大灵力*特("burst_mp_cost"),主角.当前灵气护盾);
        s.冷却=周期(s);s.持续=基持续(s);特性施放次数++;
        if(id==25||id==28)return true;
        if(id==23){授范围盾(s,特("shield_g"),基持续(s));return true;}
        if(id==19){治疗(0,主角.最大灵力*特("spring_mp")*百分(s));授范围盾(s,特("spring_shield_g"),特("spring_shield_seconds")*久(s));return true;}
        if(id==33){授范围盾(s,特("harmony_shield_g"),基持续(s));return true;}
        if(id==16)
        {var chosen=敌人数据.Where(e=>e.存活&&Vector2.Distance(e.位置,玩家)<=半径(s,特("attack_range"))).OrderByDescending(e=>e.布点.级别).ThenByDescending(e=>e.最大血量).Take(Mathf.Min((int)特("shape_quantity_max"),数量(s)+(int)下(s,道纹属性.连锁)));foreach(var e in chosen){e.特性易伤=Mathf.Max(e.特性易伤秒>0?e.特性易伤:0,特("mark_vulnerability")*百分(s));e.特性易伤秒=基持续(s);}return true;}
        if(特性效果.Count>=限("effects_alive"))return true;
        Vector2 point=target>=0?敌人数据[target].位置:玩家,dir=(point-玩家).sqrMagnitude>.001f?(point-玩家).normalized:Vector2.right;
        var effect=new 特性战斗效果{特性=id,实例=s.定义.道纹.编号,位置=玩家,起点=玩家,方向=dir,运行=s,目标=target,剩余秒=基持续(s),总时长=基持续(s),暴击=战斗随机.NextDouble()<主角.暴击率?主角.暴击倍率:1,伤害=天帝特性道纹.数值(id,"伤害")*u*强(s),半径=半径(s,特("effect_hit_radius"))};
        if(id==14)
        {effect.诱饵=true;effect.半径=半径(s,特("decoy_radius"));effect.剩余秒=特("decoy_seconds")*久(s);while(特性效果.Count(e=>e.诱饵)>=限("decoys_max"))特性效果.Remove(特性效果.First(e=>e.诱饵));}
        if(id==4||id==29)
        {effect.召唤=true;effect.血量=(float)(主角.最大血量*.1);effect.伤害=天帝特性道纹.数值(id,"召唤总伤")*u*强(s);effect.下次=effect.总时长/特("summon_hits");while(特性效果.Count(e=>e.特性==id)>=限("summons_per_family"))特性效果.Remove(特性效果.First(e=>e.特性==id));}
        if(id==10)
        {effect.土垒=true;effect.位置=形态可达(玩家,玩家+dir*1.5f);effect.半径=特("wall_length")*久(s)*.5f;effect.血量=(float)(特("wall_hp_g")*u*强(s));Vector2 side=new Vector2(-dir.y,dir.x);effect.退化=!地图.可站立(effect.位置+side*(effect.半径+.8f))&&!地图.可站立(effect.位置-side*(effect.半径+.8f));}
        if(id==6){effect.剩余秒=基持续(s);effect.下次=0;}
        if(id==7)
        {effect.剩余秒=特("attack_range")/特("projectile_speed");effect.伤害=天帝特性道纹.数值(id,"持续伤")*u*强(s);}
        if(id==2||id==36){effect.护体=true;effect.半径=半径(s,特("effect_hit_radius"));}
        if(id==8){effect.位置=point;effect.下次=特("meteor_delay");effect.剩余秒=特("meteor_delay")+特("magma_seconds")*久(s);effect.半径=半径(s,特("magma_radius"));}
        if(id==9||id==38){effect.位置=point;effect.半径=半径(s,特(id==9?"trap_radius":"hunt_radius"));effect.下次=0;}
        if(id==34)effect.位置=point;
        if(id==37){effect.半径=半径(s,特("shelter_radius"));effect.下次=0;}
        if(id==27){effect.方向=dir;effect.半径=半径(s,.4f);effect.剩余秒=基持续(s);}
        if(id==22){effect.伤害=s.反震;s.反震=0;}
        if(new[]{17,20,22,24,34}.Contains(id))effect.半径=半径(s,特("outburst_radius"));
        if(id==30)effect.伤害=(特("heavy_base_g")+特("heavy_quantity_factor")*Math.Min(特("shape_quantity_max")-1,下(s,道纹属性.数量)))*u*强(s);
        if(new[]{1,3,5,11,30,31,32,35}.Contains(id)){effect.剩余秒=特(id==11?"shock_range":"attack_range")/特("projectile_speed");if(id==35){effect.回转开始=effect.剩余秒;effect.剩余秒=effect.剩余秒*2+360/Mathf.Max(1,(float)Math.Min(特("shape_arc_max"),下(s,道纹属性.弧度)));}effect.总时长=effect.剩余秒;}
        if(new[]{17,20,22,24,34}.Contains(id))effect.剩余秒=Mathf.Max(.1f,effect.剩余秒);
        添加特性覆盖(effect,point);return true;
    }
    void 授范围盾(特性运行 s,float coefficient,float seconds)
    {
        var allies=特性效果.Where(e=>e.召唤&&Vector2.Distance(e.位置,玩家)<=半径(s,特("shelter_radius"))).ToArray();
        float total=Mathf.Min((float)(coefficient*u*强(s)),护盾额度,特性盾剩余容量),each=total/(1+allies.Length);
        授盾(each,seconds,s.定义.道纹.编号);
        foreach(var e in allies){float value=Mathf.Min(each,护盾额度);护盾额度-=value;e.召唤盾=Mathf.Max(e.召唤盾,value);e.召唤盾到期=特性时钟+seconds;e.召唤盾所属=s.定义.道纹.编号;}
    }
    void 特性跳(特性战斗效果 e,int tick)
    {if(!e.释放.跳历史.TryGetValue(tick,out var history))e.释放.跳历史[tick]=history=new HashSet<int>();e.命中=history;e.已衍生=false;}
    void 添加特性覆盖(特性战斗效果 prototype,Vector2 target)
    {
        var s=prototype.运行;int id=prototype.特性,n=支持(s,"数量")?数量(s):1;
        if(id==30)n=1; // 凝矢将数量集中到一矢，不再同时扩散。
        if(prototype.召唤)n=Mathf.Min(n,(int)限("summons_per_family"));
        if(prototype.诱饵)n=Mathf.Min(n,(int)限("decoys_max"));
        var assigned=new HashSet<int>();
        for(int i=0;i<n&&特性效果.Count<限("effects_alive");i++)
        {
            var e=prototype.复制();e.环内=new Dictionary<int,bool>();特性跳(e,0);e.分段数量=n;
            float spread=n<=1?0:(i-(n-1)*.5f)*Mathf.Min(60,180f/n);
            if(支持(s,"弧度"))spread+=n<=1?0:(i-(n-1)*.5f)*Mathf.Min(特("shape_arc_max"),(float)下(s,道纹属性.弧度))/Mathf.Max(1,n-1);
            e.方向=转向(prototype.方向,spread);
            if(new[]{1,3,5,7,11,31,32,35}.Contains(id))
            {int t=找目标(玩家,特("attack_range"),assigned);if(t>=0){assigned.Add(t);e.目标=t;e.方向=(敌人数据[t].位置-玩家).normalized;}}
            if(id==8||id==9||id==38)
            {int t=找目标(玩家,半径(s,特("attack_range")),assigned);if(t<0){if(i>0)break;}else{assigned.Add(t);e.目标=t;e.位置=敌人数据[t].位置;}}
            else if(e.召唤||e.诱饵){e.位置=形态可达(玩家,玩家+转向(Vector2.right,i*360f/n)*.8f);}
            else if(e.土垒)
            {Vector2 side=new Vector2(-prototype.方向.y,prototype.方向.x);e.半径=prototype.半径/n;e.位置=prototype.位置+side*((i+.5f)/n*2-1)*prototype.半径;e.血量=prototype.血量/n;}
            if(e.召唤){e.伤害=prototype.伤害/n;e.血量=prototype.血量/n;while(特性效果.Count(x=>x.特性==id)>=限("summons_per_family"))特性效果.Remove(特性效果.First(x=>x.特性==id));}
            if(e.诱饵){while(特性效果.Count(x=>x.诱饵)>=限("decoys_max"))特性效果.Remove(特性效果.First(x=>x.诱饵));}
            if(id==6||id==37)e.仅演出=i>0;
            if(e.护体){e.方向=转向(Vector2.right,i*360f/n);e.覆盖角度=特("spin_degrees")/n;}
            if(id==27){e.位置=prototype.位置-prototype.方向*(i*特("water_length")/n);}
            if(e.土垒)e.退化=土垒封路(e);
            e.发射方向=e.方向;
            特性效果.Add(e);
        }
    }
    Vector2 回旋位置(特性战斗效果 e)
    {
        float outbound=e.回转开始,rate=Mathf.Max(1,(float)Math.Min(特("shape_arc_max"),下(e.运行,道纹属性.弧度))),turn=180/rate,t=e.已过;
        Vector2 d=e.发射方向,side=new Vector2(-d.y,d.x);float radius=e.半径,speed=特("projectile_speed");
        if(t<=outbound){e.方向=d;return e.起点+d*(speed*t);}
        if(t<=outbound+turn){float a=(t-outbound)*rate*Mathf.Deg2Rad;e.方向=d*Mathf.Cos(a)+side*Mathf.Sin(a);return e.起点+d*(speed*outbound+radius*Mathf.Sin(a))+side*(radius*(1-Mathf.Cos(a)));}
        if(t<=outbound*2+turn){e.方向=-d;return e.起点+d*(speed*(outbound*2+turn-t))+side*(radius*2);}
        float b=Mathf.Min(180,(t-outbound*2-turn)*rate)*Mathf.Deg2Rad;e.方向=-d*Mathf.Cos(b)-side*Mathf.Sin(b);return e.起点-d*(radius*Mathf.Sin(b))+side*(radius*(1+Mathf.Cos(b)));
    }
    void 附着灼印(特性战斗效果 fire,int target)
    {
        var prior=特性效果.FirstOrDefault(x=>x.特性==7&&x.灼印附着&&x.目标==target);
        float seconds=基持续(fire.运行);
        if(prior!=null){prior.剩余秒=Mathf.Max(prior.剩余秒,seconds);prior.伤害=Math.Max(prior.伤害,fire.伤害);return;}
        if(特性效果.Count>=限("effects_alive"))return;
        var dot=fire.复制();dot.灼印附着=true;dot.目标=target;dot.位置=敌人数据[target].位置;dot.已过=0;dot.跳数=0;dot.下次=特("effect_tick");dot.剩余秒=dot.总时长=seconds;dot.次级=false;
        dot.释放=fire.释放;特性跳(dot,0);特性效果.Add(dot);
    }
    void 特性伤害(特性运行 s,战斗敌人 enemy,double amount,double crit)
    {
        if(amount<=0||!enemy.存活)return;string a=天帝特性道纹.文本(种(s),"属性");int elem=Array.IndexOf(new[]{"金","木","水","火","土"},a);
        if(种(s)==34)elem=1;if(种(s)==35||种(s)==31)elem=0;if(种(s)==38)elem=4;
        var src=new 五行伤害分量(elem==0?amount:0,elem==1?amount:0,elem==2?amount:0,elem==3?amount:0,elem==4?amount:0);
        伤害敌人(enemy,new 战斗伤害包(elem<0?amount:0,elem<0?0:amount,主角.等级,1,1,1,crit,src));
    }
    void 特性减速(战斗敌人 e,float value,float seconds)
    {e.特性减速=Mathf.Max(e.特性减速秒>0?e.特性减速:0,value);e.特性减速秒=Mathf.Max(e.特性减速秒,seconds);}
    void 特性控制(战斗敌人 e,float seconds)
    {
        int rank=(int)e.布点.级别;float requested=seconds*(float)天帝特性道纹.取("limits.control_strength."+rank);
        float allowed=Mathf.Min(requested,Mathf.Max(0,(float)天帝特性道纹.取("limits.control_budget."+rank)-e.特性控制累计));
        e.特性控制累计+=allowed;e.束缚剩余秒=Mathf.Max(e.束缚剩余秒,allowed);
        if(allowed<requested)特性减速(e,限("control_fallback_slow"),requested);
    }
    void 特性作用(特性战斗效果 effect,int index,double scale=1)
    {
        var s=effect.运行;var e=敌人数据[index];int id=effect.特性;
        if(!e.存活||effect.命中.Count>=限("targets_per_cast")||!effect.命中.Add(index))return;
        特性伤害(s,e,effect.伤害*scale,effect.暴击);
        if(id==3)特性控制(e,特("tendril_bind")*久(s)*(float)scale);
        if(id==11)特性控制(e,特("shock_bind")*久(s)*(float)scale);
        if(id==38)特性控制(e,特("hunt_bind")*久(s)*(float)scale);
        if(id==5){if(e.布点.级别!=战斗敌人级别.王级)功能位移(e,effect.方向,特("wave_push"));特性减速(e,特("wave_slow"),特("wave_slow_seconds")*久(s));}
        if(id==34)特性减速(e,特("bind_slow"),基持续(s));
        if(id==24){e.特性弱化=Mathf.Min(限("enemy_weaken"),Mathf.Max(e.特性弱化秒>0?e.特性弱化:0,特("enemy_weaken")*百分(s)));e.特性弱化秒=基持续(s);}
    }
    void 特性次级(特性战斗效果 effect,int primary)
    {
        if(effect.次级||effect.已衍生)return;effect.已衍生=true;
        var s=effect.运行;int id=effect.特性;
        bool Supports(string a)=>支持(s,a);
        int chain=Supports("连锁")?Math.Min((int)特("shape_chain_max"),(int)下(s,道纹属性.连锁)):0;
        int split=Supports("分裂")?Math.Min((int)特("shape_split_max"),(id==31?1:0)+(int)下(s,道纹属性.分裂)):0;
        int count=0,last=primary;
        for(int i=0;i<chain;i++){int next=找目标(敌人数据[last].位置,3,effect.命中);if(next<0)break;留电弧(敌人数据[last].位置,敌人数据[next].位置);特性作用(effect,next,Math.Pow(特("shape_chain_factor"),i+1));last=next;count++;}
        for(int i=0;i<敌人数据.Count&&count<限("targets_per_cast");i++)
        {var e=敌人数据[i];if(!e.存活||effect.命中.Contains(i)||!寻路.无遮挡(敌人数据[primary].位置,e.位置))continue;float dist=Vector2.Distance(敌人数据[primary].位置,e.位置);if(split>0&&dist<=(float)天帝数值.取("shape.chain_range"))
            {if(特性效果.Count<限("effects_alive")){var shard=effect.复制();shard.次级=true;shard.已衍生=true;shard.召唤=shard.诱饵=shard.土垒=false;shard.仅演出=false;shard.位置=shard.起点=敌人数据[primary].位置;shard.方向=(e.位置-shard.位置).normalized;shard.已过=0;shard.剩余秒=(float)天帝数值.取("shape.chain_range")/特("projectile_speed");特性效果.Add(shard);}split--;count++;}
            else if(Supports("范围")&&dist<=Math.Min(特("shape_radius_max"),下(s,道纹属性.范围))){特性作用(effect,i,特("shape_splash_factor"));count++;}}
    }
    void 推进特性效果(float dt)
    {
        foreach(var effect in 特性效果.ToArray())
        {
            if(!特性效果.Contains(effect))continue;float active=Mathf.Min(dt,effect.剩余秒);effect.剩余秒-=dt;effect.已过+=active;int id=effect.特性;var s=effect.运行;
            if(effect.召唤盾到期<=特性时钟)effect.召唤盾=0;
            if(effect.次级)
            {
                Vector2 old=effect.位置;effect.位置=形态可达(old,old+effect.方向*特("projectile_speed")*active);
                for(int i=0;i<敌人数据.Count;i++)if(敌人数据[i].存活&&!effect.命中.Contains(i)&&线段距离(敌人数据[i].位置,old,effect.位置)<=特("effect_hit_radius")&&寻路.无遮挡(old,敌人数据[i].位置))
                {特性作用(effect,i,特("shape_split_factor"));effect.剩余秒=0;break;}
                if(Vector2.Distance(old,effect.位置)<.00001f)effect.剩余秒=0;
            }
            else if(effect.召唤)
            {
                while(effect.跳数<特("summon_hits")&&effect.已过+.00001f>=effect.下次)
                {effect.跳数++;effect.下次+=effect.总时长/特("summon_hits");特性跳(effect,effect.跳数);int target=找目标(effect.位置,特("attack_range"),effect.命中);if(target>=0){double total=effect.伤害;effect.伤害=total/特("summon_hits");特性作用(effect,target);特性次级(effect,target);effect.伤害=total;留电弧(effect.位置,敌人数据[target].位置);} }
            }
            else if(id==6){if(!effect.仅演出)治疗(主角.最大血量*特("water_heal_hp")*百分(s)*active/Mathf.Max(.01f,effect.总时长),主角.最大灵力*特("water_heal_mp")*百分(s)*active/Mathf.Max(.01f,effect.总时长));}
            else if(id==37){if(!effect.仅演出&&Vector2.Distance(玩家,effect.位置)<=effect.半径)治疗(主角.最大血量*特("shelter_heal")*百分(s)*active,0);}
            else if(effect.土垒||effect.诱饵){ }
            else if(effect.护体)
            {int index=特性效果.Where(e=>e.护体&&e.实例==effect.实例).TakeWhile(e=>e!=effect).Count();effect.位置=玩家;effect.方向=转向(Vector2.right,特性时钟*(float)Math.Min(特("shape_arc_max"),下(s,道纹属性.弧度))+index*360f/effect.分段数量);if(s.持续<=0)effect.剩余秒=0;}
            else if(id==7)
            {
                if(effect.灼印附着)
                {if(effect.目标>=0&&敌人数据[effect.目标].存活){effect.位置=敌人数据[effect.目标].位置;while(effect.已过+.00001f>=effect.下次){effect.下次+=特("effect_tick");特性跳(effect,++effect.跳数);特性作用(effect,effect.目标);特性次级(effect,effect.目标);}}else effect.剩余秒=0;}
                else
                {Vector2 old=effect.位置,desired=old+effect.方向*特("projectile_speed")*active;effect.位置=形态可达(old,desired);for(int i=0;i<敌人数据.Count;i++)if(敌人数据[i].存活&&线段距离(敌人数据[i].位置,old,effect.位置)<=effect.半径&&寻路.无遮挡(old,敌人数据[i].位置)){附着灼印(effect,i);effect.剩余秒=0;break;}if(Vector2.Distance(effect.位置,desired)>.01f)effect.剩余秒=0;}
            }
            else if(id==8)
            {
                while(effect.已过+.00001f>=effect.下次)
                {bool first=effect.跳数==0;特性跳(effect,++effect.跳数);effect.下次+=特("effect_tick");effect.伤害=天帝特性道纹.数值(id,first?"伤害":"持续伤")*u*强(s);范围特性(effect);}
            }
            else if(id==9||id==27)
            {foreach(var e in 敌人数据)if(e.存活&&寻路.无遮挡(effect.位置,e.位置)&&(id==9?Vector2.Distance(e.位置,effect.位置):线段距离(e.位置,effect.位置,effect.位置-effect.方向*特("water_length")/effect.分段数量))<=effect.半径)特性减速(e,特(id==9?"trap_slow":"water_slow"),.15f);if(id==9&&effect.跳数++==0)范围特性(effect);}
            else if(id==38)
            {for(int i=0;i<敌人数据.Count;i++){var e=敌人数据[i];if(!e.存活)continue;bool inside=Vector2.Distance(e.位置,effect.位置)<=effect.半径;if(effect.环内.TryGetValue(i,out bool prior)&&prior!=inside&&寻路.无遮挡(effect.位置,e.位置))特性作用(effect,i);effect.环内[i]=inside;}}
            else if(new[]{1,3,5,11,30,31,32,35}.Contains(id))
            {
                Vector2 old=effect.位置;float step=特("projectile_speed")*active;
                if(支持(s,"弧度")&&id!=35)effect.方向=转向(effect.方向,(float)Math.Min(特("shape_arc_max"),下(s,道纹属性.弧度))*active/Mathf.Max(.01f,effect.总时长));
                Vector2 dir=effect.方向;
                Vector2 desired=id==35?回旋位置(effect):old+dir*step;effect.位置=形态可达(old,desired);
                int primary=-1,count=0;float width=id==3?特("tendril_width"):effect.半径;
                for(int i=0;i<敌人数据.Count&&count<限("targets_per_cast");i++)if(敌人数据[i].存活&&!effect.命中.Contains(i)&&
                    (id==11 ? Vector2.Distance(敌人数据[i].位置,effect.起点)<=Mathf.Min(特("shock_range"),effect.已过*特("projectile_speed"))&&Vector2.Angle(effect.方向,敌人数据[i].位置-effect.起点)<=特("shock_degrees")*.5f : 线段距离(敌人数据[i].位置,old,effect.位置)<=width) && 寻路.无遮挡(old,敌人数据[i].位置))
                {特性作用(effect,i);primary=i;count++;}
                if(primary>=0){特性次级(effect,primary);if(id==32){if(!effect.释放.已恢复){effect.释放.已恢复=true;治疗(主角.最大血量*.02f*百分(s),0);}effect.剩余秒=0;}}
                if(Vector2.Distance(effect.位置,desired)>.01f)effect.剩余秒=0;
            }
            else if(effect.跳数++==0)范围特性(effect);
            if(effect.剩余秒<=.00001f)特性效果.Remove(effect);
        }
    }
    void 范围特性(特性战斗效果 effect)
    {
        int count=0,primary=-1;
        for(int i=0;i<敌人数据.Count&&count<限("targets_per_cast");i++)if(敌人数据[i].存活 && Vector2.Distance(敌人数据[i].位置,effect.位置)<=effect.半径 && !effect.命中.Contains(i)&&寻路.无遮挡(effect.位置,敌人数据[i].位置))
        {特性作用(effect,i);primary=i;count++;}
        if(primary>=0)特性次级(effect,primary);
        光圈数据.Add(new 战斗光圈{位置=effect.位置,半径=effect.半径});
    }
    public Vector2 特性诱饵目标(战斗敌人 enemy,Vector2 player)
    {
        if(enemy.布点.级别!=战斗敌人级别.普通&&enemy.布点.级别!=战斗敌人级别.精英)return player;
        var decoy=特性效果.Where(e=>(e.诱饵||e.召唤&&e.血量>0&&Vector2.Distance(e.位置,enemy.位置)<Vector2.Distance(player,enemy.位置))&&Vector2.Distance(e.位置,enemy.位置)<=Mathf.Max(e.半径,特("decoy_radius"))&&寻路.无遮挡(enemy.位置,e.位置)).OrderByDescending(e=>e.诱饵).ThenBy(e=>Vector2.Distance(e.位置,enemy.位置)).FirstOrDefault();
        return decoy?.位置??player;
    }
    bool 特性目标受击(战斗伤害包 packet,Vector2 point,bool avoid)
    {
        var ally=特性效果.FirstOrDefault(e=>(e.诱饵||e.召唤)&&Vector2.Distance(e.位置,point)<.05f);
        if(ally==null)return 结算玩家受伤(packet,avoid);
        if(ally.诱饵){ally.剩余秒=0;特性效果.Remove(ally);return false;}
        float amount=(float)天帝数值.结算伤害(packet,主角.等级,0,0);
        float absorbed=Mathf.Min(amount,ally.召唤盾);ally.召唤盾-=absorbed;ally.血量-=amount-absorbed;
        if(ally.血量<=0)特性效果.Remove(ally);return false;
    }
    bool 墙挡边(特性战斗效果 wall,Vector2 a,Vector2 b)
    {
        Vector2 side=new Vector2(-wall.方向.y,wall.方向.x);float width=.6f+特("wall_width");
        int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.25f));
        for(int i=0;i<=n;i++)if(线段距离(Vector2.Lerp(a,b,i/(float)n),wall.位置-side*wall.半径,wall.位置+side*wall.半径)<=width)return true;
        return false;
    }
    bool 特性墙挡路(Vector2 a,Vector2 b)=>特性效果.Any(w=>w.土垒&&!w.退化&&w.血量>0&&墙挡边(w,a,b));
    bool 土垒封路(特性战斗效果 candidate)
    {
        var path=new List<Vector2>();
        foreach(var e in 敌人数据.Where(e=>e.存活))
        {
            if(!寻路.路径(e.位置,玩家,path,false,false))continue;
            if(!寻路.路径(e.位置,玩家,path,false,false,(a,b)=>墙挡边(candidate,a,b)||特性墙挡路(a,b)))return true;
        }
        return false;
    }
    public Vector2 特性土垒阻挡(战斗敌人 enemy,Vector2 next,float dt)
    {
        foreach(var wall in 特性效果.Where(e=>e.土垒&&e.血量>0).ToArray())
        {
            Vector2 side=new Vector2(-wall.方向.y,wall.方向.x),a=wall.位置-side*wall.半径,b=wall.位置+side*wall.半径;
            if(线段距离(next,a,b)>.6f+特("wall_width"))continue;
            if(wall.退化){特性减速(enemy,特("wall_slow"),.2f);continue;}
            if(Vector2.Dot(enemy.位置-wall.位置,wall.方向)*Vector2.Dot(next-wall.位置,wall.方向)>0 && 线段距离(next,a,b)>=线段距离(enemy.位置,a,b))continue;
            wall.血量-=enemy.攻击力*dt/Mathf.Max(.1f,enemy.攻击间隔);if(wall.血量<=0)wall.剩余秒=0;return enemy.位置;
        }
        return next;
    }
    public void 清理特性战斗()
    {
        特性效果.Clear();特性盾.Clear();特性运行表.Clear();特性修订=-1;
        foreach(var e in 敌人数据){e.特性减速秒=e.特性弱化秒=e.特性易伤秒=e.特性控制累计=0;}
        主角.设置特性增益(0,0,0,0);
        治疗额度=灵力额度=护盾额度=特性时钟=特性静止秒=特性未伤秒=0;特性上次玩家=玩家;
    }
    public void 推进特性演示(float dt)
    {foreach(var e in 敌人数据)e.束缚剩余秒=Mathf.Max(0,e.束缚剩余秒-dt);推进特性战斗(dt);}
}
