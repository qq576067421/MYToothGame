# tooth 项目总览

## 这是什么

这是一个把程序、美术资源、配置表和出包产物放在同一仓库里的 Unity 游戏项目。根目录不是单一 Unity 工程，而是由两个 Unity 2021.3.58f1 工程和一组共享目录组成：

- `ARTProject`：美术资源制作和资源打包工程
- `GameProject`：程序主工程，包含运行时、热更新、配置表代码生成、客户端出包逻辑
- `build`：统一输出目录，主要承接 `StreamingAssets`、AssetBundle 和出包中间产物
- `public`：配置表中间产物和客户端导表工具
- `RawTables`：Excel 源表和 Excel -> CSV 导出工具

如果你是新同事，可以先把这个仓库理解成一条完整链路：

```text
Excel 源表(RawTables)
  -> CSV(public/Tables)
  -> 客户端配置导出(public/Tables/ClientExportTables)
  -> build/StreamingAssets/<platform>
  -> GameProject 运行时读取

GameDll 热更新代码(GameProject/GameDll)
  -> 编译产物写回 Assets/art/work/gamedll
  -> SplitDll.exe 拆分
  -> Assets/art/out/ui/animations + Assets/art/out/dll
  -> HybridCLR 运行时加载

美术资源(ARTProject/Assets/art/out)
  -> Build/AssetBundles/*
  -> build/StreamingAssets/<platform>
  -> 客户端包 / 热更资源
```

## 一分钟看懂目录

### 根目录

| 路径 | 作用 |
| --- | --- |
| `ARTProject/` | 美术资源工程，负责声音、场景、Shader 等资源制作和 AssetBundle 打包 |
| `GameProject/` | 程序工程，负责游戏运行、热更新、配置表读取、GM 工具、客户端出包 |
| `build/` | 共享输出目录，`MonoTool.GetBuildPath()` 指向这里，最终 `StreamingAssets` 也落这里 |
| `public/` | 配置表 CSV 和客户端导表工具，属于“程序和表格之间”的桥接层 |
| `RawTables/` | Excel 源表、`XLSX2CSV.exe`、Aspose 许可证等原始导表工具 |
| `.dotnet/` | 本地 .NET 工具缓存 |
| `.vs/` | Visual Studio 本地缓存 |

### `ARTProject`

`ARTProject` 是单独的 Unity 工程，当前主要可见结构如下：

- `Assets/art/out/`
  - 当前主要有 `audio_mixer`、`scene`、`shader`、`sound`
  - 可理解为美术侧最终准备参与 AB 打包的资源输出区
- `Assets/art/Work/`
  - 当前有 `Sound`、`urp`
  - 更偏向制作中的资源和模板
- `Assets/Scripts/Local/Classes/Editor/Build/BuildAssetBundle.cs`
  - 提供 `Build/AssetBundles/*` 菜单
  - 会把资源打到根目录 `build/StreamingAssets/<platform>`
- `Assets/PluginAssets/AmplifyShaderEditor`
  - Shader 编辑相关插件
- `Assets/ShaderVariantsCollector`
  - Shader 变体收集工具

一句话总结：`ARTProject` 更像“资源制作 + 资源打包工程”。

### `GameProject`

`GameProject` 是程序主工程，内容明显更多：

- `Assets/Scripts/Local/`
  - Unity 原生层、启动层、平台层、资源层、编辑器工具
  - 关键文件：
    - `Main.cs`：Unity 主入口，驱动热更和主循环
    - `HuatuoHotFixManager.cs`：`HotFixManager_SystemDll`，负责 HybridCLR/DLL 装载
    - `MonoTool.cs`：统一管理 `build/`、`StreamingAssets`、持久化目录、AB 后缀
- `Assets/Scripts/Game/Base/`
  - 运行时基础模块，如音频、数据、SQLite、渲染效果、引导等
- `Assets/Scripts/DataDll/Bean/`
  - 从配置表生成的 Unity 侧 Bean 类
- `Assets/Editor/GMTools/GMTools.cs`
  - Unity 编辑器内的 GM/调试窗口，入口是 `Tools/GMTools`
- `Assets/art/out/`
  - 程序侧也有一份 `art/out`
  - 当前主要是 `dll`、`font`、`guide`、`shader`、`texture`、`ui`、`ui_component`
  - 其中很多并不是“原始美术资源”，而是为了运行时加载准备的资源或热更产物
- `Assets/art/work/gamedll/`
  - `GameDll` 编译后的 `.bytes/.mdb/.pdb` 回写位置
- `GameDll/`
  - 真正的热更新 C# 项目，`TargetFrameworkVersion` 为 `.NET Framework 4.8`
  - 源码在 `GameDll/Src`
  - 启动、登录、大厅流程都在这里
- `hybridclr_unity/`
  - 本地 vendored 的 HybridCLR 包
  - `Packages/manifest.json` 通过 `file:../hybridclr_unity` 引用
- `Tools/Excel2Code/`
  - 配置表生成代码工具
- `Analyzers/`
  - 自定义 Roslyn 分析器，检查委托 `+=` / `-=` 是否成对
- `sqlite_build/`
  - 原生 sqlite3 构建工程，包含 Android Gradle 工程和 Windows VCXPROJ
- `pdb2mdb/`
  - `GameDll` 后处理工具，用于把 PDB 转成 MDB
- `UnityEngineLibaray/`
  - 给 `GameDll.csproj` 外部编译使用的 Unity DLL 引用目录

一句话总结：`GameProject` 是“Unity 壳层 + 热更新逻辑 + 配表 + 调试工具 + 出包流程”的中心。

### `public`

`public` 目前最关键的是 `public/Tables/`：

- `t_effectBean.csv`、`t_globalBean.csv`、`t_guideBean.csv` 等
  - 这是从 Excel 导出的客户端 CSV
- `导入表到客户端.bat`
  - 调用 `ClientExportTables/ExportTables/bin/ExportTables.exe`
  - 用于把 CSV 进一步导成客户端实际使用的数据
- `ClientExportTables/ExportTables/bin/path.ini`
  - 定义导出目标目录
  - 当前 `sqlitedir=../../../../../build/StreamingAssets`
  - 平台列表包含 `android|ios|mac|windows|webgl`

一句话总结：`public/Tables` 是“表格中间站”，不是 Excel 源，也不是最终客户端运行时目录。

### `RawTables`

`RawTables` 是配置表源头：

- Excel 源表：如 `qj 全局表.xlsx`、`sy 声音.xlsx`、`tx 特效表.xlsx`、`yy 语言包.xlsx`
- `XLSX2CSV.exe`
  - 用于把 Excel 导成 `../public/Tables`
- `导客户端表.bat`
  - 批量导表
- `export_one.bat`
  - 单表导出
- `Aspose.Cells.dll` 与 `Aid/License.lic`
  - Excel 导出依赖和许可证

一句话总结：策划/程序改表时，最先动的是这里，不是 `public/Tables`。

## 关键工作流

## 1. 配置表工作流

配置表链路可以拆成三步：

1. 在 `RawTables/` 修改 Excel 源表
2. 运行 `RawTables/导客户端表.bat` 或 `RawTables/export_one.bat`
3. 生成的 CSV 落到 `public/Tables/`

之后还有两条下游链路：

- 客户端数据导出：
  - 运行 `public/Tables/导入表到客户端.bat`
  - 实际调用 `ClientExportTables/ExportTables/bin/ExportTables.exe client_cs path.ini`
  - 输出目标由 `path.ini` 控制，当前指向 `build/StreamingAssets`
- 代码生成：
  - `GameProject/Tools/_配置表导代码(all).bat`
  - `GameProject/Tools/_配置表导代码(热更新).bat`
  - 会从 `../../public/Tables/` 读取 CSV
  - 同时生成到：
    - `GameProject/Assets/Scripts/DataDll/Bean/`
    - `GameProject/GameDll/Src/Data/Bean/`

这意味着表格改动通常至少影响三处内容：

- `RawTables/*.xlsx`
- `public/Tables/*.csv`
- `GameProject` 里的生成代码

## 2. 热更新代码工作流

热更新逻辑核心在 `GameProject/GameDll/`：

- 主入口是 `GameDll.HotFixLoop`
- 主流程类在 `GameDll/Src/GameLogic/`
  - `Start/CGamePro_StartApplication.cs`
  - `Login/CGamePro_Login.cs`
  - `Lobby/CGamePro_Lobby.cs`
- 数据 Bean 在 `GameDll/Src/Data/Bean/`

Unity 壳层通过以下链路驱动热更新：

- `GameProject/Assets/Scripts/Local/Classes/Main.cs`
  - Unity 侧总入口
  - `StartGame()` 中初始化 `HotFixManager_SystemDll`
- `GameProject/Assets/Scripts/Local/Classes/HuatuoHotFixManager.cs`
  - 类名是 `HotFixManager_SystemDll`
  - 负责：
    - 装载 HybridCLR AOT 元数据
    - 装载热更 DLL
    - 调用 `GameDll.HotFixLoop`

`GameDll.csproj` 还定义了编译后的回写逻辑：

- DLL / MDB / PDB 会写到 `GameProject/Assets/art/work/gamedll/`
- 然后调用 `SplitDll.exe`
- 将 DLL 分拆写入 `GameProject/Assets/art/out/ui/animations/`
- 打 AB 时再由 `BuildAssetBundle.CopyHybridCLRIl2CppStrip()` 处理 AOT 元数据并写入 `Assets/art/out/dll/`

这就是为什么程序代码会和 `Assets/art/...` 目录发生耦合：这里的 `art` 目录并不只是美术资源，也承载了部分运行时热更资源。

## 3. AssetBundle 工作流

`ARTProject` 和 `GameProject` 里都存在 `BuildAssetBundle.cs`，都会把资源打到根目录：

- `build/StreamingAssets/android`
- `build/StreamingAssets/windows`
- 以及其他目标平台目录

已确认的重要事实：

- `MonoTool.GetBuildPath()` 指向根目录 `build/`
- `MonoTool.GetBuildStreamingAssetsPath()` 指向根目录 `build/StreamingAssets/`
- AB 后缀不是常见的 `.ab`，而是统一使用 `.jpg`

所以在这个项目里，很多 `.jpg` 文件其实不是图片，而是 AssetBundle，例如：

- `dll/dll_holder.jpg`
- `windows.jpg`
- `art_variants.jpg`

不要按“图片资源”去理解这些文件。

## 4. 客户端出包工作流

`GameProject/Assets/Scripts/Local/Classes/Editor/Build/BuildPlayer.cs` 暴露了明确的 Unity 菜单入口：

- `Build/发布Android`

从代码上看，它会按这个顺序执行：

1. 调整 PlayerSettings
2. 调用 `BuildAssetBundle.BuildAllResourcesForPlayer()`
3. 执行 `CopyCSV()`
4. 触发 `public/Tables/导入表到客户端.bat`
5. 把 `build/StreamingAssets/<platform>` 同步到 `GameProject/Assets/StreamingAssets`
6. 再执行 Unity Player Build

当前仓库里最明确、最完整的“客户端出包入口”是 Android。AssetBundle 打包支持 Android/iOS/Windows/Mac，但完整客户端发布菜单目前只看到 Android。

## 程序运行入口

如果你要从程序角度看项目，优先记住下面这些入口：

- Unity 版本：`2021.3.58f1`
- 启动场景：`GameProject/Assets/startapp_dll.unity`
- Unity 主入口：`GameProject/Assets/Scripts/Local/Classes/Main.cs`
- 热更桥接：`GameProject/Assets/Scripts/Local/Classes/HuatuoHotFixManager.cs`
- 热更主循环：`GameProject/GameDll/Src/HotFixLoop.cs`
- 流程状态机：`GameProject/GameDll/Src/GameLogic/CGameProcedure.cs`
- 编辑器调试窗口：`Tools/GMTools`

另外有一个重要的编辑器行为：

- Windows Unity Editor 下，`Main.cs` 默认 `m_UseAB = false`
- 非 Windows Editor / 非 Editor 环境默认走 AB 模式
- 打开 `GMTools` 后可切到“真机调试(Sqlite、AB模式、原生C#)”来模拟更接近真机的运行状态

## 常见角色应该看哪里

### 程序

优先关注：

- `GameProject/Assets/Scripts/Local`
- `GameProject/Assets/Scripts/Game/Base`
- `GameProject/GameDll/Src`
- `GameProject/Tools`
- `GameProject/Analyzers`

### 美术

优先关注：

- `ARTProject/Assets/art/Work`
- `ARTProject/Assets/art/out`
- `ARTProject/Assets/Scripts/Local/Classes/Editor/Build`
- `GameProject/Assets/art/out`

### 策划 / 配表

优先关注：

- `RawTables`
- `public/Tables`
- `GameProject/Tools/_配置表导代码(all).bat`

### 打包 / 测试

优先关注：

- `build/StreamingAssets`
- `build/StreamingAssets/*/versionconfig.json`
- `GameProject/Assets/Scripts/Local/Classes/Editor/Build`
- `GameProject/Assets/Editor/GMTools`

## 版本和发布相关

`build/StreamingAssets/<platform>/versionconfig.json` 当前用于管理：

- `app_version`
- `res_version`
- CDN 列表
- 服务器配置地址
- 应用下载地址

可以把它理解成“客户端资源版本和热更入口配置”。

## 容易混淆但很重要的点

- 根目录不是一个 Unity 工程，而是两个 Unity 工程加一组共享目录。
- `build/` 是两个工程共享的输出目录，不是某一个 Unity 的普通子目录。
- `GameProject/Assets/art/...` 里不全是美术资源，也有热更 DLL、拆分产物和运行时资源。
- 项目把很多 AssetBundle 伪装成 `.jpg` 后缀，不要直接按图片处理。
- `public/Tables` 是中间层，`RawTables` 才是配置源头。
- `GameDll` 不是普通 Unity Assembly，而是独立 `.csproj` + HybridCLR 热更新程序集。
- `GameProject/Library`、`Temp`、`obj`、`UserSettings`、`.vs` 主要是本地缓存或构建中间文件，通常不是你要理解业务时应该先看的地方。

## 推荐的上手顺序

如果你是第一次接触这个仓库，建议按下面顺序：

1. 先读本文件，建立“两个 Unity 工程 + 一条共享资源链路”的心智模型
2. 程序先打开 `GameProject`，美术先打开 `ARTProject`
3. 程序先看 `Main.cs`、`HuatuoHotFixManager.cs`、`GameDll/Src/HotFixLoop.cs`
4. 配表先看 `RawTables -> public/Tables -> GameProject/Tools/_配置表导代码(all).bat`
5. 资源打包先看两个工程里的 `BuildAssetBundle.cs`
6. 客户端出包再看 `GameProject` 的 `Build/发布Android`

## 关键文件索引

- `ARTProject/ProjectSettings/ProjectVersion.txt`
- `GameProject/ProjectSettings/ProjectVersion.txt`
- `GameProject/Packages/manifest.json`
- `GameProject/ProjectSettings/HybridCLRSettings.asset`
- `GameProject/Assets/Scripts/Local/Classes/Main.cs`
- `GameProject/Assets/Scripts/Local/Classes/HuatuoHotFixManager.cs`
- `GameProject/Assets/Scripts/Local/Classes/MonoTool.cs`
- `GameProject/Assets/Scripts/Local/Classes/Editor/Build/BuildAssetBundle.cs`
- `GameProject/Assets/Scripts/Local/Classes/Editor/Build/BuildPlayer.cs`
- `GameProject/Assets/Editor/GMTools/GMTools.cs`
- `GameProject/GameDll/GameDll.csproj`
- `GameProject/GameDll/Src/`
- `public/Tables/导入表到客户端.bat`
- `public/Tables/ClientExportTables/ExportTables/bin/path.ini`
- `RawTables/导客户端表.bat`

---

这份文档是基于当前仓库实际结构整理的第一版项目导览，适合作为新开发者的入口说明。后续如果仓库继续扩展，建议优先维护本文件，而不是把说明散落到各个目录里。
