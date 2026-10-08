using UnityEngine;
using UnityEngine.UI;

// 辅助窗口只加载独立的无字美术；标题、数值和交互仍由真实UGUI控件承载。
public static class 天帝辅助页山水
{
    public static Sprite 素材(string 名, string 回退 = "剪纸界面/弹窗纸框")
        => Resources.Load<Sprite>("山水剩余界面/" + 名) ?? Resources.Load<Sprite>(回退);

    public static void 纸(Image 图, string 名 = "弹窗纸框", string 回退 = "剪纸界面/弹窗纸框", bool 拉伸 = true)
    {
        if (图 == null) return;
        var 片 = 素材(名, 回退); if (片 == null) return;
        图.sprite = 片; 图.overrideSprite = null; 图.type = 拉伸 ? Image.Type.Sliced : Image.Type.Simple;
        图.pixelsPerUnitMultiplier = 2; 图.color = Color.white;
    }

    public static void 轻纸(Image 图, bool 选中 = false)
    {
        纸(图, "轻纸框", "剪纸界面/轻纸框");
        if (图 != null) 图.color = 选中 ? new Color(.82f, .91f, .81f) : Color.white;
    }

    public static void 按钮(Button 键, bool 主 = false, bool 已选 = false, bool 标签布局 = true)
    {
        if (键 == null) return;
        if (主) 纸(键.targetGraphic as Image, "墨绿按钮", "剪纸界面/墨绿按钮", false);
        else 轻纸(键.targetGraphic as Image, 已选);
        var 区 = 键.GetComponent<天帝按钮文字区域>(); if (区 != null) 区.enabled = false;
        var 色 = 键.colors; 色.normalColor = 色.disabledColor = 色.selectedColor = Color.white;
        色.highlightedColor = new Color(.93f, 1, .91f); 色.pressedColor = new Color(.82f, .9f, .8f); 键.colors = 色;
        var 组 = 键.GetComponent<CanvasGroup>(); if (组 == null) 组 = 键.gameObject.AddComponent<CanvasGroup>();
        组.alpha = 键.interactable ? 1 : .5f;
        foreach (Transform 子 in 键.transform)
        {
            var 文 = 子.GetComponent<Text>(); if (文 == null) continue;
            文.color = 主 ? new Color(.99f,.97f,.88f) : 天帝剪纸界面皮肤.墨;
            文.fontStyle = FontStyle.Normal;
            if (!标签布局) continue;
            天帝响应布局.比例(文.rectTransform, .08f, 0, .84f, 1);
            文.alignment = TextAnchor.MiddleCenter; 文.resizeTextForBestFit = true;
            文.resizeTextMinSize = 16; 文.resizeTextMaxSize = 文.fontSize;
        }
    }
}
