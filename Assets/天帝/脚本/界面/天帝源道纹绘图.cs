using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class 天帝源道纹绘图 : MaskableGraphic
{
    public int 编号;
    public bool 封印;
    public bool 天赋样式;
    public override Texture mainTexture => 天帝美术资源.已接入 ? 天帝美术资源.当前.道纹图集 : base.mainTexture;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); var c = rectTransform.rect.center; float r = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.44f;
        Color 金 = 封印 ? new Color(0.36f, 0.43f, 0.43f) : new Color(0.94f, 0.79f, 0.46f);
        if (天帝美术资源.已接入 && 天赋样式)
        {
            天帝美术资源.画图(vh, 1, c, Vector2.one * r * 2.2f, 封印 ? new Color(.35f, .35f, .35f) : Color.white);
            if (!封印) 天帝美术资源.画图(vh, 13 + 编号, c, Vector2.one * r, 金);
            return;
        }
        多边(vh, c + new Vector2(0, -7), r + 5, 8, new Color(0.025f, 0.035f, 0.04f), 22.5f);
        多边(vh, c, r + 6, 8, 封印 ? new Color(0.27f, 0.32f, 0.33f) : new Color(0.65f, 0.57f, 0.40f), 22.5f);
        多边(vh, c, r, 8, 封印 ? new Color(0.16f, 0.21f, 0.23f) : new Color(0.36f, 0.36f, 0.29f), 22.5f);
        for (int i = 0; i < 8; i++)
        {
            var a = c + 向(22.5f + i * 45) * (r - 9); var b = c + 向(22.5f + (i + 1) * 45) * (r - 9);
            线(vh, a, b, 2, 金);
        }
        // 已苏醒源纹只有右接口开放，其余五向以封印叉标记；整枚封印款另覆盖锁形。
        if (!天赋样式) 多边(vh, c, 15, 4, 金, 0);
        for (int i = 0; i < 6; i++)
        {
            var d = 向(i * 60); var 侧 = new Vector2(-d.y, d.x);
            if (!封印 && i > 0)
            {
                var 灰 = new Color(0.43f, 0.45f, 0.40f);
                线(vh, c + d * 39, c + d * 59, 2, 灰);
                线(vh, c + d * 65 - 侧 * 5 - d * 4, c + d * 65 + 侧 * 5 + d * 4, 2, 灰);
                线(vh, c + d * 65 - 侧 * 5 + d * 4, c + d * 65 + 侧 * 5 - d * 4, 2, 灰);
                continue;
            }
            线(vh, c + d * 23, c + d * 49, 5, 金);
            线(vh, c + d * 49, c + d * 62 + 侧 * (i % 2 == 0 ? 11 : -11), 4, 金);
            多边(vh, c + d * 69, 3, 4, 金, 0);
        }
        if (封印)
        {
            var 灰 = new Color(0.58f, 0.60f, 0.54f);
            多边(vh, c, 32, 8, new Color(0.065f, 0.10f, 0.12f), 22.5f);
            for (int i = 0; i < 12; i++)
                线(vh, c + new Vector2(Mathf.Cos(i * Mathf.PI / 12) * 14, Mathf.Sin(i * Mathf.PI / 12) * 14 + 7), c + new Vector2(Mathf.Cos((i + 1) * Mathf.PI / 12) * 14, Mathf.Sin((i + 1) * Mathf.PI / 12) * 14 + 7), 3, 灰);
            线(vh, c + new Vector2(-14, 7), c + new Vector2(-14, -14), 3, 灰);
            线(vh, c + new Vector2(14, 7), c + new Vector2(14, -14), 3, 灰);
            线(vh, c + new Vector2(-14, -14), c + new Vector2(14, -14), 3, 灰);
            线(vh, c, c + new Vector2(0, -7), 3, 灰);
        }
    }
    static Vector2 向(float 角) => new Vector2(Mathf.Cos(角 * Mathf.Deg2Rad), Mathf.Sin(角 * Mathf.Deg2Rad));
    static void 多边(VertexHelper vh, Vector2 c, float r, int 数, Color 色, float 角)
    {
        for (int j = 0; j < 数; j++)
        { int i = vh.currentVertCount; vh.AddVert(c, 色, 天帝美术资源.白点); vh.AddVert(c + 向(角 + j * 360f / 数) * r, 色, 天帝美术资源.白点); vh.AddVert(c + 向(角 + (j + 1) * 360f / 数) * r, 色, 天帝美术资源.白点); vh.AddTriangle(i, i + 1, i + 2); }
    }
    static void 线(VertexHelper vh, Vector2 a, Vector2 b, float w, Color 色)
    {
        var d = b - a; var n = new Vector2(-d.y, d.x).normalized * w * 0.5f; int i = vh.currentVertCount;
        vh.AddVert(a + n, 色, 天帝美术资源.白点); vh.AddVert(b + n, 色, 天帝美术资源.白点); vh.AddVert(b - n, 色, 天帝美术资源.白点); vh.AddVert(a - n, 色, 天帝美术资源.白点);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }
}
