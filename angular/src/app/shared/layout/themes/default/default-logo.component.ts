import { Injector, Component, ViewEncapsulation, Input, OnInit } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgIf } from '@angular/common';

@Component({
    templateUrl: './default-logo.component.html',
    selector: 'default-logo',
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf],
})
export class DefaultLogoComponent extends AppComponentBase implements OnInit {
    @Input() customHrefClass = '';

    defaultLogoUrl: string;
    defaultSmallLogoUrl: string;
    tenantLogoUrl: string;
    tenantSmallLogoUrl: string;

    constructor(
        injector: Injector,
        private _logoService: LogoService
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.setLogoUrls();
    }

    private setLogoUrls(): void {
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl();
        this.defaultSmallLogoUrl = this._logoService.getDefaultLogoUrl(null, true);

        this.tenantLogoUrl = this._logoService.getLogoUrl();
        this.tenantSmallLogoUrl = this._logoService.getLogoUrl(null, true);
    }
}
