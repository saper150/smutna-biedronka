import * as React from 'react';
import { Route } from 'react-router-dom';
import { AppDetails } from './App/appComponent';
import { StaticContentList } from './StaticContent/List';
import { Subdomains } from './components/Apps';
import { Home } from './components/Home';
import { Layout } from './components/Layout';
import { DBPerf } from './components/Monitor';

export const routes = <Layout>
    <Route exact path='/' component={Home} />
    <Route path='/counter' component={DBPerf} />
    <Route path='/apps' component={Subdomains} />
    <Route path='/staticContent' component={StaticContentList} />
    <Route path='/appDetails/:apiKey' component={AppDetails} />
</Layout>;

