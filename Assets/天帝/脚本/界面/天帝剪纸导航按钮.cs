using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 导航的红色只属于悬停/按下状态，点击后的Selected不固化红色。
public sealed class 天帝剪纸导航按钮 : Button
{
    public Text 标签;
    public Graphic 图标;
    public bool 轻纸样式;
    bool 指针在内;
    static readonly Color 默认字 = new Color32(18,55,47,255);
    static readonly Color 交互字 = new Color32(246,230,192,255);
    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        var 显示态 = state == SelectionState.Selected ? (指针在内 ? SelectionState.Highlighted : SelectionState.Normal) : state;
        base.DoStateTransition(显示态, instant);
        if (标签 != null) 标签.color = !轻纸样式 && (显示态 == SelectionState.Highlighted || 显示态 == SelectionState.Pressed) ? 交互字 : 默认字;
        // 图标保留原画的纸面与墨线，不能把内部浅色一并染成实心块。
        if (图标 != null) 图标.color = Color.white;
    }
    public override void OnPointerEnter(PointerEventData eventData)
    { 指针在内 = true; base.OnPointerEnter(eventData); }
    public override void OnPointerExit(PointerEventData eventData)
    { 指针在内 = false; base.OnPointerExit(eventData); }
    protected override void OnDisable()
    { 指针在内 = false; base.OnDisable(); }
    public void 刷新文字() => DoStateTransition(currentSelectionState, true);
}
