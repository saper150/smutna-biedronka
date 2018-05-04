using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

class ServeWebsocketMiddleware {
    public RequestDelegate _next { get; set; }
    public ISocketServicec _socektService { get; set; }
    public ServeWebsocketMiddleware(RequestDelegate next, ISocketServicec socektService) {
        this._next = next;
        this._socektService = socektService;
    }

    public async Task Invoke(HttpContext context) {

        var path = context.Request.Path.ToString().Split("/").Where(x => !string.IsNullOrEmpty(x)).ToArray();
        if (path.Length >= 2 && path[0] == "ws" && context.WebSockets.IsWebSocketRequest) {
            await this._socektService.ServeSocket(await context.WebSockets.AcceptWebSocketAsync(), path[1]);
        } else {
            await _next.Invoke(context);
        }
    }
}