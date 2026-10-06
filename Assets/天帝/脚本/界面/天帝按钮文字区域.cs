using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 文字按素材实际的纯色纸面定位；九宫格边框与中段分别换算，不能只留固定像素。
[DefaultExecutionOrder(200)]
public sealed class 天帝按钮文字区域 : MonoBehaviour
{
    Image 背景;
    Selectable 控件;
    RectTransform 导航图标;
    readonly List<Text> 文字 = new List<Text>();
    Sprite 原图, 简图;
    Image.Type 原类型;
    float 原倍率;
    bool 导航, 简化;
    public bool 是关闭按钮 { get; private set; }
    int 上次签名;
    public Rect 纯色区域 { get; private set; }
    public static bool 是关闭入口(Selectable 键) => 键 != null && !键.name.Contains("遮罩") &&
        (键.name.StartsWith("关闭") || 键.name == "返回宝盒" || 键.name == "返回选择");

    public static void 绑定(Selectable 键, bool 有导航图标 = false)
    {
        if (键 == null || !(键.targetGraphic is Image 图)) return;
        var 区 = 键.GetComponent<天帝按钮文字区域>() ?? 键.gameObject.AddComponent<天帝按钮文字区域>();
        区.控件 = 键; 区.背景 = 图; 区.导航 = 有导航图标;
        区.导航图标 = null;
        foreach (Transform 子 in 键.transform)
            if (子.name.StartsWith("导航图标-")) { 区.导航图标 = 子 as RectTransform; 区.导航 = true; break; }
        区.是关闭按钮 = 是关闭入口(键);
        区.文字.Clear();
        // 卡片正文、道纹详情和下拉菜单的列表文字各自排版，仅处理控件的直接标签。
        foreach (Transform 子 in 键.transform)
            if (子.GetComponent<Text>() is Text 文) 区.文字.Add(文);
        区.记录素材(); 区.上次签名 = 0; 区.更新();
    }
    void 记录素材()
    { 原图 = 背景.sprite; 原类型 = 背景.type; 原倍率 = 背景.pixelsPerUnitMultiplier; 简化 = false; }

    Rect 素材纸面(Sprite 图)
    {
        if (图 != null && 图 == 天帝道纹美术.获取("按钮")) return Rect.MinMaxRect(.25f, .24f, .75f, .76f);
        if (图 != null && (图 == 天帝道纹美术.获取("确认按钮") || 图 == 天帝道纹美术.获取("取消按钮") || 图 == 天帝道纹美术.获取("返回按钮") || 图 == 天帝道纹美术.获取("选中按钮")))
            return Rect.MinMaxRect(.22f, .24f, .78f, .76f);
        var 美术 = 天帝美术资源.当前;
        if (图 != null && 图 == 美术?.获取("DWUI_D主页导航按钮")) return Rect.MinMaxRect(导航 ? .34f : .32f, .24f, .73f, .76f);
        if (图 != null && 图 == 美术?.获取("DWUI_D主页设置按钮")) return Rect.MinMaxRect(.20f, .25f, .75f, .70f);
        if (图 != null && 图 == 美术?.获取("DWUI_D主页主操作按钮")) return Rect.MinMaxRect(.18f, .24f, .82f, .76f);
        return Rect.MinMaxRect(导航 ? .34f : .04f, .08f, .96f, .92f);
    }
    static float 映射(float 比例, float 源长, float 前边, float 后边, float 长, float 每单位)
    {
        float 前 = 前边 / 每单位, 后 = 后边 / 每单位;
        float 缩 = Mathf.Min(1, 长 / Mathf.Max(.001f, 前 + 后)); 前 *= 缩; 后 *= 缩;
        float 点 = 比例 * 源长;
        if (点 <= 前边) return 前边 > 0 ? 点 / 前边 * 前 : 0;
        if (点 >= 源长 - 后边) return 长 - (源长 - 点) / Mathf.Max(.001f, 后边) * 后;
        return 前 + (点 - 前边) / Mathf.Max(.001f, 源长 - 前边 - 后边) * Mathf.Max(0, 长 - 前 - 后);
    }
    Rect 换算(Sprite 图, Image.Type 类型, float 倍率)
    {
        var r = 背景.rectTransform.rect; var 纸 = 素材纸面(图);
        if (图 != null && 图 == 天帝道纹美术.获取("小信息框"))
            return Rect.MinMaxRect(r.xMin + (导航 ? r.width * .34f : Mathf.Min(4, r.width * .04f)), r.yMin, r.xMax - Mathf.Min(4, r.width * .04f), r.yMax);
        if (图 == null || 类型 != Image.Type.Sliced || 图.border == Vector4.zero)
            return Rect.MinMaxRect(r.xMin + r.width * 纸.xMin, r.yMin + r.height * 纸.yMin, r.xMin + r.width * 纸.xMax, r.yMin + r.height * 纸.yMax);
        float 单位 = 背景.pixelsPerUnit * 倍率; var 边 = 图.border; var 源 = 图.rect.size;
        return Rect.MinMaxRect(r.xMin + 映射(纸.xMin, 源.x, 边.x, 边.z, r.width, 单位), r.yMin + 映射(纸.yMin, 源.y, 边.y, 边.w, r.height, 单位),
            r.xMin + 映射(纸.xMax, 源.x, 边.x, 边.z, r.width, 单位), r.yMin + 映射(纸.yMax, 源.y, 边.y, 边.w, r.height, 单位));
    }
    Rect 避开图标(Rect 区)
    {
        if (导航图标 == null || !导航图标.gameObject.activeSelf) return 区;
        float 右 = transform.InverseTransformPoint(导航图标.TransformPoint(导航图标.rect.max)).x;
        区.xMin = Mathf.Max(区.xMin, 右 + Mathf.Clamp(导航图标.rect.width * .25f, 8, 12));
        return 区;
    }
    public void 更新()
    {
        // 部分词条按钮先创建控件，再创建标签；首次更新补齐这类延迟标签。
        if (文字.Count == 0 && 控件 != null)
            foreach (Transform 子 in 控件.transform) if (子.GetComponent<Text>() is Text 文) 文字.Add(文);
        if (背景 == null || 文字.Count == 0 || 背景.rectTransform.rect.width <= 1) return;
        if (签名() == 上次签名) return;
        if (是关闭按钮)
        {
            var r = (RectTransform)transform; var 差 = Vector2.Max(Vector2.zero, new Vector2(148, 56) - r.rect.size);
            r.anchoredPosition -= Vector2.Scale(差, Vector2.one - r.pivot); r.sizeDelta += 差;
            var d = r.GetComponent<天帝比例矩形>(); if (d != null && d.待提交) d.参考尺寸 = r.rect.size;
            var 图 = 天帝道纹美术.获取("按钮");
            if (图 != null) { 背景.sprite = 图; 背景.type = Image.Type.Sliced; 背景.pixelsPerUnitMultiplier = 2; 背景.color = Color.white; }
            foreach (var 文 in 文字) { 文.text = "关闭"; 文.fontSize = 22; 文.color = 天帝道纹美术.正文; }
            记录素材();
        }
        if (背景.sprite != (简化 ? 简图 : 原图)) 记录素材();
        var 区 = 避开图标(换算(原图, 原类型, 原倍率));
        bool 窄 = false;
        foreach (var 文 in 文字)
        {
            if (文 == null || !文.gameObject.activeSelf || string.IsNullOrEmpty(文.text)) continue;
            var 设 = 文.GetGenerationSettings(new Vector2(10000, 10000));
            设.fontSize = 14; 设.resizeTextForBestFit = false; 设.horizontalOverflow = HorizontalWrapMode.Overflow;
            float 宽 = 文.cachedTextGeneratorForLayout.GetPreferredWidth(文.text, 设) / 文.pixelsPerUnit;
            float 高 = 文.cachedTextGeneratorForLayout.GetPreferredHeight(文.text, 设) / 文.pixelsPerUnit;
            float 可宽 = 控件 is Dropdown ? 区.width - 24 : 区.width;
            窄 |= 可宽 < 宽 + 4 || 区.height < 高 + 4;
        }
        // 极窄工具按钮使用已有的纯纸面皮肤，保证最小14号字完整，而非把文字挤进花纹。
        if (窄)
        {
            简图 = 天帝道纹美术.获取("小信息框");
            if (简图 != null) { 背景.sprite = 简图; 背景.type = Image.Type.Sliced; 背景.pixelsPerUnitMultiplier = 2; 简化 = true; }
        }
        else if (简化) { 背景.sprite = 原图; 背景.type = 原类型; 背景.pixelsPerUnitMultiplier = 原倍率; 简化 = false; }
        纯色区域 = 简化 ? 避开图标(换算(简图, 背景.type, 背景.pixelsPerUnitMultiplier)) : 区;
        foreach (var 文 in 文字)
        {
            if (文 == null) continue;
            var 标签区 = 纯色区域;
            标签区.xMin += 2; 标签区.xMax -= 2;
            if (背景.sprite != 天帝道纹美术.获取("小信息框")) { 标签区.yMin += 2; 标签区.yMax -= 2; }
            if (控件 is Dropdown 下拉)
            {
                if (文 == 下拉.captionText) 标签区.xMax -= 24;
                else 标签区.xMin = 标签区.xMax - 20;
            }
            var r = 文.rectTransform; var 父 = ((RectTransform)transform).rect;
            天帝响应布局.比例(r, (标签区.xMin - 父.xMin) / 父.width, (父.yMax - 标签区.yMax) / 父.height, 标签区.width / 父.width, 标签区.height / 父.height);
            文.alignment = 导航 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            文.horizontalOverflow = HorizontalWrapMode.Wrap; 文.verticalOverflow = VerticalWrapMode.Truncate;
            文.resizeTextForBestFit = true; 文.resizeTextMinSize = 14; 文.resizeTextMaxSize = Mathf.Max(14, 文.fontSize);
            if (简化) 文.color = 天帝道纹美术.纸面文字(文.color);
        }
        上次签名 = 签名();
    }
    int 签名()
    {
        unchecked
        {
            int 值 = 背景.rectTransform.rect.GetHashCode() * 31 + (背景.sprite != null ? 背景.sprite.GetInstanceID() : 0);
            值 = 值 * 31 + (int)背景.type; 值 = 值 * 31 + 背景.pixelsPerUnitMultiplier.GetHashCode();
            if (导航图标 != null)
            {
                值 = 值 * 31 + 导航图标.rect.GetHashCode();
                值 = 值 * 31 + 导航图标.localPosition.GetHashCode();
                值 = 值 * 31 + 导航图标.localScale.GetHashCode();
                值 = 值 * 31 + (导航图标.gameObject.activeSelf ? 1 : 0);
            }
            foreach (var 文 in 文字)
            {
                if (文 == null) continue; var r = 文.rectTransform;
                值 = 值 * 31 + (文.text?.GetHashCode() ?? 0); 值 = 值 * 31 + 文.fontSize;
                值 = 值 * 31 + (文.font != null ? 文.font.GetInstanceID() : 0); 值 = 值 * 31 + (int)文.fontStyle;
                值 = 值 * 31 + r.anchorMin.GetHashCode(); 值 = 值 * 31 + r.anchorMax.GetHashCode();
                值 = 值 * 31 + r.offsetMin.GetHashCode(); 值 = 值 * 31 + r.offsetMax.GetHashCode();
                值 = 值 * 31 + (int)文.alignment; 值 = 值 * 31 + (int)文.horizontalOverflow; 值 = 值 * 31 + (int)文.verticalOverflow;
                值 = 值 * 31 + (文.resizeTextForBestFit ? 1 : 0); 值 = 值 * 31 + 文.resizeTextMinSize; 值 = 值 * 31 + 文.resizeTextMaxSize;
            }
            return 值;
        }
    }
    void LateUpdate() => 更新();
}
