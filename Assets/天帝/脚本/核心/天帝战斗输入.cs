using UnityEngine;
using UnityEngine.InputSystem;

public static class 天帝战斗输入
{
    public static (Vector2 方向, bool 跑步, bool 切换调试区域) 读取(天帝界面 界面)
    {
        if (天帝移动适配.启用)
            return (界面?.战斗摇杆 != null ? 界面.战斗摇杆.方向 : Vector2.zero, 界面?.触控跑步 == true, false);
        var 键 = Keyboard.current;
        if (键 == null) return (Vector2.zero, false, false);
        var 方向 = new Vector2((键.dKey.isPressed || 键.rightArrowKey.isPressed ? 1 : 0) - (键.aKey.isPressed || 键.leftArrowKey.isPressed ? 1 : 0),
            (键.wKey.isPressed || 键.upArrowKey.isPressed ? 1 : 0) - (键.sKey.isPressed || 键.downArrowKey.isPressed ? 1 : 0));
        return (Vector2.ClampMagnitude(方向, 1), 键.leftShiftKey.isPressed || 键.rightShiftKey.isPressed, 键.f8Key.wasPressedThisFrame);
    }
}
