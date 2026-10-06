using UnityEngine;

// 主页说明、确认弹窗和战斗实例共享同一份挑战规则。
public static class 天帝地图挑战
{
    public const int 最低等级 = 1, 最高等级 = 100;
    public static int 场上上限 => (int)天帝数值.取("map.cap");
    public static int 波数 => 5;
    public static bool 等级有效(int 等级) => 等级 >= 最低等级 && 等级 <= 最高等级;
    public static int 小怪数量(int 波次) => (int)天帝数值.取("map.waves." + Mathf.Clamp(波次 - 1, 0, 波数 - 1));
    public static int 敌人等级(int 地图等级, 战斗敌人级别 类)
        => Mathf.Clamp(地图等级, 最低等级, 最高等级) + (int)天帝数值.敌参数(类, "offset");
    public static string 等级说明(int 等级)
        => "普通 Lv." + 等级 + "   精英 Lv." + (等级 + 1) + "\n头目 Lv." + (等级 + 3) + "   BOSS Lv." + (等级 + 5);
    public static string 数量说明 => 数量说明等级(1);
    public static string 数量说明等级(int 等级)
        => "共" + (天帝数值.区域敌人数(等级,0)+天帝数值.区域敌人数(等级,1)+天帝数值.区域敌人数(等级,2)+1)
        + "只 · 普通" + 天帝数值.区域敌人数(等级,0) + " / 精英" + 天帝数值.区域敌人数(等级,1)
        + " / 头目" + 天帝数值.区域敌人数(等级,2) + " / BOSS1";
    public static string 进入说明(int 等级)
        => "目的地：青岚原 · 地图等级 " + 等级 + "\n" + 天帝数值.取("map.arena.width") + " × " + 天帝数值.取("map.arena.height") + "米大地图 · 中央草甸出生\n\n"
        + 等级说明(等级) + "\n" + 数量说明等级(等级) + "\n\n"
        + "敌人分批从四周边缘入场并持续追击。\n清剿进度达到80%时，唯一狼王从边缘来袭。\n击败BOSS后可返回；已拾取道纹、通货与灵石保留。";
}
