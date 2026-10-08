#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class 天帝受击反馈验证
{
    static readonly BindingFlags 隐=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static 天帝真实数值验证.报告 报告;
    static void 查(string 名,bool 成立){(成立?报告.通过:报告.失败).Add(名);}
    public static void 运行()
    {
        天帝数值同步检查.校验();报告=new 天帝真实数值验证.报告();
        try { 验证(); }
        catch(Exception e){报告.错误.Add(e.ToString());}
        string dir=Path.Combine(Application.dataPath,"../output/受击反馈优化");Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir,"回归报告.json"),JsonUtility.ToJson(报告,true));
        string old=天帝战斗表现验证.运行();File.Copy(Path.Combine(old,"report.json"),Path.Combine(dir,"原战斗表现回归.json"),true);
        if(报告.失败.Count+报告.错误.Count>0)throw new Exception("受击反馈验证未通过");
    }
    static void 验证()
    {
        var map=new 天帝战斗地图(42,true);var net=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
        var go=new GameObject("隔离受击反馈验证");
        using(var p=new 天帝主角属性(天帝普攻.主角配置(),net))
        {
            var w=new 天帝战斗系统(map,net,p,战斗难度.普通,null,20,()=>new Rect(-25,-14,50,28));
            var artAsset=AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
            using(var art=new 天帝战斗美术(go.transform,artAsset,map,w,false))
            {
                查("正式美术与生成爆闪环可加载",art.可用&&artAsset.获取("HIT闪")!=null&&artAsset.获取("HIT环")!=null);
                战斗受击反馈 last=default;int count=0;
                w.受击表现反馈+=f=>{last=f;count++;art.命中(f);};
                var e=w.敌人.First(x=>x.存活);var pos=e.位置;
                查("零伤害无反馈",!w.伤害敌人(e,0)&&count==0);
                天帝战斗扩展验证.设(e,"护盾量",e.最大血量);
                float hp=e.血量;
                w.伤害敌人(e,new 战斗伤害包(.2,0,p.等级),Vector2.right);
                查("护盾命中来自实际盾损且不扣血",last.仅护盾&&!last.破盾&&last.护盾损失>0&&e.血量==hp);
                查("保留实际入射方向",last.方向==Vector2.right&&ReferenceEquals(last.目标,e));
                天帝战斗扩展验证.设(e,"护盾量",.001f);
                w.伤害敌人(e,new 战斗伤害包(.2,0,p.等级),Vector2.left);
                查("破盾与生命损失分离",last.破盾&&last.生命损失>0&&last.标记=="破盾");
                w.伤害敌人(e,new 战斗伤害包(.2,0,p.等级,1,1,1,2),Vector2.right);
                查("暴击采用伤害包结果而非再次抽签",last.暴击&&last.重击&&last.标记=="暴击");
                float after=e.血量;var action=e.行动;
                art.更新(w,Vector2.zero,.025f);
                var list=(IList)typeof(天帝战斗美术).GetField("敌图",隐).GetValue(art);
                int index=Enumerable.Range(0,w.敌人.Count).First(i=>ReferenceEquals(w.敌人[i],e));var unit=list[index];
                var sprite=(SpriteRenderer)unit.GetType().GetField("像",隐).GetValue(unit);
                Vector3 first=sprite.transform.position;float time=(float)unit.GetType().GetField("受击秒",隐).GetValue(unit);
                float flash=(float)unit.GetType().GetField("受击闪白秒",隐).GetValue(unit);
                for(int i=0;i<10;i++)art.命中(last);
                查("连续命中不会延长当前闪白窗口",(float)unit.GetType().GetField("受击闪白秒",隐).GetValue(unit)==flash);
                time=(float)unit.GetType().GetField("受击秒",隐).GetValue(unit);
                art.更新(w,Vector2.zero,0);first=sprite.transform.position;
                art.更新(w,Vector2.zero,0);
                查("暂停冻结回弹与位置",sprite.transform.position==first&&(float)unit.GetType().GetField("受击秒",隐).GetValue(unit)==time);
                查("回弹只作用立绘不改变实体位置生命行动",e.位置==pos&&e.血量==after&&e.行动==action);
                for(int i=0;i<20;i++)art.更新(w,Vector2.zero,.025f);
                查("回弹按时归零且不会累计漂移",(float)unit.GetType().GetField("受击秒",隐).GetValue(unit)==0&&e.位置==pos);
                for(int i=0;i<200;i++)art.命中(new 战斗受击反馈(e,e.位置,Vector2.right,new 战斗伤害明细(1),1,0,false,true,false,false));
                var pool=(IList)typeof(天帝战斗美术).GetField("特效池",隐).GetValue(art);
                查("连续暴击特效池保持48硬上限",pool.Count<=48);
                for(int i=0;i<25;i++)art.更新(w,Vector2.zero,.025f);
                查("连续命中后回弹仍能结束",(float)unit.GetType().GetField("受击秒",隐).GetValue(unit)==0);
                p.设置当前资源(p.血量,p.当前灵力,0);
                w.伤害玩家(new 战斗伤害包(.1,0,1));
                查("玩家受伤也使用真实生命损失",last.玩家受伤&&last.生命损失>0&&!last.仅护盾);
                var shields=(System.Collections.Generic.List<(float,float,int)>)typeof(天帝战斗系统).GetField("特性盾",隐).GetValue(w);
                shields.Add((5f,100f,0));hp=p.当前血量;count=0;
                w.伤害玩家(new 战斗伤害包(.1,0,1));
                查("临时护盾全吸收仍反馈实际盾损",count==1&&last.仅护盾&&last.明细.合计>0&&p.当前血量==hp);
                w.伤害敌人(e,new 战斗伤害包(e.最大血量*100,0,p.等级));
                查("致命伤快照正确且超量不算实际血损",last.击杀&&!e.存活&&last.生命损失<=e.最大血量);
                int c=count;w.伤害敌人(e,100);查("死亡单位不重复发反馈",count==c);
                foreach(float t in new[]{0f,.05f,.1f,.15f,.2f})查("回弹有界 "+t,Mathf.Abs(天帝受击表现.回弹(t,.2f))<=1);
                查("回弹起止均为零",Mathf.Abs(天帝受击表现.回弹(.2f,.2f))<.0001f&&Mathf.Abs(天帝受击表现.回弹(0,.2f))<.0001f);
            }
        }
        UnityEngine.Object.DestroyImmediate(go);
    }
}
#endif
