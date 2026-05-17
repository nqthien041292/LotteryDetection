import { Injector, Component, ViewEncapsulation, Input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgIf } from '@angular/common';

@Component({
    templateUrl: './theme7-brand.component.html',
    selector: 'theme7-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf],
})
export class Theme7BrandComponent extends AppComponentBase {
    @Input() imageClass = 'h-35px';

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
