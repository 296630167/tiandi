using System;
using System.Collections.Generic;
using UnityEngine;

// Only the presentation layer moves or scales sprites; combat positions remain authoritative.
public sealed class 天帝战斗美术 : IDisposable
{
    sealed class 单位图
    {
        public SpriteRenderer 像;
        public SpriteRenderer 阴影;
        public Vector2 上次位置;
        public float 比例;
        public float 步尘冷却,反冲秒,点缀冷却;
        public float 受击秒,受击力度,命中特效冷却;
        public float 受击闪白秒;
        public bool 有受击快照;
        public Vector2 受击方向;
        public Color 受击色=Color.white;
        public string 动作编号;
        public int 动作物种=-1,动作形态=-1;
        public 敌人行动 上次行动;
        public Vector2 朝向=Vector2.right;
        public readonly MaterialPropertyBlock 参数 = new MaterialPropertyBlock();
    }
    sealed class 短效
    {
        public SpriteRenderer 像;
        public Vector2 位置, 漂移;
        public float 剩余, 寿命, 宽;
        public Color 色;
    }
    readonly Transform 根;
    readonly 天帝美术资源 素材;
    readonly 天帝战斗地图 地图;
    readonly Material 材质;
    readonly MaterialPropertyBlock 通用参数 = new MaterialPropertyBlock();
    readonly List<单位图> 敌图 = new List<单位图>();
    readonly List<短效> 特效池 = new List<短效>();
    readonly List<SpriteRenderer> 水镜虚影 = new List<SpriteRenderer>(2);
    readonly List<Mesh> 地表网格 = new List<Mesh>();
    readonly List<Material> 地表材质 = new List<Material>();
    readonly Dictionary<战斗道纹掉落, SpriteRenderer[]> 道纹图 = new Dictionary<战斗道纹掉落, SpriteRenderer[]>();
    readonly Dictionary<战斗通货掉落, SpriteRenderer> 通货图 = new Dictionary<战斗通货掉落, SpriteRenderer>();
    单位图 玩家图;
    SpriteRenderer 玩家遮挡提示;
    readonly MaterialPropertyBlock 遮挡参数 = new MaterialPropertyBlock();
    public Bounds 玩家绘制范围 => 玩家图?.像 != null ? 玩家图.像.bounds : new Bounds();
    public bool 玩家遮挡提示可见 => 玩家遮挡提示 != null && 玩家遮挡提示.enabled;
    readonly 天帝八方向播放 主角帧播放;
    readonly 天帝战斗帧动画 主角方向配置;
    readonly 天帝战斗帧播放 主角方向播放;
    bool 主角攻击中, 主角攻击已释放, 主角释放待绘制, 主角使用新帧;
    float 主角攻击秒, 主角前摇秒, 主角回收秒;
    Vector2 主角攻击方向;
    readonly 天帝战斗系统 动画事件源;
    readonly 天帝战斗系统 战斗源;
    readonly 天帝敌人动作 敌动作=new 天帝敌人动作();
    float 总秒, 尘雾冷却, 玩家闪白, 回弹秒;
    int 上次发射;
    Vector2 移动方向;
    bool 跑步中;
    static readonly Quaternion 平面旋转 = Quaternion.Euler(90, 0, 0);
    static readonly int 染色键 = Shader.PropertyToID("_TintColor"), 闪白键 = Shader.PropertyToID("_Flash");
    public bool 可用 => 玩家图 != null && 敌图.Count == 地图.敌人.Count;
    public int 敌人立绘数 => 敌图.Count;
    public 天帝战斗美术(Transform 父, 天帝美术资源 素材, 天帝战斗地图 地图, 天帝战斗系统 战斗, bool 显示地形 = true)
    {
        this.素材 = 素材; this.地图 = 地图; 战斗源=战斗;
        if (素材 == null || 素材.立绘着色器 == null || 素材.获取("CH02") == null) return;
        for (int i = 1; i <= 4; i++) if (素材.获取("EN" + i.ToString("00")) == null) return;
        根 = new GameObject("青岚原手绘美术").transform; 根.SetParent(父, false);
        材质 = new Material(素材.立绘着色器) { name = "静态立绘共用材质" };
        if (显示地形) 建地形();
        玩家图 = 建单位("程序员主角", "CH02", 地图.出生位置, 3.7f);
        玩家遮挡提示 = 建像("主角被遮挡时的青玉剪影", "CH02", 地图.出生位置, Vector2.one, 10002);
        玩家遮挡提示.enabled = false;
        染色(玩家遮挡提示, new Color(.62f, .94f, .82f, .55f), 0, 遮挡参数);
        遮挡参数.SetFloat(Shader.PropertyToID("_Silhouette"), 1);
        玩家遮挡提示.SetPropertyBlock(遮挡参数);
        if (素材.主角移动动画 != null && 素材.主角移动动画.完整)
        {
            主角帧播放 = new 天帝八方向播放(素材.主角移动动画);
            玩家图.像.sprite = 主角帧播放.当前精灵;
            玩家图.比例 = 素材.主角移动动画.展示高度 / 玩家图.像.sprite.bounds.size.y;
        }
        var 新主角配置 = Resources.Load<天帝战斗帧动画>("角色帧动画/HERO");
        if (新主角配置 != null && 新主角配置.可用)
        {
            主角方向配置 = 新主角配置;
            主角方向播放 = new 天帝战斗帧播放(新主角配置);
        }
        if (主角帧播放 != null || 主角方向播放 != null)
        {
            动画事件源 = 战斗;
            动画事件源.准备射击 += 准备主角射击;
            动画事件源.射击释放 += 释放主角射击;
        }
        foreach (var 敌 in 战斗.敌人)
        {
            string 编号 = 天帝敌种配置.美术编号(敌);
            if (素材.获取(编号) == null) 编号 = "EN" + ((int)敌.布点.级别 + 1).ToString("00");
            var 图 = 建单位(敌.名称, 编号, 敌.位置, 天帝敌种配置.物种(敌.物种, "width"));
            图.动作编号=编号;图.动作物种=敌.物种;图.动作形态=敌.显示形态;
            敌动作.预加载(编号);
            if(敌.物种==14)for(int f=1;f<=天帝敌种配置.形态数(敌.地图档位);f++)敌动作.预加载("BTB"+f.ToString("00"));
            图.像.enabled = 敌.已生成; if (图.阴影 != null) 图.阴影.enabled = 敌.已生成;
            敌图.Add(图);
        }
        if (素材.获取("BTB04") != null)
            for (int i = 0; i < 2; i++)
            { var 影 = 建像("水镜虚影", "BTB04", Vector2.zero, Vector2.one * 天帝敌种配置.物种(14, "width")); 染色(影, new Color(.45f, .8f, 1, .24f)); 影.enabled = false; 水镜虚影.Add(影); }
    }
    SpriteRenderer 建像(string 名, string 编号, Vector2 点, Vector2 大小, int 层 = 0, float 角 = 0)
    {
        var 图 = 素材.获取(编号); if (图 == null) return null;
        var 像 = new GameObject(名, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        像.transform.SetParent(根, false); 像.sprite = 图; 像.sharedMaterial = 材质;
        像.transform.position = new Vector3(点.x, .12f, 点.y); 像.transform.rotation = 平面旋转 * Quaternion.Euler(0, 0, 角);
        像.transform.localScale = new Vector3(大小.x / 图.bounds.size.x, 大小.y / 图.bounds.size.y, 1);
        像.sortingOrder = 层; return 像;
    }
    void 染色(SpriteRenderer 像, Color 色, float 闪 = 0, MaterialPropertyBlock 参数 = null)
    {
        if (像 == null) return;
        var 值 = 参数 ?? 通用参数; 值.Clear(); 值.SetColor(染色键, 色); 值.SetFloat(闪白键, 闪); 像.SetPropertyBlock(值);
    }
    static int 深度(Vector2 点) => Mathf.Clamp(Mathf.RoundToInt(-点.y * 100), -10000, 10000);
    单位图 建单位(string 名, string 编号, Vector2 点, float 宽)
    {
        var 像 = 建像(名, 编号, 点, new Vector2(宽, 宽), 深度(点));
        var 阴影 = 建像(名 + "柔边阴影", "FX01", 点, new Vector2(宽 * .6f, 宽 * .22f), -20000);
        染色(阴影, new Color(0, 0, 0, .46f));
        return new 单位图 { 像 = 像, 阴影 = 阴影, 上次位置 = 点, 比例 = 宽 / 像.sprite.bounds.size.x };
    }
    void 建地形()
    {
        var 随机 = new System.Random(unchecked(地图.种子 ^ 0x415254));
        if (地图.横向区域) 建区域地表();
        for (int y = 0; y < 地图.地块高; y++) for (int x = 0; x < 地图.地块宽; x++)
        {
            var 类 = 地图.地块(x, y); var 点 = 地图.格中心(x, y);
            if (!地图.横向区域)
            {
                string 地面 = 类 == 战斗地块.水域 ? "TL03" : 类 == 战斗地块.王房 ? "TL04" : 类 == 战斗地块.石径 ? "TL02" : "TL01";
                var 地砖 = 建像("地面-" + x + "-" + y, 地面, 点, Vector2.one * 4, -30000);
                地砖.transform.position = new Vector3(点.x, .015f, 点.y);
                if (类 == 战斗地块.草地 || 类 == 战斗地块.林地) 地砖.transform.rotation = 平面旋转 * Quaternion.Euler(0, 0, 随机.Next(4) * 90);
                float 明度 = .77f + (float)随机.NextDouble() * .04f; 染色(地砖, new Color(明度, 明度, 明度));
            }
            if (类 == 战斗地块.木桥 && !地图.横向区域) 建像("木桥", "EV05", 点, new Vector2(4.7f, 4.4f), -29000);
            else if (类 == 战斗地块.林地)
            {
                bool 王墙 = !地图.横向区域 && Mathf.Abs(x - 地图.王房中心格.x) <= 3 && Mathf.Abs(y - 地图.王房中心格.y) <= 3;
                if (王墙)
                    建像("王房石墙", "EV06", 点, new Vector2(4.8f, 2.4f), 深度(点), Mathf.Abs(x - 地图.王房中心格.x) == 3 ? 90 : 0);
                else
                {
                    string 物 = (x * 7 + y * 11) % 6 == 0 ? "EV03" : (x + y) % 5 == 0 ? "EV02" : "EV01";
                    // Blocked tiles only: ornaments never introduce a new collision or cover a walkable tile.
                    var 林点 = 地图.横向区域 ? 点 + new Vector2((float)随机.NextDouble() - .5f, (float)随机.NextDouble() - .5f) * .5f : 点;
                    建像("林缘-" + x + "-" + y, 物, 林点, Vector2.one * (地图.横向区域 ? 3.35f + (float)随机.NextDouble() * .15f : 3.75f), 深度(林点));
                }
            }
            else if (类 == 战斗地块.草地 && (x * 7 + y * 11) % 9 == 0 && Vector2.Distance(点, 地图.出生位置) > 4)
            {
                var 偏 = new Vector2(1.1f, 1.1f);
                建像("草叶与碎石", (x + y) % 2 == 0 ? "EV08" : "EV04", 点 + 偏, Vector2.one * .65f, -28000);
            }
        }
        if (地图.横向区域)
        {
            for (int 河 = 0; 河 < 2; 河++) foreach (int 行 in new[] { (int)天帝数值.取("map.long_region.road_row"), (int)天帝数值.取("map.long_region.side_bridge_rows." + 河) })
            {
                int 列 = (int)天帝数值.取("map.long_region.river_columns." + 河) + Mathf.RoundToInt(Mathf.Sin((行 - (int)天帝数值.取("map.long_region.road_row")) * .22f) * 2);
                建像("渡河小桥", "EV05", 地图.格中心(列, 行), new Vector2(12, 12), -29000, 90);
            }
            var 门 = 地图.格中心(地图.王房入口格.x, 地图.王房入口格.y);
            foreach (float y in new[] { -7f, 7f }) 建像("东境遗迹石柱", "EV06", 门 + new Vector2(0, y), new Vector2(3, 5), 深度(门 + new Vector2(0, y)));
        }
        foreach (var 队 in 地图.小队)
        {
            var 点 = 地图.格中心(队.中心格.x, 队.中心格.y) + new Vector2(-3.3f, 3.3f);
            建像("营地残旗", "EV07", 点, new Vector2(1.2f, 2.4f), 深度(点));
        }
    }
    // 4000块地面按纹理合并，避免手机新增4000个SpriteRenderer及逐块材质。
    void 建区域地表()
    {
        foreach (string 编号 in new[] { "TL01", "TL02", "TL03", "TL04" })
        {
            var 图 = 素材.获取(编号); if (图 == null) continue;
            var 顶点 = new List<Vector3>(); var UV = new List<Vector2>(); var 色 = new List<Color>(); var 索引 = new List<int>();
            var 原顶点 = 图.vertices; var 原UV = 图.uv; var 原索引 = 图.triangles;
            for (int y = 0; y < 地图.地块高; y++) for (int x = 0; x < 地图.地块宽; x++)
            {
                var 类 = 地图.地块(x, y);
                string 地面 = 类 == 战斗地块.水域 || 类 == 战斗地块.木桥 ? "TL03" : 类 == 战斗地块.王房 ? "TL04" : 类 == 战斗地块.石径 ? "TL02" : "TL01";
                if (地面 != 编号) continue;
                var 点 = 地图.格中心(x, y); int 开 = 顶点.Count;
                var 染 = 类 == 战斗地块.平原 ? new Color(1.15f, 1.16f, .86f) : 类 == 战斗地块.林地 ? new Color(.72f, .91f, .80f)
                    : 类 == 战斗地块.水域 || 类 == 战斗地块.木桥 ? new Color(.95f, 1.17f, 1.28f) : new Color(.98f, 1.12f, 1.01f);
                for (int i = 0; i < 原顶点.Length; i++)
                {
                    var 偏 = 原顶点[i] - (Vector2)图.bounds.center;
                    顶点.Add(new Vector3(点.x + 偏.x / 图.bounds.size.x * 4, .015f, 点.y + 偏.y / 图.bounds.size.y * 4));
                    UV.Add(原UV[i]); 色.Add(染);
                }
                foreach (var 号 in 原索引) 索引.Add(开 + 号);
            }
            var 网 = new Mesh { name = "青岚原合并地表_" + 编号, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            网.SetVertices(顶点); 网.SetUVs(0, UV); 网.SetColors(色); 网.SetTriangles(索引, 0); 网.RecalculateBounds(); 地表网格.Add(网);
            var 地材 = new Material(材质) { mainTexture = 图.texture, name = "区域地表_" + 编号 }; 地表材质.Add(地材);
            var 物 = new GameObject("合并地表_" + 编号, typeof(MeshFilter), typeof(MeshRenderer)); 物.transform.SetParent(根, false);
            物.GetComponent<MeshFilter>().sharedMesh = 网;
            var 绘制 = 物.GetComponent<MeshRenderer>(); 绘制.sharedMaterial = 地材; 绘制.sortingOrder = -30000;
        }
    }
    public void 移动反馈(Vector2 方向, bool 跑步) { 移动方向 = 方向; 跑步中 = 跑步; }
    void 准备主角射击(Vector2 方向)
    {
        float 前摇 = Mathf.Min(动画事件源.射击前摇, 动画事件源.当前普攻.间隔 * (float)天帝数值.取("player.attack_windup_fraction"));
        主角帧播放?.准备射击(方向, 前摇);
        var 攻击 = 主角方向配置?.获取(天帝八方向播放.方向转朝向(方向), 战斗帧动作.攻击);
        主角攻击中 = 主角方向播放 != null && 攻击 != null && 方向.sqrMagnitude > .0001f;
        if (!主角攻击中) return;
        主角攻击方向 = 方向; 主角攻击秒 = 0; 主角前摇秒 = Mathf.Max(.001f, 前摇);
        主角回收秒 = Mathf.Max(.001f, 攻击.总秒 - 攻击.释放秒); 主角攻击已释放 = 主角释放待绘制 = false;
    }
    void 释放主角射击(Vector2 方向)
    {
        主角帧播放?.释放射击();
        if (主角攻击中) { 主角攻击已释放 = 主角释放待绘制 = true; 主角攻击秒 = 0; }
    }
    bool 推进主角方向帧(Vector2 位移, float 秒, bool 存活, out Sprite 帧)
    {
        帧 = null;
        if (主角方向播放 == null) return false;
        if (!存活) { 主角攻击中 = false; 帧 = 玩家图.像.sprite; return 主角使用新帧; }
        if (主角攻击中)
        {
            // A paused frame must keep the already displayed release pose. A real
            // release event still passes through the pending branch below once.
            if (!主角释放待绘制 && 秒 <= 0 && 位移.sqrMagnitude <= .00000001f && 主角使用新帧)
            { 帧 = 玩家图.像.sprite; return true; }
            if (主角释放待绘制)
            {
                主角释放待绘制 = false;
                主角攻击秒 += 秒;
                return 主角方向播放.尝试攻击(主角攻击方向, 0, true, out 帧);
            }
            主角攻击秒 += 秒;
            float 时长 = 主角攻击已释放 ? 主角回收秒 : 主角前摇秒;
            if (主角攻击秒 <= 时长 + (主角攻击已释放 ? 0 : .15f) &&
                主角方向播放.尝试攻击(主角攻击方向, 主角攻击秒 / 时长, 主角攻击已释放, out 帧)) return true;
            主角攻击中 = false;
        }
        bool 移动 = 秒 > 0 && 位移.sqrMagnitude > .00000001f;
        var 动作 = !移动 ? 战斗帧动作.待机 : 跑步中 ? 战斗帧动作.奔跑 : 战斗帧动作.移动;
        float 速度倍率 = 移动 ? Mathf.Clamp(位移.magnitude / Mathf.Max(.0001f, 秒 * 主角方向配置.参考移速), .35f, 1.8f) : 1;
        return 主角方向播放.尝试推进(动作, 位移, 秒, out 帧, 速度倍率);
    }
    public void 命中(Vector2 点, bool 玩家受伤)
    {
        if (!可用) return;
        if (玩家受伤) { 玩家闪白 = .1f; 发特效("FX06", 点, 1.4f, .2f, new Color(1, .45f, .26f)); }
        else
        {
            发特效("FX06", 点, .85f, .12f, new Color(.8f, 1, .91f, .8f));
            发特效("FX04", 点, 1.1f, .28f, new Color(.72f, .86f, .79f, .45f));
        }
    }
    public void 命中(战斗受击反馈 击)
    {
        if(!可用||击.明细.合计<=0)return;
        单位图 图=击.玩家受伤?玩家图:null;
        if(图==null)for(int i=0;i<战斗源.敌人.Count;i++)if(ReferenceEquals(战斗源.敌人[i],击.目标)){图=敌图[i];break;}
        if(图==null)return;
        图.有受击快照=true;
        if(图.受击闪白秒<=0)图.受击闪白秒=天帝受击表现.取("flash_seconds");
        float 力=击.重击?天帝受击表现.取("heavy_multiplier"):1;
        图.受击秒=天帝受击表现.取("recoil_seconds");图.受击力度=力;
        if(击.目标?.物种==14)图.受击力度*=天帝受击表现.取("boss_recoil_factor");
        图.受击方向=击.方向;
        图.受击色=击.仅护盾||击.破盾?new Color(.48f,.85f,1):击.玩家受伤?new Color(1,.48f,.36f):击.暴击?new Color(1,.88f,.5f):Color.white;
        if(击.玩家受伤)玩家闪白=天帝受击表现.取("flash_seconds");
        if(图.命中特效冷却>0&&!击.破盾&&!击.击杀)return;
        图.命中特效冷却=天帝受击表现.取("effect_interval");
        var 点=击.位置+new Vector2(0,.65f);
        发特效("HIT闪",点,1.05f*力,.16f,new Color(图.受击色.r,图.受击色.g,图.受击色.b,.88f),-击.方向);
        if(击.重击||击.仅护盾)发特效("HIT环",点,1.45f*力,.24f,new Color(图.受击色.r,图.受击色.g,图.受击色.b,.5f));
    }
    void 推进受击(单位图 图,float 秒)
    { 图.受击秒=Mathf.Max(0,图.受击秒-秒);图.受击闪白秒=Mathf.Max(0,图.受击闪白秒-秒);图.命中特效冷却=Mathf.Max(0,图.命中特效冷却-秒); }
    public void 更新(天帝战斗系统 战斗, Vector2 玩家, float 秒)
    {
        if (!可用) return;
        秒 = Mathf.Clamp(秒, 0, .25f); 总秒 += 秒; 玩家闪白 = Mathf.Max(0, 玩家闪白 - 秒); 回弹秒 = Mathf.Max(0, 回弹秒 - 秒);
        if (战斗.普通释放次数 != 上次发射)
        { 上次发射 = 战斗.普通释放次数; 回弹秒 = .12f; }
        var 玩家位移 = 玩家 - 玩家图.上次位置;
        var 旧主角帧 = 主角帧播放?.推进(玩家位移, 秒, !战斗.玩家死亡);
        主角使用新帧 = 推进主角方向帧(玩家位移, 秒, !战斗.玩家死亡, out var 新主角帧);
        if (主角使用新帧)
        {
            玩家图.像.sprite = 新主角帧;
            玩家图.比例 = 主角方向配置.固定缩放;
        }
        else if (旧主角帧 != null)
        {
            玩家图.像.sprite = 旧主角帧;
            玩家图.比例 = 素材.主角移动动画.展示高度 / Mathf.Max(.01f, 旧主角帧.bounds.size.y);
        }
        else if (主角方向播放 != null)
        { 玩家图.像.sprite = 素材.获取("CH02"); 玩家图.比例 = 3.7f / 玩家图.像.sprite.bounds.size.x; }
        推进受击(玩家图,秒);
        bool 玩家方向帧 = 主角使用新帧 || 主角帧播放 != null;
        更新单位(玩家图, 玩家, !战斗.玩家死亡, 战斗.玩家死亡 ? .5f : 0, 玩家闪白, false, !玩家方向帧 && 回弹秒 > 0 ? -3 : 0, 0, 玩家方向帧);
        尘雾冷却 -= 秒;
        if (移动方向.sqrMagnitude > .01f && !战斗.玩家死亡 && 尘雾冷却 <= 0 && (玩家 - 玩家图.上次位置).sqrMagnitude > .00001f)
        {
            发特效("FX04", 玩家 - 移动方向.normalized * .25f, 跑步中 ? .95f : .65f, .35f, new Color(1, 1, 1, .38f));
            if (跑步中) 发特效("FX05", 玩家 - 移动方向.normalized * .65f, 1.25f, .22f, new Color(1, 1, 1, .3f), 移动方向);
            尘雾冷却 = 跑步中 ? .13f : .22f;
        }
        玩家图.上次位置 = 玩家;
        int 尘预算=(int)天帝敌种配置.取("presentation.enemy_dust_frame_max");
        int 点缀预算 = 2;
        for (int i = 0; i < 敌图.Count; i++)
        {
            var 敌 = 战斗.敌人[i];
            推进受击(敌图[i],秒);
            if (!敌.已生成) { 敌图[i].像.enabled = false; if (敌图[i].阴影 != null) 敌图[i].阴影.enabled = false; continue; }
            var 当前图=敌图[i];
            if(当前图.动作物种!=敌.物种||当前图.动作形态!=敌.显示形态)
            {当前图.动作编号=敌.物种 == 14 ? "BTB" + 敌.显示形态.ToString("00") : 天帝敌种配置.美术编号(敌);当前图.动作物种=敌.物种;当前图.动作形态=敌.显示形态;}
            string code=当前图.动作编号;
            var 新图 = 敌动作.读取(code,敌,(敌.位置-敌图[i].上次位置).sqrMagnitude>.00001f,总秒+i*.07f)??素材.获取(code);
            bool 使用方向帧 = 敌动作.当前使用方向帧(敌);
            if (新图 != null)
            {
                当前图.像.sprite = 新图; 当前图.像.name = 敌.名称;
                当前图.比例 = 使用方向帧 ? 敌动作.获取方向配置(code).固定缩放 : 天帝敌种配置.物种(敌.物种, "width") / 新图.bounds.size.x;
            }
            更新单位(当前图, 敌.位置, 敌.存活, 敌.死亡秒, 敌.闪白秒, 敌.行动 == 敌人行动.蓄力, !使用方向帧 && 敌.行动 == 敌人行动.后摇 ? -6 : 0, i, 使用方向帧, 天帝敌种配置.立绘颜色(敌.物种));
            if (敌.存活)
            {
                var 图=敌图[i];var 位移=敌.位置-图.上次位置;
                if(位移.sqrMagnitude>.00001f)图.朝向=位移.normalized;
                if(敌.行动==敌人行动.蓄力)图.朝向=敌.锁定方向;
                if (!使用方向帧) 图.像.flipX=图.朝向.x<0;
                if(图.上次行动==敌人行动.蓄力&&敌.行动==敌人行动.后摇)图.反冲秒=天帝敌种配置.取("presentation.enemy_recoil_seconds");
                图.反冲秒=Mathf.Max(0,图.反冲秒-秒);图.步尘冷却=Mathf.Max(0,图.步尘冷却-秒);
                图.点缀冷却=Mathf.Max(0,图.点缀冷却-秒);
                if (秒 > 0 && 敌.物种 >= 15 && 点缀预算 > 0 && 图.点缀冷却 <= 0 &&
                    (敌.位置-玩家).sqrMagnitude < 196 && (位移.sqrMagnitude > .00001f || 敌.行动 == 敌人行动.蓄力))
                {
                    // 专属生成点缀使用已有48槽池；不参与碰撞或伤害。
                    var 点 = 敌.位置 - 图.朝向 * .45f + new Vector2(0,.35f);
                    发特效("BFX"+敌.物种,点,敌.物种==20?.7f:.55f,.55f,Color.white,图.朝向);
                    图.点缀冷却=.42f+i%4*.09f;点缀预算--;
                }
                if(位移.sqrMagnitude>.00001f)
                {
                    if (!使用方向帧)
                    {
                        float 步=Mathf.Abs(Mathf.Sin(总秒*(天帝敌种配置.角色(敌.物种)==2?17:13)+i));
                        图.像.transform.position+=new Vector3(0,0,步*天帝敌种配置.取("presentation.enemy_gait_bob"));
                    }
                    if(尘预算>0&&图.步尘冷却<=0&&(敌.位置-玩家).sqrMagnitude<Mathf.Pow(天帝敌种配置.取("presentation.enemy_dust_range"),2))
                    {发特效("FX04",敌.位置-图.朝向*.25f,.6f,.3f,new Color(1,1,1,.25f));尘预算--;图.步尘冷却=天帝敌种配置.取("presentation.enemy_step_interval")*(1+i%3*.15f);}
                }
                if (!使用方向帧)
                {
                float 蓄 = 敌.行动 == 敌人行动.蓄力 ? Mathf.Clamp01(1 - 敌.蓄力 / Mathf.Max(.01f, 敌.蓄力总秒)) : 0;
                float 挤 = 蓄 * 天帝敌种配置.取("presentation.windup_squash");
                float 击 = Mathf.Clamp01((图.有受击快照?图.受击闪白秒:敌.闪白秒) / 天帝受击表现.取("flash_seconds")) * 天帝敌种配置.取("presentation.hit_stretch");
                var 比例 = 敌图[i].像.transform.localScale;
                敌图[i].像.transform.localScale = new Vector3(比例.x * (1 + 挤 - 击), 比例.y * (1 - 挤 + 击), 1);
                if (蓄 > 0 || 击 > 0) 敌图[i].像.transform.rotation = 平面旋转 * Quaternion.Euler(0, 0, (敌.锁定方向.x < 0 ? 1 : -1) * (蓄 * 9 + 击 * 45));
                else if(图.反冲秒>0)图.像.transform.rotation=平面旋转*Quaternion.Euler(0,0,(图.朝向.x<0?1:-1)*天帝敌种配置.取("presentation.enemy_recoil_angle")*图.反冲秒/天帝敌种配置.取("presentation.enemy_recoil_seconds"));
                }
            }
            else 敌图[i].像.transform.rotation=平面旋转*Quaternion.Euler(0,0,(i%2==0?1:-1)*天帝敌种配置.取("presentation.enemy_death_angle")*Mathf.Clamp01(敌.死亡秒/.65f));
            敌图[i].像.transform.position += new Vector3(0, 0, 敌.跳跃高度);
            敌图[i].上次位置 = 敌.位置;
            敌图[i].上次行动=敌.行动;
        }
        foreach (var 影 in 水镜虚影) 影.enabled = false;
        int 幻数 = 0;
        foreach (var 敌 in 战斗.敌人)
            if (敌.存活 && 敌.行动 == 敌人行动.蓄力 && 敌.技能编号 == 18)
            {
                var 垂 = new Vector2(-敌.锁定方向.y, 敌.锁定方向.x);
                for (int i = 0; i < 2 && 幻数 < 水镜虚影.Count; i++)
                {
                    var 点 = 敌.位置 + 垂 * (i == 0 ? -1 : 1) * 天帝敌种配置.取("boss.mirror_offset");
                    if (地图.可站立(点)) { var 影 = 水镜虚影[幻数++]; 影.enabled = true; 影.transform.position = new Vector3(点.x, .12f, 点.y); 影.sortingOrder = 深度(点); }
                }
            }
        更新玩家遮挡(战斗); 更新掉落(战斗); 更新特效(秒);
    }
    void 更新玩家遮挡(天帝战斗系统 战斗)
    {
        bool 遮挡 = false;
        if (!战斗.玩家死亡)
            for (int i = 0; i < 敌图.Count; i++)
            {
                var 像 = 敌图[i].像;
                if (像.enabled && 战斗.敌人[i].存活 && 像.sortingOrder >= 玩家图.像.sortingOrder &&
                    天帝战斗辨识.平面相交(玩家图.像.bounds, 像.bounds)) { 遮挡 = true; break; }
            }
        玩家遮挡提示.enabled = 遮挡;
        if (!遮挡) return;
        玩家遮挡提示.sprite = 玩家图.像.sprite;
        玩家遮挡提示.transform.SetPositionAndRotation(玩家图.像.transform.position, 玩家图.像.transform.rotation);
        玩家遮挡提示.transform.localScale = 玩家图.像.transform.localScale;
        玩家遮挡提示.flipX = 玩家图.像.flipX;
    }
    void 更新单位(单位图 图, Vector2 点, bool 活, float 死亡秒, float 闪白秒, bool 蓄力, float 倾角, int 序号, bool 使用方向帧 = false, Color? 基色 = null)
    {
        if (!活 && 死亡秒 >= .65f) { 图.像.enabled = false; if (图.阴影 != null) 图.阴影.enabled = false; return; }
        图.像.enabled = true; var 位移 = 点 - 图.上次位置; bool 动 = 位移.sqrMagnitude > .00001f;
        if (使用方向帧) 图.像.flipX = false;
        else if (Mathf.Abs(位移.x) > .001f) 图.像.flipX = 位移.x < 0;
        float 呼吸 = 使用方向帧 ? 1 : 1 + Mathf.Sin(总秒 * 3 + 序号) * .012f;
        float 倾 = 使用方向帧 ? 倾角 : 动 ? Mathf.Sin(总秒 * 13 + 序号) * 2 : 倾角;
        float 缩 = 活 ? 1 : Mathf.Max(.08f, 1 - 死亡秒 / .65f);
        if (图.阴影 != null)
        {
            图.阴影.enabled = true;
            图.阴影.transform.position = new Vector3(点.x, .06f, 点.y);
            染色(图.阴影, new Color(0, 0, 0, .46f * 缩));
        }
        图.像.transform.position = new Vector3(点.x, .12f, 点.y + (动 && !使用方向帧 ? Mathf.Abs(Mathf.Sin(总秒 * 13)) * .035f : 0));
        图.像.transform.rotation = 平面旋转 * Quaternion.Euler(0, 0, 倾);
        图.像.transform.localScale = new Vector3(图.比例 * 缩, 图.比例 * 缩 * (蓄力 && !使用方向帧 ? .96f : 呼吸), 1);
        图.像.sortingOrder = 深度(点);
        Color 色 = !活 ? new Color(.36f, .39f, .40f, Mathf.Clamp01(1 - 死亡秒 / .65f)) : 蓄力 ? new Color(1, .74f, .58f) : Color.white;
        if (活) 色 *= 基色 ?? Color.white;
        float 闪=Mathf.Clamp01((图.有受击快照?图.受击闪白秒:闪白秒)/天帝受击表现.取("flash_seconds"));
        if(图.受击秒>0)
        {
            float 弹=天帝受击表现.回弹(图.受击秒,天帝受击表现.取("recoil_seconds"))*图.受击力度;
            var 偏移=图.受击方向*弹*天帝受击表现.取("recoil_distance");
            图.像.transform.position+=new Vector3(偏移.x,0,偏移.y);
            if (!使用方向帧)
            {
                float 挤=Mathf.Abs(弹)*.055f;
                var 大小=图.像.transform.localScale;图.像.transform.localScale=new Vector3(大小.x*(1+挤),大小.y*(1-挤),1);
            }
            色=Color.Lerp(色,图.受击色,闪*.45f);
        }
        染色(图.像, 色, 闪*.85f, 图.参数);
    }
    void 更新掉落(天帝战斗系统 战斗)
    {
        foreach (var 物 in 战斗.掉落.地面)
        {
            if (物.演出结束)
            {
                if (道纹图.TryGetValue(物, out var 旧图)) foreach (var 旧像 in 旧图) if (旧像 != null) 旧像.enabled = false;
                continue;
            }
            if (!道纹图.TryGetValue(物, out var 图))
            {
                int 属性序 = (int)物.道纹.属性;
                string 编号 = 物.道纹.分类 == 道纹分类.分叉 || 属性序 >= 10 ? "DW01" : 属性序 >= 5 ? "FN" + (属性序 - 4).ToString("00") : "AT" + (属性序 + 1).ToString("00");
                图 = new[] { 建像("地面道纹壳", "DW01", 物.位置, Vector2.one * 1.1f, 12000, 90), 建像("地面道纹属性", 编号, 物.位置, Vector2.one * .58f, 12001) };
                道纹图.Add(物, 图);
                染色(图[1], 天帝道纹品阶.获取(物.道纹.品阶).颜色);
            }
            for (int i = 0; i < 图.Length; i++)
                更新掉落像(图[i], 物.显示位置, 物.吸附, i == 0 ? 1.1f : .58f);
        }
        foreach (var 物 in 战斗.通货掉落.地面)
        {
            if (物.演出结束)
            {
                if (通货图.TryGetValue(物, out var 旧像) && 旧像 != null) 旧像.enabled = false;
                continue;
            }
            if (!通货图.TryGetValue(物, out var 像))
            { 像 = 建像("地面通货-" + 物.种类, "CU" + ((int)物.种类 + 1).ToString("00"), 物.位置, Vector2.one * .85f, 12002); 通货图.Add(物, 像); }
            更新掉落像(像, 物.显示位置, 物.吸附, .85f);
        }
    }
    void 更新掉落像(SpriteRenderer 像, Vector2 点, 天帝掉落吸附 吸附, float 宽)
    {
        if (像 == null) return;
        像.enabled = true;
        float 浮动 = 吸附 == null ? Mathf.Sin(总秒 * 3) * .04f : 0;
        像.transform.position = new Vector3(点.x, .26f, 点.y + 浮动);
        float 尺寸 = 宽 * (吸附?.缩放 ?? 1);
        var 基准 = 像.sprite.bounds.size;
        像.transform.localScale = new Vector3(尺寸 / 基准.x, 尺寸 / 基准.y, 1);
    }
    void 发特效(string 编号, Vector2 点, float 宽, float 寿命, Color 色, Vector2 方向 = default)
    {
        短效 项 = null;
        foreach (var 旧 in 特效池) if (旧.剩余 <= 0) { 项 = 旧; break; }
        if (项 == null)
        {
            if (特效池.Count >= 48) return;
            var 像 = 建像("静态贴图特效", 编号, 点, Vector2.one, 25000); if (像 == null) return;
            项 = new 短效 { 像 = 像 }; 特效池.Add(项);
        }
        项.像.sprite = 素材.获取(编号); 项.位置 = 点; 项.漂移 = 方向.sqrMagnitude > 0 ? -方向.normalized * .8f : Vector2.zero;
        项.寿命 = 项.剩余 = 寿命; 项.宽 = 宽; 项.色 = 色; 项.像.enabled = true;
        项.像.transform.rotation = 平面旋转 * Quaternion.Euler(0, 0, 方向.sqrMagnitude > 0 ? Mathf.Atan2(方向.y, 方向.x) * Mathf.Rad2Deg : 0);
    }
    void 更新特效(float 秒)
    {
        foreach (var 项 in 特效池)
        {
            if (项.剩余 <= 0) continue;
            项.剩余 -= 秒; if (项.剩余 <= 0) { 项.像.enabled = false; continue; }
            项.位置 += 项.漂移 * 秒;
            项.像.transform.position = new Vector3(项.位置.x, .38f, 项.位置.y);
            float 宽 = 项.宽 * (1 + (1 - 项.剩余 / 项.寿命) * .3f);
            项.像.transform.localScale = new Vector3(宽 / 项.像.sprite.bounds.size.x, 宽 / 项.像.sprite.bounds.size.x, 1);
            var 色 = 项.色; 色.a *= 项.剩余 / 项.寿命; 染色(项.像, 色);
        }
    }
    public void Dispose()
    {
        if (动画事件源 != null) { 动画事件源.准备射击 -= 准备主角射击; 动画事件源.射击释放 -= 释放主角射击; }
        if (根 != null) 释放资源(根.gameObject);
        释放资源(材质);
        foreach (var 网 in 地表网格) 释放资源(网);
        foreach (var 地材 in 地表材质) 释放资源(地材);
        地表网格.Clear(); 地表材质.Clear();
        敌图.Clear(); 特效池.Clear(); 水镜虚影.Clear(); 道纹图.Clear(); 通货图.Clear(); 玩家图 = null;
    }
    static void 释放资源(UnityEngine.Object o)
    { if(o==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o); }
}
