

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Newtonsoft.Json;
using Monad;



enum ListenType {
    MongoPerformance,
    MongoStatus
}

interface ISocketServicec {
    event Action<WebSocket, ListenType> newWebsocket;
    void Broadcast(byte[] message, ListenType to);
    void Broadcast(object message, ListenType to);
    Task ServeSocket(WebSocket socket, ListenType type);
}

class SocketService : ISocketServicec {

    struct SocketMessage {
        public byte[] Message;
        public ListenType To;
    }

    public event Action<WebSocket, ListenType> newWebsocket;

    ConcurrentDictionary<WebSocket, ListenType> Sockets { get; set; } = new ConcurrentDictionary<WebSocket, ListenType>();
    BlockingCollection<SocketMessage> MessagesToSend { get; set; } = new BlockingCollection<SocketMessage>(new ConcurrentQueue<SocketMessage>(), 100);
    public SocketService() {
        Task.Run(() => {
            while (true) {
                var message = MessagesToSend.Take();
                foreach (var item in Sockets) {
                    if (item.Value == message.To) {
                        item.Key.SendMessage(message.Message);
                    }
                }
            }
        });
    }

    public void Broadcast(byte[] message, ListenType to) {
        MessagesToSend.Add(new SocketMessage { Message = message, To = to });
    }

    public void Broadcast(object message, ListenType to) {
        MessagesToSend.Add(new SocketMessage { Message = message.ToJsonBytes(), To = to });
    }

    async public Task ServeSocket(WebSocket socket, ListenType type) {
        newWebsocket(socket, type);
        Sockets.TryAdd(socket, type);
        var buffer = new byte[1024];
        while (!(await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)).CloseStatus.HasValue) ;
        ListenType removed;
        Sockets.Remove(socket, out removed);
        await socket.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
    }
}
