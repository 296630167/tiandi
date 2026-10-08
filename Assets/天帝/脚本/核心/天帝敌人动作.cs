using System.Collections.Generic;
using UnityEngine;
// 四张生成动作关键帧＋原画待机；不是八方向逐帧动画。
public sealed class 天帝敌人动作
{
    readonly Dictionary<string,Sprite[]> 缓存=new Dictionary<string,Sprite[]>();
    public void 预加载(string code)
    {
        if(!缓存.ContainsKey(code))
        {
            var frames=new Sprite[5];for(int i=0;i<5;i++)frames[i]=Resources.Load<Sprite>("敌人动作/"+code+"_"+i);缓存.Add(code,frames);
        }
    }
    public Sprite 读取(string code,战斗敌人 e,bool 移动,float 时钟)
    {
        预加载(code);var frames=缓存[code];
        int f=!e.存活?3:e.行动==敌人行动.后摇||e.行动==敌人行动.蓄力&&e.蓄力<=e.蓄力总秒*天帝战斗润色.取("attack_pose_fraction")?2:
            移动?(int)(时钟/天帝战斗润色.取("walk_cycle")*2)%2:4;
        return frames[f];
    }
}
