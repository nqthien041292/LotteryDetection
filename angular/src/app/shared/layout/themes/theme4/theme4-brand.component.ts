import { Injector, Component, ViewEncapsulation, Input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgIf } from '@angular/common';

@Component({
    templateUrl: './theme4-brand.component.html',
    selector: 'theme4-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf],
})
export class Theme4BrandComponent extends AppComponentBase {
    @Input() customStyle = 'h-55px';

    defaultSmallLogoUrl: string;
    tenantSmallLogoUrl: string;

    constructor(
        injector: Injector,
        private _logoService: LogoService
    ) {
        super(injector);
        this.defaultSmallLogoUrl = this._logoService.getDefaultLogoUrl(null, true);
        this.tenantSmallLogoUrl = this._logoService.getLogoUrl(null, true);
    }
}
