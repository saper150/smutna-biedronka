

using System;
using System.Linq;
using System.Net.WebSockets;
using MongoDB.Driver;
public class NewWebsocekthandler {

    Try<IMongoDatabase> _db;
    public NewWebsocekthandler(Try<IMongoDatabase> db, ISocketServicec socektService) {
        _db = db;
        socektService.newWebsocket += (socket, type) => {
            if (type == "_mongoPerformance") {
                SendInitialData(socket);
            } else if (type == "_mongoStatus") {
                socket.SendJson(new { isOnline = db.IsOnline });
            } else {
                SendAppPerf(socket, type);
            };
        };
    }

    void SendAppPerf(WebSocket socekt, string appName) {
        _db.Try(db => db.GetCollection<AppPerformance>("appPerformance")
            .AsQueryable()
            .Where(x => x.AppName == appName)
            .OrderByDescending(x => x.Time)
            .Take(100)
            .ToArray()
            .Reverse()
        ).Right(records => { socekt.SendJson(records); })
        .Left(err => System.Console.WriteLine(err));
    }

    private void SendInitialData(WebSocket socket) {
        _db.Try(db => db.GetCollection<MongoPerformanceRecord>("mongoPerformance")
            .AsQueryable()
            .OrderByDescending(x => x.Time)
            .Take(100)
            .ToArray()
            .Reverse()
        ).Right(records => { socket.SendJson(records); })
        .Left(err => System.Console.WriteLine(err));
    }
}