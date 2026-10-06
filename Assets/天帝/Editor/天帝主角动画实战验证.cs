#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 使用隔离存档，在真实战斗场景验证表现层；不改变玩家存档或场景文件。
public static class 天帝主角动画实战验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static string 目录, 原存档目录;
    static bool 原后台, 原选项启用, 已进入, 已结束;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    public static string 启动()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("当前需为已保存的编辑状态。");
        if (EditorSceneManager.GetActiveScene().path != "Assets/天帝/场景/天帝.unity") throw new InvalidOperationException("请在主页场景运行此检查。");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/动画实战-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        var 货 = new 天帝通货(网,42);
        if (!new 天帝存档(Path.Combine(目录,"隔离存档")).保存(new 天帝存档数据 { 序章已完成=true, 主角=天帝普攻.主角配置(), 画布=网.导出存档(), 通货=货.导出库存() }))
            throw new InvalidOperationException("隔离存档准备失败。");
        原存档目录 = 天帝存档.验证目录; 天帝存档.验证目录 = Path.Combine(目录,"隔离存档");
        原后台=Application.runInBackground; 原选项启用=EditorSettings.enterPlayModeOptionsEnabled; 原选项=EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled=true; EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        结果=new 报告(); 已进入=false; 已结束=false; 截止=EditorApplication.timeSinceStartup+40;
        Application.logMessageReceived+=记错; EditorApplication.update+=等待; EditorApplication.isPlaying=true;
        return 目录;
    }
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static void 记错(string 消息,string 栈,LogType 类) { if(类==LogType.Error||类==LogType.Exception) 结果.错误.Add(消息+"\n"+栈); }
    static void 等待()
    {
        if (EditorApplication.timeSinceStartup>截止) { 结果.错误.Add("真实场景动画检查超时"); 完成(); return; }
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground=true; EditorApplication.QueuePlayerLoopUpdate();
        var 游戏=UnityEngine.Object.FindAnyObjectByType<天帝游戏>(); if(游戏==null) return;
        try
        {
            if(!已进入)
            {
                检查("从隔离存档继续成功",游戏.继续游戏());
                检查("主页可进入真实战斗",游戏.进入战斗()); 已进入=true; return;
            }
            if(游戏.阶段!=游戏阶段.战斗) return;
            var 场=游戏.战斗场景; 场.enabled=false;
            var 图=场.GetComponentsInChildren<SpriteRenderer>().Single(p=>p.name=="程序员主角");
            var 配=游戏.美术.主角移动动画;
            检查("场景美术使用新主角资源",场.美术.可用 && 配.完整);
            for(int i=0;i<8;i++)
            {
                float 角=i*Mathf.PI/4; var 向=new Vector2(Mathf.Sin(角),-Mathf.Cos(角));
                var 帧集=new HashSet<Sprite>(); bool 足底一致=true;
                for(int j=0;j<18;j++)
                {
                    场.移动一步(向,false,.05f); 场.美术.更新(场.战斗,场.玩家位置,.05f); 帧集.Add(图.sprite);
                    var 脚=图.transform.position; 足底一致 &= Vector2.Distance(new Vector2(脚.x,脚.z),场.玩家位置)<.001f;
                }
                var 项=配.获取((角色朝向)i);
                检查("真实位移选择对应方向帧 "+(角色朝向)i,帧集.All(p=>项.移动帧.Contains(p)) && 帧集.Count>=5);
                检查("无旧翻转和脚底漂移 "+(角色朝向)i,!图.flipX && 足底一致);
                场.美术.更新(场.战斗,场.玩家位置,.1f);
                检查("停止后保留朝向站立 "+(角色朝向)i,图.sprite==项.待机);
            }
            // 用真实场景相机留一张显示检查图；不操作窗口尺寸或保存场景。
            场.移动一步(Vector2.down,false,.1f); 场.美术.更新(场.战斗,场.玩家位置,.1f);
            场.美术.更新(场.战斗,场.玩家位置,.1f);
            var 相机=场.俯视相机; var 原目标=相机.targetTexture;
            var 缓冲=RenderTexture.GetTemporary(1280,720,24); var 原活动=RenderTexture.active; var 预览=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                相机.targetTexture=缓冲; 相机.Render(); RenderTexture.active=缓冲;
                预览.ReadPixels(new Rect(0,0,1280,720),0,0); 预览.Apply();
                File.WriteAllBytes(Path.Combine(目录,"战斗主角_显示检查.png"),预览.EncodeToPNG());
            }
            finally { 相机.targetTexture=原目标; RenderTexture.active=原活动; RenderTexture.ReleaseTemporary(缓冲); UnityEngine.Object.Destroy(预览); }
            完成();
        }
        catch(Exception ex) { 结果.错误.Add(ex.ToString()); 完成(); }
    }
    static void 完成()
    {
        if(已结束) return; 已结束=true;
        EditorApplication.update-=等待; Application.logMessageReceived-=记错;
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true));
        EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>
        {
            天帝存档.验证目录=原存档目录; Application.runInBackground=原后台;
            EditorSettings.enterPlayModeOptionsEnabled=原选项启用; EditorSettings.enterPlayModeOptions=原选项;
        };
    }
}
#endif
