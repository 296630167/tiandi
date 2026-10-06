using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// 固定漫画入口，兼容旧场景的VideoClip参数但不播放旧视频。
public class 天帝序章 : MonoBehaviour
{
    public const float 每格秒 = 6.5f;
    public static readonly string[] 页标题 = {
        "午后 · 还没成为主角", "放题 · 涌现", "灵感 · 世界多走了一步", "失重 · 这不是比赛特效",
        "异乡 · 先确认自己还活着", "问路 · 风里的一句话", "道纹 · 像一张会呼吸的网", "起点 · 五条可能的人生"
    };
    public static readonly string[] 叙述 = {
        "午后三点，我给自己报了个游戏开发比赛。理由很朴素：总不能一辈子只替别人的故事修 Bug。",
        "报名成功。文件夹建好了，名字也起好了。至于游戏做什么……先等聚光灯放题。",
        "咖啡从烫嘴放到了温热。我盯着倒计时，第一次觉得，等一个词比写一百行代码还难。",
        "主题终于出现——「涌现」。我读了一遍，又读一遍。这词很大，我的脑子暂时很空。",
        "很多简单的东西凑在一起，竟会长出谁都没预料到的结果。蚁群、城市……还有游戏？",
        "我闭上眼睛，手指在头顶画圈。要是每个小小的选择，都能连接出一条不同的路呢？",
        "纸上落下几个点。我把它们连起来：规则不必复杂，变化可以由连接自己生长。",
        "头顶忽然传来一声轻响。咖啡浮起来了。我愣了两秒：这个灵感的反馈，是不是太真实了？",
        "屋里的线条开始弯曲，纸张奔向半空。我抓住桌沿——等等，我还没保存！",
        "桌沿从指尖滑走，电脑留在原地。好消息：灵感有了。坏消息：开发者没了。",
        "一阵天旋地转。我想起比赛截止日期，又觉得眼下该先担心，自己有没有落地的地方。",
        "砰。我摔进草丛，腰被石头硌得生疼。疼得如此具体，大概不是梦。",
        "我撑着膝盖坐起来。山是真的，风是真的。至于这里是哪儿，脑子里没有可用的日志。",
        "手机黑着屏，口袋里也没有充电线。电脑没跟来，连导航都没留下——这开局也太干净了。",
        "头顶有人掠过。我刚想喊救命，却看见他们站在剑上。给我干哪来了，这还是国内吗？",
        "「两位！这是什么地方？」我挥手挥得像个人形信号灯。对方低头，看了我一眼。",
        "风里送来一句：「青岚原。天黑前下山，别碰会发光的纹。」话音还在，人已经远了。",
        "我低头。脚边一块断碑，刚好亮了。行，危险提示和危险本身，还真是无缝衔接。",
        "碑上没有字，只有细线和六角纹。手指靠近时，最前面的一个节点先醒了过来。",
        "光沿连接往外走，断开的地方就停住。它不像一句咒语，倒像一张……会呼吸的网。",
        "我还没看懂，云层里已经亮起五道光。五枚石纹缓缓落下，竟在我面前停住。",
        "每枚纹都不一样。有的厚重，有的轻灵，有的安静得像没睡醒。它们似乎都在等我的回应。",
        "我伸出手，又停住。没有哪一枚写着「标准答案」。选了它，接下来就得自己把路连下去。",
        "远处山路还长。我深吸一口气：比赛先欠着吧。现在，先替这个故事，选一个自己的起点。"
    };
    readonly RawImage[] 格 = new RawImage[3];
    readonly Image[] 边 = new Image[3];
    RawImage 屏幕;
    Action<string, string> 字幕;
    Action 完成;
    bool 运行, 暂停, 后台, 失焦, 完结;
    float 秒;
    int 上格 = -1;
    readonly AudioClip[] 配音 = new AudioClip[24];
    readonly float[] 格起点 = new float[25];
    AudioSource 配音源;
    bool 配音暂停;
    public AudioSource 当前配音源 => 配音源;
    public float 格开始秒(int n) => 格起点[Mathf.Clamp(n, 0, 24)];
    public bool 已暂停 => 暂停;
    public bool 可衔接道纹选择 => 完结;
    public string 播放状态 => !运行 ? "结束" : 后台 || 失焦 ? "后台暂停" : 暂停 ? "已暂停" : "漫画序章";
    public double 总秒 => 格起点[24] > 0 ? 格起点[24] : 叙述.Length * 每格秒;
    public double 当前秒 => 秒;
    public int 当前格
    {
        get { for (int n = 叙述.Length - 1; n >= 0; n--) if (秒 >= 格起点[n]) return n; return 0; }
    }
    public bool 可调整进度 => 运行 && !后台 && !失焦;
    public void 设置音量(float v) { if (配音源 != null) 配音源.volume = Mathf.Clamp01(v); }
    void Awake()
    {
        配音源 = gameObject.AddComponent<AudioSource>(); 配音源.playOnAwake = false; 配音源.spatialBlend = 0;
        配音源.priority = 32;
        for (int i = 0; i < 24; i++)
        {
            配音[i] = Resources.Load<AudioClip>("天帝音频/配音/VO" + (i + 1).ToString("00"));
            格起点[i + 1] = 格起点[i] + Mathf.Max(每格秒, (配音[i] != null ? 配音[i].length : 0) + .8f);
        }
        GetComponent<天帝声音>()?.注册配音(配音源, this);
    }
    public void 播放实时演算(object _, RawImage t, Font f, float v, Action<string, string> u, Action e) => 播放(null, t, v, u, e);
    public void 播放(VideoClip _, RawImage t, float v, Action<string, string> u, Action e)
    {
        释放保留画面(); 屏幕 = t; 字幕 = u; 完成 = e;
        秒 = 0; 上格 = -1; 暂停 = false; 完结 = false; 运行 = true;
        设置音量(v);
        if (屏幕 != null)
        {
            屏幕.enabled = false;
            for (int i = 0; i < 3; i++)
            {
                var 框 = new GameObject("漫画格框-" + i, typeof(RectTransform), typeof(Image));
                var 区 = (RectTransform)框.transform; 区.SetParent(屏幕.transform, false);
                区.anchorMin = i == 0 ? new Vector2(0, .5f) : new Vector2((i - 1) * .5f, 0);
                区.anchorMax = i == 0 ? Vector2.one : new Vector2(i * .5f, .5f);
                区.offsetMin = new Vector2(3, 3); 区.offsetMax = new Vector2(-3, -3);
                边[i] = 框.GetComponent<Image>(); 边[i].raycastTarget = false;
                var 画 = new GameObject("漫画格-" + i, typeof(RectTransform), typeof(RawImage));
                var 画区 = (RectTransform)画.transform; 画区.SetParent(区, false);
                画区.anchorMin = Vector2.zero; 画区.anchorMax = Vector2.one;
                画区.offsetMin = new Vector2(3, 3); 画区.offsetMax = new Vector2(-3, -3);
                格[i] = 画.GetComponent<RawImage>(); 格[i].raycastTarget = false;
                格[i].uvRect = i == 0 ? new Rect(0, .5f, 1, .5f) : new Rect((i - 1) * .5f, 0, .5f, .5f);
            }
        }
        更新画面();
    }
    void 更新画面()
    {
        int n = 当前格, 页 = n / 3, 焦点 = n % 3;
        if (上格 != n)
        {
            var 图 = 天帝美术资源.当前?.获取("CM" + (页 + 1).ToString("00"));
            for (int i = 0; i < 3; i++) if (格[i] != null) 格[i].texture = 图 != null ? 图.texture : null;
            字幕?.Invoke("序章  " + (页 + 1).ToString("00") + " / 08  ·  " + 页标题[页] + "  ·  第 " + (焦点 + 1) + " 格", 叙述[n]);
            上格 = n;
            同步配音(true);
        }
        float 出现 = Mathf.SmoothStep(0, 1, Mathf.Clamp01((秒 - 格起点[n]) / .65f));
        for (int i = 0; i < 3; i++)
        {
            if (格[i] == null) continue;
            格[i].enabled = i <= 焦点 && 格[i].texture != null;
            float 亮度 = i < 焦点 ? .82f : 1;
            格[i].color = new Color(亮度, 亮度, 亮度, i == 焦点 ? 出现 : 1);
            边[i].color = i == 焦点 ? new Color(.78f, .67f, .43f) : new Color(.22f, .30f, .28f);
        }
    }
    void 同步配音(bool 定位 = false)
    {
        if (配音源 == null) return;
        if (!运行) { 配音源.Stop(); 配音暂停 = false; return; }
        if (定位)
        {
            配音源.Stop(); 配音暂停 = false; 配音源.clip = 配音[当前格];
            float 片秒 = 秒 - 格起点[当前格];
            if (配音源.clip != null && 片秒 < 配音源.clip.length)
            {
                配音源.timeSamples = Mathf.Clamp(Mathf.RoundToInt(片秒 * 配音源.clip.frequency), 0, 配音源.clip.samples - 1);
                配音源.Play();
            }
        }
        bool 停 = 暂停 || 后台 || 失焦;
        if (停 && !配音暂停) { 配音源.Pause(); 配音暂停 = true; }
        else if (!停 && 配音暂停) { 配音源.UnPause(); 配音暂停 = false; }
    }
    public void 切换暂停() { if (运行) { 暂停 = !暂停; 同步配音(); GetComponent<天帝声音>()?.同步(); } }
    public bool 设置进度(float x)
    {
        if (!可调整进度 || float.IsNaN(x) || float.IsInfinity(x)) return false;
        x = Mathf.Clamp01(x); if (x >= 1) { 跳过(); return true; }
        秒 = (float)总秒 * x; 更新画面(); 同步配音(true); return true;
    }
    public bool 翻格(int 方向)
    {
        if (!可调整进度 || 方向 == 0) return false;
        int 下一格 = 当前格 + (方向 > 0 ? 1 : -1);
        if (下一格 >= 叙述.Length) { 跳过(); return true; }
        秒 = 格起点[Mathf.Max(0, 下一格)]; 上格 = -1; 更新画面(); return true;
    }
    void Update()
    {
        if (!运行 || 后台 || 失焦 || 暂停) return;
        秒 = Mathf.Min(秒 + Time.unscaledDeltaTime, (float)总秒);
        更新画面(); if (秒 >= 总秒) 跳过();
    }
    public void 跳过()
    {
        if (!运行) return;
        运行 = false; 完结 = true; 秒 = (float)总秒;
        同步配音();
        var 回调 = 完成; 完成 = null; 字幕 = null; 回调?.Invoke();
    }
    public void 释放保留画面()
    {
        运行 = false; 完成 = null; 字幕 = null; 完结 = false;
        同步配音();
        for (int i = 0; i < 3; i++)
        {
            if (边[i] != null) { 边[i].gameObject.SetActive(false); Destroy(边[i].gameObject); }
            格[i] = null; 边[i] = null;
        }
        if (屏幕 != null) { 屏幕.texture = null; 屏幕.enabled = false; }
        屏幕 = null;
    }
    public void 设置焦点(bool f) { 失焦 = !f; 同步配音(); }
    public void 设置后台(bool p) { 后台 = p; 同步配音(); }
    void OnDestroy() { 释放保留画面(); }
}
