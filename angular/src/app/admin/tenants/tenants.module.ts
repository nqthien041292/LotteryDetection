import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { TenantsRoutingModule } from './tenants-routing.module';
import { CreateTenantModalComponent } from './create-tenant-modal.component';
import { EditTenantModalComponent } from './edit-tenant-modal.component';
import { TenantFeaturesModalComponent } from './tenant-features-modal.component';
import { TenantsComponent } from './tenants.component';

@NgModule({
    imports: [
        AppSharedModule,
        AdminSharedModule,
        TenantsRoutingModule,
        CreateTenantModalComponent,
        EditTenantModalComponent,
        TenantFeaturesModalComponent,
        TenantsComponent,
    ],
})
export class TenantsModule {}
