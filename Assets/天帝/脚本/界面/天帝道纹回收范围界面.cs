using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝道纹回收界面
{
    readonly 天帝回收范围 批选范围=new 天帝回收范围();
    RectTransform 范围层;
    bool 操作框已打开=>确认已打开||范围层!=null;
    public 天帝回收范围 当前批选范围=>批选范围.副本();
    public bool 范围已打开=>范围层!=null;
    public void 设置批选范围(天帝回收范围 值)
    {
        if(值==null)return;
        批选范围.品阶掩码=值.品阶掩码&255;批选范围.类型=Mathf.Clamp(值.类型,0,5);批选范围.位置=Mathf.Clamp(值.位置,0,2);
    }
    IEnumerable<道纹实例> 范围候选(天帝回收范围 范围)
    {
        IEnumerable<道纹实例> 来源=范围.位置==0?数据.道纹:范围.位置==1?列表:列表.Skip(页*每页).Take(每页);
        return 来源.Where(范围.匹配);
    }
    public void 一键选中()
    {
        if(操作框已打开)return;
        选择.Clear();int 跳过=0;
        foreach(var 纹 in 范围候选(批选范围))
            if(数据.可回收(纹,out _))选择.Add(纹);else 跳过++;
        刷新();提示.text="已选 "+选择.Count+" 枚 · 跳过受保护 "+跳过+" 枚";
    }
    public void 打开范围()
    {
        if(操作框已打开)return;
        var 原状态=GetComponentsInChildren<Selectable>().ToDictionary(x=>x,x=>x.interactable);
        foreach(var 控件 in 原状态.Keys)控件.interactable=false;
        var 草稿=批选范围.副本();var 根=(RectTransform)transform;
        范围层=区块(根,"回收范围层",0,0,1600,900);
        var 遮=图(范围层,"范围遮罩",0,0,1600,900,null,new Color(0,0,0,.72f));遮.raycastTarget=true;
        var 框=图(范围层,"回收范围小窗",420,204,760,492,"一级面板").rectTransform;
        var 标题=字文(框,"一键选中范围",20,8,720,44,27);
        var 正文=区块(框,"范围正文",20,60,720,356);
        字文(正文,"勾选品阶（可多选）",0,0,720,28,19);
        var 品阶键=new Button[8];
        Button 类型键=null,位置键=null;Text 预览字=null;
        类型键=键(正文,"批选道纹类型","道纹类型",0,142,720,44,()=>{草稿.类型=(草稿.类型+1)%6;更新();});
        位置键=键(正文,"批选库存范围","库存范围",0,194,720,44,()=>{草稿.位置=(草稿.位置+1)%3;更新();});
        预览字=字文(正文,"",0,246,720,44,17,天帝道纹美术.次文);
        var 保护字=字文(正文,"自动跳过锁定、已放置及方案保护道纹。",0,298,720,48,17,天帝道纹美术.次文);
        for(int i=0;i<8;i++)
        {
            int 阶=i;
            品阶键[i]=键(正文,"批选品阶-"+i,((道纹品阶)i).ToString(),i%4*182,i/4*52+34,174,44,()=>{草稿.品阶掩码^=1<<阶;更新();});
        }
        void 更新()
        {
            for(int i=0;i<8;i++)
            {
                bool 选=(草稿.品阶掩码&(1<<i))!=0;
                品阶键[i].GetComponentInChildren<Text>().text=(选?"✓ ":"")+((道纹品阶)i);
                天帝道纹美术.选中((Image)品阶键[i].targetGraphic,选);
            }
            类型键.GetComponentInChildren<Text>().text="道纹类型："+new[]{"全部类型","属性道纹","分叉道纹","功能道纹","特性道纹","转化道纹"}[草稿.类型]+" ›";
            位置键.GetComponentInChildren<Text>().text="库存范围："+new[]{"整个背包","当前筛选","当前页"}[草稿.位置]+" ›";
            var 候选=范围候选(草稿).ToList();int 数=候选.Count(x=>数据.可回收(x,out _));
            预览字.text="可选 "+数+" 枚 · 受保护 "+(候选.Count-数)+" 枚";
        }
        void 关闭小窗()
        {
            var 旧=范围层;范围层=null;旧.gameObject.SetActive(false);
            if(Application.isPlaying)Destroy(旧.gameObject);else DestroyImmediate(旧.gameObject);
            foreach(var 项 in 原状态)if(项.Key!=null)项.Key.interactable=项.Value;
            刷新();
        }
        键(框,"取消批选范围","取消",20,432,344,44,关闭小窗);
        键(框,"应用批选范围","应用并选中",396,432,344,44,()=>{设置批选范围(草稿);关闭小窗();一键选中();},true);
        更新();
        if(天帝移动适配.启用)
        {
            var 口=天帝响应布局.滚动正文(正文);
            天帝响应布局.动态(正文);
            var 驱动=范围层.gameObject.AddComponent<天帝移动排版>();
            驱动.排版=层=>
            {
                float 宽=Mathf.Min(760,层.rect.width-24),高=Mathf.Min(492,层.rect.height-24),正文宽=宽-24;
                天帝双端页面布局.固定(框,(层.rect.width-宽)/2,(层.rect.height-高)/2,宽,高);
                天帝双端页面布局.固定(标题.rectTransform,12,4,正文宽,40);标题.fontSize=22;
                天帝双端页面布局.固定(口,12,48,正文宽,高-108);正文.sizeDelta=new Vector2(0,350);
                foreach(var 文 in 正文.GetComponentsInChildren<Text>())文.fontSize=16;
                foreach(RectTransform 子 in 正文)if(子.GetComponent<Text>()!=null)天帝双端页面布局.固定(子,0,-子.anchoredPosition.y,正文宽,子.rect.height);
                for(int i=0;i<8;i++)天帝双端页面布局.按键((RectTransform)品阶键[i].transform,i%4*(正文宽+6)/4,i/4*52+34,(正文宽-18)/4);
                天帝双端页面布局.按键((RectTransform)类型键.transform,0,142,正文宽);
                天帝双端页面布局.按键((RectTransform)位置键.transform,0,194,正文宽);
                天帝双端页面布局.按键(框.Find("取消批选范围") as RectTransform,12,高-52,(正文宽-8)/2);
                天帝双端页面布局.按键(框.Find("应用批选范围") as RectTransform,16+正文宽/2,高-52,(正文宽-8)/2);
            };
        }
    }
}
