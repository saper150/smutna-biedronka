using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
class AppConfig {
    public Dictionary<string, string> EnvVariables { get; set; } = new Dictionary<string, string>();
}
class StartApp : IDisposable {
    public event Action<string, string> LogMessage;

    public string AppName { get; private set; }
    public string WorkingDir { get { return Path.Combine("apps", AppName); } }
    Process process;
    AppConfig _config;
    IShell _shell;

    public StartApp(string appName, IShell shell, AppConfig config, Action<string, string> onMessage) {
        _shell = shell;
        AppName = appName;
        _config = config;
        LogMessage += onMessage;
        if (File.Exists(Path.Combine(WorkingDir, "index.js"))) {
            System.Console.WriteLine("Running " + appName);
            DownloadNpm();
            Run();
        } else {
            System.Console.WriteLine("directory not found " + appName);
        }
    }
    public bool IsRunning() {
        return process.HasExited;
    }
    void ProcessExit(object sender, EventArgs eventArgs) { }

    public int MemUsage() {
        if (process != null && !process.HasExited) {
            return (int)(process.WorkingSet64 / (1024 * 1024));
        } else {
            return 0;
        }
    }

    void DownloadNpm() {
        var process = new BlockingProcess() {
            StartInfo = new ProcessStartInfo() {
                FileName = _shell.GetExecuteFileName(),
                Arguments = _shell.FormatCommand("npm install"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = WorkingDir
            }
        };
        var result = process.Run();
        LogMessage("npm", result.Item2);
    }

    void Run() {

        process = new Process() {
            EnableRaisingEvents = true,
        };
        process.Exited += ProcessExit;
        process.ErrorDataReceived += (sender, message) => {
            System.Console.WriteLine("receved data");
            System.Console.WriteLine(message.Data);
            LogMessage("error", message.Data);
        };

        process.OutputDataReceived += (sender, message) => {
            LogMessage("message", message.Data);
        };

        var startInfo = new ProcessStartInfo() {
            FileName = "node",
            Arguments = "index.js",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        startInfo.WorkingDirectory = WorkingDir;
        foreach (var item in _config.EnvVariables) {
            startInfo.EnvironmentVariables[item.Key] = item.Value;
        }
        process.StartInfo = startInfo;
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
    }

    public void Dispose() {
        try {
            if (process != null) {
                process.Kill();
                process.WaitForExit();
            }
        } catch (System.Exception ex) {
            System.Console.WriteLine(ex.Message);
        }

    }
}
