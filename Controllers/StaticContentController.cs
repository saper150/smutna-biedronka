
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using LanguageExt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace smutna_biedronka.Controllers {

    public class StaticContentAddEdtiModel {
        public string Name { get; set; }
        public IEnumerable<string> ServerNames { get; set; }
    }

    public class StaticContentModel {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> ServerNames { get; set; }
        public string ApiKey { get; set; }
    }

    [Route("api/[controller]")]
    public class StaticContentController : Controller {
        Try<IMongoDatabase> _db;
        IHostingEnvironment _env;
        INginxService _ngnx;
        public StaticContentController(
            Try<IMongoDatabase> db,
            IHostingEnvironment env,
            INginxService ngnx
            ) {
            _db = db;
            _env = env;
            _ngnx = ngnx;
        }

        private Either<Exception, bool> IsNameDuplicated(string name) {
            return _db.Try(db => {
                return db.GetCollection<StaticContentModel>("staticContent")
                .AsQueryable()
                .Count(x => x.Name == name) == 0 ? false : true;
            });
        }

        [HttpGet("[action]")]
        public IActionResult Get() {
            return _db.Try(db => db.GetCollection<StaticContentModel>("staticContent").AsQueryable())
                .Right(x => Ok(x) as IActionResult)
                .Left(x => BadRequest(x) as IActionResult);
        }

        [HttpPost("[action]")]
        public IActionResult Create([FromBody] StaticContentAddEdtiModel model) {
            return IsNameDuplicated(model.Name)
                .Bind(isNameDuplicated => {
                    if (isNameDuplicated) {
                        return new Exception("name is duplicated");
                    } else {
                        return _db.Try(db => {
                            var collection = db.GetCollection<StaticContentModel>("staticContent");
                            var created = new StaticContentModel() {
                                Name = model.Name,
                                ServerNames = model.ServerNames,
                                ApiKey = Crypto.SecureRandomString()
                            };
                            db.GetCollection<StaticContentModel>("staticContent")
                                .InsertOne(created);
                            return created;
                        });
                    }
                }).Bind<dynamic>(x => {
                    _ngnx.RefreshConfig();
                    return x;
                })
                .Right(x => Ok(x) as IActionResult)
                .Left(x => BadRequest(x) as IActionResult);
        }

        [HttpPost("[action]")]
        public IActionResult AddFiles(string apiKey, IFormFile file) {
            return _db.Try(_db =>
                    _db.GetCollection<StaticContentModel>("staticContent").AsQueryable()
                        .FirstOrDefault(x => x.ApiKey == apiKey)
            ).Bind<StaticContentModel>(app => {
                var appPath = Path.Combine(_env.ContentRootPath, "staticContent", app.Name);
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
                    ZipFile.ExtractToDirectory(filePath, Path.Combine("staticContent", app.Name));
                    System.IO.File.Delete(filePath);
                    return app;
                } catch (System.Exception ex) {
                    return ex;
                }
            })
            .Right(x => Ok(x) as IActionResult)
            .Left(x => BadRequest(x.Message) as IActionResult);
        }

        [HttpDelete("[action]/{apiKey}")]
        public IActionResult Delete(string apiKey) {
            System.Console.WriteLine(apiKey);
            return _db.Try(db =>
                db.GetCollection<StaticContentModel>("staticContent").AsQueryable()
                                    .FirstOrDefault(x => x.ApiKey == apiKey)
            ).Bind<StaticContentModel>(app => {
                try {
                    Directory.Delete(Path.Combine(_env.ContentRootPath, "staticContent", app.Name), true);
                } catch (System.Exception ex) {
                    System.Console.WriteLine(ex.Message);
                }
                return app;
            }).Bind(app =>
                _db.Try(db =>
                    db.GetCollection<StaticContentModel>("staticContent").DeleteOne(x => x.ApiKey == apiKey)
                )
            ).Bind<dynamic>(x => {
                _ngnx.RefreshConfig();
                return new { Deleted = true };
            })
            .Right(x => Ok(x) as IActionResult)
            .Left(x => BadRequest(x) as IActionResult);
        }
    }
}
