#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static partial class 天帝剩余概念验收
{
    static void 主动模型检查()
    {
        主动冷却模型检查();
        周期特性手动检查();
        主动攻击形态检查();
        var 地 = new 天帝战斗地图(42, true); var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        网.设置玩家等级(50);
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通); 战.自动攻击启用 = false; 战.设置演示靶(new[] { Vector2.left * 3 });
            Vector2 发射方向 = Vector2.zero; 战.射击释放 += 向 => 发射方向 = 向;
            var 起 = 地.出生位置;
            for (int i = 0; i < 12; i++) 战.推进(起, .25f);
            检查("主动-有敌人但无输入不发射", 战.普通释放次数 == 0 && 战.灵矢.Count == 0);
            var 可用槽 = Enumerable.Range(0, 6).Where(i => 战.技能有链路(i)).ToArray();
            检查("主动-未接通接口不生成技能链路", 可用槽.Length == 1 && 可用槽[0] == 0 && !战.技能有链路(1));
            foreach (int i in 可用槽)
            {
                人.设置当前资源(人.血量, 人.灵力, 人.灵气护盾);
                float 前 = 人.当前灵力; var 向 = new Vector2(Mathf.Cos(i * .7f), Mathf.Sin(i * .7f));
                var 结果 = 战.尝试释放技能(i, 向, 起);
                检查("主动-有链路接口固定映射与消费-" + i, 结果 == 战斗操作结果.成功 && 战.当前通路 == (6 - i) % 6 && Mathf.Abs(人.当前灵力 - 前 + 天帝战斗系统.技能灵力消耗) < .001f);
                float 扣后 = 人.当前灵力;
                检查("主动-共享冷却不重复扣费-" + i, 战.尝试释放技能(i, 向, 起) == 战斗操作结果.冷却中 && 人.当前灵力 == 扣后);
                战.推进(起, .225f);
                var 弹 = 战.灵矢.LastOrDefault(x => x.参数.通路 == (6 - i) % 6);
                检查("主动-前摇后保持输入方向-" + i, Vector2.Dot(发射方向, 向.normalized) > .999f && (弹 == null || Vector2.Dot(弹.方向, 向.normalized) > .999f) && 战.通路释放次数[(6 - i) % 6] == 1);
                推进主动验收秒(战, 起, 战.技能冷却剩余 + .001f);
            }
            战.设置演示靶(Array.Empty<Vector2>()); 人.设置当前资源(人.血量, 天帝战斗系统.技能灵力消耗 - .001f, 人.灵气护盾);
            float 少 = 人.当前灵力; int 释放前 = 战.普通释放次数;
            检查("主动-不足一厘拒绝且无消费无发射", 战.尝试释放技能(0, Vector2.up, 起) == 战斗操作结果.灵力不足 && 人.当前灵力 == 少 && 战.普通释放次数 == 释放前);
            人.设置当前资源(人.血量, 天帝战斗系统.技能灵力消耗, 人.灵气护盾);
            检查("主动-恰好费用可施放到零", 战.尝试释放技能(0, Vector2.up, 起) == 战斗操作结果.成功 && 人.当前灵力 == 0);
            战.推进(起, .225f);
            检查("主动-无目标也能施法", 战.普通释放次数 == 释放前 + 1 && 战.灵矢.Any(x => Vector2.Dot(x.方向, Vector2.up) > .999f));
            float 回复前 = 人.当前灵力; 战.推进(起, .25f);
            检查("主动-正式灵力回复且不超上限", Mathf.Abs(人.当前灵力 - 回复前 - 人.灵力 * .06f * .25f) < .001f && 人.当前灵力 <= 人.灵力);
            float 无效前 = 人.当前灵力;
            检查("主动-零方向NaN越界拒绝不扣", 战.尝试释放技能(0, Vector2.zero, 起) == 战斗操作结果.无效方向 && 战.尝试释放技能(0, new Vector2(float.NaN, 0), 起) == 战斗操作结果.无效方向 && 战.尝试释放技能(6, Vector2.up, 起) == 战斗操作结果.未解封 && 人.当前灵力 == 无效前);
            人.设置当前资源(人.血量, 人.灵力, 人.灵气护盾);
            检查("闪避-之前可以受伤", 战.伤害玩家(1f));
            检查("闪避-正常启动无灵力费用", 战.尝试闪避() == 战斗操作结果.成功 && 人.当前灵力 == 人.灵力);
            float 血 = 人.当前血量, 盾 = 人.当前灵气护盾;
            检查("闪避-启动后普通和伤害包均免疫", !战.伤害玩家(10f) && !战.伤害玩家(new 战斗伤害包(10, 10, 50)) && 人.当前血量 == 血 && 人.当前灵气护盾 == 盾);
            战.推进(起, .099f);
            检查("闪避-0.099秒仍无敌", 战.闪避无敌中 && !战.伤害玩家(10f));
            战.推进(起, .001f);
            检查("闪避-0.100秒结束可以受伤", !战.闪避无敌中 && 战.伤害玩家(1f));
            检查("闪避-冷却期间拒绝重复", 战.尝试闪避() == 战斗操作结果.冷却中);
            float 冷却 = 战.闪避冷却剩余; 战.推进(起, 0);
            检查("闪避-零时间不偷跑冷却", 战.闪避冷却剩余 == 冷却);
            推进主动验收秒(战, 起, 战.闪避冷却剩余 + .001f);
            检查("闪避-冷却结束恢复使用", 战.尝试闪避() == 战斗操作结果.成功);
            检查("闪避-空地直线准确三米", Vector2.Distance(地.直线闪避(起, Vector2.right, 3), 起 + Vector2.right * 3) < .001f);
            bool 墙 = false;
            for (int y = -35; y < 35 && !墙; y++) for (int x = -35; x < 35 && !墙; x++)
            {
                var a = new Vector2(x, y); if (!地.可站立(a) || 地.可站立(a + Vector2.right)) continue;
                var b = 地.直线闪避(a, Vector2.right, 3);
                墙 = 地.可站立(b) && b.x < a.x + 1 && Mathf.Abs(b.y - a.y) < .001f;
            }
            检查("闪避-碰墙停止保持方向不穿墙", 墙);
            人.设置当前资源(0, 人.灵力, 0); int 死前 = 战.普通释放次数;
            检查("主动-死亡拒绝施法闪避与回复", 战.尝试释放技能(0, Vector2.up, 起) == 战斗操作结果.无法操作 && 战.尝试闪避() == 战斗操作结果.无法操作);
            战.推进(起, .25f); 检查("主动-死亡不补发前摇", 战.普通释放次数 == 死前 && 战.灵矢.Count == 0);
            战.清理特性战斗(); 战.战术.清理();
        }
        var 一网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 一网))
        {
            var 战 = new 天帝战斗系统(地, 一网, 人, 战斗难度.普通); float 前 = 人.当前灵力;
            检查("主动-一级仅第一接口其余按等级锁定", 战.技能已解封(0) && Enumerable.Range(1,5).All(i => !战.技能已解封(i)) && 战.尝试释放技能(1, Vector2.up, 地.出生位置) == 战斗操作结果.未解封 && 人.当前灵力 == 前);
        }
        var 回网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.余响)); 回网.设置玩家等级(50);
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(),回网))
        {
            var 战 = new 天帝战斗系统(地,回网,人,战斗难度.普通); 战.自动攻击启用 = false; 战.设置演示靶(Array.Empty<Vector2>());
            bool 方向对=true; 战.射击释放+=向=>方向对 &= Vector2.Dot(向,Vector2.up)>.999f;
            for(int i=0;i<5;i++){战.尝试释放技能(2,Vector2.up,地.出生位置);推进主动验收秒(战,地.出生位置,战.技能冷却剩余+.001f);}
            检查("余响-五次手动施放追加一次且继承方向通路", 战.普通释放次数==5&&战.回响次数==1&&方向对&&战.当前通路==4&&战.通路释放次数[4]==5);
        }
    }
    static void 推进主动验收秒(天帝战斗系统 战, Vector2 位置, float 秒)
    {
        while (秒 > .00001f) { float 步 = Mathf.Min(.25f, 秒); 战.推进(位置, 步); 秒 -= 步; }
    }
    static void 推进主动验收秒(天帝战斗场景 场, float 秒)
    {
        while (秒 > .00001f) { float 步 = Mathf.Min(.25f, 秒); 场.战斗一步(步); 秒 -= 步; }
    }
    static float 主动验收前摇(天帝战斗系统 战)
        => Mathf.Min(战.射击前摇, 战.技能冷却总时长 * (float)天帝数值.取("player.attack_windup_fraction")) + .001f;
    static void 主动冷却模型检查()
    {
        var 地 = new 天帝战斗地图(42, true); var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通); 战.自动攻击启用 = false; 战.设置演示靶(Array.Empty<Vector2>());
            try
            {
                检查("冷却-一级正式基础攻速0.7", Mathf.Abs(人.攻击速度 - .7f) < .00001f);
                检查("冷却-技能启动记录实际间隔", 战.尝试释放技能(0, Vector2.up, 地.出生位置) == 战斗操作结果.成功
                    && Mathf.Abs(战.技能冷却总时长 - 战.当前普攻.间隔) < .00001f && 战.技能冷却总时长 == 战.技能冷却剩余);
                检查("冷却-闪避启动记录实际急速间隔", 战.尝试闪避() == 战斗操作结果.成功
                    && Mathf.Abs(战.闪避冷却总时长 - (float)天帝数值.取("player.dodge_cooldown") / (1 + 人.技能急速)) < .00001f);
                推进主动验收秒(战, 地.出生位置, Mathf.Min(战.技能冷却总时长, 战.闪避冷却总时长) * .4f);
                float 技总 = 战.技能冷却总时长, 技剩 = 战.技能冷却剩余, 闪总 = 战.闪避冷却总时长, 闪剩 = 战.闪避冷却剩余;
                float 旧速 = 人.攻击速度, 旧急 = 人.技能急速;
                网.设置玩家等级(50); 人.设置特性增益(0, .2f, .25f, 0);
                检查("冷却-升级与增益确实改变攻速急速", 人.攻击速度 > 旧速 && 人.技能急速 > 旧急);
                检查("冷却-升级与急速不改变正在运行的总长剩余", 战.技能冷却总时长 == 技总 && 战.技能冷却剩余 == 技剩
                    && 战.闪避冷却总时长 == 闪总 && 战.闪避冷却剩余 == 闪剩);
                推进主动验收秒(战, 地.出生位置, Mathf.Max(技剩, 闪剩) + .001f);
                检查("冷却-两种计时结束归零", 战.技能冷却剩余 == 0 && 战.闪避冷却剩余 == 0);
                float 新技总 = 战.读取通路参数(0).间隔, 新闪总 = (float)天帝数值.取("player.dodge_cooldown") / (1 + 人.技能急速);
                人.设置当前资源(人.血量, 人.灵力, 人.灵气护盾);
                检查("冷却-下一次技能才使用升级后间隔", 战.尝试释放技能(0, Vector2.up, 地.出生位置) == 战斗操作结果.成功
                    && Mathf.Abs(战.技能冷却总时长 - 新技总) < .00001f && 战.技能冷却总时长 < 技总);
                检查("冷却-下一次闪避使用当前急速", 战.尝试闪避() == 战斗操作结果.成功 && Mathf.Abs(战.闪避冷却总时长 - 新闪总) < .00001f);
            }
            finally { 战.清理特性战斗(); 战.战术.清理(); }
        }
    }
    static T 主动HUD字段<T>(string 名)
        => (T)typeof(天帝界面).GetField(名, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(游戏.界面);
    static void 检查主动冷却HUD(string 阶段, 天帝战斗系统 战, 天帝主角属性 人)
    {
        var 按钮 = 主动HUD字段<Button[]>("技能键"); var 遮罩 = 主动HUD字段<Image[]>("技能冷却遮罩");
        bool 亮度 = true, 点击 = true, 径向 = true, 隐藏 = true;
        var 名字 = 主动HUD字段<Text[]>("技能标签"); var 状态 = 主动HUD字段<Text[]>("技能状态字"); var 序号 = 主动HUD字段<Text[]>("技能按键字");
        for (int i = 0; i < 6; i++)
        {
            bool 链 = 战.技能有链路(i), 开 = 战.技能已解封(i), 冷 = 链 && 开 && 战.技能冷却剩余 > .00001f;
            bool 可操作 = 链 && 开 && !战.玩家死亡 && !游戏.界面.战斗已暂停 && 人.当前灵力 >= 天帝战斗系统.技能灵力消耗;
            亮度 &= Mathf.Abs(按钮[i].GetComponent<CanvasGroup>().alpha - (可操作 ? 1 : .5f)) < .00001f;
            点击 &= 按钮[i].interactable == (可操作 && !冷);
            float 比例 = 冷 ? Mathf.Clamp01(战.技能冷却剩余 / Mathf.Max(.001f, 战.技能冷却总时长)) : 0;
            径向 &= 遮罩[i].gameObject.activeSelf == 冷 && 遮罩[i].type == Image.Type.Filled && 遮罩[i].fillMethod == Image.FillMethod.Radial360
                && !遮罩[i].raycastTarget && Mathf.Abs(遮罩[i].fillAmount - 比例) < .0001f;
            隐藏 &= 按钮[i].gameObject.activeSelf == 链 && 名字[i].gameObject.activeSelf == 链 && 状态[i].gameObject.activeSelf == 链
                && 序号[i].gameObject.activeSelf == 链 && 按钮[i].GetComponent<CanvasGroup>().blocksRaycasts == 链;
        }
        检查(阶段 + "-冷却保持亮度其它禁用半透明", 亮度); 检查(阶段 + "-按钮按实际状态可用", 点击);
        检查(阶段 + "-径向剩余比例与结束隐藏", 径向); 检查(阶段 + "-未接通槽按钮文字及射线均隐藏", 隐藏);
        var 闪 = 主动HUD字段<Button>("闪避键"); var 轨 = 主动HUD字段<Image>("闪避冷却轨道"); var 条 = 主动HUD字段<Image>("闪避冷却进度");
        bool 闪可 = !战.玩家死亡 && !游戏.界面.战斗已暂停, 闪冷 = 战.闪避冷却剩余 > .00001f;
        检查(阶段 + "-闪避细条按已过比例推进", 轨.gameObject.activeSelf == 闪冷 && !轨.raycastTarget && !条.raycastTarget
            && Mathf.Abs(条.rectTransform.anchorMax.x - Mathf.Clamp01(1 - 战.闪避冷却剩余 / Mathf.Max(.001f, 战.闪避冷却总时长))) < .0001f);
        检查(阶段 + "-闪避冷却保持亮度其它禁用半透明", 闪.interactable == (闪可 && !闪冷)
            && Mathf.Abs(主动HUD字段<CanvasGroup>("闪避透明").alpha - (闪可 ? 1 : .5f)) < .00001f);
    }
    static void 检查冷却遮罩对齐(string 平台, bool 手机)
    {
        var 按钮 = 主动HUD字段<Button[]>("技能键"); var 遮罩 = 主动HUD字段<Image[]>("技能冷却遮罩");
        检查(平台 + "-遮罩贴合原素材且不改布局", Enumerable.Range(0, 6).All(i =>
        {
            var 底 = 按钮[i].GetComponentsInChildren<Image>(true).First(x => x.name == (手机 ? "圆形技能青玉底" : "接口道纹图标"));
            var r = 遮罩[i].rectTransform;
            return 遮罩[i].sprite == 底.sprite && Vector2.Distance(r.anchoredPosition, 底.rectTransform.anchoredPosition) < .0001f
                && Vector2.Distance(r.rect.size, 底.rectTransform.rect.size) < .0001f
                && Mathf.Abs(r.rect.width - (手机 ? 39 : 44)) < .0001f;
        }));
    }
    static void 检查主动冷却暂停(string 平台, 天帝战斗场景 场)
    {
        var 战 = 场.战斗; var 人 = 游戏.主角属性;
        if (战.闪避冷却剩余 <= .00001f) { 场.闪避(); 游戏.界面.更新战斗状态(); }
        var 冷却片 = 主动HUD字段<Image[]>("技能冷却遮罩").FirstOrDefault(x => x.gameObject.activeInHierarchy);
        float 技 = 战.技能冷却剩余, 闪 = 战.闪避冷却剩余, 灵 = 人.当前灵力;
        bool 在冷却 = 技 > .00001f && 闪 > .00001f && 冷却片 != null;
        检查(平台 + "-暂停夹具确有两种正在计时的冷却", 在冷却);
        if (!在冷却) return;
        float 遮 = 冷却片.fillAmount, 条 = 主动HUD字段<Image>("闪避冷却进度").rectTransform.anchorMax.x;
        游戏.界面.切换战斗暂停(); 游戏.界面.更新战斗状态();
        场.战斗一步(.25f);
        检查(平台 + "-暂停冻结资源技能闪避和视觉进度", 战.技能冷却剩余 == 技 && 战.闪避冷却剩余 == 闪 && 人.当前灵力 == 灵
            && Mathf.Abs(冷却片.fillAmount - 遮) < .0001f
            && 主动HUD字段<Image>("闪避冷却进度").rectTransform.anchorMax.x == 条);
        检查(平台 + "-暂停拒绝施法闪避", 场.释放技能(0) == 战斗操作结果.无法操作 && 场.闪避() == 战斗操作结果.无法操作);
        检查主动冷却HUD(平台 + "暂停", 战, 人);
        游戏.界面.关闭战斗暂停(); 游戏.界面.更新战斗状态();
        检查主动冷却HUD(平台 + "恢复", 战, 人);
    }
    // 只建立当前特性的必要条件，避免旧全属性长直线夹具超出正式31格画布。
    static 天帝道纹 主动特性构筑(int id)
    {
        var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));网.设置玩家等级(100);
        void 放(道纹实例 纹,int x,int y)
        {
            var 格=new Vector2Int(x,y);
            if(!网.获得道纹(纹)||!网.解锁格子(格)||!网.放置(纹,格))throw new Exception("主动特性夹具放置失败 "+格);
        }
        放(天帝特性道纹.创建(1,道纹分类.特性,id,道纹品阶.普通,1,0),1,0);
        int x=2;
        var 条件=Enumerable.Range(0,(int)天帝特性道纹.数值(id,"condition_count"))
            .Select(i=>(道纹属性)Enum.Parse(typeof(道纹属性),天帝特性道纹.文本(id,"条件."+i+".0")))
            .Where(a=>!天帝道纹属性.是功能(a)).Distinct();
        foreach(var a in 条件)
        {
            var 岔=天帝道纹生成.创建(x,道纹分类.分叉,道纹品阶.普通,new System.Random(x));岔.接口=11;放(岔,x,0);
            var 叶=天帝道纹生成.创建(100+x,道纹分类.属性,道纹品阶.普通,new System.Random(x),道纹属性分组.基础);
            叶.接口=16;叶.词条.Clear();叶.词条.Add(道纹词条.从定点(a,1000000));放(叶,x,1);x++;
        }
        var 功=typeof(天帝特性与形态验证).GetMethod("功",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(var f in new[]{道纹功能.齐射,道纹功能.分裂,道纹功能.连锁,道纹功能.回旋,道纹功能.爆破})
        {放((道纹实例)功.Invoke(null,new object[]{200+x,f}),x,0);x++;}
        return 网;
    }
    static void 周期特性手动检查()
    {
        foreach(int id in new[]{1,2,3,4,5,6,7,8,9,10,11,16,19,20,23,25,29,30,31,32,33,34,35,36,37,38})
        {
            var 网=主动特性构筑(id);var 地=new 天帝战斗地图(42,true);
            using(var 人=new 天帝主角属性(天帝普攻.主角配置(),网))
            {
                var 战=new 天帝战斗系统(地,网,人,战斗难度.普通);战.自动攻击启用 = false;战.设置演示靶(new[]{Vector2.right*6});
                天帝战斗扩展验证.设(战,"演示模式",false);foreach(var e in 战.敌人)天帝战斗扩展验证.设(e,"登场剩余秒",100f);
                for(int n=0;n<120;n++)战.推进(地.出生位置,.25f);
                检查("特性-无输入不自动施放-"+id,战.特性施放次数==0&&战.普通释放次数==0);
                战.射击前摇=0;
                var 结果=战.尝试释放技能(0,Vector2.up,地.出生位置);
                检查("特性-对应接口手动触发-"+id,结果==战斗操作结果.成功&&战.特性施放次数>0);
                if(new[]{1,3,5,7,11,30,31,32,35}.Contains(id))
                {
                    // 扇形边缘可以偏离中央方向；固定敌人在右侧，整体瞄准旋转90度时覆盖应同步旋转。
                    var 原方向=战.特性效果列表.Select(e=>e.方向).ToArray();
                    var 对照网=主动特性构筑(id);
                    using(var 对照人=new 天帝主角属性(天帝普攻.主角配置(),对照网))
                    {
                        var 对照=new 天帝战斗系统(地,对照网,对照人,战斗难度.普通); 对照.自动攻击启用 = false;
                        对照.设置演示靶(new[]{Vector2.right*6});天帝战斗扩展验证.设(对照,"演示模式",false);
                        foreach(var e in 对照.敌人)天帝战斗扩展验证.设(e,"登场剩余秒",100f);
                        for(int n=0;n<120;n++)对照.推进(地.出生位置,.25f);
                        对照.射击前摇=0;var 对照结果=对照.尝试释放技能(0,Vector2.left,地.出生位置);
                        var 转方向=对照.特性效果列表.Select(e=>e.方向).ToArray();
                        检查("特性-扇形随手动瞄准整体旋转-"+id,对照结果==战斗操作结果.成功&&原方向.Length>0&&原方向.Length==转方向.Length
                            &&原方向.Select((向,i)=>Vector2.Dot(new Vector2(-向.y,向.x),转方向[i])>.999f).All(x=>x));
                        对照.清理特性战斗();对照.战术.清理();
                    }
                }
                战.清理特性战斗();战.战术.清理();
            }
        }
    }
    static void 主动攻击形态检查()
    {
        var 功=typeof(天帝特性与形态验证).GetMethod("功",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(var f in Enumerable.Range(29,10).Select(x=>(道纹功能)x).Concat(new[]{道纹功能.陨落}))
        {
            var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));网.设置玩家等级(100);
            var 纹=(道纹实例)功.Invoke(null,new object[]{1,f});var 格=new Vector2Int(1,0);
            if(!网.获得道纹(纹)||!网.解锁格子(格)||!网.放置(纹,格))throw new Exception("主动形态夹具放置失败 "+f);
            var 地=new 天帝战斗地图(42,true);
            using(var 人=new 天帝主角属性(天帝普攻.主角配置(),网))
            {
                var 战=new 天帝战斗系统(地,网,人,战斗难度.普通);战.设置演示靶(new[]{地.出生位置+Vector2.right*6});
                战.射击前摇=0;var 结果=战.尝试释放技能(0,Vector2.up,地.出生位置);
                检查("形态-手动发射不改朝右侧敌人-"+f,结果==战斗操作结果.成功&&战.灵矢.Count>0&&战.灵矢.All(v=>Vector2.Dot(v.方向,Vector2.up)>.999f));
                if(f==道纹功能.剑雨||f==道纹功能.陨落)
                {
                    var v=战.灵矢.First();var 点=f==道纹功能.剑雨
                        ?(Vector2)typeof(战斗灵矢).GetField("形态锚点",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(v):v.位置;
                    var 偏=点-地.出生位置;
                    检查("形态-落点朝手动方向-"+f,偏.y>0&&Mathf.Abs(偏.x)<.001f);
                }
                战.清理特性战斗();战.战术.清理();
            }
        }
    }
    static IEnumerator 主动战斗验证()
    {
        主动模型检查();
        if (游戏.阶段 == 游戏阶段.标题) 游戏.继续游戏(); yield return null; yield return new WaitForEndOfFrame();
        游戏.界面.跳过新手指引();
        点("开始历练"); yield return null; yield return new WaitForEndOfFrame(); 点("确认进入");
        for (int i = 0; i < 900 && 游戏.阶段 == 游戏阶段.战斗加载; i++) yield return null;
        var 场 = 游戏.战斗场景; 检查("主动-正式主页进入真实战场", 游戏.阶段 == 游戏阶段.战斗 && 场?.战斗 != null);
        if (场 == null) throw new Exception("实际战斗入口未加载");
        游戏.界面.跳过新手指引(); 场.enabled = false; 场.战斗.自动攻击启用 = false;
        var 人 = 游戏.主角属性; var 战 = 场.战斗;
        推进主动验收秒(场, Mathf.Max(战.技能冷却剩余, 战.闪避冷却剩余) + .001f);
        人.设置当前资源(人.血量, 人.灵力, 人.灵气护盾); 游戏.界面.更新战斗状态();
        游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return null; yield return new WaitForEndOfFrame();
        int 预期技能槽 = Enumerable.Range(0, 6).Count(i => 场.战斗.技能有链路(i));
        检查("HUD-技能槽按实际链路显示", 游戏.GetComponentsInChildren<Button>().Count(x => x.name.StartsWith("技能") && x.name.Length == 3) == 预期技能槽);
        检查("HUD-无链路技能卡隐藏且不拦截射线", 游戏.GetComponentsInChildren<Button>(true).Where(x => x.name.StartsWith("技能") && x.name.Length == 3 && !场.战斗.技能有链路(int.Parse(x.name.Substring(2)) - 1)).All(x => !x.gameObject.activeSelf && !x.GetComponent<CanvasGroup>().blocksRaycasts));
        检查("HUD-资源球为独立素材动态液位", 游戏.GetComponentsInChildren<Image>().Count(x => x.name == "真实液位" && x.sprite != null && x.type == Image.Type.Filled) == 2);
        检查主动冷却HUD("PC就绪", 战, 人); 检查冷却遮罩对齐("PC", false);
        Vector2 发射向 = Vector2.zero; 战.射击释放 += 向 => 发射向 = 向;
        场.设置触控瞄准(Vector2.up);
        var 首个技能 = 游戏.GetComponentsInChildren<Button>().First(x => x.name.StartsWith("技能") && x.name.Length == 3 && x.interactable);
        int 首槽 = int.Parse(首个技能.name.Substring(2)) - 1;
        float 灵 = 人.当前灵力; int 首次释放前 = 战.普通释放次数; 点(首个技能);
        检查("PC-实际按钮点击触发施法与扣灵力", 人.当前灵力 == 灵 - 天帝战斗系统.技能灵力消耗);
        检查("PC-冷却启动遮罩为整圈", 战.技能冷却剩余 > 0 && 主动HUD字段<Image[]>("技能冷却遮罩")[首槽].fillAmount == 1);
        检查("PC-闪避启动细条为空", 场.闪避() == 战斗操作结果.成功); 游戏.界面.更新战斗状态();
        检查主动冷却HUD("PC冷却开始", 战, 人);
        推进主动验收秒(场, 主动验收前摇(战));
        检查("PC-按钮前摇后真发射", 战.普通释放次数 == 首次释放前 + 1);
        推进主动验收秒(场, 战.技能冷却剩余 - 战.技能冷却总时长 * .5f);
        检查("PC-技能中段径向遮罩为半圈", Mathf.Abs(主动HUD字段<Image[]>("技能冷却遮罩")[首槽].fillAmount - .5f) < .001f);
        检查主动冷却HUD("PC冷却中段", 战, 人);
        float 旧技总 = 战.技能冷却总时长, 旧闪总 = 战.闪避冷却总时长;
        float 旧遮 = 主动HUD字段<Image[]>("技能冷却遮罩")[首槽].fillAmount, 旧条 = 主动HUD字段<Image>("闪避冷却进度").rectTransform.anchorMax.x;
        var 增益字段 = new[] { "特性移速", "特性急速", "特性攻速", "特性闪避" }.Select(名 => typeof(天帝主角属性).GetField(名, BindingFlags.Instance | BindingFlags.NonPublic)).ToArray();
        var 原增益 = 增益字段.Select(x => (float)x.GetValue(人)).ToArray();
        try
        {
            人.设置特性增益(原增益[0], 原增益[1] + .25f, 原增益[2] + .25f, 原增益[3]); 游戏.界面.更新战斗状态();
            检查("PC-急速攻速改变不跳当前冷却总长与视觉比例", 战.技能冷却总时长 == 旧技总 && 战.闪避冷却总时长 == 旧闪总
                && 主动HUD字段<Image[]>("技能冷却遮罩")[首槽].fillAmount == 旧遮 && 主动HUD字段<Image>("闪避冷却进度").rectTransform.anchorMax.x == 旧条);
        }
        finally { 人.设置特性增益(原增益[0], 原增益[1], 原增益[2], 原增益[3]); 游戏.界面.更新战斗状态(); }
        yield return 拍("战斗01_PC冷却中段");
        检查主动冷却暂停("PC", 场); yield return null;
        推进主动验收秒(场, Mathf.Max(战.技能冷却剩余, 战.闪避冷却剩余) + .001f);
        检查("PC-技能闪避冷却完成", 战.技能冷却剩余 == 0 && 战.闪避冷却剩余 == 0);
        检查主动冷却HUD("PC冷却结束", 战, 人);
        var 鼠 = InputSystem.AddDevice<Mouse>(); var 键 = InputSystem.AddDevice<Keyboard>();
        try
        {
            Vector2 屏幕 = 场.俯视相机.WorldToScreenPoint(new Vector3(场.玩家位置.x + 6, 0, 场.玩家位置.y));
            InputSystem.QueueStateEvent(鼠, new MouseState { position = 屏幕 }); InputSystem.QueueStateEvent(键, new KeyboardState((Key)((int)Key.Digit1 + 首槽))); InputSystem.Update();
            int 数字释放前 = 战.普通释放次数; 私调(场, "读取主动操作"); 推进主动验收秒(场, 主动验收前摇(战));
            检查("PC-真实InputSystem数字键朝鼠标发射", 战.普通释放次数 == 数字释放前 + 1 && Vector2.Dot(发射向, Vector2.right) > .999f);
            System.IO.File.WriteAllText(System.IO.Path.Combine(目录,"inputdiagnostic.txt"), "数字键后释放次数="+战.普通释放次数+" 事件方向="+发射向+" 鼠标="+鼠.position.ReadValue()+" 瞄准="+场.瞄准方向);
            InputSystem.QueueStateEvent(键, new KeyboardState()); InputSystem.Update();
            InputSystem.QueueStateEvent(键, new KeyboardState(Key.Space)); InputSystem.Update(); var 前 = 场.玩家位置; 私调(场, "读取主动操作"); 场.战斗一步(.1f);
            检查("PC-真实空格键朝鼠标三米闪避", Mathf.Abs(Vector2.Distance(场.玩家位置, 前) - 3) < .01f && Mathf.Abs(场.玩家位置.y - 前.y) < .01f);
            InputSystem.QueueStateEvent(键, new KeyboardState()); InputSystem.Update();
        }
        finally { InputSystem.RemoveDevice(键); InputSystem.RemoveDevice(鼠); }
        推进主动验收秒(场, Mathf.Max(战.技能冷却剩余, 战.闪避冷却剩余) + .001f);
        人.设置当前资源(人.当前血量, 0, 人.当前灵气护盾); 游戏.界面.更新战斗状态();
        int 声前 = UnityEngine.Object.FindAnyObjectByType<天帝声音>().音效播放次数;
        检查("反馈-不足灵力真实操作拒绝", 场.释放技能(首槽) == 战斗操作结果.灵力不足 && 人.当前灵力 == 0);
        检查("反馈-文字声音视觉均来自失败事件", 游戏.GetComponentsInChildren<Text>().Any(x => x.name == "技能失败提示" && x.text == "灵力不足，无法释放") && UnityEngine.Object.FindAnyObjectByType<天帝声音>().音效播放次数 > 声前);
        检查主动冷却HUD("PC灵力不足", 战, 人);
        人.设置当前资源(人.当前血量, 人.灵力, 人.当前灵气护盾);
        bool? 移原 = 天帝移动适配.验证移动平台;
        try
        {
            天帝移动适配.验证移动平台 = true; 游戏.界面.显示战斗(); 游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return null; yield return new WaitForEndOfFrame();
            检查("移动-HUD左摇杆与右侧技能手势", 游戏.界面.战斗摇杆 != null && 游戏.GetComponentsInChildren<天帝战斗触控技能>().Length == 预期技能槽);
            var 圆技能 = 游戏.GetComponentsInChildren<Transform>().Where(x => x.name == "圆形技能青玉底").ToArray();
            检查("移动-技能圆形按钮两行布局", 圆技能.Length == 预期技能槽 && 圆技能.All(x => ((RectTransform)x.parent).rect.width >= 55 && ((RectTransform)x.parent).rect.height >= 55));
            检查冷却遮罩对齐("移动", true); 检查主动冷却HUD("移动就绪", 战, 人);
            var 手势 = 游戏.GetComponentsInChildren<天帝战斗触控技能>().FirstOrDefault();
            if (手势 == null) throw new Exception("当前构筑没有可验证的技能链路按钮");
            var r = (RectTransform)手势.transform; Vector2 中 = RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
            var e = new PointerEventData(EventSystem.current) { pointerId = 7, position = 中, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(e,hits);
            检查("移动-技能触控射线入口无遮挡", hits.Count > 0 && (hits[0].gameObject == 手势.gameObject || hits[0].gameObject.transform.IsChildOf(手势.transform)));
            ExecuteEvents.Execute(手势.gameObject, e, ExecuteEvents.pointerDownHandler);
            e.position = 中 + Vector2.up * 110; ExecuteEvents.Execute(手势.gameObject, e, ExecuteEvents.dragHandler);
            int 发前 = 战.普通释放次数;
            检查("移动-拖动时瞄准但不提前发射", Vector2.Dot(场.瞄准方向,Vector2.up) > .999f && 战.普通释放次数 == 发前);
            ExecuteEvents.Execute(手势.gameObject, e, ExecuteEvents.pointerUpHandler);
            int 移槽 = int.Parse(手势.name.Substring(2)) - 1;
            检查("移动-冷却启动遮罩为整圈", 战.技能冷却剩余 > 0 && 主动HUD字段<Image[]>("技能冷却遮罩")[移槽].fillAmount == 1);
            检查("移动-闪避真实触控按钮启动", 主动HUD字段<Button>("闪避键").interactable); 点(主动HUD字段<Button>("闪避键")); 游戏.界面.更新战斗状态();
            检查主动冷却HUD("移动冷却开始", 战, 人);
            推进主动验收秒(场, 主动验收前摇(战));
            检查("移动-松手真实施法", 战.普通释放次数 == 发前 + 1);
            推进主动验收秒(场, 战.技能冷却剩余 - 战.技能冷却总时长 * .5f);
            检查("移动-技能中段径向遮罩为半圈", Mathf.Abs(主动HUD字段<Image[]>("技能冷却遮罩")[移槽].fillAmount - .5f) < .001f);
            检查主动冷却HUD("移动冷却中段", 战, 人); yield return 拍("战斗02_移动端冷却中段");
            检查主动冷却暂停("移动", 场); yield return null;
            推进主动验收秒(场, Mathf.Max(战.技能冷却剩余, 战.闪避冷却剩余) + .001f);
            检查("移动-技能闪避冷却完成", 战.技能冷却剩余 == 0 && 战.闪避冷却剩余 == 0);
            检查主动冷却HUD("移动冷却结束", 战, 人);
            ExecuteEvents.Execute(手势.gameObject, e, ExecuteEvents.pointerDownHandler); 游戏.界面.切换战斗暂停(); 游戏.界面.关闭战斗暂停();
            推进主动验收秒(场, 战.技能冷却剩余 + .001f);
            int 取消前 = 战.普通释放次数; ExecuteEvents.Execute(手势.gameObject,e,ExecuteEvents.pointerUpHandler); 场.战斗一步(.225f);
            检查("移动-暂停取消手势松手不补发", 战.普通释放次数 == 取消前);
            var 摇 = 游戏.界面.战斗摇杆; var jr=(RectTransform)摇.transform; e.pointerId=11;e.position=RectTransformUtility.WorldToScreenPoint(null,jr.TransformPoint(jr.rect.center))+Vector2.right*100;
            ExecuteEvents.Execute(摇.gameObject,e,ExecuteEvents.pointerDownHandler);
            检查("移动-摇杆真实触控输入", 摇.方向.x > .5f); ExecuteEvents.Execute(摇.gameObject,e,ExecuteEvents.pointerUpHandler);
            检查("移动-摇杆松手归零", 摇.方向 == Vector2.zero);
        }
        finally { 天帝移动适配.验证移动平台 = 移原; }
        游戏.界面.显示战斗(); yield return null;
        战.推进(场.玩家位置,.25f); 人.设置当前资源(0,人.当前灵力,0); 场.战斗一步(.01f); yield return null;
        检查("战斗-死亡沿用真实身陨入口", 游戏.GetComponentsInChildren<Transform>().Any(x=>x.name=="战斗失败面板"));
        游戏.返回主页();
        for(int i=0;i<900&&游戏.阶段==游戏阶段.战斗加载;i++)yield return null;
        检查("战斗-离场清理HUD与触控", 游戏.阶段==游戏阶段.主页 && 游戏.GetComponentsInChildren<天帝战斗触控技能>().Length==0 && !游戏.GetComponentsInChildren<Transform>().Any(x=>x.name=="主动战斗HUD"));
    }
}
#endif
