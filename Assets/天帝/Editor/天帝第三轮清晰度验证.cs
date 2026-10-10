#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class 天帝第三轮清晰度验证
{
    static readonly BindingFlags 私有实例 = BindingFlags.Instance | BindingFlags.NonPublic;
    static 天帝真实数值验证.报告 结果;
    static void 检查(string 名称, bool 通过) => (通过 ? 结果.通过 : 结果.失败).Add(名称);

    public static string 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式验证战斗清晰度生命周期。");
        结果 = new 天帝真实数值验证.报告();
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/第三轮清晰度-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(目录);
        int 原限制 = QualitySettings.globalTextureMipmapLimit;
        AnisotropicFiltering 原过滤 = QualitySettings.anisotropicFiltering;
        var 物 = new GameObject("第三轮清晰度隔离场景");
        var 场景 = 物.AddComponent<天帝战斗场景>();
        try
        {
            QualitySettings.globalTextureMipmapLimit = 1;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            typeof(天帝战斗场景).GetMethod("提升战斗清晰度", 私有实例).Invoke(场景, null);
            检查("战斗清晰度-进入时解除全局纹理降采样", QualitySettings.globalTextureMipmapLimit == 0);
            检查("战斗清晰度-进入时启用各向异性过滤", QualitySettings.anisotropicFiltering == AnisotropicFiltering.Enable);
            typeof(天帝战斗场景).GetMethod("恢复战斗清晰度", 私有实例).Invoke(场景, null);
            检查("战斗清晰度-退出时恢复纹理质量档", QualitySettings.globalTextureMipmapLimit == 1);
            检查("战斗清晰度-退出时恢复各向异性设置", QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable);
        }
        catch (Exception 异常) { 结果.错误.Add(异常.ToString()); }
        finally
        {
            QualitySettings.globalTextureMipmapLimit = 原限制;
            QualitySettings.anisotropicFiltering = 原过滤;
            UnityEngine.Object.DestroyImmediate(物);
        }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        Debug.Log("第三轮清晰度验证：" + 目录 + "；通过 " + 结果.通过.Count + "，失败 " + 结果.失败.Count + "，错误 " + 结果.错误.Count);
        return 目录;
    }
}
#endif
