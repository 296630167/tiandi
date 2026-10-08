#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class 天帝首两页验收
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
            typeof(天帝美术资源).GetProperty("当前").SetValue(null,美术);
            var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
            var 盒=new 天帝宝盒(网,42,500);var 钱=new 天帝通货(网,42);人=new 天帝主角属性(天帝普攻.主角配置(),网);
            游戏对象=new GameObject("首两页隔离验收");游戏对象.SetActive(false);游戏对象.hideFlags=HideFlags.HideAndDontSave;
            var 游戏=游戏对象.AddComponent<天帝游戏>();游戏.默认字体=AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf");游戏.美术=美术;游戏.主角立绘=美术.获取("CH01");
            设(游戏,"阶段",游戏阶段.主页);设(游戏,"主角属性",人);设(游戏,"道纹数据",网);设(游戏,"宝盒数据",盒);设(游戏,"通货数据",钱);
            设(游戏,"当前地图等级",1);设(游戏,"初始源道纹编号",0);设(游戏,"天赋池",天帝天赋池.从已选天赋恢复((int)天赋种类.普通人));
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
            界面.显示主页();拍("01_主页");
            var 导航=宿主.GetComponentsInChildren<天帝剪纸导航按钮>();
            var 轻纸=Resources.Load<Sprite>("剪纸界面/轻纸框");
            查("七个导航复用独立九宫格轻纸框",轻纸!=null&&导航.Length==7&&导航.All(b=>b.GetComponent<Image>().sprite==轻纸&&b.GetComponent<Image>().type==Image.Type.Sliced));
            var 指针=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            导航[0].OnPointerEnter(指针);查("轻纸导航悬停保留深色字与原图标",导航[0].标签.color==new Color32(18,55,47,255)&&导航[0].图标.color==Color.white);
            导航[0].OnPointerExit(指针);查("导航离开恢复深色字",导航[0].标签.color==new Color32(18,55,47,255));
            var 地图级=宿主.GetComponentsInChildren<Text>().First(t=>t.name=="地图等级实时值");
            查("地图等级数值保持居中",地图级.alignment==TextAnchor.MiddleCenter&&地图级.rectTransform.rect.width==192);
            游戏.打开道纹改造();拍("02_道纹改造");
            Button 键(string 名)=>宿主.GetComponentsInChildren<Button>().First(b=>b.name==名);
            查("十一种材料入口齐全",宿主.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("通货-"))==11);
            查("不可用材料仅整卡透明度为0.5且保留轻纸原色",宿主.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("通货-")).All(b=>!b.interactable&&b.GetComponent<Image>().sprite==轻纸&&b.GetComponent<Image>().color==Color.white&&b.colors.disabledColor==Color.white&&b.GetComponent<CanvasGroup>().alpha==.5f&&b.transform.Find("图标").GetComponent<Image>().color==Color.white));
            查("无法执行时原操作按钮透明度为0.5",!键("使用通货").interactable&&键("使用通货").GetComponent<Image>().sprite==天帝首两页山水素材.获取("墨绿按钮")&&键("使用通货").GetComponent<CanvasGroup>().alpha==.5f);
            var 改造=界面.改造页;键("选择目标道纹").onClick.Invoke();查("目标选择打开背包",改造.背包已打开);改造.关闭背包();
            查("无目标不能执行",!改造.可执行);改造.执行();查("无目标失败不扣通货",钱.导出库存().All(x=>x==0));
            var 普通=天帝道纹生成.创建(100,道纹分类.属性,道纹品阶.普通,new System.Random(42),道纹属性分组.普通);网.获得道纹(普通);
            钱.获得(通货种类.启灵石,2);改造.选目标(网.道纹.IndexOf(普通));
            查("自动选择符合条件材料",改造.当前通货==通货种类.启灵石&&改造.可执行);
            查("选中材料刷新保留轻纸框与浅绿选中色",键("通货-启灵石").GetComponent<Image>().sprite==轻纸&&键("通货-启灵石").GetComponent<Image>().color==new Color(.78f,.88f,.76f));
            查("可执行时操作按钮透明度恢复为1",键("使用通货").interactable&&键("使用通货").GetComponent<Image>().sprite==天帝首两页山水素材.获取("墨绿按钮")&&键("使用通货").GetComponent<CanvasGroup>().alpha==1f&&键("通货-启灵石").GetComponent<CanvasGroup>().alpha==1f);
            拍("03_选中道纹");
            var 操作字=键("使用通货").GetComponentInChildren<Text>();
            查("使用与消费文案完整容纳",操作字.preferredWidth<=操作字.rectTransform.rect.width&&操作字.preferredHeight<=操作字.rectTransform.rect.height);
            var 词行=键("词条-0").GetComponentInChildren<Text>();
            查("词条文字保留左右内边距",词行.rectTransform.anchoredPosition.x>=14&&词行.rectTransform.rect.width<=612);
            键("使用通货").onClick.Invoke();查("实际升阶只消耗一个材料",普通.品阶==道纹品阶.优秀&&钱.数量(通货种类.启灵石)==1);
            查("结果组件显示真实改造结果",改造.最近结果.Contains("启灵石"));
            查("条件变化后材料禁用",!键("通货-启灵石").interactable);
            查("条件变化后透明度同步恢复为0.5",键("通货-启灵石").GetComponent<CanvasGroup>().alpha==.5f&&键("使用通货").GetComponent<CanvasGroup>().alpha==.5f&&键("通货-启灵石").GetComponent<Image>().sprite==轻纸&&键("通货-启灵石").GetComponent<Image>().color==Color.white);
            拍("04_条件变化后禁用");
            钱.获得(通货种类.易纹砂,2);改造.选通货(通货种类.易纹砂);键("词条-0").onClick.Invoke();
            键("使用通货").onClick.Invoke();查("洗练选词条入口与消费保留",钱.数量(通货种类.易纹砂)==1&&普通.词条.Count>0);
            查("刷新后操作文字仍在独立纸签内",键("使用通货").GetComponentInChildren<Text>().rectTransform.sizeDelta.x<=键("使用通货").GetComponent<RectTransform>().rect.width);
            键("关闭通货").onClick.Invoke();查("关闭返回真实主页",游戏.阶段==游戏阶段.主页);
            var 等级=宿主.GetComponentsInChildren<Dropdown>().First();查("地图等级保留一到一百",等级.options.Count==100);
            等级.value=8;查("等级选择更新真实模型",游戏.当前地图等级==9&&宿主.GetComponentsInChildren<Text>().First(t=>t.name=="地图等级实时值").text=="9");
            键("开始历练").onClick.Invoke();查("开始历练仍先确认",界面.确认已打开);界面.关闭确认();
            查("两张封印地图不可点击",!宿主.GetComponentsInChildren<Button>().Any(b=>b.name.Contains("迷雾")||b.name.Contains("幽冥")));
            File.WriteAllText(Path.Combine(目录,"report.txt"),"正式一级普通人、500灵石、零通货截图；后续仅在隔离模型提供边界材料验证，未触及真实存档。\n通过 "+通过.Count+" 项\n"+string.Join("\n",通过)+"\n错误："+string.Join("\n",错误));
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
