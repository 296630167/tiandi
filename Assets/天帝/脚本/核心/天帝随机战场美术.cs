using UnityEngine;

public sealed partial class 天帝战斗场景
{
    readonly System.Collections.Generic.List<SpriteRenderer> 随机障碍立绘 = new System.Collections.Generic.List<SpriteRenderer>();
    public int 淡化障碍数量 { get; private set; }
    void 建随机战场美术()
    {
        if(!地图.生存大图)return;
        var 根=new GameObject("本局随机战场_"+地图.战场布局名).transform;根.SetParent(transform,false);
        foreach(var b in 地图.随机障碍)
        {
            var 图=b.种类==随机场景障碍种类.古树?游戏.美术.青岚原古树:b.种类==随机场景障碍种类.松树?游戏.美术.青岚原松树:b.种类==随机场景障碍种类.岩石?游戏.美术.青岚原岩石:游戏.美术.获取("EV06");
            if(图==null)throw new System.InvalidOperationException("随机障碍缺少正式美术："+b.种类);
            var o=new GameObject("随机障碍_"+b.种类,typeof(SpriteRenderer));o.transform.SetParent(根,false);
            o.transform.position=new Vector3(b.位置.x,.08f,b.位置.y);o.transform.rotation=Quaternion.Euler(90,0,0)*Quaternion.Euler(0,0,b.角度);
            o.transform.localScale=new Vector3(b.尺寸.x/图.bounds.size.x,b.尺寸.y/图.bounds.size.y,1);
            var s=o.GetComponent<SpriteRenderer>();s.sprite=图;s.sharedMaterial=背景材质;s.sortingOrder=Mathf.Clamp(Mathf.RoundToInt(-b.位置.y*100),-10000,10000);
            随机障碍立绘.Add(s);
        }
        var 浅滩图=Resources.Load<Sprite>("随机战场/浅滩");
        if(浅滩图==null)throw new System.InvalidOperationException("随机浅滩美术未接入。");
        foreach(var 水 in 地图.浅滩)
        {
            var o=new GameObject("可穿行浅滩_移速78%",typeof(SpriteRenderer));o.transform.SetParent(根,false);
            o.transform.position=new Vector3(水.位置.x,.035f,水.位置.y);o.transform.rotation=Quaternion.Euler(90,0,0);
            o.transform.localScale=new Vector3(水.半径*2/浅滩图.bounds.size.x,水.半径*2/浅滩图.bounds.size.y,1);
            var s=o.GetComponent<SpriteRenderer>();s.sprite=浅滩图;s.sharedMaterial=背景材质;s.sortingOrder=-31000;
        }
    }
    void 更新随机障碍遮挡(float 秒)
    {
        if (美术 == null || !美术.可用) return;
        var 人范围 = 美术.玩家绘制范围;
        int 人深度 = Mathf.Clamp(Mathf.RoundToInt(-玩家位置.y * 100), -10000, 10000);
        淡化障碍数量 = 0;
        foreach (var 像 in 随机障碍立绘)
        {
            if (像 == null) continue;
            bool 挡人 = 战斗?.玩家死亡 != true && 像.sortingOrder >= 人深度 && 天帝战斗辨识.平面相交(人范围, 像.bounds);
            var 色 = 像.color; float 透明 = Mathf.MoveTowards(色.a, 挡人 ? .32f : 1f, Mathf.Max(0, 秒) * 5);
            if (色.a != 透明) { 色.a = 透明; 像.color = 色; }
            if (透明 < .99f) 淡化障碍数量++;
        }
    }
}
