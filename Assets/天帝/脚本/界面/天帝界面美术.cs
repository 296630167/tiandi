using UnityEngine;
using UnityEngine.UI;

public static class 天帝界面美术
{
    public static void 面板(Image 图,string 素材,Color 色)
    {
        var s=天帝道纹美术.获取(素材);if(s==null)return;
        图.sprite=s;图.type=天帝剪纸界面皮肤.已启用&&素材.Contains("按钮")?Image.Type.Simple:Image.Type.Sliced;图.color=色;图.pixelsPerUnitMultiplier=天帝剪纸界面皮肤.已启用?(素材.Contains("按钮")?1:2):天帝道纹美术.彩绘皮肤?2:1;
    }
    public static void 自动面板(Image 图,string 名,float 宽,float 高,Color 原色,bool 淡纸=false)
    {
        if(原色.a<.08f||名.Contains("遮罩")||名.Contains("图标")||名.Contains("立绘")||名.Contains("进度")||名.Contains("血条")||名.Contains("填充")||名.Contains("手柄")||名.Contains("轨道")||名.Contains("高亮"))return;
        bool 框=名.Contains("面板")||名.Contains("窗口")||名.Contains("详情")||名.Contains("信息")||名.Contains("日志")||名.Contains("字卡")||名.Contains("衬底")||名.Contains("标题")||名.Contains("状态")||名.Contains("底")||名.Contains("通货说明")||名.Contains("道纹实力")||名.Contains("宝盒")||名.Contains("源道纹-");
        if(!框||宽<80||高<35)return;
        // 宽而浅的信息条使用同一生成纸材的留白中心，避免角饰压住靠边的文字。
        if(天帝青绿皮肤.已启用&&(名=="重点属性衬底"||高<=100&&宽>高*3))
        {面板(图,"小信息框",Color.white);return;}
        面板(图,淡纸?"属性面板":宽>600&&高>400?"一级面板":"二级面板",Color.white);
    }
    public static void 按钮(Button 键,bool 主=false)
    {
        天帝按钮声音.绑定(键);
        if (天帝移动适配.启用 && 键.GetComponent<天帝触控热区>() == null) 键.gameObject.AddComponent<天帝触控热区>();
        var 图=键.targetGraphic as Image;if(图==null)return;
        var 参考=图.GetComponent<天帝比例矩形>();
        Vector2 大小=参考!=null&&参考.参考尺寸.y>0?参考.参考尺寸:图.rectTransform.rect.size;
        float 高=大小.y;
        bool 关闭键 = 天帝按钮文字区域.是关闭入口(键);
        if (关闭键) 主 = false;
        bool 卡片=!关闭键&&!主&&高>80,紧凑=!关闭键&&!主&&(高<48||键.name.Contains("页签"));
        if(卡片||紧凑)选项(图,false);
        else 面板(图,主?"确认按钮":"按钮",Color.white);
        图.raycastTarget=true;
        var 色=键.colors;色.normalColor=Color.white;色.selectedColor=Color.white;色.highlightedColor=new Color(1.04f,1.04f,1.02f);
        色.pressedColor=new Color(.83f,.91f,.87f);色.disabledColor=new Color(.79f,.83f,.79f,1);色.fadeDuration=.12f;键.colors=色;
        if(卡片)return;
        foreach(var 文 in 键.GetComponentsInChildren<Text>())
        {
            文.color=主?天帝道纹美术.浅字:天帝道纹美术.正文;
            文.fontStyle=FontStyle.Bold;
            if(!紧凑)
            {
                标题(文);
            }
            else 文.fontSize=Mathf.Clamp(文.fontSize,16,高<=38?17:20);
        }
        天帝按钮文字区域.绑定(键);
    }
    public static void 文字(Text 文,string 名=null)
    {
        if(!天帝道纹美术.彩绘皮肤)return;
        名=名??文.name;
        bool 是标题=名.Contains("标题")||名=="角色"||名=="名称"||名=="道纹名称"||名=="天赋名称";
        bool 短标题=文.fontSize>=24&&!string.IsNullOrEmpty(文.text)&&文.text.Length<=22&&!文.text.Contains("\n")&&!文.text.Contains("<")&&!文.text.Contains("/次")&&!文.text.Contains(" / ")&&!double.TryParse(文.text,out _);
        if(是标题||短标题)标题(文);
        else if(文.fontSize>=24||名.StartsWith("数值-"))文.fontStyle=FontStyle.Bold;
    }
    public static void 标题(Text 文)
    {
        var 宋=天帝美术资源.当前?.主页标题字体;
        if(宋==null){文.fontStyle=FontStyle.Bold;return;}
        文.font=宋;文.fontStyle=FontStyle.Normal;
        // 粗宋体的行高较大，短工具仍用黑体，标题按实际容器留足行高。
        float 高=文.rectTransform.sizeDelta.y;
        if(高>0)文.fontSize=Mathf.Min(文.fontSize,Mathf.Max(16,Mathf.FloorToInt((高-4)/1.5f)));
    }
    public static void 选项(Image 图,bool 选中,bool 锁定=false)
    {
        if (天帝剪纸界面皮肤.已启用 && 图.rectTransform.rect.height <= 70 && 图.GetComponent<Button>() != null)
        {
            图.sprite=天帝剪纸界面皮肤.素材("轻纸框");图.type=Image.Type.Sliced;图.pixelsPerUnitMultiplier=2;
            图.color=选中?new Color(.82f,.91f,.82f):Color.white;图.raycastTarget=true;
            foreach(var 文 in 图.GetComponentsInChildren<Text>())文.color=天帝剪纸界面皮肤.墨;
            return;
        }
        面板(图,"小信息框",锁定?new Color(.91f,.93f,.87f):选中?(天帝剪纸界面皮肤.已启用?new Color(1,.85f,.77f):new Color(.79f,.91f,.86f)):Color.white);
        图.raycastTarget=true;
        if(图.transform.Find("控件细边")==null)
        {
            var 框=new GameObject("控件细边",typeof(RectTransform)).GetComponent<RectTransform>();框.SetParent(图.transform,false);
            框.anchorMin=Vector2.zero;框.anchorMax=Vector2.one;框.offsetMin=框.offsetMax=Vector2.zero;
            for(int i=0;i<4;i++)
            {
                var 边=new GameObject("边"+i,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();边.SetParent(框,false);
                bool 横=i<2;边.anchorMin=横?new Vector2(0,i):new Vector2(i-2,0);边.anchorMax=横?new Vector2(1,i):new Vector2(i-2,1);
                边.pivot=横?new Vector2(.5f,i):new Vector2(i-2,.5f);边.sizeDelta=横?new Vector2(-2,1):new Vector2(1,-2);边.anchoredPosition=Vector2.zero;
                var 像=边.GetComponent<Image>();像.color=new Color(.30f,.48f,.44f,.40f);像.raycastTarget=false;
            }
        }
        var 变=图.transform.Find("控件选择线") as RectTransform;
        if(变==null)
        {
            变=new GameObject("控件选择线",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();变.SetParent(图.transform,false);
            变.anchorMin=new Vector2(0,0);变.anchorMax=new Vector2(1,0);变.pivot=new Vector2(.5f,0);
            变.offsetMin=new Vector2(6,1);变.offsetMax=new Vector2(-6,4);
            var 线=变.GetComponent<Image>();线.color=天帝道纹美术.强调;线.raycastTarget=false;
        }
        变.gameObject.SetActive(选中);
    }
}
