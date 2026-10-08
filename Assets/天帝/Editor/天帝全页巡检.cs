#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 只在隔离存档巡视现有UI，不向玩家发放资源，不保存场景。
public static class 天帝全页巡检
{
    [Serializable] public sealed class 页面记录
    { public string 名称; public int 文字数, 图片数, 素材数; public List<string> 溢出=new List<string>(), 素材=new List<string>(); }
    [Serializable] public sealed class 巡检报告
    { public List<页面记录> 页面=new List<页面记录>(); public List<string> 通过=new List<string>(),失败=new List<string>(),错误=new List<string>(); public string 原存档,结束存档; public bool 完整巡检; }
    static 巡检报告 报告;
    static string 目录,原目录,真实路径;
    static bool 原后台,原选项开,开始,结束,原字幕存在,原序章存在;
    static int 原字幕,原序章;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static bool 仅回收, 仅层级补拍;
    static EditorWindow 补拍窗口;
    static int 补拍原尺寸;
    static Rect? 补拍原安全区;
    static 天帝游戏 游戏;
    static string 指纹(string p){if(!File.Exists(p))return "无文件";using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(File.ReadAllBytes(p)));}
    public static string 启动()=>启动(false);
    public static string 启动回收()=>启动(true);
    public static string 启动层级补拍()=>启动(false, true);
    static string 启动(bool 回收, bool 层级补拍 = false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("需编辑模式且场景无未保存修改。");
        if(SceneManager.GetActiveScene().path!="Assets/天帝/场景/天帝.unity")
        {
            if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("当前场景有未保存修改，请先保存。");
            // 命令行或新开的编辑器通常没有加载主场景；干净场景可直接切换，避免巡检入口误报中断。
            EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        }
        天帝数值同步检查.校验();
        仅回收=回收;
        仅层级补拍=层级补拍;
        目录=Path.Combine(天帝构建工具.项目根,"生成/验证/全页巡检-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(目录);
        真实路径=Path.Combine(Application.persistentDataPath,"天帝进度.json");报告=new 巡检报告{原存档=指纹(真实路径)};
        string 隔离=Path.Combine(目录,"隔离存档");Directory.CreateDirectory(隔离);
        if(File.Exists(真实路径))File.Copy(真实路径,Path.Combine(隔离,"天帝进度.json"));
        else
        {
            var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
            new 天帝存档(隔离).保存(new 天帝存档数据{序章已完成=true,主角=天帝普攻.主角配置(),画布=网.导出存档(),通货=new 天帝通货(网,42).导出库存(),灵石=天帝宝盒.开局灵石});
        }
        原目录=天帝存档.验证目录;天帝存档.验证目录=隔离;
        原选项开=EditorSettings.enterPlayModeOptionsEnabled;原选项=EditorSettings.enterPlayModeOptions;原后台=Application.runInBackground;
        原字幕存在=PlayerPrefs.HasKey("Tiandi.Menu.Subtitles");原字幕=PlayerPrefs.GetInt("Tiandi.Menu.Subtitles",1);
        原序章存在=PlayerPrefs.HasKey("Tiandi.Menu.PrologueSeen");原序章=PlayerPrefs.GetInt("Tiandi.Menu.PrologueSeen",0);
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        设置补拍视图();
        开始=结束=false;截止=EditorApplication.timeSinceStartup+140;游戏=null;
        EditorApplication.update+=等待;EditorApplication.playModeStateChanged+=清理;Application.logMessageReceived+=记错;EditorApplication.isPlaying=true;return 目录;
    }
    static void 设置补拍视图()
    {
        var a=typeof(Editor).Assembly;var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        补拍窗口=EditorWindow.GetWindow(a.GetType("UnityEditor.GameView"));
        补拍原尺寸=(int)补拍窗口.GetType().GetProperty("selectedSizeIndex",flags).GetValue(补拍窗口);
        var t=a.GetType("UnityEditor.GameViewSizes");
        var 单=typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var 组=t.GetMethod("GetGroup").Invoke(单,new[]{Enum.ToObject(a.GetType("UnityEditor.GameViewSizeGroupType"),0)});
        var 大小=a.GetType("UnityEditor.GameViewSize").GetConstructor(flags,null,new[]{a.GetType("UnityEditor.GameViewSizeType"),typeof(int),typeof(int),typeof(string)},null)
            .Invoke(new[]{Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"),1),(object)1920,1080,"信息层级审查1080p"});
        组.GetType().GetMethod("AddCustomSize").Invoke(组,new[]{大小});
        int 索引=(int)组.GetType().GetMethod("GetBuiltinCount").Invoke(组,null)+(int)组.GetType().GetMethod("GetCustomCount").Invoke(组,null)-1;
        补拍窗口.GetType().GetProperty("selectedSizeIndex",flags).SetValue(补拍窗口,索引);补拍窗口.Focus();
        补拍原安全区=天帝移动适配.验证安全区;天帝移动适配.验证安全区=new Rect(0,0,1920,1080);
    }
    static void 等待()
    {
        if(结束)return;if(EditorApplication.timeSinceStartup>截止){报告.错误.Add("巡检超时");完成();return;}
        if(!EditorApplication.isPlaying)return;Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();
        if(开始)return;游戏=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();if(游戏==null||游戏.阶段!=游戏阶段.标题)return;
        开始=true;var 输入=游戏.GetComponentInChildren<InputSystemUIInputModule>();if(输入!=null)输入.enabled=false;游戏.StartCoroutine(保护(巡视()));
    }
    static IEnumerator 保护(IEnumerator 流)
    {
        var 栈=new Stack<IEnumerator>();栈.Push(流);
        while(栈.Count>0)
        {bool 有;object x=null;try{有=栈.Peek().MoveNext();if(有)x=栈.Peek().Current;}catch(Exception ex){报告.错误.Add(ex.ToString());break;}
         if(!有){栈.Pop();continue;}if(x is IEnumerator 子){栈.Push(子);continue;}yield return x;}完成();
    }
    static IEnumerator 拍(string 名)
    {
        yield return null;游戏.界面.更新适配();Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
        var 页=new 页面记录{名称=名};var 图=游戏.GetComponentsInChildren<Image>();
        页.图片数=图.Length;页.素材数=图.Count(x=>x.sprite!=null);页.素材=图.Where(x=>x.sprite!=null).Select(x=>x.name+"="+x.sprite.name).Distinct().ToList();
        var 弹层=游戏.GetComponentsInChildren<RectTransform>().FirstOrDefault(x=>x.name=="设置层");
        var 顶层=游戏.界面.回收页!=null&&游戏.界面.回收页.确认已打开?游戏.界面.回收页.transform.Find("回收确认层"):游戏.界面.宝盒概率已打开?弹层.Find("宝盒概率层"):弹层;
        var 模态=游戏.GetComponentsInChildren<RectTransform>().LastOrDefault(x=>x.name=="源道纹详情遮罩"||x.name=="道纹背包窗口"||x.name=="布局方案面板");
        var 文字=模态!=null?模态.GetComponentsInChildren<Text>():弹层!=null&&Enumerable.Range(0,弹层.childCount).Any(i=>弹层.GetChild(i).gameObject.activeSelf)?顶层.GetComponentsInChildren<Text>():游戏.GetComponentsInChildren<Text>();
        foreach(var 文 in 文字.Where(x=>x.enabled&&!string.IsNullOrEmpty(x.text)))
        {
            if(文.GetComponentsInParent<CanvasGroup>().Any(x=>x.alpha<.1f))continue;
            if(!在视口(文.rectTransform))continue;
            // Overflow 用于地图等级胶囊等单行标签；仅对允许换行的文字检查裁切。
            页.文字数++;if(文.verticalOverflow==VerticalWrapMode.Truncate&&!文.resizeTextForBestFit&&文.preferredHeight>文.rectTransform.rect.height+2)
                页.溢出.Add(文.transform.parent.name+"/"+文.name+"："+文.text+" ["+文.preferredHeight+"/"+文.rectTransform.rect.height+"]");
        }
        报告.页面.Add(页);var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(目录,名+".jpg"),t.EncodeToJPG(86));UnityEngine.Object.Destroy(t);
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(报告,true));
    }
    static bool 在视口(RectTransform 区)
    {
        var 点=new Vector3[4];区.GetWorldCorners(点);var 界=new Rect(点[0],点[2]-点[0]);
        foreach(var 遮 in 区.GetComponentsInParent<RectMask2D>()){((RectTransform)遮.transform).GetWorldCorners(点);if(!界.Overlaps(new Rect(点[0],点[2]-点[0])))return false;}return true;
    }
    static void 检查(string 名,bool 对)=>(对?报告.通过:报告.失败).Add(名);
    static void 检查图鉴浮窗()
    {
        var 页=游戏.界面.图鉴页;var 根=(RectTransform)页.transform;
        var 框=根.Find("道纹悬停详情") as RectTransform;var 角=new Vector3[4];
        foreach(var 点 in new[]{new Vector2(根.rect.xMin,根.rect.yMin),new Vector2(根.rect.xMin,根.rect.yMax),new Vector2(根.rect.xMax,根.rect.yMin),new Vector2(根.rect.xMax,根.rect.yMax)})
        {
            页.显示详情(道纹属性.力量,false,RectTransformUtility.WorldToScreenPoint(null,根.TransformPoint(点)));
            Canvas.ForceUpdateCanvases();框.GetWorldCorners(角);
            检查("图鉴浮窗四角完整-"+点,角.All(x=>{var p=根.InverseTransformPoint(x);return p.x>=根.rect.xMin+11.9f&&p.x<=根.rect.xMax-11.9f&&p.y>=根.rect.yMin+11.9f&&p.y<=根.rect.yMax-11.9f;}));
            var 文=框.GetComponentInChildren<Text>();检查("图鉴浮窗正文完整-"+点,文.preferredHeight<=文.rectTransform.rect.height+.1f);
        }
        页.隐藏详情();
    }
    static void 点(string 名)
    {
        var b=游戏.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.name==名&&x.interactable);
        if(b==null)throw new InvalidOperationException("巡检按钮不存在："+名);
        Canvas.ForceUpdateCanvases();var r=(RectTransform)b.transform;var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center))};
        var 命中=new List<RaycastResult>();EventSystem.current.RaycastAll(e,命中);bool 对=命中.Count>0&&(命中[0].gameObject==b.gameObject||命中[0].gameObject.transform.IsChildOf(b.transform));检查("按钮射线-"+名,对);
        if(!对)File.AppendAllText(Path.Combine(目录,"raycast.txt"),名+" => "+string.Join(",",命中.Select(x=>x.gameObject.name))+"\n");
        ExecuteEvents.Execute(b.gameObject,e,ExecuteEvents.pointerClickHandler);
    }
    static void 私调(object o,string 名)=>o.GetType().GetMethod(名,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(o,null);
    static void 勾(string 名)
    {
        var t=游戏.GetComponentsInChildren<Toggle>().FirstOrDefault(x=>x.name==名&&x.interactable);
        if(t==null)throw new InvalidOperationException("巡检复选框不存在："+名);
        Canvas.ForceUpdateCanvases();var r=(RectTransform)t.transform;
        var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center))};
        var 命中=new List<RaycastResult>();EventSystem.current.RaycastAll(e,命中);
        bool 对=命中.Count>0&&(命中[0].gameObject==t.gameObject||命中[0].gameObject.transform.IsChildOf(t.transform));检查("复选框射线-"+名,对);
        ExecuteEvents.Execute(t.gameObject,e,ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator 巡视()
    {
        if(仅层级补拍)
        {
            yield return 层级补拍();报告.完整巡检=true;yield break;
        }
        if(仅回收)
        {
            游戏.继续游戏();yield return null;游戏.界面.显示回收();yield return 拍("01当前库存回收");游戏.界面.关闭回收();
            yield return 回收边界界面();报告.完整巡检=true;yield break;
        }
        yield return 拍("01标题");游戏.开始序章();yield return 拍("02新游戏确认");游戏.界面.关闭确认();
        游戏.界面.显示设置();yield return 拍("03标题设置");游戏.界面.关闭设置();游戏.继续游戏();yield return 拍("04主页");
        游戏.界面.显示角色();yield return 拍("05角色属性");点("页签-战斗进阶");yield return 拍("05b战斗进阶");点("页签-技能与形态");yield return 拍("06技能形态");点("页签-道纹分布");yield return 拍("07道纹分布");游戏.界面.关闭角色();
        游戏.界面.显示图鉴();yield return 拍("08道纹图鉴");游戏.界面.图鉴页.定位分类("基础属性");游戏.界面.图鉴页.显示详情(道纹属性.力量,false,new Vector2(Screen.width*.5f,Screen.height*.6f));yield return 拍("08b图鉴词条详情");游戏.界面.图鉴页.隐藏详情();游戏.界面.图鉴页.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;yield return 拍("09图鉴机制");游戏.界面.关闭图鉴();
        游戏.界面.显示宝盒();yield return 拍("10宝盒");for(int i=0;i<3;i++){点("宝盒概率-"+i);yield return 拍("10b宝盒概率-"+i);点("返回宝盒");}
        var 抽=游戏.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.name=="抽取"&&x.interactable);if(抽!=null)
        {点("抽取");yield return 拍("10a抽取日志");var 日志=游戏.GetComponentInChildren<天帝宝盒日志悬停>();日志.OnPointerEnter(new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.45f,Screen.height*.25f)});yield return 拍("10a2宝盒日志详情");日志.OnPointerExit(new PointerEventData(EventSystem.current));}
        游戏.界面.关闭宝盒();
        点("道纹回收");yield return 拍("10c道纹回收");
        检查("回收阻止其他页面与入图",!游戏.进入战斗()&&!游戏.选择地图等级(游戏.当前地图等级==1?2:1));游戏.界面.显示角色();游戏.界面.显示设置();检查("回收互斥",!游戏.界面.角色已打开&&!游戏.界面.设置已打开);
        var 回收=游戏.界面.回收页;
        int 槽=Enumerable.Range(0,9).FirstOrDefault(i=>游戏.道纹数据.可回收(回收.显示项(i),out _));var 卖纹=回收.显示项(槽);
        if(游戏.道纹数据.可回收(卖纹,out _))
        {
            int 钱=游戏.宝盒数据.灵石,价=天帝数值.道纹回收价(卖纹),数=游戏.道纹数据.道纹.Count;
            bool 无限=游戏.宝盒数据.无限灵石;int 预期余额=无限?钱:钱+价;
            点("回收道纹-"+槽);勾("回收复选框-"+槽);点("预览回收");yield return 拍("10d回收确认");点("取消回收");检查("取消不扣物品不变余额",游戏.宝盒数据.灵石==钱&&游戏.道纹数据.道纹.Count==数&&回收.选中数量==1);
            点("预览回收");yield return 拍("10d2回收确认重开");点("确认回收");检查("确认只出售勾选的一枚",游戏.宝盒数据.灵石==预期余额&&游戏.宝盒数据.无限灵石==无限&&游戏.道纹数据.道纹.Count==数-1&&!游戏.道纹数据.道纹.Contains(卖纹));
            yield return new WaitForSecondsRealtime(1.2f);var 存=new 天帝存档().读取();检查("自动保存回收余额与删除",存!=null&&存.灵石==预期余额&&存.无限灵石==无限&&存.画布.道纹.Count==数-1);
            yield return 拍("10e回收结果");
        }
        var 翻=回收.GetComponentsInChildren<Button>().First(x=>x.name=="回收下一页");if(翻.interactable){点("回收下一页");yield return 拍("10f回收翻页与保护");}回收.GetComponentsInChildren<Dropdown>().Single(x=>x.name=="回收品阶筛选").value=(int)道纹品阶.稀有+1;yield return 拍("10g回收品阶筛选");点("关闭回收");
        游戏.界面.显示设置();yield return 拍("11主页设置");游戏.界面.关闭设置();
        点("作弊码");yield return 拍("11b作弊码输入");点("关闭");
        var 等级菜单=游戏.GetComponentInChildren<Dropdown>();ExecuteEvents.Execute(等级菜单.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);yield return new WaitForSecondsRealtime(.22f);yield return 拍("12地图等级");游戏.界面.关闭等级下拉();yield return new WaitForSecondsRealtime(.22f);
        游戏.界面.请求进入地图();yield return 拍("13入图确认");游戏.界面.关闭确认();
        游戏.打开道纹();yield return 拍("14道纹画布");
        游戏.界面.道纹页.打开筛选();yield return 拍("15画布筛选");游戏.界面.道纹页.关闭筛选();
        游戏.界面.道纹页.打开布局方案();yield return 拍("16布局方案");游戏.界面.道纹页.关闭筛选();
        私调(游戏.界面.道纹页,"显示加成来源");yield return 拍("17加成来源");游戏.界面.道纹页.关闭筛选();
        if(游戏.道纹数据.道纹.Count>0){游戏.界面.道纹页.指针移动(游戏.界面.道纹页.候选屏幕位置(0));yield return 拍("18道纹详情");检查("普通道纹详情实际可见",游戏.界面.道纹页.浮窗显示);}
        游戏.界面.道纹页.指针移动(游戏.界面.道纹页.格屏幕位置(Vector2Int.zero));yield return 拍("18b源纹解封详情");检查("源纹解封详情实际可见",游戏.界面.道纹页.浮窗显示);
        游戏.返回主页();游戏.打开道纹改造();yield return 拍("19道纹改造");游戏.界面.改造页.打开背包();yield return 拍("20改造背包");
        var 包=游戏.GetComponentInChildren<天帝道纹背包>();if(包.显示项(0)!=null){包.显示详情(0,new Vector2(Screen.width*.5f,Screen.height*.5f));yield return 拍("21背包详情");}
        包.隐藏详情();
        // 桌面端接口筛选常驻显示，不创建移动端的折叠按钮；两端都直接验证筛选结果。
        var 展开接口 = 游戏.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == "展开接口筛选" && x.interactable);
        if (展开接口 != null) 点("展开接口筛选"); else 包.设置接口筛选(1, true);
        yield return 拍("21b高级接口筛选");
        包.设置接口筛选(0, false);
        var d=包.GetComponentsInChildren<Dropdown>().First(x=>x.name=="背包品阶筛选");d.Show();yield return new WaitForSecondsRealtime(.22f);yield return 拍("21c背包品阶菜单");d.Hide();
        游戏.界面.改造页.关闭背包();游戏.返回主页();游戏.界面.显示战斗加载("正在前往青岚原……");yield return 拍("21d加载界面");游戏.界面.显示主页();
        游戏.进入战斗();while(游戏.阶段==游戏阶段.战斗加载)yield return null;
        if(游戏.战斗场景!=null){游戏.战斗场景.enabled=false;yield return 拍("22战斗HUD");游戏.界面.切换战斗暂停();yield return 拍("23战斗暂停");游戏.主角属性.设置当前资源(0,0,0);游戏.界面.显示战斗失败();yield return 拍("24失败提示");游戏.返回主页();while(游戏.阶段!=游戏阶段.主页)yield return null;yield return null;}
        游戏.返回标题();游戏.重看序章();var 序=游戏.GetComponent<天帝序章>();序.设置焦点(true);序.设置进度(.37f);序.切换暂停();yield return 拍("25漫画序章");游戏.跳过序章();
        游戏.确认开始新游戏();序.设置焦点(true);游戏.跳过序章();yield return new WaitForSecondsRealtime(2.6f);yield return 拍("26天赋选择");
        游戏.界面.源道纹页.选中(0);yield return 拍("27天赋详情");
        游戏.返回标题();游戏.确认开始新游戏();序.设置焦点(true);序.设置进度(1);yield return new WaitForSecondsRealtime(2.6f);yield return 拍("28漫画衔接天赋");
        游戏.界面.显示源道纹选择(false);yield return new WaitForSecondsRealtime(2.6f);yield return 拍("29独立天赋页");游戏.界面.源道纹页.选中(0);yield return 拍("30独立天赋详情");
        游戏.界面.源道纹页.关闭选中详情();游戏.返回标题();游戏.继续游戏();yield return 回收边界界面();报告.完整巡检=true;
    }
    // 补足视觉审查所需的动态状态；构筑夹具使用独立对象，不写入玩家库存。
    static IEnumerator 层级补拍()
    {
        游戏.继续游戏();游戏.界面.显示角色();
        游戏.界面.角色页.GetComponentsInChildren<Button>().First(x=>x.name=="页签-道纹分布").onClick.Invoke();yield return 拍("S00道纹分布收尾");
        游戏.界面.角色页.GetComponentsInChildren<Button>().First(x=>x.name=="页签-角色属性").onClick.Invoke();
        游戏.界面.角色页.显示属性说明("力量",new Vector2(Screen.width*.55f,Screen.height*.55f));
        yield return 拍("S01属性短说明");
        游戏.界面.角色页.打开详细属性说明("力量",new Vector2(Screen.width*.55f,Screen.height*.55f));
        yield return 拍("S02属性公式");
        游戏.界面.角色页.打开详细属性说明("速度",new Vector2(Screen.width*.8f,Screen.height*.25f));
        yield return 拍("S03长公式边缘");游戏.界面.关闭角色();
        游戏.界面.显示图鉴();游戏.界面.图鉴页.定位分类("普通属性");yield return 拍("S04图鉴普通属性");
        游戏.界面.图鉴页.定位分类("攻击形态");yield return 拍("S05图鉴攻击形态");检查图鉴浮窗();游戏.界面.关闭图鉴();
        游戏.界面.显示战斗错误("青岚原加载失败，请返回主页后重试。");yield return 拍("S06错误提示");游戏.界面.关闭确认();

        var w=new 天帝道纹(420,天帝天赋.获取((int)天赋种类.普通人));w.设置玩家等级(100);
        var 随机=new System.Random(421);
        for(int i=0;i<4;i++)
        {
            var 纹=天帝道纹生成.创建(1,道纹分类.属性,道纹品阶.稀有,随机,道纹属性分组.基础,50);
            纹.接口=9;w.获得道纹(纹);
            var 格=new Vector2Int(i==3?2:i+1,i==3?2:0);w.解锁格子(格);w.放置(纹,格);
        }
        游戏.界面.显示道纹(w);yield return 拍("S07构筑连接夹具");
        var 页=游戏.界面.道纹页;页.指针移动(页.格屏幕位置(new Vector2Int(3,0)));yield return 拍("S08悬停有效链路");
        页.指针离开();
        var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=页.格屏幕位置(new Vector2Int(3,0))};
        页.按下(null,true,e);页.开始拖动(true,e);e.position=页.格屏幕位置(Vector2Int.zero);页.拖动(e);
        yield return 拍("S09拖放不可用反馈");页.取消拖动();
        私调(页,"显示加成来源");yield return 拍("S10有内容加成来源");页.关闭筛选();
        for(int i=0;i<天帝道纹.方案槽数;i++)w.保存布局方案(i,"构筑审查"+(i+1));
        页.打开布局方案();yield return 拍("S11已保存布局方案");
        页.GetComponentsInChildren<Button>().First(x=>x.name=="检查 / 载入"&&x.interactable).onClick.Invoke();
        yield return 拍("S12方案载入预览");
        string 原方案=JsonUtility.ToJson(w.导出存档());
        var 名称=页.GetComponentsInChildren<InputField>().First();名称.text="重命名构筑";
        页.GetComponentsInChildren<Button>().First(x=>x.name=="保存当前"&&x.interactable).onClick.Invoke();
        yield return 拍("S13方案覆盖提示");检查("覆盖确认保留输入名称",名称.text=="重命名构筑");
        页.GetComponentsInChildren<Button>().First(x=>x.name=="取消方案选择").onClick.Invoke();
        检查("取消覆盖不修改方案和画布",原方案==JsonUtility.ToJson(w.导出存档()));
        名称.text="重命名构筑";
        var 保存=页.GetComponentsInChildren<Button>().First(x=>x.name=="保存当前"&&x.interactable);
        保存.onClick.Invoke();保存.onClick.Invoke();检查("确认覆盖使用新名称",w.布局方案[0].名称=="重命名构筑");页.关闭筛选();
        var 材料=new 天帝通货(w,421,1);游戏.界面.显示道纹改造(w,材料);
        var 改=游戏.界面.改造页;改.选目标(0);改.选通货(通货种类.重铸石);yield return 拍("S14可执行改造");
        改.执行();yield return 拍("S15改造结果与消耗");
        var 结果标题=改.GetComponentsInChildren<Text>().FirstOrDefault(x=>x.text.StartsWith("已完成 · 重铸石"));
        检查("改造结果记录实际材料和一次消耗",结果标题!=null&&结果标题.text.Contains("消耗1个")&&材料.数量(通货种类.重铸石)==0);
        string 上次结果=结果标题==null?"":结果标题.text;改.选通货(通货种类.易纹砂);
        检查("切换下一材料保留上次结果",结果标题!=null&&结果标题.text==上次结果);
        改.打开背包();var 包=改.GetComponentInChildren<天帝道纹背包>();
        var 菜单=包.GetComponentsInChildren<Dropdown>().First(x=>x.name=="背包品阶筛选");菜单.value=菜单.options.Count-1;
        yield return 拍("S16背包筛选空结果");检查("空筛选有清除入口",包.筛选结果数==0&&包.GetComponentsInChildren<Button>().Any(x=>x.name=="清除背包筛选"&&x.interactable));
        包.GetComponentsInChildren<Button>().First(x=>x.name=="清除背包筛选").onClick.Invoke();
        检查("清除筛选恢复全部候选",包.筛选结果数==w.道纹.Count&&包.GetComponentsInChildren<Dropdown>().All(x=>x.value==0));改.关闭背包();
        游戏.返回主页();游戏.进入战斗();while(游戏.阶段==游戏阶段.战斗加载)yield return null;
        if(游戏.战斗场景!=null)
        {
            游戏.战斗场景.enabled=false;
            var 通知=(天帝拾取提示)typeof(天帝界面).GetField("拾取列表",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(游戏.界面);
            通知.加入(w.道纹[0]);通知.加入(通货种类.重铸石,1);通知.加入灵石(25);
            yield return new WaitForSecondsRealtime(.45f);yield return 拍("S17战斗拾取通知夹具");
            游戏.主角属性.设置当前资源(0,0,0);游戏.返回主页();while(游戏.阶段!=游戏阶段.主页)yield return null;
        }
        yield return 移动整改检查(w);
        检查("层级构筑与通知仅为独立夹具",w!=游戏.道纹数据);
    }
    static IEnumerator 移动整改检查(天帝道纹 网)
    {
        var 原移动=天帝移动适配.验证移动平台;var 原密度=天帝移动适配.验证密度;var 原池=游戏.天赋池;
        try
        {
            天帝移动适配.验证移动平台=true;天帝移动适配.验证密度=Screen.height/420f;
            游戏.界面.显示道纹(网);yield return null;游戏.界面.道纹页.打开布局方案();yield return 拍("M01手机方案窗口");
            var 页=游戏.界面.道纹页;
            var 输入=页.GetComponentsInChildren<InputField>().First();输入.text="二十字方案名称验证确认按钮始终完整显示";
            var 保存=页.GetComponentsInChildren<Button>().First(x=>x.name=="保存当前");保存.onClick.Invoke();保存.onClick.Invoke();
            页.GetComponentsInChildren<Button>().First(x=>x.name=="检查 / 载入").onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
            foreach(string 名 in new[]{"保存当前","检查 / 载入","取消方案选择","应用已检查的方案"})
            {
                var 键=页.GetComponentsInChildren<Button>().First(x=>x.name==名);
                检查("手机方案操作保留44行高-"+名,((RectTransform)键.transform).rect.height>=43.9f);
                var 文=键.GetComponentInChildren<Text>();检查("手机方案刷新不压缩标签-"+名,文.rectTransform.rect.width>40&&文.rectTransform.rect.height>=43.9f);
            }
            检查("手机方案槽支持独立滚动",页.GetComponentsInChildren<ScrollRect>().Any(x=>x.name=="方案槽列表视口"&&x.content.rect.height>x.viewport.rect.height));页.关闭筛选();
            游戏.界面.显示道纹改造(网,new 天帝通货(网,421,1));游戏.界面.改造页.打开背包();yield return 拍("M02手机背包摘要");
            var 包=游戏.界面.改造页.GetComponentInChildren<天帝道纹背包>();
            foreach(var 卡 in 包.GetComponentsInChildren<天帝背包悬停>())
                foreach(var 文 in 卡.GetComponentsInChildren<Text>().Where(x=>!string.IsNullOrEmpty(x.text)&&!x.resizeTextForBestFit))
                    检查("手机背包文字完整-"+卡.格序+" "+文.text,文.preferredHeight<=文.rectTransform.rect.height+1);
            游戏.界面.改造页.关闭背包();
            typeof(天帝游戏).GetProperty("天赋池").SetValue(游戏,new 天帝天赋池(421));
            游戏.界面.显示源道纹选择(false);yield return new WaitForSecondsRealtime(2.6f);游戏.界面.源道纹页.选中(0);yield return 拍("M03手机天赋详情");
            var 天=游戏.界面.源道纹页;
            foreach(string 名 in new[]{"返回选择","确认源道纹","关闭源道纹详情"})
            {
                var 键=天.GetComponentsInChildren<Button>().First(x=>x.name==名);检查("手机天赋操作保留44行高-"+名,((RectTransform)键.transform).rect.height>=43.9f);
                var 文=键.GetComponentInChildren<Text>();检查("手机天赋按钮文字完整-"+名,文.preferredHeight<=文.rectTransform.rect.height+1);
            }
            检查("手机天赋长说明支持滚动",天.GetComponentsInChildren<ScrollRect>().Any(x=>x.name=="天赋效果说明视口"&&x.content.rect.height>x.viewport.rect.height));
        }
        finally
        {typeof(天帝游戏).GetProperty("天赋池").SetValue(游戏,原池);天帝移动适配.验证移动平台=原移动;天帝移动适配.验证密度=原密度;游戏.界面.显示主页();游戏.界面.更新适配();}
    }
    // 临时界面与边界输入全部使用独立数据对象，不增加真实或复制的玩家库存。
    static IEnumerator 回收边界界面()
    {
        string 原库存=JsonUtility.ToJson(游戏.道纹数据.导出存档());int 原余额=游戏.宝盒数据.灵石;
        var 弹=游戏.GetComponentsInChildren<RectTransform>().First(x=>x.name=="设置层");
        var 根=new GameObject("回收边界验收",typeof(RectTransform)).GetComponent<RectTransform>();根.SetParent(弹,false);根.anchorMin=根.anchorMax=根.pivot=new Vector2(0,1);根.sizeDelta=new Vector2(1600,900);
        var w=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));var b=new 天帝宝盒(w,1,500);var ui=根.gameObject.AddComponent<天帝道纹回收界面>();ui.初始化(w,b,游戏.默认字体,()=>{});
        yield return 拍("31回收空库存");检查("空背包不能确认回收",ui.选中数量==0&&ui.总回收灵石==0&&!ui.GetComponentsInChildren<Button>().First(x=>x.name=="预览回收").interactable);
        var 随机=new System.Random(420);
        var 满=天帝道纹生成.创建(1,道纹分类.属性,道纹品阶.传说,随机,道纹属性分组.基础,100);while(满.词条.Count<6)满.词条.Add(天帝道纹属性.抽非五行词条(随机,100));w.获得道纹(满);
        for(int i=0;i<36;i++)w.获得道纹(天帝道纹生成.创建(1,道纹分类.属性,(道纹品阶)(i%8),随机,道纹属性分组.基础,100));
        w.设置玩家等级(3);w.解锁格子(new Vector2Int(1,0));w.放置(w.道纹[1],new Vector2Int(1,0));w.保存布局方案(0,"保护方案");w.收回(w.道纹[1]);w.放置(w.道纹[2],new Vector2Int(1,0));
        yield return new WaitForEndOfFrame();点("回收道纹-0");检查("查看详情不加入回收",ui.选中数量==0&&ui.总回收灵石==0);
        int 单价=天帝数值.道纹回收价(ui.显示项(0));勾("回收复选框-0");检查("勾选累计总回收灵石",ui.选中数量==1&&ui.总回收灵石==单价);yield return 拍("32回收六词条详情");
        勾("回收复选框-0");检查("取消勾选总价归零",ui.选中数量==0&&ui.总回收灵石==0);勾("回收复选框-0");
        点("清空回收选择");检查("清空选择同步复选框和总价",ui.选中数量==0&&ui.总回收灵石==0&&!ui.GetComponentsInChildren<Toggle>().First(x=>x.name=="回收复选框-0").isOn);
        ui.设置显示条件(-1,0,3);
        for(int i=0;i<9;i++)勾("回收复选框-"+i);点("回收下一页");for(int i=0;i<9;i++)勾("回收复选框-"+i);检查("跨页保留18枚选择",ui.选中数量==18);
        var 要售=Enumerable.Range(0,18).Select(i=>w.道纹.Where(x=>w.可回收(x,out _)).OrderBy(x=>x.编号).ElementAt(i)).ToArray();int 收益=要售.Sum(天帝数值.道纹回收价);
        检查("跨页累计总回收灵石",ui.总回收灵石==收益);yield return 拍("32b跨页回收总价");
        点("预览回收");yield return 拍("33跨页多品阶回收确认");点("取消回收");检查("批量取消保留选择与金额",ui.选中数量==18&&ui.总回收灵石==收益&&w.道纹.Count==37&&b.灵石==500);
        点("预览回收");yield return 拍("34批量确认重开");点("确认回收");检查("跨页只支付一次且删除18枚",b.灵石==500+收益&&w.道纹.Count==19&&ui.选中数量==0&&ui.总回收灵石==0&&要售.All(x=>!w.道纹.Contains(x)));
        yield return 拍("35批量回收结果");
        ui.设置显示条件(-1,0,0);
        var 保护=Enumerable.Range(0,9).First(i=>ui.显示项(i)!=null&&!w.可回收(ui.显示项(i),out _));点("回收道纹-"+保护);检查("保护道纹查看详情但无法勾选",ui.选中数量==0);yield return 拍("36回收保护道纹");
        var 保护勾=ui.GetComponentsInChildren<Toggle>().First(x=>x.name=="回收复选框-"+保护);检查("保护道纹复选框禁用",!保护勾.interactable&&!保护勾.isOn);
        检查("边界界面未改变玩家库存与余额",原库存==JsonUtility.ToJson(游戏.道纹数据.导出存档())&&原余额==游戏.宝盒数据.灵石);
        根.gameObject.SetActive(false);UnityEngine.Object.Destroy(根.gameObject);
        if(仅回收)yield break;
        var 改根=new GameObject("改造边界验收",typeof(RectTransform)).GetComponent<RectTransform>();改根.SetParent(弹,false);改根.anchorMin=改根.anchorMax=改根.pivot=new Vector2(0,1);改根.sizeDelta=new Vector2(1600,900);
        var 六=天帝道纹生成.创建(1,道纹分类.属性,道纹品阶.传说,随机,道纹属性分组.基础,100);while(六.词条.Count<6)六.词条.Add(天帝道纹属性.抽非五行词条(随机,100));w.获得道纹(六);
        var 改=改根.gameObject.AddComponent<天帝通货界面>();改.初始化(w,new 天帝通货(w,1,1),游戏.默认字体,()=>{});改.选目标(w.道纹.IndexOf(六));改.选通货(通货种类.问天石);yield return 拍("37改造六词条与长说明");改根.gameObject.SetActive(false);UnityEngine.Object.Destroy(改根.gameObject);
    }
    static void 记错(string 文,string 栈,LogType 类){if(类==LogType.Error||类==LogType.Exception){报告.错误.Add(文+"\n"+栈);完成();}}
    static void 完成(){if(结束)return;结束=true;EditorApplication.update-=等待;Application.logMessageReceived-=记错;EditorApplication.isPlaying=false;}
    static void 清理(PlayModeStateChange 状态)
    {
        if(状态!=PlayModeStateChange.EnteredEditMode)return;报告.结束存档=指纹(真实路径);
        检查("完整巡检完成",报告.完整巡检);检查("真实存档未改写",报告.原存档==报告.结束存档);
        天帝存档.验证目录=原目录;EditorSettings.enterPlayModeOptionsEnabled=原选项开;EditorSettings.enterPlayModeOptions=原选项;Application.runInBackground=原后台;
        if(补拍窗口!=null)
        {
            天帝移动适配.验证安全区=补拍原安全区;
            if(补拍窗口!=null)补拍窗口.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(补拍窗口,补拍原尺寸);
        }
        if(原字幕存在)PlayerPrefs.SetInt("Tiandi.Menu.Subtitles",原字幕);else PlayerPrefs.DeleteKey("Tiandi.Menu.Subtitles");
        if(原序章存在)PlayerPrefs.SetInt("Tiandi.Menu.PrologueSeen",原序章);else PlayerPrefs.DeleteKey("Tiandi.Menu.PrologueSeen");PlayerPrefs.Save();
        EditorApplication.update-=等待;EditorApplication.playModeStateChanged-=清理;Application.logMessageReceived-=记错;File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(报告,true));
    }
}
#endif
