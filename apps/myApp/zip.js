const zip = require('zip-dir')
const request = require('request')
const fs = require('fs')

zip(__dirname, {
    filter: (path, stat) => !path.endsWith('node_modules')
}, (err, buffer) => {

    var formData = {
        file: buffer,
        apiKey: 'npe+NsKvr8s0hIrTJqxzM7yfXEDXY3kwG+wc6T8JxNg='
    };

    const r = request.post('http://localhost:5000/api/Apps/AddAppFiles', function (err, httpResponse, body) {
        if (err) {
            return console.error('upload failed:', err);
        }
        console.log('Upload successful!  Server responded with:', body);
        process.exit()
    });
    const form = r.form()
    form.append('file', buffer, { filename: 'zip.zip' })
    form.append('apiKey', 'npe+NsKvr8s0hIrTJqxzM7yfXEDXY3kwG+wc6T8JxNg=')
})
