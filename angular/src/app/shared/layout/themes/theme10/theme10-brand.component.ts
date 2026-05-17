import { Injector, Component, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgIf } from '@angular/common';

@Component({
    templateUrl: './theme10-brand.component.html',
    selector: 'theme10-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf],
})
export class Theme10BrandComponent extends AppComponentBase {
    tenantLogoUrl: string;
    defaultLogoUrl: string;

    constructor(
        injector: Injector,
        private _logoService: LogoService
    ) {
        super(injector);

        this.tenantLogoUrl = this._logoService.getLogoUrl();
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl();
    }
}
