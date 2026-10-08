using System;
using System.Collections.Generic;
using UnityEngine;

// 所有外观来自Tap生成纹理；三角形只负责贴图映射、真实范围裁切，不生成图案。
public sealed class 天帝纹理特效 : IDisposable
{
    public const int 金弹=0,木弹=1,水弹=2,火弹=3,土弹=4,灵弹=5,预警环=6,危险区=7,冲锋框=8,尾迹=9,冲击环=10,爆闪=11;
    readonly List<Vector3> 顶点=new List<Vector3>(4096);
    readonly List<Vector2> UV=new List<Vector2>(4096);
    readonly List<Color> 颜色=new List<Color>(4096);
    readonly List<int> 索引=new List<int>(8192);
    readonly Mesh 网格;
    readonly Material 材质;
    readonly GameObject 根;
    [Serializable] sealed class 尺寸条目 { public float 长宽比=1,锚点=.5f; }
    [Serializable] sealed class 尺寸表 { public 尺寸条目[] 条目; }
    readonly 尺寸表 尺寸;
    int 数;
    // 4096个贴图片段硬上限。资源打包为4列3行、512像素格，8像素透明保护边。
    public 天帝纹理特效(Transform 父,int 排序=10001,string 图路径="战斗特效/战斗特效图集")
    {
        var 图=Resources.Load<Texture2D>(图路径);
        var shader=Shader.Find("天帝/生成特效");
        if(图==null||shader==null)throw new InvalidOperationException("生成战斗特效图集或着色器未接入。");
        var 表=Resources.Load<TextAsset>("战斗特效/战斗特效尺寸");
        if(表==null)throw new InvalidOperationException("生成特效尺寸未接入。");
        尺寸=JsonUtility.FromJson<尺寸表>(表.text);
        if(尺寸?.条目==null||尺寸.条目.Length!=12)throw new InvalidOperationException("生成特效尺寸数量不正确。");
        根=new GameObject("Tap生成战斗特效_合并贴图",typeof(MeshFilter),typeof(MeshRenderer));根.transform.SetParent(父,false);
        网格=new Mesh{name="生成特效动态网格",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};网格.MarkDynamic();
        材质=new Material(shader){name="Tap生成特效共享材质",mainTexture=图};
        根.GetComponent<MeshFilter>().sharedMesh=网格;根.GetComponent<MeshRenderer>().sharedMaterial=材质;
        根.GetComponent<MeshRenderer>().sortingOrder=排序;
    }
    public void 清空(){顶点.Clear();UV.Clear();颜色.Clear();索引.Clear();数=0;}
    Vector2 映射(int 图,Vector2 坐标)
    {
        float x=(图%4)*512+8,y=(2-图/4)*512+8;
        return new Vector2((x+Mathf.Clamp01(坐标.x)*496)/2048f,(y+Mathf.Clamp01(坐标.y)*496)/1536f);
    }
    void 三角(int 图,Vector2 a,Vector2 b,Vector2 c,Vector2 ua,Vector2 ub,Vector2 uc,float 高,Color 色)
    {
        int i=顶点.Count;顶点.Add(new Vector3(a.x,高,a.y));顶点.Add(new Vector3(b.x,高,b.y));顶点.Add(new Vector3(c.x,高,c.y));
        UV.Add(映射(图,ua));UV.Add(映射(图,ub));UV.Add(映射(图,uc));颜色.Add(色);颜色.Add(色);颜色.Add(色);索引.Add(i);索引.Add(i+1);索引.Add(i+2);
    }
    public void 贴图(int 图,Vector2 点,Vector2 大小,Vector2 朝向,Color 色,float 高=.26f)
    {
        if(大小.x<=0||大小.y<=0||色.a<=0||数++>=4096)return;
        Vector2 up=朝向.sqrMagnitude>.001f?朝向.normalized:Vector2.up,right=new Vector2(up.y,-up.x);
        Vector2 a=点-right*大小.x*.5f-up*大小.y*.5f,b=点+right*大小.x*.5f-up*大小.y*.5f,c=点+right*大小.x*.5f+up*大小.y*.5f,d=点-right*大小.x*.5f+up*大小.y*.5f;
        三角(图,a,b,c,Vector2.zero,Vector2.right,Vector2.one,高,色);三角(图,a,c,d,Vector2.zero,Vector2.one,Vector2.up,高,色);
    }
    public void 圈(int 图,Vector2 点,float 半径,Color 色,float 高=.18f)=>贴图(图,点,Vector2.one*半径*2,Vector2.up,色,高);
    public void 线(int 图,Vector2 起,Vector2 终,float 宽,Color 色,float 高=.2f)
    {Vector2 d=终-起;贴图(图,(起+终)*.5f,new Vector2(宽,d.magnitude),d,色,高);}
    public void 扇(int 图,Vector2 点,Vector2 朝,float 半径,float 角,Color 色,float 高=.18f)
    {
        if(半径<=0||色.a<=0||数++>=4096)return;
        float 起角=Mathf.Atan2(朝.y,朝.x)-Mathf.Clamp(角,0,360)*Mathf.Deg2Rad*.5f;
        int n=Mathf.Max(1,Mathf.CeilToInt(角/5));
        for(int i=0;i<n;i++)
        {
            float a=起角+角*Mathf.Deg2Rad*i/n,b=起角+角*Mathf.Deg2Rad*(i+1)/n;
            Vector2 va=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),vb=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
            三角(图,点,点+va*半径,点+vb*半径,Vector2.one*.5f,Vector2.one*.5f+va*.5f,Vector2.one*.5f+vb*.5f,高,色);
        }
    }
    public void 弹(int 图,Vector2 点,Vector2 方向,float 半径,Color 色)
    {
        if(方向.sqrMagnitude<.001f)方向=Vector2.right;
        // 弹体原画头向右，导入时转为头朝上；锚点位于头中心，尾迹在后。
        图=Mathf.Clamp(图,0,5);var 条目=尺寸.条目[图];
        float 长=半径*2*条目.长宽比;
        贴图(图,点-方向.normalized*长*(条目.锚点-.5f),new Vector2(半径*2,长),方向,色,.27f);
    }
    public void 提交(){网格.Clear();网格.SetVertices(顶点);网格.SetUVs(0,UV);网格.SetColors(颜色);网格.SetTriangles(索引,0);网格.RecalculateBounds();}
    static void 释放(UnityEngine.Object o){if(o==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
    public void Dispose(){释放(根);释放(网格);释放(材质);}
}
