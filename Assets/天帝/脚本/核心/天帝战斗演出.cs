using UnityEngine;

// 合并贴图演出：只读取正式战斗状态，不参与伤害、位移、时钟或索敌。
public sealed partial class 天帝战斗场景
{
    static float 演出参数(string 键)=>天帝敌种配置.取("presentation."+键);
    static int 玩家弹图(普攻参数 参数)
    {
        int 图=天帝纹理特效.灵弹;double 最大=0;
        for(int i=0;i<5;i++){double 值=参数.五行来源.读取(i);if(值>最大){最大=值;图=i;}}
        return 图;
    }
    void 画预警圈(Vector2 点,float 半径,float 进度,Color 色) => 画分层预警圈(点, 半径, 进度, 色, false);
    void 画分层预警圈(Vector2 点,float 半径,float 进度,Color 色,bool 敌方)
    {
        进度=Mathf.Clamp01(进度);
        var 底=色;底.a=演出参数("telegraph_alpha");
        特效地面.圈(天帝纹理特效.危险区,点,半径,底);
        底.a=演出参数("telegraph_fill_alpha");
        特效地面.圈(天帝纹理特效.危险区,点,半径*进度,底);
        (敌方 ? 敌方危险描边 : 特效地面).圈(天帝纹理特效.预警环,点,半径,色);
        特效地面.圈(天帝纹理特效.预警环,点,半径*进度,色);
    }
    void 画预警扇(Vector2 点,Vector2 朝,float 半径,float 角,float 进度,Color 色) => 画分层预警扇(点, 朝, 半径, 角, 进度, 色, false);
    void 画分层预警扇(Vector2 点,Vector2 朝,float 半径,float 角,float 进度,Color 色,bool 敌方)
    {
        var 底=色;底.a=演出参数("telegraph_alpha");
        特效地面.扇(天帝纹理特效.危险区,点,朝,半径,角,底);
        var 边层 = 敌方 ? 敌方危险描边 : 特效地面;
        边层.扇(天帝纹理特效.预警环,点,朝,半径,角,色);
        特效地面.扇(天帝纹理特效.预警环,点,朝,半径*Mathf.Clamp01(进度),角,色);
        float a=Mathf.Atan2(朝.y,朝.x),h=角*Mathf.Deg2Rad*.5f;
        // 用生成的长条标注两条真实径向边，不程序绘制花纹。
        for(int i=0;i<2;i++){float p=i==0?a-h:a+h;边层.线(天帝纹理特效.尾迹,点,点+new Vector2(Mathf.Cos(p),Mathf.Sin(p))*半径,.07f,色);}
    }
    void 画方向箭头(Vector2 点,Vector2 向,float 长,Color 色) => 画分层方向箭头(点, 向, 长, 色, false);
    void 画分层方向箭头(Vector2 点,Vector2 向,float 长,Color 色,bool 敌方)
    {(敌方 ? 敌方危险描边 : 特效地面).贴图(天帝纹理特效.尾迹,点,new Vector2(长*.5f,长*2),向,色,.19f);}
    void 画预警线(Vector2 起,Vector2 终,float 宽,float 进度,Color 色) => 画分层预警线(起, 终, 宽, 进度, 色, false);
    void 画分层预警线(Vector2 起,Vector2 终,float 宽,float 进度,Color 色,bool 敌方)
    {
        (敌方 ? 敌方危险描边 : 特效地面).线(天帝纹理特效.冲锋框,起,终,宽,色);
        var 底=色;底.a=演出参数("telegraph_fill_alpha");
        特效地面.线(天帝纹理特效.冲锋框,起,Vector2.Lerp(起,终,Mathf.Clamp01(进度)),宽,底);
    }
    void 画生成敌术()
    {
        int 数=0,上限=(int)演出参数("glow_visible_max");
        Rect 视野=读取战斗视野();视野.xMin-=3;视野.xMax+=3;视野.yMin-=3;视野.yMax+=3;
        for(int 层=0;层<2;层++)
        foreach(var a in 层==0?战斗.战术.敌术:战斗.战术.冲击演出)
        {
            Vector2 点=a.地面||a.二段||a.跃击?a.终点:a.位置;
            // 只裁剪纯冲击装饰；真实敌术弹体不受48个装饰预算影响。
            if(层==1){if(!视野.Contains(点))continue;if(数++>=上限)break;}
            Color 色=敌术色(a.技能);
            if(a.冲击演出)
            {
                float t=Mathf.Clamp01(a.已过/a.寿命);色.a*=1-t;
                if(a.技能.类型==敌技能类型.直线)
                    特效.线(天帝纹理特效.尾迹,点,点+a.方向*a.半径,a.技能.宽度,色);
                else if(a.技能.类型==敌技能类型.扇面||(a.技能.类型==敌技能类型.归潮&&a.半径>天帝敌种配置.取("boss.second_radius")))
                {
                    bool 王=a.来源.物种==14&&a.技能.类型==敌技能类型.扇面;
                    特效.扇(天帝纹理特效.冲击环,点,a.方向,(王?天帝敌种配置.取("boss.fan_range"):a.半径)*(.8f+.2f*t),王?天帝敌种配置.取("boss.fan_angle"):a.技能.宽度,色,.28f);
                }
                else 特效.圈(天帝纹理特效.冲击环,点,a.半径*(.8f+.2f*t),色,.28f);
                特效.圈(天帝纹理特效.爆闪,点,Mathf.Min(.6f,a.半径),色,.29f);
            }
            else if(a.地面)
            {
                var 底=色;底.a*=.35f;
                特效地面.圈(天帝纹理特效.危险区,点,a.半径,底);
                敌方危险描边.圈(天帝纹理特效.预警环,点,a.半径,色);
            }
            else if(a.二段||a.跃击)
            {
                float t=a.二段?1-Mathf.Max(0,a.延迟)/天帝敌种配置.取("boss.second_delay"):Mathf.Clamp01(a.已过/a.寿命);
                画分层预警圈(点,a.半径,t,色,true);
            }
            else if(a.位移)特效.线(天帝纹理特效.尾迹,a.起点,a.位置,.3f,色);
            else
            {
                if(a.技能.类型==敌技能类型.投掷)点+=Vector2.up*Mathf.Sin(Mathf.Clamp01(a.已过/a.寿命)*Mathf.PI)*1.5f;
                int 图=a.技能.元素>=0&&a.技能.元素<5?a.技能.元素:天帝纹理特效.灵弹;
                特效.弹(图,点,a.方向,.23f*天帝战斗润色.取("enemy_projectile_outline"),new Color(1,.25f,.12f,.85f));
                特效.弹(图,点,a.方向,.23f,Color.white);
                if(a.技能.类型==敌技能类型.投掷)敌方危险描边.圈(天帝纹理特效.预警环,a.终点,a.半径,色);
            }
        }
        foreach(var a in 战斗.战术.辅助线)
        {
            Color 色=a.盾?new Color(.42f,.82f,1,.8f):new Color(.42f,.95f,.56f,.8f);色.a*=Mathf.Clamp01(a.剩余/.5f);
            特效.线(天帝纹理特效.尾迹,a.起,a.终,.18f,色);
            特效.圈(天帝纹理特效.爆闪,a.终,.4f,色);
            特效地面.圈(天帝纹理特效.预警环,a.终,.7f+(.5f-a.剩余),色);
        }
    }
}
