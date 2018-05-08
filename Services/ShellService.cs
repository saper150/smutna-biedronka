
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
    OSPlatform OS;
    public ShellService(IEnvironmentService envService) {
        OS = envService.OSType();
        if (OS == OSPlatform.Windows) {
            cmdString = "cmd.exe";
            cmdArgument = "/c";
        } else {
            cmdString = "/bin/bash";
            cmdArgument = "-c";
        }
    }

    public string FormatCommand(string command) {
        if (OS == OSPlatform.Linux) {
            return $"{cmdArgument} \"{command}\"";
        } else {
            System.Console.WriteLine(command.Replace("\"","\\\""));
            return $"{cmdArgument} {command.Replace("\"","\\\"")}";
        }
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
