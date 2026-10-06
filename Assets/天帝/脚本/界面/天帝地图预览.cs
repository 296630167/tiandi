using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class 天帝地图预览 : MaskableGraphic
{
    public Vector2? 玩家位置;
    public Vector2? BOSS位置;
    public 天帝战斗地图 当前地图;
    static 天帝战斗地图 预览缓存;
    // 主页预览与战斗使用同一新版通行图，不在类型初始化时生成旧拼块地图。
    static 天帝战斗地图 示意地图 => 预览缓存 ??= new 天帝战斗地图(0, false, false, 天帝美术资源.当前.青岚原生存通行图, 1, true);
    Image 玩家标记, 王标记;
    Sprite 已显示图;
    Sprite 长卷图 => 当前地图 == null || 当前地图.生存大图 ? 天帝美术资源.当前?.青岚原生存大图 : 当前地图.长卷布局 ? 天帝美术资源.当前?.青岚原长卷 : null;
    public override Texture mainTexture => 长卷图 != null ? 长卷图.texture : base.mainTexture;
    void LateUpdate()
    {
        if (已显示图 != 长卷图) { 已显示图 = 长卷图; SetMaterialDirty(); SetVerticesDirty(); }
        if (玩家标记 == null)
        {
            玩家标记 = 建标记("小地图玩家", new Color(1, .84f, .43f));
            王标记 = 建标记("小地图BOSS", new Color(.94f, .35f, .26f));
        }
        var 地图 = 当前地图 ?? 示意地图; Rect 区 = rectTransform.rect;
        float 格 = Mathf.Min(区.width / 地图.地块宽, 区.height / 地图.地块高);
        var 起点 = 区.center - new Vector2(地图.地块宽, 地图.地块高) * 格 / 2;
        void 定位(Image 标, Vector2 点, float 边)
        {
            var 大小 = new Vector2(地图.地块宽, 地图.地块高) * 格;
            var 偏 = (点 + new Vector2(地图.半宽, 地图.半高)) / 天帝战斗地图.格边长 * 格;
            偏.x = Mathf.Clamp(偏.x, 边 * .5f, 大小.x - 边 * .5f); 偏.y = Mathf.Clamp(偏.y, 边 * .5f, 大小.y - 边 * .5f);
            // 子控件锚在父矩形中心，须从局部绘制坐标扣除rect.center，兼容左上轴心。
            标.rectTransform.anchoredPosition = 起点 + 偏 - 区.center;
            标.rectTransform.sizeDelta = Vector2.one * 边;
        }
        定位(玩家标记, 玩家位置 ?? 地图.出生位置, 玩家位置.HasValue ? 5 : 7);
        王标记.gameObject.SetActive(!地图.生存大图 || BOSS位置.HasValue);
        if (王标记.gameObject.activeSelf) 定位(王标记, BOSS位置 ?? 地图.王位置, 7);
    }
    Image 建标记(string 名, Color 色)
    {
        var 物 = new GameObject(名, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        物.transform.SetParent(transform, false); var 标 = 物.GetComponent<Image>(); 标.color = 色; 标.raycastTarget = false;
        标.rectTransform.anchorMin = 标.rectTransform.anchorMax = 标.rectTransform.pivot = Vector2.one * .5f;
        天帝响应布局.动态(标.rectTransform);
        物.AddComponent<Outline>().effectColor = new Color(.08f, .1f, .12f); return 标;
    }
    protected override void OnPopulateMesh(VertexHelper 网)
    {
        网.Clear(); var 地图 = 当前地图 ?? 示意地图; Rect 区 = rectTransform.rect;
        float 格 = Mathf.Min(区.width / 地图.地块宽, 区.height / 地图.地块高);
        var 起点 = 区.center - new Vector2(地图.地块宽, 地图.地块高) * 格 / 2;
        float 间隙 = 地图.精细碰撞 ? 0 : .5f;
        if (长卷图 != null)
        {
            var 大小 = new Vector2(地图.地块宽, 地图.地块高) * 格;
            网.AddVert(起点, Color.white, Vector2.zero); 网.AddVert(起点 + new Vector2(0, 大小.y), Color.white, Vector2.up);
            网.AddVert(起点 + 大小, Color.white, Vector2.one); 网.AddVert(起点 + new Vector2(大小.x, 0), Color.white, Vector2.right);
            网.AddTriangle(0, 1, 2); 网.AddTriangle(0, 2, 3);
        }
        else for (int y = 0; y < 地图.地块高; y++) for (int x = 0; x < 地图.地块宽; x++)
            方块(网, 起点 + new Vector2(x, y) * 格 + Vector2.one * (间隙 * .5f), Vector2.one * (格 - 间隙), 地图.地块颜色(x, y));
    }
    static void 方块(VertexHelper 网, Vector2 点, Vector2 大小, Color 色)
    {
        int 开 = 网.currentVertCount;
        网.AddVert(点, 色, Vector2.zero); 网.AddVert(点 + new Vector2(0, 大小.y), 色, Vector2.zero);
        网.AddVert(点 + 大小, 色, Vector2.zero); 网.AddVert(点 + new Vector2(大小.x, 0), 色, Vector2.zero);
        网.AddTriangle(开, 开 + 1, 开 + 2); 网.AddTriangle(开, 开 + 2, 开 + 3);
    }
}
