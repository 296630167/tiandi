using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 设计坐标只描述比例。提交后用真正的伸展锚点，页面不整体缩放。
public sealed class 天帝比例矩形 : MonoBehaviour
{
    public Vector2 参考尺寸;
    public bool 待提交 = true, 动态;
}

public sealed class 天帝响应布局 : MonoBehaviour
{
    readonly List<天帝比例矩形> 待建 = new List<天帝比例矩形>();
    static readonly Vector2 默认尺寸 = new Vector2(1600, 900);
    public static RectTransform 创建(RectTransform 父, string 名, float x, float y, float w, float h)
    {
        var r = new GameObject(名, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(父, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        登记(r, new Vector2(w, h));
        if (名.Contains("浮窗") || 名 == "拖动道纹") 动态(r);
        return r;
    }
    public static void 登记(RectTransform r, Vector2 参考)
    {
        var d = r.GetComponent<天帝比例矩形>() ?? r.gameObject.AddComponent<天帝比例矩形>(); d.参考尺寸 = 参考;
        var canvas = r.GetComponentInParent<Canvas>(); if (canvas == null) return;
        var 布局 = canvas.GetComponent<天帝响应布局>() ?? canvas.gameObject.AddComponent<天帝响应布局>(); 布局.待建.Add(d);
    }
    static Vector2 参考(RectTransform r)
    {
        if (r == null) return 默认尺寸;
        var d = r.GetComponent<天帝比例矩形>(); if (d != null) return d.参考尺寸;
        if (r.anchorMin != r.anchorMax && r.parent is RectTransform 父) return 参考(父);
        return r.rect.width > 1 && r.rect.height > 1 ? r.rect.size : 默认尺寸;
    }
    public static void 比例(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = new Vector2(x, 1 - y - h); r.anchorMax = new Vector2(x + w, 1 - y);
        r.offsetMin = r.offsetMax = Vector2.zero; r.localScale = Vector3.one;
        var d = r.GetComponent<天帝比例矩形>(); if (d != null) d.待提交 = false;
    }
    public static void 动态(RectTransform r)
    {
        var d = r.GetComponent<天帝比例矩形>() ?? r.gameObject.AddComponent<天帝比例矩形>(); d.动态 = true; d.待提交 = false;
    }
    public static void 设计位置(RectTransform r, Vector2 点)
    {
        var d = r.GetComponent<天帝比例矩形>();
        if (d == null || d.动态) { r.anchoredPosition = 点; return; }
        var 尺寸 = 参考(r.parent as RectTransform); var 大小 = d.参考尺寸;
        比例(r, (点.x - r.pivot.x * 大小.x) / 尺寸.x, (-点.y - (1 - r.pivot.y) * 大小.y) / 尺寸.y, 大小.x / 尺寸.x, 大小.y / 尺寸.y);
    }
    static bool 是动态子树(Transform r)
    {
        for (var t = r; t != null; t = t.parent) { var d = t.GetComponent<天帝比例矩形>(); if (d != null && d.动态) return true; }
        return false;
    }
    static bool 在纵向内容(Transform r)
    {
        for (var t = r; t != null; t = t.parent)
        { var p = t.parent; var s = p == null ? null : p.GetComponent<ScrollRect>(); if (s != null && s.vertical && s.content == t) return true; }
        return false;
    }
    public static void 字号(Text 文)
    {
        if (!天帝移动适配.启用) return;
        int 大小 = Mathf.Clamp(Mathf.RoundToInt(文.fontSize * .72f), 14, 30); 文.fontSize = 大小;
        if (文.resizeTextForBestFit) { 文.resizeTextMinSize = 14; 文.resizeTextMaxSize = Mathf.Max(14, 大小); }
    }
    public void 提交()
    {
        foreach (var d in 待建)
        {
            if (d == null) continue;
            var r = (RectTransform)d.transform;
            var 文 = r.GetComponent<Text>(); if (文 != null) 字号(文);
            if (!d.待提交 || 是动态子树(r)) continue;
            d.待提交 = false;
            if (r.anchorMin != r.anchorMax) continue; // Slider/Scrollbar由UGUI驱动
            var 父尺寸 = 参考(r.parent as RectTransform); var 大小 = r.sizeDelta; d.参考尺寸 = 大小;
            float x = 父尺寸.x * r.anchorMin.x + r.anchoredPosition.x - r.pivot.x * 大小.x;
            float y = 父尺寸.y * (1 - r.anchorMin.y) - r.anchoredPosition.y - (1 - r.pivot.y) * 大小.y;
            if (在纵向内容(r))
            {
                r.anchorMin = new Vector2(x / Mathf.Max(1, 父尺寸.x), 1); r.anchorMax = new Vector2((x + 大小.x) / Mathf.Max(1, 父尺寸.x), 1);
                r.pivot = new Vector2(0, 1); r.sizeDelta = new Vector2(0, 大小.y); r.anchoredPosition = new Vector2(0, -y);
            }
            else 比例(r, x / Mathf.Max(1, 父尺寸.x), y / Mathf.Max(1, 父尺寸.y), 大小.x / Mathf.Max(1, 父尺寸.x), 大小.y / Mathf.Max(1, 父尺寸.y));
            var 像 = r.GetComponent<Image>();
            if (像 != null && 像.type == Image.Type.Simple && (r.name.Contains("立绘") || r.name.Contains("图标"))) 像.preserveAspect = true;
        }
        待建.Clear();
    }
    void LateUpdate() => 提交();
    public static void 进度(RectTransform r, float 值, float 设计宽)
    {
        var d = r.GetComponent<天帝比例矩形>();
        if (d == null || d.待提交) { r.sizeDelta = new Vector2(设计宽 * Mathf.Clamp01(值), r.sizeDelta.y); return; }
        r.anchorMax = new Vector2(r.anchorMin.x + 设计宽 / Mathf.Max(1, 参考(r.parent as RectTransform).x) * Mathf.Clamp01(值), r.anchorMax.y);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
    public static RectTransform 横向卡列(RectTransform 父, IList<RectTransform> 卡, float x, float y, float w, float h)
    {
        var 口 = 创建(父, "天赋卡列视口", 0, 0, 1600, 390); 比例(口, x, y, w, h);
        var 图 = 口.gameObject.AddComponent<Image>(); 图.color = Color.clear; 口.gameObject.AddComponent<RectMask2D>();
        var 内容 = 创建(口, "天赋横向列表", 0, 0, 卡.Count * 232, 390);
        内容.anchorMin = Vector2.zero; 内容.anchorMax = new Vector2(0, 1); 内容.pivot = new Vector2(0, 1); 内容.sizeDelta = new Vector2(卡.Count * 232, 0);
        内容.GetComponent<天帝比例矩形>().待提交 = false;
        for (int i = 0; i < 卡.Count; i++) { 卡[i].SetParent(内容, false); 比例(卡[i], i / (float)卡.Count, 0, 220f / (卡.Count * 232), 1); }
        var s = 口.gameObject.AddComponent<ScrollRect>(); s.viewport = 口; s.content = 内容;
        s.horizontal = true; s.vertical = false; s.movementType = ScrollRect.MovementType.Clamped; s.scrollSensitivity = 32;
        return 口;
    }
    public static RectTransform 滚动正文(RectTransform 内容)
    {
        var 父 = (RectTransform)内容.parent; var 原 = 内容.GetComponent<天帝比例矩形>();
        float 高 = 原 != null ? 原.参考尺寸.y : 内容.rect.height;
        var 口 = 创建(父, 内容.name + "视口", 内容.anchoredPosition.x, -内容.anchoredPosition.y, 内容.sizeDelta.x, 高);
        口.SetSiblingIndex(内容.GetSiblingIndex());
        var 图 = 口.gameObject.AddComponent<Image>(); 图.color = Color.clear; 图.raycastTarget = true; 口.gameObject.AddComponent<RectMask2D>();
        内容.SetParent(口, false); 内容.anchoredPosition = Vector2.zero;
        内容.anchorMin = new Vector2(0, 1); 内容.anchorMax = Vector2.one; 内容.pivot = new Vector2(0, 1); 内容.sizeDelta = new Vector2(0, 高);
        if (原 != null) 原.待提交 = false;
        var s = 口.gameObject.AddComponent<ScrollRect>(); s.viewport = 口; s.content = 内容;
        s.horizontal = false; s.vertical = true; s.movementType = ScrollRect.MovementType.Clamped; s.scrollSensitivity = 40; return 口;
    }
    public static RectTransform 滚动列(RectTransform 父, string 名, float x, float y, float w, float h)
    {
        var 子 = new List<RectTransform>();
        foreach (RectTransform r in 父)
            if (r.GetComponent<天帝比例矩形>() != null && r.anchorMin == r.anchorMax && r.anchoredPosition.x >= x - .1f && -r.anchoredPosition.y >= y - .1f &&
                r.anchoredPosition.x + r.sizeDelta.x <= x + w + .1f && -r.anchoredPosition.y + r.sizeDelta.y <= y + h + .1f) 子.Add(r);
        var 内容 = 创建(父, 名, x, y, w, h);
        foreach (var r in 子) { var 点 = r.anchoredPosition; r.SetParent(内容, false); r.anchoredPosition = 点 - new Vector2(x, -y); }
        return 滚动正文(内容);
    }
}
