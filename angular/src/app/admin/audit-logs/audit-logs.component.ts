import { Component, Injector, ViewChild, ViewEncapsulation, AfterViewInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuditLogDetailModalComponent } from '@app/admin/audit-logs/audit-log-detail-modal.component';
import { EntityChangeDetailModalComponent } from '@app/shared/common/entityHistory/entity-change-detail-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AuditLogListDto,
    AuditLogServiceProxy,
    EntityChangeListDto,
    NameValueDto,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { PrimengTableHelper } from 'shared/helpers/PrimengTableHelper';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { FormsModule } from '@angular/forms';
import { BsDaterangepickerInputDirective, BsDaterangepickerDirective } from 'ngx-bootstrap/datepicker';
import { DateRangePickerLuxonModifierDirective } from '../../../shared/utils/date-time/date-range-picker-luxon-modifier.directive';
import { NgIf, NgFor } from '@angular/common';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { BsDropdownDirective, BsDropdownToggleDirective, BsDropdownMenuDirective } from 'ngx-bootstrap/dropdown';
import { AuditLogDetailModalComponent as AuditLogDetailModalComponent_1 } from './audit-log-detail-modal.component';
import { EntityChangeDetailModalComponent as EntityChangeDetailModalComponent_1 } from '../../shared/common/entityHistory/entity-change-detail-modal.component';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { ButtonModule } from 'primeng/button';

@Component({
    templateUrl: './audit-logs.component.html',
    styleUrls: ['./audit-logs.component.less'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        FormsModule,
        BsDaterangepickerInputDirective,
        BsDaterangepickerDirective,
        DateRangePickerLuxonModifierDirective,
        NgIf,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        ButtonModule,
        NgFor,
        BsDropdownDirective,
        BsDropdownToggleDirective,
        BsDropdownMenuDirective,
        AuditLogDetailModalComponent_1,
        EntityChangeDetailModalComponent_1,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class AuditLogsComponent extends AppComponentBase implements AfterViewInit {
    @ViewChild('auditLogDetailModal', { static: true }) auditLogDetailModal: AuditLogDetailModalComponent;
    @ViewChild('entityChangeDetailModal', { static: true }) entityChangeDetailModal: EntityChangeDetailModalComponent;
    @ViewChild('dataTableAuditLogs', { static: true }) dataTableAuditLogs: Table;
    @ViewChild('dataTableEntityChanges', { static: true }) dataTableEntityChanges: Table;
    @ViewChild('paginatorAuditLogs', { static: true }) paginatorAuditLogs: Paginator;
    @ViewChild('paginatorEntityChanges', { static: true }) paginatorEntityChanges: Paginator;

    //Filters
    public dateRange: DateTime[] = [this._dateTimeService.getStartOfDay(), this._dateTimeService.getEndOfDay()];

    public usernameAuditLog: string;
    public usernameEntityChange: string;
    public serviceName: string;
    public methodName: string;
    public browserInfo: string;
    public hasException: boolean = undefined;
    public minExecutionDuration: number;
    public maxExecutionDuration: number;
    public entityTypeFullName: string;
    public objectTypes: NameValueDto[];

    primengTableHelperAuditLogs = new PrimengTableHelper();
    primengTableHelperEntityChanges = new PrimengTableHelper();
    advancedFiltersAreShown = false;

    constructor(
        injector: Injector,
        private _auditLogService: AuditLogServiceProxy,
        private _fileDownloadService: FileDownloadService,
        private _dateTimeService: DateTimeService,
        private _router: Router
    ) {
        super(injector);
    }

    ngAfterViewInit(): void {
        this.primengTableHelper.adjustScroll(this.dataTableAuditLogs);
        this.primengTableHelper.adjustScroll(this.dataTableEntityChanges);
    }

    showAuditLogDetails(record: AuditLogListDto): void {
        this.auditLogDetailModal.show(record);
    }

    showEntityChangeDetails(record: EntityChangeListDto): void {
        this.entityChangeDetailModal.show(record);
    }

    getAuditLogs(event?: LazyLoadEvent) {
        if (this.primengTableHelperAuditLogs.shouldResetPaging(event)) {
            this.paginatorAuditLogs.changePage(0);

            if (this.primengTableHelperAuditLogs.records && this.primengTableHelperAuditLogs.records.length > 0) {
                return;
            }
        }

        this.primengTableHelperAuditLogs.showLoadingIndicator();

        this._auditLogService
            .getAuditLogs(
                this._dateTimeService.getStartOfDayForDate(this.dateRange[0]),
                this._dateTimeService.getEndOfDayForDate(this.dateRange[1]),
                this.usernameAuditLog,
                this.serviceName,
                this.methodName,
                this.browserInfo,
                this.hasException,
                this.minExecutionDuration,
                this.maxExecutionDuration,
                this.primengTableHelperAuditLogs.getSorting(this.dataTableAuditLogs),
                this.primengTableHelperAuditLogs.getMaxResultCount(this.paginatorAuditLogs, event),
                this.primengTableHelperAuditLogs.getSkipCount(this.paginatorAuditLogs, event)
            )
            .subscribe((result) => {
                this.primengTableHelperAuditLogs.totalRecordsCount = result.totalCount;
                this.primengTableHelperAuditLogs.records = result.items;
                this.primengTableHelperAuditLogs.hideLoadingIndicator();
            });
    }

    getEntityChanges(event?: LazyLoadEvent) {
        if (!this.objectTypes) {
            this._auditLogService.getEntityHistoryObjectTypes().subscribe((result) => {
                this.objectTypes = result;
            });
        }

        if (this.primengTableHelperEntityChanges.shouldResetPaging(event)) {
            this.paginatorEntityChanges.changePage(0);

            if (
                this.primengTableHelperEntityChanges.records &&
                this.primengTableHelperEntityChanges.records.length > 0
            ) {
                return;
            }
        }

        this.primengTableHelperEntityChanges.showLoadingIndicator();
        this._auditLogService
            .getEntityChanges(
                this._dateTimeService.getStartOfDayForDate(this.dateRange[0]),
                this._dateTimeService.getEndOfDayForDate(this.dateRange[1]),
                this.usernameEntityChange,
                this.entityTypeFullName,
                this.primengTableHelperEntityChanges.getSorting(this.dataTableEntityChanges),
                this.primengTableHelperEntityChanges.getMaxResultCount(this.paginatorEntityChanges, event),
                this.primengTableHelperEntityChanges.getSkipCount(this.paginatorEntityChanges, event)
            )
            .subscribe((result) => {
                this.primengTableHelperEntityChanges.totalRecordsCount = result.totalCount;
                this.primengTableHelperEntityChanges.records = result.items;
                this.primengTableHelperEntityChanges.hideLoadingIndicator();
            });
    }

    showEntityChangeAllDetails(record: EntityChangeListDto): void {
        this._router.navigate([
            abp.appPath + `/app/admin/entity-changes/${record.entityId}/${record.entityTypeFullName}`,
        ]);
    }

    exportToExcelAuditLogs(): void {
        const self = this;
        self._auditLogService
            .getAuditLogsToExcel(
                this._dateTimeService.getStartOfDayForDate(self.dateRange[0]),
                this._dateTimeService.getEndOfDayForDate(self.dateRange[1]),
                self.usernameAuditLog,
                self.serviceName,
                self.methodName,
                self.browserInfo,
                self.hasException,
                self.minExecutionDuration,
                self.maxExecutionDuration,
                undefined,
                1,
                0
            )
            .subscribe((result) => {
                self._fileDownloadService.downloadTempFile(result);
            });
    }

    exportToExcelEntityChanges(): void {
        const self = this;
        self._auditLogService
            .getEntityChangesToExcel(
                this._dateTimeService.getStartOfDayForDate(self.dateRange[0]),
                self._dateTimeService.getEndOfDayForDate(self.dateRange[1]),
                self.usernameEntityChange,
                self.entityTypeFullName,
                undefined,
                1,
                0
            )
            .subscribe((result) => {
                self._fileDownloadService.downloadTempFile(result);
            });
    }

    truncateStringWithPostfix(text: string, length: number): string {
        return abp.utils.truncateStringWithPostfix(text, length);
    }
}
