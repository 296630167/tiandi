#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class 天帝漫画序章验证
{
    [Serializable] public sealed class 报告
    {
        public int 页数 = 8, 格数 = 24;
        public double 时长秒 = 156;
        public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>();
        public string 原存档指纹, 结束存档指纹;
    }
    static 报告 结果;
    static string 目录, 原目录, 真实路径, 旧档路径, 旧档指纹;
    static bool 原选项启用, 原后台, 开始, 结束, 原字幕存在, 原序章存在;
    static int 原字幕, 原序章;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static string 指纹(string p)
    { if (!File.Exists(p)) return "无文件"; using (var h = SHA256.Create()) return Convert.ToBase64String(h.ComputeHash(File.ReadAllBytes(p))); }
    public static string 启动()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("漫画验收需要编辑模式且无未保存场景；不重建或保存场景。");
        if (SceneManager.GetActiveScene().path != "Assets/天帝/场景/天帝.unity") throw new InvalidOperationException("请在现有主场景验收。");
        天帝数值同步检查.校验();
        目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/漫画序章-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        真实路径 = Path.Combine(Application.persistentDataPath, "天帝进度.json");
        结果 = new 报告 { 原存档指纹 = 指纹(真实路径) };
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        var 数据 = new 天帝存档数据 { 序章已完成 = true, 主角 = 天帝普攻.主角配置(), 画布 = 网.导出存档(), 通货 = new 天帝通货(网,42).导出库存(), 灵石 = 天帝宝盒.开局灵石 };
        var 隔离 = Path.Combine(目录, "隔离存档");
        if (!new 天帝存档(隔离).保存(数据)) throw new InvalidOperationException("隔离旧档创建失败。");
        旧档路径 = Path.Combine(隔离, "天帝进度.json"); 旧档指纹 = 指纹(旧档路径);
        原目录 = 天帝存档.验证目录; 天帝存档.验证目录 = 隔离;
        原字幕存在 = PlayerPrefs.HasKey("Tiandi.Menu.Subtitles"); 原字幕 = PlayerPrefs.GetInt("Tiandi.Menu.Subtitles",1);
        原序章存在 = PlayerPrefs.HasKey("Tiandi.Menu.PrologueSeen"); 原序章 = PlayerPrefs.GetInt("Tiandi.Menu.PrologueSeen",0);
        原选项启用 = EditorSettings.enterPlayModeOptionsEnabled; 原选项 = EditorSettings.enterPlayModeOptions; 原后台 = Application.runInBackground;
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        var t = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"); if (t != null) EditorWindow.GetWindow(t).Focus();
        开始 = 结束 = false; 截止 = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update += 等待; EditorApplication.playModeStateChanged += 清理; Application.logMessageReceived += 记错;
        EditorApplication.isPlaying = true; return 目录;
    }
    static void 等待()
    {
        if (结束) return;
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("流程超时"); 完成(); return; }
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate();
        if (开始) return;
        var g = UnityEngine.Object.FindAnyObjectByType<天帝游戏>(); if (g == null || g.阶段 != 游戏阶段.标题) return;
        开始 = true; var 输入 = g.GetComponentInChildren<InputSystemUIInputModule>(); if (输入 != null) 输入.enabled = false;
        g.StartCoroutine(保护(流程(g)));
    }
    static IEnumerator 保护(IEnumerator 流)
    {
        var 栈 = new Stack<IEnumerator>(); 栈.Push(流);
        while (栈.Count > 0)
        {
            bool 有; object 值 = null;
            try { 有 = 栈.Peek().MoveNext(); if (有) 值 = 栈.Peek().Current; }
            catch (Exception ex) { 结果.错误.Add(ex.ToString()); break; }
            if (!有) { 栈.Pop(); continue; }
            if (值 is IEnumerator 子) { 栈.Push(子); continue; }
            yield return 值;
        }
        完成();
    }
    static bool 点击(天帝游戏 g, string 名)
    {
        Canvas.ForceUpdateCanvases(); var b = g.GetComponentsInChildren<Button>().FirstOrDefault(k => k.name == 名);
        if (b == null || !b.interactable) return false;
        var r = (RectTransform)b.transform;
        var p = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)) };
        var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(p,命中);
        if (命中.Count == 0 || 命中[0].gameObject != b.gameObject) return false;
        ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerClickHandler); return true;
    }
    static IEnumerator 截图(string 名)
    {
        Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        var t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(目录,名+".png"),t.EncodeToPNG()); UnityEngine.Object.Destroy(t);
    }
    static IEnumerator 流程(天帝游戏 g)
    {
        yield return null;
        g.开始序章(); 检查("旧档开新游戏先确认",g.界面.确认已打开 && g.阶段 == 游戏阶段.标题);
        yield return null;
        检查("实际按钮确认进入漫画",点击(g,"开始新游戏") && g.阶段 == 游戏阶段.序章);
        var s = g.GetComponent<天帝序章>(); s.设置焦点(true); g.设置字幕(true);
        yield return null;
        结果.时长秒 = s.总秒;
        检查("24格按配音时长且无旧视频播放器",s.总秒 >= 156 && 天帝序章.叙述.Length == 24 && g.GetComponent<UnityEngine.Video.VideoPlayer>() == null);
        for (int i=0;i<9;i++) 检查("新素材已绑定 "+i,g.美术.获取(i<8?"CM"+(i+1).ToString("00"):"CB01")?.texture.width == 1920);
        检查("暂停按钮可命中",点击(g,"暂停序章") && s.已暂停);
        double 原秒=s.当前秒; yield return new WaitForSecondsRealtime(.25f); 检查("暂停保持进度",s.当前秒==原秒);
        s.设置后台(true); s.设置焦点(false); s.设置焦点(true);
        检查("焦点恢复不解除后台暂停",!s.可调整进度 && !s.设置进度(.3f));
        s.设置后台(false); 检查("前台恢复保留手动暂停",s.已暂停 && s.可调整进度);
        s.切换暂停(); s.设置焦点(false); 原秒=s.当前秒;
        yield return new WaitForSecondsRealtime(.25f); 检查("失焦自动停止时钟",s.当前秒==原秒);
        s.设置焦点(true); s.切换暂停();
        检查("拒绝非有限进度",!s.设置进度(float.NaN)&&!s.设置进度(float.PositiveInfinity));
        s.设置进度(0);
        检查("漫画下一格按钮",点击(g,"下一格")&&s.当前格==1&&s.已暂停);
        检查("漫画上一格按钮",点击(g,"上一格")&&s.当前格==0&&s.已暂停);
        for (int n=0;n<24;n++)
        {
            检查("定位漫画格 "+n,g.调整序章进度((s.格开始秒(n)+.4f)/(float)s.总秒)&&s.当前格==n);
            yield return null; Canvas.ForceUpdateCanvases();
            var 格=g.GetComponentsInChildren<RawImage>().Where(x=>x.name.StartsWith("漫画格-")).OrderBy(x=>x.name).ToArray();
            检查("逐格显隐及素材 "+n,格.Length==3&&格.Count(x=>x.enabled)==n%3+1&&格.All(x=>x.texture==g.美术.获取("CM"+(n/3+1).ToString("00")).texture));
            var 字=g.GetComponentsInChildren<Text>().First(x=>x.text==天帝序章.叙述[n]);
            检查("正文行高完整 "+n,字.preferredHeight<=字.rectTransform.rect.height+1);
            if(n==8) yield return 截图("漫画03_灵感");
            if(n==23) yield return 截图("漫画08_选择起点");
        }
        var 条=g.GetComponentInChildren<天帝序章进度条>(); Canvas.ForceUpdateCanvases();
        var rt=(RectTransform)条.transform;
        var 指=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(new Vector3(rt.rect.xMin+rt.rect.width*.37f,rt.rect.center.y,0)))};
        条.OnPointerDown(指); 条.OnPointerUp(指);
        检查("真实UGUI进度回拖",s.当前秒/s.总秒>.33 && s.当前秒/s.总秒<.41 && !条.调整中 && s.已暂停);
        检查("字幕按钮可关闭",点击(g,"字幕")&&!g.字幕开启); 检查("字幕关闭不销毁漫画",g.GetComponentsInChildren<RawImage>().Any(x=>x.name.StartsWith("漫画格-")));
        检查("字幕按钮可重开",点击(g,"字幕")&&g.字幕开启);
        检查("选择前隔离旧档未改写",指纹(旧档路径)==旧档指纹);
        s.设置进度(.994f); s.切换暂停(); yield return new WaitForSecondsRealtime(1.4f);
        检查("正常播完进入天赋衔接",g.阶段==游戏阶段.源道纹选择&&s.可衔接道纹选择);
        yield return new WaitForSecondsRealtime(2.7f);
        检查("五张卡全部开放",g.界面.源道纹页.可选择&&g.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("源道纹-")&&b.interactable)==5);
        检查("五枚图纹均未封印",g.GetComponentsInChildren<天帝源道纹绘图>().All(x=>!x.封印));
        检查("五选一轮内无重复",g.天赋池.候选.Select(t=>t.编号).Distinct().Count()==5);
        检查("未选中确认不可点击",!点击(g,"确认天赋"));
        for(int i=0;i<5;i++) 检查("第"+(i+1)+"张卡真实命中",点击(g,"源道纹-"+i)&&g.界面.源道纹页.选中槽位==i);
        Canvas.ForceUpdateCanvases();
        var 溢出=g.GetComponentsInChildren<Text>().Where(x=>x.preferredHeight>x.rectTransform.rect.height+1).ToArray();
        检查("天赋页文字行高完整",溢出.Length==0);
        foreach(var 字 in 溢出)结果.失败.Add("文字溢出 "+字.text+" "+字.preferredHeight+"/"+字.rectTransform.rect.height);
        yield return 截图("漫画天赋衔接");
        int 旧轮=g.天赋池.轮次;
        检查("刷新清除选中并拒绝旧轮",点击(g,"刷新天赋")&&g.界面.源道纹页.选中槽位==-1&&!g.选择天赋(g.天赋池.候选[0].编号,旧轮));
        检查("刷新仍不覆盖旧档",指纹(旧档路径)==旧档指纹);
        检查("可取消开局返回标题",点击(g,"返回标题")&&g.阶段==游戏阶段.标题&&指纹(旧档路径)==旧档指纹);
        g.重看序章(); s.设置焦点(true); 检查("标题重看进入漫画",g.阶段==游戏阶段.序章);
        g.跳过序章(); yield return null;
        检查("标题重看跳过回标题",g.阶段==游戏阶段.标题&&指纹(旧档路径)==旧档指纹);
        g.确认开始新游戏(); s.设置焦点(true); yield return null;
        检查("早期跳过按钮可命中",点击(g,"跳过序章")&&g.阶段==游戏阶段.源道纹选择);
        var 池=g.天赋池; s.跳过(); 检查("完成回调不重复触发",g.天赋池==池);
        yield return new WaitForSecondsRealtime(2.7f);
        int 编号=g.天赋池.候选[4].编号;
        检查("第五槽确认后创建正式开局",点击(g,"源道纹-4")&&点击(g,"确认天赋")&&g.阶段==游戏阶段.主页&&g.当前天赋.编号==编号);
        检查("正式开局无额外道纹通货或测试数值",g.道纹数据.道纹.Count==0&&天帝通货.可用种类.All(k=>g.通货数据.数量(k)==0)&&!g.主角属性.导出配置().使用测试数据);
        检查("新档仅确认后替换并保留旧备份",g.保存进度()&&指纹(旧档路径)!=旧档指纹&&File.Exists(旧档路径+".bak"));
        string 新档指纹=指纹(旧档路径); g.重看序章(); s.设置焦点(true); s.设置进度(.995f); yield return new WaitForSecondsRealtime(1.2f);
        检查("主页重看正常完成回主页且不改档",g.阶段==游戏阶段.主页&&指纹(旧档路径)==新档指纹);
    }
    static void 记错(string 消息,string 栈,LogType 类型)
    { if(类型==LogType.Error||类型==LogType.Exception||类型==LogType.Assert){结果.错误.Add(消息+"\n"+栈);完成();} }
    static void 完成()
    {
        if(结束)return; 结束=true; EditorApplication.update-=等待; Application.logMessageReceived-=记错;
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true)); EditorApplication.isPlaying=false;
    }
    static void 清理(PlayModeStateChange 状态)
    {
        if(状态!=PlayModeStateChange.EnteredEditMode)return;
        if(!结束)结果.错误.Add("验收提前停止");
        结果.结束存档指纹=指纹(真实路径); 检查("真实玩家存档指纹不变",结果.原存档指纹==结果.结束存档指纹);
        EditorApplication.update-=等待; EditorApplication.playModeStateChanged-=清理; Application.logMessageReceived-=记错;
        天帝存档.验证目录=原目录; EditorSettings.enterPlayModeOptionsEnabled=原选项启用; EditorSettings.enterPlayModeOptions=原选项; Application.runInBackground=原后台;
        if(原字幕存在)PlayerPrefs.SetInt("Tiandi.Menu.Subtitles",原字幕);else PlayerPrefs.DeleteKey("Tiandi.Menu.Subtitles");
        if(原序章存在)PlayerPrefs.SetInt("Tiandi.Menu.PrologueSeen",原序章);else PlayerPrefs.DeleteKey("Tiandi.Menu.PrologueSeen");
        PlayerPrefs.Save(); File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));
    }
}
#endif
