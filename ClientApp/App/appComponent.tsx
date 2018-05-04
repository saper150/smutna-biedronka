import * as React from "react";
import { RouteComponentProps } from "react-router-dom";
import { Component } from "react";
import { List } from "immutable";
import { format } from "date-fns";
import { ChartData, Chart } from "../components/Chart";


interface Log {
    _id: string
    time: string
    type: 'error' | 'message' | 'npm'
    message: string
}


export class AppInfo extends React.Component<RouteComponentProps<{ name: string }>, { logs: List<Log>, memUsage: List<ChartData> }> {

    socket = new WebSocket('ws://' + location.hostname + (location.port ? ':' + location.port : '') + '/ws/' + this.props.match.params.name)

    constructor(props) {
        super(props)
        this.state = {
            logs: List(),
            memUsage: List()
        };
        fetch(`api/Apps/Logs/${this.props.match.params.name}`)
            .then(response => response.json() as Promise<Log[]>)
            .then(logs => {
                this.setState({ logs: List(logs) });
            });
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

    render() {
        return (
            <div>
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
