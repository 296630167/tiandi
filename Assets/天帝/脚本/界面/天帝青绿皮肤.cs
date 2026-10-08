using System.Collections.Generic;
using UnityEngine;

// 同一套语义素材覆盖旧皮肤编号，保留道纹图集和业务引用。
public static class 天帝青绿皮肤
{
    static readonly Dictionary<string, Sprite> 缓存 = new Dictionary<string, Sprite>();
    static Texture2D 图集;
    public static Texture2D 道纹图集 => 图集 != null ? 图集 : (图集 = Resources.Load<Texture2D>("青绿界面/构筑道纹图集"));
    public static bool 已启用 => 获取("DWUI_属性面板") != null;
    public static Sprite 获取(string 编号)
    {
        string 名 = 素材名(编号); if (名 == null) return null;
        if (!缓存.TryGetValue(名, out var 图)) { 图 = Resources.Load<Sprite>("青绿界面/" + 名); if(图!=null)缓存.Add(名, 图); }
        return 图;
    }
    static string 素材名(string 编号)
    {
        if (编号 == "BG01") return "主页背景";
        if (编号 == "CB01") return "页面背景";
        if (编号 == "DWUI_页面背景") return "页面背景";
        if (string.IsNullOrEmpty(编号)) return null;
        if (编号.StartsWith("QLUI_")) return 编号.Substring(5);
        if (编号.StartsWith("DWUI_D主页"))
        {
            string n = 编号.Substring(8);
            if (n.StartsWith("图标")) return n;
            if (n == "地图卷轴" || n == "导航留白") return "深青面板";
            if (n == "实力卷轴") return "浅纸面板";
            if (n == "封印纹章") return "图标锁定";
            if (n == "地图卡框") return "地图卡框";
            if (n == "设置按钮") return "深青按钮";
            return "浅纸按钮";
        }
        if (!编号.StartsWith("DWUI_")) return null;
        switch (编号.Substring(5))
        {
            case "一级面板": case "二级面板": case "属性面板": case "详情框": case "道纹卡槽": return "浅纸面板";
            case "小信息框": case "数值增加提示": case "数值减少提示": return "小信息纸面";
            case "画布内衬": return "画布内衬";
            case "按钮": case "取消按钮": case "返回按钮": case "纸页签": case "滚动滑块": return "浅纸按钮";
            case "确认按钮": case "选中页签": return "深青按钮";
            case "选中按钮": return "选中按钮";
            case "锁定页签": return "禁用按钮";
            case "关闭": return "图标关闭";
            case "已装备": return "图标勾选";
            case "锁定": return "图标锁定";
            case "解锁": return "图标解锁";
            case "主页落地阴影": return "主页阴影";
            case "分割线": return "分割线";
            case "选中框": return "选中六边框";
            case "道纹底板": case "源纹底板": case "品质框": case "返回图标": case "旋转图标": case "源点图标": case "撤销图标": case "下拉图标": return 编号.Substring(5);
            case "滚动轨": return "禁用按钮";
            case "未装备": return "图标道纹";
            case "可替换": return "可替换";
            case "不可替换": return "图标关闭";
            default: return null;
        }
    }
}
