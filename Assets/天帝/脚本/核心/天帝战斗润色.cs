using UnityEngine;
using System.Collections.Generic;
public static class 天帝战斗润色
{
    static readonly Dictionary<string,float> 缓存=new Dictionary<string,float>();
    static bool 有限(Vector2 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y)
        && !float.IsInfinity(v.x) && !float.IsInfinity(v.y);
    static Vector2 限制输入(Vector2 v)
    {
        float 分量=Mathf.Max(Mathf.Abs(v.x),Mathf.Abs(v.y));
        if(float.IsNaN(分量)||float.IsInfinity(分量)) return Vector2.zero;
        if(分量>1) v/=分量;
        float 长=v.magnitude;
        return 长>1 ? v/长 : v;
    }
    public static float 取(string k)
    {
        if(!缓存.TryGetValue(k,out float value)){value=天帝敌种配置.取("presentation.polish."+k);缓存.Add(k,value);}
        return value;
    }
    public static Vector2 摇杆响应(Vector2 输入)
    {
        // 触控驱动在失焦/设备切换时可能短暂给出 NaN 或无穷值；将其当作松手，
        // 避免异常向量污染移动位置和后续的闪避方向。
        if(!有限(输入)) return Vector2.zero;
        float 长=输入.magnitude;
        if(float.IsNaN(长)||float.IsInfinity(长))
        {
            // 先按最大分量缩放，避免超大有限值在 normalized 内部出现 Inf/Inf。
            float 分量=Mathf.Max(Mathf.Abs(输入.x),Mathf.Abs(输入.y));
            输入=分量>0 ? 输入/分量 : Vector2.zero;
        }
        长=输入.magnitude;
        float 死区=取("joystick_deadzone");
        if(float.IsNaN(死区)||float.IsInfinity(死区)) 死区=.1f;
        死区=Mathf.Clamp(死区,0,.95f);
        float 响应=取("joystick_response");
        if(float.IsNaN(响应)||float.IsInfinity(响应)) 响应=.7f;
        响应=Mathf.Max(.01f,响应);
        if(长<=死区||长<=.000001f)return Vector2.zero;
        float 归一=Mathf.Clamp01((Mathf.Min(长,1)-死区)/Mathf.Max(.001f,1-死区));
        return 输入.normalized*Mathf.Pow(归一,响应);
    }
    public static Vector2 推进输入(Vector2 当前,Vector2 目标,float dt)
    {
        if(!有限(当前)) 当前=Vector2.zero;
        if(!有限(目标)) 目标=Vector2.zero;
        if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return 当前;
        目标=限制输入(目标);
        // 松手立即停，反向快速响应，起步最多十余毫秒过渡；不让惯性妨碍躲避。
        if(目标.sqrMagnitude<.0001f)return Vector2.zero;
        // 同向减速使用专门的刹车参数；否则半推摇杆/收杆时会沿用起步加速度，产生拖滞。
        float rate=Vector2.Dot(当前,目标)<0?取("input_reverse"):
            目标.sqrMagnitude<当前.sqrMagnitude?取("input_brake"):取("input_acceleration");
        return Vector2.MoveTowards(当前,Vector2.ClampMagnitude(目标,1),rate*Mathf.Min(dt,.1f));
    }
}
