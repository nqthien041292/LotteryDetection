import { Component, Injector, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationInfoDto,
    InvoiceServiceProxy,
    PaymentServiceProxy,
    SessionServiceProxy,
    SubscriptionServiceProxy,
    TenantLoginInfoDto,
    UserLoginInfoDto,
    CreateInvoiceDto,
    SubscriptionStartType,
    SubscriptionPaymentType,
    TenantRegistrationServiceProxy,
    EditionWithFeaturesDto,
    StartExtendSubscriptionInput,
    StartUpgradeSubscriptionInput,
    PaymentPeriodType,
    StartTrialToBuySubscriptionInput,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { FormsModule } from '@angular/forms';
import { NgIf, NgFor, NgClass, DecimalPipe } from '@angular/common';
import { BsDropdownDirective, BsDropdownToggleDirective, BsDropdownMenuDirective } from 'ngx-bootstrap/dropdown';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { ShowDetailModalComponent } from './show-detail-modal.component';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    templateUrl: './subscription-management.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        FormsModule,
        NgIf,
        NgFor,
        NgClass,
        BsDropdownDirective,
        BsDropdownToggleDirective,
        BsDropdownMenuDirective,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        ShowDetailModalComponent,
        DecimalPipe,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class SubscriptionManagementComponent extends AppComponentBase implements OnInit {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    subscriptionStartType: typeof SubscriptionStartType = SubscriptionStartType;
    subscriptionPaymentType: typeof SubscriptionPaymentType = SubscriptionPaymentType;

    loading: boolean;
    user: UserLoginInfoDto = new UserLoginInfoDto();
    tenant: TenantLoginInfoDto = new TenantLoginInfoDto();
    application: ApplicationInfoDto = new ApplicationInfoDto();

    filterText = '';
    editions: EditionWithFeaturesDto[];

    constructor(
        injector: Injector,
        private _sessionService: SessionServiceProxy,
        private _paymentServiceProxy: PaymentServiceProxy,
        private _invoiceServiceProxy: InvoiceServiceProxy,
        private _subscriptionServiceProxy: SubscriptionServiceProxy,
        private _tenantRegistrationAppService: TenantRegistrationServiceProxy,
        private _activatedRoute: ActivatedRoute,
        private _router: Router
    ) {
        super(injector);
        this.filterText = this._activatedRoute.snapshot.queryParams['filterText'] || '';
    }

    ngOnInit(): void {
        this.getSettings();
    }

    createOrShowInvoice(paymentId: number, invoiceNo: string): void {
        if (invoiceNo) {
            window.open('/app/admin/invoice/' + paymentId, '_blank');
        } else {
            this._invoiceServiceProxy
                .createInvoice(new CreateInvoiceDto({ subscriptionPaymentId: paymentId }))
                .subscribe(() => {
                    this.getPaymentHistory();
                    window.open('/app/admin/invoice/' + paymentId, '_blank');
                });
        }
    }

    getSettings(): void {
        this.loading = true;
        this.appSession.init().then(() => {
            this.loading = false;
            this.user = this.appSession.user;
            this.tenant = this.appSession.tenant;
            this.application = this.appSession.application;
        });

        this._tenantRegistrationAppService.getEditionsForSelect().subscribe((result) => {
            this.editions = result.editionsWithFeatures;
        });
    }

    getPaymentHistory(event?: LazyLoadEvent) {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);

            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._paymentServiceProxy
            .getPaymentHistory(
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

    disableRecurringPayments() {
        this._subscriptionServiceProxy.disableRecurringPayments().subscribe((result) => {
            this.tenant.subscriptionPaymentType = this.subscriptionPaymentType.RecurringManual;
        });
    }

    enableRecurringPayments() {
        this._subscriptionServiceProxy.enableRecurringPayments().subscribe((result) => {
            this.tenant.subscriptionPaymentType = this.subscriptionPaymentType.RecurringAutomatic;
        });
    }

    hasRecurringSubscription(): boolean {
        return this.tenant.subscriptionPaymentType !== this.subscriptionPaymentType.Manual;
    }

    startUpdateSubscription(editionId: number, paymentPeriodType?: string): void {
        let input = new StartUpgradeSubscriptionInput();
        input.targetEditionId = editionId;
        input.paymentPeriodType = PaymentPeriodType[paymentPeriodType];
        input.successUrl = abp.appPath + 'account/upgrade-succeed';
        input.errorUrl = abp.appPath + 'account/payment-failed';
        this._subscriptionServiceProxy.startUpgradeSubscription(input).subscribe((result) => {
            if (result.upgraded) {
                this.message.success(this.l('YourAccountIsUpgraded'));
            } else {
                this._router.navigate(['account/gateway-selection'], {
                    queryParams: {
                        paymentId: result.paymentId,
                    },
                });
            }
        });
    }

    startExtendSubscription(): void {
        let input = new StartExtendSubscriptionInput();
        input.successUrl = abp.appPath + 'account/extend-succeed';
        input.errorUrl = abp.appPath + 'account/payment-failed';
        this._subscriptionServiceProxy.startExtendSubscription(input).subscribe((paymentId) => {
            this._router.navigate(['account/gateway-selection'], {
                queryParams: {
                    paymentId: paymentId,
                },
            });
        });
    }

    startBuySubscription(paymentPeriodType?: string): void {
        let input = new StartTrialToBuySubscriptionInput();
        input.paymentPeriodType = PaymentPeriodType[paymentPeriodType];
        input.successUrl = abp.appPath + 'account/buy-succeed';
        input.errorUrl = abp.appPath + 'account/payment-failed';

        this._subscriptionServiceProxy.startTrialToBuySubscription(input).subscribe((paymentId) => {
            this._router.navigate(['account/gateway-selection'], {
                queryParams: {
                    paymentId: paymentId,
                },
            });
        });
    }
}
