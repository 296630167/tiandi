#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// 仅使用隔离模型和边界输入，不向正式开局发资源或修改真实存档。
public static class 天帝战斗扩展验证
{
    static 天帝真实数值验证.报告 结果;
    static readonly BindingFlags 隐 = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static void 设(object o, string k, object v)
    { var p = o.GetType().GetProperty(k, 隐); if (p != null) p.SetValue(o, v); else o.GetType().GetField(k, 隐).SetValue(o, v); }
    public static void 种(战斗敌人 e, int id) => typeof(战斗敌人).GetMethod("设置物种", 隐).Invoke(e, new object[] { id });
    static void 检查(string n, bool b) => (b ? 结果.通过 : 结果.失败).Add(n);
    static bool 近(double a, double b) => Math.Abs(a-b) < Math.Max(.0001, Math.Abs(b)*.00001);
    static 战斗敌人 敌(int id, int level = 80, 战斗敌人级别 q = 战斗敌人级别.普通)
    {
        var e = new 战斗敌人(new 战斗敌人布点(q, Vector2.zero), 战斗难度.普通, level);
        种(e,id); 设(e,"已生成",true); 设(e,"行动",敌人行动.追击); return e;
    }
    public static string 运行()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("模型检查需编辑模式。");
        天帝数值同步检查.校验(); 结果 = new 天帝真实数值验证.报告();
        string dir = Path.Combine(天帝构建工具.项目根,"生成/验证/战斗扩展-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(dir);
        try { 美术(); 队列与形态(); 技能(); 辅助(); 自然驱动(); 正式波次与伤损(); }
        catch (Exception ex) { 结果.错误.Add(ex.ToString()); }
        File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(结果,true)); return dir;
    }
    static void 美术()
    {
        var a = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        foreach (string c in new[] { "N", "E", "B" }) for(int i=1;i<=(c=="N"?12:c=="E"?2:5);i++)
        {
            string id="BT"+c+i.ToString("00"); var s=a.获取(id);
            检查("立绘绑定与固定脚轴 "+id,s!=null && s.texture.width==1024 && s.texture.height==1024 && 近(s.pivot.x,512) && 近(s.pivot.y,64));
        }
    }
    static void 队列与形态()
    {
        var 覆盖=new HashSet<int>();
        for(int l=1;l<=100;l++)
        {
            var q=天帝敌种配置.普通队列(l,42);
            检查("地图"+l+"普通60且符合解锁",q.Length==60 && q.All(id=>天帝敌种配置.已解锁(id,l)));
            foreach(var id in q) 覆盖.Add(id);
            int expected=l<20?1:l<40?2:l<60?3:l<80?4:5;
            检查("地图"+l+"形态数",天帝敌种配置.形态数(l)==expected && 天帝敌种配置.形态(l,1)==1 && 天帝敌种配置.形态(l,0)==expected);
        }
        检查("十二物种跨档位均出现",覆盖.Count==12);
        for(int seed=0;seed<100;seed++)
        {
            var q=天帝敌种配置.普通队列(80,seed);
            检查("80档整局必含十二物种 种子"+seed,q.Distinct().Count()==12);
        }
        for(int i=0;i<4;i++)
        {
            float t=天帝敌种配置.取("boss.thresholds.4."+i);
            检查("五形态阈值"+i,天帝敌种配置.形态(80,t+.001f)==i+1 && 天帝敌种配置.形态(80,t-.001f)==i+2);
        }
        var 王=敌(14,80,战斗敌人级别.王级); float max=王.最大血量;
        设(王,"血量",max*.19f);
        var 路=new 天帝战斗地图(42,true); var list=new List<战斗敌人>{王};
        var tcs=new 天帝敌人战术(路,new 天帝战斗寻路(路),list,p=>true,(p,d)=>true,max,null);
        tcs.推进敌(王,Vector2.right, .025f,(e,p,d)=>{});
        检查("跨多阈值直接五形态无回血无重置",近(王.最大血量,max)&&近(王.血量,max*.19)&&王.行动==敌人行动.后摇);
        检查("变形立绘编号正确",天帝敌种配置.美术编号(王)=="BTB05");
        foreach(int l in new[]{1,20,50,80,100}) for(int k=1;k<=19;k++)
        {
            var e=敌(0,l); var s=敌技能.读取(k); var p=天帝敌种配置.伤害包(e,k);
            double armor=天帝数值配置.敌人(l,0,5),res=天帝数值配置.敌人(l,0,6);
            double actual=天帝数值.结算伤害(p,l,armor,res),expected=天帝数值.结算伤害(new 战斗伤害包(e.攻击力*s.倍率,0,e.等级),l,armor,res);
            检查("正式属性归一"+l+"/"+k,近(actual,expected)&&近(p.元素,p.五行来源.总和));
        }
    }
    static void 技能()
    {
        var map=new 天帝战斗地图(42,true); var path=new 天帝战斗寻路(map); var e=敌(2);
        var list=new List<战斗敌人>{e}; int hits=0;
        var t=new 天帝敌人战术(map,path,list,p=>{hits++;return true;},(p,d)=>{hits++;return true;},e.最大血量,null);
        void 准备(int id,Vector2 player)
        { t.清理();hits=0;设(e,"位置",Vector2.zero);设(e,"技能编号",id);设(e,"锁定方向",Vector2.right);设(e,"攻击落点",player); }
        准备(4,new Vector2(4,0));t.释放(e,new Vector2(4,0));
        for(int i=0;i<90;i++)t.推进效果(new Vector2(4,0),.025f);
        检查("三发扇射共享一次命中",hits==1);
        准备(3,new Vector2(4,0));t.释放(e,new Vector2(4,0));for(int i=0;i<90;i++)t.推进效果(new Vector2(4,1.5f),.025f);
        检查("横移躲开直射弹",hits==0);
        准备(9,new Vector2(5,0));t.释放(e,new Vector2(5,0));for(int i=0;i<30;i++)t.推进效果(new Vector2(3,0),.025f);
        检查("冲锋沿途命中仅一次且落点可通行",hits==1 && map.可站立(e.位置));
        准备(10,new Vector2(4,0));t.释放(e,new Vector2(4,0));t.推进效果(new Vector2(4,0),.2f);
        检查("跃击空中未提前命中并有高度",hits==0&&e.跳跃高度>0);
        for(int i=0;i<30;i++)t.推进效果(new Vector2(4,0),.025f);
        检查("跃击落地一次且高度归零",hits==1&&近(e.跳跃高度,0));
        准备(11,new Vector2(1,0));t.释放(e,new Vector2(-1,0));检查("扇面背后安全",hits==0);
        t.释放(e,new Vector2(1,0));检查("扇面正面真实命中",hits==1);
        准备(19,new Vector2(3,0));t.释放(e,new Vector2(3,0));for(int i=0;i<50;i++)t.推进效果(new Vector2(3,2),.025f);
        检查("归潮前段与延迟二段可分别躲避",hits==2);
        准备(5,new Vector2(4,0));t.释放(e,new Vector2(4,0));for(int i=0;i<120;i++)t.推进效果(new Vector2(4,0),.025f);
        检查("投掷一次加四次地面伤害并到期移除",hits==5&&t.敌术.Count==0);
        准备(7,new Vector2(4,0));t.释放(e,new Vector2(4,0));for(int i=0;i<40;i++)t.推进效果(new Vector2(4,0),.025f);
        检查("水弹施加正式短减速",t.玩家移速倍率<1&&t.玩家移速倍率>0);
        for(int i=0;i<60;i++)t.推进效果(new Vector2(4,0),.025f);检查("减速到期恢复",近(t.玩家移速倍率,1));
        准备(3,new Vector2(4,0));t.释放(e,new Vector2(4,0));t.清理();检查("离场清除全部敌术",t.敌术.Count==0&&t.辅助线.Count==0);
        准备(3,new Vector2(4,0));for(int i=0;i<60;i++)t.释放(e,new Vector2(4,0));检查("敌方效果48上限",t.敌术.Count==48);t.清理();
        准备(1,new Vector2(1,0));t.释放(e,new Vector2(2.4f,0));检查("近战后撤离开真实范围安全",hits==0);
        // 场景边界外的弹体与冲锋不跨越通行边界。
        准备(3,new Vector2(40,0));设(e,"位置",new Vector2(34,0));t.释放(e,new Vector2(40,0));
        for(int i=0;i<100;i++)t.推进效果(new Vector2(41,0),.025f);检查("远程弹体出通行边界销毁且不命中",hits==0&&t.敌术.Count==0);
        var root=敌(10,80,战斗敌人级别.精英);list.Add(root);设(root,"技能编号",8);设(root,"锁定方向",Vector2.right);
        t.清理();t.释放(root,new Vector2(3,0));检查("60档以上精英藤刺短束缚",t.玩家移速倍率==0);
        t.推进效果(new Vector2(3,0),.5f);t.释放(root,new Vector2(3,0));检查("控制结束两秒免疫禁止连续束缚",t.玩家移速倍率==1);
        // 动态视野变化：起手后移出视野不能从屏外发布无预警弹体。
        var off=new 天帝敌人战术(map,path,list,p=>true,(p,d)=>true,e.最大血量,()=>new Rect(20,20,2,2));
        设(e,"行动",敌人行动.蓄力);设(e,"蓄力",.01f);off.推进敌(e,new Vector2(4,0),.025f,(a,b,c)=>{});
        检查("蓄力移出视野取消释放",off.技能释放数==0&&off.敌术.Count==0);
    }
    static void 辅助()
    {
        var map=new 天帝战斗地图(42,true);var path=new 天帝战斗寻路(map);
        var heal=敌(6);var shield=敌(7);var a=敌(0);var b=敌(1);var boss=敌(14,80,战斗敌人级别.王级);
        设(a,"位置",Vector2.right);设(b,"位置",Vector2.up);设(a,"血量",a.最大血量*.4f);设(b,"血量",b.最大血量*.4f);
        var list=new List<战斗敌人>{heal,shield,a,b,boss};float sum=list.Sum(x=>x.最大血量);
        var t=new 天帝敌人战术(map,path,list,p=>true,(p,d)=>true,sum,null);
        void 起手(战斗敌人 e,int id)
        {设(e,"行动",敌人行动.追击);设(e,"冷却",0f);t.推进敌(e,new Vector2(3,0),.025f,(x,y,z)=>{});}
        起手(heal,13);检查("治疗引导选中受伤非辅助队友",(int)typeof(战斗敌人).GetField("技能编号",隐).GetValue(heal)==13);
        float hp=a.血量;t.完成辅助(heal,敌技能.读取(13));检查("治疗每次8%且画出连线",近(a.血量-hp,a.最大血量*.08)&&t.辅助线.Count==2);
        for(int i=0;i<5;i++){起手(heal,13);t.完成辅助(heal,敌技能.读取(13));}
        检查("治疗个人15%和全局10%硬预算",近(a.累计恢复,a.最大血量*.15)&&t.实际治疗<=sum*.1+.01);
        检查("辅助与王不可互奶",heal.累计恢复==0&&shield.累计恢复==0&&boss.累计恢复==0);
        起手(shield,14);t.完成辅助(shield,敌技能.读取(14));检查("护盾授予10%真实盾值",近(a.护盾量,a.最大血量*.1));
        t.推进效果(new Vector2(3,0),6.1f);检查("盾六秒过期附三秒禁用",a.护盾量==0&&a.盾禁用>0);
        起手(shield,14);t.完成辅助(shield,敌技能.读取(14));检查("禁用期不能立刻补盾",a.护盾量==0);
        t.推进效果(new Vector2(3,0),3.1f);起手(shield,14);t.完成辅助(shield,敌技能.读取(14));
        检查("授盾个人20%及全局8%预算",a.累计授盾<=a.最大血量*.2+.01&&t.实际授盾<=sum*.08+.01);
        // 另一个受伤目标允许重新引导；伤害立即中断，不能追补治疗。
        设(b,"累计恢复",0f);起手(heal,13);t.受伤(heal);检查("实际受伤中断治疗并进入后摇",t.引导打断数==1&&heal.行动==敌人行动.后摇);
    }
    static void 自然驱动()
    {
        var map=new 天帝战斗地图(42,true);var path=new 天帝战斗寻路(map);
        for(int kind=0;kind<15;kind++)
        {
            var e=敌(kind,80,kind==14?战斗敌人级别.王级:kind>=12?战斗敌人级别.头目:战斗敌人级别.普通);
            var target=敌(0);设(target,"位置",Vector2.right);设(target,"血量",target.最大血量*.4f);
            var list=new List<战斗敌人>{e};if(kind==6||kind==7||kind==13)list.Add(target);
            var t=new 天帝敌人战术(map,path,list,p=>true,(p,d)=>true,list.Sum(x=>x.最大血量),null);
            var ai=new 天帝敌人AI(map,path,list,p=>true,new List<战斗光圈>(),true){战术=t};
            var player=new Vector2(kind==0||kind==1||kind==10||kind==11||kind==12?1:4,0);
            for(int i=0;i<400;i++){ai.开始帧();t.推进效果(player,.025f);ai.推进(player,.025f);}
            int id=(int)天帝敌种配置.物种(kind,"skill");
            检查("集中AI自然释放物种"+kind,t.技能统计[id]>0&&ai.本次寻路次数<=4&&map.可站立(e.位置));
        }
        for(int form=1;form<=5;form++)
        {
            var e=敌(14,80,战斗敌人级别.王级);float[] ratio={.9f,.7f,.5f,.3f,.19f};设(e,"血量",e.最大血量*ratio[form-1]);
            var list=new List<战斗敌人>{e};var t=new 天帝敌人战术(map,path,list,p=>true,(p,d)=>true,e.最大血量,null);
            var ai=new 天帝敌人AI(map,path,list,p=>true,new List<战斗光圈>(),true){战术=t};
            for(int i=0;i<1500;i++){ai.开始帧();t.推进效果(new Vector2(4,0),.025f);ai.推进(new Vector2(4,0),.025f);}
            int[][] expect={new[]{1,15,16},new[]{1,17,2},new[]{11,8,5},new[]{7,18,15},new[]{1,19,17,8,18}};
            检查("狼王形态"+form+"自然驱动轮换技能",expect[form-1].All(id=>t.技能统计[id]>0));
        }
    }
    static void 正式波次与伤损()
    {
        var map=new 天帝战斗地图(42,true);var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
        using(var p=new 天帝主角属性(天帝普攻.主角配置(),网))
        {
            var w=new 天帝战斗系统(map,网,p,战斗难度.普通,null,20,()=>new Rect(-25,-14,50,28));
            var a=w.敌人.First(x=>x.存活); w.伤害敌人(a,a.最大血量*.3f); float ratio=w.敌人损伤比例;
            设(a,"血量",Mathf.Min(a.最大血量,a.血量+a.最大血量*.08f));w.伤害敌人(a,a.最大血量*.08f);
            检查("同段生命回血再伤不重复推进召王",近(ratio,w.敌人损伤比例));
            设(a,"护盾量",a.最大血量*.1f);float hp=a.血量;w.伤害敌人(a,a.最大血量*.04f);
            检查("护盾先吸收且不推进召王",近(a.血量,hp)&&近(ratio,w.敌人损伤比例));
        }
        foreach(int l in new[]{1,5,10,20,40,60,80,100})
        {
            var net=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
            using(var p=new 天帝主角属性(天帝普攻.主角配置(),net))
            {
                var w=new 天帝战斗系统(map,net,p,战斗难度.普通,null,l,()=>new Rect(-25,-14,50,28));
                bool cap=true;
                for(int i=0;i<1200&&w.击败数<66;i++)
                {
                    for(int g=0;g<4;g++) { int[] lim={4,2,1,1};cap &= w.敌人.Count(e=>e.存活&&天帝敌种配置.分组(e.物种)==g)<=lim[g]; }
                    cap &= w.场上敌人数量<=19;
                    foreach(var e in w.敌人.Where(x=>x.存活).ToArray())w.伤害敌人(e,new 战斗伤害包(0,10000000,100));
                    w.推进(Vector2.zero,.25f);
                }
                检查("地图"+l+"角色和场上上限",cap);
                检查("地图"+l+"五波66名额不丢且召王",w.击败数==66&&w.未生成敌人数量==0&&w.BOSS已出现);
                int xp=w.本局经验;var dead=w.敌人.First();w.伤害敌人(dead,10000000);
                检查("地图"+l+"死亡不重复结算",w.本局经验==xp&&w.击败数==66);
            }
        }
    }
}
#endif
