#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class 天帝改造背包验证
{
    // 独立隐藏界面，不进入Play、不读取或写入玩家存档。
    public static string 验证材料锁定()
    {
        var r=new 报告();GameObject host=null;
        void 查(string n,bool v)=>(v?r.通过:r.失败).Add(n);
        try
        {
            var game=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
            var font=game?.默认字体??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var w=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));var c=new 天帝通货(w,42,3);
            var 普通=天帝道纹生成.创建(100,道纹分类.属性,道纹品阶.普通,new System.Random(42));w.获得道纹(普通);
            var 分叉=天帝道纹生成.创建(100,道纹分类.分叉,道纹品阶.普通,new System.Random(43));w.获得道纹(分叉);
            host=new GameObject("独立材料锁定验证",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));host.hideFlags=HideFlags.HideAndDontSave;
            host.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var group=host.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
            var root=new GameObject("独立改造页",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(host.transform,false);
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(0,1);root.sizeDelta=new Vector2(1600,900);
            var page=root.gameObject.AddComponent<天帝通货界面>();page.初始化(w,c,font,()=>{});
            Button 键(通货种类 k)=>root.GetComponentsInChildren<Button>().Single(x=>x.name=="通货-"+k);
            void 点(通货种类 k)=>ExecuteEvents.Execute(键(k).gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            void 校验锁定(string 场景)
            {
                foreach(var k in 天帝通货.可用种类)
                {
                    bool usable=c.可使用(k,page.当前目标,0,out _);
                    查(场景+" "+k+"按钮与改造条件一致",键(k).interactable==usable);
                    if(!usable)
                    {
                        var old=page.当前通货;点(k);page.选通货(k);
                        查(场景+" "+k+"禁用点击及直接选择均无效且说明可读",page.当前通货==old&&键(k).GetComponent<CanvasGroup>().alpha>=.9f);
                    }
                }
            }
            查("未选道纹时无材料选中且不可执行",(int)page.当前通货==-1&&!page.可执行);校验锁定("未选择");
            page.选目标(0);查("普通道纹默认启灵石",page.当前通货==通货种类.启灵石&&page.可执行);校验锁定("普通");
            var stocks=c.导出库存();stocks[(int)通货种类.启灵石]=0;c.读取库存(stocks);
            查("数量归零自动切到点玄石",page.当前通货==通货种类.点玄石&&!键(通货种类.启灵石).interactable);
            点(通货种类.无瑕玉);查("可用材料可以手动选中",page.当前通货==通货种类.无瑕玉);
            page.执行();查("升至完美后自动选添蕴砂",普通.品阶==道纹品阶.完美&&page.当前通货==通货种类.添蕴砂&&c.数量(通货种类.无瑕玉)==2);校验锁定("完美");
            while(普通.词条.Count<普通.词条上限)page.执行();
            查("词条满额后添蕴砂锁定并默认易纹砂",!键(通货种类.添蕴砂).interactable&&page.当前通货==通货种类.易纹砂);校验锁定("满词条");
            for(int i=0;i<3;i++)page.执行();
            查("洗练材料耗尽自动选重铸石",c.数量(通货种类.易纹砂)==0&&!键(通货种类.易纹砂).interactable&&page.当前通货==通货种类.重铸石);
            c.读取库存(new int[13]);查("无可用材料时清空选择禁用执行",(int)page.当前通货==-1&&!page.可执行);校验锁定("空库存");
            c.启用无限通货();page.选目标(1);查("分叉道纹全部锁定",(int)page.当前通货==-1&&!page.可执行);校验锁定("分叉");
            var 期望=new[]{通货种类.启灵石,通货种类.凝华石,通货种类.蕴玄石,通货种类.琢天玉,通货种类.添蕴砂,通货种类.添蕴砂,通货种类.添蕴砂,通货种类.添蕴砂};
            for(int i=0;i<8;i++)
            {
                typeof(道纹实例).GetProperty("品阶").SetValue(普通,(道纹品阶)i);普通.词条.Clear();普通.词条.Add(new 道纹词条(道纹属性.力量,2));
                page.选目标(0);查("无限模式 "+(道纹品阶)i+"默认第一个可用材料",page.当前通货==期望[i]);校验锁定("无限 "+(道纹品阶)i);
                点(通货种类.重铸石);page.选目标(0);查("重新选道纹重置默认材料 "+i,page.当前通货==期望[i]);
            }
            typeof(道纹实例).GetProperty("品阶").SetValue(普通,道纹品阶.普通);
            stocks=new int[13];stocks[(int)通货种类.问天石]=1;stocks[(int)通货种类.易纹砂]=1;c.读取库存(stocks);page.选目标(0);
            查("默认顺序按视觉位置问天石优先于洗练",page.当前通货==通货种类.问天石);
            c.获得(通货种类.启灵石,1);点(通货种类.启灵石);page.选目标(1);page.选目标(0);
            查("掉落解锁后重新选目标按首个可用选择",page.当前通货==通货种类.启灵石);
        }
        catch(Exception ex){r.错误.Add(ex.ToString());}
        finally{if(host!=null)UnityEngine.Object.DestroyImmediate(host);}
        string dir=Path.Combine(天帝构建工具.项目根,"生成/验证/材料锁定-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(r,true));return dir;
    }
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(),失败 = new List<string>(),错误 = new List<string>(); }
    static 报告 结果;
    static string 目录;
    static 天帝通货界面 页面;
    static 天帝道纹 数据;
    static 天帝通货 通货;
    static Font 字体;
    static 天帝美术资源 美术, 原美术;
    static SceneSetup[] 原场景;
    static bool 原后台,原启用,已完成;
    static EnterPlayModeOptions 原模式;
    static double 截止;
    static MonoBehaviour 协程宿主;
    static void 检查(string 名,bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    public static string 启动()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("等待编辑器空闲。");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); if (scene.isDirty) throw new InvalidOperationException("当前场景未保存。");
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (游戏 == null) throw new InvalidOperationException("请在天帝主场景运行此检查。");
        字体 = 游戏.默认字体; 美术 = 游戏.美术; 原美术 = 天帝美术资源.当前;
        结果 = new 报告(); 已完成 = false; 页面 = null; 协程宿主 = null;
        目录 = Path.Combine(天帝构建工具.项目根,"生成/验证/改造背包-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        原场景 = EditorSceneManager.GetSceneManagerSetup(); 原后台 = Application.runInBackground;
        原启用 = EditorSettings.enterPlayModeOptionsEnabled; 原模式 = EditorSettings.enterPlayModeOptions;
        // An isolated temporary scene avoids starting or saving the user's gameplay session.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        Application.runInBackground = true; EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        截止 = EditorApplication.timeSinceStartup + 50; Application.logMessageReceived += 日志; EditorApplication.update += 更新; EditorApplication.isPlaying = true;
        return 目录;
    }
    static void 日志(string 文,string 栈,LogType 类) { if (类 == LogType.Error || 类 == LogType.Exception) 结果.错误.Add(文 + "\n" + 栈); }
    static void 更新()
    {
        if (已完成) return; EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("背包验证超时"); 结束(); return; }
        if (!EditorApplication.isPlaying || 协程宿主 != null) return;
        var 根 = new GameObject("改造背包验证",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        根.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scale = 根.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1600,900); scale.matchWidthOrHeight = .5f;
        new GameObject("界面输入",typeof(EventSystem),typeof(InputSystemUIInputModule));
        var r = new GameObject("道纹改造页",typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(根.transform,false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0,1); r.sizeDelta = new Vector2(1600,900);
        数据 = new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人)); 通货 = new 天帝通货(数据,42,3);
        typeof(天帝美术资源).GetProperty("当前").SetValue(null,美术);
        页面 = r.gameObject.AddComponent<天帝通货界面>(); 页面.初始化(数据,通货,字体,() => { });
        协程宿主 = 页面; 页面.StartCoroutine(保护(流程()));
    }
    static IEnumerator 保护(IEnumerator 流)
    {
        var 栈 = new Stack<IEnumerator>(); 栈.Push(流);
        while (栈.Count > 0)
        {
            bool 有; object 项 = null;
            try { 有 = 栈.Peek().MoveNext(); if (有) 项 = 栈.Peek().Current; }
            catch (Exception e) { 结果.错误.Add(e.ToString()); break; }
            if (!有) { 栈.Pop(); continue; } if (项 is IEnumerator 子) { 栈.Push(子); continue; } yield return 项;
        }
        结束();
    }
    static Button 按钮(string 名) => 页面.GetComponentsInChildren<Button>().Single(x => x.name == 名);
    static void 点(string 名)
    {
        var b = 按钮(名); var e = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(b.gameObject,e,ExecuteEvents.pointerClickHandler);
    }
    static Dropdown 下拉(string 名) => 页面.GetComponentsInChildren<Dropdown>().Single(x => x.name == 名);
    static bool 可命中(Button b)
    {
        Canvas.ForceUpdateCanvases(); var r = (RectTransform)b.transform;
        var e = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(e,hits);
        bool 对 = hits.Count > 0 && (hits[0].gameObject == b.gameObject || hits[0].gameObject.transform.IsChildOf(b.transform));
        if (!对) File.AppendAllText(Path.Combine(目录,"raycast.txt"),b.name + " => " + string.Join(",",hits.Select(x => x.gameObject.name)) + "\n");
        return 对;
    }
    static IEnumerator 拍(string 名)
    {
        Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        var 图 = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(目录,名 + ".png"),图.EncodeToPNG()); UnityEngine.Object.Destroy(图);
    }
    static 天帝道纹背包 背包 => 页面.GetComponentInChildren<天帝道纹背包>();
    static IEnumerator 流程()
    {
        yield return null;
        检查("空库存显示可点击选择按钮",页面.当前目标 == null && 按钮("选择目标道纹").interactable);
        点("选择目标道纹"); yield return null;
        检查("空背包可打开且无报错",页面.背包已打开 && 背包.筛选结果数 == 0 && 页面.GetComponentsInChildren<Text>().Any(x => x.text == "背包暂无道纹"));
        点("关闭背包"); yield return null; 检查("关闭空背包仍无目标",!页面.背包已打开 && 页面.当前目标 == null);
        数据.设置玩家等级(40);
        for (int i = 0; i < 38; i++)
        {
            var 纹 = 天帝道纹生成.创建(1,道纹分类.属性,(道纹品阶)(i % 8),new System.Random(100 + i));
            纹.词条.Clear(); 纹.词条.Add(new 道纹词条((道纹属性)(i % 19),2)); 纹.接口 = 9; 数据.获得道纹(纹);
        }
        数据.解锁格子(new Vector2Int(1,0)); 数据.放置(数据.道纹[0],new Vector2Int(1,0));
        检查("有库存也不自动选择目标",页面.当前目标 == null && !页面.可执行);
        检查("目标按钮实际射线可点击",可命中(按钮("选择目标道纹")));
        yield return 拍("01-未选择"); 点("选择目标道纹"); yield return null;
        检查("所有普通道纹含已放置进入网格并分页",背包.筛选结果数 == 38 && 背包.总页数 == 2 && 背包.显示项(0) == 数据.道纹[0]);
        点("背包下一页"); 检查("翻页显示剩余14枚",背包.页码 == 1 && 背包.显示项(0) == 数据.道纹[24] && 背包.显示项(14) == null);
        点("背包上一页"); yield return null;
        var 格 = 按钮("背包道纹-0"); Canvas.ForceUpdateCanvases();
        var pos = RectTransformUtility.WorldToScreenPoint(null,格.transform.position);
        var hover = new PointerEventData(EventSystem.current) { position = pos };
        ExecuteEvents.Execute(格.gameObject,hover,ExecuteEvents.pointerEnterHandler);
        检查("悬停显示分区详情且不挡射线",背包.详情显示 && 背包.GetComponentInChildren<天帝道纹详情卡>().当前道纹 == 数据.道纹[0] &&
            页面.GetComponentsInChildren<Text>().Any(x => x.text == "属性词条  1 / 1") && 背包.GetComponentInChildren<CanvasGroup>().blocksRaycasts == false);
        检查("详情不会遮挡背包格点击",可命中(格));
        背包.显示详情(0,new Vector2(Screen.width - 1,1)); Canvas.ForceUpdateCanvases();
        var tip = (RectTransform)背包.GetComponentInChildren<CanvasGroup>().transform; var corners = new Vector3[4]; tip.GetWorldCorners(corners);
        检查("右下角详情始终在屏幕内",corners.All(x => x.x >= 0 && x.x <= Screen.width && x.y >= 0 && x.y <= Screen.height));
        背包.显示详情(0,pos);
        yield return 拍("02-背包悬停"); ExecuteEvents.Execute(格.gameObject,hover,ExecuteEvents.pointerExitHandler);
        检查("移出隐藏详情",!背包.详情显示);
        下拉("背包属性筛选").value = 4;
        检查("元素筛选检查所有词条",背包.筛选结果数 == 数据.道纹.Count(x => x.词条.Exists(t => 天帝道纹属性.分组(t.属性) == 道纹属性分组.元素)));
        下拉("背包品阶筛选").value = 1;
        检查("属性品阶组合筛选",Enumerable.Range(0,32).Select(背包.显示项).Where(x => x != null).All(x => x.品阶 == 道纹品阶.传说 && x.词条.Exists(t => 天帝道纹属性.分组(t.属性) == 道纹属性分组.元素)));
        下拉("背包状态筛选").value = 2;
        检查("无匹配明确空状态",背包.筛选结果数 == 0 && 页面.GetComponentsInChildren<Text>().Any(x => x.text == "没有符合筛选条件的道纹"));
        下拉("背包品阶筛选").value = 0; 下拉("背包属性筛选").value = 0; 下拉("背包状态筛选").value = 0;
        var dropdown = 下拉("背包品阶筛选"); dropdown.Show(); yield return new WaitForSeconds(.2f);
        检查("实际下拉菜单正确生成",页面.GetComponentsInChildren<Toggle>().Count(x => x.name.StartsWith("Item")) >= 9);
        yield return 拍("03-筛选下拉");
        var option = 页面.GetComponentsInChildren<Toggle>().First(x => x.GetComponentInChildren<Text>()?.text == "普通"); option.isOn = true;
        yield return new WaitForSeconds(.2f);
        检查("点击下拉选项应用品阶筛选",dropdown.value == 8 && 背包.筛选结果数 == 5 && 背包.显示项(0).品阶 == 道纹品阶.普通);
        dropdown.value = 0;
        下拉("背包排序").value = 1; 检查("品阶高到低排序",背包.显示项(0).品阶 == 道纹品阶.传说);
        下拉("背包排序").value = 0;
        点("背包道纹-0"); yield return null;
        检查("点击选择后关闭背包且不卸载道纹",!页面.背包已打开 && 页面.当前目标 == 数据.道纹[0] && 数据.道纹[0].格子 == new Vector2Int(1,0));
        检查("提示按钮变为同一实例图标",页面.GetComponentsInChildren<天帝道纹绘图>().Single().单纹 == 页面.当前目标 && !按钮("选择目标道纹").GetComponentsInChildren<Text>().Any());
        yield return 拍("04-选中目标"); 点("选择目标道纹"); yield return null;
        检查("图标再次打开背包且当前项锁定",页面.背包已打开 && !按钮("背包道纹-0").interactable && 页面.GetComponentsInChildren<Text>().Any(x => x.text == "当前选择 · 锁定"));
        ExecuteEvents.Execute(按钮("背包道纹-0").gameObject,hover,ExecuteEvents.pointerEnterHandler);
        检查("锁定项仍可查看详情",背包.详情显示);
        点("背包道纹-0"); 检查("锁定项不能重复选中",页面.背包已打开 && 页面.当前目标 == 数据.道纹[0]);
        yield return 拍("05-当前目标锁定"); 点("背包道纹-1"); yield return null;
        检查("可选择其他道纹并解锁原目标",页面.当前目标 == 数据.道纹[1] && !页面.背包已打开);
        点("选择目标道纹"); yield return null; 检查("切换后只锁定新目标",按钮("背包道纹-0").interactable && !按钮("背包道纹-1").interactable);
        点("关闭背包"); yield return null;
        页面.选目标(0); 页面.选通货(通货种类.启灵石); 页面.执行();
        检查("新选择流程仍能改造扣通货刷新图标",数据.道纹[0].品阶 == 道纹品阶.优秀 && 通货.数量(通货种类.启灵石) == 2 && 页面.当前目标 == 数据.道纹[0]);
        页面.打开背包(); yield return null;
        检查("改造后背包锁定详情即时刷新",背包.显示项(0).品阶 == 道纹品阶.优秀 && !按钮("背包道纹-0").interactable);
        Canvas.ForceUpdateCanvases();
        检查("背包文字不溢出",背包.GetComponentsInChildren<Text>().All(x => x.preferredHeight <= x.rectTransform.rect.height + 1));
        页面.关闭背包(); yield return null;
        检查("关闭重开无残留详情",页面.GetComponentsInChildren<天帝道纹背包>().Length == 0);
    }
    static void 结束()
    {
        if (已完成) return; 已完成 = true; EditorApplication.update -= 更新;
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true)); EditorApplication.isPlaying = false; EditorApplication.update += 恢复;
    }
    static void 恢复()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= 恢复; Application.logMessageReceived -= 日志;
        EditorSettings.enterPlayModeOptionsEnabled = 原启用; EditorSettings.enterPlayModeOptions = 原模式; Application.runInBackground = 原后台; typeof(天帝美术资源).GetProperty("当前").SetValue(null,原美术);
        EditorSceneManager.RestoreSceneManagerSetup(原场景); File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true)); 页面 = null; 协程宿主 = null;
    }
}
#endif
