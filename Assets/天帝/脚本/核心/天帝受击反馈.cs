using UnityEngine;

// 从真实结算发出的表现快照，不重新判定暴击、护盾或击杀。
public readonly struct 战斗受击反馈
{
    public readonly 战斗敌人 目标;
    public readonly Vector2 位置, 方向;
    public readonly 战斗伤害明细 明细;
    public readonly float 生命损失, 护盾损失;
    public readonly bool 玩家受伤, 暴击, 破盾, 击杀;
    public bool 仅护盾 => 护盾损失 > 0 && 生命损失 <= 0;
    public bool 重击 => 暴击 || 破盾 || 击杀;
    public 战斗受击反馈(战斗敌人 目标, Vector2 位置, Vector2 方向, 战斗伤害明细 明细,
        float 生命损失, float 护盾损失, bool 玩家受伤, bool 暴击, bool 破盾, bool 击杀)
    {
        this.目标=目标;this.位置=位置;this.方向=方向.sqrMagnitude>.0001f?方向.normalized:Vector2.up;
        this.明细=明细;this.生命损失=Mathf.Max(0,生命损失);this.护盾损失=Mathf.Max(0,护盾损失);
        this.玩家受伤=玩家受伤;this.暴击=暴击;this.破盾=破盾;this.击杀=击杀;
    }
    public string 标记 => 破盾 ? "破盾" : 击杀 ? 玩家受伤?"致命":"击破" : 仅护盾 ? "护盾" : 暴击 ? "暴击" : 玩家受伤 ? "受伤" : "";
}

public static class 天帝受击表现
{
    public static float 取(string 键) => 天帝敌种配置.取("presentation.hit_feedback."+键);
    // 快速冲击后单次回弹归零，不让连续受击永久漂移或改变物理位置。
    public static float 回弹(float 剩余,float 时长)
    { float t=1-Mathf.Clamp01(剩余/Mathf.Max(.001f,时长));return Mathf.Sin(t*Mathf.PI*2)*(1-t); }
}
