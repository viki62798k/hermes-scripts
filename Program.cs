using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HermesEnvGui
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    enum TaskMode
    {
        InitEnv,
        UpdateStartup,
        DownloadYaml,
        BindAccount,
        ClearCache,
        RestartService,
        SystemUpgrade,
        RunAll
    }

    sealed class MainForm : Form
    {
        const string EnvPath = @"C:\Users\admin\AppData\Local\hermes\.env";
        const string StartupPath = @"C:\Users\admin\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\AiStartup.cmd";
        const string ConfigYamlPath = @"C:\Users\admin\AppData\Local\hermes\config.yaml";
        const string ConfigYamlUrl = "https://mirrors.qilu-pharma.com/ps-scripts/config.yaml";
        const string UxEnhancePath = @"C:\Users\admin\AppData\Local\hermes\ux-enhance";
        const string ProgramFilesPath = @"C:\Program Files";
        const string HermesAgentPath = @"C:\Program Files\hermes-agent";
        const string HermesWebUiPath = @"C:\Program Files\hermes-web-ui";
        const string HermesAgentZipUrl = "https://mirrors.qilu-pharma.com/ps-scripts/hermes-agent.zip";
        const string HermesWebUiZipUrl = "https://mirrors.qilu-pharma.com/ps-scripts/hermes-web-ui.zip";

        TextBox domainAccountBox;
        TextBox employeeIdBox;
        readonly TextBox logBox;
        readonly Label statusLabel;
        readonly StatusLamp statusLamp;
        readonly List<Button> taskButtons = new List<Button>();

        public MainForm()
        {
            Text = "AI助理优化工具";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 640);
            Size = new Size(820, 680);
            Font = new Font("Microsoft YaHei UI", 10F);
            BackColor = Color.FromArgb(246, 248, 251);

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(18);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 336F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            Controls.Add(root);

            var header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(218, 238, 252);
            header.Padding = new Padding(18, 10, 18, 10);
            header.Margin = new Padding(0, 0, 0, 12);
            root.Controls.Add(header, 0, 0);

            var headerIcon = new WrenchBadge();
            headerIcon.Location = new Point(20, 14);
            headerIcon.Size = new Size(44, 44);
            header.Controls.Add(headerIcon);

            var title = new Label();
            title.AutoSize = true;
            title.Text = "AI助理优化工具";
            title.Font = new Font(Font.FontFamily, 20F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(28, 72, 112);
            title.Location = new Point(78, 19);
            header.Controls.Add(title);

            var featurePanel = CreatePanel();
            featurePanel.Dock = DockStyle.Fill;
            featurePanel.Padding = new Padding(12);
            featurePanel.Margin = new Padding(0, 0, 0, 12);
            root.Controls.Add(featurePanel, 0, 1);

            var featureGrid = new TableLayoutPanel();
            featureGrid.Dock = DockStyle.Fill;
            featureGrid.ColumnCount = 2;
            featureGrid.RowCount = 4;
            featureGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            featureGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            featureGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
            featureGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            featureGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            featureGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            featurePanel.Controls.Add(featureGrid);

            var accountCard = CreateAccountTaskCard();
            featureGrid.SetColumnSpan(accountCard, 2);
            featureGrid.Controls.Add(accountCard, 0, 0);
            featureGrid.Controls.Add(TaskButton("env配置优化", TaskMode.InitEnv, Color.FromArgb(45, 104, 173)), 0, 1);
            featureGrid.Controls.Add(TaskButton("启动项优化", TaskMode.UpdateStartup, Color.FromArgb(45, 104, 173)), 1, 1);
            featureGrid.Controls.Add(TaskButton("config配置优化", TaskMode.DownloadYaml, Color.FromArgb(45, 104, 173)), 0, 2);
            featureGrid.Controls.Add(TaskButton("缓存清理", TaskMode.ClearCache, Color.FromArgb(105, 92, 160)), 1, 2);
            featureGrid.Controls.Add(TaskButton("系统升级", TaskMode.SystemUpgrade, Color.FromArgb(35, 128, 150)), 1, 3);
            var runAllButton = TaskButton("全部执行", TaskMode.RunAll, Color.FromArgb(188, 91, 35));
            featureGrid.Controls.Add(runAllButton, 0, 3);

            var logPanel = CreatePanel();
            logPanel.Dock = DockStyle.Fill;
            logPanel.Padding = new Padding(10);
            logPanel.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(logPanel, 0, 2);

            var logLayout = new TableLayoutPanel();
            logLayout.Dock = DockStyle.Fill;
            logLayout.ColumnCount = 1;
            logLayout.RowCount = 2;
            logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            logPanel.Controls.Add(logLayout);

            var logTitle = new Label();
            logTitle.Text = "执行日志";
            logTitle.AutoSize = true;
            logTitle.Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold);
            logTitle.ForeColor = Color.FromArgb(36, 45, 59);
            logLayout.Controls.Add(logTitle, 0, 0);

            logBox = new TextBox();
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.BorderStyle = BorderStyle.FixedSingle;
            logBox.BackColor = Color.FromArgb(252, 253, 255);
            logBox.Font = new Font("Consolas", 9F);
            logLayout.Controls.Add(logBox, 0, 1);

            var footer = new TableLayoutPanel();
            footer.Dock = DockStyle.Fill;
            footer.ColumnCount = 3;
            footer.RowCount = 1;
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38F));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            root.Controls.Add(footer, 0, 3);

            statusLamp = new StatusLamp();
            statusLamp.Dock = DockStyle.Fill;
            statusLamp.LampColor = Color.FromArgb(138, 147, 160);
            footer.Controls.Add(statusLamp, 0, 0);

            statusLabel = new Label();
            statusLabel.AutoSize = true;
            statusLabel.Text = IsAdministrator() ? "状态：等待执行" : "状态：当前未以管理员身份运行，请重新以管理员权限启动";
            statusLabel.ForeColor = IsAdministrator() ? Color.FromArgb(54, 65, 82) : Color.Firebrick;
            statusLabel.Anchor = AnchorStyles.Left;
            footer.Controls.Add(statusLabel, 1, 0);

            var restartButton = TaskButton("重启服务", TaskMode.RestartService, Color.FromArgb(35, 128, 150));
            restartButton.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
            restartButton.MinimumSize = new Size(90, 30);
            restartButton.Margin = new Padding(8, 4, 0, 4);
            footer.Controls.Add(restartButton, 2, 0);

            if (!IsAdministrator())
            {
                SetButtonsEnabled(false);
                SetStatus("状态：需要管理员权限运行", Color.Firebrick, Color.FromArgb(208, 55, 55));
            }
        }

        Panel CreateAccountTaskCard()
        {
            var card = CreatePanel();
            card.Margin = new Padding(6);
            card.Padding = new Padding(12);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 3;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            card.Controls.Add(layout);

            layout.Controls.Add(FormLabel("域账号"), 0, 0);

            domainAccountBox = CreateInputBox("域账号，例如: yaqi.liu");
            domainAccountBox.Margin = new Padding(0, 8, 12, 8);
            layout.Controls.Add(domainAccountBox, 1, 0);

            layout.Controls.Add(FormLabel("工号"), 0, 1);

            employeeIdBox = CreateInputBox("工号，例如: 033633");
            employeeIdBox.Margin = new Padding(0, 8, 12, 8);
            layout.Controls.Add(employeeIdBox, 1, 1);

            var button = TaskButton("账号信息绑定", TaskMode.BindAccount, Color.FromArgb(30, 120, 82));
            button.Margin = new Padding(8, 14, 0, 14);
            layout.SetRowSpan(button, 2);
            layout.Controls.Add(button, 2, 0);
            return card;
        }

        Button TaskButton(string text, TaskMode mode, Color color)
        {
            var button = new Button();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font(Font.FontFamily, 12F, FontStyle.Bold);
            button.Margin = new Padding(7);
            button.MinimumSize = new Size(120, 42);
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseVisualStyleBackColor = false;
            button.Tag = mode;
            button.Click += async (sender, args) => await RunSelectedTaskAsync((TaskMode)((Button)sender).Tag);
            taskButtons.Add(button);
            return button;
        }

        async Task RunSelectedTaskAsync(TaskMode mode)
        {
            var domainAccount = domainAccountBox.RealText();
            var employeeId = employeeIdBox.RealText();

            if ((mode == TaskMode.BindAccount || mode == TaskMode.RunAll) &&
                (domainAccount.Length == 0 || employeeId.Length == 0))
            {
                SetStatus("状态：请输入域账号和工号", Color.DarkOrange, Color.FromArgb(236, 154, 45));
                return;
            }

            SetButtonsEnabled(false);
            logBox.Clear();
            SetStatus("状态：正在执行...", Color.FromArgb(54, 65, 82), Color.FromArgb(52, 132, 215));

            var result = await Task.Run(() => Execute(mode, domainAccount, employeeId));

            foreach (var line in result.LogLines)
            {
                AppendLog(line);
            }

            if (result.Succeeded && !result.HasWarnings)
            {
                SetStatus(GetSuccessStatusText(mode), Color.ForestGreen, Color.FromArgb(35, 168, 89));
            }
            else if (result.Succeeded)
            {
                SetStatus(GetSuccessStatusText(mode), Color.ForestGreen, Color.FromArgb(35, 168, 89));
            }
            else
            {
                SetStatus("状态：执行失败", Color.Firebrick, Color.FromArgb(208, 55, 55));
            }

            SetButtonsEnabled(IsAdministrator());
        }

        static string GetSuccessStatusText(TaskMode mode)
        {
            if (mode == TaskMode.RestartService)
            {
                return "状态：重启完成";
            }

            if (mode == TaskMode.SystemUpgrade)
            {
                return "状态：执行成功";
            }

            return "状态：执行成功，请重启服务";
        }

        static ExecutionResult Execute(TaskMode mode, string domainAccount, string employeeId)
        {
            var result = new ExecutionResult();
            try
            {
                if (mode == TaskMode.InitEnv || mode == TaskMode.RunAll)
                {
                    InitEnv(result);
                    if (!result.Succeeded) return result;
                }

                if (mode == TaskMode.UpdateStartup || mode == TaskMode.RunAll)
                {
                    UpdateStartup(result);
                    if (!result.Succeeded) return result;
                }

                if (mode == TaskMode.DownloadYaml || mode == TaskMode.RunAll)
                {
                    DownloadYaml(result);
                    if (!result.Succeeded) return result;
                }

                if (mode == TaskMode.BindAccount || mode == TaskMode.RunAll)
                {
                    BindAccount(result, domainAccount, employeeId);
                    if (!result.Succeeded) return result;
                }

                if (mode == TaskMode.ClearCache || mode == TaskMode.RunAll)
                {
                    ClearCache(result);
                    if (!result.Succeeded) return result;
                }

                if (mode == TaskMode.RestartService)
                {
                    RestartService(result);
                    if (!result.Succeeded) return result;
                }

                if (mode == TaskMode.SystemUpgrade)
                {
                    SystemUpgrade(result);
                    if (!result.Succeeded) return result;
                }
            }
            catch (Exception ex)
            {
                result.Error(ex.Message);
            }

            return result;
        }

        static void InitEnv(ExecutionResult result)
        {
            result.Info("开始更新覆盖初始化 env 文件...");
            Directory.CreateDirectory(Path.GetDirectoryName(EnvPath));

            File.WriteAllText(EnvPath, BuildDefaultEnvContent(), new UTF8Encoding(true));

            if (File.Exists(EnvPath) && new FileInfo(EnvPath).Length > 0)
            {
                result.Success("初始化 env 文件已更新覆盖。");
            }
            else
            {
                result.Error("初始化 env 文件写入失败。");
            }
        }

        static void DownloadYaml(ExecutionResult result)
        {
            result.Info("开始下载 YAML 配置文件...");
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigYamlPath));

            var downloaded = DownloadFileWithFallback(ConfigYamlUrl, ConfigYamlPath, "YAML", result);
            if (downloaded && File.Exists(ConfigYamlPath) && new FileInfo(ConfigYamlPath).Length > 0)
            {
                result.Success("YAML 配置文件已下载。");
            }
            else
            {
                result.Error("YAML 配置文件下载失败。");
            }
        }

        static void UpdateStartup(ExecutionResult result)
        {
            result.Info("开始更新启动配置...");
            Directory.CreateDirectory(Path.GetDirectoryName(StartupPath));

            File.WriteAllText(StartupPath, BuildStartupContent(), Encoding.ASCII);

            if (File.Exists(StartupPath) && new FileInfo(StartupPath).Length > 0)
            {
                result.Success("启动配置已更新。");
            }
            else
            {
                result.Error("启动配置写入失败。");
            }
        }

        static void BindAccount(ExecutionResult result, string domainAccount, string employeeId)
        {
            result.Info("开始写入域账号和工号...");

            if (!File.Exists(EnvPath))
            {
                result.Warn("未找到 env 文件，先写入内置初始化 env。");
                Directory.CreateDirectory(Path.GetDirectoryName(EnvPath));
                File.WriteAllText(EnvPath, BuildDefaultEnvContent(), new UTF8Encoding(true));
            }

            var currentContent = File.ReadAllText(EnvPath, Encoding.UTF8);
            currentContent = ReplaceOrAppend(currentContent, "MSGW_AGENT_NAME", domainAccount);
            currentContent = ReplaceOrAppend(currentContent, "MSGW_HOME_CHANNEL", "qiwork:" + employeeId);
            File.WriteAllText(EnvPath, currentContent, new UTF8Encoding(true));

            var savedContent = File.ReadAllText(EnvPath, Encoding.UTF8);
            if (savedContent.Contains("MSGW_AGENT_NAME=" + domainAccount) &&
                savedContent.Contains("MSGW_HOME_CHANNEL=qiwork:" + employeeId))
            {
                result.Success("域账号和工号已保存到 env 文件。");
                result.Info("绑定信息：域账号 " + domainAccount + "，工号 " + employeeId);
            }
            else
            {
                result.Error("域账号或工号保存失败。");
            }
        }

        static void ClearCache(ExecutionResult result)
        {
            result.Info("开始清理缓存文件...");

            if (Directory.Exists(UxEnhancePath))
            {
                try
                {
                    Directory.Delete(UxEnhancePath, true);
                }
                catch (Exception ex)
                {
                    result.Error("缓存文件清理失败。详情：" + ex.Message);
                    return;
                }
            }

            if (!Directory.Exists(UxEnhancePath))
            {
                result.Success("缓存文件已清理。");
            }
            else
            {
                result.Error("缓存文件清理失败。");
            }
        }

        static void RestartService(ExecutionResult result)
        {
            result.Info("开始重启服务...");

            if (!StopHermes(result))
            {
                return;
            }

            KillPythonProcesses(result);
            if (!result.Succeeded) return;

            if (!StartCommandDetached("hermes-web-ui start", result))
            {
                return;
            }

            if (!WaitForPythonProcess(60000))
            {
                result.Error("服务启动后未检测到 python.exe 进程。");
                return;
            }

            Thread.Sleep(8000);
            result.Success("服务已重启。");
        }

        static void SystemUpgrade(ExecutionResult result)
        {
            result.Info("开始系统升级...");

            if (!StopHermes(result))
            {
                return;
            }

            KillPythonProcesses(result);
            if (!result.Succeeded) return;

            if (!BackupProgramFolder(HermesAgentPath, result))
            {
                return;
            }

            if (!BackupProgramFolder(HermesWebUiPath, result))
            {
                return;
            }

            try
            {
                DownloadAndExtractPackage(HermesAgentZipUrl, "hermes-agent", HermesAgentPath, result);
                DownloadAndExtractPackage(HermesWebUiZipUrl, "hermes-web-ui", HermesWebUiPath, result);
            }
            catch (Exception ex)
            {
                result.Error("系统文件下载失败。详情：" + ex.Message);
                return;
            }

            if (!StartCommandDetached("hermes-web-ui start", result))
            {
                return;
            }

            if (!WaitForPythonProcess(60000))
            {
                result.Error("升级后未检测到 python.exe 进程。");
                return;
            }

            Thread.Sleep(8000);
            result.Success("系统升级完成。");
        }

        static bool StopHermes(ExecutionResult result)
        {
            return RunCommandAndWait("hermes-web-ui stop", 60000, result);
        }

        static void KillPythonProcesses(ExecutionResult result)
        {
            RunCommandAndWaitAllowFailure(@"taskkill /F /T /IM python.exe", 30000, result);
            RunCommandAndWaitAllowFailure(@"taskkill /F /T /IM pythonw.exe", 30000, result);

            var deadline = DateTime.Now.AddSeconds(20);
            while (DateTime.Now < deadline)
            {
                if (CountProcessesByName("python") == 0 && CountProcessesByName("pythonw") == 0)
                {
                    return;
                }

                Thread.Sleep(1000);
            }

            result.Error("结束 python.exe 进程失败，请确认没有残留的 AI 助理进程。");
        }

        static bool BackupProgramFolder(string path, ExecutionResult result)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return true;
                }

                var backupPath = path + ".bak";
                if (Directory.Exists(backupPath))
                {
                    var archivedBackup = backupPath + "." + DateTime.Now.ToString("yyyyMMddHHmmss");
                    Directory.Move(backupPath, archivedBackup);
                }

                MoveDirectoryWithRetry(path, backupPath);
                return true;
            }
            catch (Exception ex)
            {
                result.Error("备份目录失败：" + path + "；详情：" + ex.Message);
                return false;
            }
        }

        static void MoveDirectoryWithRetry(string sourcePath, string targetPath)
        {
            Exception lastError = null;
            for (var attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    Directory.Move(sourcePath, targetPath);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(2000);
                }
            }

            throw lastError;
        }

        static int CountProcessesByName(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                var count = processes.Length;
                foreach (var process in processes)
                {
                    process.Dispose();
                }

                return count;
            }
            catch
            {
                return 0;
            }
        }

        static void DownloadAndExtractPackage(string packageUrl, string folderName, string targetPath, ExecutionResult result)
        {
            var sevenZipPath = Find7ZipPath();
            if (sevenZipPath.Length == 0)
            {
                throw new InvalidOperationException("未找到 7-Zip，请确认客户端已安装 7-Zip。");
            }

            var tempRoot = Path.Combine(Path.GetTempPath(), "AIOptimizeUpgrade_" + Guid.NewGuid().ToString("N"));
            var zipPath = Path.Combine(tempRoot, folderName + ".zip");
            var extractPath = Path.Combine(tempRoot, "extract");

            try
            {
                Directory.CreateDirectory(tempRoot);
                Directory.CreateDirectory(extractPath);

                if (!DownloadFileToPath(packageUrl, zipPath, 600000))
                {
                    var psResult = TryDownloadWithPowerShell(packageUrl, zipPath);
                    if (psResult.Length > 0)
                    {
                        result.Info(psResult);
                    }
                }

                if (!File.Exists(zipPath) || new FileInfo(zipPath).Length == 0)
                {
                    throw new InvalidOperationException(folderName + " 压缩包下载失败。");
                }

                if (!RunExecutableAndWait(sevenZipPath, "x \"" + zipPath + "\" -o\"" + extractPath + "\" -y", 600000))
                {
                    throw new InvalidOperationException(folderName + " 解压失败。");
                }

                MoveExtractedContent(extractPath, folderName, targetPath);
                result.Success(folderName + " 已更新。");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempRoot))
                    {
                        Directory.Delete(tempRoot, true);
                    }
                }
                catch
                {
                }
            }
        }

        static string Find7ZipPath()
        {
            var candidates = new[]
            {
                @"C:\Program Files\7-Zip\7z.exe",
                @"C:\Program Files (x86)\7-Zip\7z.exe",
                "7z.exe"
            };

            foreach (var candidate in candidates)
            {
                try
                {
                    if (Path.IsPathRooted(candidate) && File.Exists(candidate))
                    {
                        return candidate;
                    }

                    if (!Path.IsPathRooted(candidate) && RunExecutableAndWait(candidate, "", 5000))
                    {
                        return candidate;
                    }
                }
                catch
                {
                }
            }

            return "";
        }

        static void MoveExtractedContent(string extractPath, string folderName, string targetPath)
        {
            var nestedRoot = Path.Combine(extractPath, folderName);
            var sourceRoot = Directory.Exists(nestedRoot) ? nestedRoot : extractPath;

            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            Directory.CreateDirectory(targetPath);

            if (!RunRobocopy(sourceRoot, targetPath, 900000))
            {
                throw new InvalidOperationException(folderName + " 文件复制失败。");
            }
        }

        static bool RunRobocopy(string sourcePath, string targetPath, int timeoutMilliseconds)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = "robocopy.exe";
            psi.Arguments = "\"" + sourcePath + "\" \"" + targetPath + "\" /E /COPY:DAT /DCOPY:DAT /R:3 /W:2 /NFL /NDL /NP";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = false;
            psi.RedirectStandardError = false;

            using (var process = Process.Start(psi))
            {
                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    try { process.Kill(); } catch { }
                    return false;
                }

                return process.ExitCode <= 7;
            }
        }

        static bool RunExecutableAndWait(string fileName, string arguments, int timeoutMilliseconds)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = fileName;
            psi.Arguments = arguments;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = false;
            psi.RedirectStandardError = false;

            using (var process = Process.Start(psi))
            {
                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    try { process.Kill(); } catch { }
                    return false;
                }

                return process.ExitCode == 0;
            }
        }

        static bool WaitForPythonProcess(int timeoutMilliseconds)
        {
            var deadline = DateTime.Now.AddMilliseconds(timeoutMilliseconds);
            while (DateTime.Now < deadline)
            {
                try
                {
                    var processes = Process.GetProcessesByName("python");
                    if (processes.Length > 0)
                    {
                        foreach (var process in processes)
                        {
                            process.Dispose();
                        }
                        return true;
                    }
                }
                catch
                {
                }

                Thread.Sleep(1000);
            }

            return false;
        }

        static bool RunCommandAndWait(string command, int timeoutMilliseconds, ExecutionResult result)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c " + command;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = false;
                psi.RedirectStandardError = false;

                using (var process = Process.Start(psi))
                {
                    if (!process.WaitForExit(timeoutMilliseconds))
                    {
                        try { process.Kill(); } catch { }
                        result.Error("命令执行超时：" + command);
                        return false;
                    }

                    if (process.ExitCode != 0)
                    {
                        result.Error("命令执行失败：" + command);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                result.Error("命令执行失败：" + command + "；详情：" + ex.Message);
                return false;
            }
        }

        static void RunCommandAndWaitAllowFailure(string command, int timeoutMilliseconds, ExecutionResult result)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c " + command;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = false;
                psi.RedirectStandardError = false;

                using (var process = Process.Start(psi))
                {
                    if (!process.WaitForExit(timeoutMilliseconds))
                    {
                        try { process.Kill(); } catch { }
                        result.Warn("命令执行超时：" + command);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Warn("命令执行遇到问题：" + command + "；详情：" + ex.Message);
            }
        }

        static bool StartCommandDetached(string command, ExecutionResult result)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c start \"\" /b " + command;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = false;
                psi.RedirectStandardError = false;

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit(5000);
                }

                return true;
            }
            catch (Exception ex)
            {
                result.Error("启动服务失败。详情：" + ex.Message);
                return false;
            }
        }

        static bool DownloadFileWithFallback(string url, string path, string displayName, ExecutionResult result)
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }

            try
            {
                ServicePointManager.SecurityProtocol =
                    (SecurityProtocolType)3072 |
                    SecurityProtocolType.Tls |
                    SecurityProtocolType.Ssl3;

                using (var client = CreateWebClient(30000))
                {
                    client.DownloadFile(url, path);
                }

                if (File.Exists(path) && new FileInfo(path).Length > 0)
                {
                    result.Info(displayName + " 文件已通过 .NET 下载。");
                    return true;
                }
            }
            catch (Exception ex)
            {
                result.Info(".NET 下载 " + displayName + " 未成功，改用 PowerShell 下载方式。详情：" + ex.Message);
            }

            var psResult = TryDownloadWithPowerShell(url, path);
            if (psResult.Length > 0)
            {
                result.Info(psResult);
            }

            return File.Exists(path) && new FileInfo(path).Length > 0;
        }

        static string TryDownloadWithPowerShell(string url, string path)
        {
            try
            {
                var command = "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
                    "Invoke-WebRequest -Uri '" + url + "' -OutFile '" + path + "' -UseBasicParsing -ErrorAction Stop";
                var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));

                var psi = new ProcessStartInfo();
                psi.FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -EncodedCommand " + encoded;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                using (var process = Process.Start(psi))
                {
                    if (!process.WaitForExit(60000))
                    {
                        try { process.Kill(); } catch { }
                        return "PowerShell 下载超时。";
                    }

                    var error = process.StandardError.ReadToEnd().Trim();
                    if (process.ExitCode != 0 && error.Length > 0)
                    {
                        return "PowerShell 下载返回错误：" + error;
                    }
                }
            }
            catch (Exception ex)
            {
                return "PowerShell 下载调用失败：" + ex.Message;
            }

            return "";
        }

        static TimeoutWebClient CreateWebClient(int timeoutMilliseconds)
        {
            ServicePointManager.SecurityProtocol =
                (SecurityProtocolType)3072 |
                SecurityProtocolType.Tls |
                SecurityProtocolType.Ssl3;

            var client = new TimeoutWebClient(timeoutMilliseconds);
            client.Headers.Add("User-Agent", "WindowsPowerShell/5.1 AIOptimizeTool");
            if (WebRequest.DefaultWebProxy != null)
            {
                client.Proxy = WebRequest.DefaultWebProxy;
                client.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }

            return client;
        }

        static bool DownloadFileToPath(string url, string path, int timeoutMilliseconds)
        {
            try
            {
                using (var client = CreateWebClient(timeoutMilliseconds))
                {
                    client.DownloadFile(url, path);
                }

                return File.Exists(path) && new FileInfo(path).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        static string ReplaceOrAppend(string content, string key, string value)
        {
            var lines = new List<string>(content.Replace("\r\n", "\n").Split('\n'));
            var prefix = key + "=";
            var replaced = false;

            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = prefix + value;
                    replaced = true;
                }
            }

            if (!replaced)
            {
                lines.Add(prefix + value);
            }

            return string.Join(Environment.NewLine, lines.ToArray()).TrimEnd() + Environment.NewLine;
        }

        static string BuildDefaultEnvContent()
        {
            return
@"TERMINAL_MODAL_IMAGE=nikolaik/python-nodejs:python3.11-nodejs20
TERMINAL_TIMEOUT=60
TERMINAL_LIFETIME_SECONDS=300
BROWSERBASE_PROXIES=true
BROWSERBASE_ADVANCED_STEALTH=false
BROWSER_SESSION_TIMEOUT=300
BROWSER_INACTIVITY_TIMEOUT=120
WEB_TOOLS_DEBUG=false
VISION_TOOLS_DEBUG=false
MOA_TOOLS_DEBUG=false
IMAGE_TOOLS_DEBUG=false
HERMES_MAX_ITERATIONS=35
CONNECTED_PLATFORMS=msgw
MSGW_URL=https://mcp.qilu-pharma.com
MSGW_AGENT_SECRET=4b8e1d9c6f2a7e3b5c0d1a9f8e6b2c4d
MSGW_AGENT_NAME=default.none
MSGW_ALLOW_ALL_USERS=true
GATEWAY_ALLOW_ALL_USERS=true
MSGW_LOG=false
MSGW_AGENT_DEBUG=false
MSGW_HOME_CHANNEL=qiwork:000000
";
        }

        static string BuildStartupContent()
        {
            return
@"@echo off
setlocal

tasklist /FI ""IMAGENAME eq LdTerm.exe"" /NH | findstr /I /B ""LdTerm.exe"" >nul

if %errorlevel%==0 (
    timeout /t 5 /nobreak >nul
) else (
    timeout /t 10 /nobreak >nul
)

start """" /b hermes-web-ui start

endlocal
";
        }

        static Panel CreatePanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;
            return panel;
        }

        static Label FormLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Anchor = AnchorStyles.Left;
            label.ForeColor = Color.FromArgb(36, 45, 59);
            label.Margin = new Padding(0, 10, 10, 8);
            return label;
        }

        static TextBox CreateInputBox(string placeholder)
        {
            var box = new TextBox();
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(0, 6, 0, 8);
            box.Font = new Font("Microsoft YaHei UI", 10.5F);
            box.PlaceholderTextCompat(placeholder);
            return box;
        }

        void SetButtonsEnabled(bool enabled)
        {
            foreach (var button in taskButtons)
            {
                button.Enabled = enabled;
            }
        }

        void SetStatus(string text, Color textColor, Color lampColor)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = textColor;
            statusLamp.LampColor = lampColor;
            statusLamp.Invalidate();
        }

        void AppendLog(string message)
        {
            logBox.AppendText(DateTime.Now.ToString("HH:mm:ss ") + message + Environment.NewLine);
        }

        static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }

    sealed class ExecutionResult
    {
        public readonly List<string> LogLines = new List<string>();
        public bool Succeeded = true;
        public bool HasWarnings;

        public void Info(string message) { LogLines.Add("[INFO] " + message); }
        public void Success(string message) { LogLines.Add("[OK] " + message); }
        public void Warn(string message) { HasWarnings = true; LogLines.Add("[WARN] " + message); }
        public void Error(string message) { Succeeded = false; LogLines.Add("[ERROR] " + message); }
    }

    sealed class StatusLamp : Control
    {
        public Color LampColor { get; set; }

        public StatusLamp()
        {
            LampColor = Color.FromArgb(138, 147, 160);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var size = Math.Min(18, Math.Min(Width, Height) - 8);
            var x = (Width - size) / 2;
            var y = (Height - size) / 2;
            using (var shadow = new SolidBrush(Color.FromArgb(32, 0, 0, 0)))
            {
                e.Graphics.FillEllipse(shadow, x + 1, y + 2, size, size);
            }
            using (var brush = new SolidBrush(LampColor))
            {
                e.Graphics.FillEllipse(brush, x, y, size, size);
            }
            using (var highlight = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
            {
                e.Graphics.FillEllipse(highlight, x + 4, y + 3, size / 3, size / 3);
            }
        }
    }

    sealed class WrenchBadge : Control
    {
        public WrenchBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(2, 2, Width - 4, Height - 4);
            using (var brush = new SolidBrush(Color.FromArgb(241, 248, 254)))
            using (var pen = new Pen(Color.FromArgb(112, 177, 223), 2F))
            {
                e.Graphics.FillEllipse(brush, rect);
                e.Graphics.DrawEllipse(pen, rect);
            }

            DrawWrench(e.Graphics, new Rectangle(10, 10, Width - 20, Height - 20), Color.FromArgb(35, 92, 145));
        }

        static void DrawWrench(Graphics graphics, Rectangle bounds, Color color)
        {
            using (var pen = new Pen(color, 5F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                graphics.DrawLine(pen, bounds.Left + 10, bounds.Bottom - 8, bounds.Right - 9, bounds.Top + 11);
            }

            using (var brush = new SolidBrush(color))
            {
                graphics.FillEllipse(brush, bounds.Left + 5, bounds.Bottom - 14, 12, 12);
                graphics.FillPie(brush, bounds.Right - 18, bounds.Top + 2, 18, 18, 30, 250);
            }

            using (var cutout = new SolidBrush(Color.FromArgb(241, 248, 254)))
            {
                graphics.FillEllipse(cutout, bounds.Right - 12, bounds.Top + 8, 9, 9);
            }
        }
    }

    sealed class TimeoutWebClient : WebClient
    {
        readonly int timeoutMilliseconds;

        public TimeoutWebClient(int timeoutMilliseconds)
        {
            this.timeoutMilliseconds = timeoutMilliseconds;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = timeoutMilliseconds;
            return request;
        }
    }

    static class TextBoxExtensions
    {
        public static string RealText(this TextBox textBox)
        {
            var placeholder = textBox.Tag as string;
            if (textBox.ForeColor == Color.Gray && textBox.Text == placeholder)
            {
                return "";
            }

            return textBox.Text.Trim();
        }

        public static void PlaceholderTextCompat(this TextBox textBox, string text)
        {
            textBox.Tag = text;
            textBox.ForeColor = Color.Gray;
            textBox.Text = text;

            textBox.GotFocus += (sender, args) =>
            {
                if (textBox.ForeColor == Color.Gray && textBox.Text == text)
                {
                    textBox.Text = "";
                    textBox.ForeColor = SystemColors.WindowText;
                }
            };

            textBox.LostFocus += (sender, args) =>
            {
                if (textBox.Text.Length == 0)
                {
                    textBox.ForeColor = Color.Gray;
                    textBox.Text = text;
                }
            };
        }
    }
}
