from pathlib import Path
p = Path(__file__).resolve().parents[1] / 'Assets/天帝/Editor'
f = p / '天帝通货验证.cs'
s = f.read_text(encoding='utf-8')
s = s.replace('游戏.打开道纹(); break;', '天帝道纹夹具.填充(游戏.道纹数据, 4, 游戏.道纹数据.技能点); break;', 1)
s = s.replace('点击(游戏.界面.道纹页.transform, "通货改造");', '点击(游戏.transform, "道纹改造");')
s = s.replace('检查("画布入口打开通货弹窗", 游戏.界面.道纹页.通货已打开);', '检查("主页入口打开独立改造页", 游戏.阶段 == 游戏阶段.道纹改造 && 游戏.界面.道纹页 == null);')
s = s.replace('游戏.界面.道纹页.通货页', '游戏.界面.改造页')
s = s.replace('模态弹窗屏蔽底层画布命中', '独立改造页面正确命中')
s = s.replace('检查("候选品阶标签即时刷新", 游戏.界面.道纹页.GetComponentsInChildren<Text>().Any(t => t.text == 天帝道纹品阶.彩色品阶文字(道纹品阶.完美)));', '检查("改造品阶详情即时刷新", 页.GetComponentsInChildren<Text>().Any(t => t.text.Contains(天帝道纹品阶.彩色品阶文字(道纹品阶.完美))));')
s = s.replace('检查("关闭回画布", !游戏.界面.道纹页.通货已打开);', '检查("关闭回主页", 游戏.阶段 == 游戏阶段.主页 && 游戏.界面.改造页 == null);')
s = s.replace('游戏.返回主页(); 游戏.打开道纹(); 游戏.界面.道纹页.打开通货();', '游戏.打开道纹改造();')
s = s.replace('重入画布不补库存且保留改造', '重入改造不补库存且保留改造')
f.write_text(s, encoding='utf-8')
f = p / '天帝天赋验证.cs'
s = f.read_text(encoding='utf-8').replace('实际确认穿越者1级获得三点', '实际确认穿越者1级基础一点加额外三点').replace('游戏.道纹数据.技能点 == 3', '游戏.道纹数据.技能点 == 4')
f.write_text(s, encoding='utf-8')
f = p / '天帝构建工具.cs'
s = f.read_text(encoding='utf-8').replace('public static string 运行通货验证()', 'public static string 运行开局验证() => 天帝开局验证.启动();\n    public static string 运行通货验证()')
f.write_text(s, encoding='utf-8')
