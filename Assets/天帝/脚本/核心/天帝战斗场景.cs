using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class 天帝战斗场景 : MonoBehaviour
{
    public Material 地图材质;
    [Tooltip("青岚原固定大图；配置后使用按该图描绘的通行边界，不再生成随机迷宫。")]
    public Sprite 地图大图;
    [HideInInspector] public bool 显示移动区域 = false; // 仅兼容旧场景，不再绘制红色填充。
    [HideInInspector] public float 区域显示透明度 = .28f; // 兼容旧场景序列化。
    [Tooltip("沿实际通行边界绘制平滑青绿色发光线，F8切换；不改变碰撞。")]
    public bool 显示边界线 = true;
    MeshRenderer 边界绘制;
    Material 边界材质;
    Material 背景材质;
    // 兼容旧验证夹具与旧场景序列化；纯2D运行时不会赋值或读取。
    public 天帝战斗地图 地图 { get; private set; }
    public Camera 俯视相机 { get; private set; }
    public Vector2 玩家位置 { get; private set; }
    public int 格子数 => 地图 != null ? 地图.地块宽 * 地图.地块高 : 天帝战斗地图.宽 * 天帝战斗地图.高;
    public float 预览移动速度 = 6, 预览跑步速度 = 10;
    public bool 王已击败 { get; private set; }
    public 天帝战斗系统 战斗 { get; private set; }
    public 天帝战斗美术 美术 { get; private set; }
    public bool 可离开 => 王已击败 || (战斗 != null && 战斗.玩家死亡);
    天帝游戏 游戏;
    Transform 主角;
    Mesh 战斗网格;
    Mesh 电光网格;
    Material 电光材质;
    readonly 几何 电光几何 = new 几何();
    readonly 几何 战斗几何 = new 几何();
    bool 已显示死亡;
    int 已播升级次数;
    readonly HashSet<战斗敌人> 已播登场 = new HashSet<战斗敌人>();
    readonly List<Mesh> 网格资源 = new List<Mesh>();
    public void 初始化(天帝游戏 游戏)
    {
        if (this.游戏 != null) return;
        if (地图材质 == null) throw new System.InvalidOperationException("地图材质未配置，请运行天帝/初始化战斗场景。");
        this.游戏 = 游戏;
        显示移动区域 = false; 显示边界线 = true;
        if (游戏.美术 == null || 游戏.美术.青岚原生存大图 == null || 游戏.美术.青岚原生存通行图 == null) throw new System.InvalidOperationException("青岚原生存大图与通行图未接入。");
        地图 = new 天帝战斗地图(System.Environment.TickCount, false, false, 游戏.美术.青岚原生存通行图, 游戏.当前地图等级, true); 玩家位置 = 地图.出生位置;
        bool 使用大图 = 地图.生存大图 || 地图.长卷布局 || 地图.固定图片布局 && 地图大图 != null;
        var 当前大图 = 地图.生存大图 ? 游戏.美术.青岚原生存大图 : 地图.长卷布局 ? 游戏.美术.青岚原长卷 : 地图大图;
        var 地面 = new 几何();
        if (!使用大图) for (int y = 0; y < 地图.地块高; y++) for (int x = 0; x < 地图.地块宽; x++)
        {
            var 中 = 地图.格中心(x, y); var 色 = 地图.地块颜色(x, y);
            地面.方块(中, 0, new Vector2(4, 4), 色 * 0.54f);
            地面.方块(中, 0.01f, new Vector2(3.94f, 3.94f), 色 * .66f);
            var 类 = 地图.地块(x, y);
            if (类 == 战斗地块.林地)
            {
                地面.圆(中 + new Vector2(0.65f, -0.25f), 0.03f, 1.15f, new Color(0.09f, 0.19f, 0.16f), 7);
                地面.圆(中 + new Vector2(-0.5f, 0.5f), 0.04f, 1.1f, new Color(0.19f, 0.34f, 0.23f), 7);
            }
            else if (类 == 战斗地块.水域)
            {
                地面.方块(中 + new Vector2(0.15f, 0.5f), 0.03f, new Vector2(2.5f, 0.07f), new Color(0.28f, 0.54f, 0.58f));
                地面.方块(中 + new Vector2(-0.15f, -0.65f), 0.03f, new Vector2(1.7f, 0.07f), new Color(0.24f, 0.49f, 0.54f));
            }
            else if (类 == 战斗地块.木桥)
                for (int i = 0; i < 5; i++) 地面.方块(中 + new Vector2(-1.6f + i * 0.8f, 0), 0.03f, new Vector2(0.07f, 3.8f), new Color(0.31f, 0.24f, 0.16f));
            else if (类 == 战斗地块.草地 && (x * 7 + y * 11) % 5 == 0)
                地面.圆(中 + new Vector2(0.7f, 0.7f), 0.025f, 0.4f, new Color(0.42f, 0.49f, 0.36f), 5);
        }
        var 地面物 = 建物("青岚原区域地面", 地面, transform);
        if (使用大图)
        {
            var 背景物 = new GameObject("青岚原固定大地图背景", typeof(SpriteRenderer));
            背景物.transform.SetParent(transform, false);
            背景物.transform.position = new Vector3(0, -.03f, 0);
            背景物.transform.rotation = Quaternion.Euler(90, 0, 0);
            var 着色器 = 游戏.美术 != null ? 游戏.美术.立绘着色器 : Shader.Find("天帝/静态立绘");
            背景材质 = new Material(着色器) { name = "青岚原大图与阻挡提示材质" };
            var 背景绘制 = 背景物.GetComponent<SpriteRenderer>(); 背景绘制.sprite = 当前大图; 背景绘制.sortingOrder = -32000;
            背景绘制.sharedMaterial = 背景材质;
            var 尺寸 = 当前大图.bounds.size; 背景物.transform.localScale = new Vector3(地图.半宽 * 2 / Mathf.Max(.01f, 尺寸.x), 地图.半高 * 2 / Mathf.Max(.01f, 尺寸.y), 1);
            if (地图.长卷布局 && 游戏.美术.青岚原高清长卷.Length > 0)
            { 建高清长卷(); 背景绘制.enabled = false; }
            地面物.GetComponent<MeshRenderer>().enabled = false;
            if (地图.长卷布局) 建长卷障碍();
            建发光边界();
        }
        else if (地图.横向区域)
        {
            背景材质 = new Material(游戏.美术 != null ? 游戏.美术.立绘着色器 : Shader.Find("天帝/静态立绘"));
            建发光边界();
        }
        // 首波选点就要读取真实视野，不能等生成敌人之后才创建相机。
        var 相机物 = new GameObject("战斗俯视相机", typeof(Camera)); 相机物.transform.SetParent(transform, false);
        俯视相机 = 相机物.GetComponent<Camera>(); 俯视相机.orthographic = true;
        俯视相机.orthographicSize = 地图.生存大图 ? (float)天帝数值.取("map.arena.camera_half_height") : 14;
        var 屏幕 = 天帝移动适配.屏幕尺寸;
        var 画面 = 天帝移动适配.横屏视口(天帝移动适配.有效安全区(天帝移动适配.安全区, 屏幕));
        if (屏幕.x > 0 && 屏幕.y > 0) 俯视相机.rect = new Rect(画面.x / 屏幕.x, 画面.y / 屏幕.y, 画面.width / 屏幕.x, 画面.height / 屏幕.y);
        俯视相机.clearFlags = CameraClearFlags.SolidColor; 俯视相机.backgroundColor = new Color(0.07f, 0.13f, 0.12f);
        俯视相机.nearClipPlane = 0.1f; 俯视相机.farClipPlane = 100; 俯视相机.depth = 10;
        俯视相机.transform.rotation = Quaternion.Euler(90, 0, 0); 更新相机();
        游戏.主角属性.设置当前资源(游戏.主角属性.血量, 游戏.主角属性.灵力, 游戏.主角属性.灵气护盾);
        战斗 = new 天帝战斗系统(地图, 游戏.道纹数据, 游戏.主角属性, 游戏.当前战斗难度, 游戏.通货数据, 游戏.当前地图等级, 读取战斗视野, 游戏.宝盒数据);
        战斗.敌人死亡 += 敌人死亡;
        战斗.伤害分项反馈 += 命中反馈;
        战斗.射击释放 += 射击声音;
        战斗.掉落.获得道纹 += 道纹声音;
        战斗.通货掉落.获得通货 += 通货声音;
        战斗.灵石掉落.获得灵石 += 灵石声音;
        战斗网格 = 建物("敌人与灵矢动态网格", 战斗几何, transform).GetComponent<MeshFilter>().sharedMesh;
        战斗网格.MarkDynamic(); 更新战斗绘制();
        电光材质 = new Material(Shader.Find("天帝/电光"));
        var 电光物 = 建物("闪电箭与跳链余辉", 电光几何, transform);
        电光物.GetComponent<MeshRenderer>().sharedMaterial = 电光材质;
        电光网格 = 电光物.GetComponent<MeshFilter>().sharedMesh;
        电光网格.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 电光网格.MarkDynamic();
        var 人形 = new 几何();
        人形.圆(Vector2.zero, 0.05f, 0.85f, new Color(0.12f, 0.18f, 0.15f), 24);
        人形.圆(Vector2.zero, 0.06f, 0.72f, new Color(0.87f, 0.72f, 0.36f), 24);
        人形.圆(Vector2.zero, 0.07f, 0.62f, new Color(0.15f, 0.23f, 0.23f), 24);
        人形.三角(new Vector3(-0.45f, 0.09f, -0.5f), new Vector3(0.45f, 0.09f, -0.5f), new Vector3(0, 0.09f, 0.25f), new Color(0.18f, 0.30f, 0.35f));
        人形.方块(new Vector2(0, -0.01f), 0.1f, new Vector2(0.28f, 0.44f), new Color(0.83f, 0.76f, 0.59f));
        人形.圆(new Vector2(0, 0.33f), 0.11f, 0.22f, new Color(0.94f, 0.81f, 0.59f), 16);
        主角 = 建物("主角俯视占位", 人形, transform).transform;
        主角.position = new Vector3(玩家位置.x, 0, 玩家位置.y);
        美术 = new 天帝战斗美术(transform, 游戏.美术, 地图, 战斗, !使用大图);
        if (美术.可用)
        {
            地面物.GetComponent<MeshRenderer>().enabled = false; 主角.GetComponent<MeshRenderer>().enabled = false;
            美术.更新(战斗, 玩家位置, 0); 更新战斗绘制();
        }
    }
    void 建高清长卷()
    {
        // 纹理从同一完整高清图切出，每个内部边缘共享8像素，分段只为控制纹理大小。
        const int 留边 = 8;
        var 分段 = 游戏.美术.青岚原高清长卷;
        int 总像素宽 = 0, 累计宽 = 0;
        for (int i = 0; i < 分段.Length; i++)
        {
            if (分段[i] == null) throw new System.InvalidOperationException("青岚原高清长卷缺少区域：" + (i + 1));
            总像素宽 += (int)分段[i].rect.width - (i > 0 ? 留边 : 0) - (i < 分段.Length - 1 ? 留边 : 0);
        }
        for (int i = 0; i < 分段.Length; i++)
        {
            var 图 = 分段[i];
            int 核心宽 = (int)图.rect.width - (i > 0 ? 留边 : 0) - (i < 分段.Length - 1 ? 留边 : 0);
            int 左 = 累计宽 - (i > 0 ? 留边 : 0), 右 = 累计宽 + 核心宽 + (i < 分段.Length - 1 ? 留边 : 0);
            累计宽 += 核心宽;
            float 世界左 = 左 / (float)总像素宽 * 地图.半宽 * 2 - 地图.半宽, 世界右 = 右 / (float)总像素宽 * 地图.半宽 * 2 - 地图.半宽;
            var 像 = new GameObject("青岚原高清长卷_" + (i + 1), typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            像.transform.SetParent(transform, false); 像.sprite = 图; 像.sharedMaterial = 背景材质; 像.sortingOrder = -32000;
            像.transform.position = new Vector3((世界左 + 世界右) * .5f, -.03f, 0); 像.transform.rotation = Quaternion.Euler(90, 0, 0);
            var 大小 = 图.bounds.size; 像.transform.localScale = new Vector3((世界右 - 世界左) / 大小.x, 地图.半高 * 2 / 大小.y, 1);
        }
    }
    void 建长卷障碍()
    {
        var 根 = new GameObject("青岚原独立树石障碍").transform; 根.SetParent(transform, false);
        foreach (var 数据 in 天帝长卷障碍数据.布点)
        {
            var 图 = 数据.种类 == 长卷障碍种类.古树 ? 游戏.美术.青岚原古树 : 数据.种类 == 长卷障碍种类.松树 ? 游戏.美术.青岚原松树 : 游戏.美术.青岚原岩石;
            if (图 == null) throw new System.InvalidOperationException("青岚原独立障碍素材缺失：" + 数据.种类);
            var 物 = new GameObject("独立障碍_" + 数据.种类, typeof(SpriteRenderer), typeof(天帝场景障碍)); 物.transform.SetParent(根, false);
            var 脚 = 数据.世界脚底(地图); var 大小 = 数据.展示尺寸;
            物.transform.position = new Vector3(脚.x, .08f, 脚.y); 物.transform.rotation = Quaternion.Euler(90, 0, 0);
            物.transform.localScale = new Vector3(大小.x / 图.bounds.size.x, 大小.y / 图.bounds.size.y, 1);
            var 像 = 物.GetComponent<SpriteRenderer>(); 像.sprite = 图; 像.sharedMaterial = 背景材质;
            像.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-脚.y * 100), -19000, 19000);
            物.GetComponent<天帝场景障碍>().初始化(数据, 地图);
        }
    }
    void 更新区域显示()
    {
        if (边界绘制 != null) 边界绘制.enabled = 显示边界线;
    }
    void 建发光边界()
    {
        var 数据 = new 几何();
        void 画线(Vector2 起, Vector2 终)
        {
            // 外晕、内晕和亮芯；沿真实通行交界简化并圆角，不重建碰撞。
            数据.线(起, 终, .035f, .55f, new Color(.10f, .80f, .55f, .08f));
            数据.线(起, 终, .036f, .24f, new Color(.20f, 1, .72f, .22f));
            数据.线(起, 终, .037f, .05f, new Color(.70f, 1, .86f, .75f));
        }
        foreach (var 轮廓 in 天帝通行轮廓.获取(地图))
            for (int i = 0; i < 轮廓.Count; i++) 画线(轮廓[i], 轮廓[(i + 1) % 轮廓.Count]);
        边界材质 = new Material(Shader.Find("天帝/电光")) { name = "青岚原通行边界发光材质" };
        边界绘制 = 建物("青岚原通行边界_青绿发光线", 数据, transform).GetComponent<MeshRenderer>();
        边界绘制.sharedMaterial = 边界材质; 边界绘制.sortingOrder = 地图.横向区域 ? -28500 : -30500;
        边界绘制.enabled = 显示边界线;
    }
    // 未来由王级战斗实体的死亡事件调用；位置到达、普通怪死亡和重复通知不能开启离开。
    public bool 通知王级击败(战斗敌人布点 已击败)
    {
        if (游戏 == null || 游戏.阶段 != 游戏阶段.战斗 || 王已击败 || 已击败 == null || 已击败.级别 != 战斗敌人级别.王级) return false;
        bool 属于本图 = false; foreach (var 点 in 地图.敌人) if (ReferenceEquals(点, 已击败)) { 属于本图 = true; break; }
        if (!属于本图) return false;
        王已击败 = true; 天帝声音.提示("ZD07_胜利"); 游戏.界面.更新战斗目标(); return true;
    }
    void Update()
    {
        if (游戏 == null || 游戏.阶段 != 游戏阶段.战斗 || 游戏.界面.战斗已暂停 || !Application.isFocused) return;
        var 输入 = 天帝战斗输入.读取(游戏.界面);
        if (输入.切换调试区域) 显示边界线 = !显示边界线;
        移动一步(输入.方向, 输入.跑步, Time.deltaTime);
        战斗一步(Time.deltaTime);
    }
    public void 战斗一步(float 秒)
    {
        if (战斗 == null || 游戏.阶段 != 游戏阶段.战斗 || 游戏.界面.战斗已暂停) return;
        战斗.推进(玩家位置, 秒);
        foreach (var 敌 in 战斗.敌人) if (敌.已生成 && 敌.布点.级别 != 战斗敌人级别.普通 && 已播登场.Add(敌)) 天帝声音.提示("ZD04_强敌登场");
        if (战斗.本局升级次数 > 已播升级次数) { 已播升级次数 = 战斗.本局升级次数; 天帝声音.提示("ZD06_升级"); }
        美术?.更新(战斗, 玩家位置, 秒); 更新战斗绘制();
        游戏.界面.更新战斗状态();
        游戏.界面.更新战斗目标();
        if (战斗.玩家死亡 && !已显示死亡) { 已显示死亡 = true; 天帝声音.提示("ZD08_失败"); 主角.localScale = Vector3.one * .65f; 游戏.界面.显示战斗失败(); }
    }
    void 敌人死亡(战斗敌人 敌)
    {
        if (敌.布点.级别 == 战斗敌人级别.王级) 通知王级击败(敌.布点);
    }
    void 命中反馈(Vector2 位置, 战斗伤害明细 明细, bool 玩家受伤)
    { if (明细.合计 > 0) 天帝声音.提示(玩家受伤 ? "ZD03_受伤" : "ZD02_命中"); 游戏.界面.显示伤害飘字(位置, 明细, 玩家受伤); 美术?.命中(位置, 玩家受伤); }
    void 射击声音(Vector2 _) { 天帝声音.提示("ZD01_灵力弹"); }
    void 道纹声音(道纹实例 _) { 天帝声音.提示("ZD05_拾取"); }
    void 通货声音(通货种类 _, int 数) { 天帝声音.提示("ZD05_拾取"); }
    void 灵石声音(int _) { 天帝声音.提示("ZD05_拾取"); }
    void 更新战斗绘制()
    {
        战斗几何.清空();
        foreach (var 敌 in 战斗.敌人)
        {
            if (!敌.已生成) continue;
            if (!敌.存活 && 敌.死亡秒 >= .65f) continue;
            float 半径 = 敌.布点.级别 == 战斗敌人级别.王级 ? 1.4f : 敌.布点.级别 == 战斗敌人级别.头目 ? .9f : .55f;
            Color 色 = 敌.布点.级别 == 战斗敌人级别.王级 ? new Color(.85f, .30f, .22f) : 敌.布点.级别 == 战斗敌人级别.头目 ? new Color(.75f, .43f, .76f) : 敌.布点.级别 == 战斗敌人级别.精英 ? new Color(.91f, .66f, .24f) : new Color(.65f, .77f, .69f);
            if (!敌.存活) { 色 = Color.Lerp(色, new Color(.17f, .18f, .17f), .8f); 半径 *= Mathf.Max(.05f, 1 - 敌.死亡秒 / .65f); }
            else if (敌.闪白秒 > 0) 色 = Color.white;
            if (美术 == null || !美术.可用)
            {
                战斗几何.圆(敌.位置, .08f, 半径 + .12f, new Color(.08f, .13f, .12f), 16);
                战斗几何.圆(敌.位置, .10f, 半径, 色, 10);
            }
            if (敌.显示血条)
            {
                float 宽 = 敌.布点.级别 == 战斗敌人级别.王级 ? 3.4f : 1.6f;
                float 顶 = 美术 != null && 美术.可用 ? 天帝敌种配置.物种(敌.物种, "width") * .9f + 敌.跳跃高度 : 半径 + .5f;
                var 中 = 敌.位置 + Vector2.up * 顶; float 填宽 = 宽 * 敌.血量 / 敌.最大血量;
                战斗几何.方块(中, .15f, new Vector2(宽 + .12f, .25f), new Color(.08f, .07f, .07f));
                战斗几何.方块(中 + Vector2.left * (宽 - 填宽) / 2, .16f, new Vector2(填宽, .16f), new Color(.85f, .26f, .23f));
                if (敌.护盾量 > 0)
                {
                    float 盾宽 = 宽 * Mathf.Clamp01(敌.护盾量 / (敌.最大血量 * 天帝敌种配置.取("support.shield_fraction")));
                    战斗几何.方块(中 + Vector2.up * .16f + Vector2.left * (宽 - 盾宽) / 2, .18f, new Vector2(盾宽, .1f), new Color(.42f, .86f, 1));
                    战斗几何.环(敌.位置, .18f, 半径 + .2f, new Color(.42f, .86f, 1, .7f));
                }
            }
            if (敌.存活 && 敌.登场剩余秒 > 0) 战斗几何.环(敌.位置, .17f, 半径 + .55f, new Color(1f, .75f, .25f, .7f));
            if (敌.存活 && (敌.行动 == 敌人行动.搜索 || 敌.行动 == 敌人行动.归巢))
                战斗几何.圆(敌.位置 + Vector2.up * (半径 + .22f), .19f, .13f, 敌.行动 == 敌人行动.搜索 ? new Color(1, .8f, .3f) : new Color(.4f, .7f, 1), 6);
            if (敌.行动 == 敌人行动.蓄力 && 敌.技能编号 > 0) 画敌预警(敌);
            if (敌.形态提示秒 > 0) 战斗几何.环(敌.位置, .2f, 2 + (1 - 敌.形态提示秒) * 3, new Color(.95f, .8f, .4f, .8f));
        }
        画敌术();
        foreach (var 物 in 战斗.掉落.地面)
        {
            if (物.演出结束) continue;
            var 位 = 物.显示位置; float 缩 = 物.吸附?.缩放 ?? 1;
            var 色 = 天帝道纹品阶.获取(物.道纹.品阶).颜色;
            战斗几何.环(位, .21f, (.65f + Mathf.Sin(Time.time * 3) * .06f) * 缩, 色);
            战斗几何.圆(位, .22f, .43f * 缩, new Color(.05f, .08f, .09f), 6);
            战斗几何.圆(位, .23f, .29f * 缩, 色, 6);
            for (int d = 0; d < 6; d++)
            {
                float a = d * Mathf.PI / 3;
                Vector2 点 = 位 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (.41f * 缩);
                战斗几何.圆(点, .24f, .08f * 缩, 物.道纹.有接口(d) ? 天帝道纹品阶.边颜色(物.道纹.品阶, d) : new Color(.13f, .17f, .16f), 4);
            }
        }
        foreach (var 物 in 战斗.通货掉落.地面)
        {
            if (物.演出结束) continue;
            var 位 = 物.显示位置; float 缩 = 物.吸附?.缩放 ?? 1;
            var 色 = new Color(1, .78f, .32f);
            战斗几何.环(位, .21f, (.60f + Mathf.Sin(Time.time * 3) * .06f) * 缩, 色);
            战斗几何.圆(位, .22f, .38f * 缩, new Color(.09f, .07f, .03f), 4);
            战斗几何.圆(位, .23f, .28f * 缩, 色, 4);
            战斗几何.方块(位, .24f, new Vector2(.08f, .30f) * 缩, new Color(.25f, .16f, .04f));
        }
        foreach (var 物 in 战斗.灵石掉落.地面)
        {
            if (物.演出结束) continue;
            var 位 = 物.显示位置; float 缩 = 物.吸附?.缩放 ?? 1;
            战斗几何.环(位, .21f, .48f * 缩, new Color(.36f, .90f, .78f, .7f));
            战斗几何.圆(位, .22f, .34f * 缩, new Color(.04f, .16f, .13f), 6);
            战斗几何.圆(位, .23f, .25f * 缩, new Color(.40f, .96f, .82f), 4);
            战斗几何.圆(位 + Vector2.up * (.07f * 缩), .24f, .10f * 缩, new Color(.84f, 1, .93f), 4);
        }
        int 弹体绘制数 = 0;
        foreach (var 矢 in 战斗.灵矢)
        {
            if (弹体绘制数++ >= 天帝数值.弹体上限) break;
            if (天帝顺序道纹.攻击形态(矢.执行段?.功能 ?? 道纹功能.旧版)) continue;
            if (矢.等待秒 > 0)
            {
                var f = 矢.执行段.功能;
                float 预警半径 = 天帝顺序道纹.扩展数值(f == 道纹功能.陨落 ? "fall_radius" : f == 道纹功能.烙印 ? "mark_radius" : f == 道纹功能.震荡 ? "pulse_radius" : "trail_radius");
                战斗几何.环(矢.位置,.23f,预警半径,new Color(.9f,.7f,.25f,.8f));
                战斗几何.环(矢.位置,.24f,预警半径*(1-矢.等待秒/矢.等待总秒),new Color(.4f,.9f,.8f));
                if (f == 道纹功能.陨落) 战斗几何.线(矢.位置+Vector2.up*(矢.等待秒/矢.等待总秒*4),矢.位置,.25f,.08f,new Color(.7f,.9f,.8f));
                continue;
            }
            float r = (矢.子矢 ? .12f : .22f) * 矢.参数.体型倍率;
            战斗几何.圆(矢.位置, .2f, r + .03f, new Color(.16f, .18f, .16f), 7);
            战斗几何.圆(矢.位置, .21f, r, 天帝道纹绘图.通路颜色[矢.参数.通路], 7);
            战斗几何.圆(矢.位置 + new Vector2(-r * .25f, r * .25f), .22f, r * .4f, new Color(.76f, .78f, .70f), 5);
        }
        foreach (var e in 战斗.功能地面效果) { 战斗几何.圆(e.位置,.17f,e.半径,new Color(.25f,.75f,.6f,.16f),16); 战斗几何.环(e.位置,.18f,e.半径,new Color(.35f,.85f,.65f,.5f)); }
        foreach (var e in 战斗.攻击形态效果) 天帝攻击形态绘制.画(e,
            (a,b,w,c)=>战斗几何.线(a,b,.24f,w,c), (p,r,c)=>战斗几何.环(p,.24f,r,c));
        foreach(var e in 战斗.特性效果列表)
        {
            var c=e.召唤?new Color(.45f,.83f,.38f,.8f):e.诱饵?new Color(.55f,.8f,1,.55f):new Color(.63f,.78f,.42f,.75f);
            if(e.护体)天帝攻击形态绘制.画护体(e,(a,b,w,color)=>战斗几何.线(a,b,.25f,w,color));
            else if(e.土垒){var side=new Vector2(-e.方向.y,e.方向.x);战斗几何.线(e.位置-side*e.半径,e.位置+side*e.半径,.25f,.6f,c);}
            else if(e.召唤||e.诱饵)战斗几何.圆(e.位置,.25f,.5f,c,6);
            else 战斗几何.环(e.位置,.25f,e.半径,c);
        }
        foreach (var 敌 in 战斗.敌人) if (敌.存活 && 敌.束缚剩余秒 > 0) 战斗几何.环(敌.位置,.26f,.8f,new Color(.55f,.45f,.9f));
        foreach (var 圈 in 战斗.光圈) 战斗几何.环(圈.位置, .18f, 圈.半径 * (1.15f - 圈.剩余秒), 圈.敌方 ? new Color(.95f, .25f, .14f) : new Color(.42f, .84f, .86f));
        战斗网格.Clear(); 战斗网格.SetVertices(战斗几何.顶点); 战斗网格.SetColors(战斗几何.颜色); 战斗网格.SetTriangles(战斗几何.索引, 0); 战斗网格.RecalculateBounds();
        更新电光();
    }
    static Color 敌术色(敌技能 s)
    {
        if (s.类型 == 敌技能类型.治疗) return new Color(.32f, .95f, .55f, .8f);
        if (s.类型 == 敌技能类型.护盾) return new Color(.45f, .85f, 1, .8f);
        switch (s.元素)
        {
            case 0: return new Color(1, .78f, .3f, .85f);
            case 1: return new Color(.45f, .85f, .35f, .85f);
            case 2: return new Color(.28f, .78f, 1, .85f);
            case 3: return new Color(1, .4f, .22f, .85f);
            case 4: return new Color(.82f, .65f, .4f, .85f);
            default: return new Color(.95f, .42f, .27f, .85f);
        }
    }
    void 画敌预警(战斗敌人 e)
    {
        var s = 敌技能.读取(e.技能编号); var 色 = 敌术色(s);
        if (s.辅助)
        {
            战斗几何.环(e.位置, .19f, .8f, 色);
            foreach (var a in e.辅助目标) if (a.存活)
            { 战斗几何.线(e.位置, a.位置, .18f, .05f, 色); 战斗几何.环(a.位置, .19f, .7f, 色); }
            return;
        }
        switch (s.类型)
        {
            case 敌技能类型.单击: 战斗几何.环(e.攻击落点, .18f, e.攻击范围, 色); break;
            case 敌技能类型.扇面: case 敌技能类型.归潮:
                战斗几何.扇(e.位置, e.锁定方向, e.物种 == 14 && s.类型 == 敌技能类型.扇面 ? 天帝敌种配置.取("boss.fan_range") : s.距离,
                    e.物种 == 14 && s.类型 == 敌技能类型.扇面 ? 天帝敌种配置.取("boss.fan_angle") : s.宽度, 色); break;
            case 敌技能类型.自周: 战斗几何.环(e.位置, .18f, e.本次攻击范围, 色); break;
            case 敌技能类型.投掷: case 敌技能类型.跳跃:
                战斗几何.环(e.攻击落点, .18f, s.宽度, 色);
                战斗几何.线(e.位置, e.攻击落点, .18f, .06f, 色); break;
            case 敌技能类型.冲锋: case 敌技能类型.直线:
                战斗几何.线(e.位置, s.类型 == 敌技能类型.冲锋 ? e.攻击落点 : e.位置 + e.锁定方向 * s.距离, .18f, s.宽度, 色 * .65f); break;
            case 敌技能类型.飞弹:
                int 数 = s.编号 == 4 && e.布点.级别 == 战斗敌人级别.精英 ? (int)天帝敌种配置.取("elite.fan_count") : s.数量;
                float 间角 = 数 == s.数量 ? s.宽度 : 天帝敌种配置.取("elite.fan_angle");
                for (int i = 0; i < 数; i++)
                {
                    float a = Mathf.Atan2(e.锁定方向.y, e.锁定方向.x) + (i - (数 - 1) * .5f) * 间角 * Mathf.Deg2Rad;
                    var 起 = e.位置;
                    if (s.编号 == 18)
                    { 起 += new Vector2(-e.锁定方向.y, e.锁定方向.x) * (i - 1) * 天帝敌种配置.取("boss.mirror_offset"); 战斗几何.环(起, .19f, .65f, 色); }
                    战斗几何.线(起, 起 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s.距离, .18f, .07f, 色);
                }
                break;
        }
    }
    void 画敌术()
    {
        foreach (var a in 战斗.战术.敌术)
        {
            var 色 = 敌术色(a.技能);
            if (a.冲击演出)
            {
                色.a *= Mathf.Clamp01(1 - a.已过 / a.寿命);
                if (a.技能.类型 == 敌技能类型.直线) 战斗几何.线(a.位置, a.位置 + a.方向 * a.半径, .2f, a.技能.宽度, 色);
                else if (a.技能.类型 == 敌技能类型.扇面) 战斗几何.扇(a.位置, a.方向, a.来源.物种 == 14 ? 天帝敌种配置.取("boss.fan_range") : a.半径, a.来源.物种 == 14 ? 天帝敌种配置.取("boss.fan_angle") : a.技能.宽度, 色);
                else 战斗几何.环(a.位置, .2f, a.半径 * (.8f + .2f * a.已过 / a.寿命), 色);
            }
            else if (a.地面) { 战斗几何.圆(a.终点, .17f, a.半径, 色 * .28f, 24); 战斗几何.环(a.终点, .18f, a.半径, 色); }
            else if (a.二段 || a.跃击) 战斗几何.环(a.终点, .19f, a.半径, 色);
            else if (a.位移) 战斗几何.线(a.起点, a.位置, .19f, .18f, 色);
            else
            {
                var 点 = a.位置;
                if (a.技能.类型 == 敌技能类型.投掷) 点 += Vector2.up * Mathf.Sin(Mathf.Clamp01(a.已过 / a.寿命) * Mathf.PI) * 1.5f;
                战斗几何.圆(点, .22f, .23f, 色, 8);
                战斗几何.线(点 - a.方向 * .45f, 点, .21f, .10f, 色);
                if (a.技能.类型 == 敌技能类型.投掷) 战斗几何.环(a.终点, .18f, a.半径, 色);
            }
        }
        foreach (var a in 战斗.战术.辅助线)
        {
            var 色 = a.盾 ? new Color(.5f, .9f, 1) : new Color(.4f, 1, .55f);
            战斗几何.线(a.起, a.终, .2f, .12f, 色);
            战斗几何.环(a.终, .21f, .7f + (.5f - a.剩余), 色);
            if (!a.盾) { 战斗几何.方块(a.终 + Vector2.up, .22f, new Vector2(.45f, .12f), 色); 战斗几何.方块(a.终 + Vector2.up, .22f, new Vector2(.12f, .45f), 色); }
        }
    }
    void 更新电光()
    {
        if (电光网格 == null) return;
        电光几何.清空();
        int 弹体绘制数 = 0;
        foreach (var 矢 in 战斗.灵矢)
        {
            if (弹体绘制数++ >= 天帝数值.弹体上限) break;
            Vector2 尾 = 矢.位置 - 矢.方向 * .45f * 矢.参数.体型倍率;
            电光几何.线(尾, 矢.位置, .29f, .06f * 矢.参数.体型倍率, new Color(.68f, .72f, .64f, .3f));
        }
        foreach (var 圈 in 战斗.光圈)
        {
            if (圈.敌方) continue;
            float a = Mathf.Clamp01(圈.剩余秒 / .18f), r = .22f + (1-a)*.85f;
            电光几何.环(圈.位置,.33f,r,new Color(.67f,.72f,.59f,a*.5f));
            for(int i=0;i<6;i++)
            {
                float 角=i*Mathf.PI/3; var 向=new Vector2(Mathf.Cos(角),Mathf.Sin(角));
                电光几何.线(圈.位置+向*r*.65f,圈.位置+向*r,.34f,.035f,new Color(.72f,.75f,.64f,a));
            }
        }
        电光网格.Clear(); 电光网格.SetVertices(电光几何.顶点); 电光网格.SetColors(电光几何.颜色); 电光网格.SetTriangles(电光几何.索引,0); 电光网格.RecalculateBounds();
    }
    void 画电弧(Vector2 起, Vector2 终, int 种, float 强, bool 跳链)
    {
        Vector2 向=终-起, 垂=new Vector2(-向.y,向.x).normalized;
        int 节=Mathf.Clamp(Mathf.CeilToInt(向.magnitude*3),2,16);
        Vector2 前=起;
        for(int i=1;i<=节;i++)
        {
            Vector2 点=Vector2.Lerp(起,终,i/(float)节);
            if(i<节) 点+=垂*Mathf.Sin(种*1.37f+i*8.71f)*(跳链?.23f:.09f);
            电光几何.线(前,点,.29f,跳链?.22f:.15f,new Color(.05f,.35f,1,强*.24f));
            电光几何.线(前,点,.30f,.075f,new Color(.16f,.72f,1,强*.6f));
            电光几何.线(前,点,.31f,.027f,new Color(.8f,.96f,1,强));
            if(跳链 && i%3==0) 电光几何.线(点,点+垂*.38f+向.normalized*.3f,.30f,.03f,new Color(.2f,.7f,1,强*.45f));
            前=点;
        }
    }
    public void 移动一步(Vector2 方向, bool 跑步, float 秒)
    {
        if (地图 == null || 游戏 == null || 游戏.阶段 != 游戏阶段.战斗 || 游戏.界面.战斗已暂停 || (战斗 != null && 战斗.玩家死亡) || float.IsNaN(秒) || float.IsInfinity(秒) || 秒 <= 0) return;
        float 速度 = 跑步 ? 游戏.主角属性.跑步速度 : 游戏.主角属性.移动速度;
        // 当前主角速度配置为0；仅在场景使用预览值，不写回角色数据。
        if (速度 <= 0) 速度 = 天帝天赋效果.移动速度(游戏.当前天赋, 跑步 ? 预览跑步速度 : 预览移动速度);
        var 原位置 = 玩家位置;
        玩家位置 = 地图.移动(玩家位置, 方向, 速度 * (战斗?.战术.玩家移速倍率 ?? 1) * Mathf.Min(秒, 0.25f));
        主角.position = new Vector3(玩家位置.x, 0, 玩家位置.y);
        美术?.移动反馈(方向, 跑步);
        if ((美术 == null || !美术.可用) && 方向.sqrMagnitude > 0.001f) 主角.rotation = Quaternion.Euler(0, Mathf.Atan2(方向.x, 方向.y) * Mathf.Rad2Deg, 0);
        游戏.界面.更新战斗位置(玩家位置, 地图.所在格(玩家位置));
    }
    void LateUpdate() { if (俯视相机 != null && 地图 != null) 更新相机(); 更新区域显示(); }
    public Rect 读取战斗视野()
    {
        更新相机();
        var 左下 = 俯视相机.ViewportToWorldPoint(new Vector3(0, 0, 俯视相机.transform.position.y));
        var 右上 = 俯视相机.ViewportToWorldPoint(new Vector3(1, 1, 俯视相机.transform.position.y));
        return Rect.MinMaxRect(左下.x, 左下.z, 右上.x, 右上.z);
    }
    void 更新相机()
    {
        float 半高 = 俯视相机.orthographicSize, 半宽 = 半高 * 俯视相机.aspect;
        float x = Mathf.Clamp(玩家位置.x, -Mathf.Max(0, 地图.半宽 - 半宽), Mathf.Max(0, 地图.半宽 - 半宽));
        float y = Mathf.Clamp(玩家位置.y, -Mathf.Max(0, 地图.半高 - 半高), Mathf.Max(0, 地图.半高 - 半高));
        if (战斗?.战术.震屏秒 > 0)
        {
            float 幅 = (float)天帝数值.取("battle_content.growth.shake_amplitude") * 战斗.战术.震屏秒 / (float)天帝数值.取("battle_content.growth.shake_seconds");
            x = Mathf.Clamp(x + Mathf.Sin(Time.time * 73) * 幅, -Mathf.Max(0, 地图.半宽 - 半宽), Mathf.Max(0, 地图.半宽 - 半宽));
            y = Mathf.Clamp(y + Mathf.Sin(Time.time * 91) * 幅, -Mathf.Max(0, 地图.半高 - 半高), Mathf.Max(0, 地图.半高 - 半高));
        }
        俯视相机.transform.position = new Vector3(x, 45, y);
    }
    GameObject 建物(string 名, 几何 数据, Transform 父)
    {
        var 物 = new GameObject(名, typeof(MeshFilter), typeof(MeshRenderer)); 物.transform.SetParent(父, false);
        var 网 = new Mesh { name = 名, indexFormat = 数据.顶点.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 }; 网.SetVertices(数据.顶点); 网.SetColors(数据.颜色); 网.SetTriangles(数据.索引, 0); 网.RecalculateBounds();
        网格资源.Add(网); 物.GetComponent<MeshFilter>().sharedMesh = 网; 物.GetComponent<MeshRenderer>().sharedMaterial = 地图材质; return 物;
    }
    void OnDestroy()
    {
        if (战斗 != null) { 战斗.射击释放 -= 射击声音; 战斗.掉落.获得道纹 -= 道纹声音; 战斗.通货掉落.获得通货 -= 通货声音; 战斗.灵石掉落.获得灵石 -= 灵石声音; }
        美术?.Dispose(); if (战斗 != null) { 战斗.清理特性战斗(); 战斗.战术.清理(); 战斗.敌人死亡 -= 敌人死亡; 战斗.伤害分项反馈 -= 命中反馈; }
        foreach (var 网 in 网格资源) if (网 != null) Destroy(网); if (电光材质 != null) Destroy(电光材质);
        if (背景材质 != null) Destroy(背景材质);
        if (边界材质 != null) Destroy(边界材质);
    }
    sealed class 几何
    {
        public readonly List<Vector3> 顶点 = new List<Vector3>(); public readonly List<Color> 颜色 = new List<Color>(); public readonly List<int> 索引 = new List<int>();
        public void 清空() { 顶点.Clear(); 颜色.Clear(); 索引.Clear(); }
        public void 线(Vector2 起, Vector2 终, float 高, float 宽, Color 色)
        {
            Vector2 垂=new Vector2(-(终-起).y,(终-起).x).normalized*宽*.5f;
            三角(new Vector3(起.x+垂.x,高,起.y+垂.y),new Vector3(终.x+垂.x,高,终.y+垂.y),new Vector3(起.x-垂.x,高,起.y-垂.y),色);
            三角(new Vector3(终.x+垂.x,高,终.y+垂.y),new Vector3(终.x-垂.x,高,终.y-垂.y),new Vector3(起.x-垂.x,高,起.y-垂.y),色);
        }
        public void 环(Vector2 中, float 高, float 半径, Color 色)
        {
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24, b = (i + 1) * Mathf.PI * 2 / 24;
                Vector2 va = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), vb = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Vector2 p = 中 + va * 半径, q = 中 + vb * 半径, r = 中 + va * Mathf.Max(0, 半径 - .09f), s = 中 + vb * Mathf.Max(0, 半径 - .09f);
                三角(new Vector3(p.x, 高, p.y), new Vector3(q.x, 高, q.y), new Vector3(r.x, 高, r.y), 色);
                三角(new Vector3(q.x, 高, q.y), new Vector3(s.x, 高, s.y), new Vector3(r.x, 高, r.y), 色);
            }
        }
        public void 三角(Vector3 a, Vector3 b, Vector3 c, Color 色)
        { int 开 = 顶点.Count; 顶点.Add(a); 顶点.Add(b); 顶点.Add(c); 颜色.Add(色); 颜色.Add(色); 颜色.Add(色); 索引.Add(开); 索引.Add(开 + 1); 索引.Add(开 + 2); }
        public void 扇(Vector2 中, Vector2 向, float 半径, float 角, Color 色)
        {
            float 朝 = Mathf.Atan2(向.y, 向.x); var 淡 = 色; 淡.a *= .22f;
            for (int i = 0; i < 18; i++)
            {
                float a = 朝 + (i / 18f - .5f) * 角 * Mathf.Deg2Rad, b = 朝 + ((i + 1) / 18f - .5f) * 角 * Mathf.Deg2Rad;
                var p = 中 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 半径; var q = 中 + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 半径;
                三角(new Vector3(中.x, .17f, 中.y), new Vector3(p.x, .17f, p.y), new Vector3(q.x, .17f, q.y), 淡);
                线(p, q, .18f, .07f, 色); if (i == 0) 线(中, p, .18f, .07f, 色); if (i == 17) 线(中, q, .18f, .07f, 色);
            }
        }
        public void 方块(Vector2 中, float 高, Vector2 大小, Color 色)
        {
            var a = new Vector3(中.x - 大小.x / 2, 高, 中.y - 大小.y / 2); var b = a + new Vector3(大小.x, 0, 0);
            var c = a + new Vector3(大小.x, 0, 大小.y); var d = a + new Vector3(0, 0, 大小.y);
            三角(a, b, c, 色); 三角(a, c, d, 色);
        }
        public void 圆(Vector2 中, float 高, float 半径, Color 色, int 边)
        {
            for (int i = 0; i < 边; i++)
            {
                float a = i * Mathf.PI * 2 / 边, b = (i + 1) * Mathf.PI * 2 / 边;
                三角(new Vector3(中.x, 高, 中.y), new Vector3(中.x + Mathf.Cos(a) * 半径, 高, 中.y + Mathf.Sin(a) * 半径), new Vector3(中.x + Mathf.Cos(b) * 半径, 高, 中.y + Mathf.Sin(b) * 半径), 色);
            }
        }
    }
}
