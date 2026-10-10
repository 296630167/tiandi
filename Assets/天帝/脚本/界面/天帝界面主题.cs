using UnityEngine;
using UnityEngine.UI;

// 全局 UI 语义令牌。页面可以各自使用不同装饰密度，但文字和状态颜色由这里统一，
// 避免新增页面再次复制一套近似的青绿/纸色数值。
public static class 天帝界面主题
{
    public static readonly Color 正文 = new Color(.09f, .24f, .27f);
    public static readonly Color 次文 = new Color(.27f, .37f, .37f);
    public static readonly Color 强调 = new Color(.14f, .38f, .39f);
    public static readonly Color 金墨 = new Color(.53f, .33f, .13f);
    public static readonly Color 浅字 = new Color(.95f, .91f, .78f);
    public static readonly Color 纸色 = new Color(.95f, .91f, .80f);
    public static readonly Color 行底 = new Color(.93f, .94f, .85f);
    public static readonly Color 成功色 = new Color(.16f, .41f, .30f);
    public static readonly Color 警示色 = new Color(.64f, .27f, .17f);
    public static readonly Color 禁用字 = new Color(.46f, .53f, .51f);

    // UGUI 页面统一使用项目注入字体；仅在独立验证夹具或场景漏配时回退。
    public static Font 正文字体(Font 候选)
        => 候选 != null ? 候选 : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static Font 标题字体(Font 候选)
        => 天帝美术资源.当前?.主页标题字体 ?? 正文字体(候选);

    public static void 应用正文(Text 文, Font 候选 = null)
    {
        if (文 == null) return;
        文.font = 正文字体(候选 ?? 文.font);
        文.color = 文.color.a <= 0 ? 正文 : 文.color;
        文.raycastTarget = false;
    }
}
