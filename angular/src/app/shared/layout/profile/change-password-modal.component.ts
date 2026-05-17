import { Component, ElementRef, Injector, ViewChild } from '@angular/core';
import { PasswordMeterComponent } from '@metronic/app/kt/components';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ChangePasswordInput,
    PasswordComplexitySetting,
    ProfileServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { EqualValidator } from '../../../../shared/utils/validation/equal-validator.directive';
import { PasswordComplexityValidator } from '../../../../shared/utils/validation/password-complexity-validator.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'changePasswordModal',
    templateUrl: './change-password-modal.component.html',
    styleUrls: ['./change-password-modal.component.less'],
    imports: [
        AppBsModalDirective,
        NgIf,
        FormsModule,
        ValidationMessagesComponent,
        EqualValidator,
        PasswordComplexityValidator,
        LocalizePipe,
    ],
})
export class ChangePasswordModalComponent extends AppComponentBase {
    @ViewChild('currentPasswordInput', { static: true }) currentPasswordInput: ElementRef;
    @ViewChild('changePasswordModal', { static: true }) modal: ModalDirective;

    passwordComplexitySetting: PasswordComplexitySetting = new PasswordComplexitySetting();
    currentPassword: string;
    password: string;
    confirmPassword: string;

    saving = false;
    active = false;

    constructor(
        injector: Injector,
        private _profileService: ProfileServiceProxy
    ) {
        super(injector);
    }

    show(): void {
        this.active = true;
        this.currentPassword = '';
        this.password = '';
        this.confirmPassword = '';

        this._profileService.getPasswordComplexitySetting().subscribe((result) => {
            this.passwordComplexitySetting = result.setting;
            this.modal.show();
        });
    }

    onShown(): void {
        PasswordMeterComponent.bootstrap();
        document.getElementById('CurrentPassword').focus();
    }

    close(): void {
        this.active = false;
        this.modal.hide();
    }

    save(): void {
        let input = new ChangePasswordInput();
        input.currentPassword = this.currentPassword;
        input.newPassword = this.password;

        this.saving = true;
        this._profileService
            .changePassword(input)
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe(() => {
                this.notify.info(this.l('YourPasswordHasChangedSuccessfully'));
                this.close();
            });
    }
}
