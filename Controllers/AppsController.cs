using System;
using System.Collections.Generic;
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
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("serverNames")]
    public IEnumerable<string> ServerNames { get; set; }
    [JsonProperty("port")]
    public int Port { get; set; }
    [JsonProperty("apiKey")]
    public string ApiKey { get; set; }
}

namespace smutna_biedronka.Controllers {

    [Route("api/[controller]")]
    public class AppsController : Controller {

        public class AppCreateInfo {
            public string Name { get; set; }
            public int Port { get; set; }
            public IEnumerable<string> ServerNames { get; set; }
        }

        public class AppUpdateInfo {
            public int Port { get; set; }
            public IEnumerable<string> ServerNames { get; set; }
        }
        Try<IMongoDatabase> _db;
        IHostingEnvironment _environment;
        IProcessManager _processManager;

        IHostingEnvironment _env;
        INginxService _nginxService;
        public AppsController(
            Try<IMongoDatabase> db,
            IProcessManager processManager,
            IHostingEnvironment environment,
            IHostingEnvironment env,
            INginxService nginxService
        ) {
            _nginxService = nginxService;
            _env = env;
            _processManager = processManager;
            _environment = environment;
            _db = db;
        }

        [HttpPost("[action]")]
        public IActionResult Create([FromBody] AppCreateInfo info) {
            return _db.Try(_db => {
                var doc = new AppInfo {
                    Name = info.Name,
                    Port = info.Port,
                    ServerNames = info.ServerNames,
                    ApiKey = Crypto.SecureRandomString()
                };
                _db.GetCollection<AppInfo>("apps").InsertOne(doc);
                _nginxService.RefreshConfig();
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

        [HttpGet("[action]/{apiKey}")]
        public IActionResult Get(string apiKey) {
            return _db.Try(_db => {
                return _db.GetCollection<AppInfo>("apps").AsQueryable()
                    .FirstOrDefault(x => x.ApiKey == apiKey);
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
        [HttpDelete("[action]/{apiKey}")]
        public IActionResult Delete(string apiKey) {
            return _db.Try(_db => {
                return _db.GetCollection<AppInfo>("apps").AsQueryable()
                    .FirstOrDefault(x => x.ApiKey == apiKey);
            }).Bind(app => {
                _processManager.Remove(app);
                return _db.Try(_db => {
                    _db.GetCollection<AppInfo>("apps").DeleteOne(x => x._id == app._id);
                    return app;
                });
            }).Right(x => Ok(x) as IActionResult)
                .Left(x => BadRequest(x.Message) as IActionResult);
        }

        [HttpPut("[action]/{apiKey}")]
        public IActionResult Update(string apiKey, [FromBody]AppUpdateInfo info) {
            return _db.Try(_db => {
                _db.GetCollection<AppInfo>("apps")
                    .UpdateOne(x => x.ApiKey == apiKey,
                     Builders<AppInfo>.Update.Combine(
                        Builders<AppInfo>.Update.Set(x => x.Port, info.Port),
                        Builders<AppInfo>.Update.Set(x => x.ServerNames, info.ServerNames)
                    ));
                return _db.GetCollection<AppInfo>("apps").AsQueryable().FirstOrDefault(x => x.ApiKey == apiKey);
            }).Bind<AppInfo>(app => {
                _nginxService.RefreshConfig();
                _processManager.Restart(app);
                return app;
            })
            .Right(x => Ok(x) as IActionResult)
            .Left(x => BadRequest(x.Message) as IActionResult);
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
                var appPath = Path.Combine(_env.ContentRootPath, "apps", app.Name);
                if (System.IO.Directory.Exists(appPath)) {
                    try {
                        System.IO.Directory.Delete(appPath, true);
                    } catch (System.Exception) { }
                }
                Directory.CreateDirectory("workingDir");
                var filePath = Path.Combine(_env.ContentRootPath, "workingDir", Guid.NewGuid().ToString());
                using (var stream = new FileStream(filePath, FileMode.Create)) {
                    file.CopyTo(stream);
                }
                try {
                    ZipFile.ExtractToDirectory(filePath, Path.Combine("apps", app.Name));
                    System.IO.File.Delete(filePath);
                    _processManager.Restart(app);
                    return app;
                } catch (System.Exception ex) {
                    return ex;
                }
            }))
            .Right(x => Ok(x) as IActionResult)
            .Left(x => BadRequest(x.Message) as IActionResult);
        }

        [HttpPost("[action]/{apiKey}")]
        public IActionResult Restart(string apiKey) {
            return _db.Try(_db => {
                return _db.GetCollection<AppInfo>("apps").AsQueryable()
                    .FirstOrDefault(x => x.ApiKey == apiKey);
            }).Bind<AppInfo>(app => {
                _processManager.Restart(app);
                return app;
            }).Right(x => Ok(x) as IActionResult)
                .Left(x => BadRequest(x.Message) as IActionResult);
        }


    }
}
