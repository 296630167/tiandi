#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class 天帝图三四验收
{
    public static void 运行()
    {
        var 目录=Environment.GetEnvironmentVariable("TIANDI_CAPTURE_DIR");
        if(string.IsNullOrEmpty(目录))throw new Exception("需要隔离截图目录");
        Directory.CreateDirectory(目录);
        GameObject 宿主=null,游戏对象=null;天帝主角属性 人=null;
        var 错误=new System.Collections.Generic.List<string>();
        var 通过=new System.Collections.Generic.List<string>();
        void 查(string 名,bool 对){if(!对)throw new Exception(名);通过.Add(名);}
        var 原移动=天帝移动适配.验证移动平台;var 原屏幕=天帝移动适配.验证屏幕尺寸;var 原安全=天帝移动适配.验证安全区;var 原美术=天帝美术资源.当前;
        Application.LogCallback 日志=(文,栈,类)=>{if(类==LogType.Error||类==LogType.Exception)错误.Add(文+"\n"+栈);};
        Application.logMessageReceived+=日志;
        void 设(object 对象,string 名,object 值)=>对象.GetType().GetProperty(名).SetValue(对象,值);
        try
        {
            var 美术=AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
            foreach(var f in Directory.GetFiles("Assets/天帝/Resources/山水首两页","*.png"))AssetDatabase.ImportAsset(f.Replace('\\','/'),ImportAssetOptions.ForceUpdate);
            if(Environment.GetEnvironmentVariable("TIANDI_NEW_SKIN")=="1")foreach(var f in Directory.GetFiles("Assets/天帝/Resources/山水图三四","*.png"))AssetDatabase.ImportAsset(f.Replace('\\','/'),ImportAssetOptions.ForceUpdate);
            typeof(天帝美术资源).GetProperty("当前").SetValue(null,美术);
            bool 排版验收=Environment.GetEnvironmentVariable("TIANDI_LAYOUT_FIX")=="1";
            var 天赋=排版验收?天赋种类.双生矢:天赋种类.普通人;
            var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋));
            var 盒=new 天帝宝盒(网,42,500);var 钱=new 天帝通货(网,42);人=new 天帝主角属性(天帝普攻.主角配置(),网);
            游戏对象=new GameObject("首两页隔离验收");游戏对象.SetActive(false);游戏对象.hideFlags=HideFlags.HideAndDontSave;
            var 游戏=游戏对象.AddComponent<天帝游戏>();游戏.默认字体=AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf");游戏.美术=美术;游戏.主角立绘=美术.获取("CH01");
            设(游戏,"阶段",游戏阶段.主页);设(游戏,"主角属性",人);设(游戏,"道纹数据",网);设(游戏,"宝盒数据",盒);设(游戏,"通货数据",钱);
            设(游戏,"当前地图等级",1);设(游戏,"初始源道纹编号",0);设(游戏,"天赋池",天帝天赋池.从已选天赋恢复((int)天赋));
            天帝移动适配.验证移动平台=false;天帝移动适配.验证屏幕尺寸=new Vector2Int(1920,1080);天帝移动适配.验证安全区=new Rect(0,0,1920,1080);
            var 界面=new 天帝界面(游戏);设(游戏,"界面",界面);
            宿主=(GameObject)typeof(天帝界面).GetField("根",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(界面);
            宿主.transform.SetParent(null,false);宿主.hideFlags=HideFlags.HideAndDontSave;((RectTransform)宿主.transform).sizeDelta=new Vector2(1920,1080);
            宿主.GetComponent<CanvasScaler>().enabled=false;宿主.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            宿主.transform.localScale=Vector3.one;宿主.transform.position=Vector3.zero;((RectTransform)宿主.transform).sizeDelta=new Vector2(1920,1080);
            宿主.AddComponent<CanvasGroup>().alpha=0;
            void 拍(string 名)
            {
                界面.更新适配();Canvas.ForceUpdateCanvases();界面.更新适配();Canvas.ForceUpdateCanvases();
                foreach(var c in 宿主.GetComponentsInChildren<天帝按钮文字区域>())if(c.enabled)c.更新();
                Canvas.ForceUpdateCanvases();var 原=天帝青绿验收.图片目录;天帝青绿验收.图片目录=目录;
                try{天帝青绿验收.拍摄(宿主,名,1920,1080);}finally{天帝青绿验收.图片目录=原;}
            }
            Button 键(string 名)=>宿主.GetComponentsInChildren<Button>().First(b=>b.name==名);
            if(排版验收)
            {
                界面.显示主页();游戏.打开道纹();Canvas.ForceUpdateCanvases();
                var 空页=界面.道纹页;
                var 解锁点=RectTransformUtility.WorldToScreenPoint(null,空页.画布.rectTransform.TransformPoint(空页.画布.格位置(new Vector2Int(1,0))*空页.画布.缩放+空页.画布.平移));
                空页.点击(true,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=解锁点});
                拍("03_空藏匣双生矢");
                var 空提示=空页.GetComponentsInChildren<Text>().First(t=>t.text.StartsWith("藏匣尚空"));
                查("空藏匣文字居中且左右等距",空提示.alignment==TextAnchor.MiddleCenter&&空提示.rectTransform.anchoredPosition.x==18&&空提示.rectTransform.sizeDelta.x==234);
                查("固定道纹详情已移除",空页.transform.Find("山水固定详情框")==null);
                查("释放区域扩展为可用画布",空页.画布.工作区.width==878);
                空页.显示属性(网.已放置[Vector2Int.zero],new Vector2(640,500));
                查("悬停真实详情浮窗恢复",空页.浮窗显示);拍("03_悬停详情恢复");
                空页.指针离开();查("离开道纹隐藏浮窗",!空页.浮窗显示);
                查("空藏匣验证解锁可撤销",空页.撤销上一步());游戏.返回主页();
            }
            // 图像中的六枚道纹只存在于本次隔离模型，使用正式一级属性值。
            for(int i=0;i<6;i++)网.获得道纹(天帝道纹生成.创建(100+i,道纹分类.属性,(道纹品阶)(i%4),new System.Random(42+i)));
            界面.显示主页();游戏.打开道纹();拍("03_道纹构筑");
            var 页=界面.道纹页;
            var 演示=页.GetComponentInChildren<天帝道纹攻击演示>();for(int i=0;i<180;i++)演示.推进演示(1f/60);
            查("真实攻击演示命中可推进",演示.演示命中次数>0);
            查("31乘31拓扑保留",天帝道纹.边长==31);
            查("画布拖放输入保留",页.画布.GetComponent<天帝道纹输入>()!=null);
            查("六枚候选真实可筛选",页.筛选结果数==6);
            页.显示属性(网.道纹[0],new Vector2(640,500));页.指针离开();拍("03_道纹详情");
            查("预览真实参数已计算",页.当前演示参数!=null);
            查("未开放通路拒绝选择",!页.选择预览通路(3));
            查("空撤销拒绝",!页.撤销上一步());
            页.设置接口筛选(63,false);查("接口筛选应用",页.当前接口筛选==63);
            页.设置接口筛选(0,false);
            void 查弹窗字(string 层名)
            {
                var 层=页.transform.Find(层名);
                foreach(var b in 层.GetComponentsInChildren<Button>().Where(b=>b.name!="关闭筛选遮罩"))
                {
                    var 文=b.GetComponentInChildren<Text>();
                    查(层名+"按钮字可渲染 "+b.name,文!=null&&!string.IsNullOrEmpty(文.text)&&文.rectTransform.rect.width>24&&文.rectTransform.rect.height>20&&文.cachedTextGenerator.vertexCount>=4);
                }
            }
            if(排版验收)
            {
                页.打开筛选();拍("03_筛选文字修正");查弹窗字("道纹筛选层");
                键("基础属性").onClick.Invoke();查("分类按钮生效并关闭弹窗",!页.筛选已打开);
                页.打开筛选();键("显示全部").onClick.Invoke();
                页.打开筛选();键(天帝道纹.方向名[0]).onClick.Invoke();查("接口按钮仍可切换",页.当前接口筛选==1);
                键("按当前朝向匹配").onClick.Invoke();查("旋转匹配按钮生效",页.允许旋转筛选);
                键("清除接口").onClick.Invoke();拍("03_筛选再次打开");查弹窗字("道纹筛选层");页.关闭筛选();
            }
            页.打开布局方案();查("方案入口打开",页.筛选已打开);
            if(Environment.GetEnvironmentVariable("TIANDI_NEW_SKIN")=="1")查("方案禁用按钮透明度同步",页.GetComponentsInChildren<Button>().Where(t=>!t.interactable).All(t=>t.GetComponent<CanvasGroup>()!=null&&t.GetComponent<CanvasGroup>().alpha==.5f));
            if(排版验收)
            {
                拍("03_方案空槽文字修正");查弹窗字("布局方案层");
                键("保存当前").onClick.Invoke();查("布局保存按钮生效",网.布局方案[0].已保存);
                键("检查 / 载入").onClick.Invoke();查("检查后应用按钮启用",键("应用已检查的方案").interactable);
                拍("03_方案载入文字修正");查弹窗字("布局方案层");
                键("应用已检查的方案").onClick.Invoke();查("应用方案后按钮恢复半透明",!键("应用已检查的方案").interactable&&键("应用已检查的方案").GetComponent<CanvasGroup>().alpha==.5f);
                键("关闭").onClick.Invoke();页.打开布局方案();拍("03_方案再次打开");查弹窗字("布局方案层");
            }
            页.关闭筛选();查("方案关闭",!页.筛选已打开);
            if(排版验收)
            {
                查("主栏摘要保持可读行数",页.GetComponentsInChildren<Text>().Where(t=>t.text.StartsWith("本通路已接通")).All(t=>t.text.Split('\n').Length==2));
                查("完整预览诊断保留原始词条",页.构筑预览说明.Contains("原始词条"));
                键("加成来源").onClick.Invoke();拍("03_加成来源完整说明");
                查("完整链路来源仍可展开",页.筛选已打开&&页.GetComponentsInChildren<Text>().Any(t=>t.text=="有效加成来源")&&页.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("暂无有效属性加成")));
                页.关闭筛选();
            }
            游戏.返回主页();界面.显示角色();拍("04_角色属性");
            if(排版验收)foreach(var 标 in 宿主.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("标签-")))
            {
                var 数=标.transform.parent.Find("数值-"+标.name.Substring(3)).GetComponent<Text>();
                查("属性同字体字号行高 "+标.text,标.font==数.font&&标.fontSize==数.fontSize&&!标.resizeTextForBestFit&&!数.resizeTextForBestFit&&标.rectTransform.anchoredPosition.y==数.rectTransform.anchoredPosition.y&&标.rectTransform.rect.height==数.rectTransform.rect.height);
            }
            foreach(string 名 in new[]{"角色属性","战斗进阶","技能与形态","道纹分布"})
            {键("页签-"+名).onClick.Invoke();查("角色页签保留 "+名,界面.角色页!=null);}
            键("页签-角色属性").onClick.Invoke();
            界面.角色页.显示属性说明("力量",new Vector2(1100,350));拍("04_属性轻说明");
            File.WriteAllText(Path.Combine(目录,"说明布局.txt"),string.Join("\n",宿主.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("属性说明")).Select(t=>t.name+" 文="+t.text+" rect="+t.rectTransform.rect+" pos="+t.rectTransform.anchoredPosition+" fontsize="+t.fontSize+" active="+t.gameObject.activeInHierarchy)));
            界面.角色页.打开详细属性说明("力量",new Vector2(1100,350));
            查("真实属性公式可读",宿主.GetComponentsInChildren<Text>().Any(t=>t.name=="属性说明正文"&&t.text.Contains("力量")));
            拍("04_属性完整公式");界面.角色页.隐藏属性说明();
            if(Environment.GetEnvironmentVariable("TIANDI_NEW_SKIN")=="1")
            {
                查("独立角色全身资源接入",宿主.GetComponentsInChildren<Image>().Any(t=>t.name=="角色立绘"&&t.sprite==Resources.Load<Sprite>("山水图三四/角色立绘")));
                查("属性十枚图标接入",宿主.GetComponentsInChildren<Image>().Count(t=>t.name.StartsWith("山水属性图标-"))==10);
                界面.关闭角色();游戏.打开道纹();页=界面.道纹页;
                查("浅纸网格工作区接入",页.画布.山水工作区);
                查("只保留鼠标详情卡",页.GetComponentsInChildren<天帝道纹详情卡>(true).Length==1);
                查("禁用按钮只用半透明",页.GetComponentsInChildren<Button>().Where(t=>!t.interactable).All(t=>t.GetComponent<CanvasGroup>()!=null&&t.GetComponent<CanvasGroup>().alpha==.5f&&t.colors.disabledColor==Color.white));
                // 在隔离模型使用正式开局技能点，执行真实解锁、拖放、旋转与撤销入口。
                var 格=new Vector2Int(1,0);var 纹=网.道纹[0];纹.接口=1<<3;
                页.回到源点();Canvas.ForceUpdateCanvases();
                Vector2 点(Vector2Int g)=>RectTransformUtility.WorldToScreenPoint(null,页.画布.rectTransform.TransformPoint(页.画布.格位置(g)*页.画布.缩放+页.画布.平移));
                var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=点(格)};
                int 原点=网.技能点;页.点击(true,e);查("新工作区点击正确解锁并扣一点",网.格已解锁(格)&&网.技能点==原点-1);
                e.position=new Vector2(-200,-200);页.按下(纹,false,e);页.开始拖动(false,e);查("候选真实开始拖动",页.拖动中);
                e.position=点(格);页.拖动(e);查("新工作区拖放正确命中",页.放置反馈==true);页.结束拖动(e);
                查("放置写入真实格子且接通",纹.格子==格&&纹.生效);
                页.显示属性(纹,e.position);拍("03_接通实际预览");
                int 接口=纹.接口;e.button=PointerEventData.InputButton.Right;页.点击(true,e);查("右键旋转保留",纹.接口!=接口);
                查("旋转可撤销且恢复接口",页.撤销上一步()&&纹.接口==接口);
                int 余点=网.技能点;var 原格=纹.格子;
                e.button=PointerEventData.InputButton.Left;e.position=RectTransformUtility.WorldToScreenPoint(null,页.transform.Find("山水战斗变化框").position);
                页.点击(true,e);查("战斗变化区域不会误操作网格",网.技能点==余点&&纹.格子==原格);
                页.显示属性(纹,e.position);页.指针离开();拍("03_道纹构筑最终");
            }
            File.WriteAllText(Path.Combine(目录,"report.txt"),"正式一级"+天赋+"；空库存、长源纹说明与六枚候选仅隔离验收，不写真实存档。\n通过 "+通过.Count+" 项\n"+string.Join("\n",通过)+"\n错误："+string.Join("\n",错误));
            if(错误.Count>0)throw new Exception("界面渲染出现错误");
        }
        finally
        {
            Application.logMessageReceived-=日志;if(人!=null)人.Dispose();if(宿主!=null)UnityEngine.Object.DestroyImmediate(宿主);if(游戏对象!=null)UnityEngine.Object.DestroyImmediate(游戏对象);
            天帝移动适配.验证移动平台=原移动;天帝移动适配.验证屏幕尺寸=原屏幕;天帝移动适配.验证安全区=原安全;typeof(天帝美术资源).GetProperty("当前").SetValue(null,原美术);
        }
    }
}
#endif
