# AI助理优化工具

这是一个 Windows 桌面工具，用于运维和用户侧执行 AI 助理相关配置优化任务。

## 功能

- 账号信息绑定
- env 配置优化
- 启动项优化
- config 配置优化
- 缓存清理
- AI 助理升级
- 工具升级
- 启动服务
- 停止服务
- 重启服务
- 执行日志查看

## 运行要求

- Windows
- .NET Framework 4.x
- 7-Zip，用于 AI 助理升级时解压安装包
- 管理员权限

## 编译

在仓库目录中运行：

```powershell
.\build.ps1
```

编译完成后会生成两个 exe：

```text
bin\AIOptimizeTool_v2.0.11.exe
bin\AIOptimizeTool.exe
```

带版本号的文件用于人工分发和归档，固定文件名 `AIOptimizeTool.exe` 用于云端工具升级。

## 主要文件

- `Program.cs`：主程序代码和全部功能逻辑
- `app.manifest`：管理员权限声明
- `ai-assistant.ico`：exe 图标
- `IconMaker.cs`：图标生成辅助程序
- `build.ps1`：本地编译脚本

## 云端依赖

AI 助理升级、工具升级会访问：

```text
https://mirrors.qilu-pharma.com/ps-scripts/
```

当前使用的云端文件：

- `hermes-agent.zip`
- `hermes-web-ui.zip`
- `AIOptimizeTool.version`
- `config.yaml`
- `AIOptimizeTool.exe`

## config 配置优化

config 配置优化会从云端下载 `config.yaml` 并覆盖本地文件：

```text
C:\Users\admin\AppData\Local\hermes\config.yaml
```

下载地址：

```text
https://mirrors.qilu-pharma.com/ps-scripts/config.yaml
```

工具会先下载到临时文件，确认下载成功且文件非空后，再覆盖本地 `config.yaml`。

## 工具升级

工具升级依赖云端两个文件：

```text
AIOptimizeTool.version
AIOptimizeTool.exe
```

`AIOptimizeTool.version` 只需要包含一行版本号，例如：

```text
2.0.11
```

当云端版本号高于程序内置版本号时，工具会下载 `AIOptimizeTool.exe` 到临时目录，退出当前程序，然后原地替换并重新打开新版工具。

升级时会先弹出提示，用户确认后当前工具会关闭，更新脚本会完成原地替换并自动重新打开新版工具。
