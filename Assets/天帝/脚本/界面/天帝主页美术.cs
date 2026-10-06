using System;
using UnityEngine;
using UnityEngine.UI;

// 主页按已选 D 概念保留完整造型，不经过通用小控件的九宫格缩边。
public partial class 天帝界面
{
    static readonly Color 主页石青字 = new Color(.075f, .29f, .36f);
    static readonly Color 主页灰墨 = new Color(.38f, .40f, .36f);
    Sprite 主页素材(string 名) => 游戏.美术?.获取("DWUI_D主页" + 名);
    Image 主页面板(RectTransform 父, string 名, string 素材, float x, float y, float 宽, float 高)
    {
        var 像 = 图(父, 名, x, y, 宽, 高, Color.white, 主页素材(素材));
        像.type = Image.Type.Simple; 像.pixelsPerUnitMultiplier = 1;
        return 像;
    }
    Text 主页字(RectTransform 父, string 内容, float x, float y, float 宽, float 高, int 大小, Color 色, bool 标题 = false)
    {
        var 文 = 字(父, 内容, x, y, 宽, 高, 大小, 色); 文.color = 色;
        if (标题)
        {
            var 字体 = 游戏.美术?.主页标题字体;
            if (字体 != null) 文.font = 字体;
            else 文.fontStyle = FontStyle.Bold;
        }
        return 文;
    }
    Button 主页按钮(RectTransform 父, string 名, string 素材, float x, float y, float 宽, float 高, Action 点击, int 大小, Color 字色)
    {
        var 键 = 按钮(父, 名, x, y, 宽, 高, 点击);
        var 像 = (Image)键.targetGraphic;
        var 资源 = 主页素材(素材);
        if (资源 != null) { 像.sprite = 资源; 像.type = Image.Type.Simple; 像.pixelsPerUnitMultiplier = 1; }
        // D主页保留完整轮廓，紧凑控件的细边仅用于其它页面。
        foreach(var 名称 in new[]{"控件细边","控件选择线"}){var 装饰=键.transform.Find(名称);if(装饰!=null)装饰.gameObject.SetActive(false);}
        var 文 = 键.GetComponentInChildren<Text>(); 文.color = 字色; 文.fontSize = 大小;
        if (游戏.美术?.主页标题字体 != null) 文.font = 游戏.美术.主页标题字体;
        else 文.fontStyle = FontStyle.Bold;
        天帝按钮文字区域.绑定(键);
        return 键;
    }
    void 主页导航(string 名, string 图标, int 行, Action 点击)
    {
        bool 次级 = 行 >= 3;
        float 宽 = 次级 ? 268 : 296, 高 = 次级 ? 82 : 92;
        float y = 行 < 3 ? 62 + 行 * 98 : 376 + (行 - 3) * 88;
        var 键 = 主页按钮(页面, 名, "导航按钮", 次级 ? 42 : 28, y, 宽, 高, 点击, 次级 ? 25 : 30, 主页石青字);
        var 阴影 = 键.gameObject.AddComponent<Shadow>();
        阴影.effectColor = new Color(.04f, .17f, .20f, .26f); 阴影.effectDistance = new Vector2(0, -4);
        阴影.useGraphicAlpha = true;
        var 文 = 键.GetComponentInChildren<Text>();
        文.rectTransform.anchoredPosition = new Vector2(次级 ? 108 : 120, -16); 文.rectTransform.sizeDelta = new Vector2(宽 - (次级 ? 128 : 142), 52);
        文.alignment = TextAnchor.MiddleLeft;
        var 图标像 = 主页面板((RectTransform)键.transform, "导航图标-" + 名, "图标" + 图标, 次级 ? 61 : 69, 次级 ? 22 : 24, 次级 ? 38 : 44, 次级 ? 38 : 44);
        图标像.preserveAspect = true;
        天帝按钮文字区域.绑定(键, true);
    }
}
