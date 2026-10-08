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
                for (int n = 0; n < 6; n++) 战.推进(起, .25f);
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
            for (int i = 0; i < 6; i++) 战.推进(起, .25f);
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
            for(int i=0;i<5;i++){战.尝试释放技能(2,Vector2.up,地.出生位置);for(int n=0;n<6;n++)战.推进(地.出生位置,.25f);}
            检查("余响-五次手动施放追加一次且继承方向通路", 战.普通释放次数==5&&战.回响次数==1&&方向对&&战.当前通路==4&&战.通路释放次数[4]==5);
        }
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
        游戏.界面.跳过新手指引(); 场.enabled = false;
        游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return null; yield return new WaitForEndOfFrame();
        int 预期技能槽 = Enumerable.Range(0, 6).Count(i => 场.战斗.技能有链路(i));
        检查("HUD-技能槽按实际链路显示", 游戏.GetComponentsInChildren<Button>().Count(x => x.name.StartsWith("技能") && x.name.Length == 3) == 预期技能槽);
        检查("HUD-无链路技能卡隐藏且不拦截射线", 游戏.GetComponentsInChildren<Button>(true).Where(x => x.name.StartsWith("技能") && x.name.Length == 3 && !场.战斗.技能有链路(int.Parse(x.name.Substring(2)) - 1)).All(x => !x.gameObject.activeSelf && !x.GetComponent<CanvasGroup>().blocksRaycasts));
        检查("HUD-资源球为独立素材动态液位", 游戏.GetComponentsInChildren<Image>().Count(x => x.name == "真实液位" && x.sprite != null && x.type == Image.Type.Filled) == 2);
        检查("HUD-不可用技能只整卡半透明", 游戏.GetComponentsInChildren<Button>().Where(x => x.name.StartsWith("技能") && x.name.Length == 3 && !x.interactable).All(x => x.GetComponent<CanvasGroup>().alpha == .5f));
        yield return 拍("战斗01_PC真实存档HUD");
        var 人 = 游戏.主角属性; var 战 = 场.战斗; 战.自动攻击启用 = false;
        Vector2 发射向 = Vector2.zero; 战.射击释放 += 向 => 发射向 = 向;
        float 血 = 人.当前血量;
        场.设置触控瞄准(Vector2.up);
        var 首个技能 = 游戏.GetComponentsInChildren<Button>().First(x => x.name.StartsWith("技能") && x.name.Length == 3 && x.interactable);
        int 首槽 = int.Parse(首个技能.name.Substring(2)) - 1;
        float 灵 = 人.当前灵力; 点(首个技能);
        检查("PC-实际按钮点击触发施法与扣灵力", 人.当前灵力 == 灵 - 天帝战斗系统.技能灵力消耗);
        场.战斗一步(.225f);
        检查("PC-按钮前摇后真发射", 战.普通释放次数 == 1);
        场.战斗一步(.25f); 场.战斗一步(.25f); 场.战斗一步(.25f); 场.战斗一步(.25f);
        var 鼠 = InputSystem.AddDevice<Mouse>(); var 键 = InputSystem.AddDevice<Keyboard>();
        try
        {
            Vector2 屏幕 = 场.俯视相机.WorldToScreenPoint(new Vector3(场.玩家位置.x + 6, 0, 场.玩家位置.y));
            InputSystem.QueueStateEvent(鼠, new MouseState { position = 屏幕 }); InputSystem.QueueStateEvent(键, new KeyboardState((Key)((int)Key.Digit1 + 首槽))); InputSystem.Update();
            私调(场, "读取主动操作"); 场.战斗一步(.225f);
            检查("PC-真实InputSystem数字键朝鼠标发射", 战.普通释放次数 == 2 && Vector2.Dot(发射向, Vector2.right) > .999f);
            System.IO.File.WriteAllText(System.IO.Path.Combine(目录,"inputdiagnostic.txt"), "数字键后释放次数="+战.普通释放次数+" 事件方向="+发射向+" 鼠标="+鼠.position.ReadValue()+" 瞄准="+场.瞄准方向);
            InputSystem.QueueStateEvent(键, new KeyboardState()); InputSystem.Update();
            InputSystem.QueueStateEvent(键, new KeyboardState(Key.Space)); InputSystem.Update(); var 前 = 场.玩家位置; 私调(场, "读取主动操作"); 场.战斗一步(.1f);
            检查("PC-真实空格键朝鼠标三米闪避", Mathf.Abs(Vector2.Distance(场.玩家位置, 前) - 3) < .01f && Mathf.Abs(场.玩家位置.y - 前.y) < .01f);
            InputSystem.QueueStateEvent(键, new KeyboardState()); InputSystem.Update();
        }
        finally { InputSystem.RemoveDevice(键); InputSystem.RemoveDevice(鼠); }
        for (int i = 0; i < 6; i++) 场.战斗一步(.25f);
        人.设置当前资源(人.当前血量, 0, 人.当前灵气护盾); 游戏.界面.更新战斗状态();
        int 声前 = UnityEngine.Object.FindAnyObjectByType<天帝声音>().音效播放次数;
        检查("反馈-不足灵力真实操作拒绝", 场.释放技能(首槽) == 战斗操作结果.灵力不足 && 人.当前灵力 == 0);
        检查("反馈-文字声音视觉均来自失败事件", 游戏.GetComponentsInChildren<Text>().Any(x => x.name == "技能失败提示" && x.text == "灵力不足，无法释放") && UnityEngine.Object.FindAnyObjectByType<天帝声音>().音效播放次数 > 声前);
        yield return 拍("战斗02_PC灵力不足反馈");
        游戏.界面.切换战斗暂停(); yield return null;
        float 暂灵 = 人.当前灵力, 暂冷 = 战.技能冷却剩余; 场.战斗一步(.25f);
        检查("暂停-拒绝操作冻结回复冷却", 场.释放技能(首槽) == 战斗操作结果.无法操作 && 场.闪避() == 战斗操作结果.无法操作 && 人.当前灵力 == 暂灵 && 战.技能冷却剩余 == 暂冷);
        yield return 拍("战斗03_PC暂停说明"); 游戏.界面.关闭战斗暂停();
        人.设置当前资源(人.当前血量, 人.灵力, 人.当前灵气护盾);
        bool? 移原 = 天帝移动适配.验证移动平台;
        try
        {
            天帝移动适配.验证移动平台 = true; 游戏.界面.显示战斗(); 游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return null; yield return new WaitForEndOfFrame();
            检查("移动-HUD左摇杆与右侧技能手势", 游戏.界面.战斗摇杆 != null && 游戏.GetComponentsInChildren<天帝战斗触控技能>().Length == 预期技能槽);
            var 圆技能 = 游戏.GetComponentsInChildren<Transform>().Where(x => x.name == "圆形技能青玉底").ToArray();
            检查("移动-技能圆形按钮两行布局", 圆技能.Length == 预期技能槽 && 圆技能.All(x => ((RectTransform)x.parent).rect.width >= 55 && ((RectTransform)x.parent).rect.height >= 55));
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
            ExecuteEvents.Execute(手势.gameObject, e, ExecuteEvents.pointerUpHandler); 场.战斗一步(.225f);
            检查("移动-松手真实施法", 战.普通释放次数 == 发前 + 1);
            yield return 拍("战斗04_移动端摇杆技能HUD");
            ExecuteEvents.Execute(手势.gameObject, e, ExecuteEvents.pointerDownHandler); 游戏.界面.切换战斗暂停(); 游戏.界面.关闭战斗暂停();
            for(int i=0;i<6;i++)场.战斗一步(.25f);
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
        yield return 拍("战斗05_返回主页原美术保留");
    }
}
#endif
