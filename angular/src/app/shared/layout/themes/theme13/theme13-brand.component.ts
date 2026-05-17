import { Injector, Component, ViewEncapsulation, Input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgIf } from '@angular/common';

@Component({
    templateUrl: './theme13-brand.component.html',
    selector: 'theme13-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf],
})
export class Theme13BrandComponent extends AppComponentBase {
    @Input() anchorClass = '';
    @Input() imageClass = 'h-25px logo';

    defaultLogoUrl: string;
    tenantLogoUrl: string;

    constructor(
        injector: Injector,
        private _logoService: LogoService
    ) {
        super(injector);
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl('dark');
        this.tenantLogoUrl = this._logoService.getLogoUrl('dark');
    }

    triggerAsideToggleClickEvent(): void {
        abp.event.trigger('app.kt_aside_toggler.onClick');
    }
}
