#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class 天帝剪纸七页验证
{
    [Serializable] sealed class 报告
    { public List<string> 通过=new List<string>(),失败=new List<string>(),错误=new List<string>(); }
    public static void 运行()
    {
        var 结果=new 报告(); string 目录=Path.Combine(天帝构建工具.项目根,"生成/验证/剪纸七页实装_20261007");Directory.CreateDirectory(目录);
        void 查(string 名,bool 对)=>(对?结果.通过:结果.失败).Add(名);
        void 设(object 对象,string 名,object 值)=>对象.GetType().GetProperty(名).SetValue(对象,值);
        var 原移动=天帝移动适配.验证移动平台;var 原屏幕=天帝移动适配.验证屏幕尺寸;var 原安全=天帝移动适配.验证安全区;var 原美术=天帝美术资源.当前;
        GameObject 宿主=null,游戏对象=null;天帝主角属性 人=null;
        Application.LogCallback 日志=(文,栈,类)=>{if(类==LogType.Error||类==LogType.Exception||类==LogType.Assert)结果.错误.Add(文+"\n"+栈);};Application.logMessageReceived+=日志;
        try
        {
            foreach(var f in Directory.GetFiles("Assets/天帝/Resources/剪纸界面","*.png"))AssetDatabase.ImportAsset(f.Replace('\\','/'),ImportAssetOptions.ForceUpdate);
            var 美术=AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");typeof(天帝美术资源).GetProperty("当前").SetValue(null,美术);
            var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));网.设置玩家等级(12);
            for(int i=0;i<24;i++)网.获得道纹(天帝道纹生成.创建(i+100,道纹分类.属性,(道纹品阶)(i%8),new System.Random(42+i)));
            var 盒=new 天帝宝盒(网,42,1280);var 钱=new 天帝通货(网,42);人=new 天帝主角属性(天帝普攻.主角配置(),网);
            游戏对象=new GameObject("剪纸七页隔离模型");游戏对象.SetActive(false);游戏对象.hideFlags=HideFlags.HideAndDontSave;
            var 游戏=游戏对象.AddComponent<天帝游戏>();游戏.默认字体=AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf");游戏.美术=美术;
            设(游戏,"阶段",游戏阶段.主页);设(游戏,"主角属性",人);设(游戏,"道纹数据",网);设(游戏,"宝盒数据",盒);设(游戏,"通货数据",钱);
            设(游戏,"当前地图等级",12);设(游戏,"初始源道纹编号",0);设(游戏,"天赋池",天帝天赋池.从已选天赋恢复((int)天赋种类.普通人));
            查("剪纸页面与新标题素材可用",天帝剪纸界面皮肤.已启用&&天帝剪纸界面皮肤.素材("轻纸框")!=null&&天帝剪纸界面皮肤.素材("标题山水背景")!=null);
            foreach(bool 手机 in new[]{false,true})
            {
                string 前=手机?"移动":"PC";天帝移动适配.验证移动平台=手机;天帝移动适配.验证屏幕尺寸=new Vector2Int(1920,1080);天帝移动适配.验证安全区=new Rect(0,0,1920,1080);
                var 界面=new 天帝界面(游戏);设(游戏,"界面",界面);
                宿主=(GameObject)typeof(天帝界面).GetField("根",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(界面);
                宿主.transform.SetParent(null,false);宿主.hideFlags=HideFlags.HideAndDontSave;((RectTransform)宿主.transform).sizeDelta=new Vector2(1920,1080);
                宿主.GetComponent<CanvasScaler>().enabled=false;宿主.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;宿主.AddComponent<CanvasGroup>().alpha=0;
                Button 键(string 名)=>宿主.GetComponentsInChildren<Button>().First(b=>b.name==名);
                void 排()
                {
                    foreach(var s in 宿主.GetComponentsInChildren<Slider>(true))s.onValueChanged.RemoveAllListeners();
                    // 编辑模式的Toggle Rebuild会重发回调；与既有双端夹具一致，排版期间不执行业务刷新。
                    foreach(var t in 宿主.GetComponentsInChildren<Toggle>(true))t.onValueChanged.RemoveAllListeners();
                    界面.更新适配();Canvas.ForceUpdateCanvases();界面.更新适配();Canvas.ForceUpdateCanvases();
                    foreach(var c in 宿主.GetComponentsInChildren<天帝道纹详情卡>())typeof(天帝道纹详情卡).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
                    foreach(var c in 宿主.GetComponentsInChildren<天帝按钮文字区域>())c.更新();Canvas.ForceUpdateCanvases();
                    foreach(var t in 宿主.GetComponentsInChildren<Toggle>(true))if(t.graphic!=null)t.graphic.gameObject.SetActive(t.isOn);
                }
                void 拍(string 名){排();var 原=天帝青绿验收.图片目录;天帝青绿验收.图片目录=目录;try{天帝青绿验收.拍摄(宿主,前+"_"+名,1920,1080);}finally{天帝青绿验收.图片目录=原;}}
                void 主页(){设(游戏,"阶段",游戏阶段.主页);界面.显示主页();排();}
                主页();界面.显示角色();查(前+"角色页面打开",界面.角色已打开);拍("01角色");
                foreach(string 页 in new[]{"角色属性","战斗进阶","技能与形态","道纹分布"}){键("页签-"+页).onClick.Invoke();排();查(前+"角色页签可切换"+页,宿主.GetComponentsInChildren<Text>().Any(t=>t.name.StartsWith("数值-")||t.name.Contains("标题")));}
                界面.关闭角色();查(前+"角色关闭回主页",!界面.角色已打开);
                主页();游戏.打开道纹();拍("02道纹构筑");查(前+"画布拖放输入保留",界面.道纹页.画布.GetComponent<天帝道纹输入>()!=null&&界面.道纹页.画布.轻透画布);
                查(前+"六方向切换保留",宿主.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("通路"))==6);
                var 预览框=宿主.GetComponentsInChildren<RectTransform>().First(r=>r.name=="实时构筑预览");
                if(!手机)
                {
                    var 内框=预览框.rect;内框.xMin+=14;内框.xMax-=14;内框.yMin+=8;内框.yMax-=8;
                    查("PC六路按钮四角全部在纸框内边距内",宿主.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("通路")).All(b=>{
                        var 四角=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(四角);
                        return 四角.All(p=>内框.Contains(预览框.InverseTransformPoint(p)));}));
                }
                查(前+"概念图工作区与真实网格同区域",界面.道纹页.画布.可见格数>0&&(手机||界面.道纹页.画布.山水工作区&&宿主.GetComponentsInChildren<Image>().Any(i=>i.name=="山水战斗变化框"||i.name=="山水构筑标题")));
                主页();游戏.打开道纹改造();var 改造=界面.改造页;查(前+"十一种材料全部保留",宿主.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("通货-"))==11);
                改造.选目标(网.道纹.FindIndex(t=>t.分类==道纹分类.属性));改造.选通货(通货种类.易纹砂);拍("03道纹改造");
                查(前+"改造目标选择模型一致",改造.当前目标!=null);改造.打开背包();查(前+"目标背包可打开",改造.背包已打开);改造.关闭背包();
                主页();键("道纹回收").onClick.Invoke();查(前+"回收页面打开",界面.回收已打开);拍("04道纹回收");
                var 余额=宿主.GetComponentsInChildren<Text>().First(t=>t.name=="回收灵石余额");var 关闭=(RectTransform)键("关闭回收").transform;
                查(前+"回收余额和关闭中心对齐",Mathf.Abs(余额.rectTransform.TransformPoint(余额.rectTransform.rect.center).y-关闭.TransformPoint(关闭.rect.center).y)<.2f);
                查(前+"回收分页保留库存",键("回收下一页").interactable);
                键("一键选中").onClick.Invoke();查(前+"回收批选与正式价格可用",界面.回收页.选中数量>0&&界面.回收页.总回收灵石>0);
                键("预览回收").onClick.Invoke();查(前+"回收必须先确认",界面.回收页.确认已打开);界面.回收页.关闭确认();
                查(前+"回收取消没有修改余额",盒.灵石==1280);界面.关闭回收();
                主页();界面.显示图鉴();查(前+"图鉴所有八类可定位",new[]{"天赋道纹","基础属性","普通属性","功能道纹","五行元素","分叉道纹","特性道纹","转化道纹"}.All(n=>界面.图鉴页.定位分类(n)));
                界面.图鉴页.定位分类("功能道纹");界面.图鉴页.设置查询物品等级(12);拍("05道纹图鉴");
                if(!手机){查("PC图鉴九宫格显示",宿主.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("剪纸图鉴条目-"))==9);键("剪纸图鉴下一页").onClick.Invoke();查("PC图鉴下一页切换",宿主.GetComponentsInChildren<Text>().Any(t=>t.name=="剪纸图鉴页码"&&t.text=="2 / 5"));键("查看共通规则").onClick.Invoke();查("PC图鉴规则说明仍可查看",宿主.GetComponentsInChildren<Text>().Any(t=>t.name=="剪纸详情标题"&&t.text=="品阶与连接规则"));}
                界面.关闭图鉴();主页();界面.显示宝盒();拍("06宝盒");查(前+"三类宝盒真实概率入口",宿主.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("宝盒概率-"))==3);
                键("宝盒概率-0").onClick.Invoke();查(前+"宝盒概率可打开",宿主.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("概率")));界面.关闭宝盒();
                主页();界面.显示设置();拍("07设置");查(前+"四项音量和字幕保留",宿主.GetComponentsInChildren<Slider>().Length==4&&宿主.GetComponentsInChildren<Toggle>().Length==1);
                查(前+"设置关闭重看返回入口保留",new[]{"关闭","重看序章","返回标题"}.All(n=>宿主.GetComponentsInChildren<Button>().Any(b=>b.name==n)));
                界面.显示标题();拍("08标题版本居中");
                var 版本=宿主.GetComponentsInChildren<Text>().First(t=>t.name=="实际版本");
                var 签=宿主.GetComponentsInChildren<RectTransform>().First(r=>r.name=="版本纸签");
                查(前+"版本文字相对纸签水平垂直居中",版本.alignment==TextAnchor.MiddleCenter&&Vector3.Distance(版本.rectTransform.TransformPoint(版本.rectTransform.rect.center),签.TransformPoint(签.rect.center))<.2f);
                UnityEngine.Object.DestroyImmediate(宿主);宿主=null;
            }
        }
        catch(Exception e){结果.错误.Add(e.ToString());}
        finally
        {
            Application.logMessageReceived-=日志;if(人!=null)人.Dispose();if(宿主!=null)UnityEngine.Object.DestroyImmediate(宿主);if(游戏对象!=null)UnityEngine.Object.DestroyImmediate(游戏对象);
            天帝移动适配.验证移动平台=原移动;天帝移动适配.验证屏幕尺寸=原屏幕;天帝移动适配.验证安全区=原安全;typeof(天帝美术资源).GetProperty("当前").SetValue(null,原美术);
            File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));
        }
        if(结果.失败.Count+结果.错误.Count>0)throw new Exception("剪纸七页验收失败 "+结果.失败.Count+" / 错误 "+结果.错误.Count);
        Debug.Log("剪纸七页验收通过 "+结果.通过.Count+" 项。"+目录);
        // 主页已更换为独立山水素材，继续验证现用的首两页实现。
        var 原截图目录 = Environment.GetEnvironmentVariable("TIANDI_CAPTURE_DIR");
        try
        {
            if (string.IsNullOrEmpty(原截图目录))
                Environment.SetEnvironmentVariable("TIANDI_CAPTURE_DIR", Path.Combine(目录, "首两页山水"));
            天帝首两页验收.运行();
        }
        finally { Environment.SetEnvironmentVariable("TIANDI_CAPTURE_DIR", 原截图目录); }
    }
}
#endif
