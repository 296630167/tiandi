using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class 天帝战斗场景
{
    public Vector2 瞄准方向 { get; private set; } = Vector2.right;
    float 闪避剩余;
    Vector2 闪避方向;
    bool 输入已冻结;
    public bool 可以主动操作 => 游戏 != null && 游戏.阶段 == 游戏阶段.战斗 && 战斗 != null
        && !战斗.玩家死亡 && !游戏.界面.战斗已暂停 && !游戏.界面.新手指引冻结战斗;
    public bool 设置屏幕瞄准(Vector2 屏幕)
    {
        if (俯视相机 == null) return false;
        var 射线 = 俯视相机.ScreenPointToRay(屏幕);
        if (!new Plane(Vector3.up, Vector3.zero).Raycast(射线, out float 距)) return false;
        var 点 = 射线.GetPoint(距); var 向 = new Vector2(点.x, 点.z) - 玩家位置;
        if (向.sqrMagnitude < .000001f) return false;
        瞄准方向 = 向.normalized; return true;
    }
    public void 设置触控瞄准(Vector2 向)
    { if (向.sqrMagnitude > .000001f) 瞄准方向 = 向.normalized; }
    public 战斗操作结果 释放技能(int 槽)
    {
        var 结果 = 可以主动操作 ? 战斗.尝试释放技能(槽, 瞄准方向, 玩家位置) : 战斗操作结果.无法操作;
        if (可以主动操作) 游戏.界面.显示战斗操作反馈(结果, 槽, false);
        游戏?.界面?.更新战斗状态(); return 结果;
    }
    void 自动战斗操作反馈(战斗操作结果 结果, int 槽, bool 闪)
    { if (可以主动操作) 游戏.界面.显示战斗操作反馈(结果, 槽, 闪); }
    public 战斗操作结果 闪避()
    {
        if (!可以主动操作) return 战斗操作结果.无法操作;
        var 结果 = 战斗.尝试闪避();
        if (结果 == 战斗操作结果.成功)
        {
            闪避方向 = 瞄准方向;
            闪避剩余 = (float)天帝数值.取("player.dodge_duration"); 平滑输入 = Vector2.zero;
            天帝声音.提示("润色_跑步");
        }
        游戏.界面.显示战斗操作反馈(结果, -1, true); return 结果;
    }
    void 读取主动操作()
    {
        if (天帝移动适配.启用 || !可以主动操作) return;
        if (Mouse.current != null) 设置屏幕瞄准(Mouse.current.position.ReadValue());
        var 选 = EventSystem.current?.currentSelectedGameObject;
        if (选 != null && 选.GetComponentInParent<InputField>() != null) return;
        var 键 = Keyboard.current; if (键 == null) return;
        for (int i = 0; i < 6; i++) if (键[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) 释放技能(i);
        if (键.spaceKey.wasPressedThisFrame) 闪避();
    }
    void 推进闪避位移(float 秒)
    {
        if (战斗.玩家死亡) { 闪避剩余 = 0; return; }
        if (闪避剩余 <= 0) return;
        float 步 = Mathf.Min(秒, 闪避剩余); 闪避剩余 = Mathf.Max(0, 闪避剩余 - 步);
        玩家位置 = 地图.直线闪避(玩家位置, 闪避方向,
            步 * (float)天帝数值.取("player.dodge_distance") / (float)天帝数值.取("player.dodge_duration"));
        主角.position = new Vector3(玩家位置.x, 0, 玩家位置.y);
        美术?.移动反馈(闪避方向, true);
        游戏.界面.更新战斗位置(玩家位置, 地图.所在格(玩家位置));
    }
}
