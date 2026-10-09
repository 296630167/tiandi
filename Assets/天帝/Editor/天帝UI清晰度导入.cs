#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

// 旧资源的导入设置不会因为 AssetPostprocessor 修改而自动重导入。
// 首次编译后补齐已有 UI 资源的平台覆盖，避免编辑器截图和移动包使用旧压缩纹理。
public static class 天帝UI清晰度导入
{
    static readonly string[] 目录 =
    {
        "Assets/天帝/Resources/剪纸界面",
        "Assets/天帝/Resources/青绿界面",
        "Assets/天帝/Resources/山水首两页",
        "Assets/天帝/Resources/山水图三四",
        "Assets/天帝/Resources/山水剩余界面",
        "Assets/天帝/Resources/山水战斗HUD",
        "Assets/天帝/Resources/通货图标"
    };

    [InitializeOnLoadMethod]
    static void 启动修复()
    {
        EditorApplication.delayCall += 修复已有资源;
    }

    [MenuItem("天帝/修复UI纹理清晰度")]
    public static void 修复已有资源()
    {
        int 数量 = 0;
        foreach (var 根 in 目录)
        {
            if (!Directory.Exists(根)) continue;
            foreach (var 文件 in Directory.GetFiles(根, "*.png", SearchOption.TopDirectoryOnly))
            {
                var 路径 = 文件.Replace('\\', '/');
                var 入 = AssetImporter.GetAtPath(路径) as TextureImporter;
                if (入 == null || !需要修复(入)) continue;
                设置无损(入, "Android"); 设置无损(入, "iPhone"); 设置无损(入, "Standalone");
                入.SaveAndReimport(); 数量++;
            }
        }
        if (数量 > 0) Debug.Log("天帝 UI 纹理清晰度已修复：" + 数量 + " 个资源。");
    }

    static bool 需要修复(TextureImporter 入)
    {
        foreach (var 平台 in new[] { "Android", "iPhone", "Standalone" })
        {
            var s = 入.GetPlatformTextureSettings(平台);
            if (!s.overridden || s.textureCompression != TextureImporterCompression.Uncompressed || s.crunchedCompression)
                return true;
        }
        return false;
    }

    static void 设置无损(TextureImporter 入, string 平台)
    {
        var s = 入.GetPlatformTextureSettings(平台); s.name = 平台; s.overridden = true;
        s.maxTextureSize = Mathf.Max(2048, s.maxTextureSize);
        s.textureCompression = TextureImporterCompression.Uncompressed;
        s.crunchedCompression = false; s.compressionQuality = 100;
        入.SetPlatformTextureSettings(s);
    }
}
#endif
