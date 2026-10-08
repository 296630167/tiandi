#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 同步执行在独立隐形Canvas上，不切场景、启停Play或触碰玩家存档。
public static class 天帝移动适配验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    public static string 运行()
    {
        var r = new 报告(); GameObject host = null, 假游戏物 = null; 天帝主角属性 人 = null;
        bool? 原移动 = 天帝移动适配.验证移动平台; float? 原密度 = 天帝移动适配.验证密度;
        var 原屏幕 = 天帝移动适配.验证屏幕尺寸; var 原安全区 = 天帝移动适配.验证安全区;
        Application.LogCallback 日志 = (消息,栈,类型)=>{if(类型==LogType.Error || 类型==LogType.Exception || 类型==LogType.Assert) r.错误.Add(消息+"\n"+栈);};
        Application.logMessageReceived += 日志;
        void 查(string 名, bool 对) => (对 ? r.通过 : r.失败).Add(名);
        void 设(object 目标, string 名, object 值) => 目标.GetType().GetProperty(名)?.SetValue(目标, 值);
        try
        {
            天帝移动适配.验证移动平台 = true; 天帝移动适配.验证密度 = 2.5f;
            var 字体 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>()?.默认字体 ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var 尺寸 in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(2400,1080), new Vector2Int(2778,1284), new Vector2Int(2048,1536) })
            {
                foreach (var 区 in new[] { new Rect(0,0,尺寸.x,尺寸.y), new Rect(80,24,尺寸.x-144,尺寸.y-48), Rect.zero, new Rect(-100,-100,尺寸.x+200,尺寸.y+200) })
                { var 安 = 天帝移动适配.有效安全区(区, 尺寸); 查("安全区合法 " + 尺寸 + " " + 区, 安.width>0 && 安.height>0 && 安.xMin>=0 && 安.yMin>=0 && 安.xMax<=尺寸.x && 安.yMax<=尺寸.y); }
            }
            host = new GameObject("独立触屏验证", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            host.hideFlags = HideFlags.HideAndDontSave; host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            host.GetComponent<CanvasGroup>().alpha = 0; host.GetComponent<CanvasGroup>().blocksRaycasts = false;
            RectTransform 页根(string 名)
            {
                var 根 = new GameObject(名,typeof(RectTransform)).GetComponent<RectTransform>(); 根.SetParent(host.transform,false);
                根.anchorMin=根.anchorMax=new Vector2(.5f,.5f); 根.pivot=new Vector2(0,1); 根.sizeDelta=new Vector2(1600,900); 根.anchoredPosition=new Vector2(-800,450); return 根;
            }
            var 网 = new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人)); 网.设置玩家等级(10);
            var 纹 = 天帝道纹生成.创建(100,道纹分类.属性,道纹品阶.普通,new System.Random(42)); 网.获得道纹(纹);
            var 页 = 页根("触屏道纹").gameObject.AddComponent<天帝道纹界面>(); 页.初始化(网,字体,()=>{}); Canvas.ForceUpdateCanvases();
            var 输入 = 页.画布.GetComponent<天帝道纹输入>();
            PointerEventData 事件(Vector2 点,int id=101,int 连点=1)=>new PointerEventData(EventSystem.current){position=点,pointerId=id,button=PointerEventData.InputButton.Left,clickCount=连点};
            void 点(天帝道纹输入 对象,Vector2 位置,int id=101,int 连点=1)
            { var e=事件(位置,id,连点); ExecuteEvents.Execute(对象.gameObject,e,ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(对象.gameObject,e,ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(对象.gameObject,e,ExecuteEvents.pointerClickHandler); }
            void 键(string 名)=>页.GetComponentsInChildren<Button>(true).Single(x=>x.name==名).onClick.Invoke();
            var 卡 = 页.GetComponentsInChildren<天帝道纹输入>().Single(x=>x.道纹==纹);
            点(卡,页.候选屏幕位置(0)); 查("点选候选道纹",页.触屏选中道纹==纹 && 页.浮窗显示);
            int 接口=纹.接口, 点数=网.技能点; string 原库存=JsonUtility.ToJson(网.导出存档());
            键("旋转60°"); 查("待放旋转不改库存",纹.接口==接口 && 原库存==JsonUtility.ToJson(网.导出存档()) && 页.触屏待放接口==天帝道纹.顺时针接口(接口));
            var 格=new Vector2Int(1,0); 点(输入,页.格屏幕位置(格));
            查("触屏解锁只扣一点不提前放置",网.格已解锁(格) && 网.技能点==点数-1 && !纹.格子.HasValue);
            点(输入,页.格屏幕位置(格)); 查("再次点格放置并提交朝向",纹.格子==格 && 纹.接口==天帝道纹.顺时针接口(接口));
            点(输入,页.格屏幕位置(格),101,2); 查("手机双击不误卸下",纹.格子==格);
            接口=纹.接口; 键("旋转60°"); 查("已放道纹旋转按钮生效",纹.接口==天帝道纹.顺时针接口(接口));
            查("触屏旋转可撤销",页.撤销上一步() && 纹.接口==接口);
            点(输入,页.格屏幕位置(Vector2Int.zero)); 查("中心天赋只看详情不能选为操作目标",页.触屏选中道纹==null && 页.浮窗显示 && !页.触屏旋转() && !页.触屏卸下());
            typeof(天帝道纹界面).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(页,null);
            查("中心保护提示可持续阅读",页.GetComponentsInChildren<Text>().Any(x=>x.text=="中心天赋不可移动、旋转或卸下"));
            点(输入,页.格屏幕位置(格));
            var p=页.格屏幕位置(new Vector2Int(2,0)); var a=事件(p+new Vector2(-50,0),301); var b=事件(p+new Vector2(50,0),302);
            string 原布局=JsonUtility.ToJson(网.导出存档()); float 原缩放=页.画布.缩放;
            输入.OnPointerDown(a); 输入.OnBeginDrag(a); 输入.OnPointerDown(b); b.position+=new Vector2(70,0); 输入.OnDrag(b);
            查("双指缩放放大画布",页.画布.缩放>原缩放); 输入.OnPointerUp(b); 输入.OnPointerClick(b); 输入.OnPointerUp(a); 输入.OnPointerClick(a);
            查("双指释放不解锁不改布局",原布局==JsonUtility.ToJson(网.导出存档()));
            var 平移=页.画布.平移; var 拖=事件(页.格屏幕位置(格),401); 输入.OnPointerDown(拖); 输入.OnBeginDrag(拖); 拖.delta=new Vector2(50,30); 拖.position+=拖.delta; 输入.OnDrag(拖); 输入.OnPointerUp(拖); 输入.OnEndDrag(拖);
            查("画布单指拖动只平移不搬走道纹",页.画布.平移!=平移 && 纹.格子==格);
            for(int i=0;i<30;i++)页.触屏缩放(.8f);查("缩小有下限",Math.Abs(页.画布.缩放-.08f)<.001);
            for(int i=0;i<40;i++)页.触屏缩放(1.25f);查("放大有上限",Math.Abs(页.画布.缩放-1.65f)<.001);
            点(输入,页.格屏幕位置(格)); 键("卸下道纹"); 查("触屏卸下保留同一库存实例",!纹.格子.HasValue && 网.道纹.Contains(纹));
            查("触屏卸下可撤销",页.撤销上一步() && 网.道纹.Single(x=>x.编号==纹.编号).格子==格);
            纹=网.道纹.Single(x=>x.编号==纹.编号); 页.触屏选择(纹,页.格屏幕位置(格)); 页.取消触屏选择(); 查("取消选择清空浮窗和待放朝向",页.触屏选中道纹==null && !页.浮窗显示 && !页.触屏待放接口.HasValue);
            页.定位格子(new Vector2Int(2,0)); // 前面极限缩放/平移后，将本次点击目标移回真实可编辑区。
            点(输入,页.格屏幕位置(new Vector2Int(2,0)));typeof(天帝道纹界面).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(页,null);
            查("无选择解锁结果提示可持续阅读",页.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("格子已解锁")&&x.text.Contains("消耗1技能点")));

            int 背包选择次数=0;var 背包=页根("触屏背包").gameObject.AddComponent<天帝道纹背包>();背包.初始化(网,字体,纹,x=>背包选择次数++,()=>{});
            var 锁定卡=背包.GetComponentsInChildren<天帝背包悬停>().Single(x=>背包.显示项(x.格序)==纹);var 背包事件=事件(RectTransformUtility.WorldToScreenPoint(null,锁定卡.transform.position));
            锁定卡.OnPointerEnter(背包事件);查("触屏背包不依赖悬停",!背包.详情显示);
            ExecuteEvents.Execute(锁定卡.gameObject,背包事件,ExecuteEvents.pointerClickHandler);查("触屏锁定道纹可点开详情且不重新选择",背包.详情显示&&背包选择次数==0);
            锁定卡.OnPointerExit(背包事件);查("背包详情抬指保留",背包.详情显示);背包.隐藏详情();

            var 摇区=页根("摇杆");摇区.sizeDelta=new Vector2(300,300);var 摇=摇区.gameObject.AddComponent<天帝移动摇杆>(); Canvas.ForceUpdateCanvases();
            Vector2 摇点(Vector2 偏)=>RectTransformUtility.WorldToScreenPoint(null,摇区.TransformPoint(摇区.rect.center+偏));
            var 指=事件(摇点(new Vector2(100,0)),501);摇.OnPointerDown(指);查("摇杆实际尺寸驱动方向",摇.方向.x>.5f && Math.Abs(摇.方向.y)<.01f);
            var 第二指=事件(摇点(new Vector2(0,100)),502);摇.OnPointerDown(第二指);摇.OnPointerUp(第二指);查("第二根手指不抢摇杆也不松开第一指",摇.方向.x>.5f);
            摇.OnPointerUp(指);查("抬起控制指即停止",摇.方向==Vector2.zero);
            指.position=摇点(new Vector2(3,0));摇.OnPointerDown(指);查("摇杆中心死区不抖动",摇.方向==Vector2.zero);摇.OnPointerUp(指);
            指.position=摇点(new Vector2(300,0));摇.OnPointerDown(指);查("摇杆边缘限幅",摇.方向.magnitude<=1.001f);摇.重置输入();查("后台重置摇杆",摇.方向==Vector2.zero);

            人=new 天帝主角属性(天帝普攻.主角配置(),网);var 角色=页根("触屏角色").gameObject.AddComponent<天帝角色界面>();角色.初始化(人,网,字体,()=>{});
            var 属性=角色.GetComponentsInChildren<天帝角色属性悬停>().First();var 属性事件=事件(RectTransformUtility.WorldToScreenPoint(null,属性.transform.position));
            属性.OnPointerEnter(属性事件);查("触屏触入不弹悬停说明",!角色.GetComponentsInChildren<RectTransform>(true).Any(x=>x.name=="角色属性说明浮窗"&&x.gameObject.activeSelf));
            属性.OnPointerClick(属性事件);查("属性第一次点开简短介绍",角色.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("再次点此属性")));
            属性.OnPointerExit(属性事件);查("触屏抬起保留属性介绍",角色.GetComponentsInChildren<RectTransform>().Any(x=>x.name=="角色属性说明浮窗"));
            属性.OnPointerClick(属性事件);查("第二次点开完整公式",角色.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("公式")&&!x.text.Contains("再次点此属性")));
            角色.GetComponentsInChildren<Button>().Single(x=>x.name=="关闭属性公式").onClick.Invoke();查("手机可关闭属性介绍",!角色.GetComponentsInChildren<RectTransform>().Any(x=>x.name=="角色属性说明浮窗"));

            假游戏物=new GameObject("独立移动页面模型");假游戏物.SetActive(false);假游戏物.hideFlags=HideFlags.HideAndDontSave;
            var g=假游戏物.AddComponent<天帝游戏>();g.默认字体=字体;设(g,"阶段",游戏阶段.道纹);typeof(天帝游戏).GetField("原监听音量",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,AudioListener.volume);
            var ui=new 天帝界面(g); var ui根=(GameObject)typeof(天帝界面).GetField("根",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui);ui根.transform.SetParent(host.transform,false);设(g,"界面",ui);
            ui.显示道纹(网);Canvas.ForceUpdateCanvases();ui.更新适配();
            var 设计=ui根.transform.Find("安全区/设计区") as RectTransform;
            查("设计区固定16比9逻辑尺寸",Vector2.Distance(设计.rect.size,天帝移动适配.布局尺寸)<.1f);
            var 屏幕=天帝移动适配.屏幕尺寸;var 有效区=天帝移动适配.有效安全区(天帝移动适配.安全区,屏幕);
            float 安全比例=天帝移动适配.显示比例(有效区)/天帝移动适配.显示比例(new Rect(0,0,屏幕.x,屏幕.y));
            查("设计区以1920比1080基准适配可用安全区",Vector2.Distance(Vector2.Scale(设计.rect.size,设计.localScale),(Vector2)天帝移动适配.固定分辨率*安全比例)<.1f);
            查("已移除整页阅读缩放工具",!ui根.GetComponentsInChildren<Transform>(true).Any(x=>x.name=="触屏阅读工具"));

            // 检查真实HUD生成函数，测试屏幕只作用于本隐形画布，不调整GameView。
            var 场=假游戏物.AddComponent<天帝战斗场景>();var 地图=new 天帝战斗地图(42,true);var 钱=new 天帝通货(网,42);var 盒=new 天帝宝盒(网,42,500);
            var 战=new 天帝战斗系统(地图,网,人,战斗难度.普通,钱,1,null,盒);设(g,"天赋池",天帝天赋池.从已选天赋恢复((int)天赋种类.普通人));
            设(场,"地图",地图);设(场,"战斗",战);设(g,"战斗场景",场);设(g,"主角属性",人);设(g,"道纹数据",网);设(g,"通货数据",钱);设(g,"宝盒数据",盒);设(g,"阶段",游戏阶段.战斗);
            ui.显示战斗(); var canvas=ui根.GetComponent<Canvas>();ui根.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;canvas.scaleFactor=1;
            foreach (var 尺寸 in new[] {new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2400,1080),new Vector2Int(2778,1284),new Vector2Int(2048,1536)})
            {
                天帝移动适配.验证屏幕尺寸=尺寸;天帝移动适配.验证安全区=new Rect(60,24,尺寸.x-100,尺寸.y-48);
                天帝移动适配.验证密度=尺寸.y/420f;((RectTransform)ui根.transform).sizeDelta=(Vector2)尺寸/天帝移动适配.显示比例(天帝移动适配.有效安全区(天帝移动适配.安全区,尺寸));
                ui.更新适配();Canvas.ForceUpdateCanvases();ui.更新适配();var 安=ui根.transform.Find("安全区") as RectTransform;var hud=安.Find("战斗HUD") as RectTransform;
                Rect 框(RectTransform x){var a=安.InverseTransformPoint(x.TransformPoint(x.rect.min));var b=安.InverseTransformPoint(x.TransformPoint(x.rect.max));return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}
                foreach(string 名 in new[]{"主角战斗状态","波次敌人信息","移动摇杆","暂停入口","跑步入口","离开入口","战斗小地图底","拾取提示列表","战斗目标"})
                {var 区=hud.Find(名) as RectTransform;var 边=框(区);查("HUD安全区内 "+尺寸+" "+名,边.xMin>=安.rect.xMin-.1 && 边.xMax<=安.rect.xMax+.1 && 边.yMin>=安.rect.yMin-.1 && 边.yMax<=安.rect.yMax+.1);}
                foreach(string 名 in new[]{"暂停入口","跑步入口","离开入口"})
                {var 区=hud.Find(名) as RectTransform;查("战斗按钮至少44逻辑像素 "+尺寸+" "+名,区.rect.height>=43.9f);}
                查("左右触控区无重叠 "+尺寸,!框(hud.Find("移动摇杆") as RectTransform).Overlaps(框(hud.Find("跑步入口") as RectTransform)));
                ui.切换战斗暂停();ui.更新适配();Canvas.ForceUpdateCanvases();
                查("手机暂停详情使用百分比布局 "+尺寸,ui根.GetComponentsInChildren<RectTransform>().Any(x=>x.name=="暂停详情" && x.anchorMin!=x.anchorMax && x.localScale==Vector3.one));
                ui.关闭战斗暂停();
            }
            ui.战斗摇杆.OnPointerDown(事件(RectTransformUtility.WorldToScreenPoint(null,ui.战斗摇杆.transform.position)+new Vector2(200,0),701));
            ui.移动失去焦点();查("后台暂停战斗且清空摇杆跑步",ui.战斗已暂停 && ui.战斗摇杆.方向==Vector2.zero && !ui.触控跑步);ui.移动失去焦点();查("重复后台事件不重复解除暂停",ui.战斗已暂停);ui.关闭战斗暂停();
            foreach(var 热 in ui根.GetComponentsInChildren<天帝触控热区>(true))热.更新();
            foreach(string 名 in new[]{"暂停","跑步：关","返回主页"})
                查("手机按钮加入触控热区 "+名,ui根.GetComponentsInChildren<Button>(true).Any(x=>x.name==名 && x.GetComponent<天帝触控热区>()!=null));
            // 正常战斗中返回键按设计隐藏；暂停后验证实际可见的返回入口。
            ui.切换战斗暂停();ui.更新适配();Canvas.ForceUpdateCanvases();
            查("手机暂停返回入口可见且有热区",ui根.GetComponentsInChildren<Button>().Any(x=>x.name=="返回主页" && x.GetComponent<天帝触控热区>()!=null));
            ui.关闭战斗暂停();

            var 热区根=页根("热区相邻验证");
            Button 热键(string 名,float x){var 区=new GameObject(名,typeof(RectTransform),typeof(Image),typeof(Button)).GetComponent<RectTransform>();区.SetParent(热区根,false);区.anchorMin=区.anchorMax=区.pivot=new Vector2(0,1);区.sizeDelta=new Vector2(20,20);区.anchoredPosition=new Vector2(x,-20);var 键=区.GetComponent<Button>();键.targetGraphic=区.GetComponent<Image>();天帝界面美术.按钮(键);return 键;}
            var 左键=热键("左热键",0);var 右键=热键("右热键",30);Canvas.ForceUpdateCanvases();左键.GetComponent<天帝触控热区>().更新();右键.GetComponent<天帝触控热区>().更新();
            var 左垫=左键.targetGraphic.raycastPadding;var 右垫=右键.targetGraphic.raycastPadding;
            查("小按钮空白方向扩展触控范围",左垫.x<0&&左垫.y<0&&右垫.z<0);
            查("相邻按钮扩展不越过间距中线",-左垫.z<=5.01f&&-右垫.x<=5.01f);

            天帝移动适配.验证移动平台=false;var pc=页根("PC道纹").gameObject.AddComponent<天帝道纹界面>();pc.初始化(网,字体,()=>{});查("PC没有触屏操作条",!pc.GetComponentsInChildren<Button>(true).Any(x=>x.name=="旋转60°"));
            锁定卡.OnPointerEnter(背包事件);查("PC背包悬停显示详情",背包.详情显示);锁定卡.OnPointerExit(背包事件);查("PC背包移开隐藏详情",!背包.详情显示);
        }
        catch(Exception ex){r.错误.Add(ex.ToString());}
        finally
        {
            天帝移动适配.验证移动平台=原移动;天帝移动适配.验证密度=原密度;
            天帝移动适配.验证屏幕尺寸=原屏幕;天帝移动适配.验证安全区=原安全区;
            if(host!=null)UnityEngine.Object.DestroyImmediate(host); if(假游戏物!=null)UnityEngine.Object.DestroyImmediate(假游戏物);人?.Dispose();
            Application.logMessageReceived -= 日志;
        }
        string dir=Path.Combine(天帝构建工具.项目根,"生成/验证/移动适配-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(r,true));return dir;
    }
}
#endif
