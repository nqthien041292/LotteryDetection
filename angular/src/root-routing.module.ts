import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
    { path: '', redirectTo: '/app/main/dashboard', pathMatch: 'full' },
    {
        path: 'account',
        loadChildren: () => import('account/account.module').then((m) => m.AccountModule), //Lazy load account module
        data: { preload: true },
    },
    {
        path: 'could-not-connect-server',
        loadChildren: () => import('connection-failed/connection-failed.module').then((m) => m.ConnectionFailedModule),
    },
    { path: '**', redirectTo: '/app/main/dashboard' },
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule],
})
export class RootRoutingModule {}
