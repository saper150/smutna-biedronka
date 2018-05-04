using System;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using LanguageExt;
using LanguageExt.DataTypes.Serialisation;

public interface Try<T> {
    Either<Exception, U> Try<U>(Func<T, U> action);
    event Action<bool> databaseStatusChange;
    bool IsOnline { get; }

}

class TryMongoService : Try<IMongoDatabase> {

    IMongoDatabase db;
    public event Action<bool> databaseStatusChange;
    public bool IsOnline { get; private set; } = true;
    public TryMongoService() {
        this.db = new MongoClient("mongodb://localhost/analitics").GetDatabase("analytics");
    }

    public Either<Exception, T> Try<T>(Func<IMongoDatabase, T> action) {
        try {
            var result = action(this.db);
            if (!IsOnline) {
                IsOnline = true;
                databaseStatusChange(true);
            }
            if (result == null) {
                return new Exception("Element not found");
            }
            return result;
        } catch (System.Exception ex) {
            if (IsOnline) {
                IsOnline = false;
                databaseStatusChange(false);
            }
            return ex;
        }
    }
}
