import { Component, ElementRef, EventEmitter, Injector, Output, ViewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LinkToUserInput, UserLinkServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgIf, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'linkAccountModal',
    templateUrl: './link-account-modal.component.html',
    imports: [
        AppBsModalDirective,
        NgIf,
        FormsModule,
        NgClass,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class LinkAccountModalComponent extends AppComponentBase {
    @ViewChild('linkAccountModal', { static: true }) modal: ModalDirective;

    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();

    active = false;
    saving = false;

    linkUser: LinkToUserInput = new LinkToUserInput();

    constructor(
        injector: Injector,
        private _userLinkService: UserLinkServiceProxy
    ) {
        super(injector);
    }

    show(): void {
        this.active = true;
        this.linkUser = new LinkToUserInput();
        this.linkUser.tenancyName = this.appSession.tenancyName;
        this.modal.show();
    }

    onShown(): void {
        document.getElementById('TenancyName').focus();
    }

    save(): void {
        this.saving = true;
        this._userLinkService
            .linkToUser(this.linkUser)
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }

    close(): void {
        this.active = false;
        this.modal.hide();
    }
}
