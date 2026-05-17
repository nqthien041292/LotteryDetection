import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { HttpClientModule, HttpClientJsonpModule } from '@angular/common/http';
import { LotteryDetectionCommonModule } from '@shared/common/common.module';
import { FormsModule } from '@angular/forms';
import { ServiceProxyModule } from '@shared/service-proxies/service-proxy.module';
import { UtilsModule } from '@shared/utils/utils.module';
import { RecaptchaV3Module, RECAPTCHA_V3_SITE_KEY } from 'ng-recaptcha';
import { ModalModule } from 'ngx-bootstrap/modal';
import { AccountRoutingModule } from './account-routing.module';
import { AccountComponent } from './account.component';
import { AccountRouteGuard } from './auth/account-route-guard';
import { LanguageSwitchComponent } from './language-switch.component';
import { LoginService } from './login/login.service';
import { TenantRegistrationHelperService } from './register/tenant-registration-helper.service';
import { OAuthModule } from 'angular-oauth2-oidc';
import { PaymentHelperService } from './payment/payment-helper.service';
import { LocaleMappingService } from '@shared/locale-mapping.service';
import { PasswordModule } from 'primeng/password';

import { AccountSharedModule } from '@account/shared/account-shared.module';
import { AppConsts } from '@shared/AppConsts';
import { ReCaptchaV3WrapperService } from './shared/recaptchav3-wrapper.service';
import { MsalModule, MsalService } from '@azure/msal-angular';
import { InteractionType, PublicClientApplication } from '@azure/msal-browser';
import { QrLoginSignalRService } from './qr-login/qr-login-signalr.service';

export function getRecaptchaLanguage(): string {
    return new LocaleMappingService().map('recaptcha', abp.localization.currentLanguage.name);
}

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        HttpClientJsonpModule,
        RecaptchaV3Module,
        ModalModule.forRoot(),
        LotteryDetectionCommonModule,
        UtilsModule,
        ServiceProxyModule,
        AccountRoutingModule,
        OAuthModule.forRoot(),
        PasswordModule,
        AccountSharedModule,
        MsalModule.forRoot(
            new PublicClientApplication({
                auth: {
                    clientId: '',
                    authority: '',
                    redirectUri: '',
                },
            }),
            {
                interactionType: InteractionType.Popup,
                authRequest: null,
            },
            {
                interactionType: InteractionType.Redirect,
                protectedResourceMap: new Map(),
            }
        ),
        AccountComponent,
        LanguageSwitchComponent,
    ],
    providers: [
        LoginService,
        QrLoginSignalRService,
        TenantRegistrationHelperService,
        PaymentHelperService,
        AccountRouteGuard,
        MsalService,
        ReCaptchaV3WrapperService,
        { provide: RECAPTCHA_V3_SITE_KEY, useValue: AppConsts.recaptchaSiteKey },
    ],
})
export class AccountModule {}
