# 从参加聚光灯比赛到我为天帝镇压世间一切

《天帝》Unity工程：横屏2D战斗、六边格道纹构筑，以及PC与移动端两套交互界面。

## 打开工程

使用 **Unity 6000.3.10f1**，项目使用URP 17.3.0。图片、配音、字体和建模源等二进制资源使用Git LFS。

```sh
git lfs install
git clone https://github.com/296630167/tiandi.git
cd tiandi
git lfs pull
```

在Unity Hub添加仓库根目录，等待资源导入和Package Manager恢复依赖。打开 `Assets/天帝/场景/天帝.unity` 后运行；主页会加载 `Assets/天帝/场景/青岚原.unity` 进入战斗。

## 目录

- `Assets/`：游戏脚本、编辑器工具、场景、美术、字体、配音和Unity资源元数据。
- `Packages/`、`ProjectSettings/`：依赖锁定与Unity工程设置。
- `工具/`、`数据/`：制作与数值工具、正式数值对照数据。
- `建模源/`、`美术需求/`、`开发日志第二期/`：源素材、需求与开发记录。
- `游戏数值配置.md`：正式数值唯一基准。
- `项目变更记录.md`、`开发说明.md`、`AGENTS.md`：项目记录、操作说明与开发约定。

`Library`、`Temp`、日志、生成/验收输出和PC/手机构建包不纳入版本管理，由本地工具或Unity生成。

## 正式数值更新

先修改 `游戏数值配置.md` 中的权威JSON与对应说明，再在仓库根目录使用Python 3运行：

```sh
python 工具/战斗数值计算.py
python 工具/导出游戏数值.py
```

提交配置、生成的运行参数和数值对照文件后，在Unity刷新并验证受影响流程。进入Play及构建时会检查配置指纹。
