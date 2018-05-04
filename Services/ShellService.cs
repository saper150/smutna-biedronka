
using System.Diagnostics;
using System.Runtime.InteropServices;

interface IShell {
    ProcessStartInfo GetProcessStartInfo(string command);
    string GetExecuteFileName();
    string FormatCommand(string command);
}


class ShellService : IShell {

    string cmdString;
    string cmdArgument;
    public ShellService(IEnvironmentService envService) {
        var os = envService.OSType();
        if (os == OSPlatform.Windows) {
            cmdString = "cmd.exe";
            cmdArgument = "/c";
        } else {
            cmdString = "/bin/bash";
            cmdArgument = "-c";
        }
    }

    public string FormatCommand(string command) {
        return $"{cmdArgument} {command}";
    }

    public string GetExecuteFileName() {
        return cmdString;
    }

    public ProcessStartInfo GetProcessStartInfo(string command) {
        return new ProcessStartInfo {
            FileName = GetExecuteFileName(),
            Arguments = FormatCommand(command),
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
    }
}
