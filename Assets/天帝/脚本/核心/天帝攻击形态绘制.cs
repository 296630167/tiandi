using System;
using UnityEngine;

// 同一份世界几何用于实战和UGUI演示，所有尺寸和命中保持一致。
public static class 天帝攻击形态绘制
{
    public static void 画护体(天帝战斗系统.特性战斗效果 e,Action<Vector2,Vector2,float,Color> line)
    {
        Color c=new Color(.87f,.82f,.49f,.85f);Vector2 side=new Vector2(-e.方向.y,e.方向.x);
        if(e.特性==2){line(e.位置+e.方向*e.半径,e.位置+e.方向*(e.半径+.4f),.12f,c);return;}
        Vector2 last=Vector2.zero;
        for(int i=0;i<=12;i++){float a=(-e.覆盖角度*.5f+e.覆盖角度*i/12)*Mathf.Deg2Rad;Vector2 p=e.位置+(e.方向*Mathf.Cos(a)+side*Mathf.Sin(a))*e.半径;if(i>0)line(last,p,.13f,c);last=p;}
    }
    public static void 画(天帝战斗系统.攻击形态演出 e, Action<Vector2,Vector2,float,Color> line, Action<Vector2,float,Color> ring)
    {
        Color c=e.预警?new Color(1,.76f,.3f,.8f):new Color(.38f,.91f,.78f,.85f);
        Vector2 d=e.终-e.起, side=d.sqrMagnitude>.00001f?new Vector2(-d.y,d.x).normalized:Vector2.right;
        if(e.预警) { ring(e.终,e.半径,c); return; }
        switch(e.功能)
        {
            case 道纹功能.光束:
                line(e.起,e.终,e.半径*2,c); line(e.起,e.终,e.半径*.5f,Color.white); break;
            case 道纹功能.刃波:
                Vector2 forward=new Vector2(side.y,-side.x); Vector2 last=e.起;
                for(int i=1;i<=12;i++) { float t=i/12f; Vector2 p=Vector2.Lerp(e.起,e.终,t)+forward*Mathf.Sin(t*Mathf.PI)*d.magnitude*.24f; line(last,p,e.半径*2,c);last=p; } break;
            case 道纹功能.地刺:
                ring(e.终,e.半径,c); for(int i=0;i<3;i++) { Vector2 p=e.终+Vector2.right*(i-1)*e.半径*.5f; line(p-Vector2.up*.15f,p+Vector2.up*e.半径*1.4f,.13f,c); } break;
            case 道纹功能.剑雨:
                line(e.终+Vector2.up*1.3f,e.终,.15f,c); line(e.终+new Vector2(-.3f,.8f),e.终+new Vector2(.3f,.8f),.13f,c); ring(e.终,e.半径,c); break;
            case 道纹功能.飞轮:
                ring(e.终,e.半径,c); for(int i=0;i<8;i++) { float a=i*Mathf.PI/4; Vector2 v=new Vector2(Mathf.Cos(a),Mathf.Sin(a));line(e.终+v*e.半径*.7f,e.终+v*e.半径*1.12f,.1f,c); } break;
            case 道纹功能.游龙:
                line(e.起,e.终,e.半径*2,c); ring(e.终,e.半径*1.15f,c);line(e.终-side*e.半径,e.终+side*e.半径,.1f,Color.white);break;
            case 道纹功能.灵网:
                line(e.起,e.终,.12f,c); for(int i=0;i<=6;i++) { Vector2 p=Vector2.Lerp(e.起,e.终,i/6f);line(p-side*.4f,p+side*.4f,.06f,c); }break;
            case 道纹功能.地雷:
                ring(e.终,e.半径,c); ring(e.终,e.半径*.4f,c);line(e.终-Vector2.right*.25f,e.终+Vector2.right*.25f,.12f,c);break;
            default: line(e.起,e.终,e.半径*2,c); ring(e.终,e.半径,c);break;
        }
    }
}
