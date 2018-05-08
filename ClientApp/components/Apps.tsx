import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import 'isomorphic-fetch';
import { Link } from 'react-router-dom';

import * as TagsInput from 'react-tagsinput'
import 'react-tagsinput/react-tagsinput.css'
import { AppForm, AppCreateInfo } from '../App/AppForm';

export class Subdomains extends React.Component<RouteComponentProps<{}>, { apps: any[], newApp: AppCreateInfo }> {
    formRef: React.RefObject<AppForm> = React.createRef()
    constructor(props) {
        super(props);
        this.state = {
            apps: [],
            newApp: {
                name: '',
                port: '',
                serverNames: []
            }
        };
        fetch('api/Apps/Get')
            .then(response => response.json())
            .then(apps => {
                this.setState({ apps });
            });
    }

    createApp = () => {
        fetch("api/Apps/Create", {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(this.state.newApp)
        }).then(x => x.json())
            .then(x => {
                this.setState({ apps: this.state.apps.concat([x]) })
            })
    }

    appChange = newApp => {
        this.setState({ newApp: { ...newApp } })
    }

    render() {
        return (
            <div>
                <AppForm buttonText={'add'} app={this.state.newApp} onChange={this.appChange} onSubmit={this.createApp} />
                {this.list()}
            </div>
        )
    }

    navigate = (apiKey: string) => {
        this.props.history.push(`/appDetails/${apiKey}`)
    }

    delete = (apiKey: string) => {
        fetch(`api/Apps/Delete/${apiKey}`, {
            method: 'delete'
        })
            .then(x => x.json())
            .then(x => {
                this.setState({
                    apps: this.state.apps.filter(x => x.apiKey !== apiKey)
                })
            })
    }

    private list() {
        return (
            <table className="table">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Port</th>
                        <th>Key</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    {this.state.apps.map(app =>
                        <tr key={app._id}>
                            <td className="clickable-row" onClick={this.navigate.bind(null, app.apiKey)}>{app.name}</td>
                            <td>{app.port}</td>
                            <td>{app.apiKey}</td>
                            <td>
                                <a onClick={this.delete.bind(this, app.apiKey)}>Delete</a>
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        )
    }
}
