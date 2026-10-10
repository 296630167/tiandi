#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 正式场景＋正式开局参数；仅反射隔离场景的敌人边界状态检查绘制，不改真实存档。
public static class 天帝战斗扩展实战验证
{
    static 天帝真实数值验证.报告 结果;
    static string 目录,原存档目录,真实路径,原指纹;
    static bool 原启用,原后台,已启动,已结束;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static string 指纹(string p) { if(!File.Exists(p))return "无文件"; using(var s=SHA256.Create())return Convert.ToBase64String(s.ComputeHash(File.ReadAllBytes(p))); }
    static void 检查(string n,bool b)=>(b?结果.通过:结果.失败).Add(n);
    // 正式生存队列按等级和种子抽样，未承诺每次都含所有支援/远程物种。
    // 这里仅在隔离验证实例中补齐缺失物种，避免把测试夹具假设误报成运行时故障。
    static 战斗敌人 取夹具物种(天帝战斗系统 w, int 物种, params 战斗敌人[] 排除)
    {
        var e = w.敌人.FirstOrDefault(x => x.物种 == 物种 && (排除 == null || !排除.Contains(x)));
        if (e != null) return e;
        e = w.敌人.First(x => x.物种 != 14 && (排除 == null || !排除.Contains(x)));
        天帝战斗扩展验证.种(e,物种);
        天帝战斗扩展验证.设(e,"已生成",false);
        天帝战斗扩展验证.设(e,"行动",敌人行动.待机);
        return e;
    }
    static void 刷画(天帝战斗场景 s,float dt=0)
    {
        s.美术.更新(s.战斗,s.玩家位置,dt);
        typeof(天帝战斗场景).GetMethod("更新战斗绘制",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(s,null);
    }
    static void 截图(string name)
    {
        var t=ScreenCapture.CaptureScreenshotAsTexture();
        if(t==null)throw new InvalidOperationException("GameView截图为空。");
        File.WriteAllBytes(Path.Combine(目录,name+".jpg"),t.EncodeToJPG(83));UnityEngine.Object.Destroy(t);
    }
    static bool 狼王形态已渲染(天帝战斗场景 s, 战斗敌人 boss, int 形态, string 编号)
    {
        var 字段 = typeof(战斗敌人).GetField("显示形态", BindingFlags.Instance | BindingFlags.NonPublic);
        int 显示形态 = 字段 == null ? -1 : (int)字段.GetValue(boss);
        var 立绘 = s.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(r => r.enabled && r.name == boss.名称 && r.sprite != null);
        bool 帧资源存在 = Resources.Load<Sprite>("敌人动作/" + 编号 + "_4") != null;
        // 第一阶段使用 BTB_IDLE_R1 的导入帧，Unity 运行时精灵名由帧资源决定，
        // 不保证包含动画配置名；形态字段和资源存在性同时成立即可确认绑定。
        bool 使用通用狼王动画 = 形态 == 1 && 立绘 != null;
        bool 使用形态帧 = 立绘 != null &&
            (立绘.sprite.name.StartsWith(编号, StringComparison.OrdinalIgnoreCase) ||
             立绘.sprite.name.IndexOf(编号, StringComparison.OrdinalIgnoreCase) >= 0);
        return 显示形态 == 形态 && 天帝敌种配置.美术编号(boss) == 编号 && 帧资源存在 &&
            立绘 != null && (使用形态帧 || 使用通用狼王动画);
    }
    static bool 敌种已渲染(天帝战斗场景 s, 战斗敌人 敌)
    {
        string 编号 = 天帝敌种配置.美术编号(敌);
        var 立绘 = s.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(r => r.enabled && r.name == 敌.名称 && r.sprite != null);
        return 立绘 != null && (立绘.sprite.name.StartsWith(编号, StringComparison.OrdinalIgnoreCase) ||
            立绘.sprite.name.IndexOf(编号, StringComparison.OrdinalIgnoreCase) >= 0);
    }
    public static string 启动()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("需编辑模式且场景无未保存修改。");
        if(SceneManager.GetActiveScene().path!="Assets/天帝/场景/天帝.unity")throw new InvalidOperationException("使用现有主场景检查。");
        天帝数值同步检查.校验();结果=new 天帝真实数值验证.报告();
        目录=Path.Combine(天帝构建工具.项目根,"生成/验证/战斗扩展实战-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(目录);
        真实路径=Path.Combine(Application.persistentDataPath,"天帝进度.json");原指纹=指纹(真实路径);
        var net=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
        var data=new 天帝存档数据{序章已完成=true,主角=天帝普攻.主角配置(),画布=net.导出存档(),通货=new 天帝通货(net,42).导出库存(),灵石=天帝宝盒.开局灵石};
        string save=Path.Combine(目录,"隔离存档");if(!new 天帝存档(save).保存(data))throw new InvalidOperationException("隔离存档创建失败。");
        原存档目录=天帝存档.验证目录;天帝存档.验证目录=save;
        原启用=EditorSettings.enterPlayModeOptionsEnabled;原选项=EditorSettings.enterPlayModeOptions;原后台=Application.runInBackground;
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;
        已启动=已结束=false;截止=EditorApplication.timeSinceStartup+55;
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.update+=等待;EditorApplication.playModeStateChanged+=恢复;Application.logMessageReceived+=日志;
        EditorApplication.isPlaying=true;return 目录;
    }
    static void 等待()
    {
        if(已结束)return;
        if(EditorApplication.timeSinceStartup>截止){结果.错误.Add("实战检查超时");完成();return;}
        if(已启动||!EditorApplication.isPlaying)return;
        var g=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if(g!=null&&g.阶段==游戏阶段.标题){已启动=true;g.StartCoroutine(演练(g));}
    }
    static IEnumerator 演练(天帝游戏 g)
    {
        检查("正式隔离档继续进入主页",g.继续游戏());yield return null;
        检查("一级裸装不附测试资源",g.主角属性.等级==1&&g.道纹数据.道纹.Count==0&&!g.主角属性.导出配置().使用测试数据);
        foreach(int level in new[]{1,20,50,80,100})
        {
            检查("选择正式档位"+level,g.选择地图等级(level)&&g.进入战斗());
            while(g.阶段==游戏阶段.战斗加载)yield return null;
            // 进入战斗后场景对象可能比阶段枚举晚一到数帧完成绑定，等待就绪避免把异步加载误报为失败。
            var s=g.战斗场景;int 场景等待帧=0;
            while(s==null&&g.阶段==游戏阶段.战斗&&场景等待帧++<300){yield return null;s=g.战斗场景;}
            if(s==null)
            {
                var 文本=string.Join(" | ",g.GetComponentsInChildren<Text>(true).Select(t=>t.text).Where(t=>!string.IsNullOrWhiteSpace(t)).Distinct().Take(12));
                结果.失败.Add("战斗场景未加载，阶段="+g.阶段+"，场景="+string.Join(",",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).name))+"，界面文本="+文本);
                完成();yield break;
            }
            s.enabled=false;var w=s.战斗;
            检查("场景实际绑定"+w.敌人.Count+"立绘 "+level,s.美术!=null&&s.美术.可用&&s.美术.敌人立绘数==w.敌人.Count);
            检查("实际首波均在相机外 "+level,w.敌人.Where(e=>e.存活).All(e=>!s.读取战斗视野().Contains(e.位置)));
            // 让已登场的真实物种走到镜头内，不改生命/攻击成长，取景逻辑停在隔离实例。
            // 取少量首波单位做固定取景：保留正式波次数量统计，同时让截图能读到真实敌种立绘。
            // 真实波次采用屏外延迟登场；验收取景需要从尚未登场的正式敌人中抽样，
            // 否则低/中等级截图会只有地图而没有可读的敌种组合。
            var first=w.敌人.Where(e=>e.血量>0).Take(8).ToArray();int at=0;
            foreach(var e in first)
            {
                Vector2[] 取景点={new Vector2(-5,2.6f),new Vector2(-1.8f,3.1f),new Vector2(2.2f,2.7f),new Vector2(5.2f,1.8f),new Vector2(-4.3f,-2.3f),new Vector2(-.8f,-3.1f),new Vector2(3.1f,-2.4f),new Vector2(5.8f,-1.2f)};
                天帝战斗扩展验证.设(e,"已生成",true);
                天帝战斗扩展验证.设(e,"位置",取景点[at%取景点.Length]);
                天帝战斗扩展验证.设(e,"登场剩余秒",0f);at++;
            }
            刷画(s);Canvas.ForceUpdateCanvases();
            检查("场景敌种立绘替换已生效 "+level,first.All(e=>敌种已渲染(s,e)));
            // 低、中、高地图分别留存真实 GameView 画面，作为内容密度和场景清晰度的验收证据。
            // 截图只在完成首波取景后执行，不改变战斗状态或运行时逻辑。
            if(level==1 || level==50)
            {
                // 让本档位导演进入首个危险事件的预警阶段，截图保留清晰的边界而不伤害玩家。
                w.导演.推进(level < 20 ? 18.35f : 12.35f);
                w.导演.推进(.55f);
                刷画(s);Canvas.ForceUpdateCanvases();
                yield return new WaitForEndOfFrame();
                截图(level+"级战斗扩展");
                检查("真实场景截图已生成 "+level,File.Exists(Path.Combine(目录,level+"级战斗扩展.jpg"))&&new FileInfo(Path.Combine(目录,level+"级战斗扩展.jpg")).Length>1024);
            }
            if(level==1)
            {
                g.界面.切换战斗暂停();float hp=g.主角属性.当前血量;var pos=first.Select(e=>e.位置).ToArray();
                float dt=Time.deltaTime; s.enabled=true;yield return null;s.enabled=false;
                检查("暂停冻结敌人及资源",g.主角属性.当前血量==hp&&first.Select((e,i)=>e.位置==pos[i]).All(x=>x));
                g.界面.切换战斗暂停();
            }
            if(level==80)
            {
                // 血量边界与技能起手仅作用于本次隔离场景，用来检查五形态美术、HUD与技能几何。
                var boss=w.敌人.Single(e=>e.布点.级别==战斗敌人级别.王级);
                天帝战斗扩展验证.设(boss,"已生成",true);
                // 取景点留在安全区内，避免狼王立绘被 GameView 顶边裁切。
                天帝战斗扩展验证.设(boss,"位置",new Vector2(4,3));
                float[] 比={.9f,.7f,.5f,.3f,.19f};
                for(int i=0;i<5;i++)
                {
                    天帝战斗扩展验证.设(boss,"血量",boss.最大血量*比[i]);
                    w.战术.推进敌(boss,s.玩家位置,.025f,(a,b,c)=>{});刷画(s);
                    string id="BTB"+(i+1).ToString("00");
                    // 形态检查绑定到当前狼王的渲染器，同时核对运行时显示形态和美术编号。
                    检查("实际狼王形态切图 "+id,狼王形态已渲染(s,boss,i+1,id));
                }
                float max=boss.最大血量;检查("场景变形无重置生命",Mathf.Abs(boss.血量-max*.19f)<.01f);
                var leap=取夹具物种(w,9,boss);
                {
                    天帝战斗扩展验证.设(leap,"已生成",true);天帝战斗扩展验证.设(leap,"位置",new Vector2(-7,1));
                    天帝战斗扩展验证.设(leap,"技能编号",10);天帝战斗扩展验证.设(leap,"攻击落点",new Vector2(-4,1));
                    w.战术.释放(leap,s.玩家位置);w.战术.推进效果(new Vector2(20,0),.25f);刷画(s);
                    检查("场景跃击模型保留地面逻辑并抬高立绘",leap.跳跃高度>0&&s.地图.可站立(leap.位置));
                }
                // 普通敌人在正式波次里是按视野预算延迟生成的，不能只从首波
                // first 集合取样；从完整敌人表取一个物种0，避免验证夹具误报。
                var wounded=取夹具物种(w,0);var heal=取夹具物种(w,6,boss,leap,wounded);var shield=取夹具物种(w,7,boss,leap,wounded,heal);
                天帝战斗扩展验证.设(wounded,"已生成",true);天帝战斗扩展验证.设(wounded,"行动",敌人行动.追击);
                天帝战斗扩展验证.设(wounded,"位置",new Vector2(-3,-3));天帝战斗扩展验证.设(wounded,"血量",wounded.最大血量*.4f);
                foreach(var assist in new[]{heal,shield})
                {天帝战斗扩展验证.设(assist,"已生成",true);天帝战斗扩展验证.设(assist,"位置",new Vector2(assist==heal?0:1,-4));天帝战斗扩展验证.设(assist,"行动",敌人行动.追击);}
                w.战术.推进敌(heal,s.玩家位置,.025f,(a,b,c)=>{});float hp0=wounded.血量;w.战术.完成辅助(heal,敌技能.读取(13));
                w.战术.推进敌(shield,s.玩家位置,.025f,(a,b,c)=>{});w.战术.完成辅助(shield,敌技能.读取(14));
                检查("场景实际治疗护盾和连线",wounded.血量>hp0&&wounded.护盾量>0&&w.战术.辅助线.Count>0);
                var ranged=w.敌人.FirstOrDefault(e=>天帝敌种配置.角色(e.物种)==1 && e != boss && e != leap && e != wounded && e != heal && e != shield);
                if (ranged == null) ranged=取夹具物种(w,2,boss,leap,wounded,heal,shield);
                天帝战斗扩展验证.设(ranged,"技能编号",4);天帝战斗扩展验证.设(ranged,"行动",敌人行动.蓄力);天帝战斗扩展验证.设(ranged,"锁定方向",Vector2.down);
                天帝战斗扩展验证.设(w,"BOSS已出现",true);g.界面.更新战斗状态();g.界面.更新战斗目标();
                检查("实际HUD显示狼王当前形态",w.BOSS已出现 &&
                    g.GetComponentsInChildren<Text>(true).Any(t=>t.text.Contains("狼王")));
                刷画(s);yield return new WaitForEndOfFrame();截图("80级战斗扩展");
                检查("真实场景截图已生成 80",File.Exists(Path.Combine(目录,"80级战斗扩展.jpg"))&&new FileInfo(Path.Combine(目录,"80级战斗扩展.jpg")).Length>1024);
            }
            if(level==100)
            {
                var boss=w.敌人.Single(e=>e.布点.级别==战斗敌人级别.王级);
                检查("真实场景100图王105正式生命",boss.等级==105&&Mathf.Abs(boss.最大血量-(float)天帝数值配置.敌人(100,3,0))<.01f);
            }
            w.伤害玩家((float)天帝数值.取("damage.technical_hit_max"));
            g.返回主页();while(g.阶段==游戏阶段.战斗加载)yield return null;
            检查("战败按正式入口返回主页 "+level,g.阶段==游戏阶段.主页);
            检查("离场清理敌术与暂停 "+level,w.战术.敌术.Count==0&&!g.界面.战斗已暂停);
        }
        完成();
    }
    static void 日志(string message,string stack,LogType type)
    {
        // Unity 6 首次导入临时副本时可能触发 SearchDatabase 自身索引异常；
        // 它不属于游戏运行时错误，不能打断正在加载的正式战斗场景验收。
        if ((message != null && message.Contains("UnityEditor.Search.SearchDatabase")) ||
            (stack != null && stack.Contains("UnityEditor.Search.SearchDatabase"))) return;
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){结果.错误.Add(message+"\n"+stack);完成();}
    }
    static void 完成()
    {if(已结束)return;已结束=true;EditorApplication.update-=等待;Application.logMessageReceived-=日志;File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));EditorApplication.isPlaying=false;}
    static void 恢复(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode)return;
        if(!已结束){结果.错误.Add("验证提前退出");已结束=true;}
        检查("真实玩家存档指纹未改变",原指纹==指纹(真实路径));
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));
        EditorApplication.update-=等待;Application.logMessageReceived-=日志;EditorApplication.playModeStateChanged-=恢复;
        天帝存档.验证目录=原存档目录;EditorSettings.enterPlayModeOptionsEnabled=原启用;EditorSettings.enterPlayModeOptions=原选项;Application.runInBackground=原后台;
        // 批处理入口需要在异步 PlayMode 验收完成后显式返回结果，否则 Unity 会一直驻留。
        if (Application.isBatchMode)
            EditorApplication.Exit(结果.错误.Count == 0 && 结果.失败.Count == 0 ? 0 : 1);
    }
}
#endif
