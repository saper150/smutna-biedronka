using System;
using Monad;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
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
        return () => {
            try {
                var result = action(this.db);
                if (!IsOnline) {
                    System.Console.WriteLine("status online");
                    IsOnline = true;
                    databaseStatusChange(true);
                }
                return result;
            } catch (System.Exception ex) {
                if (IsOnline) {
                    IsOnline = false;
                    databaseStatusChange(false);
                }
                return ex;
            }
        };
    }
}
