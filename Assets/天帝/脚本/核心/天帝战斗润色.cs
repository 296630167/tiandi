using UnityEngine;
using System.Collections.Generic;
public static class 天帝战斗润色
{
    static readonly Dictionary<string,float> 缓存=new Dictionary<string,float>();
    public static float 取(string k)
    {
        if(!缓存.TryGetValue(k,out float value)){value=天帝敌种配置.取("presentation.polish."+k);缓存.Add(k,value);}
        return value;
    }
    public static Vector2 摇杆响应(Vector2 输入)
    {
        float 长=输入.magnitude,死区=取("joystick_deadzone");
        return 长<=死区?Vector2.zero:输入.normalized*Mathf.Pow(Mathf.Clamp01((长-死区)/(1-死区)),取("joystick_response"));
    }
    public static Vector2 推进输入(Vector2 当前,Vector2 目标,float dt)
    {
        if(dt<=0)return 当前;
        // 松手立即停，反向快速响应，起步最多十余毫秒过渡；不让惯性妨碍躲避。
        if(目标.sqrMagnitude<.0001f)return Vector2.zero;
        // 同向减速使用专门的刹车参数；否则半推摇杆/收杆时会沿用起步加速度，产生拖滞。
        float rate=Vector2.Dot(当前,目标)<0?取("input_reverse"):
            目标.sqrMagnitude<当前.sqrMagnitude?取("input_brake"):取("input_acceleration");
        return Vector2.MoveTowards(当前,Vector2.ClampMagnitude(目标,1),rate*Mathf.Min(dt,.1f));
    }
}
