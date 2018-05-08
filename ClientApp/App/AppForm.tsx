import * as React from "react"

import * as TagsInput from 'react-tagsinput'
import 'react-tagsinput/react-tagsinput.css'

export interface AppCreateInfo {
    name: string,
    port: string,
    serverNames: string[]
}

export class AppForm extends React.Component<{
    onChange: (AppCreateInfo) => void,
    onSubmit: () => void,
    app: AppCreateInfo,
    buttonText: string
    nameDisabled?: boolean
}> {
    static get defaultProps() {
        return {
            nameDisabled: false
        }
    }

    constructor(props) {
        super(props)

    }

    onSubmit = (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault()
        this.props.onSubmit()
    }

    handleInputChange = event => {
        const target = event.target
        const value = target.type === 'checkbox' ? target.checked : target.value
        const name = target.name

        this.props.onChange({ ...this.props.app, [name]: value })
    }
    handleTagsChange = serverNames => {
        this.props.onChange({ ...this.props.app, serverNames: serverNames });
    }

    render() {
        return (
            <form onSubmit={this.onSubmit}>
                <div className="form-group">
                    <label>
                        Name
                        <input
                            disabled={this.props.nameDisabled}
                            minLength={2}
                            required
                            className="form-control"
                            type="text"
                            name="name"
                            value={this.props.app.name}
                            onChange={this.handleInputChange} />
                    </label>
                </div>
                <div>
                    <label>
                        Port
                        <input min={0} type="number" required className="form-control" name="port" value={this.props.app.port}
                            onChange={this.handleInputChange} />
                    </label>
                </div>
                <div className="form-group">
                    <TagsInput
                        inputProps={{ placeholder: 'server names' }}
                        onlyUnique={true}
                        value={this.props.app.serverNames}
                        onChange={this.handleTagsChange} />
                </div>
                <div className="form-group">
                    <button className="btn btn-primary" type="submit">{this.props.buttonText}</button>
                </div>
            </form>
        );
    }
}
