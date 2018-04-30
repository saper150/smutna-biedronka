
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

static class WebsocketExtensions {
    public static Task SendMessage(this WebSocket socket, byte[] message) {
        return socket.SendAsync(new System.ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
    }
    public static Task SendJson(this WebSocket socket, object message) {
        return socket.SendMessage(message.ToJsonBytes());
    }
}

static class JsonExtension {
    public static byte[] ToJsonBytes(this object obj) {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
    }
}
