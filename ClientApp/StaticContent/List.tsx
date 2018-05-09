import * as React from "react";
import { RouteComponentProps } from "react-router-dom";
import { Component } from "react";
import { List } from "immutable";
import { format } from "date-fns";
import { ChartData, Chart } from "../components/Chart";
import { AddStaticContent } from "./Add";


interface StaticContentInfo {
    name: string
    apiKey: string
    serverNames: string[]
}


export class StaticContentList extends React.Component<RouteComponentProps<{}>, { contents: List<StaticContentInfo> }> {

    constructor(props) {
        super(props)
        this.state = {
            contents: List()
        };

        fetch(`api/StaticContent/Get`)
            .then(x => x.json())
            .then(app => {
                this.setState({ contents: List(app) })
            })
    }

    onFormChange = app => {
        this.setState({
            contents: { ...this.state.contents, ...app }
        })
    }

    delete = (apiKey: string) => {
        fetch(`api/StaticContent/Delete/${apiKey}`, {
            method: 'delete'
        }).then(x => x.json())
            .then(() => {
                this.setState({
                    contents: this.state.contents.filter(x => x.apiKey !== apiKey).toList()
                })
            })
    }

    contentAdded = (info: StaticContentInfo) => {
        console.log('added')
        console.log(info)
        this.setState({
            contents: this.state.contents.push(info)
        })
    }

    render() {
        return (
            <div>
                <AddStaticContent staticContendAdded={this.contentAdded} />
                <table className="table">
                    <thead>
                        <tr>
                            <th className="clickable-row">Name</th>
                            <th>ApiKey</th>
                            <th>server names</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {this.state.contents.map(x =>
                            <tr>
                                <td>{x.name}</td>
                                <td>{x.apiKey}</td>
                                <td>{x.serverNames.join(', ')}</td>
                                <td><a onClick={this.delete.bind(this, x.apiKey)}>Delete</a></td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        )
    }
}
