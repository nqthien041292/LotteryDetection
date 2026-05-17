import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { EntityChangesComponent } from '@app/admin/entity-changes/entity-changes.component';

const routes: Routes = [
    {
        path: '',
        component: EntityChangesComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class EntityChangesRoutingModule {}
