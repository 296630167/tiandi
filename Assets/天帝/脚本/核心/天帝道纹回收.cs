using System;
using System.Collections.Generic;

public sealed partial class 天帝道纹
{
    public bool 设置回收锁定(道纹实例 纹,bool 锁定)
    {
        if(纹==null||纹.是源纹||纹.是天赋||!道纹.Contains(纹))return false;
        if(纹.回收锁定==锁定)return true;
        纹.回收锁定=锁定;状态改变?.Invoke();return true;
    }
    public bool 可回收(道纹实例 纹,out string 原因)
    {
        if(纹==null||纹.是天赋||纹.是源纹){原因="源纹不可回收";return false;}
        if(!道纹.Contains(纹)){原因="道纹已不在背包";return false;}
        if(纹.回收锁定){原因="已锁定 · 不可回收";return false;}
        if(纹.格子.HasValue){原因="已放置 · 请先卸下";return false;}
        foreach(var 方案 in 布局方案)if(方案.已保存&&方案.摆放.Exists(x=>x.编号==纹.编号)){原因="布局方案保护";return false;}
        if(天帝数值.道纹回收价(纹)<=0){原因="此道纹无法回收";return false;}
        原因="";return true;
    }
    // 支付方先校验整批余额与实例；无事件地删除后，余额同步提交，再发布库存变更。
    internal bool 移除回收批次(IReadOnlyList<道纹实例> 批次,out string 原因)
    {
        if(批次==null||批次.Count==0){原因="请先选择道纹";return false;}
        var 唯一=new HashSet<道纹实例>();
        foreach(var 纹 in 批次)if(!唯一.Add(纹)){原因="重复选择了同一道纹";return false;}else if(!可回收(纹,out 原因))return false;
        foreach(var 纹 in 批次)道纹.Remove(纹);
        原因="";return true;
    }
    internal void 发布回收改变()=>重算();
}
