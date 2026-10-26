# 1.6 / 1.7 构建与读档修复

两版共享 `1.6/Source` 下的生产代码。1.6 使用 .NET Framework 4.8 和 Harmony 2；1.7 使用 .NET 10、UnityStubs 和 Harmony 3，分别编译自己的 DLL。

`Common` 存放两版共用的 Defs 和翻译，根目录保留贴图。`LoadFolders.xml` 为每个游戏版本选择 Common 和对应版本的程序集，避免同时加载两份 DLL。

Git 仓库保留源码、测试、构建脚本、Mod 内容和两个可安装的 Mod DLL。原版与 Milira 的依赖/反编译代码、构建缓存、旧安装备份和本地草稿由 `.gitignore` 排除，构建时需自行准备依赖。

## 本机构建

在仓库根目录运行：

```powershell
./build.ps1
dotnet run --project Tests/Regression/Regression.csproj -c Release
dotnet run --project Tests/Compatibility/Compatibility.csproj -c Release
```

单独编译：`./build.ps1 -Version 1.6` 或 `./build.ps1 -Version 1.7`。

依赖默认位置：

- 1.6 原版和 Harmony：`1.6/ExternalMods` 中保留的程序集副本。
- Milira：相邻的 `Rimworld_Assmb/Milira/3256974620/1.6/Assemblies/Milira.dll`。
- 1.7 原版和 UnityStubs：当前 RimWorld 安装根目录。
- 1.7 Harmony：Steam 工坊 `2009463077/1.7/Assemblies/0Harmony.dll`。

直接使用 dotnet build 时，可用 `-p:RimWorldManagedPath=...`、`-p:MiliraAssemblyPath=...` 和 `-p:HarmonyAssemblyPath=...` 覆盖路径。编译产物只复制本 Mod 的 DLL，不打包游戏或其他 Mod 的依赖。

编译并部署到本机：

```powershell
./build.ps1 -Deploy
```

部署前将整个旧 Mod 目录移到 `Build/backups/installed-时间戳`；部署失败会恢复。旧的版本内 Defs、翻译和多余 DLL 不会残留在新安装中。

## 修复行为

- 在世界组件加载阶段恢复外交权限，并清除 Milira 的好感上限缓存；不修改存档的基础好感或敌对状态。
- 任务首次解除永久敌对时才补到中立。后续辱骂、战斗造成的敌对状态在读档后继续保持。
- Milira 自身的读档回调不能覆盖 BFA 的外交权限；BFA 已解除限制时不再应用 Milira 的永久敌对或有限解冻上限。帝国玩家派系继续由原来的逻辑管理。
- 已加入的殖民者不再重复调用 SetGuestStatus，保留床位和手术账单。真正的招募、俘虏或奴隶转换仍执行正常加入流程。
- 旧存档继续使用原有字段名，并能从旧任务、故事状态或对话标记恢复缺失的解锁字段。

回归测试直接编译三个生产源文件，使用根据原版代码实现的少量引擎替身，覆盖 XML 保存/加载、连续读档、负好感、正好感、缓存、旧档迁移、玩家派系例外和床位。测试不等于在真实游戏中加载玩家存档；仍应在 1.6、1.7 中各测试一次袭击中读档和床位保持。
