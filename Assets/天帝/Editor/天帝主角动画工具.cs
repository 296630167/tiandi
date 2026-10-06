#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class 天帝主角动画工具
{
    const string 帧目录 = "Assets/天帝/美术/角色/主角八方向";
    const string 配置路径 = 帧目录 + "/主角八方向帧动画.asset";
    const string 新帧目录 = "Assets/天帝/美术/角色/主角十组动画";
    const string 新配置路径 = 新帧目录 + "/主角十组帧动画.asset";
    [MenuItem("天帝/美术/接入主角八方向动画")]
    public static void 接入菜单() => Debug.Log(接入());
    public static string 接入()
    {
        if (Directory.Exists(新帧目录)) return 接入十组();
        if (EditorApplication.isPlaying) throw new InvalidOperationException("运行中不改素材绑定，请先停止运行。");
        for (int i = 0; i < 8; i++) for (int 帧 = 0; 帧 <= 6; 帧++)
            if (!File.Exists(帧路径((角色朝向)i, 帧))) throw new FileNotFoundException("八方向素材尚未生成完整", 帧路径((角色朝向)i, 帧));
        AssetDatabase.Refresh();
        var 动作 = new 天帝角色帧动画.方向动作[8];
        for (int i = 0; i < 8; i++)
        {
            var 项 = new 天帝角色帧动画.方向动作 { 朝向 = (角色朝向)i, 移动帧 = new Sprite[6] }; 动作[i] = 项;
            for (int 帧 = 0; 帧 <= 6; 帧++)
            {
                string 路径 = 帧路径(项.朝向, 帧); var 导入 = (TextureImporter)AssetImporter.GetAtPath(路径);
                导入.textureType = TextureImporterType.Sprite; 导入.spriteImportMode = SpriteImportMode.Single;
                导入.alphaIsTransparency = true; 导入.mipmapEnabled = false; 导入.isReadable = false;
                导入.sRGBTexture = true; 导入.wrapMode = TextureWrapMode.Clamp; 导入.filterMode = FilterMode.Bilinear;
                导入.maxTextureSize = 256; 导入.textureCompression = TextureImporterCompression.Uncompressed;
                导入.spritePixelsPerUnit = 100;
                var 设置 = new TextureImporterSettings(); 导入.ReadTextureSettings(设置);
                设置.spriteMeshType = SpriteMeshType.FullRect; 设置.spriteAlignment = (int)SpriteAlignment.Custom;
                // 所有PNG脚底在从顶端算236像素处，战斗权威位置对应脚底，不是图片中心。
                设置.spritePivot = new Vector2(.5f, 20f / 256); 导入.SetTextureSettings(设置);
                导入.SaveAndReimport(); var 图 = AssetDatabase.LoadAssetAtPath<Sprite>(路径);
                if (帧 == 0) 项.待机 = 图; else 项.移动帧[帧 - 1] = 图;
            }
        }
        var 配置 = AssetDatabase.LoadAssetAtPath<天帝角色帧动画>(配置路径);
        if (配置 == null) { 配置 = ScriptableObject.CreateInstance<天帝角色帧动画>(); AssetDatabase.CreateAsset(配置, 配置路径); }
        配置.动作 = 动作; 配置.展示高度 = 3.5f; 配置.走路帧率 = 10; 配置.参考移动速度 = 6;
        if (!配置.完整) throw new InvalidOperationException("方向动画缺少帧，不修改主角绑定。");
        var 资源 = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        if (资源 == null) throw new InvalidOperationException("现有美术资源配置缺失。");
        资源.主角移动动画 = 配置; EditorUtility.SetDirty(配置); EditorUtility.SetDirty(资源);
        AssetDatabase.SaveAssets(); return "已绑定主角8方向×6帧，固定脚底轴心，原主页素材保持现有绑定。";
    }
    static string 帧路径(角色朝向 朝向, int 帧) => 帧目录 + "/主角_" + 朝向 + (帧 == 0 ? "_待机" : "_移动_" + 帧.ToString("00")) + ".png";

    [MenuItem("天帝/美术/接入主角十组动画")]
    public static void 接入十组菜单() => Debug.Log(接入十组());
    public static string 接入十组()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("运行中不修改动画绑定，请先停止运行。");
        var 路径们 = new List<string>();
        for (int i = 0; i < 8; i++)
        {
            路径们.Add(新帧目录 + "/主角_基准_" + (角色朝向)i + ".png");
            for (int 帧 = 1; 帧 <= 16; 帧++) 路径们.Add(新帧目录 + "/主角_移动_" + (角色朝向)i + "_" + 帧.ToString("00") + ".png");
        }
        for (int 帧 = 1; 帧 <= 16; 帧++) foreach (string 动作 in new[] { "待机", "射击" })
            路径们.Add(新帧目录 + "/主角_" + 动作 + "_南_" + 帧.ToString("00") + ".png");
        // 全部检查通过之后才改变现有资源引用；不保存或重建用户场景。
        foreach (var 路 in 路径们) if (!File.Exists(路)) throw new FileNotFoundException("十组动画缺帧，保持原主角绑定。", 路);
        AssetDatabase.Refresh();
        foreach (var 路 in 路径们)
        {
            var 导入 = (TextureImporter)AssetImporter.GetAtPath(路);
            导入.textureType = TextureImporterType.Sprite; 导入.spriteImportMode = SpriteImportMode.Single;
            导入.alphaIsTransparency = true; 导入.mipmapEnabled = false; 导入.isReadable = false;
            导入.sRGBTexture = true; 导入.wrapMode = TextureWrapMode.Clamp; 导入.filterMode = FilterMode.Bilinear;
            导入.maxTextureSize = 512; 导入.textureCompression = TextureImporterCompression.CompressedHQ;
            导入.spritePixelsPerUnit = 100;
            var 设置 = new TextureImporterSettings(); 导入.ReadTextureSettings(设置);
            设置.spriteMeshType = SpriteMeshType.FullRect; 设置.spriteAlignment = (int)SpriteAlignment.Custom;
            设置.spritePivot = new Vector2(.5f, 48f / 512); 导入.SetTextureSettings(设置);
            导入.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Standalone", overridden = true, maxTextureSize = 512, format = TextureImporterFormat.BC7, compressionQuality = 100 });
            导入.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 512, format = TextureImporterFormat.ASTC_4x4, compressionQuality = 100 });
            导入.SaveAndReimport();
        }
        Sprite 取(string 名) => AssetDatabase.LoadAssetAtPath<Sprite>(新帧目录 + "/主角_" + 名 + ".png");
        var 配置 = AssetDatabase.LoadAssetAtPath<天帝角色帧动画>(新配置路径);
        if (配置 == null) { 配置 = ScriptableObject.CreateInstance<天帝角色帧动画>(); AssetDatabase.CreateAsset(配置, 新配置路径); }
        配置.动作 = new 天帝角色帧动画.方向动作[8];
        for (int i = 0; i < 8; i++)
        {
            var 项 = new 天帝角色帧动画.方向动作 { 朝向 = (角色朝向)i, 待机 = 取("基准_" + (角色朝向)i), 移动帧 = new Sprite[16] };
            for (int 帧 = 0; 帧 < 16; 帧++) 项.移动帧[帧] = 取("移动_" + 项.朝向 + "_" + (帧 + 1).ToString("00"));
            配置.动作[i] = 项;
        }
        配置.正面待机帧 = new Sprite[16]; 配置.正面射击帧 = new Sprite[16];
        for (int 帧 = 0; 帧 < 16; 帧++)
        { 配置.正面待机帧[帧] = 取("待机_南_" + (帧 + 1).ToString("00")); 配置.正面射击帧[帧] = 取("射击_南_" + (帧 + 1).ToString("00")); }
        配置.展示高度 = 3.5f; 配置.走路帧率 = 16; 配置.参考移动速度 = 6; 配置.待机帧率 = 8;
        配置.射击释放帧 = 7; 配置.射击回收秒 = .28f;
        if (!配置.十组完整) throw new InvalidOperationException("十组帧引用不完整，保持原主角绑定。");
        var 资源 = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        if (资源 == null) throw new InvalidOperationException("美术资源缺失，保持原主角绑定。");
        资源.主角移动动画 = 配置; EditorUtility.SetDirty(配置); EditorUtility.SetDirty(资源);
        AssetDatabase.SaveAssets(); return "已接入十组×16帧；512统一画布、固定(256,464)根节点、正面待机和真实发射同步的抬手射击。";
    }

    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(); public string 错误; }
    public static string 验证()
    {
        var 结果 = new 报告();
        void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
        try
        {
            var 配置 = AssetDatabase.LoadAssetAtPath<天帝角色帧动画>(Directory.Exists(新帧目录) ? 新配置路径 : 配置路径);
            if (配置 == null) throw new InvalidOperationException("尚未导入八方向素材。");
            int 帧数 = 配置.获取(角色朝向.南).移动帧.Length;
            检查("8方向各" + 帧数 + "帧完整", 配置.完整);
            var 资源 = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
            检查("现有战斗美术引用新动画", 资源 != null && 资源.主角移动动画 == 配置);
            var 全帧 = new HashSet<Sprite>();
            for (int i = 0; i < 8; i++)
            {
                var 项 = 配置.获取((角色朝向)i);
                foreach (var 帧 in 项.移动帧)
                {
                    全帧.Add(帧);
                    var 尺寸 = 帧数 == 16 ? new Vector2(512,512) : new Vector2(256,256);
                    var 轴心 = 帧数 == 16 ? new Vector2(256,48) : new Vector2(128,20);
                    检查("帧尺寸轴心一致 " + 帧.name, 帧.rect.size == 尺寸 && Vector2.Distance(帧.pivot, 轴心) < .01f);
                }
                float 角 = i * Mathf.PI / 4; var 向 = new Vector2(Mathf.Sin(角), -Mathf.Cos(角));
                var 播放 = new 天帝八方向播放(配置); 播放.推进(向 * .6f, .1f, true);
                检查("方向选择 " + (角色朝向)i, 播放.朝向 == (角色朝向)i && 播放.移动中);
                var 已见 = new HashSet<Sprite>(); for (int 帧 = 0; 帧 < 帧数 * 3; 帧++) 已见.Add(播放.推进(向 * .3f, .05f, true));
                检查("走路播放完全部帧并循环 " + (角色朝向)i, 已见.Count == 帧数);
                播放.推进(Vector2.zero, .1f, true);
                检查("停下保留朝向并停止踩步 " + (角色朝向)i, 播放.朝向 == (角色朝向)i && !播放.移动中 && (播放.当前精灵 == 项.待机 || (i == 0 && Array.IndexOf(配置.正面待机帧, 播放.当前精灵) >= 0)));
                var 旧 = 播放.当前精灵; 播放.推进(向, .1f, false);
                检查("死亡不播放移动 " + (角色朝向)i, !播放.移动中 && 播放.当前精灵 == 旧);
            }
            检查("全部方向移动帧独立引用", 全帧.Count == 帧数 * 8);
            if (帧数 == 16)
            {
                检查("十组动作完整", 配置.十组完整);
                var 待机 = new 天帝八方向播放(配置); var 待机已见 = new HashSet<Sprite>();
                for (int i = 0; i < 32; i++) 待机已见.Add(待机.推进(Vector2.zero,.125f,true));
                检查("正面16帧待机循环", 待机已见.Count == 16);
                待机.准备射击(Vector2.down,.2f); 待机.推进(Vector2.zero,.1f,true);
                检查("准备时处于真实释放帧之前", Array.IndexOf(配置.正面射击帧,待机.当前精灵) >= 0 && Array.IndexOf(配置.正面射击帧,待机.当前精灵) < 配置.射击释放帧);
                待机.释放射击(); 待机.推进(Vector2.zero,0,true);
                检查("真实释放事件切到释放帧", 待机.当前精灵 == 配置.正面射击帧[配置.射击释放帧]);
                for (int i = 0; i < 6; i++) 待机.推进(Vector2.zero,.1f,true);
                检查("射击回收后归待机", Array.IndexOf(配置.正面待机帧,待机.当前精灵) >= 0);
                待机.准备射击(Vector2.up,.2f); 待机.推进(Vector2.zero,.05f,true);
                检查("无背面射击时不伪装正面射击", Array.IndexOf(配置.正面射击帧,待机.当前精灵) < 0);
            }
            var 稳定 = new 天帝八方向播放(配置);
            for (int i = 0; i < 20; i++)
            {
                float 角 = (i % 2 == 0 ? 22 : 24) * Mathf.Deg2Rad;
                稳定.推进(new Vector2(Mathf.Sin(角), -Mathf.Cos(角)) * .3f, .05f, true);
            }
            检查("分界小抖动不反复翻转", 稳定.朝向 == 角色朝向.南);
            var 普通 = new 天帝八方向播放(配置); var 跑步 = new 天帝八方向播放(配置);
            普通.推进(Vector2.right * .6f, .1f, true); 跑步.推进(Vector2.right * 1.0f, .1f, true);
            普通.推进(Vector2.right * .6f, .1f, true); 跑步.推进(Vector2.right * 1.0f, .1f, true);
            检查("跑步按实际速度加快帧率", 普通.当前精灵 != 跑步.当前精灵);
            var 旧帧 = 普通.当前精灵; 普通.推进(new Vector2(float.NaN,0), .1f, true);
            检查("无效输入保留精灵", 普通.当前精灵 == 旧帧);
            var 图 = new 天帝战斗地图(42, true); var 点 = 图.出生位置;
            for (int i = 0; i < 200; i++)
            {
                var 新点 = 图.移动(点, Vector2.down, .6f); 普通.推进(新点 - 点, .1f, true); 点 = 新点;
            }
            检查("玩家顶空气墙停步且仍在可移动区域", !普通.移动中 && 图.可站立(点));
        }
        catch (Exception ex) { 结果.错误 = ex.ToString(); }
        string 路径 = Path.Combine(天帝构建工具.项目根,"生成/验证/主角八方向-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(路径)); File.WriteAllText(路径, JsonUtility.ToJson(结果,true));
        return "通过="+结果.通过.Count+"，失败="+结果.失败.Count+"，错误="+(string.IsNullOrEmpty(结果.错误)?0:1)+"；"+路径;
    }
}
#endif
