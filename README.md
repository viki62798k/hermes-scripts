# AI助理优化工具

这是一个 Windows 桌面工具，用于运维和用户侧执行 AI 助理相关配置优化任务。

## 功能

- 账号信息绑定
- env 配置优化
- 启动项优化
- config 配置优化
- 缓存清理
- 系统升级
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

系统升级和 config 配置优化会访问：

```text
https://mirrors.qilu-pharma.com/ps-scripts/
```

目前使用的云端文件：

- `config.yaml`
- `hermes-agent.zip`
- `hermes-web-ui.zip`
