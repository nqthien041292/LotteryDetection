import { Injector, Component, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgIf } from '@angular/common';

@Component({
    templateUrl: './theme11-brand.component.html',
    selector: 'theme11-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf],
})
export class Theme11BrandComponent extends AppComponentBase {
    defaultLogoUrl: string;
    tenantLogoUrl: string;

    constructor(
        injector: Injector,
        private _logoService: LogoService
    ) {
        super(injector);
        this.tenantLogoUrl = this._logoService.getLogoUrl();
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl();
    }
}
