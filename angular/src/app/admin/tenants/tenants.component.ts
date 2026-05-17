import { Component, Injector, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ImpersonationService } from '@app/admin/users/impersonation.service';
import { CommonLookupModalComponent } from '@app/shared/common/lookup/common-lookup-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CommonLookupServiceProxy,
    EntityDtoOfInt64,
    FindUsersInput,
    FindUsersOutputDto,
    TenantListDto,
    TenantServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { CreateTenantModalComponent } from './create-tenant-modal.component';
import { EditTenantModalComponent } from './edit-tenant-modal.component';
import { TenantFeaturesModalComponent } from './tenant-features-modal.component';
import { filter as _filter } from 'lodash-es';
import { finalize } from 'rxjs/operators';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EditionComboComponent } from '../shared/edition-combo.component';
import { BsDaterangepickerInputDirective, BsDaterangepickerDirective } from 'ngx-bootstrap/datepicker';
import { DateRangePickerLuxonModifierDirective } from '../../../shared/utils/date-time/date-range-picker-luxon-modifier.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { BsDropdownDirective, BsDropdownToggleDirective, BsDropdownMenuDirective } from 'ngx-bootstrap/dropdown';
import { CommonLookupModalComponent as CommonLookupModalComponent_1 } from '../../shared/common/lookup/common-lookup-modal.component';
import { EntityTypeHistoryModalComponent } from '../../shared/common/entityHistory/entity-type-history-modal.component';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    templateUrl: './tenants.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        NgIf,
        FormsModule,
        EditionComboComponent,
        BsDaterangepickerInputDirective,
        BsDaterangepickerDirective,
        DateRangePickerLuxonModifierDirective,
        BusyIfDirective,
        TableModule,
        BsDropdownDirective,
        BsDropdownToggleDirective,
        BsDropdownMenuDirective,
        PaginatorModule,
        CreateTenantModalComponent,
        EditTenantModalComponent,
        TenantFeaturesModalComponent,
        CommonLookupModalComponent_1,
        EntityTypeHistoryModalComponent,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class TenantsComponent extends AppComponentBase implements OnInit {
    @ViewChild('impersonateUserLookupModal', { static: true }) impersonateUserLookupModal: CommonLookupModalComponent;
    @ViewChild('createTenantModal', { static: true }) createTenantModal: CreateTenantModalComponent;
    @ViewChild('editTenantModal', { static: true }) editTenantModal: EditTenantModalComponent;
    @ViewChild('tenantFeaturesModal', { static: true }) tenantFeaturesModal: TenantFeaturesModalComponent;
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    subscriptionDateRange: DateTime[] = [
        this._dateTimeService.getStartOfDay(),
        this._dateTimeService.getEndOfDayPlusDays(30),
    ];
    creationDateRange: DateTime[] = [this._dateTimeService.getStartOfDay(), this._dateTimeService.getEndOfDay()];

    _entityTypeFullName = 'LotteryDetection.MultiTenancy.Tenant';
    entityHistoryEnabled = false;

    filters: {
        filterText: string;
        creationDateRangeActive: boolean;
        subscriptionEndDateRangeActive: boolean;
        selectedEditionId: number;
    } = <any>{};

    constructor(
        injector: Injector,
        private _tenantService: TenantServiceProxy,
        private _activatedRoute: ActivatedRoute,
        private _commonLookupService: CommonLookupServiceProxy,
        private _impersonationService: ImpersonationService,
        private _dateTimeService: DateTimeService,
        private _router: Router
    ) {
        super(injector);
        this.setFiltersFromRoute();
    }

    setFiltersFromRoute(): void {
        if (this._activatedRoute.snapshot.queryParams['subscriptionEndDateStart'] != null) {
            this.filters.subscriptionEndDateRangeActive = true;
            this.subscriptionDateRange[0] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['subscriptionEndDateStart']
            );
        } else {
            this.subscriptionDateRange[0] = this._dateTimeService.getStartOfDay();
        }

        if (this._activatedRoute.snapshot.queryParams['subscriptionEndDateEnd'] != null) {
            this.filters.subscriptionEndDateRangeActive = true;
            this.subscriptionDateRange[1] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['subscriptionEndDateEnd']
            );
        } else {
            this.subscriptionDateRange[1] = this._dateTimeService.getEndOfDayPlusDays(30);
        }

        if (this._activatedRoute.snapshot.queryParams['creationDateStart'] != null) {
            this.filters.creationDateRangeActive = true;
            this.creationDateRange[0] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['creationDateStart']
            );
        } else {
            this.creationDateRange[0] = this._dateTimeService.getEndOfDayMinusDays(7);
        }

        if (this._activatedRoute.snapshot.queryParams['creationDateEnd'] != null) {
            this.filters.creationDateRangeActive = true;
            this.creationDateRange[1] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['creationDateEnd']
            );
        } else {
            this.creationDateRange[1] = this._dateTimeService.getEndOfDay();
        }

        if (this._activatedRoute.snapshot.queryParams['editionId'] != null) {
            this.filters.selectedEditionId = parseInt(this._activatedRoute.snapshot.queryParams['editionId']);
        }
    }

    ngOnInit(): void {
        this.filters.filterText = this._activatedRoute.snapshot.queryParams['filterText'] || '';

        this.setIsEntityHistoryEnabled();

        this.impersonateUserLookupModal.configure({
            title: this.l('SelectAUser'),
            dataSource: (skipCount: number, maxResultCount: number, filter: string, tenantId?: number) => {
                let input = new FindUsersInput();
                input.filter = filter;
                input.maxResultCount = maxResultCount;
                input.skipCount = skipCount;
                input.tenantId = tenantId;
                return this._commonLookupService.findUsers(input);
            },
        });
    }

    getTenants(event?: LazyLoadEvent) {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);

            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._tenantService
            .getTenants(
                this.filters.filterText,
                this.filters.subscriptionEndDateRangeActive ? this.subscriptionDateRange[0] : undefined,
                this.filters.subscriptionEndDateRangeActive ? this.subscriptionDateRange[1].endOf('day') : undefined,
                this.filters.creationDateRangeActive ? this.creationDateRange[0] : undefined,
                this.filters.creationDateRangeActive ? this.creationDateRange[1].endOf('day') : undefined,
                this.filters.selectedEditionId,
                this.filters.selectedEditionId !== undefined && this.filters.selectedEditionId + '' !== '-1',
                this.primengTableHelper.getSorting(this.dataTable),
                this.primengTableHelper.getMaxResultCount(this.paginator, event),
                this.primengTableHelper.getSkipCount(this.paginator, event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount = result.totalCount;
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    showUserImpersonateLookUpModal(record: any): void {
        if (abp.multiTenancy.getTenantIdCookie()) {
            this.message.warn(this.l('YouAreNotLoggedInAsAHostUser')).then(() => {
                document.location.reload();
            });

            return;
        }

        this.impersonateUserLookupModal.tenantId = record.id;
        this.impersonateUserLookupModal.show();
    }

    unlockUser(record: any): void {
        this._tenantService.unlockTenantAdmin(new EntityDtoOfInt64({ id: record.id })).subscribe(() => {
            this.notify.success(this.l('UnlockedTenandAdmin', record.name));
        });
    }

    reloadPage(): void {
        this.paginator.changePage(this.paginator.getPage());
    }

    createTenant(): void {
        this.createTenantModal.show();
    }

    deleteTenant(tenant: TenantListDto): void {
        this.message.confirm(
            this.l('TenantDeleteWarningMessage', tenant.tenancyName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._tenantService.deleteTenant(tenant.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }

    showHistory(tenant: TenantListDto): void {
        this._router.navigate([abp.appPath + `/app/admin/entity-changes/${tenant.id}/${this._entityTypeFullName}`]);
    }

    impersonateUser(item: FindUsersOutputDto): void {
        this._impersonationService.impersonateTenant(item.id, this.impersonateUserLookupModal.tenantId);
    }

    private setIsEntityHistoryEnabled(): void {
        let customSettings = (abp as any).custom;
        this.entityHistoryEnabled =
            customSettings.EntityHistory &&
            customSettings.EntityHistory.isEnabled &&
            _filter(
                customSettings.EntityHistory.enabledEntities,
                (entityType) => entityType === this._entityTypeFullName
            ).length === 1;
    }
}
