#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class 天帝战斗表现验证
{
    static readonly BindingFlags 隐 = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static 天帝真实数值验证.报告 r;
    static void 查(string 名, bool 成立) => (成立 ? r.通过 : r.失败).Add(名);
    static void 设(object o, string k, object v) => 天帝战斗扩展验证.设(o, k, v);
    static object 读(object o, string k) => o.GetType().GetField(k, 隐).GetValue(o);
    static 战斗敌人 敌(int 种, Vector2 点, int 序 = 0)
    {
        var e = new 战斗敌人(new 战斗敌人布点(种 == 14 ? 战斗敌人级别.王级 : 战斗敌人级别.普通, 点), 战斗难度.普通, 80);
        天帝战斗扩展验证.种(e, 种); 设(e, "已生成", true); 设(e, "行动", 敌人行动.追击); 设(e, "序号", 序); return e;
    }
    public static string 运行()
    {
        天帝数值同步检查.校验(); r = new 天帝真实数值验证.报告();
        AssetDatabase.ImportAsset("Assets/天帝/Resources/战斗特效/战斗特效图集.png",ImportAssetOptions.ForceUpdate);
        string dir = Path.Combine(天帝构建工具.项目根, "生成/验证/战斗表现-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(dir);
        try { 战术(); 命中与演出(); 几何(); }
        catch (Exception ex) { r.错误.Add(ex.ToString()); }
        File.WriteAllText(Path.Combine(dir, "report.json"), JsonUtility.ToJson(r, true)); Debug.Log("战斗表现验证 " + dir); return dir;
    }
    static void 战术()
    {
        var map = new 天帝战斗地图(42, true); var path = new 天帝战斗寻路(map);
        foreach (int 种 in new[] { 2, 3, 8, 9, 21 })
        {
            var e = 敌(种, new Vector2(5.5f, 0)); 设(e, "冷却", 2f);
            var t = new 天帝敌人战术(map, path, new List<战斗敌人>{e}, p=>true, (p,d)=>true, e.最大血量, null);
            Vector2 目标 = e.位置; t.推进敌(e, Vector2.zero, .025f, (a,b,c)=>目标=b);
            查("冷却期间侧移而非站桩 " + 种, Vector2.Distance(e.位置,目标) > .1f && map.可站立(目标, .45f));
            var 锁点 = 目标; t.推进敌(e, Vector2.zero, .025f, (a,b,c)=>目标=b); 查("低频站位不每帧左右抖动 " + 种, 锁点 == 目标);
        }
        var heal = 敌(6, new Vector2(5,0)); var front = 敌(0,new Vector2(3,0)); 设(heal,"冷却",2f);
        var list = new List<战斗敌人>{heal,front};
        var support = new 天帝敌人战术(map,path,list,p=>true,(p,d)=>true,heal.最大血量+front.最大血量,null);
        Vector2 cover=Vector2.zero; support.推进敌(heal,Vector2.zero,.025f,(a,b,c)=>cover=b);
        查("辅助站在近战队友后方",cover.x>front.位置.x && map.可站立(cover,.45f));
        设(front,"血量",0f);support.推进敌(heal,Vector2.zero,.6f,(a,b,c)=>cover=b);查("死亡队友不再作为掩护",!ReferenceEquals(读(heal,"掩护队友"),front));
        var boss=敌(14,new Vector2(0,0));设(boss,"已攻击次数",1);
        var bt=new 天帝敌人战术(map,path,new List<战斗敌人>{boss},p=>true,(p,d)=>true,boss.最大血量,null);
        var select=typeof(天帝敌人战术).GetMethod("下一技",隐);
        int close=(int)select.Invoke(bt,new object[]{boss,new Vector2(2,0)}),far=(int)select.Invoke(bt,new object[]{boss,new Vector2(7,0)});
        查("狼王根据距离切换第二击",close==16 && far==15);
        设(boss,"技能编号",15);设(boss,"行动",敌人行动.蓄力);设(boss,"蓄力",.5f);设(boss,"蓄力总秒",.5f);设(boss,"锁定方向",Vector2.right);设(boss,"攻击落点",new Vector2(5,0));
        bt.推进敌(boss,Vector2.up*4,.1f,(a,b,c)=>{});查("非撕咬前摇方向与落点锁定",(Vector2)读(boss,"锁定方向")==Vector2.right && (Vector2)读(boss,"攻击落点")==new Vector2(5,0));
        float wind=(float)读(boss,"蓄力");bt.推进敌(boss,Vector2.zero,0,(a,b,c)=>{});查("零时间不推进AI",(float)读(boss,"蓄力")==wind);
    }
    static void 命中与演出()
    {
        var map=new 天帝战斗地图(42,true);var e=敌(2,Vector2.zero);int hit=0;
        var t=new 天帝敌人战术(map,new 天帝战斗寻路(map),new List<战斗敌人>{e},p=>{hit++;return true;},(p,d)=>{hit++;return true;},e.最大血量,null);
        设(e,"技能编号",16);设(e,"锁定方向",Vector2.right);设(e,"攻击落点",Vector2.zero);
        for(int i=0;i<100;i++)t.释放(e,new Vector2(20,0));
        查("纯演出48预算且不伤害远处玩家",t.冲击演出.Count==48 && hit==0 && t.敌术.Count==0);
        设(e,"技能编号",3);t.释放(e,new Vector2(4,0));查("演出满载不会挡住真实飞弹",t.敌术.Count==1);
        float elapsed=t.冲击演出[0].已过;t.推进效果(new Vector2(4,2),0);查("零时间不推进特效",t.冲击演出[0].已过==elapsed);
        for(int i=0;i<100;i++)t.推进效果(new Vector2(4,2),.025f);
        查("普通直线弹可以横移躲开",hit==0);查("冲击与弹体正常到期清理",t.冲击演出.Count==0 && t.敌术.Count==0);
        设(e,"技能编号",4);t.释放(e,new Vector2(4,0));for(int i=0;i<100;i++)t.推进效果(new Vector2(4,0),.025f);查("扇射仍共享一次命中",hit==1);
        设(e,"技能编号",16);t.释放(e,Vector2.zero);t.清理();查("离场同时清理逻辑和演出",t.敌术.Count==0 && t.冲击演出.Count==0 && t.辅助线.Count==0);
    }
    static void 几何()
    {
        var shader=Shader.Find("天帝/生成特效");查("生成贴图透明材质可用于URP且无编译错误",shader!=null&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader));
        var go=new GameObject("隔离战斗演出几何");var s=go.AddComponent<天帝战斗场景>();
        try
        {
            var g=new 天帝纹理特效(go.transform,-10001);
            var air=new 天帝纹理特效(go.transform);
            设(s,"特效地面",g);设(s,"特效",air);
            foreach(float progress in new[]{0f,.25f,.75f,1f})
            {
                g.清空();
                typeof(天帝战斗场景).GetMethod("画预警圈",隐).Invoke(s,new object[]{Vector2.zero,2f,progress,Color.red});
                var vertices=(IList)读(g,"顶点");float max=0;bool finite=true;
                foreach(Vector3 p in vertices){max=Mathf.Max(max,Mathf.Abs(p.x),Mathf.Abs(p.z));finite &= !float.IsNaN(p.x)&&!float.IsNaN(p.z);}
                查("圆贴图直径固定且坐标有效 "+progress,finite&&Mathf.Abs(max-2)<.001f);
            }
            g.清空();
            typeof(天帝战斗场景).GetMethod("画预警线",隐).Invoke(s,new object[]{Vector2.zero,new Vector2(5,0),2f,.5f,Color.red});
            var line=(IList)读(g,"顶点");bool bounded=true;
            foreach(Vector3 p in line)bounded&=p.x>=-.001f&&p.x<=5.001f&&Mathf.Abs(p.z)<=1.001f;
            查("冲锋边框和箭头吻合真实宽度",bounded);
            g.清空();g.扇(天帝纹理特效.预警环,Vector2.zero,Vector2.right,3,80,Color.white);
            bool fan=true;foreach(Vector3 p in (IList)读(g,"顶点"))if(new Vector2(p.x,p.z).sqrMagnitude>.0001f)fan&=p.x>=0&&new Vector2(p.x,p.z).magnitude<=3.001f&&Mathf.Abs(Mathf.Atan2(p.z,p.x)*Mathf.Rad2Deg)<=40.001f;
            查("扇形按真实半径与角度裁切生成纹理",fan);
            g.清空();for(int i=0;i<12;i++)g.贴图(i,Vector2.zero,Vector2.one,Vector2.up,Color.white);
            var uv=(IList)读(g,"UV");bool atlas=true;
            for(int i=0;i<12;i++)for(int j=0;j<6;j++){Vector2 p=(Vector2)uv[i*6+j];atlas&=p.x>i%4/4f&&p.x<(i%4+1)/4f&&p.y>(2-i/4)/3f&&p.y<(3-i/4)/3f;}
            查("十二格UV均在保护边内无跨格采样",atlas);
            g.清空();for(int i=0;i<5000;i++)g.圈(6,Vector2.zero,1,Color.white);查("贴图预算限制4096片段",((IList)读(g,"顶点")).Count==4096*6);
            air.清空();for(int i=0;i<6;i++)air.弹(i,Vector2.zero,Vector2.right,.22f,Color.white);查("六种弹体独立贴图且使用原画尺寸",((IList)读(air,"顶点")).Count==36);
            air.清空();typeof(天帝战斗场景).GetMethod("画电弧",隐).Invoke(s,new object[]{Vector2.zero,new Vector2(3,0),1,1f,true});
            查("直接连锁的既有电弧数据使用生成流光连接",((IList)读(air,"顶点")).Count>6&&((IList)读(air,"UV")).Count==((IList)读(air,"顶点")).Count);
            g.清空();g.提交();查("清空时同步清除旧顶点和UV",((IList)读(g,"顶点")).Count==0&&((IList)读(g,"UV")).Count==0);
            var texture=Resources.Load<Texture2D>("战斗特效/战斗特效图集");查("正式图集可加载且尺寸未被NPOT缩放",texture!=null&&texture.width==2048&&texture.height==1536);
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/天帝/Resources/战斗特效/战斗特效图集.png");查("图集无mipmap且Clamp并保留透明",importer!=null&&!importer.mipmapEnabled&&importer.wrapMode==TextureWrapMode.Clamp&&importer.alphaSource==TextureImporterAlphaSource.FromInput);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
#endif
