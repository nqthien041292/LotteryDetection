import { Injector, Component, ViewEncapsulation, Input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgIf } from '@angular/common';

@Component({
    templateUrl: './theme6-brand.component.html',
    selector: 'theme6-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf],
})
export class Theme6BrandComponent extends AppComponentBase {
    @Input() anchorClass = 'd-flex align-items-center';
    @Input() imageClass = 'h-45px logo';

    defaultLogoUrl: string;
    tenantLogoUrl: string;

    constructor(
        injector: Injector,
        private _logoService: LogoService
    ) {
        super(injector);
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl();
        this.tenantLogoUrl = this._logoService.getLogoUrl();
    }
}
