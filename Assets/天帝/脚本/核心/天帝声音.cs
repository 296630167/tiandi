using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 游戏自己的声音表现层，不参与资源结算和战斗计算。
public sealed class 天帝声音 : MonoBehaviour
{
    public static 天帝声音 当前 { get; private set; }
    public static readonly string[] 音乐名称 = { "BGM01_山门初晴", "BGM02_纹理生长", "BGM03_异乡起点", "BGM04_青岚交锋", "BGM05_山王临阵" };
    public static readonly string[] 音效名称 = { "UI01_点击", "UI02_确认", "UI03_返回", "UI04_拒绝", "DW01_解锁", "DW02_放置", "DW03_旋转", "DW04_卸下", "DW05_接通", "YS01_宝盒开启", "YS02_普通获得", "YS03_高阶获得", "YS04_改造成功", "YS05_回收完成", "YS06_天赋确认", "ZD01_灵力弹", "ZD02_命中", "ZD03_受伤", "ZD04_强敌登场", "ZD05_拾取", "ZD06_升级", "ZD07_胜利", "ZD08_失败" };
    readonly Dictionary<string, AudioClip> 音效 = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> 上次 = new Dictionary<string, float>();
    readonly AudioSource[] 池 = new AudioSource[12];
    readonly float[] 通道增益 = new float[12];
    readonly AudioSource[] 乐 = new AudioSource[2];
    readonly AudioClip[] 音乐 = new AudioClip[5];
    天帝游戏 游戏;
    AudioSource 语音;
    天帝序章 序章;
    int 目标曲 = -1, 主源;
    bool 后台, 失焦, 已停;
    public int 当前音乐编号 => 目标曲;
    public int 音效播放次数 { get; private set; }
    public int 音效池数量 => 池.Length;
    public bool 已暂停 => 已停;
    public bool 配音中 => 语音 != null && 语音.isPlaying;
    public void 初始化(天帝游戏 游戏)
    {
        this.游戏 = 游戏; 当前 = this;
        for (int i = 0; i < 音乐.Length; i++) 音乐[i] = Resources.Load<AudioClip>("天帝音频/音乐/" + 音乐名称[i]);
        if (音乐[0] == null) 音乐[0] = 游戏.主页音乐;
        foreach (string 名 in 音效名称) 音效[名] = Resources.Load<AudioClip>("天帝音频/音效/" + 名);
        for (int i = 0; i < 乐.Length; i++) { 乐[i] = 建源("音乐交叉淡化" + i); 乐[i].loop = true; }
        for (int i = 0; i < 池.Length; i++) 池[i] = 建源("音效通道" + i);
    }
    AudioSource 建源(string 名)
    {
        var g = new GameObject(名); g.transform.SetParent(transform, false);
        var a = g.AddComponent<AudioSource>(); a.playOnAwake = false; a.spatialBlend = 0; a.volume = 0; return a;
    }
    public void 注册配音(AudioSource 源, 天帝序章 漫画) { 语音 = 源; 序章 = 漫画; }
    public void 同步()
    {
        if (游戏 == null) return;
        更新音效音量();
        bool 停 = 后台 || 失焦 || (游戏.阶段 == 游戏阶段.战斗 && 游戏.界面?.战斗已暂停 == true) || (游戏.阶段 == 游戏阶段.序章 && 序章?.已暂停 == true);
        if (停 != 已停)
        {
            foreach (var a in 乐) if (a != null) { if (停) a.Pause(); else a.UnPause(); }
            foreach (var a in 池) if (a != null && 停) a.Stop();
            已停 = 停;
        }
        int 曲 = 游戏.阶段 == 游戏阶段.序章 ? 2 : 游戏.阶段 == 游戏阶段.战斗 || 游戏.阶段 == 游戏阶段.战斗加载 ? (游戏.战斗场景?.战斗?.BOSS已出现 == true ? 4 : 3) : 游戏.阶段 == 游戏阶段.道纹 || 游戏.阶段 == 游戏阶段.道纹改造 || 游戏.阶段 == 游戏阶段.源道纹选择 ? 1 : 0;
        if (曲 != 目标曲)
        {
            目标曲 = 曲; 主源 = 1 - 主源; var a = 乐[主源];
            a.Stop(); a.clip = 音乐[曲]; a.volume = 0;
            if (a.clip != null) { a.Play(); if (已停) a.Pause(); }
        }
        float 音量 = 游戏.音乐音量 * .38f * (配音中 && 游戏.剧情音量 > 0 ? .28f : 1f);
        for (int i = 0; i < 乐.Length; i++)
        {
            var a = 乐[i]; a.volume = Mathf.MoveTowards(a.volume, i == 主源 ? 音量 : 0, Time.unscaledDeltaTime * .65f);
            if (i != 主源 && a.volume <= 0 && a.isPlaying) a.Stop();
        }
    }
    void Update() { 同步(); }
    public bool 播放(string 名)
    {
        return 音效.TryGetValue(名, out var 片) && 播放(片, 名);
    }
    public bool 播放(AudioClip 片, string 标识 = null)
    {
        if (片 == null || 游戏 == null || 后台 || 失焦 || 游戏.音效音量 <= 0 || 游戏.音量 <= 0) return false;
        bool 战斗声 = 标识 != null && 标识.StartsWith("ZD");
        if (战斗声 && 游戏.界面?.战斗已暂停 == true) return false;
        float 间隔 = 标识 == "ZD01_灵力弹" ? .10f : 标识 == "ZD02_命中" ? .125f : 标识 == "ZD05_拾取" || 标识 == "ZD03_受伤" ? .25f : .045f;
        string 键 = 标识 ?? 片.GetInstanceID().ToString(); float 现在 = Time.unscaledTime;
        if (上次.TryGetValue(键, out float 前) && 现在 - 前 < 间隔) return false;
        // 前四路保留给UI与重要提示，普通战斗声音只用后八路。
        bool 普通战斗 = 标识 == "ZD01_灵力弹" || 标识 == "ZD02_命中" || 标识 == "ZD05_拾取" || 标识 == "ZD03_受伤";
        int 起 = 普通战斗 ? 4 : 0;
        for (int i = 起; i < 池.Length; i++) if (!池[i].isPlaying)
        {
            var a = 池[i]; a.clip = 片; a.volume = 游戏.音效音量 * (普通战斗 ? .40f : .70f); a.pitch = 1;
            通道增益[i] = 普通战斗 ? .40f : .70f;
            a.Play(); 更新音效音量(); 上次[键] = 现在; 音效播放次数++; return true;
        }
        return false;
    }
    public void 更新音效音量()
    {
        if (游戏 == null) return;
        float 总 = 0; for (int i = 0; i < 池.Length; i++) if (池[i] != null && 池[i].isPlaying) 总 += 通道增益[i];
        float 系数 = Mathf.Min(1, (配音中 ? .5f : 1.2f) / Mathf.Max(.001f, 总));
        for (int i = 0; i < 池.Length; i++) if (池[i] != null) 池[i].volume = 游戏.音效音量 * 通道增益[i] * 系数;
    }
    public static void 提示(string 名) { 当前?.播放(名); }
    void OnApplicationFocus(bool f) { 失焦 = !f; 同步(); }
    void OnApplicationPause(bool p) { 后台 = p; 同步(); }
    void OnDestroy() { if (当前 == this) 当前 = null; }
}

// 绑定一次到有效按钮提交；关闭面板后也保留返回音。
public sealed class 天帝按钮声音 : MonoBehaviour
{
    public static void 绑定(Button 键)
    {
        if (键 == null || 键.GetComponent<天帝按钮声音>() != null) return;
        var 声 = 键.gameObject.AddComponent<天帝按钮声音>(); 键.onClick.AddListener(声.响应);
    }
    void 响应()
    {
        string 名 = gameObject.name;
        天帝声音.提示(名.Contains("返回") || 名.Contains("取消") || 名.Contains("关闭") ? "UI03_返回" : "UI01_点击");
    }
}
