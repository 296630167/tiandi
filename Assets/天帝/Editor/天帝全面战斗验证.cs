#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class 天帝全面战斗验证
{
    static readonly BindingFlags 隐=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static 天帝真实数值验证.报告 r;
    static void 查(string n,bool ok)=>(ok?r.通过:r.失败).Add(n);
    static void 设(object o,string n,object v)=>天帝战斗扩展验证.设(o,n,v);
    static object 读(object o,string n)=>o.GetType().GetField(n,隐).GetValue(o);
    static object 调(object o,string n,params object[] a)=>o.GetType().GetMethod(n,隐).Invoke(o,a);
    static string dir;
    public static void 运行()
    {
        天帝数值同步检查.校验();r=new 天帝真实数值验证.报告();dir=Path.Combine(Application.dataPath,"../output/战斗全面优化");Directory.CreateDirectory(dir);
        try{素材();输入();节奏();强敌与协作();形态与压力();}
        catch(Exception ex){r.错误.Add(ex.ToString());}
        File.WriteAllText(Path.Combine(dir,"综合回归.json"),JsonUtility.ToJson(r,true));
        foreach(var pair in new[]{("原战斗表现",(Func<string>)天帝战斗表现验证.运行),("道纹形态与特性",(Func<string>)天帝特性与形态验证.运行),("屏外刷新",(Func<string>)天帝战斗视野验证.运行)})
        {string p=pair.Item2();File.Copy(Path.Combine(p,"report.json"),Path.Combine(dir,pair.Item1+"回归.json"),true);}
        天帝受击反馈验证.运行();
        if(r.失败.Count+r.错误.Count>0)throw new Exception("综合回归失败，请看报告");
    }
    static void 素材()
    {
        var go=new GameObject("独立形态资源验证");
        try
        {
            var tex=Resources.Load<Texture2D>("独立攻击形态/形态图集");
            查("十种独立形态图集尺寸与Shader有效",tex!=null&&tex.width==2048&&tex.height==1536&&Shader.Find("天帝/生成特效").isSupported);
            using(var fx=new 天帝纹理特效(go.transform,10000,"独立攻击形态/形态图集"))
            {for(int i=0;i<10;i++)fx.贴图(i,Vector2.right*i,Vector2.one,Vector2.up,Color.white);fx.提交();查("十格生成原画可合并绘制",((IList)读(fx,"顶点")).Count==60);}
            var codes=new List<string>();for(int i=1;i<=12;i++)codes.Add("BTN"+i.ToString("00"));for(int i=1;i<=2;i++)codes.Add("BTE"+i.ToString("00"));for(int i=1;i<=5;i++)codes.Add("BTB"+i.ToString("00"));for(int i=15;i<=21;i++)codes.Add("BTV"+i);
            foreach(string code in codes)
            {
                bool ok=true;var unique=new HashSet<Sprite>();
                for(int i=0;i<5;i++)
                {
                    var s=Resources.Load<Sprite>("敌人动作/"+code+"_"+i);ok&=s!=null&&unique.Add(s);
                    if(s==null)continue;var t=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(s));
                    ok&=t.spriteImportMode==SpriteImportMode.Single&&Mathf.Abs(t.spritePivot.y-.12f)<.001f&&!t.mipmapEnabled&&s.rect.width==512;
                }
                查("动作与共同脚底导入 "+code,ok);
            }
            var actor=new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通,Vector2.zero),战斗难度.普通,20);设(actor,"已生成",true);设(actor,"行动",敌人行动.追击);
            var anim=new 天帝敌人动作();var a=anim.读取("BTN01",actor,true,0);var b=anim.读取("BTN01",actor,true,天帝战斗润色.取("walk_cycle")*.55f);
            查("行走姿态真实交替",a!=null&&b!=null&&a!=b);设(actor,"行动",敌人行动.后摇);查("释放姿态独立",anim.读取("BTN01",actor,false,0)!=a);设(actor,"血量",0f);查("死亡姿态独立",anim.读取("BTN01",actor,false,0)==Resources.Load<Sprite>("敌人动作/BTN01_3"));
            foreach(string name in new[]{"咬击","翼射","炎术","岩击","疗愈","护盾","狼王","破盾","暴击","跑步"})
            {var clip=Resources.Load<AudioClip>("战斗润色音效/"+name);查("独立音效加载与时长 "+name,clip!=null&&clip.length>.1f&&clip.length<2&&clip.channels==1);}
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static void 输入()
    {
        查("摇杆死区归零",天帝战斗润色.摇杆响应(new Vector2(.05f,0))==Vector2.zero);
        查("摇杆最大幅度与斜向速度有界",Mathf.Abs(天帝战斗润色.摇杆响应(Vector2.one*2).magnitude-1)<.0001f);
        查("轻推可细调且不必推到底",天帝战斗润色.摇杆响应(new Vector2(.5f,0)).magnitude>0&&天帝战斗润色.摇杆响应(new Vector2(.5f,0)).magnitude<1);
        查("松手立即停而非惯性漂移",天帝战斗润色.推进输入(Vector2.right,Vector2.zero,.016f)==Vector2.zero);
        查("零时间不推进移动响应",天帝战斗润色.推进输入(Vector2.right,Vector2.left,0)==Vector2.right);
        查("反向两帧内完成",天帝战斗润色.推进输入(天帝战斗润色.推进输入(Vector2.right,Vector2.left,.016f),Vector2.left,.016f)==Vector2.left);
    }
    static void 节奏()
    {
        var art=AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        foreach(int level in new[]{1,50,100})
        {
            var map=new 天帝战斗地图(512+level,false,false,art.青岚原生存通行图,level,true);var net=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
            using(var p=new 天帝主角属性(天帝普攻.主角配置(),net))
            {
                var w=new 天帝战斗系统(map,net,p,战斗难度.普通,null,level,()=>new Rect(-14,-8,28,16));
                for(int n=0;n<5000&&w.未生成敌人数量>1;n++)
                {
                    调(w,"推进生存",.1f);
                    foreach(var e in w.敌人)if(e.已生成&&e.物种!=14)设(e,"血量",0f);
                }
                查("新节奏完整刷完非王名额不漏怪 Lv"+level,w.敌人.Where(x=>x.物种!=14).All(x=>x.已生成));
                查("不重排敌人索引/数量 Lv"+level,w.敌人.Count==map.敌人.Count);
                设(w,"累计敌人损伤",(float)读(w,"总敌人最大生命"));调(w,"推进生存",1f);
                查("按真实伤损门槛仍能登场狼王 Lv"+level,w.BOSS已出现&&w.敌人.Last().已生成);
                查("喘息批次已真实执行 Lv"+level,(int)读(w,"生存增援轮")>=3);
            }
        }
    }
    static 战斗敌人 敌(int species,Vector2 pos,战斗敌人级别 rank=战斗敌人级别.普通)
    {var e=new 战斗敌人(new 战斗敌人布点(rank,pos),战斗难度.普通,80);天帝战斗扩展验证.种(e,species);设(e,"已生成",true);设(e,"行动",敌人行动.追击);return e;}
    static void 强敌与协作()
    {
        var map=new 天帝战斗地图(42,true);var path=new 天帝战斗寻路(map);var boss=敌(14,Vector2.zero,战斗敌人级别.王级);
        var t=new 天帝敌人战术(map,path,new List<战斗敌人>{boss},p=>true,(p,d)=>true,boss.最大血量,null);
        设(boss,"行动",敌人行动.蓄力);设(boss,"技能编号",16);设(boss,"蓄力",.01f);设(boss,"蓄力总秒",.5f);设(boss,"攻击落点",Vector2.zero);
        t.推进敌(boss,Vector2.right,.02f,(e,p,dt)=>{});
        查("狼王大招后有正式反击窗口",(float)读(boss,"后摇秒")>=天帝战斗润色.取("attack_pose_fraction")+天帝敌种配置.取("boss.opening_seconds"));
        var mobile=敌(8,new Vector2(4,0));设(mobile,"序号",1);var list=new List<战斗敌人>{mobile};
        t=new 天帝敌人战术(map,path,list,p=>true,(p,d)=>true,mobile.最大血量,null);
        设(t,"突进协作等待",.2f);
        查("错开突进起手不同时压上",!(bool)调(t,"预警许可",mobile,敌技能.读取(9)));
        t.推进效果(Vector2.zero,0);查("暂停不缩短协作间隔",(float)读(t,"突进协作等待")==.2f);
        t.推进效果(Vector2.zero,.3f);查("协作间隔结束可再次突进",(bool)调(t,"预警许可",mobile,敌技能.读取(9)));
    }
    static void 形态与压力()
    {
        var net=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));var map=new 天帝战斗地图(42,true);
        using(var p=new 天帝主角属性(天帝普攻.主角配置(),net))
        {
            var w=new 天帝战斗系统(map,net,p,战斗难度.普通);
            for(int i=0;i<200;i++)调(w,"留形态",道纹功能.地雷,Vector2.zero,Vector2.zero,1f,true);
            查("驻留预警重复采样合并为一个",w.攻击形态效果.Count==1);
            调(w,"留形态",道纹功能.地雷,Vector2.zero,Vector2.zero,1f,false);查("真实释放与预警不互相合并",w.攻击形态效果.Count==2);
            for(int i=0;i<500;i++)调(w,"留形态",道纹功能.光束,Vector2.right*i,Vector2.right*i+Vector2.up,1f,false);
            查("演出饱和仍遵守正式上限",w.攻击形态效果.Count<=天帝顺序道纹.形态数值("visuals_alive"));
            调(w,"推进形态演出",1f);查("演出到期全部释放",w.攻击形态效果.Count==0);
        }
        var art=AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        var big=new 天帝战斗地图(99,false,false,art.青岚原生存通行图,100,true);
        net.设置玩家等级(100);
        using(var p=new 天帝主角属性(天帝普攻.主角配置(),net))
        {
            var w=new 天帝战斗系统(big,net,p,战斗难度.普通,null,100,()=>new Rect(-14,-8,28,16));
            for(int i=0;i<20;i++)调(w,"推进生存",1f);
            int active=w.场上敌人数量;
            long probeBefore=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[65536];long probeDelta=GC.GetAllocatedBytesForCurrentThread()-probeBefore;GC.KeepAlive(probe);
            var sw=Stopwatch.StartNew();long before=GC.GetAllocatedBytesForCurrentThread();bool budget=true;
            for(int n=0;n<180;n++){w.推进(Vector2.zero,1/60f);budget&=w.本次寻路次数<=4;}
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;sw.Stop();
            File.WriteAllText(Path.Combine(dir,"模型压力数据.txt"),"高等级正式配置：起始在场="+active+"；结束在场="+w.场上敌人数量+"；玩家死亡="+w.玩家死亡+"；180次推进累计毫秒="+sw.Elapsed.TotalMilliseconds.ToString("0.##")+"；当前线程分配字节="+(probeDelta>0?bytes.ToString():"当前Mono计数器不支持，不能据此判定零分配")+"；单次平均毫秒="+(sw.Elapsed.TotalMilliseconds/180).ToString("0.###")+"；不等同于渲染FPS或手机性能\n");
            查("高等级真实敌人场上上限",active>0&&active<=w.生存场上上限+1);
            查("压力运行寻路仍保持每帧四次预算",budget);
            查("压力运行敌人仍在合法通行位置",w.敌人.Where(x=>x.存活).All(x=>big.可站立(x.位置)));
        }
    }
}
#endif
