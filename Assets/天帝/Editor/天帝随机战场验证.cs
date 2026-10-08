#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public static class 天帝随机战场验证
{
    static 天帝真实数值验证.报告 r;
    static readonly BindingFlags 隐=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static void 查(string 名,bool 成立)=>(成立?r.通过:r.失败).Add(名);
    static float P(string k)=>(float)天帝数值.取("map.arena.terrain."+k);
    static string 签名(天帝战斗地图 m)
    {var s=new System.Text.StringBuilder();s.Append(m.战场布局);foreach(var b in m.随机障碍)s.Append('|').Append(b.种类).Append(b.位置.x).Append(b.位置.y).Append(b.角度);foreach(var p in m.浅滩)s.Append(p.位置).Append(p.半径);return s.ToString();}
    public static string 运行()
    {
        天帝数值同步检查.校验();r=new 天帝真实数值验证.报告();
        string dir=Path.Combine(天帝构建工具.项目根,"生成/验证/随机战场-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(dir);
        var watch=Stopwatch.StartNew();var hashes=new HashSet<string>();var layouts=new HashSet<int>();
        try
        {
            var a=AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
            var empty=new Texture2D(128,128,TextureFormat.RGBA32,false);var colors=new Color32[128*128];for(int i=0;i<colors.Length;i++)colors[i]=new Color32(255,255,255,255);empty.SetPixels32(colors);empty.Apply();
            try
            {
                for(int seed=0;seed<12;seed++)
                {
                    var m=new 天帝战斗地图(seed,false,false,seed<6?a.青岚原生存通行图:empty,seed%2==0?1:100,true);
                    hashes.Add(签名(m));layouts.Add(m.战场布局);
                    查("种子"+seed+"生成足量实物障碍",m.随机障碍.Count>=24&&m.随机障碍.Count<=104);
                    查("种子"+seed+"浅滩数量范围",m.浅滩.Count>=4&&m.浅滩.Count<=7);
                    bool safe=true;for(float x=-4;x<=4;x+=1)for(float y=-4;y<=4;y+=1)if(new Vector2(x,y).magnitude<4.4f)safe&=m.可站立(new Vector2(x,y));查("种子"+seed+"出生安全区",safe);
                    var path=new 天帝战斗寻路(m);var nodes=new List<Vector2>();path.取得连通节点(Vector2.zero,nodes);var reachable=new HashSet<Vector2>(nodes);
                    bool connected=true;int sides=0;
                    for(float y=-31.5f;y<32;y++)for(float x=-31.5f;x<32;x++)if(m.可站立(new Vector2(x,y)))connected&=reachable.Contains(new Vector2(x,y));
                    foreach(var p in nodes){if(p.x<-26)sides|=1;if(p.x>26)sides|=2;if(p.y<-26)sides|=4;if(p.y>26)sides|=8;}
                    // 原图不可达节点不纳入新增障碍责任；白图必须全可达。
                    查("种子"+seed+"四面边缘都有连通刷新区",sides==15);
                    if(seed>=6)查("种子"+seed+"所有剩余导航节点均可达",connected);
                    bool blocks=true;foreach(var b in m.随机障碍)blocks&=!m.可站立(b.位置,0);查("种子"+seed+"美术脚底全部参与碰撞",blocks);
                    bool water=true;foreach(var p in m.浅滩)water&=m.可站立(p.位置)&&Mathf.Abs(m.地形移速(p.位置)-.78f)<.001f;查("种子"+seed+"浅滩可走且一致减速",water);
                    if(seed==7)
                    {
                        var m2=new 天帝战斗地图(seed,false,false,empty,1,true);查("同种子同布局且不因地图等级重排",签名(m)==签名(m2));
                        战术(m,path,nodes);
                    }
                }
                查("十二个种子全部不同",hashes.Count==12);查("三类布局均可生成",layouts.Count==3);
                查("浅滩正式Sprite可加载",Resources.Load<Sprite>("随机战场/浅滩")!=null);
                var map=new 天帝战斗地图(77,true);查("旧固定图未添加随机地形",map.随机障碍.Count==0&&map.浅滩.Count==0&&map.地形移速(Vector2.zero)==1);
            }
            finally{UnityEngine.Object.DestroyImmediate(empty);}
        }
        catch(Exception ex){r.错误.Add(ex.ToString());}
        File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(r,true));File.WriteAllText(Path.Combine(dir,"耗时.txt"),watch.Elapsed.TotalSeconds.ToString("0.00"));return dir;
    }
    static void 战术(天帝战斗地图 map,天帝战斗寻路 path,List<Vector2> nodes)
    {
        var b=map.随机障碍[0];Vector2 玩家=Vector2.zero;foreach(var p in nodes)if((p-b.位置).sqrMagnitude>16&&(p-b.位置).sqrMagnitude<36){玩家=p;break;}
        var e=new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通,玩家+Vector2.right*3),战斗难度.普通,80);
        天帝战斗扩展验证.种(e,2);天帝战斗扩展验证.设(e,"已生成",true);天帝战斗扩展验证.设(e,"行动",敌人行动.追击);天帝战斗扩展验证.设(e,"序号",2);
        foreach(var p in nodes)if(Vector2.Distance(p,玩家)>4&&Vector2.Distance(p,玩家)<7&&!path.无遮挡(p,玩家)){天帝战斗扩展验证.设(e,"位置",p);break;}
        var list=new List<战斗敌人>{e};var t=new 天帝敌人战术(map,path,list,p=>true,(p,d)=>true,e.最大血量,null);
        var select=typeof(天帝敌人战术).GetMethod("选择地形站位",隐);
        var pos=(Vector2)select.Invoke(t,new object[]{e,玩家,true,10f,1.5f});查("远程选择有效射击角度或出口",map.可站立(pos)&&pos!=e.位置);
        var wrap=new HashSet<Vector2>();for(int i=0;i<8;i++){天帝战斗扩展验证.设(e,"序号",i);wrap.Add((Vector2)select.Invoke(t,new object[]{e,玩家,false,2f,1.5f}));}查("近战分配多个包围站位",wrap.Count>=4);
        天帝战斗扩展验证.设(e,"序号",0);var ai=new 天帝敌人AI(map,path,list,(unit,damage,skill)=>true,new List<战斗光圈>(),true){战术=t};
        bool legal=true;float start=Vector2.Distance(e.位置,玩家);for(int frame=0;frame<240;frame++){ai.开始帧();for(int step=0;step<4;step++){t.推进效果(玩家,.025f);ai.推进(玩家,.025f);legal&=map.可站立(e.位置);}查("本帧寻路预算"+frame,ai.本次寻路次数<=4);}
        查("复杂地形推进不穿障碍",legal);查("绕障碍后进入射击或接近目标",t.技能释放数>0||Vector2.Distance(e.位置,玩家)<start);
        Vector2 before=e.位置;ai.推进(玩家,0);查("暂停不移动",e.位置==before);
    }
}
#endif
