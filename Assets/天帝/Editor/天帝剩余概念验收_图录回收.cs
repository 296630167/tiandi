#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static partial class 天帝剩余概念验收
{
    static IEnumerator 图录回收验证()
    {
        检查("图录回收从真实主页开始",游戏.阶段==游戏阶段.主页);
        string 原库存=JsonUtility.ToJson(游戏.道纹数据.导出存档());
        int 原余额=游戏.宝盒数据.灵石;
        点("道纹图鉴");yield return null;
        var 图鉴=游戏.界面.图鉴页;
        检查("主页实际打开图鉴组件",游戏.界面.图鉴已打开&&图鉴!=null);
        var 图鉴背景=图鉴.GetComponentsInChildren<Image>().First(x=>x.name=="剪纸图鉴底图");
        检查("图鉴接入独立新山水背景",图鉴背景.sprite!=null&&图鉴背景.sprite.name=="图鉴背景");
        检查("图鉴满屏三列九张图录卡",图鉴.GetComponentsInChildren<Button>().Count(x=>x.name.StartsWith("剪纸图鉴条目-"))==9);
        foreach(string 分类 in new[]{"天赋道纹","基础属性","普通属性","功能道纹","五行元素","分叉道纹","特性道纹","转化道纹"})
        {
            点("图鉴分类-"+分类);yield return null;
            var 首卡=图鉴.GetComponentsInChildren<Button>().First(x=>x.name=="剪纸图鉴条目-0");
            var 标题=图鉴.GetComponentsInChildren<Text>().First(x=>x.name=="剪纸详情标题");
            检查("图鉴实际切换分类-"+分类,首卡.transform.Find("条目名称").GetComponent<Text>().text==标题.text&&标题.text.Length>0);
            检查("图鉴分类首卡使用独立图录素材-"+分类,((Image)首卡.targetGraphic).sprite!=null&&((Image)首卡.targetGraphic).sprite.name=="图录卡");
        }
        点("图鉴分类-功能道纹");yield return null;
        string 原首名=图鉴.GetComponentsInChildren<Text>().First(x=>x.name=="剪纸详情标题").text;
        点("剪纸图鉴下一页");yield return null;
        检查("图鉴九项分页更新详情焦点",图鉴.GetComponentsInChildren<Text>().First(x=>x.name=="剪纸图鉴页码").text.StartsWith("2 / ")&&图鉴.GetComponentsInChildren<Text>().First(x=>x.name=="剪纸详情标题").text!=原首名);
        点("剪纸图鉴上一页");yield return null;
        var 上页=图鉴.GetComponentsInChildren<Button>().First(x=>x.name=="剪纸图鉴上一页");
        检查("图鉴首页禁用仅整体半透明",!上页.interactable&&Mathf.Approximately(上页.GetComponent<CanvasGroup>().alpha,.5f)&&上页.colors.disabledColor==Color.white);
        var 等级=图鉴.GetComponentsInChildren<InputField>().Single();等级.text="100";等级.onEndEdit.Invoke("100");
        检查("图鉴查询真实物品等级上限",图鉴.查询物品等级==100);
        点("图鉴分类-基础属性");yield return null;
        检查("图鉴查询传入正式词条区间",图鉴.GetComponentsInChildren<Text>().First(x=>x.name=="完整正式说明").text.Contains("物品等级 100"));
        点("图鉴通用规则入口");yield return null;
        var 说明滚动=图鉴.GetComponentsInChildren<ScrollRect>().Single(x=>x.name=="正式说明视口");
        检查("图鉴完整共通规则可独立滚动",说明滚动.content.rect.height>说明滚动.viewport.rect.height&&说明滚动.content.GetComponent<Text>().text.Contains("连接与计算规则"));
        说明滚动.verticalNormalizedPosition=0;yield return null;
        检查("图鉴长说明实际滚到下方",说明滚动.content.anchoredPosition.y>0);
        点("图鉴分类-功能道纹");等级.text="1";等级.onEndEdit.Invoke("1");yield return null;
        yield return 拍("05_道纹图鉴_山水图录实装");
        游戏.界面.关闭图鉴();yield return null;
        检查("图鉴只读不改真实库存余额",原库存==JsonUtility.ToJson(游戏.道纹数据.导出存档())&&原余额==游戏.宝盒数据.灵石);

        点("道纹回收");yield return null;
        var 正式回收=游戏.界面.回收页;
        检查("主页实际打开回收组件",游戏.界面.回收已打开&&正式回收!=null);
        检查("回收接入独立新山水背景",正式回收.GetComponentsInChildren<Image>().Any(x=>x.name=="剪纸回收底图"&&x.sprite!=null&&x.sprite.name=="回收背景"));
        检查("回收右侧使用新详情长卷",正式回收.GetComponentsInChildren<Image>().Any(x=>x.name=="回收道纹详情"&&x.sprite!=null&&x.sprite.name=="详情长卷"));
        yield return 拍("07_道纹回收_真实主页入口");
        点("回收范围设置");yield return new WaitForEndOfFrame();
        检查("真实入口打开新范围纸卷",正式回收.范围已打开&&正式回收.GetComponentsInChildren<Image>().Any(x=>x.name=="回收范围小窗"&&x.sprite!=null&&x.sprite.name=="范围纸卷"));
        点("取消批选范围");游戏.界面.关闭回收();yield return null;

        // 仅把边界输入放在独立数据和界面中，不修改正式游戏库存或存档。
        var 弹层=游戏.GetComponentsInChildren<RectTransform>().First(x=>x.name=="设置层");
        var 根=new GameObject("图录回收隔离边界",typeof(RectTransform)).GetComponent<RectTransform>();根.SetParent(弹层,false);
        根.anchorMin=根.anchorMax=根.pivot=new Vector2(0,1);根.sizeDelta=new Vector2(1600,900);
        var 网=创建道纹夹具();var 钱=new 天帝宝盒(网,1,500);var 回收=根.gameObject.AddComponent<天帝道纹回收界面>();
        回收.初始化(网,钱,游戏.默认字体,()=>{根.gameObject.SetActive(false);});
        try
        {
            yield return null;
            检查("隔离回收同真实入口三列九卡",回收.GetComponentsInChildren<Button>().Count(x=>x.name.StartsWith("回收道纹-"))==9&&回收.显示项(9)==null);
            yield return 拍("07_道纹回收_山水清单实装");
            int 槽=Enumerable.Range(0,9).First(i=>网.可回收(回收.显示项(i),out _));
            点("回收道纹-"+槽);检查("回收查看详情不会勾选",回收.选中数量==0);
            勾("回收复选框-"+槽);int 原选择=回收.选中数量,原金额=回收.总回收灵石;var 原范围=回收.当前批选范围;
            点("回收范围设置");yield return new WaitForEndOfFrame();
            检查("范围弹窗阻断底层操作",回收.范围已打开&&!回收.GetComponentsInChildren<Button>().First(x=>x.name=="一键选中").interactable);
            var 范围框=回收.GetComponentsInChildren<RectTransform>().First(x=>x.name=="回收范围小窗");
            var 范围标题=范围框.GetComponentsInChildren<Text>().First(x=>x.text=="一键选中范围");
            检查("范围标题放在纸内纯底区",图录局部矩形(范围标题.rectTransform,范围框).yMax<=-89.9f);
            var 类型标题=范围框.GetComponentsInChildren<RectTransform>().First(x=>x.name=="批选道纹类型标题");
            var 类型下拉=范围框.GetComponentsInChildren<Dropdown>().First(x=>x.name=="批选类型下拉");
            检查("范围类型标题在下拉上方留白",图录局部矩形(类型标题,范围框).yMin>=图录局部矩形((RectTransform)类型下拉.transform,范围框).yMax+5);
            var 取消范围=范围框.Find("取消批选范围") as RectTransform;var 应用范围=范围框.Find("应用批选范围") as RectTransform;
            for(int i=0;i<3;i++)
            {
                var 库存项=范围框.GetComponentsInChildren<RectTransform>().First(x=>x.name=="库存范围-"+i);
                检查("库存范围与底部操作不重叠-"+i,!图录局部矩形(库存项,范围框).Overlaps(图录局部矩形(取消范围,范围框))&&!图录局部矩形(库存项,范围框).Overlaps(图录局部矩形(应用范围,范围框)));
            }
            for(int i=0;i<8;i++)
            {
                var 品阶卡=范围框.GetComponentsInChildren<RectTransform>().First(x=>x.name=="批选品阶-"+i);
                var 勾选框=品阶卡.Find("品阶勾选标识") as RectTransform;var 勾范围=图录局部矩形(勾选框,品阶卡);
                检查("品阶勾选保持在卡框内部-"+i,品阶卡.rect.Contains(勾范围.min)&&品阶卡.rect.Contains(勾范围.max));
                var 勾字=勾选框.GetComponent<Text>();
                检查("品阶勾选字形完整且留足行高-"+i,勾字.rectTransform.rect.height>=40&&勾字.preferredHeight<=勾字.rectTransform.rect.height+.1f&&勾字.preferredWidth<=勾字.rectTransform.rect.width+.1f&&勾字.cachedTextGenerator.characterCountVisible>=勾字.text.Length);
            }
            foreach(var 文 in 范围框.GetComponentsInChildren<Text>().Where(x=>!string.IsNullOrWhiteSpace(x.text)))
                检查("范围纸卷完整文字字形-"+文.name+"-"+文.text,文.preferredHeight<=文.rectTransform.rect.height+.1f&&文.cachedTextGenerator.characterCountVisible>=文.text.Length);
            点("批选品阶-0");
            回收.GetComponentsInChildren<Dropdown>().Single(x=>x.name=="批选类型下拉").value=3;
            点("库存范围-2");
            yield return new WaitForEndOfFrame();
            var 未选勾=范围框.GetComponentsInChildren<RectTransform>().First(x=>x.name=="批选品阶-0").Find("品阶勾选标识").GetComponent<Text>();
            检查("未选品阶方框字形完整",未选勾.text=="□"&&未选勾.preferredHeight<=未选勾.rectTransform.rect.height+.1f&&未选勾.cachedTextGenerator.characterCountVisible>=1);
            检查("范围类型下拉与品阶草稿保留原勾选",回收.选中数量==原选择&&回收.总回收灵石==原金额&&回收.当前批选范围.品阶掩码==原范围.品阶掩码&&回收.当前批选范围.位置==原范围.位置);
            yield return 拍("12_一键选中范围_山水纸卷实装");
            点("取消批选范围");yield return null;
            检查("取消范围不提交草稿且保留勾选",!回收.范围已打开&&回收.选中数量==原选择&&回收.总回收灵石==原金额&&回收.当前批选范围.品阶掩码==原范围.品阶掩码&&回收.当前批选范围.类型==原范围.类型&&回收.当前批选范围.位置==原范围.位置);

            for(int 范围=0;范围<3;范围++)
            {
                回收.设置显示条件(范围==2?-1:(int)道纹品阶.普通,0,范围==2?3:0);
                回收.设置批选范围(new 天帝回收范围{品阶掩码=255,类型=0,位置=1});
                var 可选=(范围==0?网.道纹:范围==1?网.道纹.Where(x=>x.品阶==道纹品阶.普通):Enumerable.Range(0,9).Select(回收.显示项).Where(x=>x!=null)).Where(x=>网.可回收(x,out _)).ToArray();
                点("回收范围设置");yield return new WaitForEndOfFrame();
                点("库存范围-"+范围);点("应用批选范围");yield return null;
                检查("应用真实库存范围-"+范围,!回收.范围已打开&&回收.当前批选范围.位置==范围&&回收.选中数量==可选.Length&&回收.总回收灵石==可选.Sum(天帝数值.道纹回收价));
            }
            回收.设置显示条件(-1,0,3);
            检查("主列表筛选清空旧选择避免隐藏回收",回收.选中数量==0&&回收.总回收灵石==0);
            var 待售=new List<道纹实例>();
            for(int 页=0;页<2;页++)
            {
                for(int i=0;i<9;i++){var 纹=回收.显示项(i);检查("跨页实际可回收-"+页+"-"+i,纹!=null&&网.可回收(纹,out _));待售.Add(纹);勾("回收复选框-"+i);}
                if(页==0){点("回收下一页");yield return null;}
            }
            int 收益=待售.Sum(天帝数值.道纹回收价),旧数=网.道纹.Count,旧钱=钱.灵石;
            检查("跨页九项保留十八枚勾选",回收.选中数量==18&&回收.总回收灵石==收益&&待售.Distinct().Count()==18);
            点("预览回收");yield return new WaitForEndOfFrame();
            检查("确认回收使用新绿边纸卷与真实品阶摘要",回收.确认已打开&&回收.GetComponentsInChildren<Image>().Any(x=>x.name=="回收确认面板"&&x.sprite!=null&&x.sprite.name=="确认纸卷")&&回收.GetComponentsInChildren<Transform>().Count(x=>x.name.StartsWith("回收摘要-"))==待售.Select(x=>x.品阶).Distinct().Count());
            var 确认框=回收.GetComponentsInChildren<RectTransform>().First(x=>x.name=="回收确认面板");
            var 摘要口=回收.GetComponentsInChildren<RectTransform>().First(x=>x.name=="回收摘要视口");
            var 确认标题=确认框.Find("回收确认标题") as RectTransform;var 确认数量=确认框.Find("回收确认数量") as RectTransform;
            检查("确认标题在纯底区且与正文留白",图录局部矩形(确认标题,确认框).yMax<=-89.9f&&图录局部矩形(确认标题,确认框).yMin>=图录局部矩形(确认数量,确认框).yMax+10);
            foreach(string 名 in new[]{"取消回收","确认回收"})检查("确认摘要不遮挡底部按钮-"+名,!图录局部矩形(摘要口,确认框).Overlaps(图录局部矩形(确认框.Find(名) as RectTransform,确认框)));
            检查("确认大标题实际生成字形",确认框.Find("回收确认标题").GetComponent<Text>().cachedTextGenerator.vertexCount>=4);
            foreach(var 文 in 确认框.GetComponentsInChildren<Text>().Where(x=>!string.IsNullOrWhiteSpace(x.text)))
                检查("确认纸卷完整文字字形-"+文.name+"-"+文.text,文.preferredHeight<=文.rectTransform.rect.height+.1f&&(文.canvasRenderer.cull||文.cachedTextGenerator.characterCountVisible>=文.text.Length));
            // 未进入摘要视口的行先检查容器；滚到尾部后检查这些行真实生成的字形。
            var 摘要滚动=摘要口.GetComponent<ScrollRect>();摘要滚动.verticalNormalizedPosition=0;yield return new WaitForEndOfFrame();
            foreach(var 文 in 摘要滚动.content.GetComponentsInChildren<Text>().Where(x=>!x.canvasRenderer.cull&&!string.IsNullOrWhiteSpace(x.text)))
                检查("确认摘要尾部实际完整字形-"+文.text,文.preferredHeight<=文.rectTransform.rect.height+.1f&&文.cachedTextGenerator.characterCountVisible>=文.text.Length);
            摘要滚动.verticalNormalizedPosition=1;yield return new WaitForEndOfFrame();
            yield return 拍("12_确认回收_山水纸卷实装");
            点("取消回收");yield return null;
            检查("取消批量确认保留十八枚与收益",!回收.确认已打开&&回收.选中数量==18&&回收.总回收灵石==收益&&网.道纹.Count==旧数&&钱.灵石==旧钱);
            点("预览回收");yield return new WaitForEndOfFrame();点("确认回收");yield return null;
            检查("真实回收十八枚只结算一次",!回收.确认已打开&&网.道纹.Count==旧数-18&&钱.灵石==旧钱+收益&&回收.选中数量==0&&回收.总回收灵石==0&&待售.All(x=>!网.道纹.Contains(x)));
            yield return 拍("07_道纹回收_真实回收结果");

            回收.设置显示条件(-1,0,0);yield return null;
            int 保护槽=-1;
            for(int p=0;p<8&&保护槽<0;p++)
            {
                保护槽=Enumerable.Range(0,9).Where(i=>回收.显示项(i)!=null&&!网.可回收(回收.显示项(i),out _)).DefaultIfEmpty(-1).First();
                if(保护槽>=0)break;
                if(!回收.GetComponentsInChildren<Button>().First(x=>x.name=="回收下一页").interactable)break;
                点("回收下一页");yield return null;
            }
            检查("回收保留原保护实例",保护槽>=0);
            if(保护槽>=0)
            {
                点("回收道纹-"+保护槽);
                var 保护勾=回收.GetComponentsInChildren<Toggle>().First(x=>x.name=="回收复选框-"+保护槽);
                检查("受保护可查看但勾选仅半透明禁用",!保护勾.interactable&&!保护勾.isOn&&Mathf.Approximately(保护勾.GetComponent<CanvasGroup>().alpha,.5f)&&回收.选中数量==0);
            }
            回收.设置显示条件(-1,0,3);yield return null;
            var 锁纹=回收.显示项(0);勾("回收复选框-0");
            点("道纹回收锁定");yield return null;
            检查("真实锁定立即剔除勾选与总价",锁纹.回收锁定&&回收.选中数量==0&&回收.总回收灵石==0);
            检查("隔离边界未改变正式玩家库存与余额",原库存==JsonUtility.ToJson(游戏.道纹数据.导出存档())&&原余额==游戏.宝盒数据.灵石);
        }
        finally{根.gameObject.SetActive(false);UnityEngine.Object.Destroy(根.gameObject);}
        yield return null;
        检查("图录回收完成后真实主页可用",游戏.阶段==游戏阶段.主页&&!游戏.界面.回收已打开&&!游戏.界面.图鉴已打开);
    }
    static Rect 图录局部矩形(RectTransform 子,RectTransform 父)
    {
        var 角=new Vector3[4];子.GetWorldCorners(角);Vector2 最小=new Vector2(float.MaxValue,float.MaxValue),最大=new Vector2(float.MinValue,float.MinValue);
        foreach(var 点 in 角){Vector2 本地=父.InverseTransformPoint(点);最小=Vector2.Min(最小,本地);最大=Vector2.Max(最大,本地);}return Rect.MinMaxRect(最小.x,最小.y,最大.x,最大.y);
    }
}
#endif
