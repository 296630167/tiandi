using UnityEngine;

public sealed partial class 天帝战斗场景
{
    void 画导演场景()
    {
        var d = 战斗?.导演;
        if (d == null || 地图 == null) return;
        var 环境 = d.环境颜色; 环境.a = .025f + d.环境强度 * .035f;
        战斗几何.方块(Vector2.zero, .025f, new Vector2(地图.半宽 * 2, 地图.半高 * 2), 环境);
        var 事件 = d.当前事件;
        if (事件 != null)
        {
            Color 色 = 导演危险色(事件.类型);
            float 脉冲 = .92f + Mathf.Sin(Time.time * 8f + 事件.种子) * .08f;
            float 半径 = 事件.有效半径;
            if (事件.类型 == 战斗危险类型.范围收缩)
            {
                战斗几何.环(事件.中心, .19f, 半径, new Color(色.r, 色.g, 色.b, .76f));
                战斗几何.环(事件.中心, .18f, 半径 + .55f, new Color(色.r, 色.g, 色.b, .2f));
            }
            else
            {
                float 填充 = 事件.已生效 ? .16f : .055f;
                战斗几何.圆(事件.中心, .045f, 半径, new Color(色.r, 色.g, 色.b, 填充), 32);
                战斗几何.环(事件.中心, .19f, 半径, new Color(色.r, 色.g, 色.b, (事件.已生效 ? .8f : .45f) * 脉冲));
                if (!事件.已生效) 战斗几何.环(事件.中心, .20f, 半径 + .3f * (1 - 事件.预警进度), new Color(1, .9f, .62f, .5f));
            }
            // 事件中心保留四条短刻度，帮助观战者读懂边界和方向。
            for (int i = 0; i < 4; i++)
            {
                float a = (事件.种子 * .73f + i * Mathf.PI * .5f) + Time.time * (事件.已生效 ? .35f : .12f);
                Vector2 向 = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                战斗几何.线(事件.中心 + 向 * (半径 + .12f), 事件.中心 + 向 * (半径 + .38f), .2f, .045f, new Color(色.r, 色.g, 色.b, .7f));
            }
        }
        else if (d.事件结束反馈进度 > .001f)
        {
            // 事件结束后保留短暂的退场环，让观战者知道危险已经解除。
            float t = d.事件结束反馈进度;
            Color 色 = 导演危险色(d.最后事件);
            战斗几何.环(d.最后事件位置, .16f, d.最后事件范围 + (1f - t) * .55f,
                new Color(色.r, 色.g, 色.b, t * .46f));
        }
        float 链路进度 = d.链路演出进度;
        if (链路进度 > 0.001f)
        {
            int 当前 = d.当前链路槽位 < 0 ? 0 : d.当前链路槽位;
            Vector2 中心 = 玩家位置;
            int 已点亮 = Mathf.Clamp(Mathf.CeilToInt(链路进度 * 6), 1, 6);
            Vector2 上一点 = 中心;
            for (int i = 0; i < 6; i++)
            {
                float a = (i - 2.5f) * Mathf.PI / 3f;
                Vector2 点 = 中心 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.25f;
                bool 亮 = i < 已点亮;
                Color 色 = i == 当前 ? new Color(1, .82f, .32f, .95f) : 亮 ? new Color(.42f, .9f, .78f, .86f) : new Color(.35f, .58f, .53f, .28f);
                战斗几何.环(点, .18f, .16f + (亮 ? .035f : 0), 色);
                if (i < 已点亮) { 战斗几何.线(上一点, 点, .22f, .065f, 色); 上一点 = 点; }
            }
            战斗几何.环(中心, .18f, .55f + 链路进度 * .25f, new Color(1, .85f, .4f, .36f));
        }
        if (d.完整共鸣剩余秒 > 0)
        {
            float a = Mathf.Clamp01(d.完整共鸣剩余秒 / .3f);
            战斗几何.环(玩家位置, .25f, 1.45f + (1 - a) * .65f, new Color(.86f, 1f, .79f, a * .9f));
            for (int i = 0; i < 6; i++)
            {
                float r = i * Mathf.PI / 3f;
                Vector2 终 = 玩家位置 + new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * 2.15f;
                战斗几何.线(玩家位置, 终, .24f, .045f, new Color(.45f, .95f, .82f, a * .62f));
            }
        }
        if (d.事件结束反馈剩余秒 > 0)
        {
            float a = Mathf.Clamp01(d.事件结束反馈剩余秒 / .46f);
            战斗几何.环(玩家位置, .24f, 1.1f + (1 - a) * .8f, new Color(.74f, 1f, .86f, a * .62f));
        }
        if (d.BOSS阶段提示剩余秒 > 0)
        {
            float a = Mathf.Clamp01(d.BOSS阶段提示剩余秒 / 1.1f);
            战斗几何.环(玩家位置, .20f, 1.7f + (1 - a) * .65f, new Color(1f, .72f, .26f, a * .56f));
        }
    }

    static Color 导演危险色(战斗危险类型 类型)
    {
        switch (类型)
        {
            case 战斗危险类型.火焰裂隙: return new Color(1f, .38f, .14f);
            case 战斗危险类型.冰霜潮汐: return new Color(.26f, .78f, 1f);
            case 战斗危险类型.雷击标记: return new Color(.72f, .58f, 1f);
            case 战斗危险类型.毒雾扩散: return new Color(.42f, .94f, .42f);
            case 战斗危险类型.能量风暴: return new Color(1f, .42f, .86f);
            default: return new Color(1f, .72f, .28f);
        }
    }
}
