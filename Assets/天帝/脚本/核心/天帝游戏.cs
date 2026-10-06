using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.SceneManagement;

public enum 游戏阶段 { 标题, 序章, 主页, 道纹, 源道纹选择, 道纹改造, 战斗加载, 战斗 }

public class 天帝游戏 : MonoBehaviour
{
    public const string 全名 = "从参加聚光灯比赛到我为天帝镇压世间一切";
    [Header("界面资源")]
    public Font 默认字体;
    public Sprite 主页背景;
    public Sprite 主角立绘;
    public 天帝美术资源 美术;
    public AudioClip 主页音乐;
    [Header("拾取提示：每条滑入后完整停留的秒数")]
    [Min(.1f)] public float 拾取提示停留秒 = 天帝拾取提示.默认停留秒;
    [Header("序章：新版固定分格漫画；旧视频引用仅保留兼容")]
    public VideoClip 开场视频;
    [Header("主角属性：默认使用游戏数值配置，测试值须显式开启")]
    public 主角属性配置 主角初始属性 = new 主角属性配置();
    [Header("战斗场景成功进入后的兼容事件")]
    public UnityEvent 新玩法开始 = new UnityEvent();
    public 游戏阶段 阶段 { get; private set; }
    public 天帝界面 界面 { get; private set; }
    public float 音量 { get; private set; }
    public float 音乐音量 { get; private set; }
    public float 音效音量 { get; private set; }
    public float 剧情音量 { get; private set; }
    public bool 字幕开启 { get; private set; }
    public bool 可继续游戏 => 当前存档 != null;
    public bool 有旧存档 => 存档 != null && 存档.有文件;
    public string 存档提示 => 存档?.提示 ?? "";
    public bool 序章已解锁 => PlayerPrefs.GetInt("Tiandi.Menu.PrologueSeen", 0) == 1 || 当前存档?.序章已完成 == true;
    public bool 序章已暂停 => 序章 != null && 序章.已暂停;
    public 天帝道纹 道纹数据 { get; private set; }
    public 天帝通货 通货数据 { get; private set; }
    public 天帝宝盒 宝盒数据 { get; private set; }
    public 天帝主角属性 主角属性 { get; private set; }
    public 天帝天赋池 天赋池 { get; private set; }
    public 天赋定义 当前天赋 => 天赋池?.已选天赋;
    public 天帝余响计数 余响计数 { get; private set; }
    public int 初始源道纹编号 { get; private set; } = -1;
    public 天帝战斗场景 战斗场景 { get; private set; }
    public int 当前地图编号 { get; private set; }
    public int 当前地图等级 { get; private set; } = 1;
    public 战斗难度 当前战斗难度 { get; private set; }
    Camera[] 主页相机;
    bool[] 相机原状态;
    public bool 设置玩家等级(int 等级) => 道纹数据 != null && 道纹数据.设置玩家等级(等级);
    const string 音量键 = "Tiandi.Menu.Volume";
    public 天帝声音 声音 { get; private set; }
    天帝序章 序章;
    天帝存档 存档;
    天帝存档数据 当前存档;
    bool 当前进度可保存, 待保存, 重看中;
    游戏阶段 重看返回阶段;
    float 保存等待, 原监听音量;

    void Awake()
    {
        天帝美术资源.当前 = 美术;
        if (美术 != null) { 主页背景 = 美术.获取("BG01") ?? 主页背景; 主角立绘 = 美术.获取("CH01") ?? 主角立绘; }
        Application.targetFrameRate = 60;
        天帝移动适配.设置横屏();
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var 输入 = new GameObject("界面输入", typeof(EventSystem), typeof(InputSystemUIInputModule));
            输入.transform.SetParent(transform, false);
        }
        音量 = Mathf.Clamp01(PlayerPrefs.GetFloat(音量键, 0.6f));
        音乐音量 = Mathf.Clamp01(PlayerPrefs.GetFloat("Tiandi.Menu.Music", 1));
        音效音量 = Mathf.Clamp01(PlayerPrefs.GetFloat("Tiandi.Menu.Effects", 1));
        剧情音量 = Mathf.Clamp01(PlayerPrefs.GetFloat("Tiandi.Menu.Story", 1));
        字幕开启 = PlayerPrefs.GetInt("Tiandi.Menu.Subtitles", 1) != 0;
        原监听音量 = AudioListener.volume; AudioListener.volume = 音量;
        存档 = new 天帝存档(); 当前存档 = 存档.读取();
        声音 = gameObject.AddComponent<天帝声音>(); 声音.初始化(this);
        序章 = gameObject.AddComponent<天帝序章>();
        界面 = new 天帝界面(this);
        阶段 = 游戏阶段.标题; 界面.显示标题();
        声音.同步();
    }
    void Update()
    {
        界面?.更新适配();
        界面?.更新拾取提示(Time.unscaledDeltaTime);
        if (阶段 == 游戏阶段.序章) 界面.更新序章进度(序章.当前秒, 序章.总秒, 序章.可调整进度, 序章.播放状态);
        if (待保存 && 当前进度可保存)
        {
            保存等待 -= Time.unscaledDeltaTime;
            if (保存等待 <= 0 && !保存进度()) 保存等待 = 5;
        }
        var 键 = Keyboard.current;
        if (天帝移动适配.启用)
        {
            // Android返回键复用触屏返回流程，其余键盘快捷键只属于PC。
            if (键 != null && 键.escapeKey.wasPressedThisFrame) 触屏返回();
            return;
        }
        if (键 == null) return;
        if (键.escapeKey.wasPressedThisFrame)
        {
            if (界面.关闭等级下拉()) return;
            if (界面.确认已打开) 界面.关闭确认();
            else if (界面.设置已打开) 界面.关闭设置();
            else if (界面.地图已打开) 界面.关闭地图选择();
            else if (界面.角色已打开) 界面.关闭角色();
            else if (界面.图鉴已打开) 界面.关闭图鉴();
            else if (界面.宝盒已打开) { if(界面.宝盒概率已打开)界面.关闭宝盒概率();else 界面.关闭宝盒(); }
            else if (界面.回收已打开) { if(界面.回收页!=null&&界面.回收页.确认已打开)界面.回收页.关闭确认();else 界面.关闭回收(); }
            else if (阶段 == 游戏阶段.序章) 序章.跳过();
            else if (阶段 == 游戏阶段.源道纹选择)
            { if (界面.源道纹页 != null && 界面.源道纹页.详情已打开) 界面.源道纹页.关闭选中详情(); else 返回标题(); }
            else if (阶段 == 游戏阶段.标题 || 阶段 == 游戏阶段.主页) 界面.显示设置();
            else if (阶段 == 游戏阶段.战斗)
            { if (战斗场景?.战斗?.玩家死亡 == true) 返回主页(); else 界面.切换战斗暂停(); }
            else if (阶段 == 游戏阶段.道纹改造)
            {
                if (界面.改造页 != null && 界面.改造页.背包已打开) 界面.改造页.关闭背包();
                else 返回主页();
            }
            else if (阶段 == 游戏阶段.道纹)
            {
                if (界面.道纹页 != null && 界面.道纹页.筛选已打开) 界面.道纹页.关闭筛选();
                else if (界面.道纹页 != null && 界面.道纹页.拖动中) 界面.道纹页.取消拖动();
                else 返回主页();
            }
        }
        else if (阶段 == 游戏阶段.标题 && 键.enterKey.wasPressedThisFrame && !界面.设置已打开 && !界面.确认已打开)
        { if (可继续游戏) 继续游戏(); else 开始序章(); }
        else if (阶段 == 游戏阶段.序章 && 键.spaceKey.wasPressedThisFrame) 切换序章暂停();
    }
    public void 开始序章()
    {
        if (阶段 != 游戏阶段.标题) return;
        if (界面.设置已打开 || 界面.确认已打开) return;
        if (有旧存档) { 界面.显示新游戏确认(); return; }
        确认开始新游戏();
    }
    public void 确认开始新游戏()
    {
        if (阶段 != 游戏阶段.标题) return;
        界面.关闭确认(); 当前进度可保存 = 待保存 = false; 重看中 = false;
        阶段 = 游戏阶段.序章;
        播放序章内容();
    }
    public void 跳过序章() { if (阶段 == 游戏阶段.序章) 序章.跳过(); }
    public bool 调整序章进度(float 比例) => 阶段 == 游戏阶段.序章 && 序章.设置进度(比例);
    public void 序章翻格(int 方向) { if (阶段 == 游戏阶段.序章) 序章.翻格(方向); }
    public void 切换序章暂停() { if (阶段 == 游戏阶段.序章) 序章.切换暂停(); }
    public void 重看序章()
    {
        if (!序章已解锁 || (阶段 != 游戏阶段.标题 && 阶段 != 游戏阶段.主页)) return;
        界面.关闭设置(); 重看返回阶段 = 阶段; 重看中 = true;
        阶段 = 游戏阶段.序章;
        播放序章内容();
    }
    void 播放序章内容()
    {
        var 屏幕 = 界面.显示序章(false);
        序章.播放(开场视频, 屏幕, 剧情音量 * .85f, 界面.更新序章文字, 序章结束);
    }
    void 序章结束()
    {
        if (阶段 != 游戏阶段.序章) return;
        PlayerPrefs.SetInt("Tiandi.Menu.PrologueSeen", 1); 保存设置();
        if (!重看中) { 进入源道纹选择(); return; }
        序章.释放保留画面();
        重看中 = false; 阶段 = 重看返回阶段;
        if (阶段 == 游戏阶段.主页) 界面.显示主页(); else 界面.显示标题();
        声音.同步();
    }
    public bool 继续游戏()
    {
        if (阶段 != 游戏阶段.标题 || 界面.设置已打开 || 界面.确认已打开) return false;
        var 数据 = 存档.读取(); 当前存档 = 数据;
        if (数据 == null) { 界面.显示标题(); 界面.显示消息(存档提示.Length > 0 ? 存档提示 : "没有可继续的存档。"); return false; }
        var 网 = 天帝道纹.读取存档(数据.画布);
        var 钱 = new 天帝通货(网, System.Environment.TickCount); 钱.读取库存(数据.通货, 数据.无限通货);
        var 盒 = new 天帝宝盒(网, System.Environment.TickCount, 数据.灵石, 数据.无限灵石);
        var 人 = new 天帝主角属性(数据.主角, 网);
        释放角色(); 道纹数据 = 网; 通货数据 = 钱; 宝盒数据 = 盒; 主角属性 = 人;
        天赋池 = 天帝天赋池.从已选天赋恢复(数据.画布.天赋编号); 初始源道纹编号 = 数据.画布.天赋编号;
        余响计数 = new 天帝余响计数(当前天赋); 当前地图编号 = 数据.地图编号;
        当前地图等级 = Mathf.Clamp(数据.地图等级, 1, 100); 当前战斗难度 = 战斗难度.普通;
        当前进度可保存 = true; 待保存 = false; 订阅角色();
        阶段 = 游戏阶段.主页; 界面.显示主页(); return true;
    }
    void 进入源道纹选择()
    {
        if (阶段 != 游戏阶段.序章) return;
        天赋池 = new 天帝天赋池(System.Environment.TickCount);
        bool 衔接序章 = 序章.可衔接道纹选择;
        阶段 = 游戏阶段.源道纹选择; 界面.显示源道纹选择(衔接序章);
        if (衔接序章) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        声音.同步();
    }
    public bool 选择源道纹(int 编号)
    {
        // 兼容旧入口名称；编号现在表示当前五选一卡片槽位。
        if (天赋池 == null || 编号 < 0 || 编号 >= 天赋池.候选.Count) return false;
        return 选择天赋(天赋池.候选[编号].编号, 天赋池.轮次);
    }
    public bool 刷新天赋()
    {
        if (阶段 != 游戏阶段.源道纹选择 || 界面.源道纹页 == null || !界面.源道纹页.可选择 || 天赋池 == null || !天赋池.刷新()) return false;
        界面.源道纹页.刷新候选(); return true;
    }
    public bool 选择天赋(int 编号, int 轮次)
    {
        if (阶段 != 游戏阶段.源道纹选择 || 界面.源道纹页 == null || !界面.源道纹页.可选择 || 天赋池 == null || !天赋池.确认(编号, 轮次)) return false;
        初始源道纹编号 = 编号;
        天帝声音.提示("YS06_天赋确认");
        释放角色();
        道纹数据 = new 天帝道纹(System.Environment.TickCount, 天赋池.已选天赋);
        通货数据 = new 天帝通货(道纹数据, System.Environment.TickCount);
        宝盒数据 = new 天帝宝盒(道纹数据, System.Environment.TickCount, 天帝宝盒.开局灵石);
        主角属性 = new 天帝主角属性(主角初始属性?.使用测试数据 == true ? 主角初始属性 : 天帝普攻.主角配置(), 道纹数据);
        订阅角色(); 当前地图编号 = 0; 当前地图等级 = 1; 当前战斗难度 = 战斗难度.普通;
        余响计数 = new 天帝余响计数(当前天赋);
        序章.释放保留画面();
        阶段 = 游戏阶段.主页; 界面.显示主页();
        当前进度可保存 = true; 标记待保存();
        if (!保存进度()) 界面.显示消息(存档提示);
        return true;
    }
    public void 开始新玩法()
    {
        进入战斗();
    }
    public bool 兑换作弊码(string 代码, out string 提示)
    {
        if (阶段 != 游戏阶段.主页) { 提示 = "请在主页使用作弊码。"; return false; }
        if (!天帝作弊码.兑换(代码, 道纹数据, 宝盒数据, 通货数据, out 提示)) return false;
        标记待保存();
        if (!保存进度()) 提示 += "\n进度尚未保存：" + 存档提示;
        return true;
    }
    public bool 选择战斗地图(int 编号)
    {
        if (阶段 != 游戏阶段.主页 || 编号 != 0 || 界面.确认已打开 || 界面.设置已打开 || 界面.角色已打开 || 界面.图鉴已打开 || 界面.宝盒已打开 || 界面.回收已打开) return false;
        if (当前地图编号 == 编号) return true;
        当前地图编号 = 编号; 标记待保存(); return true;
    }
    public bool 选择地图等级(int 等级)
    {
        if (阶段 != 游戏阶段.主页 || !天帝地图挑战.等级有效(等级) || 界面.确认已打开 || 界面.设置已打开 || 界面.角色已打开 || 界面.图鉴已打开 || 界面.宝盒已打开 || 界面.回收已打开) return false;
        if (当前地图等级 == 等级 && 当前战斗难度 == 战斗难度.普通) return true;
        当前地图等级 = 等级; 当前战斗难度 = 战斗难度.普通; 标记待保存(); return true;
    }
    public bool 选择战斗难度(战斗难度 难度)
    {
        if (阶段 != 游戏阶段.主页 || 界面.设置已打开 || 界面.宝盒已打开 || !界面.地图已打开 || (难度 != 战斗难度.普通 && 难度 != 战斗难度.困难)) return false;
        当前战斗难度 = 难度; 标记待保存(); return true;
    }
    public bool 进入战斗()
    {
        if (阶段 != 游戏阶段.主页 || 界面.确认已打开 || 界面.设置已打开 || 界面.地图已打开 || 界面.角色已打开 || 界面.图鉴已打开 || 界面.宝盒已打开 || 界面.回收已打开 || 主角属性 == null) return false;
        if (!Application.CanStreamedLevelBeLoaded(天帝战斗地图.场景路径))
        { 界面.显示战斗错误("2D战斗地图尚未配置，请运行“天帝/初始化战斗场景”。"); return false; }
        阶段 = 游戏阶段.战斗加载; 界面.显示战斗加载("正在前往青岚原……");
        StartCoroutine(加载战斗()); return true;
    }
    IEnumerator 加载战斗()
    {
        主页相机 = FindObjectsByType<Camera>(FindObjectsSortMode.None); 相机原状态 = new bool[主页相机.Length];
        for (int i = 0; i < 主页相机.Length; i++) 相机原状态[i] = 主页相机[i].enabled;
        AsyncOperation 操作 = null; string 错误 = null;
        try { 操作 = SceneManager.LoadSceneAsync(天帝战斗地图.场景路径, LoadSceneMode.Additive); }
        catch (System.Exception ex) { 错误 = ex.Message; }
        if (操作 != null) yield return 操作;
        var 场景 = SceneManager.GetSceneByPath(天帝战斗地图.场景路径);
        try
        {
            if (!场景.IsValid() || !场景.isLoaded) throw new System.InvalidOperationException(错误 ?? "地图加载失败");
            foreach (var 根 in 场景.GetRootGameObjects()) { 战斗场景 = 根.GetComponentInChildren<天帝战斗场景>(); if (战斗场景 != null) break; }
            if (战斗场景 == null) throw new System.InvalidOperationException("地图缺少场景入口");
            战斗场景.初始化(this);
        }
        catch (System.Exception ex) { 错误 = ex.Message; }
        if (错误 != null)
        {
            if (场景.IsValid() && 场景.isLoaded) yield return SceneManager.UnloadSceneAsync(场景);
            战斗场景 = null; 恢复主页相机(); 阶段 = 游戏阶段.主页; 界面.显示主页(); 界面.显示战斗错误(错误); yield break;
        }
        foreach (var 相机 in 主页相机) if (相机 != null) 相机.enabled = false;
        阶段 = 游戏阶段.战斗; 声音.同步(); 界面.显示战斗();
        新玩法开始?.Invoke();
    }
    IEnumerator 退出战斗()
    {
        阶段 = 游戏阶段.战斗加载; 界面.显示战斗加载("正在返回主页……");
        var 场景 = SceneManager.GetSceneByPath(天帝战斗地图.场景路径);
        if (场景.IsValid() && 场景.isLoaded) yield return SceneManager.UnloadSceneAsync(场景);
        战斗场景 = null; 恢复主页相机(); 阶段 = 游戏阶段.主页; 界面.显示主页(); 保存进度();
        声音.同步();
    }
    void 恢复主页相机()
    {
        if (主页相机 != null) for (int i = 0; i < 主页相机.Length; i++) if (主页相机[i] != null) 主页相机[i].enabled = 相机原状态[i];
        主页相机 = null; 相机原状态 = null;
    }
    void 更新实力评语() { if (阶段 == 游戏阶段.主页) 界面?.更新主页实力(); }
    public void 打开道纹()
    {
        if (阶段 != 游戏阶段.主页 || 界面.确认已打开 || 界面.设置已打开 || 界面.地图已打开 || 界面.角色已打开 || 界面.图鉴已打开 || 界面.宝盒已打开 || 界面.回收已打开 || 初始源道纹编号 < 0 || 道纹数据 == null) return;
        阶段 = 游戏阶段.道纹; 界面.显示道纹(道纹数据);
    }
    public void 打开道纹改造()
    {
        if (阶段 != 游戏阶段.主页 || 界面.确认已打开 || 界面.设置已打开 || 界面.地图已打开 || 界面.角色已打开 || 界面.图鉴已打开 || 界面.宝盒已打开 || 界面.回收已打开 || 道纹数据 == null || 通货数据 == null) return;
        阶段 = 游戏阶段.道纹改造; 界面.显示道纹改造(道纹数据, 通货数据);
    }
    public void 返回主页()
    {
        if (阶段 == 游戏阶段.战斗) { StartCoroutine(退出战斗()); return; }
        if (阶段 != 游戏阶段.道纹 && 阶段 != 游戏阶段.道纹改造) return;
        阶段 = 游戏阶段.主页; 界面.显示主页(); 保存进度();
    }
    void 订阅角色()
    {
        主角属性.属性改变 += 更新实力评语; 主角属性.基础配置改变 += 标记待保存;
        道纹数据.状态改变 += 标记待保存; 通货数据.数量改变 += 标记待保存; 宝盒数据.余额改变 += 标记待保存;
    }
    void 释放角色()
    {
        if (主角属性 != null) { 主角属性.属性改变 -= 更新实力评语; 主角属性.基础配置改变 -= 标记待保存; 主角属性.Dispose(); }
        if (道纹数据 != null) 道纹数据.状态改变 -= 标记待保存;
        if (通货数据 != null) 通货数据.数量改变 -= 标记待保存;
        if (宝盒数据 != null) 宝盒数据.余额改变 -= 标记待保存;
    }
    void 标记待保存() { if (!当前进度可保存) return; if (!待保存) 保存等待 = .6f; 待保存 = true; }
    public bool 保存进度()
    {
        if (!当前进度可保存 || 道纹数据 == null || 通货数据 == null || 宝盒数据 == null || 主角属性 == null) return true;
        if (!待保存 && 当前存档 != null) return true;
        var 数据 = new 天帝存档数据 { 序章已完成 = true, 主角 = 主角属性.导出配置(), 画布 = 道纹数据.导出存档(),
            通货 = 通货数据.导出库存(), 无限通货 = 通货数据.无限通货, 灵石 = 宝盒数据.灵石, 无限灵石 = 宝盒数据.无限灵石, 地图编号 = 当前地图编号, 地图等级 = 当前地图等级, 难度 = 当前战斗难度 };
        if (!存档.保存(数据)) { 界面?.更新存档状态(存档提示); return false; }
        当前存档 = 数据; 待保存 = false; 界面?.更新存档状态(""); return true;
    }
    public void 返回标题()
    {
        if (阶段 != 游戏阶段.主页 && 阶段 != 游戏阶段.源道纹选择) return;
        if (!保存进度()) { 界面.显示消息(存档提示); return; }
        序章.释放保留画面();
        当前进度可保存 = false; 待保存 = false; 界面.关闭设置(); 当前存档 = 存档.读取(); 阶段 = 游戏阶段.标题; 界面.显示标题();
    }
    public void 退出游戏()
    {
        if (!保存进度()) { 界面.显示消息(存档提示); return; }
        保存设置();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void 设置音量(float 值)
    {
        音量 = Mathf.Clamp01(值); AudioListener.volume = 音量;
        PlayerPrefs.SetFloat(音量键, 音量);
    }
    public void 设置音乐音量(float 值)
    { 音乐音量 = Mathf.Clamp01(值); 声音?.同步(); PlayerPrefs.SetFloat("Tiandi.Menu.Music", 音乐音量); }
    public void 设置音效音量(float 值)
    { 音效音量 = Mathf.Clamp01(值); 声音?.更新音效音量(); PlayerPrefs.SetFloat("Tiandi.Menu.Effects", 音效音量); }
    public void 设置剧情音量(float 值)
    { 剧情音量 = Mathf.Clamp01(值); 序章?.设置音量(剧情音量 * .85f); PlayerPrefs.SetFloat("Tiandi.Menu.Story", 剧情音量); }
    public void 设置字幕(bool 开启)
    { 字幕开启 = 开启; PlayerPrefs.SetInt("Tiandi.Menu.Subtitles", 开启 ? 1 : 0); 界面?.更新字幕显示(); }
    public void 播放音效(AudioClip 片) { 声音?.播放(片); }
    public void 保存设置() => PlayerPrefs.Save();
    public void 触屏返回()
    {
        if (界面.关闭等级下拉()) return;
        if (界面.作弊码已打开) 界面.关闭作弊码();
        else if (界面.确认已打开) 界面.关闭确认();
        else if (界面.设置已打开) 界面.关闭设置();
        else if (界面.地图已打开) 界面.关闭地图选择();
        else if (界面.角色已打开) 界面.关闭角色();
        else if (界面.图鉴已打开) 界面.关闭图鉴();
        else if (界面.宝盒已打开) { if (界面.宝盒概率已打开) 界面.关闭宝盒概率(); else 界面.关闭宝盒(); }
        else if (界面.回收已打开) { if (界面.回收页?.确认已打开 == true) 界面.回收页.关闭确认(); else 界面.关闭回收(); }
        else if (阶段 == 游戏阶段.道纹)
        { if (界面.道纹页.筛选已打开) 界面.道纹页.关闭筛选(); else if (界面.道纹页.触屏选中道纹 != null || 界面.道纹页.拖动中) 界面.道纹页.取消触屏选择(); else 返回主页(); }
        else if (阶段 == 游戏阶段.道纹改造)
        { if (界面.改造页.背包已打开) 界面.改造页.关闭背包(); else 返回主页(); }
        else if (阶段 == 游戏阶段.源道纹选择)
        { if (界面.源道纹页.详情已打开) 界面.源道纹页.关闭选中详情(); else 返回标题(); }
        else if (阶段 == 游戏阶段.战斗 && 战斗场景?.战斗?.玩家死亡 != true) 界面.关闭战斗暂停();
    }
    void OnApplicationFocus(bool 焦点) { 序章?.设置焦点(焦点); if (!焦点) { 界面?.移动失去焦点(); if (待保存) 保存进度(); } }
    void OnApplicationPause(bool 暂停) { 序章?.设置后台(暂停); if (暂停) { 界面?.移动失去焦点(); 保存进度(); 保存设置(); } }
    void OnApplicationQuit() { 保存进度(); 保存设置(); }
    void OnDestroy() { if (待保存) 保存进度(); 释放角色(); AudioListener.volume = 原监听音量; 界面?.销毁(); if (天帝美术资源.当前 == 美术) 天帝美术资源.当前 = null; }
}
