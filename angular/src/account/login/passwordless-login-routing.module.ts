import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PasswordlessLoginComponent } from './passwordless-login.component';

const routes: Routes = [
    {
        path: '',
        component: PasswordlessLoginComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class PasswordlessLoginRoutingModule {}
