using UnityEngine;
using UnityEngine.UI;

// 只扩大空隙中的点击范围，遇相邻按钮在间隙中线止步，避免串选。
public sealed class 天帝触控热区 : MonoBehaviour
{
    Graphic 图;
    float 上次比例 = -1;
    Vector2 上次尺寸;
    int 上次数量 = -1;
    void OnEnable() { 图 = GetComponent<Graphic>(); 上次比例 = -1; }
    void LateUpdate() => 更新();
    public void 更新()
    {
        if (!天帝移动适配.启用) return;
        if (图 == null) 图 = GetComponent<Graphic>();
        if (图 == null) return;
        var canvas = 图.canvas; if (canvas == null) return;
        float 比例 = Mathf.Abs(图.rectTransform.lossyScale.x) * canvas.scaleFactor / 天帝移动适配.像素密度;
        int 数量 = transform.parent != null ? transform.parent.childCount : 0;
        if (Mathf.Abs(上次比例 - 比例) < .001f && 上次尺寸 == 图.rectTransform.rect.size && 上次数量 == 数量) return;
        上次数量 = 数量;
        上次比例 = 比例; 上次尺寸 = 图.rectTransform.rect.size;
        float 横 = Mathf.Max(0, (44 / Mathf.Max(.01f, 比例) - 上次尺寸.x) * .5f);
        float 竖 = Mathf.Max(0, (44 / Mathf.Max(.01f, 比例) - 上次尺寸.y) * .5f);
        float 左 = 横, 右 = 横, 下 = 竖, 上 = 竖;
        var 父 = transform.parent as RectTransform;
        if (父 != null)
        {
            Rect 范围(RectTransform 区)
            {
                var a = 父.InverseTransformPoint(区.TransformPoint(区.rect.min));
                var b = 父.InverseTransformPoint(区.TransformPoint(区.rect.max));
                return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
            }
            var 本 = 范围(图.rectTransform);
            for (int i = 0; i < 父.childCount; i++)
            {
                var 他 = 父.GetChild(i) as RectTransform;
                if (他 == 图.rectTransform || 他.GetComponent<Selectable>() == null) continue;
                var 邻 = 范围(他);
                if (邻.yMin < 本.yMax && 邻.yMax > 本.yMin)
                {
                    if (邻.xMax <= 本.xMin) 左 = Mathf.Min(左, (本.xMin - 邻.xMax) * .5f);
                    if (邻.xMin >= 本.xMax) 右 = Mathf.Min(右, (邻.xMin - 本.xMax) * .5f);
                }
                if (邻.xMin < 本.xMax && 邻.xMax > 本.xMin)
                {
                    if (邻.yMax <= 本.yMin) 下 = Mathf.Min(下, (本.yMin - 邻.yMax) * .5f);
                    if (邻.yMin >= 本.yMax) 上 = Mathf.Min(上, (邻.yMin - 本.yMax) * .5f);
                }
            }
        }
        图.raycastPadding = new Vector4(-左, -下, -右, -上);
    }
}
