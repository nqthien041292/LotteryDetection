import { Component, Injector, ViewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GetNotificationSettingsOutput,
    NotificationServiceProxy,
    NotificationSubscriptionDto,
    UpdateNotificationSettingsInput,
} from '@shared/service-proxies/service-proxies';
import { map as _map } from 'lodash-es';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { NgIf, NgFor } from '@angular/common';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'notificationSettingsModal',
    templateUrl: './notification-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, NgIf, NgFor, ButtonBusyDirective, LocalizePipe],
})
export class NotificationSettingsModalComponent extends AppComponentBase {
    @ViewChild('modal', { static: true }) modal: ModalDirective;

    saving = false;

    settings: GetNotificationSettingsOutput;

    constructor(
        injector: Injector,
        private _notificationService: NotificationServiceProxy
    ) {
        super(injector);
    }

    show() {
        this.getSettings(() => {
            this.modal.show();
        });
    }

    save(): void {
        const input = new UpdateNotificationSettingsInput();
        input.receiveNotifications = this.settings.receiveNotifications;
        input.notifications = _map(this.settings.notifications, (n) => {
            let subscription = new NotificationSubscriptionDto();
            subscription.name = n.name;
            subscription.isSubscribed = n.isSubscribed;
            return subscription;
        });

        this.saving = true;
        this._notificationService
            .updateNotificationSettings(input)
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
            });
    }

    close(): void {
        this.modal.hide();
    }

    private getSettings(callback: () => void) {
        this._notificationService.getNotificationSettings().subscribe((result: GetNotificationSettingsOutput) => {
            this.settings = result;
            callback();
        });
    }
}
