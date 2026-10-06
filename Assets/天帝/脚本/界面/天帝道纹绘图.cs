using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class 天帝道纹绘图 : MaskableGraphic
{
    public 天帝道纹 数据;
    public 道纹实例 单纹;
    public bool 单纹模式;
    public bool 幽灵;
    public bool 候选暗;
    public bool 构筑美术;
    public float 单纹半径 = 44;
    public Vector2 平移;
    public float 缩放 = 1;
    public Vector2Int? 预览格;
    public bool 预览可放;
    public 道纹实例 拖动纹;
    public int 强调通路;
    public static readonly Color[] 通路颜色 = { new Color(.35f,.92f,.73f), new Color(.70f,.70f,1), new Color(.95f,.63f,.80f), new Color(.97f,.74f,.40f), new Color(.47f,.77f,1), new Color(.75f,.90f,.43f) };
    public readonly List<Vector2Int> 高亮路径 = new List<Vector2Int>();
    public readonly HashSet<Vector2Int> 预览亮起 = new HashSet<Vector2Int>(), 预览暗掉 = new HashSet<Vector2Int>();
    public int 可见格数 { get; private set; }
    bool 新图集 => (构筑美术 || 天帝道纹美术.彩绘皮肤) && 天帝美术资源.当前 != null && 天帝美术资源.当前.道纹构筑图集 != null;
    Vector2 白点 => 新图集 ? new Vector2(2f / 2048, 2f / 2048) : 天帝美术资源.白点;
    public override Texture mainTexture => 新图集 ? 天帝美术资源.当前.道纹构筑图集 : 天帝美术资源.已接入 ? 天帝美术资源.当前.道纹图集 : base.mainTexture;
    public static readonly Color 属性色 = new Color(0.30f, 0.82f, 0.70f);
    public static readonly Color 源色 = new Color(1f, 0.83f, 0.40f);
    static readonly Color 接口深青 = new Color(.055f, .24f, .29f);
    static readonly Color 接口浅边 = new Color(.94f, .97f, .86f);
    static readonly Color 链路金 = new Color(1f, .74f, .22f);
    static readonly Color 链路描边 = new Color(.10f, .20f, .22f);
    static readonly int[] 数字段 = { 63, 6, 91, 79, 102, 109, 125, 7, 127, 111 };
    public static Color 分类色(道纹实例 纹) => 纹.是天赋 ? 源色 : 属性色;
    public static Color 品阶色(道纹实例 纹) => 纹.是天赋 ? 源色 : 天帝道纹品阶.获取(纹.品阶).颜色;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); 可见格数 = 0;
        if (单纹模式)
        {
            if (单纹 != null) 画纹(vh, rectTransform.rect.center, Mathf.Min(单纹半径, Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .46f), 单纹, !候选暗, 幽灵 ? 0.8f : 1);
            return;
        }
        if (数据 == null) return;
        var 区 = rectTransform.rect;
        var 左下 = 天帝道纹.位置格((区.min - 平移) / 缩放);
        var 右上 = 天帝道纹.位置格((区.max - 平移) / 缩放);
        var 左上 = 天帝道纹.位置格((new Vector2(区.xMin, 区.yMax) - 平移) / 缩放);
        var 右下 = 天帝道纹.位置格((new Vector2(区.xMax, 区.yMin) - 平移) / 缩放);
        int qMin = Mathf.Max(-50, Mathf.Min(左下.x, 右上.x, 左上.x, 右下.x) - 2);
        int qMax = Mathf.Min(49, Mathf.Max(左下.x, 右上.x, 左上.x, 右下.x) + 2);
        int rMin = Mathf.Max(-50, Mathf.Min(左下.y, 右上.y) - 2), rMax = Mathf.Min(49, Mathf.Max(左下.y, 右上.y) + 2);
        float 半径 = 天帝道纹.半径 * 缩放;
        for (int q = qMin; q <= qMax; q++) for (int r = rMin; r <= rMax; r++)
        {
            var 格 = new Vector2Int(q, r); var 点 = 天帝道纹.格位置(格) * 缩放 + 平移;
            if (点.x < 区.xMin - 半径 || 点.x > 区.xMax + 半径 || 点.y < 区.yMin - 半径 || 点.y > 区.yMax + 半径) continue;
            可见格数++;
            bool 已解锁 = 数据.格已解锁(格);
            if (缩放 < .45f)
            {
                // 大范围总览只画已解锁格的简化六边形，避免一万格突破UGUI顶点上限。
                if (已解锁 && vh.currentVertCount < 60000)
                {
                    bool 有纹 = 数据.已放置.TryGetValue(格, out var 小纹);
                    var 色 = !有纹 ? new Color(.20f,.34f,.33f) : 小纹.是源纹 ? 源色 : 小纹.生效 ? 属性色 : new Color(.40f,.33f,.32f);
                    总览六边(vh, 点, 半径 - .4f, 色);
                }
                continue;
            }
            bool 石青底 = 构筑美术 && 天帝道纹美术.彩绘皮肤;
            六边(vh, 点, 半径 - 2,
                石青底 ? 已解锁 ? new Color(.42f,.66f,.60f,.22f) : new Color(.44f,.61f,.61f,.025f) : 已解锁 ? new Color(.70f,.86f,.79f,.45f) : new Color(.44f,.61f,.61f,.06f),
                石青底 ? 已解锁 ? new Color(.72f,.88f,.77f,.90f) : new Color(.67f,.79f,.74f,.26f) : 已解锁 ? new Color(.25f,.50f,.48f,.64f) : new Color(.37f,.53f,.53f,.23f), 已解锁 ? 1.8f : .65f);
            if (!已解锁 && !构筑美术) 画锁(vh, 点, 缩放);
            else if (!已解锁 && 新图集) 画新图(vh, 4, 点, Vector2.one * 18 * 缩放, new Color(.78f,.87f,.82f,.62f));
            else 画权重(vh, 点, 天帝道纹.格权重(格), 缩放);
            if (数据.已放置.TryGetValue(格, out var 纹))
            {
                画纹(vh, 点, 半径 - 4, 纹, 纹.生效, 纹 == 拖动纹 ? 0.35f : 1);
                int 归属 = 数据.通路掩码(纹);
                if ((归属 & (1 << 强调通路)) != 0)
                {
                    if (石青底) 六边(vh, 点, 半径 - 1, Color.clear, 通路颜色[强调通路], 1.8f);
                    else if (新图集) 画新图(vh, 2, 点, Vector2.one * 半径 * 2.02f, new Color(1,1,1,.85f));
                    else 六边(vh, 点, 半径 - 1, Color.clear, 通路颜色[强调通路], 2);
                }
                if (归属 != 0 && (归属 & (归属 - 1)) != 0)
                    总览六边(vh, 点 + Vector2.down * 半径 * .75f, Mathf.Max(2.5f, 4 * 缩放), Color.white);
                for (int d = 0; d < 3; d++)
                {
                    var 邻格 = 格 + 天帝道纹.邻向[d];
                    if (纹.生效 && 纹.有接口(d) && 数据.已放置.TryGetValue(邻格, out var 邻) && 邻.生效 && 邻.有接口(d + 3) &&
                        (纹.允许传出(d) && 邻.允许接入(d + 3) || 邻.允许传出(d + 3) && 纹.允许接入(d)))
                    {
                        var 起 = 天帝道纹.格权重(格) <= 天帝道纹.格权重(邻格) ? 点 : 天帝道纹.格位置(邻格) * 缩放 + 平移;
                        var 终 = 起 == 点 ? 天帝道纹.格位置(邻格) * 缩放 + 平移 : 点;
                        var 方向 = (终 - 起).normalized;
                        var a = 起 + 方向 * 半径 * .73f; var b = 终 - 方向 * 半径 * .73f;
                        if (石青底)
                        {
                            线(vh, a, b, Mathf.Max(5, 6 * 缩放), 接口深青);
                            线(vh, a, b, Mathf.Max(2.6f, 3 * 缩放), new Color(.47f,.83f,.75f));
                        }
                        else 线(vh, a, b, Mathf.Max(2, 3 * 缩放), 构筑美术 ? 天帝道纹美术.强调 : 属性色);
                    }
                }
            }
            if (预览亮起.Contains(格) || 预览暗掉.Contains(格))
            {
                var 色 = 预览亮起.Contains(格) ? new Color(.25f, 1f, .75f) : new Color(1f, .40f, .35f);
                六边(vh, 点, 半径 - 1, new Color(色.r, 色.g, 色.b, .10f), 色, 3);
            }
        }
        for (int i = 0; i < 高亮路径.Count; i++)
        {
            if (vh.currentVertCount > 63000) break;
            var 点 = 天帝道纹.格位置(高亮路径[i]) * 缩放 + 平移;
            bool 彩绘 = 天帝道纹美术.彩绘皮肤;
            if (彩绘)
            {
                六边(vh, 点, 半径 - 1, Color.clear, 链路描边, 8);
                六边(vh, 点, 半径 - 1, Color.clear, 链路金, 4);
            }
            else 六边(vh, 点, 半径 - 3, Color.clear, 源色, 2.5f);
            if (i == 0) continue;
            var 起 = 天帝道纹.格位置(高亮路径[i - 1]) * 缩放 + 平移; var 向 = (点 - 起).normalized;
            var a = 起 + 向 * 半径 * .70f; var b = 点 - 向 * 半径 * .70f;
            if (彩绘)
            {
                线(vh, a, b, Mathf.Max(8, 10 * 缩放), 链路描边);
                线(vh, a, b, Mathf.Max(4.8f, 6 * 缩放), 链路金);
                线(vh, a, b, Mathf.Max(1.6f, 2 * 缩放), new Color(1,.96f,.72f));
            }
            else 线(vh, a, b, 4 * 缩放, 源色);
        }
        if (预览格.HasValue)
            六边(vh, 天帝道纹.格位置(预览格.Value) * 缩放 + 平移, 半径 - 1,
                预览可放 ? new Color(0.2f, 1, 0.5f, 0.25f) : new Color(1, 0.15f, 0.14f, 0.30f),
                预览可放 ? new Color(0.3f, 1, 0.55f) : new Color(1, 0.25f, 0.25f), 3);
    }
    public static Vector2 单位(int d) => new Vector2(Mathf.Cos(d * Mathf.PI / 3), Mathf.Sin(d * Mathf.PI / 3));
    void 总览六边(VertexHelper vh, Vector2 中, float 半, Color 色)
    {
        int n = vh.currentVertCount;
        for (int i = 0; i < 6; i++) { float a = (30 + i * 60) * Mathf.Deg2Rad; vh.AddVert(中 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 半, 色, 白点); }
        for (int i = 1; i < 5; i++) vh.AddTriangle(n, n + i, n + i + 1);
    }
    void 画权重(VertexHelper vh, Vector2 点, int 权重, float 比例)
    {
        var 文 = 权重.ToString();
        float 单位 = Mathf.Max(2.2f, 3.4f * 比例), 宽 = 文.Length * 3 * 单位 - 单位;
        var 色 = 构筑美术 ? new Color(.88f, .93f, .83f, .82f) : new Color(.28f,.44f,.43f,.72f);
        for (int i = 0; i < 文.Length; i++)
        {
            int 段 = 数字段[文[i] - '0'];
            var 中 = 点 + new Vector2(-宽 * .5f + i * 3 * 单位 + 单位, 0);
            Vector2 左上 = 中 + new Vector2(-单位, 2 * 单位), 右上 = 中 + new Vector2(单位, 2 * 单位);
            Vector2 左中 = 中 + new Vector2(-单位, 0), 右中 = 中 + new Vector2(单位, 0);
            Vector2 左下 = 中 + new Vector2(-单位, -2 * 单位), 右下 = 中 + new Vector2(单位, -2 * 单位);
            float 粗 = Mathf.Max(1.35f, .95f * 比例);
            if ((段 & 1) != 0) 线(vh, 左上, 右上, 粗, 色);
            if ((段 & 2) != 0) 线(vh, 右上, 右中, 粗, 色);
            if ((段 & 4) != 0) 线(vh, 右中, 右下, 粗, 色);
            if ((段 & 8) != 0) 线(vh, 左下, 右下, 粗, 色);
            if ((段 & 16) != 0) 线(vh, 左中, 左下, 粗, 色);
            if ((段 & 32) != 0) 线(vh, 左上, 左中, 粗, 色);
            if ((段 & 64) != 0) 线(vh, 左中, 右中, 粗, 色);
        }
    }
    void 画锁(VertexHelper vh, Vector2 点, float 比例)
    {
        var 色 = new Color(0.34f, 0.42f, 0.43f, 0.65f);
        线(vh, 点 + new Vector2(-5, -3) * 比例, 点 + new Vector2(5, -3) * 比例, 8 * 比例, 色);
        var 左 = 点 + new Vector2(-4, 1) * 比例; var 右 = 点 + new Vector2(4, 1) * 比例;
        线(vh, 左, 左 + Vector2.up * 6 * 比例, 1.6f * 比例, 色);
        线(vh, 右, 右 + Vector2.up * 6 * 比例, 1.6f * 比例, 色);
        线(vh, 左 + Vector2.up * 6 * 比例, 右 + Vector2.up * 6 * 比例, 1.6f * 比例, 色);
    }
    void 画纹(VertexHelper vh, Vector2 点, float r, 道纹实例 纹, bool 亮, float alpha)
    {
        Color 色 = 品阶色(纹);
        if (!亮) 色 = Color.Lerp(new Color(0.20f, 0.25f, 0.27f), 色, 0.20f);
        色.a = alpha;
        if (新图集)
        {
            var 灰 = 亮 ? Color.white : new Color(.73f,.78f,.74f); 灰.a = alpha;
            画新图(vh, 纹.是源纹 ? 1 : 0, 点, Vector2.one * r * 2.15f, 灰);
            var 框色 = Color.Lerp(new Color(.68f,.73f,.67f), 色, .42f); 框色.a = alpha * (亮 ? .85f : .40f);
            画新图(vh, 3, 点, Vector2.one * r * 2.04f, 框色);
            if (纹.品阶 == 道纹品阶.传说) 六边(vh, 点, r * .90f, Color.clear, 框色, .7f, 纹.品阶, 亮, alpha * .7f);
        }
        else if (天帝美术资源.已接入)
            天帝美术资源.画图(vh, 0, 点, Vector2.one * r * 2.2f, new Color(亮 ? 1 : .33f, 亮 ? 1 : .33f, 亮 ? 1 : .33f, alpha));
        if (!新图集) 六边(vh, 点, r, 天帝美术资源.已接入 ? Color.clear : new Color(色.r * 0.17f, 色.g * 0.17f, 色.b * 0.17f, alpha), 色, 亮 ? 2.3f : 1.5f,
            纹.品阶 == 道纹品阶.传说 ? (道纹品阶?)纹.品阶 : null, 亮, alpha);
        int 标 = 天帝美术资源.道纹图标(纹);
        if (纹.是特性道纹)
        {
            var c=纹.特性状态.StartsWith("已激活")?new Color(.16f,.5f,.38f,alpha):纹.生效?new Color(.7f,.46f,.16f,alpha):new Color(.35f,.4f,.4f,alpha);
            float s=r*.3f;
            if(纹.分类==道纹分类.转化){画箭头(vh,点-Vector2.right*s,点+Vector2.right*s,2,c);六边(vh,点,s*.6f,Color.clear,c,1.5f);}
            else {六边(vh,点,s,Color.clear,c,2);线(vh,点-Vector2.up*s*.6f,点+Vector2.up*s*.6f,2,c);线(vh,点-Vector2.right*s*.6f,点+Vector2.right*s*.6f,2,c);}
        }
        else if (纹.分类 == 道纹分类.功能 && 纹.功能 > 道纹功能.穿透)
            画新功能符号(vh, 点, r * .38f, 纹.功能, 新图集 ? new Color(.16f,.35f,.42f,alpha) : 色);
        else if (纹.分类 == 道纹分类.功能 && 纹.功能 >= 道纹功能.增大 && 纹.功能 <= 道纹功能.穿透)
            画扩展功能(vh, 点, r * .38f, 纹.功能, 新图集 ? new Color(.16f,.35f,.42f,alpha) : 色);
        else if (新图集 && 标 >= 0)
            画UV(vh, new Rect(标 % 8 / 8f, (1 - (标 / 8 + 1) / 4f) * .5f, 1f/8, 1f/8), 点, Vector2.one * r * .94f,
                纹.是源纹 ? new Color(.36f,.25f,.12f,alpha) : 亮 ? new Color(.16f,.35f,.42f,alpha) : new Color(.42f,.47f,.45f,alpha));
        else 天帝美术资源.画图(vh, 标, 点, Vector2.one * r * .94f, new Color(色.r, 色.g, 色.b, alpha));
        if (新图集) { 色 = 亮 ? 天帝道纹美术.强调 : new Color(.33f,.39f,.38f); 色.a = alpha; }
        for (int d = 0; d < 6; d++) if (纹.有接口(d))
        {
            var 方向 = 单位(d);
            if (天帝道纹美术.彩绘皮肤)
            {
                // 接口跨过装饰边缘，以浅描边包住深色实心口；小图也能辨认方向。
                var 端 = 点 + 方向 * r * 1.08f; var 起 = 点 + 方向 * r * .62f;
                float 粗 = Mathf.Clamp(r * .145f, 3.8f, 7);
                var 口色 = 亮 ? 接口深青 : new Color(.27f,.35f,.35f); 口色.a = alpha;
                var 边色 = 接口浅边; 边色.a = alpha;
                线(vh, 起, 端, 粗 + 3.2f, 边色); 线(vh, 起, 端, 粗, 口色);
                总览六边(vh, 端, 粗 + 1.8f, 边色); 总览六边(vh, 端, 粗, 口色);
                // 空心口与深色连杆形成稳定方向标记，不被底纹花边淹没。
                总览六边(vh, 端, 粗*.40f, 边色);
            }
            else
            {
                var 端 = 点 + 方向 * r * .89f;
                线(vh, 点 + 方向 * r * .58f, 端, 亮 ? 3 : 2, 色);
                六边(vh, 端, 3.3f, 色, 色, 1);
            }
        }
        else if (纹.接口封印(d))
        {
            var 方向 = 单位(d); var 端 = 点 + 方向 * r * 1.05f;
            var 灰 = new Color(.33f, .36f, .32f, alpha);
            float 大小=Mathf.Clamp(r*.13f,3.4f,6);
            var 边=接口浅边;边.a=alpha;
            总览六边(vh,端,大小+1.4f,边);总览六边(vh,端,大小,灰);
            var 横 = new Vector2(-方向.y, 方向.x) * 大小*.48f;
            线(vh, 端 - 横 - 方向 * 大小*.48f, 端 + 横 + 方向 * 大小*.48f, 1.4f, 边);
            线(vh, 端 - 横 + 方向 * 大小*.48f, 端 + 横 - 方向 * 大小*.48f, 1.4f, 边);
        }
    }
    void 画扩展功能(VertexHelper 网, Vector2 中, float r, 道纹功能 功能, Color 色)
    {
        画体型弹速符号(网, 中, r, 功能, 色);
    }
    void 画新功能符号(VertexHelper 网, Vector2 中, float r, 道纹功能 功能, Color 色)
    {
        float 粗 = Mathf.Max(1.2f, r * .15f);
        void 杆(float x, float y, float a, float b) => 线(网, 中 + new Vector2(x,y) * r, 中 + new Vector2(a,b) * r, 粗, 色);
        void 箭(float x, float y, float a, float b) => 画箭头(网, 中 + new Vector2(x,y) * r, 中 + new Vector2(a,b) * r, 粗, 色);
        void 环(float 半)
        { for (int i = 0; i < 16; i++) { float a = i * Mathf.PI / 8, b = (i+1) * Mathf.PI / 8; 杆(Mathf.Cos(a)*半,Mathf.Sin(a)*半,Mathf.Cos(b)*半,Mathf.Sin(b)*半); } }
        if (天帝顺序道纹.多弹功能(功能))
        {
            for (int i = 0; i < 天帝顺序道纹.多弹数量(功能); i++)
            { float a = 天帝顺序道纹.多弹角度(功能, i) * Mathf.Deg2Rad; 箭(0,0,Mathf.Cos(a),Mathf.Sin(a)); }
            return;
        }
        switch (功能)
        {
            case 道纹功能.折返: 杆(-.9f,.6f,.7f,.6f); 杆(.7f,.6f,.7f,-.6f); 箭(.7f,-.6f,-.9f,-.6f); break;
            case 道纹功能.回旋: 环(.7f); 箭(.6f,.4f,.8f,-.4f); break;
            case 道纹功能.波动:
                for (int i=0;i<12;i++) { float x=-1+i/6f, a=-1+(i+1)/6f; 杆(x,Mathf.Sin(x*Mathf.PI)*.6f,a,Mathf.Sin(a*Mathf.PI)*.6f); } break;
            case 道纹功能.弹墙: 杆(.8f,-1,.8f,1); 杆(-.9f,-.8f,.65f,0); 箭(.65f,0,-.9f,.8f); break;
            case 道纹功能.跃迁: 六边(网,中-Vector2.right*r*.8f,r*.25f,Color.clear,色,粗); 六边(网,中+Vector2.right*r*.8f,r*.25f,Color.clear,色,粗); 箭(-.4f,0,.4f,0); break;
            case 道纹功能.延时: 环(.9f); 杆(0,0,0,.6f); 杆(0,0,.5f,-.2f); break;
            case 道纹功能.停驻: 杆(-.35f,-.8f,-.35f,.8f); 杆(.35f,-.8f,.35f,.8f); break;
            case 道纹功能.蓄势: 箭(0,-.9f,0,.9f); 杆(-.9f,-.8f,-.4f,-.4f); 杆(.9f,-.8f,.4f,-.4f); break;
            case 道纹功能.爆破: for(int i=0;i<8;i++){float a=i*Mathf.PI/4; 箭(Mathf.Cos(a)*.3f,Mathf.Sin(a)*.3f,Mathf.Cos(a),Mathf.Sin(a));} break;
            case 道纹功能.震荡: 环(.9f); 环(.5f); break;
            case 道纹功能.拖尾: for(int i=0;i<3;i++) 六边(网,中+Vector2.right*(i-1)*r*.7f,r*(.15f+i*.07f),色,色,粗); break;
            case 道纹功能.击退: 箭(-1,0,.7f,0); 杆(.9f,-.8f,.9f,.8f); break;
            case 道纹功能.牵引: 箭(-1,0,-.15f,0); 箭(1,0,.15f,0); break;
            case 道纹功能.束缚: 六边(网,中,r*.85f,Color.clear,色,粗); 杆(-.65f,-.45f,.65f,.45f); 杆(-.65f,.45f,.65f,-.45f); break;
            case 道纹功能.烙印: 环(.9f); 杆(-.45f,-.45f,.45f,.45f); 杆(-.45f,.45f,.45f,-.45f); break;
            case 道纹功能.陨落: 箭(0,1,0,-.7f); 杆(-.9f,-.9f,.9f,-.9f); break;
            case 道纹功能.光束: 杆(-1,0,1,0); 杆(-1,.3f,.8f,.3f); 杆(-1,-.3f,.8f,-.3f); break;
            case 道纹功能.刃波: for(int i=0;i<8;i++){float a=-Mathf.PI*.6f+i*Mathf.PI*.15f,b=a+Mathf.PI*.15f;杆(Mathf.Cos(a)*.85f,Mathf.Sin(a)*.85f,Mathf.Cos(b)*.85f,Mathf.Sin(b)*.85f);} break;
            case 道纹功能.地刺: for(int i=-1;i<=1;i++){杆(i*.6f-.2f,-.7f,i*.6f,.7f);杆(i*.6f,.7f,i*.6f+.2f,-.7f);} break;
            case 道纹功能.剑雨: for(int i=-1;i<=1;i++){箭(i*.6f,.9f,i*.6f,-.9f);杆(i*.6f-.2f,.4f,i*.6f+.2f,.4f);}break;
            case 道纹功能.旋刃: 环(.8f); 杆(0,-.8f,.5f,-.5f); 杆(.8f,0,.5f,.5f); break;
            case 道纹功能.灵鞭: 杆(-.9f,-.7f,-.3f,.4f); 杆(-.3f,.4f,.5f,.9f); 杆(.5f,.9f,1,.2f); break;
            case 道纹功能.飞轮: 环(.7f); for(int i=0;i<8;i++){float a=i*Mathf.PI/4;杆(Mathf.Cos(a)*.65f,Mathf.Sin(a)*.65f,Mathf.Cos(a+.2f),Mathf.Sin(a+.2f));}break;
            case 道纹功能.游龙: 杆(-1,-.5f,-.4f,.5f);杆(-.4f,.5f,.2f,-.3f);杆(.2f,-.3f,.8f,.5f);杆(.8f,.5f,1,.2f);break;
            case 道纹功能.灵网: for(int i=-1;i<=1;i++){杆(-1,i*.65f,1,i*.65f);杆(i*.65f,-1,i*.65f,1);}break;
            case 道纹功能.地雷: 环(.85f);环(.3f);杆(-1,-.9f,1,-.9f);break;
        }
    }
    void 画体型弹速符号(VertexHelper 网, Vector2 中, float r, 道纹功能 功能, Color 色)
    {
        float 粗 = Mathf.Max(1.2f, r * .15f);
        if (功能 == 道纹功能.增大 || 功能 == 道纹功能.缩小)
        {
            for (int i = 0; i < 4; i++)
            {
                var 向 = new Vector2(i < 2 ? 1 : -1, i % 2 == 0 ? 1 : -1).normalized;
                var 起 = 中 + 向 * r * .2f; var 末 = 中 + 向 * r;
                if (功能 == 道纹功能.缩小) { var 临 = 起; 起 = 末; 末 = 临; }
                画箭头(网, 起, 末, 粗, 色);
            }
        }
        else if (功能 == 道纹功能.加速 || 功能 == 道纹功能.减速)
        {
            float 向 = 功能 == 道纹功能.加速 ? 1 : -1;
            for (int i = -1; i <= 1; i++)
            {
                var 尖 = 中 + Vector2.right * (i * .65f + 向 * .3f) * r;
                线(网, 尖 + new Vector2(-向 * .45f, .55f) * r, 尖, 粗, 色);
                线(网, 尖, 尖 + new Vector2(-向 * .45f, -.55f) * r, 粗, 色);
            }
        }
        else
        {
            for (int i = -1; i <= 1; i += 2)
                线(网, 中 + new Vector2(i * .4f, -.7f) * r, 中 + new Vector2(i * .4f, .7f) * r, 粗, 色);
            画箭头(网, 中 - Vector2.right * r, 中 + Vector2.right * r, 粗, 色);
        }
    }
    void 画箭头(VertexHelper 网, Vector2 起, Vector2 末, float 粗, Color 色)
    {
        var 向 = (末 - 起).normalized; var 横 = new Vector2(-向.y, 向.x);
        float 长 = Vector2.Distance(起, 末) * .35f;
        线(网, 起, 末, 粗, 色);
        线(网, 末, 末 - 向 * 长 + 横 * 长 * .65f, 粗, 色);
        线(网, 末, 末 - 向 * 长 - 横 * 长 * .65f, 粗, 色);
    }
    void 画新图(VertexHelper 网, int 编号, Vector2 中, Vector2 大小, Color 色)
        => 画UV(网, new Rect(编号 % 8 / 8f, 1 - (编号 / 8 + 1) / 8f, 1f/8, 1f/8), 中, 大小, 色);
    static void 画UV(VertexHelper 网, Rect 区, Vector2 中, Vector2 大小, Color 色)
    {
        int n = 网.currentVertCount; var 半 = 大小 * .5f;
        网.AddVert(中 + new Vector2(-半.x,-半.y), 色, new Vector2(区.xMin,区.yMin));
        网.AddVert(中 + new Vector2(-半.x,半.y), 色, new Vector2(区.xMin,区.yMax));
        网.AddVert(中 + new Vector2(半.x,半.y), 色, new Vector2(区.xMax,区.yMax));
        网.AddVert(中 + new Vector2(半.x,-半.y), 色, new Vector2(区.xMax,区.yMin));
        网.AddTriangle(n,n+1,n+2); 网.AddTriangle(n,n+2,n+3);
    }
    void 六边(VertexHelper vh, Vector2 c, float r, Color fill, Color border, float thickness, 道纹品阶? 彩阶 = null, bool 亮 = true, float alpha = 1)
    {
        for (int i = 0; i < 6; i++)
        {
            float a = (30 + i * 60) * Mathf.Deg2Rad, b = (30 + (i + 1) * 60) * Mathf.Deg2Rad;
            var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            var n = c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r;
            int index = vh.currentVertCount;
            vh.AddVert(c, fill, 白点); vh.AddVert(p, fill, 白点); vh.AddVert(n, fill, 白点); vh.AddTriangle(index, index + 1, index + 2);
            var 边色 = 彩阶.HasValue ? 天帝道纹品阶.边颜色(彩阶.Value, i) : border;
            if (彩阶.HasValue) { if (!亮) 边色 = Color.Lerp(new Color(0.20f, 0.25f, 0.27f), 边色, 0.20f); 边色.a = alpha; }
            线(vh, p, n, thickness, 边色);
        }
    }
    void 线(VertexHelper vh, Vector2 a, Vector2 b, float width, Color 色)
    {
        var d = b - a; if (d.sqrMagnitude < 0.001f) return;
        var n = new Vector2(-d.y, d.x).normalized * width * 0.5f; int i = vh.currentVertCount;
        vh.AddVert(a + n, 色, 白点); vh.AddVert(b + n, 色, 白点); vh.AddVert(b - n, 色, 白点); vh.AddVert(a - n, 色, 白点);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }
}
