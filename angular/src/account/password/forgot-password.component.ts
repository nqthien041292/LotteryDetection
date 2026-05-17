import { Component, Injector } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppUrlService } from '@shared/common/nav/app-url.service';
import { AccountServiceProxy, SendPasswordResetCodeInput } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './forgot-password.component.html',
    animations: [accountModuleAnimation()],
    imports: [
        FormsModule,
        AutoFocusDirective,
        ValidationMessagesComponent,
        RouterLink,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class ForgotPasswordComponent extends AppComponentBase {
    model: SendPasswordResetCodeInput = new SendPasswordResetCodeInput();

    saving = false;

    constructor(
        injector: Injector,
        private _accountService: AccountServiceProxy,
        private _appUrlService: AppUrlService,
        private _router: Router
    ) {
        super(injector);
    }

    save(): void {
        this.saving = true;
        this._accountService
            .sendPasswordResetCode(this.model)
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe(() => {
                this.message.success(this.l('PasswordResetMailSentMessage'), this.l('MailSent')).then(() => {
                    this._router.navigate(['account/login']);
                });
            });
    }
}
