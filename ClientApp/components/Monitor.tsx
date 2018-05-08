import * as React from 'react';
import { RouteComponentProps } from 'react-router';



import { List } from "immutable"

import { format } from "date-fns"
import { ChartData, Chart } from './Chart';


interface PerformanceModel {
    _id: string
    Time: string
    Inserted: number
    Returned: number
    Updated: number
    Deleted: number
    MemUsage: number
}

export class DBPerf extends React.Component<RouteComponentProps<{}>, { [key: string]: List<ChartData> }> {
    socket = new WebSocket('ws://' + location.hostname + (location.port ? ':' + location.port : '') + '/ws/_mongoPerformance')
    constructor(props) {
        super(props);
        const metrics = ['Inserted', 'Returned', 'Updated', 'Deleted', 'MemUsage']
        this.state = metrics.reduce((acc, c) => ({ ...acc, [c]: List() }), {})

        this.socket.onmessage = (message) => {
            const response: PerformanceModel | PerformanceModel[] = JSON.parse(message.data)
            if (Array.isArray(response)) {
                this.setState(metrics.reduce((acc, c) => ({
                    ...acc,
                    [c]: this.state[c].concat(response.map(x => ({ name: format(x.Time, 'HH:mm'), [c]: x[c] })))
                }), {}))
            } else {
                this.setState(metrics.reduce((acc, c) => ({
                    ...acc,
                    [c]: this.state[c].push({ name: format(new Date().toUTCString(), 'HH:mm'), [c]: response[c] })
                }), {}))
            }
        }
    }

    componentWillUnmount() {
        this.socket.close();
    }

    public render() {
        return (
            <div>
                <ServerStatus socket={this.socket} />
                <MongoOnlineStatus />
                <div style={{ display: 'flex', flexWrap: 'wrap' }}>
                    {Object.keys(this.state)
                        .map(key => <Chart key={key} data={this.state[key]} dataKey={key} />)}
                </div>
            </div>
        );
    }
}



class MongoOnlineStatus extends React.Component<{}, { isOnline: boolean }>{
    socket = new WebSocket('ws://' + location.hostname + (location.port ? ':' + location.port : '') + '/ws/_mongoStatus')
    constructor(props) {
        super(props);
        this.state = { isOnline: true }
        this.socket.onmessage = message => {
            this.setState({
                isOnline: JSON.parse(message.data).isOnline
            })
        }
    }

    componentWillUnmount() {
        this.socket.close();
    }

    render() {
        return (
            <div>
                <p style={{ fontSize: '30px' }}>Mongo status:
                <span className={this.state.isOnline ? 'text-success' : 'text-danger'}>
                        {this.state.isOnline ? 'ONLINE' : 'OFFLINE'}
                    </span>
                </p>
            </div>
        )
    }
}


class ServerStatus extends React.Component<{ socket: WebSocket }, { isOnline: boolean }> {
    constructor(props) {
        super(props);
        this.state = { isOnline: false }
        this.props.socket.onopen = () => {
            this.setState({ isOnline: true })
        }
        this.props.socket.onclose = () => {
            this.setState({ isOnline: false })
        }
        this.props.socket.onerror = console.log
    }
    render() {
        return (
            <div>
                <p style={{ fontSize: '30px' }}>Server Status:
                <span className={this.state.isOnline ? 'text-success' : 'text-danger'}>
                        {this.state.isOnline ? 'ONLINE' : 'OFFLINE'}
                    </span>
                </p>
            </div>
        )
    }
}