const NginxConfFile = require('nginx-conf').NginxConfFile;
const MongoClient = require('mongodb').MongoClient;

MongoClient.connect('mongodb://localhost/analitics')
    .then(client => client.db('analytics'))
    .then(db => db.collection('apps').find({}).toArray())
    .then(apps => {
        return new Promise(resolve => {
            NginxConfFile.create(require('./config.json').configPath, (err, conf) => {
                if (err) {
                    console.log(err);
                    resolve();
                    return;
                }
                conf.nginx._remove('events')
                conf.nginx._add('events')
                conf.nginx.events._add('worker_connections', '1024')

                conf.nginx._remove('http')
                conf.nginx._add("http")

                conf.nginx.http._add("server")
                conf.nginx.http.server._add("listen", '80 default_server')
                conf.nginx.http.server._add("return", '444')


                for (let i = 0; i < apps.length; i++) {
                    conf.nginx.http._add("server")
                    addProxyServer(conf.nginx.http.server[i + 1], apps[i])
                }
                conf.nginx.http._add("server")
                addProxyServer(conf.nginx.http.server[conf.nginx.http.server.length - 1], {
                    Port: 5000,
                    ServerNames: ['admin.localhost']
                })

                conf.on('flushed', function () {
                    resolve()
                });
                conf.flush()
            })
        })
    }).then(() => {
        process.exit()
    }).catch(err => {
        console.log(err)
        process.exit()
    })


function addProxyServer(server, serverConf) {
    server._add("listen", '80')
    server._add("location", '/')
    if (serverConf.ServerNames) {
        server._add('server_name', serverConf.ServerNames.join(' '))
    }
    server.location._add('proxy_pass', `http://localhost:${serverConf.Port}`)
    server.location._add('proxy_http_version', "1.1")
    server.location._add('proxy_set_header', "Upgrade $http_upgrade")
    server.location._add('proxy_set_header', "Host $http_host")
    server.location._add('proxy_set_header', `Connection $http_connection`)
}
