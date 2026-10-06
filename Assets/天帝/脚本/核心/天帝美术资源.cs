using System;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "天帝/美术资源")]
public sealed class 天帝美术资源 : ScriptableObject
{
    [Serializable] public sealed class 图片条目 { public string 编号; public Sprite 图片; }
    public 图片条目[] 图片 = Array.Empty<图片条目>();
    public Texture2D 道纹图集;
    public Texture2D 道纹构筑图集;
    public Shader 立绘着色器;
    public Sprite 青岚原长卷;
    public Sprite[] 青岚原高清长卷 = Array.Empty<Sprite>();
    public Texture2D 青岚原通行图;
    public Sprite 青岚原生存大图;
    public Texture2D 青岚原生存通行图;
    public Sprite 青岚原古树;
    public Sprite 青岚原松树;
    public Sprite 青岚原岩石;
    public 天帝角色帧动画 主角移动动画;
    public Font 主页标题字体;
    public static 天帝美术资源 当前 { get; internal set; }
    public Sprite 获取(string 编号)
    {
        foreach (var 项 in 图片) if (项.编号 == 编号) return 项.图片;
        return null;
    }
    public static bool 已接入 => 当前 != null && 当前.道纹图集 != null;
    public static Vector2 白点 => 已接入 ? new Vector2(2f / 2048, 2f / 1024) : Vector2.zero;
    public static int 道纹图标(道纹实例 纹)
        => 纹.是特性道纹 ? -1 : 纹.是天赋 ? 13 + 纹.天赋.编号 : 纹.分类 == 道纹分类.分叉 || 纹.是顺序功能 && 纹.功能 >= 道纹功能.增大 ? -1 : (int)纹.属性 < 10 ? 2 + (int)纹.属性 : -1;
    public static bool 有道纹图标(道纹实例 纹) => 已接入 && 道纹图标(纹) >= 0;
    public static void 画图(VertexHelper 网, int 编号, Vector2 中心, Vector2 大小, Color 色)
    {
        if (!已接入 || 编号 < 0 || 编号 >= 26) return;
        var 区 = new Rect(编号 % 8 / 8f, 1 - (编号 / 8 + 1) / 4f, 1f / 8, 1f / 4);
        int 开 = 网.currentVertCount; var 半 = 大小 * .5f;
        网.AddVert(中心 + new Vector2(-半.x, -半.y), 色, new Vector2(区.xMin, 区.yMin));
        网.AddVert(中心 + new Vector2(-半.x, 半.y), 色, new Vector2(区.xMin, 区.yMax));
        网.AddVert(中心 + new Vector2(半.x, 半.y), 色, new Vector2(区.xMax, 区.yMax));
        网.AddVert(中心 + new Vector2(半.x, -半.y), 色, new Vector2(区.xMax, 区.yMin));
        网.AddTriangle(开, 开 + 1, 开 + 2); 网.AddTriangle(开, 开 + 2, 开 + 3);
    }
}
