import { AfterViewInit, Component, Injector } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProfileServiceProxy } from '@shared/service-proxies/service-proxies';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { LoginService, ExternalLoginProvider } from './login.service';
import { AppConsts } from '@shared/AppConsts';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'app-session-lock-screen',
    templateUrl: './session-lock-screen.component.html',
    styleUrls: ['session-lock-screen.component.less'],
    animations: [accountModuleAnimation()],
    imports: [NgIf, FormsModule, ValidationMessagesComponent, LocalizePipe],
})
export class SessionLockScreenComponent extends AppComponentBase implements AfterViewInit {
    userInfo: any;
    submitting = false;
    isExternalLoginEnabled: boolean;
    externalLoginProviderName: string;

    constructor(
        injector: Injector,
        private _profileService: ProfileServiceProxy,
        public loginService: LoginService,
        private _recaptchaWrapperService: ReCaptchaV3WrapperService
    ) {
        super(injector);
        this.getLastUserInfo();
    }

    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnLogin();
    }

    getLastUserInfo(): void {
        let cookie = abp.utils.getCookieValue('userInfo');
        if (!cookie) {
            location.href = '';
        }

        let userInfo = JSON.parse(cookie);
        if (!userInfo) {
            location.href = '';
        }

        this.loginService.authenticateModel.userNameOrEmailAddress = userInfo.userName;
        this.userInfo = {
            userName: userInfo.userName,
            tenant: userInfo.tenant,
            profilePicture: '',
            externalLoginProviderName: userInfo.externalLoginProviderName,
        };

        this.setIsExternalLoginEnabled(userInfo.externalLoginProviderName);
        this.externalLoginProviderName = userInfo.externalLoginProviderName;

        this._profileService.getProfilePictureByUserName(userInfo.userName).subscribe(
            (data) => {
                if (data.profilePicture) {
                    this.userInfo.profilePicture = 'data:image/jpeg;base64,' + data.profilePicture;
                } else {
                    this.userInfo.profilePicture =
                        AppConsts.appBaseUrl + '/assets/common/images/default-profile-picture.png';
                }
            },
            () => {
                this.userInfo.profilePicture =
                    AppConsts.appBaseUrl + '/assets/common/images/default-profile-picture.png';
            }
        );
    }

    login(): void {
        let recaptchaCallback = (token: string) => {
            this.showMainSpinner();

            this.submitting = true;
            this.loginService.authenticate(
                () => {
                    this.submitting = false;
                    this.hideMainSpinner();
                },
                null,
                token
            );
        };

        if (this._recaptchaWrapperService.useCaptchaOnLogin()) {
            this._recaptchaWrapperService
                .getService()
                .execute('login')
                .subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }

    externalLogin(): void {
        const foundProvider = this.loginService.externalLoginProviders.find(
            (p) => p.name === this.externalLoginProviderName
        );

        this.loginService.externalAuthenticate(foundProvider);
    }

    setIsExternalLoginEnabled(externalLoginProviderName: string): void {
        this.isExternalLoginEnabled = externalLoginProviderName != null;
    }
}
