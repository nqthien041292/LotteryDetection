import { IAjaxResponse, TokenService } from 'abp-ng2-module';
import { Component, ElementRef, Injector, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    SettingScopes,
    SendTestEmailInput,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
    JsonClaimMapDto,
} from '@shared/service-proxies/service-proxies';
import { FileUploader, FileUploaderOptions, FileUploadModule } from 'ng2-file-upload';
import { finalize } from 'rxjs/operators';
import { KeyValueListManagerComponent } from '@app/shared/common/key-value-list-manager/key-value-list-manager.component';
import { UntypedFormControl, FormsModule } from '@angular/forms';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { NgIf, NgClass } from '@angular/common';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { TimeZoneComboComponent } from '../../shared/common/timing/timezone-combo.component';
import { TooltipDirective } from 'ngx-bootstrap/tooltip';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { PasswordInputWithShowButtonComponent } from '../../shared/common/password-input-with-show-button/password-input-with-show-button.component';
import { KeyValueListManagerComponent as KeyValueListManagerComponent_1 } from '../../shared/common/key-value-list-manager/key-value-list-manager.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './tenant-settings.component.html',
    styleUrls: ['./tenant-settings.component.css'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        NgIf,
        TabsetComponent,
        TabDirective,
        TimeZoneComboComponent,
        FormsModule,
        TooltipDirective,
        FileUploadModule,
        NgClass,
        ValidationMessagesComponent,
        PasswordInputWithShowButtonComponent,
        KeyValueListManagerComponent_1,
        LocalizePipe,
    ],
})
export class TenantSettingsComponent extends AppComponentBase implements OnInit, AfterViewInit {
    @ViewChild('wsFederationClaimsMappingManager') wsFederationClaimsMappingManager: KeyValueListManagerComponent;
    @ViewChild('openIdConnectClaimsMappingManager') openIdConnectClaimsMappingManager: KeyValueListManagerComponent;
    @ViewChild('emailSmtpSettingsForm') emailSmtpSettingsForm: UntypedFormControl;
    @ViewChild('securitySettingsForm') securitySettingsForm: UntypedFormControl;
    @ViewChild('uploadCustomCSSInputLabel') uploadCustomCSSInputLabel: ElementRef;

    @ViewChild('darkLogoUploaderFileInput') darkLogoUploaderFileInput: any;
    @ViewChild('darkLogoMinimalUploaderFileInput') darkLogoMinimalUploaderFileInput: any;
    @ViewChild('lightLogoUploaderFileInput') lightLogoUploaderFileInput: any;
    @ViewChild('lightLogoMinimalUploaderFileInput') lightLogoMinimalUploaderFileInput: any;

    usingDefaultTimeZone = false;
    initialTimeZone: string = null;
    testEmailAddress: string = undefined;
    setRandomPassword: boolean;

    isMultiTenancyEnabled: boolean = this.multiTenancy.isEnabled;
    showTimezoneSelection: boolean = abp.clock.provider.supportsMultipleTimezone;
    activeTabIndex: number = abp.clock.provider.supportsMultipleTimezone ? 0 : 1;
    loading = false;
    settings: TenantSettingsEditDto = undefined;

    remoteServiceBaseUrl = AppConsts.remoteServiceBaseUrl;

    defaultTimezoneScope: SettingScopes = SettingScopes.Tenant;

    enabledSocialLoginSettings: string[];
    useFacebookHostSettings: boolean;
    useGoogleHostSettings: boolean;
    useMicrosoftHostSettings: boolean;
    useWsFederationHostSettings: boolean;
    useOpenIdConnectHostSettings: boolean;
    useTwitterHostSettings: boolean;

    wsFederationClaimMappings: { key: string; value: string }[];
    openIdConnectClaimMappings: { key: string; value: string }[];
    openIdConnectResponseTypeCode: boolean;
    openIdConnectResponseTypeToken: boolean;
    openIdConnectResponseTypeIdToken: boolean;

    initialEmailSettings: string;

    darkLogoUploader: FileUploader;
    darkLogoMinimalUploader: FileUploader;
    lightLogoUploader: FileUploader;
    lightLogoMinimalUploader: FileUploader;
    customCssUploader: FileUploader;

    darkLogoUrl: string;
    darkSmallLogoUrl: string;
    lightLogoUrl: string;
    lightSmallLogoUrl: string;

    emailDomainPattern = '^[a-zA-Z0-9._%+-]+(?<!@)[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$';

    constructor(
        injector: Injector,
        private _tenantSettingsService: TenantSettingsServiceProxy,
        private _tokenService: TokenService,
        private _logoService: LogoService
    ) {
        super(injector);
        this.initLogoUrls();
    }

    ngOnInit(): void {
        this.testEmailAddress = this.appSession.user.emailAddress;
        this.getSettings();
        this.loadSocialLoginSettings();
    }

    ngAfterViewInit() {
        this.initUploaders();
    }

    getSettings(): void {
        this.loading = true;
        this._tenantSettingsService
            .getAllSettings()
            .pipe(
                finalize(() => {
                    this.loading = false;
                })
            )
            .subscribe((result: TenantSettingsEditDto) => {
                this.settings = result;
                if (this.settings.general) {
                    this.initialTimeZone = this.settings.general.timezone;
                    this.usingDefaultTimeZone =
                        this.settings.general.timezoneForComparison === abp.setting.values['Abp.Timing.TimeZone'];
                }
                this.useFacebookHostSettings = !(
                    this.settings.externalLoginProviderSettings.facebook.appId ||
                    this.settings.externalLoginProviderSettings.facebook_IsDeactivated
                );
                this.useGoogleHostSettings = !(
                    this.settings.externalLoginProviderSettings.google.clientId ||
                    this.settings.externalLoginProviderSettings.google_IsDeactivated
                );
                this.useMicrosoftHostSettings = !(
                    this.settings.externalLoginProviderSettings.microsoft.clientId ||
                    this.settings.externalLoginProviderSettings.microsoft_IsDeactivated
                );
                this.useWsFederationHostSettings = !(
                    this.settings.externalLoginProviderSettings.wsFederation.clientId ||
                    this.settings.externalLoginProviderSettings.wsFederation_IsDeactivated
                );
                this.useOpenIdConnectHostSettings = !(
                    this.settings.externalLoginProviderSettings.openIdConnect.clientId ||
                    this.settings.externalLoginProviderSettings.openIdConnect_IsDeactivated
                );
                this.useTwitterHostSettings = !(
                    this.settings.externalLoginProviderSettings.twitter.consumerKey ||
                    this.settings.externalLoginProviderSettings.twitter_IsDeactivated
                );

                this.wsFederationClaimMappings =
                    this.settings.externalLoginProviderSettings.openIdConnectClaimsMapping.map((item) => ({
                        key: item.key,
                        value: item.claim,
                    }));

                this.openIdConnectClaimMappings =
                    this.settings.externalLoginProviderSettings.openIdConnectClaimsMapping.map((item) => ({
                        key: item.key,
                        value: item.claim,
                    }));

                if (this.settings.externalLoginProviderSettings.openIdConnect.responseType) {
                    let openIdConnectResponseTypes =
                        this.settings.externalLoginProviderSettings.openIdConnect.responseType.split(',');

                    this.openIdConnectResponseTypeCode = openIdConnectResponseTypes.indexOf('code') > -1;
                    this.openIdConnectResponseTypeIdToken = openIdConnectResponseTypes.indexOf('id_token') > -1;
                    this.openIdConnectResponseTypeToken = openIdConnectResponseTypes.indexOf('token') > -1;
                }

                this.initialEmailSettings = JSON.stringify(this.settings.email);
            });
    }

    initLogoUrls(): void {
        this.setLogoUrlBySkin('dark');
        this.setLogoUrlBySkin('dark-sm');
        this.setLogoUrlBySkin('light');
        this.setLogoUrlBySkin('light-sm');
    }

    initUploaders(): void {
        this.darkLogoUploader = this.createUploader('/TenantCustomization/UploadDarkLogo', (result) => {
            this.appSession.tenant.darkLogoFileType = result.fileType;
            this.appSession.tenant.darkLogoId = result.id;
            this.setLogoUrlBySkin('dark');
        });

        this.darkLogoMinimalUploader = this.createUploader('/TenantCustomization/UploadDarkLogoMinimal', (result) => {
            this.appSession.tenant.darkLogoMinimalFileType = result.fileType;
            this.appSession.tenant.darkLogoMinimalId = result.id;
            this.setLogoUrlBySkin('dark-sm');
        });

        this.lightLogoUploader = this.createUploader('/TenantCustomization/UploadLightLogo', (result) => {
            this.appSession.tenant.lightLogoFileType = result.fileType;
            this.appSession.tenant.lightLogoId = result.id;
            this.setLogoUrlBySkin('light');
        });

        this.lightLogoMinimalUploader = this.createUploader('/TenantCustomization/UploadLightLogoMinimal', (result) => {
            this.appSession.tenant.lightLogoMinimalFileType = result.fileType;
            this.appSession.tenant.lightLogoMinimalId = result.id;
            this.setLogoUrlBySkin('light-sm');
        });

        this.customCssUploader = this.createUploader('/TenantCustomization/UploadCustomCss', (result) => {
            this.appSession.tenant.customCssId = result.id;

            let oldTenantCustomCss = document.getElementById('TenantCustomCss');
            if (oldTenantCustomCss) {
                oldTenantCustomCss.remove();
            }

            let tenantCustomCss = document.createElement('link');
            tenantCustomCss.setAttribute('id', 'TenantCustomCss');
            tenantCustomCss.setAttribute('rel', 'stylesheet');
            tenantCustomCss.setAttribute(
                'href',
                AppConsts.remoteServiceBaseUrl +
                    '/TenantCustomization/GetCustomCss?tenantId=' +
                    this.appSession.tenant.id
            );
            document.head.appendChild(tenantCustomCss);
        });
    }

    createUploader(url: string, success?: (result: any) => void): FileUploader {
        const uploaderOptions: FileUploaderOptions = { url: AppConsts.remoteServiceBaseUrl + url };
        uploaderOptions.authToken = 'Bearer ' + this._tokenService.getToken();
        uploaderOptions.removeAfterUpload = true;

        const uploader = new FileUploader(uploaderOptions);

        uploader.onAfterAddingFile = (file) => {
            file.withCredentials = false;
        };

        uploader.onSuccessItem = (item, response, status) => {
            const ajaxResponse = <IAjaxResponse>JSON.parse(response);

            if (ajaxResponse.success) {
                this.notify.info(this.l('SavedSuccessfully'));
                if (success) {
                    success(ajaxResponse.result);
                }
            } else {
                this.message.error(ajaxResponse.error.message);
            }
        };

        return uploader;
    }

    uploadDarkLogo(): void {
        this.darkLogoUploader.uploadAll();
        this.darkLogoUploaderFileInput.nativeElement.value = '';
    }

    uploadDarkLogoMinimal(): void {
        this.darkLogoMinimalUploader.uploadAll();
        this.darkLogoMinimalUploaderFileInput.nativeElement.value = '';
    }

    uploadLightLogo(): void {
        this.lightLogoUploader.uploadAll();
        this.lightLogoUploaderFileInput.nativeElement.value = '';
    }

    uploadLightLogoMinimal(): void {
        this.lightLogoMinimalUploader.uploadAll();
        this.lightLogoMinimalUploaderFileInput.nativeElement.value = '';
    }

    uploadCustomCss(): void {
        this.customCssUploader.uploadAll();
    }

    clearDarkLogo(): void {
        this._tenantSettingsService.clearDarkLogo().subscribe(() => {
            this.appSession.tenant.darkLogoFileType = null;
            this.appSession.tenant.darkLogoId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.setLogoUrlBySkin('dark');
        });
    }

    clearDarkLogoMinimal(): void {
        this._tenantSettingsService.clearDarkLogoMinimal().subscribe(() => {
            this.appSession.tenant.darkLogoMinimalFileType = null;
            this.appSession.tenant.darkLogoMinimalId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.setLogoUrlBySkin('dark-sm');
        });
    }

    clearLightLogo(): void {
        this._tenantSettingsService.clearLightLogo().subscribe(() => {
            this.appSession.tenant.lightLogoFileType = null;
            this.appSession.tenant.lightLogoId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.setLogoUrlBySkin('light');
        });
    }

    clearLightLogoMinimal(): void {
        this._tenantSettingsService.clearLightLogoMinimal().subscribe(() => {
            this.appSession.tenant.lightLogoMinimalFileType = null;
            this.appSession.tenant.lightLogoMinimalId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.setLogoUrlBySkin('light-sm');
        });
    }

    clearCustomCss(): void {
        this._tenantSettingsService.clearCustomCss().subscribe(() => {
            this.appSession.tenant.customCssId = null;

            let oldTenantCustomCss = document.getElementById('TenantCustomCss');
            if (oldTenantCustomCss) {
                oldTenantCustomCss.remove();
            }

            this.notify.info(this.l('ClearedSuccessfully'));
        });
    }

    mapClaims(): void {
        if (this.wsFederationClaimsMappingManager) {
            this.settings.externalLoginProviderSettings.wsFederationClaimsMapping =
                this.wsFederationClaimsMappingManager.getItems().map(
                    (item) =>
                        new JsonClaimMapDto({
                            key: item.key,
                            claim: item.value,
                        })
                );
        }

        if (this.openIdConnectClaimsMappingManager) {
            this.settings.externalLoginProviderSettings.openIdConnectClaimsMapping =
                this.openIdConnectClaimsMappingManager.getItems().map(
                    (item) =>
                        new JsonClaimMapDto({
                            key: item.key,
                            claim: item.value,
                        })
                );
        }
    }

    saveAll(): void {
        if (!this.isSmtpSettingsFormValid()) {
            return;
        }

        if (!this.isSecuritySettingsFormValid()) {
            return;
        }

        this.settings.externalLoginProviderSettings.openIdConnect.responseType =
            this.getSelectedOpenIdConnectResponseTypes();

        this.mapClaims();
        this._tenantSettingsService.updateAllSettings(this.settings).subscribe(() => {
            this.notify.info(this.l('SavedSuccessfully'));

            if (
                abp.clock.provider.supportsMultipleTimezone &&
                this.usingDefaultTimeZone &&
                this.initialTimeZone !== this.settings.general.timezone
            ) {
                this.message.info(this.l('TimeZoneSettingChangedRefreshPageNotification')).then(() => {
                    window.location.reload();
                });
            }
            this.initialEmailSettings = JSON.stringify(this.settings.email);
        });
    }

    getSelectedOpenIdConnectResponseTypes(): string {
        let openIdConnectResponseTypes = '';
        if (this.openIdConnectResponseTypeToken) {
            openIdConnectResponseTypes += 'token';
        }

        if (this.openIdConnectResponseTypeIdToken) {
            if (openIdConnectResponseTypes.length > 0) {
                openIdConnectResponseTypes += ',';
            }
            openIdConnectResponseTypes += 'id_token';
        }

        if (this.openIdConnectResponseTypeCode) {
            if (openIdConnectResponseTypes.length > 0) {
                openIdConnectResponseTypes += ',';
            }
            openIdConnectResponseTypes += 'code';
        }

        return openIdConnectResponseTypes;
    }

    sendTestEmail(): void {
        const input = new SendTestEmailInput();
        input.emailAddress = this.testEmailAddress;

        if (this.initialEmailSettings !== JSON.stringify(this.settings.email)) {
            this.message.confirm(this.l('SendEmailWithSavedSettingsWarning'), this.l('AreYouSure'), (isConfirmed) => {
                if (isConfirmed) {
                    this._tenantSettingsService.sendTestEmail(input).subscribe((result) => {
                        this.notify.info(this.l('TestEmailSentSuccessfully'));
                    });
                }
            });
        } else {
            this._tenantSettingsService.sendTestEmail(input).subscribe((result) => {
                this.notify.info(this.l('TestEmailSentSuccessfully'));
            });
        }
    }

    loadSocialLoginSettings(): void {
        const self = this;
        this._tenantSettingsService.getEnabledSocialLoginSettings().subscribe((setting) => {
            self.enabledSocialLoginSettings = setting.enabledSocialLoginSettings;
        });
    }

    clearFacebookSettings(): void {
        this.settings.externalLoginProviderSettings.facebook.appId = '';
        this.settings.externalLoginProviderSettings.facebook.appSecret = '';
        this.settings.externalLoginProviderSettings.facebook_IsDeactivated = false;
    }

    clearGoogleSettings(): void {
        this.settings.externalLoginProviderSettings.google.clientId = '';
        this.settings.externalLoginProviderSettings.google.clientSecret = '';
        this.settings.externalLoginProviderSettings.google.userInfoEndpoint = '';
        this.settings.externalLoginProviderSettings.google_IsDeactivated = false;
    }

    clearMicrosoftSettings(): void {
        this.settings.externalLoginProviderSettings.microsoft.clientId = '';
        this.settings.externalLoginProviderSettings.microsoft.clientSecret = '';
        this.settings.externalLoginProviderSettings.microsoft_IsDeactivated = false;
    }

    clearWsFederationSettings(): void {
        this.settings.externalLoginProviderSettings.wsFederation.clientId = '';
        this.settings.externalLoginProviderSettings.wsFederation.authority = '';
        this.settings.externalLoginProviderSettings.wsFederation.wtrealm = '';
        this.settings.externalLoginProviderSettings.wsFederation.metaDataAddress = '';
        this.settings.externalLoginProviderSettings.wsFederation.tenant = '';
        this.settings.externalLoginProviderSettings.wsFederationClaimsMapping = [];
        this.settings.externalLoginProviderSettings.wsFederation_IsDeactivated = false;
    }

    clearOpenIdSettings(): void {
        this.settings.externalLoginProviderSettings.openIdConnect.clientId = '';
        this.settings.externalLoginProviderSettings.openIdConnect.clientSecret = '';
        this.settings.externalLoginProviderSettings.openIdConnect.authority = '';
        this.settings.externalLoginProviderSettings.openIdConnect.loginUrl = '';
        this.settings.externalLoginProviderSettings.openIdConnectClaimsMapping = [];
        this.settings.externalLoginProviderSettings.openIdConnect_IsDeactivated = false;
    }

    clearTwitterSettings(): void {
        this.settings.externalLoginProviderSettings.twitter.consumerKey = '';
        this.settings.externalLoginProviderSettings.twitter.consumerSecret = '';
    }

    isSocialLoginEnabled(name: string): boolean {
        return this.enabledSocialLoginSettings && this.enabledSocialLoginSettings.indexOf(name) !== -1;
    }

    isSmtpSettingsFormValid(): boolean {
        if (!this.emailSmtpSettingsForm) {
            return true;
        }
        return this.emailSmtpSettingsForm.valid;
    }

    isSecuritySettingsFormValid(): boolean {
        return this.securitySettingsForm.valid;
    }

    private setLogoUrlBySkin(skin: string): void {
        const logoUrls: { [key: string]: string } = {
            dark: 'darkLogoUrl',
            'dark-sm': 'darkSmallLogoUrl',
            light: 'lightLogoUrl',
            'light-sm': 'lightSmallLogoUrl',
        };

        const baseLogoUrl = this._logoService.getLogoUrl(skin);
        const urlProperty = logoUrls[skin] || 'lightLogoUrl';

        (this as any)[urlProperty] = baseLogoUrl;
    }
}
