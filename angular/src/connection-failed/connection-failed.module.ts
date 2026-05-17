import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { ConnectionFailedRoutingModule } from './connection-failed-routing-module';
import { ConnectionFailedComponent } from './connection-failed.component';

@NgModule({
    imports: [AppSharedModule, AdminSharedModule, ConnectionFailedRoutingModule, ConnectionFailedComponent],
})
export class ConnectionFailedModule {}
