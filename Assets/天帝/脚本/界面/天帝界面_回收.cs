using UnityEngine;
using UnityEngine.UI;

public partial class 天帝界面
{
    public bool 回收已打开{get;private set;}
    public 天帝道纹回收界面 回收页{get;private set;}
    public void 显示回收()
    {
        if(游戏.阶段!=游戏阶段.主页||游戏.道纹数据==null||游戏.宝盒数据==null||回收已打开||设置已打开||角色已打开||图鉴已打开||宝盒已打开||确认已打开||地图已打开)return;
        关闭等级下拉();回收已打开=true;
        foreach(var 键 in 页面.GetComponentsInChildren<Selectable>())键.interactable=false;
        var 区=区块(弹层,"道纹回收页面",0,0,1600,900);回收页=区.gameObject.AddComponent<天帝道纹回收界面>();
        回收页.初始化(游戏.道纹数据,游戏.宝盒数据,游戏.默认字体,关闭回收);
    }
    public void 关闭回收()
    {
        if(!回收已打开)return;回收已打开=false;回收页=null;清空(弹层);
        if(主页灵石字!=null)主页灵石字.text="灵石  "+游戏.宝盒数据.灵石显示;
        foreach(var 键 in 页面.GetComponentsInChildren<Selectable>())键.interactable=true;
    }
}
