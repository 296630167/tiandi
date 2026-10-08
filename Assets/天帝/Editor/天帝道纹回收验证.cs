#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class 天帝道纹回收验证
{
    [Serializable] sealed class 报告 { public List<string> 通过=new List<string>(),失败=new List<string>(),错误=new List<string>(); }
    public static string 运行()
    {
        天帝数值同步检查.校验();
        var r=new 报告();void 检查(string n,bool b)=>(b?r.通过:r.失败).Add(n);
        Application.LogCallback 日志=(文,栈,类)=>{if(类==LogType.Error||类==LogType.Exception||类==LogType.Assert)r.错误.Add(文+"\n"+栈);};Application.logMessageReceived+=日志;
        string 目录=Path.Combine(天帝构建工具.项目根,"生成/验证/道纹回收-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(目录);
        天帝道纹 网()=>new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
        道纹实例 属性(天帝道纹 w,int 阶=0,int 级=1){var x=天帝道纹生成.创建(1,道纹分类.属性,(道纹品阶)阶,new System.Random(123),道纹属性分组.基础,级);w.获得道纹(x);return x;}
        int[] 价={15,25,40,55,70,80,90,95},叉价={100,150,220,320};
        for(int i=0;i<8;i++)foreach(int l in new[]{1,50,100}){var w=网();var x=属性(w,i,l);检查("品阶固定价格-"+i+"-物品等级"+l,天帝数值.道纹回收价(x)==价[i]&&价[i]<天帝宝盒.价格(宝盒种类.属性));}
        for(int p=3;p<=6;p++){var w=网();var x=天帝道纹生成.创建(1,道纹分类.分叉,道纹品阶.普通,new System.Random(3));x.接口=(1<<p)-1;w.获得道纹(x);检查("分叉固定价格-"+p,天帝数值.道纹回收价(x)==叉价[p-3]&&叉价[p-3]<天帝宝盒.价格(宝盒种类.分叉));}
        foreach(宝盒种类 k in Enum.GetValues(typeof(宝盒种类)))
        {
            double 期望=k==宝盒种类.分叉?天帝宝盒.分叉概率().Select((v,i)=>v*叉价[i]/100d).Sum():天帝宝盒.品阶概率(k).Select((v,i)=>v*价[i]/10000d).Sum();
            检查("精确回收期望-"+k,Math.Abs(期望-(k==宝盒种类.属性?19.455:k==宝盒种类.功能?55:167))<.000001);
            var w=网();var b=new 天帝宝盒(w,9341,1000000);bool 全亏=true;
            for(int i=0;i<300;i++){int 前=b.灵石;if(!b.抽取(k,out var x,out _)||!b.预览回收(new[]{x},out int 钱,out _)||!b.回收道纹(new[]{x},钱,out _)||b.灵石>=前||w.道纹.Count!=0){全亏=false;break;}}
            检查("连续300次开盒回收余额严格下降-"+k,全亏);
        }
        {
            var w=网();var b=new 天帝宝盒(w,1,100);var x=属性(w);var y=属性(w,1);
            void 拒绝(string n,IReadOnlyList<道纹实例> a,int 金额){string 前=JsonUtility.ToJson(w.导出存档());int 钱=b.灵石;检查(n,!b.回收道纹(a,金额,out _)&&前==JsonUtility.ToJson(w.导出存档())&&钱==b.灵石);}
            拒绝("空批次拒绝",Array.Empty<道纹实例>(),0);拒绝("null批次拒绝",null,0);拒绝("null实例拒绝",new 道纹实例[]{null},0);
            拒绝("重复实例拒绝整批",new[]{x,x},30);拒绝("金额不一致拒绝整批",new[]{x,y},39);拒绝("外部同编号实例拒绝",new[]{天帝道纹生成.创建(x.编号,道纹分类.属性,道纹品阶.普通,new System.Random(2))},15);
            拒绝("中心源纹保护",new[]{w.已放置[Vector2Int.zero]},0);
            w.解锁格子(new Vector2Int(1,0));w.放置(y,new Vector2Int(1,0));拒绝("已放置混合批次保护",new[]{x,y},40);
            w.保存布局方案(0,"保护布局");w.收回(y);拒绝("卸下后仍受保存方案保护",new[]{x,y},40);
            string 方案=JsonUtility.ToJson(w.布局方案[0]);string 旧标识=JsonUtility.ToJson(w.导出存档());int 库存事件=0,钱事件=0;bool 同步=true,重入拒=false;
            w.状态改变+=()=>{库存事件++;同步&=!w.道纹.Contains(x)&&b.灵石==115;重入拒=!b.回收道纹(new[]{x},15,out _);};b.余额改变+=()=>钱事件++;
            检查("一次提交库存余额且拒绝重入",b.回收道纹(new[]{x},15,out _)&&库存事件==1&&钱事件==1&&同步&&重入拒);
            拒绝("重复出售旧引用拒绝",new[]{x},15);
            检查("回收改变库存快照",旧标识!=JsonUtility.ToJson(w.导出存档()));检查("布局方案原样保留且仍可载入",方案==JsonUtility.ToJson(w.布局方案[0])&&w.检查布局方案(0,out _));
            var 存=new 天帝存档(Path.Combine(目录,"隔离存档"));var d=new 天帝存档数据{序章已完成=true,主角=天帝普攻.主角配置(),画布=w.导出存档(),通货=new 天帝通货(w,1).导出库存(),灵石=b.灵石};
            检查("正式保存成功",存.保存(d));var 读=存.读取();var 回=天帝道纹.读取存档(读.画布);
            检查("重读保存金额且售出物品不复活",读.灵石==115&&回.道纹.Count==1&&回.道纹[0].编号==y.编号);检查("重读仍保护方案道纹",!回.可回收(回.道纹[0],out _));
        }
        {
            var w=网();var x=属性(w);var b=new 天帝宝盒(w,1,int.MaxValue-10);检查("余额溢出不吞道纹",!b.回收道纹(new[]{x},15,out _)&&w.道纹.Contains(x)&&b.灵石==int.MaxValue-10);
            var c=new 天帝宝盒(w,1,0);var t=new 天帝通货(w,1,1);c.预览回收(new[]{x},out int 旧价,out _);t.使用(通货种类.启灵石,x,0,out _);检查("预览后改造价格变化拒绝",!c.回收道纹(new[]{x},旧价,out _)&&w.道纹.Contains(x)&&c.灵石==0);
            检查("改造不按投入额外加价",天帝数值.道纹回收价(x)==25);
        }
        {
            var w=网();var b=new 天帝宝盒(w,1,500);var x=属性(w);b.回收道纹(new[]{x},15,out _);b.抽取(宝盒种类.属性,out var 新,out _);
            检查("售后开盒不恢复原引用",!w.道纹.Contains(x)&&w.道纹.Contains(新)&&!b.回收道纹(new[]{x},15,out _)&&b.灵石==415);
        }
        {
            var w=网();var x=属性(w);var y=属性(w,1);var b=new 天帝宝盒(w,1,100);int 事件=0;w.状态改变+=()=>事件++;
            检查("锁定发布保存事件",w.设置回收锁定(x,true)&&事件==1);
            检查("重复锁定不重复发布",w.设置回收锁定(x,true)&&事件==1);
            检查("锁定拒绝混合回收整批",!b.回收道纹(new[]{x,y},40,out _)&&b.灵石==100&&w.道纹.Contains(x)&&w.道纹.Contains(y));
            var 快照=JsonUtility.ToJson(w.导出存档());var 重读=天帝道纹.读取存档(JsonUtility.FromJson<道纹存档数据>(快照));
            检查("JSON重读保留逐枚回收锁",重读.道纹.Single(z=>z.编号==x.编号).回收锁定&&!重读.道纹.Single(z=>z.编号==y.编号).回收锁定);
            var 旧JSON=快照.Replace("\"回收锁定\":true,","").Replace("\"回收锁定\":false,","");
            检查("旧存档缺字段默认未锁定",天帝道纹.读取存档(JsonUtility.FromJson<道纹存档数据>(旧JSON)).道纹.All(z=>!z.回收锁定));
            检查("再次点击解锁可回收",w.设置回收锁定(x,false)&&w.可回收(x,out _)&&事件==2);
            b.预览回收(new[]{x,y},out int 预价,out _);w.设置回收锁定(y,true);
            检查("确认前锁定不吞物品或余额",!b.回收道纹(new[]{x,y},预价,out _)&&w.道纹.Count==2&&b.灵石==100);
            检查("不允许锁定源纹和外部实例",!w.设置回收锁定(w.已放置[Vector2Int.zero],true)&&!w.设置回收锁定(new 道纹实例(),true));
        }
        foreach(bool 手机 in new[]{false,true})
        {
            bool? 原移动=天帝移动适配.验证移动平台;GameObject host=null;
            try
            {
                天帝移动适配.验证移动平台=手机;var w=网();for(int i=0;i<31;i++)属性(w,i%8);var b=new 天帝宝盒(w,1,500);
                w.设置回收锁定(w.道纹[0],true);
                host=new GameObject("隔离回收批选",typeof(RectTransform),typeof(Canvas));host.hideFlags=HideFlags.HideAndDontSave;
                host.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)host.transform).sizeDelta=new Vector2(1600,900);
                var p=host.AddComponent<天帝道纹回收界面>();p.初始化(w,b,AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf"),()=>{});
                string 前=手机?"手机":"PC";
                检查(前+"移除选择本页且显示三个条件下拉",!host.GetComponentsInChildren<Transform>(true).Any(z=>z.name=="选本页")&&host.GetComponentsInChildren<Dropdown>().Length==3);
                var 品阶下拉=host.GetComponentsInChildren<Dropdown>().Single(z=>z.name=="回收品阶筛选");
                // Dropdown.Start只在Play自动执行；隔离Edit夹具显式初始化动画器。
                var 隐=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(Dropdown).GetMethod("Start",隐).Invoke(品阶下拉,null);
                try
                {
                    品阶下拉.Show();
                    检查(前+"下拉展开创建真实选项列表",host.GetComponentsInChildren<Transform>().Any(z=>z.name=="Dropdown List")&&host.GetComponentsInChildren<Toggle>().Count(z=>z.name.StartsWith("Item "))==9);
                    var 实际项=host.GetComponentsInChildren<Toggle>().Where(z=>z.name.StartsWith("Item ")).ToArray();
                    检查(前+"展开选项不使用压字花饰且背景不透明",实际项.All(z=>z.GetComponent<Image>().sprite==null&&z.GetComponent<Image>().color.a==1));
                    检查(前+"展开文字为勾选标记保留独立空间",实际项.All(z=>{var 文=z.GetComponentsInChildren<Text>().Single(t=>t.text!="✓");return 文.rectTransform.anchorMin.x>=.14f&&文.rectTransform.rect.height>=43.9f;}));
                }
                finally
                {
                    // Edit模式同步清理，避免UGUI自己的延迟Destroy改动真实画布。
                    foreach(var t in host.GetComponentsInChildren<Transform>().Where(z=>z.name=="Dropdown List"||z.name=="Blocker").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
                    typeof(Dropdown).GetField("m_Dropdown",隐).SetValue(品阶下拉,null);typeof(Dropdown).GetField("m_Blocker",隐).SetValue(品阶下拉,null);
                }
                p.一键选中();检查(前+"默认跨页选当前筛选且跳过锁",p.选中数量==30&&p.总回收灵石==w.道纹.Where(z=>!z.回收锁定).Sum(天帝数值.道纹回收价));
                var 原位置=Enumerable.Range(0,手机?6:9).Select(p.显示项).ToArray();
                var 项=p.显示项(1);int 旧数=p.选中数量;
                var 锁键=host.GetComponentsInChildren<天帝道纹锁定按钮>().First(z=>z.transform.parent.name=="回收道纹-1").GetComponent<Button>();
                锁键.onClick.Invoke();
                检查(前+"锁按钮立即剔除已勾选实例",项.回收锁定&&p.选中数量==旧数-1);
                检查(前+"点击锁定卡片不跳位也不改变其他卡片顺序",原位置.SequenceEqual(Enumerable.Range(0,手机?6:9).Select(p.显示项)));
                锁键.onClick.Invoke();检查(前+"解锁仍保持原位",!项.回收锁定&&原位置.SequenceEqual(Enumerable.Range(0,手机?6:9).Select(p.显示项)));锁键.onClick.Invoke();
                p.设置显示条件(0);p.一键选中();检查(前+"按品阶显示并批选",p.选中数量==w.道纹.Count(z=>z.品阶==道纹品阶.普通&&!z.回收锁定)&&p.显示项(0).品阶==道纹品阶.普通);
                p.设置显示条件(-1,0,2);p.一键选中();检查(前+"只显示锁定时批选为空",p.选中数量==0&&p.显示项(0).回收锁定);
                var 待解=p.显示项(0);host.GetComponentsInChildren<天帝道纹锁定按钮>().First(z=>z.transform.parent.name=="回收道纹-0").GetComponent<Button>().onClick.Invoke();
                检查(前+"锁图标再次点击解锁且锁筛选移除",!待解.回收锁定&&p.选中数量==0);
                var 下拉=host.GetComponentsInChildren<Dropdown>().Single(z=>z.name=="回收状态筛选");下拉.value=3;p.一键选中();
                检查(前+"下拉事件筛可回收",p.选中数量==30&&Enumerable.Range(0,手机?6:9).Select(p.显示项).Where(z=>z!=null).All(z=>w.可回收(z,out _)));
                p.设置批选范围(new 天帝回收范围{品阶掩码=255,位置=2});p.一键选中();检查(前+"范围支持当前页",p.选中数量==(手机?6:9));
                p.设置批选范围(new 天帝回收范围{品阶掩码=0});p.一键选中();检查(前+"未选任何品阶清空选择",p.选中数量==0);
                p.设置批选范围(new 天帝回收范围());p.打开范围();
                var 顶部=host.GetComponentsInChildren<Button>().Single(z=>z.name=="一键选中");
                检查(前+"范围弹窗阻断底层批选",p.范围已打开&&!顶部.interactable);
                host.GetComponentsInChildren<Button>().Single(z=>z.name=="批选品阶-0").onClick.Invoke();
                host.GetComponentsInChildren<Button>().Single(z=>z.name=="应用批选范围").onClick.Invoke();
                检查(前+"应用范围排除取消的品阶",!p.范围已打开&&(p.当前批选范围.品阶掩码&1)==0&&p.选中数量==w.道纹.Count(z=>z.品阶!=道纹品阶.普通&&w.可回收(z,out _)));
                p.打开范围();host.GetComponentsInChildren<Button>().Single(z=>z.name=="取消批选范围").onClick.Invoke();检查(前+"取消恢复控件且保留范围",!p.范围已打开&&顶部.interactable&&(p.当前批选范围.品阶掩码&1)==0);
            }
            catch(Exception ex){r.失败.Add((手机?"手机":"PC")+"批选UI异常 "+ex);}
            finally{if(host!=null)UnityEngine.Object.DestroyImmediate(host);天帝移动适配.验证移动平台=原移动;}
        }
        Application.logMessageReceived-=日志;
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(r,true));return 目录+"："+r.通过.Count+"通过 / "+r.失败.Count+"失败 / "+r.错误.Count+"错误";
    }
}
#endif
