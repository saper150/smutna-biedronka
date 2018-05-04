

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



public enum ListenType {
    MongoPerformance,
    MongoStatus
}


public interface ISocketServicec {
    event Action<WebSocket, string> newWebsocket;
    void Broadcast(byte[] message, string chanel);
    void Broadcast(object message, string chanel);
    Task ServeSocket(WebSocket socket, string chanel);
}

class SocketService : ISocketServicec {

    struct SocketMessage {
        public byte[] Message;
        public string Chanel;
    }

    public event Action<WebSocket, string> newWebsocket;

    ConcurrentDictionary<WebSocket, string> Sockets { get; set; } = new ConcurrentDictionary<WebSocket, string>();
    BlockingCollection<SocketMessage> MessagesToSend { get; set; } = new BlockingCollection<SocketMessage>(new ConcurrentQueue<SocketMessage>(), 100);

    public SocketService() {
        Task.Run(() => {
            while (true) {
                var message = MessagesToSend.Take();
                foreach (var item in Sockets) {
                    if (item.Value == message.Chanel) {
                        item.Key.SendMessage(message.Message);
                    }
                }
            }
        });
    }

    public void Broadcast(byte[] message, string chanel) {
        MessagesToSend.Add(new SocketMessage { Message = message, Chanel = chanel });
    }

    public void Broadcast(object message, string chanel) {
        MessagesToSend.Add(new SocketMessage { Message = message.ToJsonBytes(), Chanel = chanel });
    }

    async public Task ServeSocket(WebSocket socket, string chanel) {
        newWebsocket(socket, chanel);
        Sockets.TryAdd(socket, chanel);
        var buffer = new byte[1024];
        while (!(await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)).CloseStatus.HasValue) ;
        string removed;
        Sockets.Remove(socket, out removed);
        await socket.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
    }

    public void SendToApp(object message, string appName) {
        throw new NotImplementedException();
    }
}
