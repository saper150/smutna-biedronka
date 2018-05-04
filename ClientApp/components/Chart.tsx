import * as React from "react";
import { List } from "immutable";
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from "recharts";

export type ChartData = { name: string, [key: string]: string, }

export class Chart extends React.Component<{ dataKey: string, data: List<ChartData> }> {
    public render() {
        return <div>
            <p>{this.props.dataKey}</p>
            <LineChart width={600} height={300} data={this.props.data.toArray()}
                margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <XAxis dataKey="name" />
                <YAxis />
                <CartesianGrid strokeDasharray="3 3" />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey={this.props.dataKey} stroke="#8884d8" />
            </LineChart>
        </div>
    }
}
