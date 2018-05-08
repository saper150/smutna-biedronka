using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;


public interface IProcessManager {
    void Restart(AppInfo info);
    void Remove(AppInfo info);
    IEnumerable<AppUsage> MemUsage();
    void KillAll();
}

class LogModel {
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;
    public string Type { get; set; }
    public string Message { get; set; }
}

public struct AppUsage {
    public int MemUsage { get; set; }
    public string AppName { get; set; }
}

class ProcessManager : IProcessManager {
    ConcurrentDictionary<string, StartApp> Apps = new ConcurrentDictionary<string, StartApp>();
    Try<IMongoDatabase> _db;
    IShell _shell;
    public ProcessManager(Try<IMongoDatabase> tryDb, IShell shell, IEnvironmentService envService) {
        _shell = shell;
        _db = tryDb;
        tryDb.Try(db => {
            return db.GetCollection<AppInfo>("apps").AsQueryable().ToList();
        }).Right(apps => {
            foreach (var item in apps) {
                Restart(item);
            }
            return Unit.Default;
        }).Left(x => Unit.Default);
    }
    public void Restart(AppInfo info) {
        lock (this) {
            StartApp removed = null;
            if (Apps.TryRemove(info.Name, out removed)) {
                removed.Dispose();
            }
            var app = new StartApp(info.Name, _shell, new AppConfig() {
                EnvVariables = new Dictionary<string, string>() {
                    { "PORT", info.Port.ToString() }
                }
            }, HandleLogMessage(info.Name));
            Apps.TryAdd(info.Name, app);
        }
    }

    public void KillAll() {
        lock (this) {
            foreach (var item in Apps) {
                item.Value.Dispose();
            }
            this.Apps.Clear();
        }
    }

    IEnumerable<AppUsage> IProcessManager.MemUsage() {
        return Apps.Select(app => new AppUsage() {
            AppName = app.Value.AppName,
            MemUsage = app.Value.MemUsage()
        });
    }

    Action<string, string> HandleLogMessage(string appName) {
        return (type, message) => {
            System.Console.WriteLine(message);
            _db.Try(db => {
                return db.GetCollection<LogModel>(appName).InsertOneAsync(new LogModel() {
                    Type = type,
                    Message = message
                });
            }).Right(x => Unit.Default)
            .Left(x => Unit.Default);
        };
    }

    public void Remove(AppInfo info) {
        StartApp app;
        if (Apps.TryRemove(info.Name, out app)) {
            app.Dispose();
        }
    }
}
