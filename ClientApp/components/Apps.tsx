import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import 'isomorphic-fetch';
import { Link } from 'react-router-dom';


export class Subdomains extends React.Component<RouteComponentProps<{}>, { apps: any[], newApp: any }> {
    constructor() {
        super();
        this.state = { apps: [], newApp: { name: '', port: '' } };
        fetch('api/Apps/Get')
            .then(response => response.json())
            .then(apps => {
                this.setState({ apps });
            });
        this.handleInputChange = this.handleInputChange.bind(this);
    }

    handleInputChange(event) {
        const target = event.target;
        const value = target.type === 'checkbox' ? target.checked : target.value;
        const name = target.name;

        this.setState({
            newApp: { ...this.state.newApp, [name]: value }
        });
    }

    addSubdomain(event: Event) {
        event.preventDefault()
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

    render() {
        return (
            <div>
                {this.addForm()}
                {this.list()}
            </div>
        )
    }
    private addForm() {
        return (
            <form onSubmit={this.addSubdomain.bind(this)}>
                <div className="form-group">
                    <label >
                        Name
                        <input
                            minLength={2}
                            required
                            className="form-control"
                            type="text"
                            name="name"
                            value={this.state.newApp.name}
                            onChange={this.handleInputChange} />
                    </label>
                </div>
                <div>
                    <label>
                        Port
                        <input min={0} type="number" required className="form-control" name="port" value={this.state.newApp.port}
                            onChange={this.handleInputChange} />
                    </label>
                </div>
                <div className="form-group">
                    <button className="btn btn-primary" type="submit"> Add new</button>
                </div>
            </form>
        );
    }

    navigate = (name: string) => {
        this.props.history.push(`/appDetails/${name}`)
    }

    private list() {
        return (
            <table className="table">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Port</th>
                        <th>Key</th>
                    </tr>
                </thead>
                <tbody>
                    {this.state.apps.map(app =>
                        <tr className="clickable-row" key={app._id} onClick={this.navigate.bind(null, app.name)}>
                            <td>{app.name}</td>
                            <td>{app.port}</td>
                            <td>{app.apiKey}</td>
                        </tr>
                    )}
                </tbody>
            </table>
        )
    }
}
