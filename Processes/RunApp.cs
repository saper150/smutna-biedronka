using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
class AppConfig {
    public Dictionary<string, string> EnvVariables { get; set; } = new Dictionary<string, string>();
}
class RunApp : IDisposable {
    public event Action<string, string> LogMessage;

    string _appName;
    Process process;
    AppConfig _config;
    string _id;
    public RunApp(string appName, string id, AppConfig config) {
        _appName = appName;
        _config = config;
        _id = id;
        Run();
    }
    public bool IsRunning() {
        return process.HasExited;
    }
    int retries = 0;
    async void ProcessExit(object sender, EventArgs eventArgs) {
        System.Console.WriteLine("exited");
        if (retries++ <= 5) {
            LogMessage("controll", "app exited with code " + process.ExitCode);
            await Task.Delay(TimeSpan.FromSeconds(1));
            LogMessage("controll", "retrying to start app for " + retries);
            Run();
        }
        LogMessage("controll", "retry 5 failed giving up");
    }

    void Run() {
        process = new Process() {
            EnableRaisingEvents = true,
        };
        process.Exited += ProcessExit;
        process.ErrorDataReceived += (sender, message) => {
            LogMessage("error", message.Data);
        };

        process.OutputDataReceived += (sender, message) => {
            LogMessage("message", message.Data);
        };

        var startInfo = new ProcessStartInfo {
            WorkingDirectory = "apps/" + _appName,
            FileName = "cmd.exe",
            Arguments = "/c npm run start",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var item in _config.EnvVariables) {
            startInfo.EnvironmentVariables[item.Key] = item.Value;
        }
        process.StartInfo = startInfo;
        process.Start();
        while (!process.StandardOutput.EndOfStream) {
            System.Console.WriteLine(process.StandardOutput.ReadLine());
        }
    }

    public void Dispose() {
        process.Kill();
        process.Dispose();
    }
}
