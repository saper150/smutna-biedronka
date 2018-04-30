const http = require('http')
console.log('hello')
http.createServer((req, res) => {
    res.end('Hello')
}).listen(8080)

process.exit()