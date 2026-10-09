using System.Collections.Generic;
using UnityEngine;
// 验收后的方向帧动画按真实位移与攻击相位播放；缺失动作保留原关键帧。
public sealed class 天帝敌人动作
{
    readonly Dictionary<string,Sprite[]> 缓存=new Dictionary<string,Sprite[]>();
    readonly Dictionary<string,天帝战斗帧动画> 方向配置=new Dictionary<string,天帝战斗帧动画>();
    readonly Dictionary<战斗敌人,播放状态> 方向播放=new Dictionary<战斗敌人,播放状态>();
    sealed class 播放状态
    {
        public string 编号;
        public 天帝战斗帧播放 播放;
        public Vector2 位置, 朝向=Vector2.down;
        public float 时钟, 后摇总秒;
        public 敌人行动 行动;
        public bool 攻击已释放;
        public int 蓄力开始攻击次数;
    }
    public void 预加载(string code)
    {
        if(string.IsNullOrEmpty(code))return;
        if(!缓存.ContainsKey(code))
        {
            var frames=new Sprite[5];for(int i=0;i<5;i++)frames[i]=Resources.Load<Sprite>("敌人动作/"+code+"_"+i);缓存.Add(code,frames);
            var 配置=Resources.Load<天帝战斗帧动画>("角色帧动画/"+code);
            // 狼王凑活候选只提供南向待机；缺少的移动、奔跑和攻击动作继续回退旧静态帧。
            if(配置==null&&code.StartsWith("BTB",System.StringComparison.OrdinalIgnoreCase))
                配置=Resources.Load<天帝战斗帧动画>("角色帧动画/BTB_IDLE_R1");
            方向配置.Add(code,配置!=null&&配置.可用?配置:null);
        }
    }
    public 天帝战斗帧动画 获取方向配置(string 编号)
    {预加载(编号);return !string.IsNullOrEmpty(编号)&&方向配置.TryGetValue(编号,out var 配置)?配置:null;}
    public bool 当前使用方向帧(战斗敌人 敌)
        =>敌!=null&&方向播放.TryGetValue(敌,out var 状态)&&状态.播放.使用新帧;
    public Sprite 读取(string code,战斗敌人 e,bool 移动,float 时钟)
    {
        if(string.IsNullOrEmpty(code)||e==null)return null;
        var 配置=获取方向配置(code);
        if(配置!=null&&尝试方向帧(code,配置,e,时钟,out var 图))return 图;
        if(方向播放.TryGetValue(e,out var 状态))状态.播放.回退();
        预加载(code);var frames=缓存[code];
        if(float.IsNaN(时钟)||float.IsInfinity(时钟)||时钟<0)return frames[e.存活?4:3];
        int f=!e.存活?3:e.行动==敌人行动.后摇||e.行动==敌人行动.蓄力&&e.蓄力<=e.蓄力总秒*天帝战斗润色.取("attack_pose_fraction")?2:
            移动?(int)(时钟/天帝战斗润色.取("walk_cycle")*2)%2:4;
        return frames[f];
    }
    bool 尝试方向帧(string 编号,天帝战斗帧动画 配置,战斗敌人 敌,float 时钟,out Sprite 图)
    {
        图=null;
        if(float.IsNaN(时钟)||float.IsInfinity(时钟)||时钟<0)return false;
        if(!方向播放.TryGetValue(敌,out var 状态)||状态.编号!=编号)
        {
            状态=new 播放状态{编号=编号,播放=new 天帝战斗帧播放(配置),位置=敌.位置,时钟=时钟,行动=敌人行动.待机};
            方向播放[敌]=状态;
        }
        float 秒=Mathf.Max(0,时钟-状态.时钟);
        var 位移=敌.位置-状态.位置;状态.位置=敌.位置;状态.时钟=时钟;
        if(!敌.存活){状态.播放.回退();return false;}
        if(秒==0&&位移.sqrMagnitude<=.00000001f&&状态.行动==敌.行动&&状态.播放.使用新帧)
        {图=状态.播放.当前精灵;return 图!=null;}
        bool 移动=秒>0&&位移.sqrMagnitude>.00000001f;
        if(移动)状态.朝向=位移;
        var 上次行动=状态.行动;
        if (敌.行动 == 敌人行动.蓄力 && 上次行动 != 敌人行动.蓄力) 状态.蓄力开始攻击次数 = 敌.已攻击次数;
        状态.行动=敌.行动;
        if(敌.行动==敌人行动.蓄力)
        {
            状态.攻击已释放=false;
            if(敌.锁定方向.sqrMagnitude>.00000001f)状态.朝向=敌.锁定方向;
            else if((敌.攻击落点-敌.位置).sqrMagnitude>.00000001f)状态.朝向=敌.攻击落点-敌.位置;
            return 状态.播放.尝试攻击(状态.朝向,1-敌.蓄力/Mathf.Max(.001f,敌.蓄力总秒),false,out 图);
        }
        if(敌.行动==敌人行动.后摇)
        {
            if(上次行动==敌人行动.蓄力 && 敌.已攻击次数 > 状态.蓄力开始攻击次数)
            {状态.后摇总秒=Mathf.Max(.001f,敌.后摇秒);状态.攻击已释放=true;}
            if(状态.攻击已释放)
                return 状态.播放.尝试攻击(状态.朝向,1-敌后摇比例(敌,状态),true,out 图);
        }
        else 状态.攻击已释放=false;
        var 动作=!移动?战斗帧动作.待机:位移.magnitude/Mathf.Max(.0001f,秒)>敌.战术移速*1.2f?战斗帧动作.奔跑:战斗帧动作.移动;
        float 速度倍率=移动?Mathf.Clamp(位移.magnitude/Mathf.Max(.0001f,秒*配置.参考移速),.35f,1.8f):1;
        return 状态.播放.尝试推进(动作,状态.朝向,秒,out 图,速度倍率);
    }
    static float 敌后摇比例(战斗敌人 敌,播放状态 状态)=>Mathf.Clamp01(敌.后摇秒/状态.后摇总秒);
}
