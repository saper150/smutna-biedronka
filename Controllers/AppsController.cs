using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Monad;
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
        public AppsController(Try<IMongoDatabase> db, IProcessManager processManager, IHostingEnvironment environment) {
            _processManager = processManager;
            _environment = environment;
            _db = db;
        }

        [HttpPost("[action]")]
        public IActionResult Create([FromBody] AppCreateInfo info) {
            var tryResult = _db.Try(_db => {
                var doc = new AppInfo {
                    Name = info.name,
                    Port = info.port,
                    ApiKey = Crypto.SecureRandomString()
                };
                _db.GetCollection<AppInfo>("apps").InsertOne(doc);
                return doc;
            }).Memo()();

            if (tryResult.IsRight) {
                return Ok(tryResult.Right);
            } else {
                return BadRequest(tryResult.Left);
            }
        }

        [HttpGet("[action]")]
        public IActionResult Get() {
            var tryResult = _db.Try(_db => {
                return _db.GetCollection<AppInfo>("apps").AsQueryable();
            }).Memo()();

            if (tryResult.IsRight) {
                return Ok(tryResult.Right);
            } else {
                return BadRequest(tryResult.Left);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddAppFiles(string apiKey, IFormFile file) {
            var tryResult = _db.Try(_db => {
                return _db.GetCollection<AppInfo>("apps").AsQueryable()
                    .FirstOrDefault(x => x.ApiKey == apiKey);
            }).Memo()();

            if (tryResult.IsLeft) {
                return BadRequest(tryResult.Left);
            }

            if (tryResult.Right == null) {
                return BadRequest(new { error = "wrong api key" });
            }

            if (Path.GetExtension(file.Name) == "zip") {
                return BadRequest(new { error = "only zip files" });
            }

            var appPath = Path.Combine("apps", tryResult.Right.Name);

            if (System.IO.Directory.Exists(appPath)) {
                System.IO.Directory.Delete(appPath, true);
            }

            Directory.CreateDirectory("workingDir");

            var filePath = Path.Combine("workingDir", Guid.NewGuid().ToString());
            using (var stream = new FileStream(filePath, FileMode.Create)) {
                await file.CopyToAsync(stream);
            }
            ZipFile.ExtractToDirectory(filePath, Path.Combine("apps", tryResult.Right.Name));

            System.IO.File.Delete(filePath);
            return Ok(file);

        }
    }
}
