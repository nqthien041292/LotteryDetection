import { Injectable } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppSessionService } from '@shared/common/session/app-session.service';

@Injectable({
    providedIn: 'root',
})
export class LogoService {
    private remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
    private skin: string = this._appSessionService.theme.baseSettings.layout.darkMode ? 'dark' : 'light';

    constructor(
        private _dateTimeService: DateTimeService,
        private _appSessionService: AppSessionService
    ) {}

    getLogoUrl(skin: string = this.skin, small: boolean = false): string {
        let resolvedSkin = skin ? skin : this.skin;
        const tenantId = this._appSessionService.tenant?.id;
        const date = this._dateTimeService.getDate();

        const suffix = small ? '-sm' : '';

        return `${this.remoteServiceBaseUrl}/TenantCustomization/GetTenantLogo?skin=${resolvedSkin}${suffix}&tenantId=${tenantId}&c=${date}`;
    }

    getDefaultLogoUrl(skin: string = this.skin, small: boolean = false): string {
        let resolvedSkin = skin ? skin : this.skin;
        const suffix = small ? '-sm' : '';
        return `${AppConsts.appBaseUrl}/assets/common/images/app-logo-on-${resolvedSkin}${suffix}.svg`;
    }
}
