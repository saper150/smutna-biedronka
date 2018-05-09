import * as React from "react";
import * as TagsInput from 'react-tagsinput'
import 'react-tagsinput/react-tagsinput.css'


export class AddStaticContent extends React.Component<{ staticContendAdded: (StaticContentInfo) => void },
    { name: string, serverNames: string[] }> {

    constructor(props) {
        super(props)
        this.state = {
            name: '',
            serverNames: []
        };
    }

    handleInputChange = (event) => {
        const target = event.target;
        const value = target.type === 'checkbox' ? target.checked : target.value;
        const name = target.name;

        this.setState({
            [name]: value
        });
    }

    handleTagsChange = serverNames => {
        this.setState({
            serverNames: [...serverNames]
        })
    }

    submit(event) {
        event.preventDefault();
        fetch('/api/StaticContent/Create', {
            method: 'post',
            body: JSON.stringify(this.state),
            headers: {
                'Content-Type': 'application/json'
            }
        })
            .then(x => x.json())
            .then(response => {
                this.setState({ name: '', serverNames: [] })
                this.props.staticContendAdded(response)
            })
    }

    render() {
        return (
            <form onSubmit={this.submit.bind(this)}>
                <div className="form-group">
                    <label>
                        Name
                        <input className="form-control" type="text" name="name"
                            required value={this.state.name} onChange={this.handleInputChange} />
                    </label>
                </div>
                <div className="form-group">
                    <TagsInput
                        inputProps={{ placeholder: 'server names' }}
                        onlyUnique={true}
                        value={this.state.serverNames}
                        onChange={this.handleTagsChange} />
                </div>
                <button className="btn btn-primary" type="submit">Add</button>
            </form>
        )
    }


}