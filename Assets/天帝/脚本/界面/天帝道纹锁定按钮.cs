using UnityEngine;
using UnityEngine.UI;

// 每张道纹卡复用同一实例锁；子按钮独立接收点击，不触发父卡选择或拖放。
public sealed class 天帝道纹锁定按钮 : MonoBehaviour
{
    天帝道纹 数据;
    道纹实例 道纹;
    Button 按钮;
    天帝回收锁图标 图标;
    public static 天帝道纹锁定按钮 创建(RectTransform 卡,天帝道纹 网)
    {
        var r=new GameObject("道纹回收锁定",typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(卡,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,1);r.anchoredPosition=new Vector2(-4,-4);
        float 边=天帝移动适配.启用?44:32;r.sizeDelta=new Vector2(边,边);
        var 底=r.gameObject.AddComponent<Image>();底.color=new Color(.04f,.09f,.1f,.95f);底.raycastTarget=true;
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=底;
        var 图区=new GameObject("锁定图标",typeof(RectTransform)).GetComponent<RectTransform>();图区.SetParent(r,false);
        图区.anchorMin=Vector2.zero;图区.anchorMax=Vector2.one;图区.offsetMin=new Vector2(6,6);图区.offsetMax=new Vector2(-6,-6);
        var 图=图区.gameObject.AddComponent<天帝回收锁图标>();图.raycastTarget=false;
        var 锁=r.gameObject.AddComponent<天帝道纹锁定按钮>();锁.数据=网;锁.按钮=b;锁.图标=图;
        b.onClick.AddListener(()=>{if(锁.道纹!=null)网.设置回收锁定(锁.道纹,!锁.道纹.回收锁定);});
        网.状态改变+=锁.刷新;return 锁;
    }
    public void 设置(道纹实例 纹){道纹=纹;刷新();}
    void 刷新()
    {
        bool 可用=道纹!=null&&!道纹.是源纹&&!道纹.是天赋&&数据.道纹.Contains(道纹);
        gameObject.SetActive(可用);if(!可用)return;
        图标.锁住=道纹.回收锁定;图标.color=道纹.回收锁定?new Color(.98f,.82f,.4f):new Color(.68f,.79f,.76f);图标.SetVerticesDirty();
    }
    void OnDestroy(){if(数据!=null)数据.状态改变-=刷新;}
}

// 矢量挂锁避免依赖字体是否包含锁图标；开锁时锁梁右侧抬起。
[RequireComponent(typeof(CanvasRenderer))]
public sealed class 天帝回收锁图标 : MaskableGraphic
{
    public bool 锁住;
    protected override void OnPopulateMesh(VertexHelper v)
    {
        v.Clear();var r=rectTransform.rect;
        void 块(float x,float y,float w,float h)
        {
            int n=v.currentVertCount;var c=(Color32)color;
            v.AddVert(new Vector3(r.x+x*r.width,r.y+y*r.height),c,Vector2.zero);
            v.AddVert(new Vector3(r.x+x*r.width,r.y+(y+h)*r.height),c,Vector2.zero);
            v.AddVert(new Vector3(r.x+(x+w)*r.width,r.y+(y+h)*r.height),c,Vector2.zero);
            v.AddVert(new Vector3(r.x+(x+w)*r.width,r.y+y*r.height),c,Vector2.zero);
            v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);
        }
        块(.16f,.12f,.68f,.4f);块(.27f,.49f,.1f,.31f);块(.27f,.76f,.46f,.1f);
        块(.63f,锁住?.49f:.65f,.1f,锁住?.31f:.15f);
    }
}
