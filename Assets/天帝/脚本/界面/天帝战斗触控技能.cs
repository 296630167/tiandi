using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 多指互不抢占：技能手势独立于左侧移动摇杆，失焦或暂停取消而不补发。
public sealed class 天帝战斗触控技能 : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
{
    public Action<Vector2> 瞄准;
    public Action 释放;
    public Func<bool> 可以操作;
    int 指针 = int.MinValue;
    Vector2 按下;
    public void OnPointerDown(PointerEventData e)
    {
        if (指针 != int.MinValue || e.button != PointerEventData.InputButton.Left || 可以操作?.Invoke() != true) return;
        指针 = e.pointerId; 按下 = e.position;
    }
    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != 指针) return;
        if (可以操作?.Invoke() != true) { 取消(); return; }
        var 向 = e.position - 按下;
        if (向.magnitude > Mathf.Max(10, Screen.height * .018f)) 瞄准?.Invoke(向.normalized);
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != 指针) return;
        OnDrag(e); if (指针 == int.MinValue) return;
        取消(); if (可以操作?.Invoke() == true) 释放?.Invoke();
    }
    public void OnCancel(BaseEventData e) => 取消();
    public void 取消() => 指针 = int.MinValue;
    void OnDisable() => 取消();
    void OnApplicationFocus(bool 有焦点) { if (!有焦点) 取消(); }
    void OnApplicationPause(bool 暂停) { if (暂停) 取消(); }
}
