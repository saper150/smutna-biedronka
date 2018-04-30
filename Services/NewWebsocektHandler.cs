

using System;
using System.Linq;
using System.Net.WebSockets;
using MongoDB.Driver;
using Monad;
class NewWebsocekthandler {

    Try<IMongoDatabase> _db;
    public NewWebsocekthandler(Try<IMongoDatabase> db, ISocketServicec socektService) {
        _db = db;
        socektService.newWebsocket += (socket, type) => {
            if (type == ListenType.MongoPerformance) {
                SendInitialData(socket);
            } else if (type == ListenType.MongoStatus) {
                socket.SendJson(new { isOnline = db.IsOnline });
            };
        };

    }

    private void SendInitialData(WebSocket socket) {
        _db.Try(db => db.GetCollection<MongoPerformanceRecord>("mongoPerformance")
            .AsQueryable()
            .OrderByDescending(x => x.Time)
            .Take(100)
            .ToArray()
            .Reverse()
    ).Match(
        Right: records => { socket.SendJson(records); },
        Left: error => System.Console.WriteLine(error.Message)
    )();

    }
}