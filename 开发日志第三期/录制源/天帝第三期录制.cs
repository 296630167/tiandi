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
using UnityEngine.UI;
using Process = System.Diagnostics.Process;

// 隔离工程专用：复制已有进度、调用正式UI与战斗，不添加道纹、资源、生命或伤害。
public static class 天帝第三期录制
{
    [Serializable] public sealed class 片段 { public string 名; public int 开始帧,结束帧; }
    [Serializable] public sealed class 报告
    {
        public int 帧数,开始等级,地图等级,击败数;
        public bool 完成,玩家死亡;
        public List<片段> 片段=new List<片段>();
        public List<string> 错误=new List<string>(),编辑器诊断=new List<string>();
        public string 方法="当前运行源码；现有进度隔离副本；正式UI与正式战斗；30fps原速选段。";
    }
    static 天帝游戏 游戏;
    static 报告 结果=new 报告();
    static string 目录="C:/Users/123/Documents/ChatGPT/杂谈/天帝/开发日志第三期/录制源";
    static Process 编码;
    static Camera 界面相机;
    static RenderTexture 目标;
    static Texture2D 帧图;
    static Texture2D 场景图;
    static bool 已开始,结束;
    static double 截止;
    static int 原序章偏好;
    public static void 运行()
    {
        Directory.CreateDirectory(目录);
        原序章偏好=PlayerPrefs.GetInt("Tiandi.Menu.PrologueSeen",0);
        天帝存档.验证目录=Path.Combine(目录,"隔离存档");Directory.CreateDirectory(天帝存档.验证目录);
        File.Copy("C:/Users/123/AppData/LocalLow/TiandiStudio/从参加聚光灯比赛到我为天帝镇压世间一切/天帝进度.json",Path.Combine(天帝存档.验证目录,"天帝进度.json"),true);
        天帝移动适配.验证移动平台=false;天帝移动适配.验证屏幕尺寸=new Vector2Int(1920,1080);天帝移动适配.验证安全区=new Rect(0,0,1920,1080);
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        Application.runInBackground=true;Time.captureFramerate=30;
        截止=EditorApplication.timeSinceStartup+900;
        Application.logMessageReceived+=日志;EditorApplication.update+=等待;EditorApplication.isPlaying=true;
    }
    static void 等待()
    {
        if(结束)return;
        if(EditorApplication.timeSinceStartup>截止){结果.错误.Add("录制超时");完成();return;}
        EditorApplication.QueuePlayerLoopUpdate();
        if(已开始||!EditorApplication.isPlaying)return;
        游戏=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if(游戏==null||游戏.阶段!=游戏阶段.标题)return;
        已开始=true;游戏.StartCoroutine(保护(流程()));
    }
    static IEnumerator 保护(IEnumerator 流)
    {
        var 栈=new Stack<IEnumerator>();栈.Push(流);
        while(栈.Count>0)
        {
            bool 有;object 当前=null;
            try{有=栈.Peek().MoveNext();if(有)当前=栈.Peek().Current;}
            catch(Exception 异常){结果.错误.Add(异常.ToString());break;}
            if(!有){栈.Pop();continue;}if(当前 is IEnumerator 子){栈.Push(子);continue;}yield return 当前;
        }
        完成();
    }
    static void 段(string 名)
    {
        if(结果.片段.Count>0)结果.片段.Last().结束帧=结果.帧数;
        结果.片段.Add(new 片段{名=名,开始帧=结果.帧数});
        File.WriteAllText(Path.Combine(目录,"progress.json"),JsonUtility.ToJson(结果,true));
    }
    static IEnumerator 录(int 数,Action<int> 动作=null)
    {
        for(int 序=0;序<数;序++){动作?.Invoke(序);yield return null;拍();}
    }
    static void 拍()
    {
        if(目标==null)
        {
            目标=new RenderTexture(1920,1080,24);目标.Create();帧图=new Texture2D(1920,1080,TextureFormat.RGBA32,false);场景图=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
            var 对象=new GameObject("第三期取景相机",typeof(Camera));界面相机=对象.GetComponent<Camera>();界面相机.enabled=false;
            界面相机.orthographic=true;界面相机.orthographicSize=540;界面相机.aspect=16f/9;界面相机.transform.position=new Vector3(10000,0,-20);
            编码=new Process();编码.StartInfo.FileName="C:/Users/123/Documents/ChatGPT/杂谈/天帝/生成/录制工具/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
            编码.StartInfo.Arguments="-hide_banner -loglevel error -y -f image2pipe -framerate 30 -vcodec mjpeg -i - -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \""+Path.Combine(目录,"当前版本原始实录.mp4")+"\"";
            编码.StartInfo.UseShellExecute=false;编码.StartInfo.CreateNoWindow=true;编码.StartInfo.RedirectStandardInput=true;编码.Start();
        }
        bool 战=游戏.阶段==游戏阶段.战斗;
        if(战)
        {
            var 相机=游戏.战斗场景.俯视相机;var 原目标=相机.targetTexture;var 原比例=相机.aspect;var 原框=相机.rect;
            相机.aspect=16f/9;相机.rect=new Rect(0,0,1,1);相机.targetTexture=目标;相机.Render();相机.targetTexture=原目标;相机.aspect=原比例;相机.rect=原框;
            var 原激活场景=RenderTexture.active;RenderTexture.active=目标;场景图.ReadPixels(new Rect(0,0,1920,1080),0,0);场景图.Apply();RenderTexture.active=原激活场景;
        }
        var 布=游戏.GetComponentInChildren<Canvas>();var 缩放=布.GetComponent<CanvasScaler>();
        var 父=布.transform.parent;var 位置=布.transform.localPosition;var 比例=布.transform.localScale;var 大小=((RectTransform)布.transform).sizeDelta;var 启用=缩放.enabled;
        布.transform.SetParent(null,false);布.transform.position=new Vector3(10000,0,0);布.transform.localScale=Vector3.one;
        缩放.enabled=false;布.renderMode=RenderMode.WorldSpace;布.worldCamera=界面相机;((RectTransform)布.transform).sizeDelta=new Vector2(1920,1080);
        Canvas.ForceUpdateCanvases();游戏.界面.更新适配();Canvas.ForceUpdateCanvases();游戏.界面.更新适配();Canvas.ForceUpdateCanvases();
        foreach(var 按钮 in 游戏.GetComponentsInChildren<天帝按钮文字区域>())按钮.更新();
        界面相机.clearFlags=CameraClearFlags.SolidColor;界面相机.backgroundColor=战?Color.clear:new Color(.05f,.09f,.08f);界面相机.targetTexture=目标;界面相机.Render();
        var 原激活=RenderTexture.active;RenderTexture.active=目标;帧图.ReadPixels(new Rect(0,0,1920,1080),0,0);帧图.Apply();RenderTexture.active=原激活;
        布.transform.SetParent(父,false);布.transform.localPosition=位置;布.transform.localScale=比例;布.renderMode=RenderMode.ScreenSpaceOverlay;((RectTransform)布.transform).sizeDelta=大小;缩放.enabled=启用;
        if(战)
        {
            var 前=帧图.GetPixels32();var 后=场景图.GetPixels32();
            for(int 序=0;序<前.Length;序++){int 透=前[序].a;前[序]=new Color32((byte)((前[序].r*透+后[序].r*(255-透))/255),(byte)((前[序].g*透+后[序].g*(255-透))/255),(byte)((前[序].b*透+后[序].b*(255-透))/255),255);}
            帧图.SetPixels32(前);帧图.Apply();
            if(结果.帧数-结果.片段.Last().开始帧==15)File.WriteAllBytes(Path.Combine(目录,"场景-"+结果.片段.Count.ToString("00")+".jpg"),场景图.EncodeToJPG(90));
        }
        var 字节=帧图.EncodeToJPG(92);编码.StandardInput.BaseStream.Write(字节,0,字节.Length);
        if(结果.帧数-结果.片段.Last().开始帧==15)File.WriteAllBytes(Path.Combine(目录,"画面-"+结果.片段.Count.ToString("00")+".jpg"),字节);
        结果.帧数++;
    }
    static void 点(string 名)
    {
        var 按钮=游戏.GetComponentsInChildren<Button>().First(b=>b.name==名&&b.gameObject.activeInHierarchy);
        if(!按钮.interactable)throw new Exception("按钮不可用："+名);按钮.onClick.Invoke();
    }
    static IEnumerator 流程()
    {
        for(int 序=0;序<8;序++)yield return null;
        var 输入=游戏.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();if(输入!=null)输入.enabled=false;
        段("第三期开发日志 · 当前版本实录");yield return 录(45);
        游戏.确认开始新游戏();游戏.GetComponent<天帝序章>().设置后台(false);游戏.GetComponent<天帝序章>().设置焦点(true);
        for(int 序=0;序<30;序++)yield return null;游戏.调整序章进度(.25f);段("序章 · 从比赛现场到异界");yield return 录(30);
        游戏.跳过序章();int 等=0;while(!游戏.界面.源道纹页.可选择&&等++<240)yield return null;
        if(!游戏.界面.源道纹页.可选择)throw new Exception("天赋演出未就绪");
        游戏.界面.源道纹页.显示详情(0,new Vector2(700,550));段("十三种天赋 · 随机五选一");yield return 录(30);
        游戏.返回标题();if(!游戏.继续游戏())throw new Exception("进度读取失败");结果.开始等级=游戏.道纹数据.玩家等级;
        段("剪纸山水主页 · 自选地图等级");yield return 录(60);
        游戏.界面.显示角色();段("角色 · 属性与当前构筑");yield return 录(30);游戏.界面.关闭角色();
        游戏.界面.显示宝盒();段("宝盒 · 灵石收集道纹");yield return 录(15);
        var 抽取=游戏.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="抽取"&&b.interactable);if(抽取!=null)抽取.onClick.Invoke();yield return 录(30);游戏.界面.关闭宝盒();
        游戏.打开道纹();段("白瓷道纹 · 接口连接与实时构筑");yield return 录(75);
        游戏.界面.道纹页.显示属性(游戏.道纹数据.已放置[Vector2Int.zero],new Vector2(810,560));yield return 录(30);游戏.界面.道纹页.指针离开();游戏.返回主页();
        游戏.打开道纹改造();var 改=游戏.界面.改造页;int 索引=游戏.道纹数据.道纹.FindIndex(t=>t.分类==道纹分类.属性);
        if(索引>=0)改.选目标(索引);改.选通货(通货种类.易纹砂);段("道纹改造 · 十一种通货");yield return 录(60);点("关闭通货");
        游戏.界面.显示回收();点("一键选中");段("道纹回收 · 批量选择与灵石预览");yield return 录(45);游戏.界面.关闭回收();
        游戏.界面.显示图鉴();游戏.界面.图鉴页.定位分类("功能道纹");段("图鉴 · 功能、特性与转化");yield return 录(30);游戏.界面.图鉴页.定位分类("特性道纹");yield return 录(30);游戏.界面.图鉴页.定位分类("转化道纹");yield return 录(30);游戏.界面.关闭图鉴();
        游戏.界面.显示设置();段("设置 · 音乐、音效与剧情音量");yield return 录(30);游戏.界面.关闭设置();
        游戏.选择地图等级(1);游戏.界面.显示主页();游戏.界面.请求进入地图();段("青岚原 · 进入前确认挑战");yield return 录(30);点("确认进入");
        等=0;while(游戏.阶段==游戏阶段.战斗加载&&等++<600)yield return null;
        if(游戏.阶段!=游戏阶段.战斗)throw new Exception("入图未完成");
        var 场=游戏.战斗场景;场.enabled=false;结果.地图等级=游戏.当前地图等级;
        // 省略开场空等，推进正式战斗时钟，不生成额外敌人。
        for(int 序=0;序<180;序++){战斗帧();yield return null;}
        段("青岚原实战 · 四面来敌与走位");yield return 录(150,序=>战斗帧());
        段("实战原速 · 命中反馈与自动拾取");yield return 录(180,序=>战斗帧());
        游戏.界面.切换战斗暂停();段("暂停 · 查看战况与通路参数");yield return 录(30);
        结果.玩家死亡=场.战斗.玩家死亡;结果.击败数=场.战斗.击败数;结果.完成=true;
    }
    static void 战斗帧()
    {
        var 场=游戏.战斗场景;var 人=场.玩家位置;var 敌=场.战斗.敌人.Where(t=>t.存活).ToArray();
        Vector2 最好=Vector2.zero;float 最大=float.MinValue;
        for(int 序=0;序<=16;序++)
        {
            var 向=序==16?Vector2.zero:new Vector2(Mathf.Cos(序*Mathf.PI/8),Mathf.Sin(序*Mathf.PI/8));var 点=场.地图.移动(人,向,游戏.主角属性.跑步速度*.4f);
            if(向!=Vector2.zero&&(点-人).sqrMagnitude<.1f)continue;float 分=-点.magnitude*.05f,近=100;
            foreach(var 对手 in 敌){float 距=Vector2.Distance(点,对手.位置);近=Mathf.Min(近,距);if(距<4)分-=(4-距)*(4-距)*14;}
            if(敌.Length>0)分-=Mathf.Abs(近-6);if(分>最大){最大=分;最好=向;}
        }
        场.移动一步(最好,true,1f/30);场.战斗一步(1f/30);
        typeof(天帝战斗场景).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(场,null);
    }
    static void 日志(string 文,string 栈,LogType 类)
    {if(类==LogType.Error||类==LogType.Exception)(栈.Contains("UnityEditor.Search.")?结果.编辑器诊断:结果.错误).Add(文+"\n"+栈);}
    static void 完成()
    {
        if(结束)return;结束=true;
        if(结果.片段.Count>0)结果.片段.Last().结束帧=结果.帧数;
        if(编码!=null){编码.StandardInput.Close();if(!编码.WaitForExit(10000)){编码.Kill();结果.错误.Add("编码超时");}else if(编码.ExitCode!=0)结果.错误.Add("编码退出码 "+编码.ExitCode);编码.Dispose();}
        File.WriteAllText(Path.Combine(目录,"录制报告.json"),JsonUtility.ToJson(结果,true));Application.logMessageReceived-=日志;EditorApplication.update-=等待;
        PlayerPrefs.SetInt("Tiandi.Menu.PrologueSeen",原序章偏好);PlayerPrefs.Save();
        EditorApplication.Exit(结果.完成&&结果.错误.Count==0?0:2);
    }
}
#endif
