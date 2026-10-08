using UnityEngine;
using UnityEngine.UI;

// 仅用于瓷牌中央的单字。按生成后的字形边界居中，避免字体基线导致偏上。
[RequireComponent(typeof(Text))]
public sealed class 天帝道纹单字 : BaseMeshEffect
{
    public static void 绑定(Text 字, RectTransform 瓷牌 = null)
    {
        字.alignment = TextAnchor.MiddleCenter;
        字.raycastTarget = false;
        字.horizontalOverflow = HorizontalWrapMode.Overflow;
        字.verticalOverflow = VerticalWrapMode.Overflow;
        // 中央字随瓷牌伸展；不交给手机正文的逐行重排或延后比例提交。
        天帝响应布局.动态(字.rectTransform);
        if (瓷牌 != null)
        {
            var 区 = 字.rectTransform;
            区.SetParent(瓷牌, false);
            区.anchorMin = Vector2.zero; 区.anchorMax = Vector2.one;
            区.pivot = new Vector2(.5f, .5f);
            区.offsetMin = 区.offsetMax = Vector2.zero;
        }
        if (字.GetComponent<天帝道纹单字>() == null) 字.gameObject.AddComponent<天帝道纹单字>();
    }

    public override void ModifyMesh(VertexHelper 网)
    {
        if (!IsActive() || 网.currentVertCount == 0) return;
        var 最小 = new Vector2(float.MaxValue, float.MaxValue);
        var 最大 = new Vector2(float.MinValue, float.MinValue);
        var 顶点 = default(UIVertex);
        bool 有字形 = false;
        // 排除TextGenerator可能保留的零面积尾四边形。
        for (int i = 0; i + 3 < 网.currentVertCount; i += 4)
        {
            网.PopulateUIVertex(ref 顶点, i); var 左下 = (Vector2)顶点.position;
            网.PopulateUIVertex(ref 顶点, i + 2); var 右上 = (Vector2)顶点.position;
            if ((左下 - 右上).sqrMagnitude < .001f) continue;
            有字形 = true;
            for (int j = 0; j < 4; j++)
            {
                网.PopulateUIVertex(ref 顶点, i + j);
                最小 = Vector2.Min(最小, 顶点.position); 最大 = Vector2.Max(最大, 顶点.position);
            }
        }
        if (!有字形) return;
        Vector3 偏移 = graphic.rectTransform.rect.center - (最小 + 最大) * .5f;
        for (int i = 0; i < 网.currentVertCount; i++)
        {
            网.PopulateUIVertex(ref 顶点, i); 顶点.position += 偏移; 网.SetUIVertex(顶点, i);
        }
    }
}
