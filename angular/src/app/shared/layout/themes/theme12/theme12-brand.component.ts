import { Injector, Component, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgIf } from '@angular/common';

@Component({
    templateUrl: './theme12-brand.component.html',
    selector: 'theme12-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf],
})
export class Theme12BrandComponent extends AppComponentBase {
    defaultLogoUrl: string;
    defaultSmallLogoUrl: string;
    tenantLogoUrl: string;
    tenantSmallLogoUrl: string;

    constructor(
        injector: Injector,
        private _logoService: LogoService
    ) {
        super(injector);
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl();
        this.defaultSmallLogoUrl = this._logoService.getDefaultLogoUrl(null, true);
        this.tenantLogoUrl = this._logoService.getLogoUrl();
        this.tenantSmallLogoUrl = this._logoService.getLogoUrl(null, true);
    }
}
