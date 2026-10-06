using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class 天帝界面
{
    public bool 作弊码已打开 { get; private set; }
    public void 显示作弊码()
    {
        if (游戏.阶段 != 游戏阶段.主页 || 确认已打开 || 设置已打开 || 地图已打开 || 角色已打开 || 图鉴已打开 || 宝盒已打开 || 回收已打开) return;
        关闭等级下拉(); 作弊码已打开 = 确认已打开 = true;
        foreach (var 控件 in 页面.GetComponentsInChildren<Selectable>()) 控件.interactable = false;
        var 遮 = 图(弹层, "作弊码遮罩", 0, 0, 1600, 900, new Color(.02f, .04f, .04f, .68f)); 遮.raycastTarget = true;
        var 框 = 图(弹层, "确认面板", 400, 220, 800, 460, 纸).rectTransform;
        if (天帝移动适配.启用) 天帝响应布局.比例(框, .075f, .08f, .85f, .84f);
        框.name = "作弊码面板";
        字(框, "作弊码", 40, 24, 720, 64, 32, 墨);
        字(框, "点击启用即可获得作弊效果", 55, 90, 690, 40, 22, 次墨);
        var 输入底 = 图(框, "作弊码输入", 80, 146, 640, 64, new Color(.88f, .9f, .84f)); 输入底.raycastTarget = true;
        var 输入 = 输入底.gameObject.AddComponent<InputField>(); 输入.targetGraphic = 输入底;
        var 文 = 字(输入底.rectTransform, "", 18, 0, 604, 64, 26, 墨); 文.alignment = TextAnchor.MiddleLeft; 文.supportRichText = false;
        var 占位 = 字(输入底.rectTransform, "请输入作弊码", 18, 0, 604, 64, 24, 次墨); 占位.alignment = TextAnchor.MiddleLeft;
        输入.textComponent = 文; 输入.placeholder = 占位; 输入.contentType = InputField.ContentType.Standard;
        输入.lineType = InputField.LineType.SingleLine; 输入.characterLimit = 32;
        输入.customCaretColor = true; 输入.caretColor = 墨; 输入.selectionColor = new Color(.35f, .7f, .58f, .35f);
        输入.text = "涌现";
        var 提示 = 字(框, "", 55, 226, 690, 102, 23, 次墨); 提示.name = "作弊码反馈";
        var 关闭键 = 按钮(框, "关闭", 95, 354, 270, 60, 关闭作弊码);
        关闭键.GetComponentInChildren<Text>().color = 天帝道纹美术.彩绘皮肤 ? 天帝道纹美术.正文 : 纸;
        var 启用键 = 按钮(框, "启用", 435, 354, 270, 60, () =>
        {
            bool 成功 = 游戏.兑换作弊码(输入.text, out string 结果);
            提示.text = 结果; 提示.color = 成功 ? 墨 : 朱;
            if (成功) { 更新主页实力(); if (主页灵石字 != null) 主页灵石字.text = "灵石  " + 游戏.宝盒数据.灵石显示; 关闭作弊码(); }
            else { EventSystem.current?.SetSelectedGameObject(输入.gameObject); 输入.ActivateInputField(); }
        }, true);
        EventSystem.current?.SetSelectedGameObject(启用键.gameObject);
    }
    public void 关闭作弊码()
    {
        if (!作弊码已打开) return;
        作弊码已打开 = 确认已打开 = false; 清空(弹层);
        foreach (var 控件 in 页面.GetComponentsInChildren<Selectable>()) 控件.interactable = true;
        EventSystem.current?.SetSelectedGameObject(null);
    }
}
