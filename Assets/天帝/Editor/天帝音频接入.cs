#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class 天帝音频接入
{
    public static string 诊断()
    {
        var t = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil");
        return "Listener pause=" + AudioListener.pause + "\n" + string.Join("\n", t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Where(m => m.Name.Contains("Mute") || m.Name.Contains("Disabled") || m.Name.Contains("Output")).Select(m=>m.ToString()));
    }
    public static string 导入()
    {
        int 数 = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/天帝/Resources/天帝音频" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var 导入器 = (AudioImporter)AssetImporter.GetAtPath(path);
            bool 音乐 = path.Contains("/音乐/"), 语音 = path.Contains("/配音/");
            var 设置 = 导入器.defaultSampleSettings;
            设置.loadType = 音乐 ? AudioClipLoadType.Streaming : 语音 ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            设置.compressionFormat = AudioCompressionFormat.Vorbis; 设置.quality = 语音 ? .85f : 音乐 ? .80f : .90f;
            设置.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            设置.preloadAudioData = !音乐;
            导入器.defaultSampleSettings = 设置; 导入器.forceToMono = !音乐; 导入器.loadInBackground = false;
            导入器.SaveAndReimport(); 数++;
        }
        return "已配置" + 数 + "个音频；未保存或重建场景";
    }
    public static string 验证素材()
    {
        int 数 = 0;
        foreach (string 名 in 天帝声音.音乐名称) 检查("音乐/" + 名, ref 数);
        foreach (string 名 in 天帝声音.音效名称) 检查("音效/" + 名, ref 数);
        for (int i = 1; i <= 24; i++) 检查("配音/VO" + i.ToString("00"), ref 数);
        return "完整有效音频=" + 数;
    }
    static void 检查(string 路径, ref int 数)
    {
        var 片 = Resources.Load<AudioClip>("天帝音频/" + 路径);
        if (片 == null || 片.length < .1f || 片.samples < 1 || 片.channels < 1) throw new InvalidOperationException("无效音频：" + 路径);
        数++;
    }
}
#endif
