import { Component, Injector, OnInit } from '@angular/core';
import { ActivatedRoute, Params } from '@angular/router';
import { AppConsts } from '@shared/AppConsts';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { InvoiceDto, InvoiceServiceProxy } from '@shared/service-proxies/service-proxies';
import { NgFor, DecimalPipe } from '@angular/common';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './invoice.component.html',
    styleUrls: ['./invoice.component.less'],
    animations: [appModuleAnimation()],
    imports: [NgFor, DecimalPipe, LuxonFormatPipe, LocalizePipe],
})
export class InvoiceComponent extends AppComponentBase implements OnInit {
    paymentId = 0;
    invoiceInfo: InvoiceDto = new InvoiceDto();
    companyLogo = '';

    constructor(
        injector: Injector,
        private _invoiceServiceProxy: InvoiceServiceProxy,
        private activatedRoute: ActivatedRoute
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.getAllInfo();
        const skin = this.currentTheme.baseSettings.layout.darkMode ? 'dark' : 'light';
        this.companyLogo = AppConsts.appBaseUrl + '/assets/common/images/app-logo-on-' + skin + '.svg';
    }

    getAllInfo(): void {
        this.activatedRoute.params.subscribe((params: Params) => {
            this.paymentId = params['paymentId'];
        });

        this._invoiceServiceProxy.getInvoiceInfo(this.paymentId).subscribe((result) => {
            this.invoiceInfo = result;
        });
    }
}
