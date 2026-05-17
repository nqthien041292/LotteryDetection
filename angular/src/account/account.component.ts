import { Component, Injector, NgZone, OnInit, ViewContainerRef, ViewEncapsulation } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppUiCustomizationService } from '@shared/common/ui/app-ui-customization.service';
import { filter as _filter } from 'lodash-es';
import { LoginService } from './login/login.service';
import { AppPreBootstrap } from 'AppPreBootstrap';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgIf, NgTemplateOutlet } from '@angular/common';
import { TenantChangeComponent } from './shared/tenant-change.component';
import { LanguageSwitchComponent } from './language-switch.component';
import { QrLoginSignalRService } from './qr-login/qr-login-signalr.service';
import { PasswordlessAuthenticateResultModel } from '@shared/service-proxies/service-proxies';
import { SignalRHelper } from '@shared/helpers/SignalRHelper';

@Component({
    templateUrl: './account.component.html',
    styleUrls: ['./account.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [NgIf, NgTemplateOutlet, TenantChangeComponent, RouterOutlet, LanguageSwitchComponent],
})
export class AccountComponent extends AppComponentBase implements OnInit {
    viewContainerRef: ViewContainerRef;
    tenantLogoUrl: string;

    skin = this.appSession.theme.baseSettings.layout.darkMode ? 'dark' : 'light';
    defaultLogo = AppConsts.appBaseUrl + '/assets/common/images/app-logo-on-' + this.skin + '.svg';
    backgroundImageName = this.appSession.theme.baseSettings.layout.darkMode ? 'login-dark' : 'login';
    qrCodeUrl: string;
    qrCodeRefreshInterval: any;

    tenantChangeDisabledRoutes: string[] = [
        'select-edition',
        'gateway-selection',
        'register-tenant',
        'stripe-pre-payment',
        'stripe-post-payment',
        'paypal-pre-payment',
        'paypal-post-payment',
        'stripe-cancel-payment',
        'buy-succeed',
        'extend-succeed',
        'upgrade-succeed',
        'payment-failed',
        'session-locked',
    ];

    public constructor(
        injector: Injector,
        private _router: Router,
        private _loginService: LoginService,
        private _uiCustomizationService: AppUiCustomizationService,
        viewContainerRef: ViewContainerRef,
        private _logoService: LogoService,
        private _qrLoginSignalRService: QrLoginSignalRService,
        public _zone: NgZone
    ) {
        super(injector);

        // We need this small hack in order to catch application root view container ref for modals
        this.viewContainerRef = viewContainerRef;

        this.tenantLogoUrl = this._logoService.getLogoUrl();
    }

    showTenantChange(): boolean {
        if (!this._router.url) {
            return false;
        }

        if (
            _filter(this.tenantChangeDisabledRoutes, (route) => this._router.url.indexOf('/account/' + route) >= 0)
                .length
        ) {
            return false;
        }

        return abp.multiTenancy.isEnabled && !this.supportsTenancyNameInUrl();
    }

    isSelectEditionPage(): boolean {
        return this._router.url.indexOf('/account/select-edition') >= 0;
    }

    isQrLoginEnabled(): boolean {
        return this.setting.getBoolean('App.UserManagement.IsQrLoginEnabled');
    }

    ngOnInit(): void {
        this._loginService.init();

        if (this.isQrLoginEnabled()) {
            this.initializeQrLoginSignalR();
        }

        document.body.className = this._uiCustomizationService.getAccountModuleBodyClass();
    }

    goToHome(): void {
        (window as any).location.href = '/';
    }

    getBgUrl(): string {
        return 'url(./assets/metronic/themes/' + this.currentTheme.baseSettings.theme + '/images/bg/bg-4.jpg)';
    }

    private supportsTenancyNameInUrl() {
        return AppPreBootstrap.resolveTenancyName(AppConsts.appBaseUrlFormat) != null;
    }

    private initializeQrLoginSignalR() {
        if (this.appSession.application) {
            SignalRHelper.initSignalR(() => {
                this._qrLoginSignalRService.init().then(() => {
                    this.subscribeToEvent('app.qrlogin.generateQrCode', (url) => {
                        this._zone.run(() => {
                            this.qrCodeUrl = url;
                        });
                    });
                });
            });
        }

        this.subscribeToEvent('app.qrlogin.getAuthData', (authData) => {
            this._zone.run(() => {
                this._loginService.qrLoginAuthenticate(
                    authData.accessToken,
                    authData.encryptedAccessToken,
                    authData.expireInSeconds,
                    authData.refreshToken,
                    authData.refreshTokenExpireInSeconds
                );
            });
        });
    }
}
