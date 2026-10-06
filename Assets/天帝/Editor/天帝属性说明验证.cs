#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 隐形独立Canvas，只检查真实UGUI尺寸与事件，不切换场景、Play状态或玩家页面。
public static class 天帝属性说明验证
{
    [Serializable] sealed class 报告
    { public List<string> 通过=new List<string>(),失败=new List<string>(),错误=new List<string>(); }
    public static string 运行()
    {
        var r=new 报告();GameObject host=null;天帝主角属性 人=null;
        void 检查(string n,bool v)=>(v?r.通过:r.失败).Add(n);
        try
        {
            var 游戏=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
            var 字体=游戏?.默认字体??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));网.设置玩家等级(100);
            人=new 天帝主角属性(天帝普攻.主角配置(),网);
            host=new GameObject("独立属性说明验证",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));
            host.hideFlags=HideFlags.HideAndDontSave;
            host.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var 组=host.GetComponent<CanvasGroup>();组.alpha=0;组.blocksRaycasts=false;组.interactable=true;
            var root=new GameObject("独立角色页",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(host.transform,false);
            root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(0,1);root.sizeDelta=new Vector2(1600,900);root.anchoredPosition=new Vector2(-800,450);
            var 页=root.gameObject.AddComponent<天帝角色界面>();页.初始化(人,网,字体,()=>{});
            Canvas.ForceUpdateCanvases();
            foreach(string tab in new[]{"角色属性","战斗进阶","技能与形态","道纹分布"})
            {
                root.GetComponentsInChildren<Button>().Single(x=>x.name=="页签-"+tab).onClick.Invoke();
                foreach(var entry in root.GetComponentsInChildren<天帝角色属性悬停>())
                {
                    var e=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,entry.transform.position)};
                    ExecuteEvents.Execute(entry.gameObject,e,ExecuteEvents.pointerEnterHandler);Canvas.ForceUpdateCanvases();
                    var panel=root.Find("角色属性说明浮窗") as RectTransform;
                    var body=panel.GetComponentsInChildren<Text>().Single(x=>x.name=="属性说明正文");
                    检查(tab+" "+entry.属性+"移入显示简短说明",panel.gameObject.activeSelf&&body.text.Contains("点击属性查看完整公式")&&!body.text.Contains("F(A,K)")&&body.text.Length<240);
                    检查(tab+" "+entry.属性+"正文不溢出",body.preferredHeight<=body.rectTransform.rect.height+.1f&&panel.rect.height<=root.rect.height-24);
                    foreach(var local in new[]{new Vector3(root.rect.xMin,root.rect.yMin),new Vector3(root.rect.xMax,root.rect.yMax)})
                    {
                        e.position=RectTransformUtility.WorldToScreenPoint(null,root.TransformPoint(local));
                        ExecuteEvents.Execute(entry.gameObject,e,ExecuteEvents.pointerMoveHandler);
                        float x=panel.anchoredPosition.x,y=-panel.anchoredPosition.y;
                        检查(entry.属性+"边缘浮窗完整",x>=11.9f&&y>=11.9f&&x+panel.rect.width<=root.rect.width-11.9f&&y+panel.rect.height<=root.rect.height-11.9f);
                    }
                    检查(entry.属性+"浮窗不阻挡鼠标",!panel.GetComponent<CanvasGroup>().blocksRaycasts);
                    ExecuteEvents.Execute(entry.gameObject,e,ExecuteEvents.pointerExitHandler);检查(entry.属性+"移出隐藏",!panel.gameObject.activeSelf);
                    ExecuteEvents.Execute(entry.gameObject,e,ExecuteEvents.pointerClickHandler);
                    检查(entry.属性+"点击后完整公式可选阅读",panel.gameObject.activeSelf&&body.text.Contains("公式\n")&&body.text.Contains("当前结果")&&panel.GetComponent<CanvasGroup>().blocksRaycasts&&panel.GetComponentsInChildren<Button>().Single(x=>x.name=="关闭属性公式").IsInteractable());
                    检查(entry.属性+"完整公式正文不截断",body.preferredHeight<=body.rectTransform.rect.height+.1f);
                    if(entry.属性=="力量")检查("成长公式保留0.002系数",body.text.Contains("0.002"));
                    ExecuteEvents.Execute(entry.gameObject,e,ExecuteEvents.pointerExitHandler);检查(entry.属性+"详细说明固定保留",panel.gameObject.activeSelf);
                    panel.GetComponentsInChildren<Button>().Single(x=>x.name=="关闭属性公式").onClick.Invoke();检查(entry.属性+"关闭公式恢复浮窗",!panel.gameObject.activeSelf);
                }
            }
            页.显示属性说明("速度",Vector2.zero);root.GetComponentsInChildren<Button>().Single(x=>x.name=="页签-角色属性").onClick.Invoke();
            检查("切换页签隐藏浮窗",!root.Find("角色属性说明浮窗").gameObject.activeSelf);
            页.显示属性说明("智力",Vector2.zero);
            if(!Application.isPlaying)typeof(天帝角色界面).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(页,null);
            root.gameObject.SetActive(false);
            检查("关闭角色页隐藏浮窗",!root.Find("角色属性说明浮窗").gameObject.activeSelf);
        }
        catch(Exception ex){r.错误.Add(ex.ToString());}
        finally{if(host!=null)UnityEngine.Object.DestroyImmediate(host);人?.Dispose();}
        string dir=Path.Combine(天帝构建工具.项目根,"生成/验证/属性说明-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(r,true));return dir;
    }
}
#endif
