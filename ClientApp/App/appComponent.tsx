import * as React from "react";
import { RouteComponentProps } from "react-router-dom";
import { Component } from "react";
import { List } from "immutable";
import { format } from "date-fns";
import { ChartData, Chart } from "../components/Chart";
import { AppForm } from "./appForm";


interface Log {
    _id: string
    time: string
    type: 'error' | 'message' | 'npm'
    message: string
}

interface AppInfo {
    name: string
    apiKey: string
    serverNames: string[]
    port: number
}


export class AppDetails extends React.Component<RouteComponentProps<{ apiKey: string }>,
    { logs: List<Log>, memUsage: List<ChartData>, app: AppInfo }> {

    socket: WebSocket

    constructor(props) {
        super(props)
        this.state = {
            logs: List(),
            memUsage: List(),
            app: { name: '', apiKey: '', serverNames: [], port: 0 }
        };

        fetch(`api/Apps/Get/${this.props.match.params.apiKey}`)
            .then(x => x.json())
            .then(app => {
                console.log(app)
                this.setState({ app: app })
                this.setUpSocket(app);
                return fetch(`api/Apps/Logs/${app.name}`)
            }).then(response => response.json() as Promise<Log[]>)
            .then(logs => {
                this.setState({ logs: List(logs) });
            });


    }

    setUpSocket(app: AppInfo) {
        this.socket = new WebSocket('ws://'
            + location.hostname + (location.port ? ':' + location.port : '')
            + '/ws/' + app.name
        )
        this.socket.onclose = console.log
        this.socket.onopen = console.log
        this.socket.onerror = console.log
        this.socket.onmessage = msg => {
            const message = JSON.parse(msg.data)
            if (Array.isArray(message)) {
                this.setState({
                    memUsage: List(message.map(x => ({
                        name: format(x.Time, 'HH:mm'),
                        'mem usage': x.MemUsage
                    })))
                })
            } else {
                this.setState({
                    memUsage: this.state.memUsage.push({
                        name: format(new Date().toUTCString(), 'HH:mm'),
                        'mem usage': message.MemUsage
                    })
                })
            }
        }
    }

    onFormChange = app => {
        this.setState({
            app: { ...this.state.app, ...app }
        })
    }

    onSubmit = () => {
        fetch(`api/Apps/Update/${this.state.app.apiKey}`, {
            method: 'put',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(this.state.app)
        }).then(x => x.json())
    }

    render() {
        return (
            <div>
                <AppForm buttonText={"Update"} app={this.state.app as any} onChange={this.onFormChange} onSubmit={this.onSubmit} nameDisabled={true} />
                <Chart dataKey="mem usage" data={this.state.memUsage} />
                <Logs logs={this.state.logs} />
            </div>
        )
    }
}

class Logs extends Component<{ logs: List<Log> }> {
    render() {
        return (
            <div>
                {this.props.logs.map(log =>
                    <div key={log._id}>{`[${format(log.time, "YYYY-MM-DD HH-mm")}] ${log.message}`}</div>
                )}
            </div>
        )
    }
}
