using UnityEngine;

// 情境建议只读取正式进度；不送资源，不代替玩家放置或旋转道纹。
public static class 天帝修行指引
{
    public static string 下一步(天帝道纹 网, 天帝宝盒 盒, int 地图等级)
    {
        if (网 == null) return "选定天赋，开始修行";
        if (地图等级 > 网.玩家等级) return "当前地图高于角色等级 · 可先降低等级历练";
        if (网.道纹.Count == 0) return 盒 != null && 盒.灵石 >= 天帝宝盒.价格(宝盒种类.属性)
            ? "下一步：在宝盒页开属性盒，再接入源纹" : "下一步：前往青岚原收集灵石与道纹";
        bool 待放=false, 未接=false;
        foreach (var 纹 in 网.道纹)
        { 待放 |= !纹.格子.HasValue; 未接 |= 纹.格子.HasValue && !纹.生效; }
        if (未接) return "下一步：道纹页定位未接通，查看接口原因";
        if (待放)
        {
            foreach (var 格 in 网.所有解锁格) if (!网.已放置.ContainsKey(格)) return "下一步：旋转接口，把道纹放到已解锁空格";
            if (网.技能点 > 0) return "下一步：解锁源纹右侧格，旋转接口后放置";
        }
        if (网.玩家等级 == 1) return "下一步：一级青岚原历练 · 升级获得技能点";
        return 网.技能点 > 0 ? "下一步：用技能点扩展构筑，再去历练" : "下一步：检查攻击链路，再挑战同级地图";
    }
    public static string 详情(天帝道纹 网, 天帝宝盒 盒, int 地图等级)
        => 下一步(网, 盒, 地图等级) + "\n向下滚动查看完整指引\n\n"
        + "① 准备：开局灵石可开属性盒；功能盒改变攻击方式，未必直接增伤。无需道纹也能入图。\n\n"
        + "② 构筑：每格花1技能点。源纹初始只开右口，将道纹放在右侧，旋转至左口对接；仅同圈或向外传导。加成来源可查执行顺序，特性接通后还需满足条件。\n\n"
        + "③ 历练：先选一级青岚原，移动避开暖红预警。PC鼠标瞄准、按1—6施法、空格闪避；手机左摇杆移动，拖动右侧技能瞄准后松手释放。施法消耗灵力。杀敌得经验，每升一级获得1技能点。\n\n"
        + "④ 返回：道纹、通货和灵石自动入库，死亡或离场也保留。用新道纹扩展链路；8级以上敌人才可能掉落特性与转化。";
}

public partial class 天帝界面
{
    public void 显示修行指引()
    {
        if (游戏.阶段 != 游戏阶段.主页 || 新手指引已打开) return;
        if (确认已打开 || 设置已打开 || 角色已打开 || 宝盒已打开 || 图鉴已打开 || 回收已打开) return;
        开始交互指引();
    }
    public void 显示修行手册()
    {
        if (游戏.阶段 != 游戏阶段.主页) return;
        string 内容 = 天帝修行指引.详情(游戏.道纹数据, 游戏.宝盒数据, 游戏.当前地图等级);
        显示确认("修行指引", 内容, "知道了", () => { }, 740);
        foreach (var 文 in 弹层.GetComponentsInChildren<UnityEngine.UI.Text>())
            if (文.text == 内容)
            {
                文.fontSize = 天帝移动适配.启用 ? 16 : 21; 文.alignment = TextAnchor.UpperLeft; 文.lineSpacing = 1.05f;
                if (!天帝移动适配.启用)
                {
                    // PC同样允许滚动长指引，按钮留在正文视口之外。
                    var 框 = (RectTransform)文.transform.parent;
                    var 口 = 区块(框, "修行指引正文视口", 55, 108, 690, 504);
                    var 图 = 口.gameObject.AddComponent<UnityEngine.UI.Image>(); 图.color = Color.clear; 图.raycastTarget = true;
                    口.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                    文.rectTransform.SetParent(口, false); 天帝双端页面布局.固定(文.rectTransform, 0, 0, 690, 504);
                    文.rectTransform.sizeDelta = new Vector2(690, Mathf.Max(504, 文.preferredHeight + 12));
                    var 滚 = 口.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); 滚.viewport = 口; 滚.content = 文.rectTransform;
                    滚.horizontal = false; 滚.vertical = true; 滚.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; 滚.scrollSensitivity = 28;
                }
            }
    }
}
