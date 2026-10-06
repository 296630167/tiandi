#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class 天帝美术接入
{
    const string 根 = "Assets/天帝/美术/生成素材";
    [MenuItem("天帝/美术/导入并绑定生成素材")]
    public static void 菜单() => 导入();
    public static string 导入()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式。");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("当前场景有未保存修改，请先保存。");
        AssetDatabase.Refresh();
        var 路径 = Directory.GetFiles(根, "*.png").OrderBy(p => p).ToArray();
        foreach (var 文件 in 路径)
        {
            var 导入器 = AssetImporter.GetAtPath(文件) as TextureImporter;
            if (导入器 == null) throw new InvalidOperationException("图片未识别：" + 文件);
            string 编号 = Path.GetFileName(文件).Split('_')[0]; bool 地面 = 编号.StartsWith("TL");
            导入器.textureType = 编号 == "UI" ? TextureImporterType.Default : TextureImporterType.Sprite;
            导入器.spriteImportMode = SpriteImportMode.Single;
            导入器.alphaIsTransparency = true; 导入器.isReadable = false; 导入器.sRGBTexture = true;
            导入器.mipmapEnabled = 地面; 导入器.wrapMode = 地面 ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            导入器.filterMode = FilterMode.Bilinear;
            导入器.maxTextureSize = 编号.StartsWith("BG") && 编号 != "BG02" || 编号.StartsWith("SC") ? 4096 : 编号 == "UI" || 编号 == "CH01" ? 2048 : 1024;
            导入器.textureCompression = 编号 == "UI" || 编号.StartsWith("AT") || 编号.StartsWith("FN") || 编号.StartsWith("TF") || 编号.StartsWith("SK") ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            var 设置 = new TextureImporterSettings(); 导入器.ReadTextureSettings(设置);
            设置.spriteMeshType = SpriteMeshType.FullRect; 设置.spriteAlignment = (int)SpriteAlignment.Custom;
            设置.spritePivot = 编号 == "CH02" || 编号.StartsWith("EN") ? new Vector2(.5f, .12f) : new Vector2(.5f, .5f);
            导入器.SetTextureSettings(设置);
            导入器.SaveAndReimport();
        }
        string 配置路径 = "Assets/天帝/美术/天帝美术资源.asset";
        var 配置 = AssetDatabase.LoadAssetAtPath<天帝美术资源>(配置路径);
        if (配置 == null) { 配置 = ScriptableObject.CreateInstance<天帝美术资源>(); AssetDatabase.CreateAsset(配置, 配置路径); }
        配置.图片 = 路径.Where(p => !Path.GetFileName(p).StartsWith("UI_")).Select(p => new 天帝美术资源.图片条目 { 编号 = Path.GetFileName(p).Split('_')[0], 图片 = AssetDatabase.LoadAssetAtPath<Sprite>(p) }).ToArray();
        if (配置.图片.Length != 75 || 配置.图片.Any(p => p.图片 == null)) throw new InvalidOperationException("75张素材未完整导入。");
        配置.道纹图集 = AssetDatabase.LoadAssetAtPath<Texture2D>(根 + "/UI_道纹图集.png");
        配置.立绘着色器 = AssetDatabase.LoadAssetAtPath<Shader>("Assets/天帝/美术/静态立绘.shader");
        if (配置.立绘着色器 == null || ShaderUtil.ShaderHasError(配置.立绘着色器)) throw new InvalidOperationException("立绘Shader编译失败。");
        EditorUtility.SetDirty(配置);
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (游戏 == null) throw new InvalidOperationException("主页场景入口缺失。");
        游戏.美术 = 配置; 游戏.主页背景 = 配置.获取("BG01"); 游戏.主角立绘 = 配置.获取("CH01");
        EditorUtility.SetDirty(游戏); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { 配置.获取("APP01").texture }, IconKind.Any);
        AssetDatabase.SaveAssets();
        return "75张图片、26项UI图集、主页引用与应用图标已导入绑定。";
    }
}
#endif
