#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class 天帝作弊码验证
{
    static 天帝真实数值验证.报告 结果;
    static string 目录,原目录,真实路径,原指纹;
    static bool 原启用,原后台,已启动,已结束;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static void 检查(string n,bool b)=>(b?结果.通过:结果.失败).Add(n);
    static string 指纹(string p){if(!File.Exists(p))return "无文件";using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(File.ReadAllBytes(p)));}
    static 天帝道纹 网()=>new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
    static 天帝存档数据 数据(天帝道纹 w,天帝宝盒 b,天帝通货 c=null)=>new 天帝存档数据{序章已完成=true,主角=天帝普攻.主角配置(),画布=w.导出存档(),通货=(c??new 天帝通货(w,42)).导出库存(),无限通货=c?.无限通货??false,灵石=b.灵石,无限灵石=b.无限灵石};
    // 只检查独立模型及隔离文件，不进入Play或改变当前场景。
    public static string 验证模型()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("请在编辑模式检查独立模型。");
        结果=new 天帝真实数值验证.报告();目录=Path.Combine(天帝构建工具.项目根,"生成/验证/无限通货-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(目录);
        string real=Path.Combine(Application.persistentDataPath,"天帝进度.json"),hash=指纹(real);
        try{模型();无限通货模型();}catch(Exception ex){结果.错误.Add(ex.ToString());}
        检查("独立模型检查未改写真实存档",hash==指纹(real));
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));return 目录;
    }
    public static string 启动()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("需编辑模式且无未保存场景。");
        if(SceneManager.GetActiveScene().path!="Assets/天帝/场景/天帝.unity")throw new InvalidOperationException("在现有主场景执行检查。");
        结果=new 天帝真实数值验证.报告();目录=Path.Combine(天帝构建工具.项目根,"生成/验证/作弊码-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(目录);
        try{模型();}catch(Exception ex){结果.错误.Add(ex.ToString());File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));return 目录;}
        真实路径=Path.Combine(Application.persistentDataPath,"天帝进度.json");原指纹=指纹(真实路径);
        string 隔离=Path.Combine(目录,"隔离存档");var w=网();if(!new 天帝存档(隔离).保存(数据(w,new 天帝宝盒(w,42,500))))throw new InvalidOperationException("隔离档创建失败。");
        原目录=天帝存档.验证目录;天帝存档.验证目录=隔离;
        原启用=EditorSettings.enterPlayModeOptionsEnabled;原选项=EditorSettings.enterPlayModeOptions;原后台=Application.runInBackground;
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;
        已启动=已结束=false;截止=EditorApplication.timeSinceStartup+50;
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.update+=等待;EditorApplication.playModeStateChanged+=恢复;Application.logMessageReceived+=日志;
        EditorApplication.isPlaying=true;return 目录;
    }
    static void 模型()
    {
        var w=网();var b=new 天帝宝盒(w,42,500);var c=new 天帝通货(w,42);int start=w.技能点;
        foreach(string code in new[]{null,"","　","涌","涌现x"})
        {string before=JsonUtility.ToJson(w.导出存档());检查("无效码拒绝且无状态变化 "+code,!天帝作弊码.兑换(code,w,b,c,out _)&&b.灵石==500&&!b.无限灵石&&!c.无限通货&&c.导出库存().All(x=>x==0)&&JsonUtility.ToJson(w.导出存档())==before);}
        w.设置玩家等级(20);w.解锁格子(new Vector2Int(1,0));int point=w.技能点;
        using(var p=new 天帝主角属性(天帝普攻.主角配置(),w))
        {
            检查("正确码含两端空白有效",天帝作弊码.兑换("　涌现 \n",w,b,c,out _)&&c.无限通货);
            检查("满级补齐剩余升级点保留已消费点",w.玩家等级==100&&w.技能点==point+80&&w.技能点==start+98&&w.当前经验==0&&w.已解锁格数==2);
            检查("中心六接口与主角正式成长同步",w.已放置[Vector2Int.zero].接口==63&&p.等级==100&&!p.导出配置().使用测试数据);
            int pts=w.技能点;for(int i=0;i<5;i++)天帝作弊码.兑换("涌现",w,b,c,out _);
            检查("重复兑换不重复发点",pts==w.技能点);
            foreach(宝盒种类 k in Enum.GetValues(typeof(宝盒种类)))
            {
                bool ok=true;for(int i=0;i<20;i++){ok&=b.抽取(k,out var x,out _)&&x.物品等级==100&&b.灵石==int.MaxValue;}
                检查("无限模式连续开盒不扣灵石 "+k,ok);
            }
            检查("无限余额显示文字并接受掉落",b.无限灵石&&b.灵石显示=="无限"&&b.获得灵石(5)&&b.灵石==int.MaxValue&&!b.获得灵石(0));
            var item=w.道纹.First();检查("无限余额回收不溢出不吞请求",b.预览回收(new[]{item},out int price,out _)&&b.回收道纹(new[]{item},price,out _)&&!w.道纹.Contains(item)&&b.灵石==int.MaxValue);
            var save=new 天帝存档(Path.Combine(目录,"模型存档"));检查("作弊状态保存",save.保存(数据(w,b,c)));
            var data=save.读取();var r=天帝道纹.读取存档(data.画布);var money=new 天帝宝盒(r,42,data.灵石,data.无限灵石);
            var restored=new 天帝通货(r,42);restored.读取库存(data.通货,data.无限通货);
            检查("重读满级无限余额通货和技能点",r.玩家等级==100&&r.技能点==pts&&money.无限灵石&&restored.无限通货&&天帝通货.可用种类.All(k=>restored.数量(k)==int.MaxValue)&&money.抽取(宝盒种类.属性,out _,out _)&&money.灵石==int.MaxValue);
        }
        var normal=网();var finite=new 天帝宝盒(normal,42,500);检查("普通开局仍一级有限500灵石",!finite.无限灵石&&normal.玩家等级==1&&finite.抽取(宝盒种类.属性,out _,out _)&&finite.灵石==500-天帝宝盒.价格(宝盒种类.属性));
        var old=数据(normal,finite);string json=JsonUtility.ToJson(old).Replace("\"无限灵石\":false,","").Replace("\"无限通货\":false,","");var legacy=JsonUtility.FromJson<天帝存档数据>(json);天帝存档.迁移(legacy);检查("缺少新字段的普通旧档默认有限",!legacy.无限灵石&&!legacy.无限通货);
    }
    static void 无限通货模型()
    {
        var w=网();var c=new 天帝通货(w,42);var b=new 天帝宝盒(w,42,500);
        检查("缺少通货数据拒绝兑换且不改角色",!天帝作弊码.兑换("涌现",w,b,null,out _)&&!b.无限灵石&&w.玩家等级==1);
        检查("隔离改造目标来自正式宝盒",b.抽取(宝盒种类.属性,out _,out _));
        int events=0;c.数量改变+=()=>events++;c.启用无限通货();c.启用无限通货();
        检查("启用无限通货只通知一次",events==1);
        foreach(var k in 天帝通货.可用种类)
        {
            var def=天帝通货.定义[(int)k];var item=w.道纹.First(x=>x.分类==道纹分类.属性);int[] before=c.导出库存();bool ok=true;
            for(int i=0;i<20;i++)
            {
                typeof(道纹实例).GetProperty("品阶").SetValue(item,def.来源??道纹品阶.完美);
                item.词条.Clear();item.词条.Add(new 道纹词条(道纹属性.力量,2));
                ok&=c.使用(k,item,0,out _)&&c.数量(k)==int.MaxValue&&c.导出库存().SequenceEqual(before);
            }
            检查(k+"零库存连续改造20次不扣量",ok);
            检查(k+"显示无限且接收满整数掉落不溢出",c.数量显示(k)=="无限"&&c.获得(k,int.MaxValue)&&c.获得(k,1)&&c.导出库存().SequenceEqual(before)&&!c.获得(k,0)&&!c.获得(k,-1));
        }
        foreach(var k in new[]{通货种类.通脉针,通货种类.六通玉,(通货种类)99,(通货种类)(-1)})
            检查("无限模式不恢复旧索引或非法通货 "+k,c.数量(k)==0&&!c.获得(k,1)&&!c.使用(k,w.道纹.First(),0,out _));
        检查("无限模式仍保护中心和洗练索引",!c.使用(通货种类.重铸石,w.已放置[Vector2Int.zero],0,out _)&&!c.使用(通货种类.易纹砂,w.道纹.First(x=>x.分类==道纹分类.属性),-1,out _));
        var save=new 天帝存档(Path.Combine(目录,"通货独立存档"));var data=数据(w,b,c);检查("无限通货可独立保存",save.保存(data));
        var read=save.读取();var restored=new 天帝通货(天帝道纹.读取存档(read.画布),42);restored.读取库存(read.通货,read.无限通货);
        检查("退出重读全部通货无限",read.无限通货&&restored.无限通货&&天帝通货.可用种类.All(k=>restored.数量(k)==int.MaxValue)&&restored.导出库存().All(x=>x==0));
        var old=数据(w,b);old.无限灵石=true;string oldjson=JsonUtility.ToJson(old).Replace("\"无限通货\":false,","");
        File.WriteAllText(save.路径,oldjson);string hash=指纹(save.路径);var migrated=save.读取();
        检查("旧版涌现存档自动补无限通货且读取不写原档",migrated!=null&&migrated.无限通货&&hash==指纹(save.路径));
        restored.读取库存(new int[13]);检查("读取普通库存复原有限模式",!restored.无限通货&&天帝通货.可用种类.All(k=>restored.数量(k)==0));
    }
    static void 等待()
    {
        if(已结束)return;if(EditorApplication.timeSinceStartup>截止){结果.错误.Add("检查超时");完成();return;}
        if(已启动||!EditorApplication.isPlaying)return;var g=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if(g!=null&&g.阶段==游戏阶段.标题){已启动=true;g.StartCoroutine(演练(g));}
    }
    static Button 按键(天帝游戏 g,string name)=>g.GetComponentsInChildren<Button>().First(x=>x.name==name&&x.gameObject.activeInHierarchy);
    static IEnumerator 演练(天帝游戏 g)
    {
        var save = (天帝存档)typeof(天帝游戏).GetField("存档",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(g);
        bool isolated = save.路径 == Path.Combine(目录,"隔离存档","天帝进度.json");
        检查("实际游戏保存路径确认隔离",isolated);
        if (!isolated) { 完成(); yield break; }
        检查("正式隔离档继续主页",g.继续游戏());yield return null;
        按键(g,"作弊码").onClick.Invoke();yield return null;检查("主页按钮打开中文输入弹窗",g.界面.作弊码已打开&&g.界面.确认已打开);
        bool 彩绘 = 天帝道纹美术.彩绘皮肤;
        检查("作弊与关闭按钮文字适配皮肤",彩绘 ? 按键(g,"作弊码").GetComponentInChildren<Text>().color==天帝道纹美术.正文&&按键(g,"关闭").GetComponentInChildren<Text>().color==天帝道纹美术.正文 : 按键(g,"作弊码").GetComponentInChildren<Text>().color.r>.8f&&按键(g,"关闭").GetComponentInChildren<Text>().color.r>.8f);
        var input=g.GetComponentsInChildren<InputField>().Single(x=>x.name=="作弊码输入");
        检查("输入允许中文且字体配置",input.contentType==InputField.ContentType.Standard&&input.textComponent.font==g.默认字体);
        检查("输入框默认填好涌现",input.text=="涌现");
        input.text="错误码";按键(g,"启用").onClick.Invoke();检查("实际无效码提示且不变资源",!g.宝盒数据.无限灵石&&g.主角属性.等级==1&&g.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("作弊码无效")));
        Canvas.ForceUpdateCanvases();var hint=g.GetComponentsInChildren<Text>().Single(t=>t.name=="作弊码反馈");检查("失败反馈完整且窗口保留",g.界面.作弊码已打开&&hint.preferredHeight<=hint.rectTransform.rect.height+.1f);
        g.界面.关闭作弊码();yield return null;按键(g,"作弊码").onClick.Invoke();yield return null;
        检查("重开恢复预填作弊码",g.GetComponentsInChildren<InputField>().Single(x=>x.name=="作弊码输入").text=="涌现");
        按键(g,"启用").onClick.Invoke();yield return null;
        检查("实际正确兑换同步100级无限余额和通货",g.宝盒数据.无限灵石&&g.通货数据.无限通货&&g.主角属性.等级==100&&g.道纹数据.技能点==100);
        检查("实际主页显示无限灵石",g.GetComponentsInChildren<Text>().Any(t=>t.text=="灵石  无限"));
        检查("启用成功自动关闭且主页控件可用",!g.界面.作弊码已打开&&!g.界面.确认已打开&&按键(g,"作弊码").interactable&&按键(g,"道纹").interactable);
        int pts=g.道纹数据.技能点;按键(g,"作弊码").onClick.Invoke();yield return null;按键(g,"启用").onClick.Invoke();yield return null;检查("实际重复点击不重复发点且自动关闭",g.道纹数据.技能点==pts&&!g.界面.作弊码已打开);
        按键(g,"作弊码").onClick.Invoke();yield return null;
        yield return new WaitForEndOfFrame();var screenshot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(目录,"作弊码弹窗.jpg"),screenshot.EncodeToJPG(82));UnityEngine.Object.Destroy(screenshot);
        g.界面.关闭确认();yield return null;检查("Esc关闭入口复原主页控件",!g.界面.作弊码已打开&&按键(g,"作弊码").interactable&&按键(g,"道纹").interactable);
        按键(g,"宝盒").onClick.Invoke();yield return null;
        检查("宝盒余额同步显示无限",g.GetComponentsInChildren<Text>().Any(t=>t.text=="灵石  无限"));
        var draw=g.GetComponentsInChildren<Button>().Where(x=>x.name=="抽取").ToArray();int count=g.道纹数据.道纹.Count;
        foreach(var button in draw)button.onClick.Invoke();检查("实际三个宝盒均可抽取且不扣余额",draw.Length==3&&g.道纹数据.道纹.Count==count+3&&g.宝盒数据.灵石==int.MaxValue);
        g.界面.关闭宝盒();g.返回标题();yield return null;检查("重返标题并继续恢复作弊状态",g.继续游戏()&&g.宝盒数据.无限灵石&&g.通货数据.无限通货&&g.主角属性.等级==100&&g.道纹数据.技能点==pts);
        检查("继续后无限开盒生效",g.宝盒数据.抽取(宝盒种类.分叉,out _,out _)&&g.宝盒数据.灵石==int.MaxValue);
        完成();
    }
    static void 日志(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){结果.错误.Add(message+"\n"+stack);完成();}}
    static void 完成(){if(已结束)return;已结束=true;EditorApplication.update-=等待;Application.logMessageReceived-=日志;File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));EditorApplication.isPlaying=false;}
    static void 恢复(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode)return;if(!已结束){结果.错误.Add("检查提前结束");已结束=true;}
        检查("真实存档未改写",原指纹==指纹(真实路径));File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));
        EditorApplication.update-=等待;Application.logMessageReceived-=日志;EditorApplication.playModeStateChanged-=恢复;
        天帝存档.验证目录=原目录;EditorSettings.enterPlayModeOptionsEnabled=原启用;EditorSettings.enterPlayModeOptions=原选项;Application.runInBackground=原后台;
    }
}
#endif
