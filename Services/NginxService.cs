

using System.Diagnostics;
using System.IO;
using System.Linq;
using LanguageExt;
using MongoDB.Driver;
using Newtonsoft.Json;


public interface INginxService {
    void RefreshConfig();
}

class NginxService : INginxService {

    IShell _shell;
    Try<IMongoDatabase> _db;
    public NginxService(IShell shell, Try<IMongoDatabase> db) {
        _db = db;
        _shell = shell;
    }

    class Config {
        public string configPath { get; set; }
    }

    public void RefreshConfig() {
        var p = new BlockingProcess() {
            StartInfo = new ProcessStartInfo() {
                FileName = _shell.GetExecuteFileName(),
                Arguments = _shell.FormatCommand($"node updateConfig.js"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            }
        }.Run();
        System.Console.WriteLine(p.Item2);
        new BlockingProcess() {
            StartInfo = new ProcessStartInfo() {
                FileName = _shell.GetExecuteFileName(),
                Arguments = _shell.FormatCommand($"nginx -s reload"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            }
        }.Run();
    }
}
