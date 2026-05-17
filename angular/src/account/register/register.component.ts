import { AfterViewInit, Component, Injector, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountServiceProxy,
    PasswordComplexitySetting,
    ProfileServiceProxy,
    RegisterOutput,
} from '@shared/service-proxies/service-proxies';
import { LoginService } from '../login/login.service';
import { RegisterModel } from './register.model';
import { finalize } from 'rxjs/operators';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { EqualValidator } from '../../shared/utils/validation/equal-validator.directive';
import { PasswordModule } from 'primeng/password';
import { PasswordComplexityValidator } from '../../shared/utils/validation/password-complexity-validator.directive';
import { NgIf } from '@angular/common';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './register.component.html',
    animations: [accountModuleAnimation()],
    imports: [
        FormsModule,
        AutoFocusDirective,
        ValidationMessagesComponent,
        EqualValidator,
        PasswordModule,
        PasswordComplexityValidator,
        NgIf,
        RouterLink,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class RegisterComponent extends AppComponentBase implements OnInit, AfterViewInit {
    model: RegisterModel = new RegisterModel();
    passwordComplexitySetting: PasswordComplexitySetting = new PasswordComplexitySetting();
    saving = false;

    constructor(
        injector: Injector,
        private _accountService: AccountServiceProxy,
        private _router: Router,
        private readonly _loginService: LoginService,
        private _profileService: ProfileServiceProxy,
        private _recaptchaWrapperService: ReCaptchaV3WrapperService
    ) {
        super(injector);
    }

    ngOnInit() {
        //Prevent to register new users in the host context
        if (this.appSession.tenant == null) {
            this._router.navigate(['account/login']);
            return;
        }

        this._profileService.getPasswordComplexitySetting().subscribe((result) => {
            this.passwordComplexitySetting = result.setting;
        });
    }

    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnRegister();
    }

    save(): void {
        let recaptchaCallback = (token: string) => {
            this.saving = true;
            this.model.captchaResponse = token;
            this._accountService
                .register(this.model)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    })
                )
                .subscribe((result: RegisterOutput) => {
                    if (!result.canLogin) {
                        this.notify.success(this.l('SuccessfullyRegistered'));
                        this._router.navigate(['account/login']);
                        return;
                    }

                    //Autheticate
                    this.saving = true;
                    this._loginService.authenticateModel.userNameOrEmailAddress = this.model.userName;
                    this._loginService.authenticateModel.password = this.model.password;
                    this._loginService.authenticate(() => {
                        this.saving = false;
                    });
                });
        };

        if (this._recaptchaWrapperService.useCaptchaOnRegister()) {
            this._recaptchaWrapperService
                .getService()
                .execute('register')
                .subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }
}
