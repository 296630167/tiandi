using System;
using UnityEngine;
using UnityEngine.UI;

// 手机分区在固定横屏逻辑画面内排版，正文保留可读行高；外层统一等比缩放。
public sealed class 天帝移动排版 : MonoBehaviour
{
    public Action<RectTransform> 排版;
    Action<RectTransform> 上次排版;
    Vector2 上次尺寸 = new Vector2(-1, -1);
    public void 更新()
    {
        var 根 = (RectTransform)transform;
        if (!天帝移动适配.启用 || 根.rect.width <= 1 || 根.rect.height <= 1 || 根.rect.size == 上次尺寸 && 排版 == 上次排版) return;
        上次尺寸 = 根.rect.size; 上次排版 = 排版; 排版?.Invoke(根);
    }
    void LateUpdate() => 更新();
}

public static class 天帝双端页面布局
{
    // 关闭按钮上边4、图高56，正文再留8单位间隔。
    public const float 页头高度 = 68;
    // 保存正文的设计关系，宽度变化时扩展换行行高，并把后续内容向下推。
    public static Action 重排正文(RectTransform 正文)
    {
        var 布局 = new 天帝正文重排(正文);
        var 文字 = 正文.GetComponentsInChildren<Text>(true); float 上次宽 = -1; int 上次签名 = 0;
        Action 更新 = () =>
        {
            int 签名 = 17;
            unchecked { foreach (var 文 in 文字) if (文 != null) 签名 = 签名 * 31 + (文.text?.GetHashCode() ?? 0) + 文.fontSize + (文.gameObject.activeSelf ? 1 : 0); }
            if (Mathf.Abs(上次宽 - 正文.rect.width) < .1f && 签名 == 上次签名) return;
            上次宽 = 正文.rect.width; 上次签名 = 签名; 布局.排版(正文.rect.width);
        };
        var 驱动 = 正文.GetComponent<天帝正文排版>() ?? 正文.gameObject.AddComponent<天帝正文排版>(); 驱动.更新 = 更新;
        return 更新;
    }
    public static void 移动页(RectTransform 框, Action<RectTransform> 排版)
    {
        if (!天帝移动适配.启用) return;
        天帝响应布局.比例(框, .02f, .02f, .96f, .96f);
        var 布局 = 框.gameObject.AddComponent<天帝移动排版>(); 布局.排版 = 排版;
    }
    public static void 固定(RectTransform 区, float 左, float 上, float 宽, float 高)
    {
        if (区 == null) return;
        var 定义 = 区.GetComponent<天帝比例矩形>(); if (定义 != null) 定义.待提交 = false;
        区.anchorMin = 区.anchorMax = 区.pivot = new Vector2(0, 1);
        区.anchoredPosition = new Vector2(左, -上); 区.sizeDelta = new Vector2(Mathf.Max(1, 宽), Mathf.Max(1, 高));
        区.localScale = Vector3.one;
    }
    public static void 区域(RectTransform 框, string 名, float 左, float 上, float 宽, float 高)
        => 固定(子区(框, 名), 左, 上, 宽, 高);
    public static RectTransform 子区(RectTransform 框, string 名)
    { foreach (RectTransform 子 in 框) if (子.name == 名) return 子; return null; }
    public static void 按键(RectTransform 键, float 左, float 上, float 宽, float 高 = 44)
    {
        if (键 == null) return;
        if (键.GetComponent<天帝按钮文字区域>()?.是关闭按钮 == true)
        { 左 -= Mathf.Max(0, 148 - 宽); 宽 = Mathf.Max(148, 宽); 高 = Mathf.Max(56, 高); }
        固定(键, 左, 上, 宽, 高);
        foreach (var 文 in 键.GetComponentsInChildren<Text>())
        { 天帝响应布局.比例(文.rectTransform, .04f, 0, .92f, 1); 文.fontSize = 16; 文.resizeTextForBestFit = false; }
        键.GetComponent<天帝按钮文字区域>()?.更新();
    }
    public static void 页头(RectTransform 框, string 返回)
    {
        var 键 = 框.Find(返回) as RectTransform;
        if (键 != null) 按键(键, 框.rect.width - 108, 4, 100);
        for (int i = 0; i < 框.childCount; i++)
        {
            var 文 = 框.GetChild(i).GetComponent<Text>();
            if (文 == null) continue;
            float 左=天帝青绿皮肤.已启用?20:12;
            固定(文.rectTransform, 左, 8, 框.rect.width - 180 - (左-12), 40); 文.fontSize = 22; 文.alignment = TextAnchor.MiddleLeft; break;
        }
    }
    public static RectTransform 滚动组(RectTransform 框, string 名, float 左, float 上, float 宽, float 高)
        => 天帝响应布局.滚动列(框, 名, 左, 上, 宽, 高);
    public static RectTransform 横列(RectTransform 父, string 名, RectTransform[] 控件, float 宽度, float 高度 = 44)
    {
        var 口 = 天帝响应布局.创建(父, 名, 0, 0, 100, 高度);
        var 图 = 口.gameObject.AddComponent<Image>(); 图.color = Color.clear; 口.gameObject.AddComponent<RectMask2D>();
        var 内容 = 天帝响应布局.创建(口, 名 + "内容", 0, 0, 控件.Length * (宽度 + 6), 高度);
        天帝响应布局.动态(内容);
        for (int i = 0; i < 控件.Length; i++)
        { 控件[i].SetParent(内容, false); 按键(控件[i], i * (宽度 + 6), 0, 宽度, 高度); }
        var 滚 = 口.gameObject.AddComponent<ScrollRect>(); 滚.viewport = 口; 滚.content = 内容;
        滚.horizontal = true; 滚.vertical = false; 滚.movementType = ScrollRect.MovementType.Clamped;
        return 口;
    }
}

public sealed class 天帝正文排版 : MonoBehaviour
{
    public Action 更新;
    float 上次检查;
    void LateUpdate() { if (Time.unscaledTime - 上次检查 < .15f) return; 上次检查 = Time.unscaledTime; 更新?.Invoke(); }
}

sealed class 天帝正文重排
{
    readonly RectTransform 根;
    readonly float 原宽, 原高;
    readonly System.Collections.Generic.List<项> 子 = new System.Collections.Generic.List<项>();
    sealed class 项 { public RectTransform 区; public float x, y, 宽, 高; public 天帝正文重排 内容; }
    public 天帝正文重排(RectTransform 根)
    {
        this.根 = 根; var 定义 = 根.GetComponent<天帝比例矩形>();
        原宽 = Mathf.Max(1, 定义 != null ? 定义.参考尺寸.x : 根.rect.width); 原高 = 定义 != null ? 定义.参考尺寸.y : 根.rect.height;
        foreach (RectTransform r in 根)
        {
            if (r.GetComponent<天帝比例矩形>()?.动态 == true || r.GetComponent<Scrollbar>() != null) continue;
            子.Add(new 项 { 区 = r, x = r.anchoredPosition.x - r.pivot.x * r.rect.width,
                y = -r.anchoredPosition.y - (1 - r.pivot.y) * r.rect.height,
                宽 = r.rect.width, 高 = r.rect.height, 内容 = r.childCount > 0 && r.GetComponent<Slider>() == null ? new 天帝正文重排(r) : null });
        }
        子.Sort((a, b) => a.y.CompareTo(b.y));
    }
    public float 排版(float 宽)
    {
        var 扩展 = new System.Collections.Generic.List<Vector2>();
        float 映射(float y) { float 值 = y; foreach (var e in 扩展) if (y >= e.x - .1f) 值 += e.y; return 值; }
        float 底 = 原高;
        foreach (var 项 in 子)
        {
            if (项.区 == null) continue;
            float 新宽 = Mathf.Max(1, 项.宽 / 原宽 * 宽);
            天帝双端页面布局.固定(项.区, 项.x / 原宽 * 宽, 映射(项.y), 新宽, 项.高);
            float 高 = 项.高;
            var 文 = 项.区.GetComponent<Text>();
            if (文 != null && !文.resizeTextForBestFit && !string.IsNullOrWhiteSpace(文.text)) 高 = Mathf.Max(高, Mathf.Ceil(文.preferredHeight) + 4);
            if (项.内容 != null) 高 = Mathf.Max(高, 项.内容.排版(新宽));
            if (项.区.GetComponent<Selectable>() != null) 高 = Mathf.Max(44, 高);
            if (高 > 项.高 + .1f) 扩展.Add(new Vector2(项.y + 项.高, 高 - 项.高));
            天帝双端页面布局.固定(项.区, 项.x / 原宽 * 宽, 映射(项.y), 新宽, 高);
            底 = Mathf.Max(底, 映射(项.y) + 高);
        }
        // 背景跨越多行时跟随新增的正文高度。
        foreach (var 项 in 子)
            if (项.区 != null && 项.区.GetComponent<Text>() == null && 项.区.childCount == 0 && 项.高 > 80 && 项.区.GetComponent<Selectable>() == null)
                天帝双端页面布局.固定(项.区, 项.x / 原宽 * 宽, 映射(项.y), 项.宽 / 原宽 * 宽, 映射(项.y + 项.高) - 映射(项.y));
        根.sizeDelta = new Vector2(根.sizeDelta.x, Mathf.Max(底, 映射(原高)));
        return 根.rect.height;
    }
}
