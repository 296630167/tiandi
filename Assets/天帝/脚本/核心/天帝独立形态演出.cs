using UnityEngine;
public sealed partial class 天帝战斗场景
{
    天帝纹理特效 独立形态特效;
    void 画独立形态()
    {
        if(独立形态特效==null)独立形态特效=new 天帝纹理特效(transform,10000,"独立攻击形态/形态图集");
        独立形态特效.清空();
        Rect view=读取战斗视野();view.xMin-=4;view.xMax+=4;view.yMin-=4;view.yMax+=4;
        int count=0,max=(int)天帝战斗润色.取("shape_visible_max");
        foreach(var e in 战斗.攻击形态效果)
        {
            if(!view.Contains(e.终)&&!view.Contains(e.起)&&!线穿视野(e.起,e.终,view))continue;
            if(e.预警)
            {特效地面.圈(天帝纹理特效.预警环,e.终,e.半径,new Color(.45f,.95f,.8f,.5f));continue;}
            if(count++>=max)break;
            int id=(int)e.功能-(int)道纹功能.光束;
            Vector2 d=e.终-e.起,up=d.sqrMagnitude>.00001f?d.normalized:Vector2.up;
            float alpha=Mathf.Clamp01(e.剩余秒/天帝顺序道纹.形态数值("visual_seconds"))*天帝战斗润色.取("friendly_alpha");
            var c=new Color(1,1,1,alpha);
            switch(e.功能)
            {
                case 道纹功能.光束:case 道纹功能.灵鞭:case 道纹功能.游龙:
                    独立形态特效.贴图(id,(e.起+e.终)*.5f,new Vector2(e.半径*2,d.magnitude),up,c);break;
                case 道纹功能.刃波:
                    // 原逻辑起终是横向刃宽，贴图弧刃也沿相同横向范围。
                    独立形态特效.贴图(id,(e.起+e.终)*.5f,new Vector2(d.magnitude,Mathf.Max(e.半径*2,d.magnitude*.3f)),new Vector2(up.y,-up.x),c);break;
                case 道纹功能.灵网:
                    独立形态特效.贴图(id,(e.起+e.终)*.5f,new Vector2(d.magnitude,Mathf.Max(.6f,d.magnitude*.5f)),new Vector2(up.y,-up.x),c);break;
                case 道纹功能.剑雨:
                    独立形态特效.贴图(id,e.终+Vector2.up*.65f,new Vector2(e.半径*2,1.6f),Vector2.up,c);break;
                case 道纹功能.旋刃:
                    独立形态特效.贴图(id,e.终,Vector2.one*e.半径*2,up,c);break;
                default:独立形态特效.贴图(id,e.终,Vector2.one*e.半径*2,Vector2.up,c);break;
            }
        }
        独立形态特效.提交();
    }
    static bool 线穿视野(Vector2 a,Vector2 b,Rect r)
    {
        float t0=0,t1=1;Vector2 d=b-a;
        bool Clip(float p,float q){if(Mathf.Abs(p)<.000001f)return q>=0;float t=q/p;if(p<0){if(t>t1)return false;t0=Mathf.Max(t0,t);}else{if(t<t0)return false;t1=Mathf.Min(t1,t);}return true;}
        return Clip(-d.x,a.x-r.xMin)&&Clip(d.x,r.xMax-a.x)&&Clip(-d.y,a.y-r.yMin)&&Clip(d.y,r.yMax-a.y);
    }
}
