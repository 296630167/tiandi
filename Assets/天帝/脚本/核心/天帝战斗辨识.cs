using UnityEngine;

public static class 天帝战斗辨识
{
    // 俯视立绘位于不同Y平面，遮挡只比较可见的XZ投影。
    public static bool 平面相交(Bounds 左, Bounds 右)
        => 左.min.x < 右.max.x && 左.max.x > 右.min.x && 左.min.z < 右.max.z && 左.max.z > 右.min.z;
}
