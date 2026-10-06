using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;

public partial class 天帝界面
{
    Dropdown 地图等级下拉;
    Text 主页地图等级字;
    void 显示常驻选图()
    {
        var 区 = 区块(页面, "主页常驻选图", 1046, 168, 532, 686);
        var 青 = new Color(.20f, .62f, .63f);
        Sprite 地图图 = null;
        // 大图直接引用已经接入的资源；不创建额外生图或重复纹理。
        if (游戏.美术 != null) 地图图 = 游戏.美术.获取("青岚原") ?? 游戏.美术.获取("MAP01");
        if (地图图 == null && 游戏.美术 != null) 地图图 = 游戏.美术.获取("BG01");
        for (int i = 0; i < 3; i++)
        {
            bool 开放 = i == 0;
            var 边 = 区块(区, "地图卡" + (i + 1), i * 181, 0, 170, 188);
            if (!开放 && !天帝移动适配.启用)
            { 边.localScale = Vector3.one * .88f; 边.anchoredPosition += new Vector2(10, -12); }
            var 裁 = 区块(边, "方形图片裁切", 17, 17, 141, 141); 裁.gameObject.AddComponent<RectMask2D>();
            Sprite 图像 = 开放 ? 地图图 : 游戏.美术?.获取(i == 1 ? "BG03" : "BG01");
            var 像 = 图(裁, "地图图", 0, 0, 141, 141, 开放 ? Color.white : new Color(.53f, .60f, .58f), 图像);
            if (图像 != null)
            {
                var 比例 = 像.gameObject.AddComponent<AspectRatioFitter>(); 比例.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                比例.aspectRatio = 图像.rect.width / 图像.rect.height;
            }
            if (开放)
                主页面板(边, "地图名称纸面", "地图名称绢纸", 20, 144, 136, 35);
            else
                图(边, "封印地图名称遮罩", 17, 145, 141, 34, new Color(.055f, .16f, .18f, .91f));
            主页字(边, 开放 ? "青岚原" : i == 1 ? "未知秘境" : "未知禁地", 19, 144, 139, 37, 23, 开放 ? 主页石青字 : 天帝道纹美术.浅字, true);
            var 卡框 = 主页面板(边, "地图卡金框", "地图卡框", 0, 0, 170, 188);
            if (开放)
            {
                var 选 = 边.gameObject.AddComponent<Button>(); 天帝按钮声音.绑定(选); 选.targetGraphic = 卡框; 卡框.raycastTarget = true;
                选.onClick.AddListener(() => 游戏.选择战斗地图(0));
                图(边, "地图已选高亮", 128, 18, 27, 27, new Color(.09f, .44f, .48f, .96f));
                var 勾 = 图(边, "地图已选图标", 129, 18, 25, 25, Color.white, 天帝道纹美术.获取("已装备"));
                勾.preserveAspect = true;
            }
            else
            {
                // 无可选择控件，关闭其他弹窗也不会把锁定地图重新启用。
                主页面板(边, "封印图标", "封印纹章", 49, 48, 74, 69);
                主页字(边, "封印", 30, 62, 110, 39, 25, 天帝道纹美术.浅字, true);
                主页字(边, "尚未开放", 24, 111, 122, 27, 18, 天帝道纹美术.浅字);
            }
        }
        var 框 = 主页面板(区, "地图信息面板", "地图卷轴", 0, 202, 532, 484).rectTransform;
        主页字(框, "青岚原", 52, 42, 400, 62, 42, 主页石青字, true).alignment = TextAnchor.MiddleLeft;
        var 简介 = 主页字(框, "击退狼群，挑战青岚狼王。", 52, 118, 430, 42, 23, 主页石青字, true); 简介.alignment = TextAnchor.MiddleLeft;
        主页字(框, "战斗与掉落详情将在进入前展示", 52, 165, 430, 36, 18, 主页灰墨).alignment = TextAnchor.MiddleLeft;
        图(框, "内容分隔", 52, 222, 426, 1, new Color(.62f, .44f, .16f, .23f));
        主页字(框, "地图等级", 52, 242, 132, 46, 23, 主页灰墨, true).alignment = TextAnchor.MiddleLeft;
        地图等级下拉 = 创建等级下拉(框, 207, 244, 272, 46);
        地图等级下拉.SetValueWithoutNotify(游戏.当前地图等级 - 1);
        地图等级下拉.RefreshShownValue();
        主页地图等级字 = 主页字(框, "掉落物品等级随地图等级提升", 52, 304, 430, 36, 18, 主页灰墨);
        主页地图等级字.alignment = TextAnchor.UpperLeft;
        地图等级下拉.onValueChanged.AddListener(序号 =>
        {
            if (!游戏.选择地图等级(序号 + 1)) { 地图等级下拉.SetValueWithoutNotify(游戏.当前地图等级 - 1); 地图等级下拉.RefreshShownValue(); return; }
            主页地图等级字.text = "掉落物品等级随地图等级提升";
        });
        var 开始 = 主页按钮(框, "开始游戏", "主操作按钮", 70, 366, 390, 88, 请求进入地图, 33, 天帝道纹美术.浅字);
        var 开始字 = 开始.GetComponentInChildren<Text>(); 开始字.rectTransform.anchoredPosition = new Vector2(42, -5); 开始字.rectTransform.sizeDelta = new Vector2(306, 76);
        if (天帝移动适配.启用)
        {
            天帝响应布局.比例(地图等级下拉.GetComponent<RectTransform>(), .39f, .50f, .51f, .23f);
            天帝响应布局.比例(开始.GetComponent<RectTransform>(), .13f, .75f, .74f, .23f);
            主页地图等级字.gameObject.SetActive(false);
        }
    }
    Dropdown 创建等级下拉(RectTransform 父, float x, float y, float 宽, float 高)
    {
        var 底 = 主页面板(父, "地图等级下拉", "主操作按钮", x, y, 宽, 高); 底.raycastTarget = true;
        var 下拉 = 底.gameObject.AddComponent<天帝等级下拉>(); 下拉.targetGraphic = 底;
        var 文 = 主页字(底.rectTransform, "", 34, 0, 宽 - 68, 高, 24, 天帝道纹美术.浅字); 文.alignment = TextAnchor.MiddleCenter;
        主页字(底.rectTransform, "▼", 宽 - 43, 0, 26, 高, 17, 天帝道纹美术.浅字); 下拉.captionText = 文;
        天帝按钮文字区域.绑定(下拉);
        var 列表 = 图(底.rectTransform, "Template", 0, 0, 宽, 264, new Color(.055f, .10f, .10f, .99f)).rectTransform;
        var 列表皮肤=图(列表,"等级菜单素材",0,0,宽,264,Color.white);天帝界面美术.面板(列表皮肤,"二级面板",Color.white);列表皮肤.raycastTarget=false;
        列表.anchorMin = 列表.anchorMax = new Vector2(0, 1); 列表.pivot = new Vector2(0, 0); 列表.anchoredPosition = new Vector2(0, 8);
        天帝响应布局.动态(列表); 列表.anchorMax = Vector2.one; 列表.sizeDelta = new Vector2(0, 264);
        天帝响应布局.比例(列表皮肤.rectTransform, 0, 0, 1, 1);
        var 视口 = 铺满(列表, "Viewport"); 视口.offsetMin = new Vector2(4, 4); 视口.offsetMax = new Vector2(-16, -4);
        var 口像 = 视口.gameObject.AddComponent<Image>(); 口像.color = new Color(0, 0, 0, .01f); 视口.gameObject.AddComponent<RectMask2D>();
        var 内容 = 区块(视口, "Content", 0, 0, 宽 - 20, 48);
        内容.anchorMin = new Vector2(0, 1); 内容.anchorMax = Vector2.one; 内容.sizeDelta = new Vector2(0, 48);
        var 项 = 区块(内容, "Item", 0, 0, 宽 - 20, 48); 项.anchorMin = new Vector2(0, 1); 项.anchorMax = Vector2.one; 项.sizeDelta = new Vector2(0, 48);
        var 项底 = 项.gameObject.AddComponent<Image>(); 项底.color = 天帝道纹美术.行底; 项底.raycastTarget = true;
        var 勾 = 图(项, "选中高亮", 0, 0, 宽 - 20, 48, new Color(.89f, .78f, .47f, .6f));
        天帝响应布局.比例(勾.rectTransform, 0, 0, 1, 1);
        var 开关 = 项.gameObject.AddComponent<Toggle>(); 开关.targetGraphic = 项底; 开关.graphic = 勾;
        var 项文 = 字(项, "1级", 0, 0, 宽 - 20, 48, 23, 纸); 项文.alignment = TextAnchor.MiddleCenter;
        项文.rectTransform.anchorMin = Vector2.zero; 项文.rectTransform.anchorMax = Vector2.one;
        项文.rectTransform.offsetMin = new Vector2(8, 0); 项文.rectTransform.offsetMax = new Vector2(-8, 0);
        var 滚动 = 列表.gameObject.AddComponent<ScrollRect>(); 滚动.content = 内容; 滚动.viewport = 视口;
        滚动.horizontal = false; 滚动.vertical = true; 滚动.movementType = ScrollRect.MovementType.Clamped; 滚动.scrollSensitivity = 32;
        var 滑 = 图(列表, "Scrollbar", 宽 - 11, 4, 7, 256, new Color(.18f, .27f, .23f));
        滑.rectTransform.anchorMin = new Vector2(1, 0); 滑.rectTransform.anchorMax = Vector2.one;
        滑.rectTransform.offsetMin = new Vector2(-11, 4); 滑.rectTransform.offsetMax = new Vector2(-4, -4);
        var 柄 = 铺满(滑.rectTransform, "Handle"); var 柄图 = 柄.gameObject.AddComponent<Image>(); 柄图.color = new Color(.49f, .76f, .62f);
        var 条 = 滑.gameObject.AddComponent<Scrollbar>(); 条.direction = Scrollbar.Direction.BottomToTop; 条.handleRect = 柄; 条.targetGraphic = 柄图;
        滚动.verticalScrollbar = 条;
        下拉.template = 列表; 下拉.itemText = 项文;
        var 选项 = new List<string>(); for (int i = 1; i <= 100; i++) 选项.Add(i + "级"); 下拉.AddOptions(选项);
        列表.gameObject.SetActive(false); return 下拉;
    }
    public bool 关闭等级下拉()
    {
        if (地图等级下拉 == null) return false;
        var 展开 = 地图等级下拉.transform.Find("Dropdown List");
        if (展开 == null || !展开.gameObject.activeSelf) return false;
        地图等级下拉.Hide(); return true;
    }
    void 清理主页选图() { 关闭等级下拉(); 地图等级下拉 = null; 主页地图等级字 = null; }
    public void 请求进入地图()
    {
        if (游戏.阶段 != 游戏阶段.主页) return;
        关闭等级下拉();
        int 等级 = 游戏.当前地图等级;
        显示确认("确定进入青岚原吗？", 天帝地图挑战.进入说明(等级), "确认进入", () =>
        {
            if (游戏.当前地图编号 == 0 && 游戏.当前地图等级 == 等级) 游戏.进入战斗();
        }, 600);
        var 说明 = 弹层.GetComponentsInChildren<Text>().FirstOrDefault(x => x.text == 天帝地图挑战.进入说明(等级));
        if (说明 != null)
        {
            string 原文 = 说明.text; int 行尾 = 原文.IndexOf('\n');
            说明.text = "<b><size=26>" + 原文.Substring(0, 行尾) + "</size></b>\n<b>目标：击败青岚狼王，保留本局拾取。</b>\n\n" + 原文.Substring(行尾 + 1);
            说明.fontSize = 18; 说明.alignment = TextAnchor.UpperLeft; 说明.lineSpacing = 1.1f;
        }
    }
}

// 展开后将列表滚到当前选项，100级不需要每次重新从第1项开始找。
public sealed class 天帝等级下拉 : Dropdown
{
    public override void OnPointerClick(PointerEventData e) { base.OnPointerClick(e); StartCoroutine(定位当前项()); }
    public override void OnSubmit(BaseEventData e) { base.OnSubmit(e); StartCoroutine(定位当前项()); }
    IEnumerator 定位当前项()
    {
        yield return null;
        var 列表 = transform.Find("Dropdown List"); if (列表 == null) yield break;
        Canvas.ForceUpdateCanvases(); var 滚 = 列表.GetComponent<ScrollRect>(); if (滚 == null) yield break;
        var 项 = 列表.GetComponentsInChildren<Toggle>().Where(t=>t.gameObject.activeSelf).ToArray();
        if (value < 0 || value >= 项.Length) yield break;
        var 目标 = (RectTransform)项[value].transform;
        float 内容高 = 滚.content.rect.height, 口高 = 滚.viewport.rect.height;
        if (内容高 <= 口高) yield break;
        var 包围 = RectTransformUtility.CalculateRelativeRectTransformBounds(滚.content, 目标);
        float 中 = 滚.content.rect.yMax - 包围.center.y;
        滚.verticalNormalizedPosition = 1 - Mathf.Clamp01((中 - 口高 * .5f) / (内容高 - 口高));
    }
}
