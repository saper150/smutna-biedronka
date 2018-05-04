import * as React from 'react';
import { Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { DBPerf } from './components/Monitor';
import { Subdomains } from './components/Apps';
import { AppInfo } from './App/appComponent';

export const routes = <Layout>
    <Route exact path='/' component={ Home } />
    <Route path='/counter' component={ DBPerf } />
    <Route path='/Apps' component={Subdomains} />
    <Route path='/appDetails/:name' component={ AppInfo } />
</Layout>;
