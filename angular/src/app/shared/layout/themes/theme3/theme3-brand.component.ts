import { Injector, Component, ViewEncapsulation, Inject, Input } from '@angular/core';

import { AppComponentBase } from '@shared/common/app-component-base';

import { DOCUMENT, NgIf } from '@angular/common';
import { LogoService } from '@app/shared/common/logo/logo.service';

@Component({
    templateUrl: './theme3-brand.component.html',
    selector: 'theme3-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf],
})
export class Theme3BrandComponent extends AppComponentBase {
    @Input() logoSize = '';

    defaultLogoUrl: string;
    tenantLogoUrl: string;

    constructor(
        injector: Injector,
        @Inject(DOCUMENT) private document: Document,
        private _logoService: LogoService
    ) {
        super(injector);
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl();
        this.tenantLogoUrl = this._logoService.getLogoUrl();
    }

    clickTopbarToggle(): void {
        this.document.body.classList.toggle('topbar-mobile-on');
    }

    clickLeftAsideHideToggle(): void {
        this.document.body.classList.toggle('header-menu-wrapper-on');
    }
}
