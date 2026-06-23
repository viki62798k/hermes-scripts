# AI助理优化工具

这是一个 Windows 桌面工具，用于运维和用户侧执行 AI 助理相关配置优化任务。

## 功能

- 账号信息绑定
- env 配置优化
- 启动项优化
- config 配置优化
- 缓存清理
- 系统升级
- 工具升级
- 启动服务
- 停止服务
- 重启服务
- 执行日志查看

## 运行要求

- Windows
- .NET Framework 4.x
- 7-Zip，用于系统升级时解压安装包
- 管理员权限

## 编译

在仓库目录中运行：

```powershell
.\build.ps1
```

编译完成后，exe 会生成在：

```text
bin\AIOptimizeTool.exe
```

## 主要文件

- `Program.cs`：主程序代码和全部功能逻辑
- `app.manifest`：管理员权限声明
- `ai-assistant.ico`：exe 图标
- `IconMaker.cs`：图标生成辅助程序
- `build.ps1`：本地编译脚本

## 云端依赖

系统升级、工具升级会访问：

```text
https://mirrors.qilu-pharma.com/ps-scripts/
```

目前使用的云端文件：

- `hermes-agent.zip`
- `hermes-web-ui.zip`
- `AIOptimizeTool.version`
- `AIOptimizeTool.exe`

## config 配置优化

config 配置优化会先执行：

```cmd
hermes config migrate
```

然后修改本地文件：

```text
C:\Users\admin\AppData\Local\hermes\config.yaml
```

写入以下参数：

- `context_length: 198000`
- `threshold: 0.5`
- `protect_last_n: 15`

这三个字段必须已存在于 `config.yaml` 中；工具只替换原字段值，找不到字段时会停止并报错。

## 工具升级

工具升级依赖云端两个文件：

```text
AIOptimizeTool.version
AIOptimizeTool.exe
```

`AIOptimizeTool.version` 只需要包含一行版本号，例如：

```text
2.0.1
```

当云端版本号高于程序内置版本号时，工具会下载 `AIOptimizeTool.exe` 到临时目录，退出当前程序，然后原地替换并重新打开新版工具。
