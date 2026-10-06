using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class 天帝移动摇杆 : MaskableGraphic, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Vector2 方向 { get; private set; }
    int 指针 = int.MinValue;
    public void OnPointerDown(PointerEventData e)
    { if (指针 != int.MinValue || e.button != PointerEventData.InputButton.Left) return; 指针 = e.pointerId; OnDrag(e); }
    public void OnDrag(PointerEventData e)
    {
        if (指针 != e.pointerId) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out var 点))
        {
            var 输入 = (点 - rectTransform.rect.center) / Mathf.Max(1, Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .39f);
            float 幅度 = 输入.magnitude;
            方向 = 幅度 <= .12f ? Vector2.zero : 输入.normalized * Mathf.Clamp01((幅度 - .12f) / .88f);
            SetVerticesDirty();
        }
    }
    public void OnPointerUp(PointerEventData e) { if (指针 == e.pointerId) 归零(); }
    void 归零() { 指针 = int.MinValue; 方向 = Vector2.zero; SetVerticesDirty(); }
    public void 重置输入() => 归零();
    void OnApplicationFocus(bool 焦点) { if (!焦点) 归零(); }
    void OnApplicationPause(bool 暂停) { if (暂停) 归零(); }
    protected override void OnDisable() { 归零(); base.OnDisable(); }
    protected override void OnPopulateMesh(VertexHelper 网)
    {
        网.Clear(); var 中 = rectTransform.rect.center;
        float 边 = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);
        圆(网, 中, 边 * .5f, new Color(0.04f, 0.09f, 0.10f, 0.65f));
        圆(网, 中, 边 * .46f, new Color(0.65f, 0.75f, 0.66f, 0.20f));
        圆(网, 中 + 方向 * 边 * .39f, 边 * .19f, new Color(0.90f, 0.81f, 0.57f, 0.9f));
    }
    static void 圆(VertexHelper 网, Vector2 中, float 半径, Color 色)
    {
        int 开 = 网.currentVertCount; 网.AddVert(中, 色, Vector2.zero);
        for (int i = 0; i <= 32; i++) { float a = i * Mathf.PI * 2 / 32; 网.AddVert(中 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 半径, 色, Vector2.zero); }
        for (int i = 0; i < 32; i++) 网.AddTriangle(开, 开 + i + 1, 开 + i + 2);
    }
}
