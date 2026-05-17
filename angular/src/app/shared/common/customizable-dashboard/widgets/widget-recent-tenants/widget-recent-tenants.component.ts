import { Component, ViewChild, Injector } from '@angular/core';
import { Table, TableModule } from 'primeng/table';
import { HostDashboardServiceProxy, GetRecentTenantsOutput } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { NgIf } from '@angular/common';
import { ScrollViewport, NgScrollbarExt, NgScrollbarModule } from 'ngx-scrollbar';
import { BusyIfDirective } from '../../../../../../shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '../../../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'app-widget-recent-tenants',
    templateUrl: './widget-recent-tenants.component.html',
    styleUrls: ['./widget-recent-tenants.component.css'],
    imports: [NgIf, ScrollViewport, BusyIfDirective, NgScrollbarModule, TableModule, LuxonFormatPipe, LocalizePipe],
})
export class WidgetRecentTenantsComponent extends WidgetComponentBaseComponent {
    @ViewChild('RecentTenantsTable', { static: true }) recentTenantsTable: Table;

    loading = true;
    recentTenantsData: GetRecentTenantsOutput;

    constructor(
        injector: Injector,
        private _hostDashboardServiceProxy: HostDashboardServiceProxy
    ) {
        super(injector);
        this.loadRecentTenantsData();
    }

    loadRecentTenantsData() {
        this._hostDashboardServiceProxy.getRecentTenantsData().subscribe((data) => {
            this.recentTenantsData = data;
            this.loading = false;
        });
    }

    gotoAllRecentTenants(): void {
        window.open(
            abp.appPath +
                'app/admin/tenants?' +
                'creationDateStart=' +
                encodeURIComponent(this.recentTenantsData.tenantCreationStartDate.toString())
        );
    }
}
