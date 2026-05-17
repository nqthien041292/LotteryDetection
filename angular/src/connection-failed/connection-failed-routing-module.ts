import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ConnectionFailedComponent } from './connection-failed.component';

const routes: Routes = [
    {
        path: '',
        component: ConnectionFailedComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ConnectionFailedRoutingModule {}
