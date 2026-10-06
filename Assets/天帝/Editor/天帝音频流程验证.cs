#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class 天帝音频流程验证
{
    [Serializable] public sealed class 报告
    {
        public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>();
        public float 序章总秒, 输出峰值;
        public List<string> 诊断 = new List<string>();
    }
    static 报告 结果;
    static string 目录, 原目录, 真实路径, 原指纹;
    static bool 原选项启用, 原后台, 已启动, 已结束;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static readonly string[] 设置键 = { "Tiandi.Menu.Volume", "Tiandi.Menu.Music", "Tiandi.Menu.Effects", "Tiandi.Menu.Story" };
    static bool[] 原存在;
    static float[] 原值;
    static bool 原已看存在;
    static int 原已看;
    static string 指纹(string p) { if (!File.Exists(p)) return "无"; using(var h = SHA256.Create()) return Convert.ToBase64String(h.ComputeHash(File.ReadAllBytes(p))); }
    static void 检查(string 名, bool 对) { (对 ? 结果.通过 : 结果.失败).Add(名); }
    public static string 启动()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("音频验收需编辑模式且场景已保存，工具不保存场景。");
        if (SceneManager.GetActiveScene().path != "Assets/天帝/场景/天帝.unity") throw new InvalidOperationException("请在已有主场景验收。");
        天帝音频接入.验证素材();
        结果 = new 报告(); 目录 = Path.Combine(天帝构建工具.项目根, "output/音频接入/流程验收"); Directory.CreateDirectory(目录);
        真实路径 = Path.Combine(Application.persistentDataPath, "天帝进度.json"); 原指纹 = 指纹(真实路径);
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        var 数据 = new 天帝存档数据 { 序章已完成 = true, 主角 = 天帝普攻.主角配置(), 画布 = 网.导出存档(), 通货 = new 天帝通货(网,42).导出库存(), 灵石 = 天帝宝盒.开局灵石 };
        string 隔离 = Path.Combine(目录,"隔离存档"); if (!new 天帝存档(隔离).保存(数据)) throw new InvalidOperationException("隔离失败");
        原目录 = 天帝存档.验证目录; 天帝存档.验证目录 = 隔离;
        原存在 = 设置键.Select(PlayerPrefs.HasKey).ToArray(); 原值 = 设置键.Select(k => PlayerPrefs.GetFloat(k,1)).ToArray();
        原已看存在 = PlayerPrefs.HasKey("Tiandi.Menu.PrologueSeen"); 原已看 = PlayerPrefs.GetInt("Tiandi.Menu.PrologueSeen");
        原选项启用 = EditorSettings.enterPlayModeOptionsEnabled; 原选项 = EditorSettings.enterPlayModeOptions; 原后台 = Application.runInBackground;
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload; Application.runInBackground = true;
        已启动 = 已结束 = false; 截止 = EditorApplication.timeSinceStartup + 90;
        var gv = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"); if (gv != null) EditorWindow.GetWindow(gv).Focus();
        EditorApplication.update += 等待; EditorApplication.playModeStateChanged += 清理; Application.logMessageReceived += 记错;
        EditorApplication.isPlaying = true; return 目录;
    }
    static void 等待()
    {
        if (已结束) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.isPlaying)
        {
            var 声 = UnityEngine.Object.FindAnyObjectByType<天帝声音>(); 声?.SendMessage("OnApplicationFocus", true);
            var 漫画 = UnityEngine.Object.FindAnyObjectByType<天帝序章>(); 漫画?.设置焦点(true);
        }
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("超时"); 完成(); return; }
        if (已启动 || !EditorApplication.isPlaying) return;
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>(); if (游戏 == null || 游戏.界面 == null) return;
        已启动 = true; 游戏.StartCoroutine(守护(执行(游戏)));
    }
    static IEnumerator 守护(IEnumerator e)
    {
        while (!已结束)
        {
            bool 有 = false; object 当前 = null;
            try { 有 = e.MoveNext(); if (有) 当前 = e.Current; }
            catch(Exception ex) { 结果.错误.Add(ex.ToString()); 完成(); }
            if (!有) break; yield return 当前;
        }
        完成();
    }
    static IEnumerator 执行(天帝游戏 游戏)
    {
        游戏.声音.SendMessage("OnApplicationFocus",true); 游戏.设置音量(.6f); 游戏.设置音效音量(1); 游戏.设置音乐音量(1); 游戏.设置剧情音量(1);
        yield return new WaitForSecondsRealtime(.5f);
        检查("标题主页曲",游戏.声音.当前音乐编号==0);
        var b = new GameObject("关闭声音验证",typeof(RectTransform),typeof(Button)); b.transform.SetParent(游戏.transform,false);
        var 按键 = b.GetComponent<Button>(); 按键.onClick.AddListener(()=>b.SetActive(false)); 天帝按钮声音.绑定(按键); 天帝按钮声音.绑定(按键);
        int 按前 = 游戏.声音.音效播放次数; 按键.onClick.Invoke(); 检查("关闭按钮保留声音且重复绑定不叠音",游戏.声音.音效播放次数==按前+1); UnityEngine.Object.Destroy(b);
        检查("有限音效池12通道",游戏.声音.音效池数量==12);
        foreach(string 名 in 天帝声音.音效名称) { 检查("播放素材-"+名,游戏.声音.播放(名)); yield return new WaitForSecondsRealtime(.12f); }
        yield return new WaitForSecondsRealtime(.3f);
        bool 首发=游戏.声音.播放("ZD01_灵力弹"); int 计数=游戏.声音.音效播放次数;
        for(int i=0;i<500;i++) 游戏.声音.播放("ZD01_灵力弹");
        检查("500次密集射击限频",首发&&游戏.声音.音效播放次数==计数);
        游戏.设置音效音量(0); 检查("音效静音即时生效",!游戏.声音.播放("UI02_确认")&&游戏.声音.GetComponentsInChildren<AudioSource>().Where(a=>a.name.StartsWith("音效通道")).All(a=>a.volume==0)); 游戏.设置音效音量(1);
        游戏.设置音量(0); 检查("总音量静音",AudioListener.volume==0); 游戏.设置音量(.6f);
        游戏.设置音乐音量(0); yield return new WaitForSecondsRealtime(.7f);
        检查("音乐单独静音",游戏.声音.GetComponentsInChildren<AudioSource>().Where(a=>a.name.StartsWith("音乐交叉淡化")).All(a=>a.volume<.001f)); 游戏.设置音乐音量(1);
        游戏.重看序章(); var 漫画=游戏.GetComponent<天帝序章>(); 漫画.设置焦点(true);
        yield return new WaitForSecondsRealtime(.25f);
        检查("序章音乐切换",游戏.阶段==游戏阶段.序章&&游戏.声音.当前音乐编号==2);
        检查("首格已播放选定声线",漫画.当前配音源.clip!=null&&漫画.当前配音源.clip.name=="VO01"&&漫画.当前配音源.isPlaying);
        yield return new WaitForSecondsRealtime(.6f);
        检查("配音自动压低音乐",游戏.声音.GetComponentsInChildren<AudioSource>().Where(a=>a.name.StartsWith("音乐")&&a.isPlaying).All(a=>a.volume<=.11f));
        结果.序章总秒=(float)漫画.总秒;
        for(int i=0;i<24;i++)
        {
            漫画.设置进度((漫画.格开始秒(i)+.1f)/(float)漫画.总秒);
            var a=漫画.当前配音源;
            检查("格"+(i+1)+"时轴与片段",漫画.当前格==i&&a.clip!=null&&a.clip.name=="VO"+(i+1).ToString("00")&&漫画.格开始秒(i+1)-漫画.格开始秒(i)>=a.clip.length+.79f);
        }
        漫画.设置进度(0); yield return new WaitForSecondsRealtime(.2f);
        游戏.设置剧情音量(0); 检查("剧情音量响应",漫画.当前配音源.volume==0); 游戏.设置剧情音量(1);
        漫画.切换暂停(); double 原秒=漫画.当前秒; int 样本=漫画.当前配音源.timeSamples;
        yield return new WaitForSecondsRealtime(.3f);
        检查("剧情暂停声音与时间同步",漫画.当前秒==原秒&&!漫画.当前配音源.isPlaying&&Math.Abs(漫画.当前配音源.timeSamples-样本)<500);
        漫画.切换暂停(); yield return new WaitForSecondsRealtime(.2f);
        检查("剧情续播",漫画.当前秒>原秒&&漫画.当前配音源.isPlaying);
        漫画.设置后台(true); 原秒=漫画.当前秒; yield return new WaitForSecondsRealtime(.2f);
        检查("后台暂停且禁跳",漫画.当前秒==原秒&&!漫画.当前配音源.isPlaying&&!漫画.设置进度(.5f)); 漫画.设置后台(false);
        漫画.设置焦点(false); 检查("失焦暂停",!漫画.当前配音源.isPlaying); 漫画.设置焦点(true);
        漫画.翻格(1); 检查("翻格从语音开头播放",漫画.当前格==1&&漫画.当前配音源.timeSamples<1500);
        漫画.设置进度((漫画.格开始秒(1)+2)/(float)漫画.总秒); 检查("格内定位对应语音",Math.Abs(漫画.当前配音源.timeSamples/(float)漫画.当前配音源.clip.frequency-2)<.15f);
        游戏.跳过序章(); 游戏.跳过序章(); 检查("跳过停止旁白并返回标题",游戏.阶段==游戏阶段.标题&&!漫画.当前配音源.isPlaying);
        游戏.重看序章(); yield return null; 检查("重看重置首格",漫画.当前格==0&&漫画.当前配音源.clip.name=="VO01"); 游戏.跳过序章();
        检查("隔离正式存档继续",游戏.继续游戏()); yield return null;
        检查("主页音乐恢复",游戏.声音.当前音乐编号==0);
        游戏.打开道纹(); yield return null; 检查("画布构筑曲",游戏.声音.当前音乐编号==1); 游戏.返回主页();
        游戏.打开道纹改造(); yield return null; 检查("改造构筑曲",游戏.声音.当前音乐编号==1); 游戏.返回主页();
        yield return new WaitForSecondsRealtime(.3f); 游戏.声音.SendMessage("OnApplicationPause",true); 检查("游戏后台暂停音乐",游戏.声音.已暂停&&游戏.声音.GetComponentsInChildren<AudioSource>().Where(a=>a.name.StartsWith("音乐")).All(a=>!a.isPlaying));
        游戏.声音.SendMessage("OnApplicationPause",false); 检查("游戏回前台续播音乐",!游戏.声音.已暂停&&游戏.声音.GetComponentsInChildren<AudioSource>().Any(a=>a.name.StartsWith("音乐")&&a.isPlaying));
        检查("进入实际青岚原",游戏.进入战斗()); while(游戏.阶段==游戏阶段.战斗加载) yield return null;
        var 场=游戏.战斗场景; if(场==null) throw new InvalidOperationException("地图未加载"); 场.enabled=false;
        yield return null; 检查("普通战斗音乐",游戏.声音.当前音乐编号==3);
        检查("战斗保持唯一有效监听器",UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.isActiveAndEnabled)==1);
        游戏.界面.切换战斗暂停(); 游戏.声音.同步(); 检查("战斗暂停冻结声音",游戏.声音.已暂停&&!游戏.声音.播放("ZD02_命中")); 游戏.界面.关闭战斗暂停(); 游戏.声音.同步();
        // 隔离场景的边界事件输入：只验收声音，不改变正式伤害、敌人成长或真实库存。
        var 战=场.战斗; var 敌=战.敌人.First(a=>a.已生成&&a.存活); typeof(战斗敌人).GetProperty("位置").SetValue(敌,场.玩家位置+Vector2.right*3);
        计数=游戏.声音.音效播放次数;
        for(int i=0;i<12;i++) { 场.战斗一步(.1f); yield return null; }
        检查("实际释放事件发声",战.普通释放次数>0&&游戏.声音.音效播放次数>计数);
        yield return new WaitForSecondsRealtime(.3f); 计数=游戏.声音.音效播放次数; 战.伤害玩家(1);
        检查("实际受伤反馈发声",游戏.声音.音效播放次数>计数);
        var 非王=战.敌人.Where(a=>a.布点.级别!=战斗敌人级别.王级).ToArray();
        foreach(var a in 非王) { typeof(战斗敌人).GetProperty("已生成").SetValue(a,true); 战.伤害敌人(a,100000); }
        结果.诊断.Add("杀敌后玩家死亡="+战.玩家死亡+" 损伤="+战.敌人损伤比例+" 存活="+战.敌人.Count(a=>a.存活));
        for(int i=0;i<12;i++) { 场.战斗一步(.1f); yield return null; }
        检查("BOSS实际登场切换音乐",战.BOSS已出现&&游戏.声音.当前音乐编号==4);
        结果.诊断.Add("BOSS="+战.BOSS已出现+" 玩家死亡="+战.玩家死亡+" 音乐="+游戏.声音.当前音乐编号);
        var 王=战.敌人.First(a=>a.布点.级别==战斗敌人级别.王级); yield return new WaitForSecondsRealtime(.3f); 计数=游戏.声音.音效播放次数;
        战.伤害敌人(王,100000); 检查("王死胜利音一次",场.王已击败&&游戏.声音.音效播放次数>计数);
        计数=游戏.声音.音效播放次数; 检查("重复胜利通知不发声",!场.通知王级击败(王.布点)&&游戏.声音.音效播放次数==计数);
        游戏.返回主页(); while(游戏.阶段==游戏阶段.战斗加载) yield return null;
        yield return null; 检查("战斗离场音乐恢复",游戏.声音.当前音乐编号==0);
        检查("再次进入验证死亡",游戏.进入战斗()); while(游戏.阶段==游戏阶段.战斗加载) yield return null;
        场=游戏.战斗场景; 场.enabled=false; yield return new WaitForSecondsRealtime(.3f); 计数=游戏.声音.音效播放次数;
        场.战斗.伤害玩家(100000); 场.战斗一步(.1f); 检查("玩家死亡失败声",场.战斗.玩家死亡&&游戏.声音.音效播放次数>计数);
        计数=游戏.声音.音效播放次数; 场.战斗一步(.1f); 检查("重复死亡不叠声音",游戏.声音.音效播放次数==计数);
        游戏.返回主页(); while(游戏.阶段==游戏阶段.战斗加载) yield return null;
        游戏.返回标题(); 游戏.确认开始新游戏(); 漫画.设置焦点(true);
        漫画.设置进度(1- .15f/(float)漫画.总秒); yield return new WaitForSecondsRealtime(.3f);
        检查("自然播完衔接天赋并停止配音",游戏.阶段==游戏阶段.源道纹选择&&!漫画.当前配音源.isPlaying);
        yield return new WaitForSecondsRealtime(2.7f); 游戏.声音.同步(); 检查("天赋选择构筑曲",游戏.声音.当前音乐编号==1);
        计数=游戏.声音.音效播放次数; 检查("正式确认天赋成功发声",游戏.选择天赋(游戏.天赋池.候选[0].编号,游戏.天赋池.轮次)&&游戏.声音.音效播放次数>计数);
        yield return new WaitForSecondsRealtime(1f);
        var 波=new float[4096]; AudioListener.GetOutputData(波,0); 结果.输出峰值=波.Max(a=>Mathf.Abs(a)); 检查("Unity实际混音输出非静音",结果.输出峰值>.00001f);
        结果.诊断.Add("pause="+AudioListener.pause+" listener="+AudioListener.volume+" dsp="+AudioSettings.dspTime+" sources="+string.Join(",",游戏.声音.GetComponentsInChildren<AudioSource>().Where(a=>a.name.StartsWith("音乐")).Select(a=>a.name+":"+a.isPlaying+":"+a.volume+":"+a.time)));
    }
    static void 记错(string m,string s,LogType t) { if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert) { 结果.错误.Add(m+"\n"+s); 完成(); } }
    static void 完成() { if(已结束)return;已结束=true;EditorApplication.update-=等待;Application.logMessageReceived-=记错;File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));EditorApplication.isPlaying=false; }
    static void 清理(PlayModeStateChange s)
    {
        if(s!=PlayModeStateChange.EnteredEditMode)return;
        if(!已结束)结果.错误.Add("检查提前停止");已结束=true;
        检查("真实存档未改写",指纹(真实路径)==原指纹);
        for(int i=0;i<设置键.Length;i++) { if(原存在[i])PlayerPrefs.SetFloat(设置键[i],原值[i]);else PlayerPrefs.DeleteKey(设置键[i]); }
        if(原已看存在)PlayerPrefs.SetInt("Tiandi.Menu.PrologueSeen",原已看);else PlayerPrefs.DeleteKey("Tiandi.Menu.PrologueSeen"); PlayerPrefs.Save();
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));
        EditorApplication.update-=等待;Application.logMessageReceived-=记错;EditorApplication.playModeStateChanged-=清理;
        天帝存档.验证目录=原目录;EditorSettings.enterPlayModeOptionsEnabled=原选项启用;EditorSettings.enterPlayModeOptions=原选项;Application.runInBackground=原后台;
    }
}
#endif
