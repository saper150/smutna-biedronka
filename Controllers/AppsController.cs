using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

public class AppInfo {

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; }
    public string Name { get; set; }
    public int Port { get; set; }
    public string ApiKey { get; set; }
}

namespace smutna_biedronka.Controllers {

    [Route("api/[controller]")]
    public class AppsController : Controller {

        public class AppCreateInfo {
            public string name { get; set; }
            public int port { get; set; }
        }
        Try<IMongoDatabase> _db;
        IHostingEnvironment _environment;
        IProcessManager _processManager;
        IHostingEnvironment _env;
        public AppsController(
            Try<IMongoDatabase> db,
            IProcessManager processManager,
            IHostingEnvironment environment,
            IHostingEnvironment env
        ) {
            _env = env;
            _processManager = processManager;
            _environment = environment;
            _db = db;
        }

        [HttpPost("[action]")]
        public IActionResult Create([FromBody] AppCreateInfo info) {
            return _db.Try(_db => {
                var doc = new AppInfo {
                    Name = info.name,
                    Port = info.port,
                    ApiKey = Crypto.SecureRandomString()
                };
                _db.GetCollection<AppInfo>("apps").InsertOne(doc);
                return doc;
            }).Right(x => Ok(x) as IActionResult)
                .Left(x => BadRequest(x) as IActionResult);
        }

        [HttpGet("[action]")]
        public IActionResult Get() {
            return _db.Try(_db => {
                return _db.GetCollection<AppInfo>("apps").AsQueryable();
            }).Right(x => Ok(x) as IActionResult)
            .Left(err => BadRequest(err) as IActionResult);
        }

        [HttpGet("[action]/{appName}")]
        public IActionResult Logs(string appName) {
            return _db.Try(_db => {
                return _db.GetCollection<LogModel>(appName).AsQueryable();
            }).Right(x => Ok(x) as IActionResult)
            .Left(err => BadRequest(err) as IActionResult);
        }

        [HttpPost("[action]")]
        public IActionResult AddAppFiles(string apiKey, IFormFile file) {
            return _db.Try<AppInfo>(_db => {
                System.Console.WriteLine(apiKey);
                var app = _db.GetCollection<AppInfo>("apps").AsQueryable()
                    .FirstOrDefault(x => x.ApiKey == apiKey);
                System.Console.WriteLine(app);
                return app;
            }).Bind<AppInfo>((app => {
                var appPath = Path.Combine(_env.WebRootPath, "apps", app.Name);
                if (System.IO.Directory.Exists(appPath)) {
                    try {
                        System.IO.Directory.Delete(appPath, true);
                    } catch (System.Exception) { }
                }
                Directory.CreateDirectory("workingDir");
                var filePath = Path.Combine(_env.WebRootPath, "workingDir", Guid.NewGuid().ToString());
                using (var stream = new FileStream(filePath, FileMode.Create)) {
                    file.CopyTo(stream);
                }
                try {
                    ZipFile.ExtractToDirectory(filePath, Path.Combine("apps", app.Name));
                    System.IO.File.Delete(filePath);
                    _processManager.UpdateApp(app);
                    return app;
                } catch (System.Exception ex) {
                    return ex;
                }
            }))
            .Right(x => Ok(x) as IActionResult)
            .Left(x => BadRequest(x.Message) as IActionResult);
        }

        [HttpPost("[action]")]
        public IActionResult Restart(string apiKey) {
            return _db.Try(_db => {
                return _db.GetCollection<AppInfo>("apps").AsQueryable()
                    .FirstOrDefault(x => x.ApiKey == apiKey);
            }).Bind<AppInfo>(app => {
                _processManager.UpdateApp(app);
                return app;
            }).Right(x => Ok(x) as IActionResult)
                .Left(x => BadRequest(x.Message) as IActionResult);
        }
    }
}
