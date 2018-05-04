

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

class MongoPerformanceRecord {
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;
    public long Inserted { get; set; }
    public long Returned { get; set; }
    public long Updated { get; set; }
    public long Deleted { get; set; }
    public int MemUsage { get; set; }
}

class AppPerformance {
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; }
    public string AppName { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;
    public int MemUsage { get; set; }
}

public interface ITimerService {
    void Run();
}
public interface IMonitorService : ITimerService { }
class MonitorService : IMonitorService {
    ISocketServicec socketServicec;
    Try<IMongoDatabase> db;
    System.Threading.Timer Timer;
    MongoPerformanceRecord LastRecord;
    IProcessManager _processManager;
    public MonitorService(
        ISocketServicec socketServicec,
        Try<IMongoDatabase> db,
        IProcessManager processManager
    ) {
        this.socketServicec = socketServicec;
        this.db = db;
        _processManager = processManager;
        db.databaseStatusChange += isOnline => {
            System.Console.WriteLine("status callback");
            socketServicec.Broadcast(new { isOnline = isOnline }, "_mongoStatus");
        };
    }

    void ITimerService.Run() {
        RawRecord()
            .Right(record => {
                this.LastRecord = record;
            })
            .Left(err => System.Console.WriteLine(err.Message));


        this.Timer = new System.Threading.Timer(e => {
            RawRecord().Right(record => {
                var diff = DiffToLast(record);
                this.LastRecord = record;

                this.db.Try(db => {
                    db.GetCollection<MongoPerformanceRecord>("mongoPerformance").InsertOne(diff);
                    return true;
                });

                this.socketServicec.Broadcast(diff, "_mongoPerformance");
            }).Left(err => System.Console.WriteLine(err));

            MonitorApps();
        }, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
    }

    Either<Exception, MongoPerformanceRecord> RawRecord() {
        return this.db.Try(db =>
            db.RunCommand<BsonDocument>(
                new BsonDocument { { "serverStatus", 1 }, { "mem", 1 }, { "metrics", 1 }, { "connections", 1 } }
            )
        ).Map(result => new MongoPerformanceRecord() {
            Inserted = result["metrics"]["document"]["inserted"].ToInt64(),
            Deleted = result["metrics"]["document"]["deleted"].ToInt64(),
            Updated = result["metrics"]["document"]["updated"].ToInt64(),
            Returned = result["metrics"]["document"]["returned"].ToInt64(),
            MemUsage = result["mem"]["resident"].ToInt32()
        });
    }

    MongoPerformanceRecord DiffToLast(MongoPerformanceRecord record) {
        return new MongoPerformanceRecord {
            Inserted = record.Inserted - LastRecord.Inserted,
            Updated = record.Updated - LastRecord.Updated,
            Deleted = record.Deleted - LastRecord.Deleted,
            Returned = record.Returned - LastRecord.Returned,
            MemUsage = record.MemUsage
        };
    }



    void MonitorApps() {
        var usages = _processManager.MemUsage().ToArray();
        db.Try(db => {
            return db.GetCollection<AppPerformance>("appPerformance")
                .InsertManyAsync(usages.Select(x => new AppPerformance() {
                    AppName = x.AppName,
                    MemUsage = x.MemUsage
            }));
        })
        .Right(x => Unit.Default)
        .Left(x => Unit.Default);

        foreach (var item in _processManager.MemUsage()) {
            socketServicec.Broadcast(item, item.AppName);
        }
    }

}
