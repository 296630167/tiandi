#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static partial class 天帝剩余概念验收
{
    static T 辅助查<T>(string 名 = null) where T : Component
        => 游戏.GetComponentsInChildren<T>(true).FirstOrDefault(x => x.gameObject.activeInHierarchy && (名 == null || x.name == 名));
    static void 辅助调用(object 对象, string 名, params object[] 参数)
    {
        var 法 = 对象.GetType().GetMethod(名, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        法.Invoke(对象, 参数);
    }
    static bool 辅助独立素材(Image 图, string 名)
    {
        var 正式 = Resources.Load<Sprite>("山水剩余界面/" + 名);
        return 正式 != null && 图 != null && 图.sprite == 正式;
    }
    static string 辅助摆放(天帝道纹 网) => string.Join(";", 网.已放置.Select(x => x.Value.编号 + "@" + x.Key + ":" + x.Value.接口).OrderBy(x => x));

    static IEnumerator 标题天赋验证()
    {
        游戏.界面.跳过新手指引(); 游戏.返回标题(); yield return null;
        var 背景 = Resources.Load<Sprite>("山水剩余界面/标题背景");
        检查("14标题使用独立无字山水背景", 背景 != null && 游戏.GetComponentsInChildren<Image>().Any(x => x.sprite == 背景));
        检查("14全名保留可编辑Text", 游戏.GetComponentsInChildren<Text>().Any(x => x.text.Replace("\n", "") == 天帝游戏.全名));
        检查("14入口跟随真实存档状态", 辅助查<Button>(游戏.可继续游戏 ? "继续游戏" : "开始游戏") != null);
        if (游戏.可继续游戏) 检查("14已有存档同时提供新游戏", 辅助查<Button>("新游戏") != null);
        yield return 拍("14_开始游戏标题页");
        游戏.确认开始新游戏(); yield return null; 游戏.跳过序章();
        float 等待 = 0;
        while (游戏.界面.源道纹页 == null && 等待 < 5) { 等待 += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSecondsRealtime(1.1f);
        var 天赋页 = 游戏.界面.源道纹页;
        检查("15实际新游戏进入五选一天赋", 游戏.阶段 == 游戏阶段.源道纹选择 && 天赋页 != null && 天赋页.可选择);
        检查("15天赋独立背景", 辅助独立素材(辅助查<Image>("天赋山水背景"), "天赋背景"));
        检查("15五张独立竖卡", 天赋页.GetComponentsInChildren<Image>().Count(x => x.name.StartsWith("源道纹-") && 辅助独立素材(x, "天赋卡片")) == 5);
        var 确认 = 辅助查<Button>("确认天赋");
        检查("15未选择只用整体半透明禁用", !确认.interactable && Mathf.Approximately(确认.GetComponent<CanvasGroup>().alpha, .5f) && 确认.colors.disabledColor == Color.white);
        int 刷新前 = 游戏.天赋池.刷新次数; 点("刷新天赋"); yield return null;
        检查("15刷新调用真实候选池", 游戏.天赋池.刷新次数 == 刷新前 + 1);
        检查("15五卡真实效果完整落在文字区域", Enumerable.Range(0,5).All(i=>
        {
            var 卡=天赋页.transform.Find("源道纹-"+i);
            var 文=卡.GetComponentsInChildren<Text>().First(x=>x.text==游戏.天赋池.候选[i].效果);
            return 文.preferredHeight<=文.rectTransform.rect.height+1;
        }));
        天赋页.选中(4); yield return null;
        检查("15选中更新下方详情而不遮住卡片", 天赋页.选中槽位 == 4 && !天赋页.详情已打开 && 确认.interactable && Mathf.Approximately(确认.GetComponent<CanvasGroup>().alpha, 1));
        检查("15横卷详情单独资源", 辅助独立素材(辅助查<Image>("天赋详情区"), "长卷详情"));
        检查("15详情读取选中天赋效果", 天赋页.GetComponentsInChildren<Text>().Any(x => x.text == 游戏.天赋池.候选[4].效果));
        yield return 拍("15_天赋初醒与源纹详情");
        int 选定编号 = 游戏.天赋池.候选[4].编号; 点("确认天赋"); yield return null;
        检查("15确认真实创建唯一源纹并进入主页", 游戏.阶段 == 游戏阶段.主页 && 游戏.初始源道纹编号 == 选定编号 && 游戏.道纹数据 != null);
        游戏.界面.跳过新手指引();
    }

    static IEnumerator 辅助页验证()
    {
        游戏.界面.跳过新手指引();
        var 网 = 创建道纹夹具();
        游戏.界面.显示道纹(网); yield return null;
        var 页 = 游戏.界面.道纹页; 页.打开布局方案(); yield return null;
        var 方案框 = 辅助查<Image>("布局方案面板");
        检查("09方案面板使用独立山水纸框", 辅助独立素材(方案框, "弹窗纸框"));
        检查("09布局方案有真实三槽", 页.GetComponentsInChildren<InputField>(true).Count(x => x.name == "方案名称") == 天帝道纹.方案槽数);
        string 原摆放 = 辅助摆放(网); int 原方案数量 = 网.布局方案[0].摆放.Count;
        辅助调用(页, "保存当前方案", 0); yield return null;
        点("取消方案选择"); yield return null;
        检查("09取消覆盖保留方案及画布", 网.布局方案[0].摆放.Count == 原方案数量 && 辅助摆放(网) == 原摆放);
        var 名称 = 页.GetComponentsInChildren<InputField>(true).First(x => x.name == "方案名称"); 名称.text = "隔离验收方案";
        辅助调用(页, "保存当前方案", 0); 辅助调用(页, "保存当前方案", 0); yield return null;
        检查("09保存真实实例而不改造道纹", 网.布局方案[0].已保存 && 网.布局方案[0].名称 == "隔离验收方案" && 辅助摆放(网) == 原摆放);
        辅助调用(页, "准备载入方案", 0); yield return null;
        检查("09检查通过启用应用操作", 辅助查<Button>("应用已检查的方案").interactable);
        yield return 拍("09_布局方案");
        点("应用已检查的方案"); yield return null;
        检查("09真实载入保持对应实例与接口", 辅助摆放(网) == 原摆放);
        页.关闭筛选(); 私调(页, "显示加成来源"); yield return null;
        检查("09来源窗口使用独立纸框", 辅助独立素材(辅助查<Image>("来源面板"), "弹窗纸框"));
        var 来源滚 = 辅助查<ScrollRect>("来源滚动视口");
        检查("09来源文字可滚动并保留实际道纹编号", 来源滚 != null && 来源滚.vertical && 来源滚.content.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("#")));
        检查("09来源有真实分组纸签和独立来源卡", 来源滚.content.GetComponentsInChildren<RectTransform>().Any(x=>x.name.StartsWith("来源分组-")) && 来源滚.content.GetComponentsInChildren<天帝道纹绘图>().Any(x=>x.单纹!=null));
        来源滚.verticalNormalizedPosition = .25f; yield return null; 来源滚.verticalNormalizedPosition = 1;
        yield return 拍("09_有效加成来源"); 页.关闭筛选();

        游戏.界面.显示道纹改造(网, new 天帝通货(网, 42)); yield return null;
        var 改造 = 辅助查<天帝通货界面>(); 改造.打开背包(); yield return null;
        var 包 = 辅助查<天帝道纹背包>();
        检查("08背包使用独立大纸框", 辅助独立素材(辅助查<Image>("道纹背包窗口"), "背包纸框"));
        检查("08大卡三乘三分页保留全库存", 包.总页数 > 1 && Enumerable.Range(0, 9).Count(x => 包.显示项(x) != null) == 9);
        检查("08九张卡名称实际渲染且容纳",Enumerable.Range(0,9).All(i=>
        {
            var 卡=包.transform.GetComponentsInChildren<Button>().First(x=>x.name=="背包道纹-"+i);
            var 名=卡.GetComponentsInChildren<Text>().First(x=>x.text==包.显示项(i).短名);
            return 名.gameObject.activeInHierarchy&&名.color.a>0&&名.preferredHeight<=名.rectTransform.rect.height+1&&名.cachedTextGenerator.vertexCount>4;
        }));
        检查("08分页中文按钮实际渲染",new[]{"背包上一页","背包下一页"}.All(n=>辅助查<Button>(n).GetComponentInChildren<Text>().cachedTextGenerator.vertexCount>4));
        int 总数 = 包.筛选结果数; 检查("08实际分页切换", 包.切页(1) && 包.页码 == 1); 包.切页(0);
        检查("08实际接口与旋转匹配筛选", 包.设置接口筛选(1, true) && 包.筛选结果数 <= 总数); 包.清除筛选();
        检查("08六方向多选按钮存在", 包.GetComponentsInChildren<Button>().Count(x => x.name.StartsWith("背包接口-")) == 6);
        var 下拉 = 辅助查<Dropdown>("背包品阶筛选"); 下拉.Show(); yield return null;
        检查("08展开筛选为真实Dropdown选项", 辅助查<RectTransform>("Dropdown List") != null);
        yield return 拍("08_背包与筛选展开"); 下拉.Hide(); yield return null;
        var 首纹 = 包.显示项(0); 点("背包道纹-0"); yield return null;
        检查("08选择真实道纹并关闭背包", 改造.当前目标 == 首纹 && !改造.背包已打开);

        游戏.界面.显示主页(); 游戏.界面.显示修行指引(); yield return null;
        var 指引 = 游戏.界面.新手指引;
        检查("16真实指引可以打开", 指引 != null && 游戏.界面.新手指引已打开);
        游戏.打开道纹(); yield return null;
        辅助调用(指引, "转到", 修行引导步骤.解锁格子); yield return null;
        检查("16引导说明使用独立卷纸", 辅助独立素材(辅助查<Image>("引导说明纸笺"), "指引纸卷"));
        yield return 拍("16_新手引导解锁格子"); 游戏.界面.跳过新手指引();
        游戏.界面.显示道纹(网); yield return null;
        页 = 游戏.界面.道纹页; var 纹 = 页.候选显示项(0);
        var 目标卡=页.定位引导候选(纹); yield return null;
        var 指针=RectTransformUtility.WorldToScreenPoint(null,目标卡.TransformPoint(目标卡.rect.center));
        页.指针移动(指针); yield return null;
        var 详情 = 辅助查<天帝道纹详情卡>();
        检查("16悬停详情保留真实道纹与独立卷纸", 详情 != null && 详情.当前道纹 == 纹 && 辅助独立素材(详情.GetComponent<Image>(), "竖卷详情"));
        检查("16小详情内有当前构筑真实攻击演示", 详情 != null && 详情.GetComponentsInChildren<天帝道纹攻击演示>().Length == 1);
        检查("16小详情演示限制在纸面内部",详情!=null&&详情.GetComponentInChildren<天帝道纹攻击演示>().GetComponentInParent<RectMask2D>()!=null);
        yield return 拍("16_跟随鼠标小详情"); 页.指针离开(); 游戏.返回主页(); 游戏.界面.显示主页();
    }
}
#endif
