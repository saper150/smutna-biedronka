







using System.Collections.Generic;
using System.Linq;
using Monad;
using MongoDB.Driver;


public interface IProcessManager {
    void AddApp(AppInfo info);

}

class ProcessManager : IProcessManager {
    List<RunApp> Apps = new List<RunApp>();

    public ProcessManager(Try<IMongoDatabase> tryDb) {
        System.Console.WriteLine("process manager");
        var tryResult = tryDb.Try(db => {
            return db.GetCollection<AppInfo>("apps").AsQueryable().ToList();
        }).Memo()();

        if (tryResult.IsRight) {
            Apps.AddRange(tryResult.Right.Select(x => new RunApp(x.Name, x._id, new AppConfig())));
        }
        foreach (var item in Apps) {
            item.LogMessage += HandleLogMessage;
        }
    }

    public void AddApp(AppInfo info) {
        throw new System.NotImplementedException();
    }

    void HandleLogMessage(string type, string message) {
        System.Console.WriteLine(type + " " + "message");
    }

}
