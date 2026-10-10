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
using UnityEngine.UI;
using Process = System.Diagnostics.Process;

// 仅编辑器自动演示。走真实界面、寻路移动和普攻，不注入伤害或虚构掉落。
public static class 天帝试玩录制
{
    [Serializable] public class 片段 { public string 名; public int 开始帧, 结束帧; }
    [Serializable] public class 报告
    {
        public string 方法 = "Unity GameView逐帧录制；自动操作真实UI与移动；后期压缩赶路。";
        public int 帧数, 宽, 高, 种子, 击败数, 连锁数, 分裂数, 拾取数, 通货拾取数, 剩余技能点;
        public int 数量, 连锁, 改造编号;
        public bool 王已击败, 玩家存活, 升阶成功, 加词成功;
        public string 改造前, 改造后;
        public List<string> 通货收获 = new List<string>(), 实际改造 = new List<string>();
        public List<片段> 片段 = new List<片段>();
        public List<string> 错误 = new List<string>();
    }
    static string 目录;
    static 报告 结果;
    static Process 编码;
    static EditorWindow 窗口;
    static bool 原最大, 原启用, 原后台;
    static EnterPlayModeOptions 原模式;
    static int 原帧率, 原尺寸;
    static 天帝游戏 游戏;
    static double 截止;
    static readonly BindingFlags 隐 = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly WaitForEndOfFrame 帧末 = new WaitForEndOfFrame();
    static Text 字幕;
    static Image 指针;
    static bool 已完成;
    static string 原验证目录;
    public static string 启动()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("先停止运行并保存场景，录制不覆盖未保存场景。");
        目录=Path.Combine(天帝构建工具.项目根,"生成/试玩视频/录制-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        // 录制只使用隔离存档，避免演示构筑、战斗拾取和改造污染玩家正式进度。
        原验证目录=天帝存档.验证目录;
        天帝存档.验证目录=Path.Combine(目录,"隔离存档"); Directory.CreateDirectory(天帝存档.验证目录);
        var 正式存档=Path.Combine(Application.persistentDataPath,"天帝进度.json");
        if (File.Exists(正式存档)) File.Copy(正式存档,Path.Combine(天帝存档.验证目录,"天帝进度.json"),true);
        结果=new 报告(); 已完成=false; 游戏=null; 字幕=null; 指针=null; 原帧率=Time.captureFramerate;
        原启用=EditorSettings.enterPlayModeOptionsEnabled; 原模式=EditorSettings.enterPlayModeOptions; 原后台=Application.runInBackground;
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        窗口=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")); 原最大=窗口.maximized; 窗口.maximized=true; 窗口.Focus();
        设置尺寸(); EditorSettings.enterPlayModeOptionsEnabled=true; EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        截止=EditorApplication.timeSinceStartup+900;
        Application.logMessageReceived+=错误; EditorApplication.update+=启动等待; EditorApplication.isPlaying=true;
        return 目录;
    }
    static void 设置尺寸()
    {
        var 程序集=typeof(Editor).Assembly;
        var 类型=程序集.GetType("UnityEditor.GameViewSizes");
        var 单例=typeof(ScriptableSingleton<>).MakeGenericType(类型).GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var 组=类型.GetMethod("GetGroup").Invoke(单例,new[]{Enum.ToObject(程序集.GetType("UnityEditor.GameViewSizeGroupType"),0)});
        var 大小类=程序集.GetType("UnityEditor.GameViewSize");
        var 构造=大小类.GetConstructor(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,new[]{程序集.GetType("UnityEditor.GameViewSizeType"),typeof(int),typeof(int),typeof(string)},null);
        var 大小=构造.Invoke(new[]{Enum.ToObject(程序集.GetType("UnityEditor.GameViewSizeType"),1),(object)1920,1080,"天帝试玩1080p"});
        组.GetType().GetMethod("AddCustomSize").Invoke(组,new[]{大小});
        int 索引=(int)组.GetType().GetMethod("GetBuiltinCount").Invoke(组,null)+(int)组.GetType().GetMethod("GetCustomCount").Invoke(组,null)-1;
        var 属性=窗口.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        原尺寸=(int)属性.GetValue(窗口); 属性.SetValue(窗口,索引);
    }
    static void 启动等待()
    {
        if(EditorApplication.timeSinceStartup>截止) { 结果.错误.Add("录制超过15分钟"); 完成(); return; }
        if(!EditorApplication.isPlaying)return;
        Application.runInBackground=true; EditorApplication.QueuePlayerLoopUpdate();
        if(游戏!=null)return;
        游戏=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if(游戏!=null) 游戏.StartCoroutine(保护(演示()));
    }
    static IEnumerator 保护(IEnumerator 流程)
    {
        // Unity会递归执行嵌套IEnumerator；各子段由顶层手工展平，使异常也能退出并恢复编辑器。
        var 栈=new Stack<IEnumerator>(); 栈.Push(流程);
        while(栈.Count>0)
        {
            object 当前=null; bool 有=false;
            try { 有=栈.Peek().MoveNext(); if(有) 当前=栈.Peek().Current; }
            catch(Exception ex) { 结果.错误.Add(ex.ToString()); break; }
            if(!有){栈.Pop();continue;}
            if(当前 is IEnumerator 子) { 栈.Push(子); continue; }
            yield return 当前;
        }
        完成();
    }
    static void 段(string 名)
    {
        if(结果.片段.Count>0)结果.片段.Last().结束帧=结果.帧数;
        结果.片段.Add(new 片段 { 名=名,开始帧=结果.帧数 });
        if(字幕!=null)字幕.text=名;
        File.WriteAllText(Path.Combine(目录,"progress.json"),JsonUtility.ToJson(结果,true));
    }
    static void 建标注()
    {
        var 根=new GameObject("录制说明",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)); 根.transform.SetParent(游戏.transform,false);
        var canvas=根.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;
        var 缩放=根.GetComponent<CanvasScaler>();缩放.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;缩放.referenceResolution=new Vector2(1920,1080);
        var 条=new GameObject("说明",typeof(RectTransform),typeof(Image));条.transform.SetParent(根.transform,false);
        var rect=(RectTransform)条.transform;rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(0,42);rect.anchoredPosition=Vector2.zero;
        条.GetComponent<Image>().color=new Color(.015f,.025f,.035f,.93f);条.GetComponent<Image>().raycastTarget=false;
        var 文=new GameObject("字幕",typeof(RectTransform),typeof(Text));文.transform.SetParent(条.transform,false);
        var vr=(RectTransform)文.transform;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=new Vector2(24,0);vr.offsetMax=new Vector2(-24,0);
        字幕=文.GetComponent<Text>();字幕.font=游戏.默认字体;字幕.fontSize=23;字幕.color=new Color(.76f,.91f,1);字幕.alignment=TextAnchor.MiddleLeft;字幕.raycastTarget=false;
        var 标签=new GameObject("自动演示标签",typeof(RectTransform),typeof(Text));标签.transform.SetParent(根.transform,false);
        var tr=(RectTransform)标签.transform;tr.anchorMin=tr.anchorMax=new Vector2(1,1);tr.pivot=new Vector2(1,1);tr.anchoredPosition=new Vector2(-24,-6);tr.sizeDelta=new Vector2(520,28);
        var t=标签.GetComponent<Text>();t.font=游戏.默认字体;t.fontSize=17;t.color=new Color(.8f,.86f,.9f,.9f);t.alignment=TextAnchor.MiddleRight;t.text="开发原型 · 演示构筑 · 片段变速";t.raycastTarget=false;
        var 鼠=new GameObject("演示指针",typeof(RectTransform),typeof(Image));鼠.transform.SetParent(根.transform,false);指针=鼠.GetComponent<Image>();指针.color=new Color(.4f,.9f,1,.85f);指针.raycastTarget=false;指针.rectTransform.sizeDelta=new Vector2(12,12);
    }
    static void 指(Vector2 屏) { if(指针!=null) 指针.rectTransform.position=屏; }
    static IEnumerator 录帧(int 数, Action 每帧=null)
    {
        for(int i=0;i<数;i++)
        {
            每帧?.Invoke(); yield return 帧末;
            var 图=ScreenCapture.CaptureScreenshotAsTexture();
            if(编码==null)
            {
                结果.宽=图.width;结果.高=图.height;
                编码=new Process();编码.StartInfo.FileName=Path.Combine(天帝构建工具.项目根,"生成/录制工具/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe");
                编码.StartInfo.Arguments="-hide_banner -loglevel error -y -f image2pipe -framerate 30 -vcodec mjpeg -i - -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \""+Path.Combine(目录,"原始试玩.mp4")+"\"";
                编码.StartInfo.UseShellExecute=false;编码.StartInfo.CreateNoWindow=true;编码.StartInfo.RedirectStandardInput=true;编码.Start();
            }
            byte[] 字节=图.EncodeToJPG(92); 编码.StandardInput.BaseStream.Write(字节,0,字节.Length);
            if(结果.片段.Count>0 && 结果.帧数-结果.片段.Last().开始帧==15)File.WriteAllBytes(Path.Combine(目录,"关键帧-"+结果.片段.Count.ToString("00")+".jpg"),字节);
            UnityEngine.Object.Destroy(图);结果.帧数++;
        }
    }
    static void 点(string 名)
    {
        Canvas.ForceUpdateCanvases();
        var b=游戏.GetComponentsInChildren<Button>().FirstOrDefault(k=>k != null && k.name==名);
        if (b == null)
        {
            // 主页在不同存档/布局模式下使用不同对象名，但按钮文字仍表达同一动作。
            var 文字 = 名 == "开始游戏" ? new[]{"开始游戏", "继续游戏", "开始历练"} : new[]{名};
            b = 游戏.GetComponentsInChildren<Button>().FirstOrDefault(k => k != null && 文字.Contains(k.GetComponentInChildren<Text>()?.text));
        }
        if (b == null) throw new InvalidOperationException("找不到按钮：" + 名);
        if(!b.interactable)throw new InvalidOperationException("按钮未就绪："+名);
        var r=(RectTransform)b.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));指(p);
        ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current){position=p},ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator 演示()
    {
        for(int i=0;i<15;i++)yield return 帧末;
        Time.captureFramerate=30; Application.runInBackground=true;
        var 输入=游戏.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();if(输入!=null)输入.enabled=false;
        建标注();段("01 开局 | 从比赛现场到异界修行");yield return 录帧(75);
        // 录制固定从隔离的新游戏开始，避免正式存档已有道纹数量与页面卡槽不一致。
        if (游戏.阶段 == 游戏阶段.标题)
        {
            游戏.开始序章();
            if (游戏.界面.确认已打开) 游戏.确认开始新游戏();
        }
        yield return 录帧(45);
        if (游戏.阶段 == 游戏阶段.序章)
        {
            游戏.跳过序章();
            段("02 天赋初醒 | 选择续雷，额外一次连锁");
            while(!游戏.界面.源道纹页.可选择)yield return 录帧(1);
            int 刷=0;while(!游戏.天赋池.候选.Any(x=>x.种类==天赋种类.续雷)&&刷++<100)游戏.刷新天赋();
            int 槽=游戏.天赋池.候选.ToList().FindIndex(x=>x.种类==天赋种类.续雷);点("源道纹-"+槽);yield return 录帧(45);点("确认天赋");
        }
        if (游戏.道纹数据.道纹.Count == 0 && 游戏.道纹数据.已解锁格数 == 1)
            天帝道纹夹具.实战包(游戏.道纹数据);
        段("03 主页 | 编辑器演示构筑，通货从战斗拾取");yield return 录帧(45);点("道纹");
        段("04 构筑 | 解锁、拖拽、接口对接，接入普攻");
        var 页=游戏.界面.道纹页;页.画布.平移=new Vector2(-190,-30);页.刷新();
        var 属性=new[]{道纹属性.力量,道纹属性.力量,道纹属性.力量,道纹属性.数量,道纹属性.数量,道纹属性.数量,道纹属性.数量,道纹属性.连锁,道纹属性.连锁,道纹属性.连锁,道纹属性.分裂,道纹属性.范围,道纹属性.弧度,道纹属性.弧度,道纹属性.弧度,道纹属性.速度,道纹属性.速度};
        var 格=new[]{new Vector2Int(1,0),new Vector2Int(2,0),new Vector2Int(3,0),new Vector2Int(4,0),new Vector2Int(5,0),new Vector2Int(5,1),new Vector2Int(4,1),new Vector2Int(3,1),new Vector2Int(2,1),new Vector2Int(1,1),new Vector2Int(0,1),new Vector2Int(0,2),new Vector2Int(1,2),new Vector2Int(2,2),new Vector2Int(3,2),new Vector2Int(4,2),new Vector2Int(5,2),new Vector2Int(6,2)};
        // 现有隔离存档可能没有足够的演示属性，补入临时候选而不改变正式存档。
        var 随机=new System.Random(20261010);
        for (int i=0;i<属性.Length;i++)
        {
            if (游戏.道纹数据.道纹.Any(x=>!x.格子.HasValue&&x.属性==属性[i])) continue;
            var 纹=天帝道纹生成.创建(游戏.道纹数据.道纹.Count+1,道纹分类.属性,道纹品阶.普通,随机,道纹属性分组.形态,1);
            纹.词条.Clear();纹.词条.Add(new 道纹词条(属性[i],1));纹.接口=63;游戏.道纹数据.道纹.Add(纹);
        }
        // 页面刷新会在每次放置时重算；此处避免在候选卡尚未建完时触发界面回调。
        页.刷新();
        for(int i=0;i<18;i++)
        {
            var 纹=游戏.道纹数据.道纹.First(x=>!x.格子.HasValue&&x.属性==(i==17?道纹属性.力量:属性[i]));
            int 序=游戏.道纹数据.道纹.IndexOf(纹);页.切换候选页(序/8);Canvas.ForceUpdateCanvases();
            游戏.道纹数据.解锁格子(格[i]);
            var 终=页.格屏幕位置(格[i]);页.点击(true,new PointerEventData(EventSystem.current){position=终,button=PointerEventData.InputButton.Left});
            var 起=页.候选屏幕位置(序);页.按下(纹,false,new PointerEventData(EventSystem.current){position=起,button=PointerEventData.InputButton.Left});
            页.开始拖动(false,new PointerEventData(EventSystem.current){position=起,button=PointerEventData.InputButton.Left});
            int 步=0;yield return 录帧(10,()=>{var p=Vector2.Lerp(起,终,++步/10f);指(p);页.拖动(new PointerEventData(EventSystem.current){position=p});});
            页.结束拖动(new PointerEventData(EventSystem.current){position=终});
            // 某些存档的源纹接口方向会让演示分支暂时未接通；仍保留真实拖放结果并继续录制流程。
            yield return 录帧(4);
        }
        var 参数=普攻参数.读取(游戏.道纹数据,游戏.主角属性);结果.数量=参数.数量;结果.连锁=参数.连锁;结果.剩余技能点=游戏.道纹数据.技能点;
        页.指针离开();字幕.text="04 构筑完成 | 多颗投石 · 连锁 · 分裂 · 范围 · 弧度追踪";yield return 录帧(60);点("返回主页");
        段("05 进入青岚原 | 普通难度，66个敌人");yield return 录帧(40);
        // 当前主页常驻地图选择，直接调用与“开始游戏”确认框相同的正式入图入口。
        if (!游戏.进入战斗()) throw new InvalidOperationException("无法进入青岚原");
        while(游戏.阶段==游戏阶段.战斗加载)yield return 录帧(1);
        var 场=游戏.战斗场景;场.enabled=false;结果.种子=场.地图.种子;指针.gameObject.SetActive(false);
        段("06 刷图 | 多发电光、分裂与跳链，边走边清怪");
        var 营=场.地图.小队.OrderBy(x=>Vector2.Distance(场.玩家位置,场.地图.格中心(x.中心格.x,x.中心格.y))).Take(3).ToArray();
        foreach(var 队 in 营)
        {
            var 目标=场.地图.格中心(队.中心格.x,队.中心格.y);
            yield return 走到(目标);
            yield return 录帧(100,()=>战斗帧(Vector2.zero));
            var 落=场.战斗.掉落.地面.FirstOrDefault(x=>!x.已拾取&&Vector2.Distance(x.位置,场.玩家位置)<10);
            if(落!=null)yield return 走到(落.位置);
        }
        段("07 前往王房 | 保留寻路与碰撞，成片压缩赶路");
        var 王=场.战斗.敌人.Single(x=>x.布点.级别==战斗敌人级别.王级);
        yield return 走到(场.地图.格中心(场.地图.王房入口格.x,场.地图.王房入口格.y));
        段("08 BOSS | 青鬃狼王，真实普攻与走位");
        yield return 走到(场.地图.王位置+Vector2.down*6);
        for(int i=0;i<600&&王.存活;i++)
        {
            Vector2 差=场.玩家位置-王.位置;Vector2 向=差.magnitude<4?差.normalized:Vector2.zero;
            yield return 录帧(1,()=>战斗帧(向));
        }
        if(王.存活||场.战斗.玩家死亡)throw new InvalidOperationException("BOSS实战失败，不以调试伤害替代。");
        段("09 击败与掉落 | 道纹与通货，逐条显示拾取收获");yield return 录帧(50,()=>战斗帧(Vector2.zero));
        var 王落=场.战斗.掉落.地面.Last();
        if(!王落.已拾取)yield return 走到(王落.位置);
        var 王货=场.战斗.通货掉落.地面.Last();
        if(!王货.已拾取)yield return 走到(王货.位置);
        foreach(var 物 in 场.战斗.通货掉落.地面.Where(x=>!x.已拾取&&Vector2.Distance(x.位置,场.玩家位置)<12).ToArray())yield return 走到(物.位置);
        yield return 录帧(100,()=>战斗帧(Vector2.zero));
        if(!王落.已拾取||!王货.已拾取)throw new InvalidOperationException("王掉落未拾取");
        结果.王已击败=场.王已击败;结果.玩家存活=!场.战斗.玩家死亡;结果.击败数=场.战斗.击败数;结果.连锁数=场.战斗.连锁发生数;结果.分裂数=场.战斗.分裂生成数;结果.拾取数=场.战斗.掉落.拾取数;
        结果.通货拾取数=场.战斗.通货掉落.拾取总量;
        foreach(通货种类 k in Enum.GetValues(typeof(通货种类)))if(游戏.通货数据.数量(k)>0)结果.通货收获.Add(k+" ×"+游戏.通货数据.数量(k));
        var 可改=场.战斗.掉落.地面.Where(x=>x.已拾取).Select(x=>x.道纹).ToArray();
        指针.gameObject.SetActive(true);点("离开");while(游戏.阶段==游戏阶段.战斗加载)yield return 录帧(1);
        段("10 回到主页 | 本次拾取的道纹保留");yield return 录帧(50);点("道纹改造");
        段("11 改造掉落 | 消耗本次拾取的通货，强化道纹");
        var 改页=游戏.GetComponentInChildren<天帝通货界面>();
        for(int n=0;n<3;n++)
        {
            道纹实例 改=null; 通货种类 货=通货种类.启灵石;
            foreach(var k in 天帝通货.可用种类)
            {
                改=可改.LastOrDefault(x=>游戏.通货数据.可使用(k,x,0,out var 原因));
                if(改!=null){货=k;break;}
            }
            if(改==null)break;
            if(n==0){结果.改造编号=改.编号;结果.改造前=改.详情文字();}
            改页.选目标(游戏.道纹数据.道纹.IndexOf(改));点("通货-"+货);yield return 录帧(30);
            int 旧数=游戏.通货数据.数量(货),旧口=改.接口,旧词=改.词条.Count;var 旧阶=改.品阶;
            点("使用通货");
            if(游戏.通货数据.数量(货)!=旧数-1)throw new InvalidOperationException("真实通货改造未消费");
            结果.升阶成功|=改.品阶>旧阶;结果.加词成功|=改.词条.Count>旧词;
            结果.实际改造.Add(货+" "+旧数+"→"+(旧数-1)+"，#"+改.编号+"："+改.详情文字());结果.改造后=改.详情文字();yield return 录帧(45);
        }
        if(结果.实际改造.Count==0)throw new InvalidOperationException("本次没有可用于掉落道纹的通货");
        结果.改造后=可改.First(x=>x.编号==结果.改造编号).详情文字();
        yield return 录帧(30);点("关闭通货");段("12 完成 | 配置道纹 → 刷图 → BOSS → 掉落 → 改造");yield return 录帧(45);
    }
    static void 战斗帧(Vector2 方向)
    {
        var 场=游戏.战斗场景;场.移动一步(方向,true,1f/30);场.战斗一步(1f/30);
        typeof(天帝战斗场景).GetMethod("更新相机",隐).Invoke(场,null);
        if(场.战斗.玩家死亡)throw new InvalidOperationException("自动试玩角色死亡");
    }
    static IEnumerator 走到(Vector2 终)
    {
        var 场=游戏.战斗场景;var 路=new List<Vector2>();
        if(!new 天帝战斗寻路(场.地图).路径(场.玩家位置,终,路,false,false))throw new InvalidOperationException("没有可行路线");
        int 总=0;
        foreach(var 点 in 路)
            while(Vector2.Distance(场.玩家位置,点)>.35f)
            {
                if(++总>3000)throw new InvalidOperationException("移动卡住");
                yield return 录帧(1,()=>战斗帧((点-场.玩家位置).normalized));
            }
    }
    static void 错误(string 文,string 栈,LogType 类) { if(类==LogType.Error||类==LogType.Exception)结果.错误.Add(文+"\n"+栈); }
    static void 完成()
    {
        if(已完成)return;已完成=true;
        if(结果.片段.Count>0)结果.片段.Last().结束帧=结果.帧数;
        if(编码!=null){编码.StandardInput.Close();if(!编码.WaitForExit(10000)){编码.Kill();结果.错误.Add("编码未正常结束");}else if(编码.ExitCode!=0)结果.错误.Add("编码退出码"+编码.ExitCode);编码.Dispose();编码=null;}
        File.WriteAllText(Path.Combine(目录,"录制报告.json"),JsonUtility.ToJson(结果,true));
        EditorApplication.update-=启动等待;Application.logMessageReceived-=错误;Time.captureFramerate=原帧率;EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>{EditorSettings.enterPlayModeOptionsEnabled=原启用;EditorSettings.enterPlayModeOptions=原模式;Application.runInBackground=原后台;天帝存档.验证目录=原验证目录;if(窗口!=null){窗口.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(窗口,原尺寸);窗口.maximized=原最大;}游戏=null;};
    }
}
#endif
