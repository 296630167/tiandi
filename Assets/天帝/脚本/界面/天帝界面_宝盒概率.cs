using System.Text;
using UnityEngine;
using UnityEngine.UI;

public partial class 天帝界面
{
    RectTransform 宝盒概率层;
    public bool 宝盒概率已打开 => 宝盒概率层 != null;
    public void 显示宝盒概率(int 序号)
    {
        if (天帝剪纸界面皮肤.已启用) { 显示山水宝盒概率(序号); return; }
        if(!宝盒已打开||宝盒概率已打开||序号<0||序号>2)return;
        foreach(var 控件 in 弹层.GetComponentsInChildren<Selectable>())控件.interactable=false;
        宝盒概率层=区块(弹层,"宝盒概率层",0,0,1600,900);
        var 遮=图(宝盒概率层,"概率遮罩",0,0,1600,900,new Color(0,0,0,.72f));遮.raycastTarget=true;
        var 框=图(宝盒概率层,"概率面板",500,180,600,540,纸).rectTransform;
        天帝界面美术.面板(框.GetComponent<Image>(),"属性面板",Color.white);
        string[] 名={"属性宝盒","功能宝盒","分叉宝盒"};
        字(框,名[序号]+" · 概率",32,16,536,56,29,墨);
        字(框,序号<2?"道纹品阶":"接口数量",52,84,308,28,17,次墨).alignment=TextAnchor.MiddleLeft;
        字(框,"每次概率",368,84,172,28,17,次墨).alignment=TextAnchor.MiddleRight;
        var 名称=new StringBuilder();var 比例=new StringBuilder();
        if(序号<2){var p=天帝宝盒.品阶概率((宝盒种类)序号);for(int i=0;i<p.Length;i++)if(p[i]>0){名称.AppendLine(天帝道纹品阶.彩色品阶文字((道纹品阶)i));比例.AppendLine((p[i]/100f).ToString("0.##")+"%");}}
        else{var p=天帝宝盒.分叉概率();for(int i=0;i<p.Length;i++){名称.AppendLine((i+3)+" 个接口");比例.AppendLine(p[i]+"%");}}
        if(序号==1)
        {
            名称.AppendLine(天帝顺序道纹.功能数量 + "种功能，等概率");比例.AppendLine("各" + (100f / 天帝顺序道纹.功能数量).ToString("0.##") + "%");
            名称.AppendLine("其余功能1口 / 2口");比例.AppendLine("各50%");
            名称.AppendLine("原三种接口保持");比例.AppendLine("方向随机");
        }
        var 左=字(框,名称.ToString().TrimEnd(),52,122,308,290,23,墨);左.alignment=TextAnchor.UpperLeft;左.lineSpacing=1.1f;
        var 右=字(框,比例.ToString().TrimEnd(),368,122,172,290,23,墨);右.alignment=TextAnchor.UpperRight;右.lineSpacing=1.1f;右.fontStyle=FontStyle.Bold;
        字(框,"独立抽取一枚 · 回收只返还少量灵石",40,418,520,32,17,次墨);
        按钮(框,"返回宝盒",192,474,216,42,关闭宝盒概率);
        if(天帝移动适配.启用)
        {
            var 口=天帝双端页面布局.滚动组(框,"宝盒概率正文",32,16,536,434);
            var 重排=天帝双端页面布局.重排正文(口.GetComponent<ScrollRect>().content);
            天帝双端页面布局.移动页(框,面板=>
            {
                天帝双端页面布局.固定(口,12,8,面板.rect.width-24,面板.rect.height-68);重排();
                天帝双端页面布局.按键(面板.Find("返回宝盒") as RectTransform,面板.rect.width*.5f-100,面板.rect.height-52,200);
            });
        }
    }
    public void 关闭宝盒概率()
    {
        if(宝盒概率层==null)return;宝盒概率层.gameObject.SetActive(false);删除界面对象(宝盒概率层.gameObject);宝盒概率层=null;
        foreach(var 控件 in 弹层.GetComponentsInChildren<Selectable>())控件.interactable=true;
        // 抽取按钮按余额恢复，避免关闭概率时启用买不起的盒子。
        foreach(var b in 弹层.GetComponentsInChildren<Button>())if(b.name=="抽取")
        {var p=b.transform.parent;int i=p.name=="属性宝盒"?0:p.name=="功能宝盒"?1:2;b.interactable=游戏.宝盒数据.灵石>=天帝宝盒.价格((宝盒种类)i);天帝首两页山水素材.按钮透明度(b);}
    }
}
