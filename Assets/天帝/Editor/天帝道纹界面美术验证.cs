#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 真实主页入口及UGUI事件验证；演示库存只存在隔离存档，退出后恢复编辑器设置。
public static class 天帝道纹界面美术验证
{
    [Serializable] sealed class 报告
    {
        public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>();
        public int 宽, 高; public string 说明 = "隔离存档的Play Mode截图，非真实玩家库存；新生图组件已用于实际运行界面";
    }
    static 报告 结果;
    static string 目录, 原存档;
    static bool 原后台, 原选项开, 已开始, 已结束;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static 天帝游戏 游戏;
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static void 错误(string 文, string 栈, LogType 类) { if (类 == LogType.Exception || 类 == LogType.Error) 结果.错误.Add(文 + "\n" + 栈); }
    static object 私调(object 对象, string 名, params object[] 参数)
        => 对象.GetType().GetMethod(名, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(对象, 参数);
    public static string 启动()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("需停止运行，且当前场景没有未保存修改；不会覆盖场景。");
        if (EditorSceneManager.GetActiveScene().path != "Assets/天帝/场景/天帝.unity") throw new InvalidOperationException("需在已保存的主页场景运行。");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/道纹UI-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        结果 = new 报告(); 资源检查();
        var 网 = 夹具();
        var 存 = new 天帝存档(Path.Combine(目录,"隔离存档"));
        var 数据 = new 天帝存档数据 { 序章已完成=true, 主角=天帝普攻.主角配置(), 画布=网.导出存档(), 通货=new 天帝通货(网,5).导出库存() };
        if (!存.保存(数据)) throw new InvalidOperationException("隔离存档无法保存");
        原存档=天帝存档.验证目录; 天帝存档.验证目录=Path.Combine(目录,"隔离存档");
        原后台=Application.runInBackground; 原选项开=EditorSettings.enterPlayModeOptionsEnabled; 原选项=EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled=true; EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        已开始=已结束=false; 截止=EditorApplication.timeSinceStartup+90;
        Application.logMessageReceived+=错误; EditorApplication.update+=等待; EditorApplication.playModeStateChanged+=退出;
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus(); EditorApplication.isPlaying=true;
        return 目录;
    }
    static void 资源检查()
    {
        var 美术=AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        检查("构筑独立图集已引用",美术.道纹构筑图集 != null && 美术.道纹构筑图集.width==2048);
        foreach(var 名 in new[]{"页面背景","一级面板","属性面板","道纹卡槽","详情框","按钮","选中页签","锁定页签","分割线","已装备","未装备","旋转图标","滚动滑块","选中框","品质框"})
        {
            var 图=美术.获取("DWUI_"+名); 检查("运行时引用:"+名,图!=null);
            if(图==null) continue;
            var 入=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(图));
            检查("alpha/过滤:"+名,入.alphaIsTransparency && !入.mipmapEnabled && 入.wrapMode==TextureWrapMode.Clamp && 入.textureCompression==TextureImporterCompression.Uncompressed);
            if(名=="一级面板" || 名=="按钮" || 名=="详情框") 检查("九宫格:"+名,图.border.x>0 && 图.border.y>0);
        }
    }
    static 天帝道纹 夹具()
    {
        var 网=new 天帝道纹(52,天帝天赋.获取((int)天赋种类.普通人)); 网.设置玩家等级(10);
        var 存=网.导出存档(); 存.技能点=50; 网=天帝道纹.读取存档(存);
        加(网,道纹属性.力量,4,9,new Vector2Int(1,0));
        加(网,道纹属性.数量,2,9,new Vector2Int(2,0));
        加(网,道纹属性.分裂,1,9,new Vector2Int(3,0));
        加(网,道纹属性.火,3,9,new Vector2Int(4,0));
        加(网,道纹属性.连锁,2,36,new Vector2Int(1,-1));
        加(网,道纹属性.金,5,36,new Vector2Int(2,-2));
        加(网,道纹属性.防御,3,1,new Vector2Int(-2,0));
        加(网,道纹属性.智力,3,9,null);
        加(网,道纹属性.范围,1,9,null);
        加(网,道纹属性.攻速,8,9,null);
        加(网,道纹属性.血量,15,9,null);
        加(网,道纹属性.移速,6,12,null);
        网.解锁格子(new Vector2Int(5,0));
        网.保存布局方案(0,"齐射分裂 / 连锁");
        return 网;
    }
    static void 加(天帝道纹 网, 道纹属性 属性,int 值,int 口,Vector2Int? 格)
    {
        var 纹=天帝道纹生成.创建(1,道纹分类.属性,道纹品阶.普通,new System.Random(3));
        纹.词条.Clear(); 纹.词条.Add(new 道纹词条(属性,值)); 纹.接口=口; 网.获得道纹(纹);
        if(格.HasValue) { 网.解锁格子(格.Value); if(!网.放置(纹,格.Value)) throw new InvalidOperationException("测试放置失败"); }
    }
    static void 等待()
    {
        if(已结束) return;
        if(EditorApplication.timeSinceStartup>截止) { 结果.错误.Add("运行验证超时"); 完成(); return; }
        if(!EditorApplication.isPlaying) return;
        Application.runInBackground=true; EditorApplication.QueuePlayerLoopUpdate();
        游戏=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if(游戏==null || 已开始) return;
        已开始=true; 游戏.StartCoroutine(捕获(运行()));
    }
    static IEnumerator 捕获(IEnumerator 程)
    {
        while(true)
        {
            bool 下; object 物=null;
            try { 下=程.MoveNext(); if(下) 物=程.Current; }
            catch(Exception ex) { 结果.错误.Add(ex.ToString()); 完成(); yield break; }
            if(!下) yield break; yield return 物;
        }
    }
    static Button 键(string 名) => 游戏.GetComponentsInChildren<Button>().First(b=>b.name==名 && b.gameObject.activeInHierarchy);
    static void 点击(string 名)
    {
        var b=键(名); var 区=(RectTransform)b.transform; Canvas.ForceUpdateCanvases();
        var e=new PointerEventData(EventSystem.current) {position=RectTransformUtility.WorldToScreenPoint(null,区.TransformPoint(区.rect.center))};
        var 命中=new List<RaycastResult>(); EventSystem.current.RaycastAll(e,命中);
        检查("真实指针可达:"+名,b.interactable && 命中.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(命中[0].gameObject)==b.gameObject);
        if(!b.interactable) throw new InvalidOperationException("按钮被禁用："+名);
        ExecuteEvents.Execute(b.gameObject,e,ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator 截(string 名)
    {
        yield return new WaitForEndOfFrame(); var t=ScreenCapture.CaptureScreenshotAsTexture();
        if(t==null) throw new InvalidOperationException("实机截图为空");
        结果.宽=t.width; 结果.高=t.height; File.WriteAllBytes(Path.Combine(目录,名+".png"),t.EncodeToPNG()); UnityEngine.Object.Destroy(t);
    }
    static IEnumerator 运行()
    {
        var 输入=游戏.GetComponentInChildren<InputSystemUIInputModule>(); if(输入!=null) 输入.enabled=false;
        检查("隔离存档继续进入实际主页",游戏.继续游戏()); yield return new WaitForSecondsRealtime(.25f);
        点击("道纹"); yield return new WaitForSecondsRealtime(.4f);
        var 页=游戏.界面.道纹页; 检查("真实入口进入道纹构筑",页!=null && 游戏.阶段==游戏阶段.道纹);
        var 图=游戏.道纹数据;
        页.聚焦解锁区域(); Canvas.ForceUpdateCanvases();
        foreach(var 名 in new[]{"道纹背景","画布主面板","实时构筑预览","画布视口"})
        {
            var 像=页.GetComponentsInChildren<Image>().First(i=>i.name==名);
            检查("使用新美术而非纯色矩形:"+名,像.sprite!=null && 像.sprite.name!="UI_道纹图集");
        }
        检查("画布确实使用构筑图集",页.画布.mainTexture==游戏.美术.道纹构筑图集);
        检查("纸栏封印页签不可操作",!键("通路3").interactable && !键("通路6").interactable);
        检查("未连接道纹可定位",页.定位未接通() && 页.连接诊断说明.Length>0); 页.聚焦解锁区域();
        检查("藏匣分页保留全部实例",页.筛选结果数==图.道纹.Count && 页.候选总页数==2);
        foreach(var t in 页.GetComponentsInChildren<Text>().Where(t=>t.transform.parent.name=="实时构筑预览"))
            检查("纸栏文字未截断:"+t.text.Split('\n')[0],t.preferredHeight<=t.rectTransform.rect.height+2);
        yield return 截("01-构筑总览");
        点击("通路2"); 检查("页签切换实战参数",页.当前预览通路==5 && 页.当前演示参数.连锁>0);
        yield return 截("02-独立通路"); 点击("通路1");
        string 悬停前 = JsonUtility.ToJson(图.导出存档());
        页.指针移动(页.格屏幕位置(new Vector2Int(4,0))); yield return null;
        检查("悬停整链从源纹到目标",页.画布.高亮路径.Count==5 && 页.画布.高亮路径.First()==Vector2Int.zero && 页.画布.高亮路径.Last()==new Vector2Int(4,0));
        检查("悬停不改变实际道纹",悬停前==JsonUtility.ToJson(图.导出存档()));
        yield return 截("02a-悬停整链高亮"); 页.指针离开(); yield return null;
        检查("离开节点清除悬停链路",页.画布.高亮路径.Count==0 && !页.浮窗显示);
        var 源位置=页.格屏幕位置(Vector2Int.zero); 页.指针移动(源位置); yield return null;
        检查("源纹详情含等级品阶与解封",页.浮窗显示 && 页.GetComponentsInChildren<天帝道纹详情卡>().Single().当前道纹.是源纹);
        yield return 截("03-源纹详情"); 页.指针离开();
        点击("筛选 / 排序"); 检查("筛选弹层可显示",页.筛选已打开); yield return 截("04-接口筛选"); 页.关闭筛选();
        页.设置接口筛选(8); 检查("筛选实际接口",Enumerable.Range(0,页.筛选结果数).All(i=>页.候选显示项(i).有接口(3))); 页.设置接口筛选(0);
        页.切换候选页(1); yield return null;
        var 纹=图.道纹[8]; var 卡=页.GetComponentsInChildren<天帝道纹输入>().First(i=>i.道纹==纹 && !i.是画布);
        var e=new PointerEventData(EventSystem.current) {position=页.候选屏幕位置(8),button=PointerEventData.InputButton.Left};
        ExecuteEvents.Execute(卡.gameObject,e,ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(卡.gameObject,e,ExecuteEvents.beginDragHandler);
        int 原口=纹.接口; 检查("拖动旋转不改实际道纹",页.旋转拖动道纹() && 纹.接口==原口); 页.旋转拖动道纹();页.旋转拖动道纹();页.旋转拖动道纹();页.旋转拖动道纹();页.旋转拖动道纹();
        e.position=页.格屏幕位置(new Vector2Int(6,0)); ExecuteEvents.Execute(卡.gameObject,e,ExecuteEvents.dragHandler); yield return null;
        检查("拖到锁定格红色反馈",页.放置反馈==false); yield return 截("05a-不可放置");
        e.position=页.格屏幕位置(new Vector2Int(5,0)); ExecuteEvents.Execute(卡.gameObject,e,ExecuteEvents.dragHandler); yield return null;
        检查("拖到已解锁格绿色反馈",页.放置反馈==true);
        检查("实际出现数值变化提示素材",页.GetComponentsInChildren<Image>().First(i=>i.name=="数值变化提示").enabled);
        yield return 截("05-拖放与数值变化");
        ExecuteEvents.Execute(卡.gameObject,e,ExecuteEvents.endDragHandler); 检查("实际放置与单步撤销",纹.格子==new Vector2Int(5,0) && 页.撤销上一步() && !纹.格子.HasValue);
        页.切换候选页(0); 页.聚焦解锁区域();
        点击("布局方案"); 检查("布局三槽保留",页.GetComponentsInChildren<InputField>().Length==3); yield return 截("06-布局方案"); 页.关闭筛选();
        点击("加成来源"); 检查("来源支持新皮肤滚动条",页.GetComponentsInChildren<Scrollbar>().Length==1); yield return 截("07-加成来源"); 页.关闭筛选();
        检查("隔离保存可回读",游戏.保存进度() && new 天帝存档().读取()!=null);
        var 原缩放=页.画布.缩放; 页.画布.缩放=.25f; 页.画布.SetVerticesDirty(); Canvas.ForceUpdateCanvases(); 页.画布.Rebuild(CanvasUpdate.PreRender);
        var 网格=页.画布.canvasRenderer.GetMesh(); 检查("大画布总览顶点有界",网格!=null && 网格.vertexCount>0 && 网格.vertexCount<65000);
        页.画布.缩放=原缩放; 页.画布.SetVerticesDirty();
        var 空物=new GameObject("隔离空藏匣",typeof(RectTransform),typeof(天帝道纹界面));
        空物.transform.SetParent(页.transform.parent,false); var 空区=(RectTransform)空物.transform;
        空区.anchorMin=Vector2.zero; 空区.anchorMax=Vector2.one; 空区.offsetMin=空区.offsetMax=Vector2.zero;
        var 空页=空物.GetComponent<天帝道纹界面>(); 空页.初始化(new 天帝道纹(7,天帝天赋.获取((int)天赋种类.普通人)),游戏.默认字体,()=>{});
        检查("真实空藏匣引导与分页禁用",空页.筛选结果数==0 && 空页.GetComponentsInChildren<Text>().Any(t=>t.text.StartsWith("藏匣尚空")) && 空页.GetComponentsInChildren<Button>().Where(b=>b.name=="上一页" || b.name=="下一页").All(b=>!b.interactable));
        yield return 截("08-开局空藏匣"); var 大存=空页.数据.导出存档(); UnityEngine.Object.Destroy(空物); yield return null;
        大存.解锁格.Clear();
        for(int q=天帝道纹.最小坐标;q<=天帝道纹.最大坐标;q++) for(int r=天帝道纹.最小坐标;r<=天帝道纹.最大坐标;r++) 大存.解锁格.Add(new Vector2Int(q,r));
        var 大物=new GameObject("隔离31×31格总览",typeof(RectTransform),typeof(天帝道纹界面)); 大物.transform.SetParent(页.transform.parent,false);
        var 大区=(RectTransform)大物.transform; 大区.anchorMin=Vector2.zero; 大区.anchorMax=Vector2.one; 大区.offsetMin=大区.offsetMax=Vector2.zero;
        var 大页=大物.GetComponent<天帝道纹界面>(); 大页.初始化(天帝道纹.读取存档(大存),游戏.默认字体,()=>{});
        Canvas.ForceUpdateCanvases(); 大页.画布.Rebuild(CanvasUpdate.PreRender);
        var 大网=大页.画布.canvasRenderer.GetMesh(); 检查("31×31格新皮肤聚焦与顶点限制",大页.数据.已解锁格数==961 && 大页.画布.缩放<.45f && 大网!=null && 大网.vertexCount>0 && 大网.vertexCount<65000);
        UnityEngine.Object.Destroy(大物); yield return null;
        var 六词=天帝道纹生成.创建(1,道纹分类.属性,道纹品阶.史诗,new System.Random(4)); 六词.词条.Clear();
        foreach(var 属性 in 天帝道纹属性.普通属性) 六词.词条.Add(new 道纹词条(属性,8));
        var 详情=页.GetComponentsInChildren<天帝道纹详情卡>(true).Single(); 详情.设置(六词); 详情.gameObject.SetActive(true); Canvas.ForceUpdateCanvases();
        检查("六词条高阶详情完整",详情.GetComponentsInChildren<Image>().Count(i=>i.name.StartsWith("词条位-"))==6 && 详情.高度<900);
        foreach(var t in 详情.GetComponentsInChildren<Text>()) 检查("六词详情文字未截断:"+t.name,t.preferredHeight<=t.rectTransform.rect.height+2);
        详情.gameObject.SetActive(false);
        点击("返回主页"); 检查("返回主页成功",游戏.阶段==游戏阶段.主页);
        检查("离开构筑恢复主页背景",游戏.GetComponentsInChildren<Image>().First(i=>i.name=="背景").sprite==游戏.主页背景);
        完成();
    }
    static void 完成()
    {
        if(已结束) return; 已结束=true;
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));
        EditorApplication.isPlaying=false;
    }
    static void 退出(PlayModeStateChange 状态)
    {
        if(状态!=PlayModeStateChange.EnteredEditMode) return;
        Application.logMessageReceived-=错误; EditorApplication.update-=等待; EditorApplication.playModeStateChanged-=退出;
        天帝存档.验证目录=原存档; Application.runInBackground=原后台;
        EditorSettings.enterPlayModeOptionsEnabled=原选项开; EditorSettings.enterPlayModeOptions=原选项;
        if(!已结束) { 结果.错误.Add("验证提前停止"); File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true)); }
    }
}
#endif
