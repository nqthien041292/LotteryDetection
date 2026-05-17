import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ValidatePasswordlessLoginComponent } from './validate-passwordless-login.component';

const routes: Routes = [
    {
        path: '',
        component: ValidatePasswordlessLoginComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ValidatePasswordlessLoginRoutingModule {}
