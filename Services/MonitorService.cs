

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monad;
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
interface ITimerService {
    void Run();
}
interface IMonitorService : ITimerService { }
class MonitorService : IMonitorService {
    ISocketServicec socketServicec;
    Try<IMongoDatabase> db;
    System.Threading.Timer Timer;
    MongoPerformanceRecord LastRecord;
    public MonitorService(ISocketServicec socketServicec, Try<IMongoDatabase> db) {
        db.databaseStatusChange += isOnline => {
            System.Console.WriteLine("status callback");
            socketServicec.Broadcast(new { isOnline = isOnline }, ListenType.MongoStatus);
        };
        this.socketServicec = socketServicec;
        this.db = db;
    }

    void ITimerService.Run() {
        RawRecord().Match(
            Right: record => this.LastRecord = record,
            Left: err => System.Console.WriteLine(err.Message)
        )();

        this.Timer = new System.Threading.Timer(e => {
            RawRecord().Match(
                Right: record => {
                    var diff = DiffToLast(record);
                    this.LastRecord = record;

                    this.db.Try(db => {
                        db.GetCollection<MongoPerformanceRecord>("mongoPerformance").InsertOne(diff);
                        return true;
                    })();
                    this.socketServicec.Broadcast(diff, ListenType.MongoPerformance);
                },
                Left: err => System.Console.WriteLine(err.Message)
            )();
        }, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
    }

    Either<Exception, MongoPerformanceRecord> RawRecord() {
        return this.db.Try(db =>
            db.RunCommand<BsonDocument>(
                new BsonDocument { { "serverStatus", 1 }, { "mem", 1 }, { "metrics", 1 }, { "connections", 1 } }
            )
        ).Select(result => new MongoPerformanceRecord() {
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

}
