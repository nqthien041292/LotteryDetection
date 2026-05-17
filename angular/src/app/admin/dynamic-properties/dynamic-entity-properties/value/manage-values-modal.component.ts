import { Component, Injector, ViewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { ManagerComponent } from '@app/admin/dynamic-properties/dynamic-entity-properties/value/manager.component';
import { AppBsModalDirective } from '../../../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgIf } from '@angular/common';
import { ManagerComponent as ManagerComponent_1 } from './manager.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'manage-dynamic-entity-property-values-modal',
    templateUrl: './manage-values-modal.component.html',
    imports: [AppBsModalDirective, NgIf, ManagerComponent_1, LocalizePipe],
})
export class ManageValuesModalComponent extends AppComponentBase {
    @ViewChild('dynamicEntityPropertyValueManager', { static: false })
    dynamicEntityPropertyValueManager: ManagerComponent;
    @ViewChild('manageDynamicEntityParameterValuesModal') modal: ModalDirective;

    entityFullName: string;
    entityId: string;

    initialized = false;

    constructor(_injector: Injector) {
        super(_injector);
    }

    saveAll(): void {
        this.dynamicEntityPropertyValueManager.saveAll();
    }

    close(): void {
        this.modal.hide();
    }

    show(entityFullName: string, entityId: string) {
        this.entityFullName = entityFullName;
        this.entityId = entityId;

        if (this.dynamicEntityPropertyValueManager) {
            this.dynamicEntityPropertyValueManager.entityFullName = entityFullName;
            this.dynamicEntityPropertyValueManager.entityId = entityId;
            this.dynamicEntityPropertyValueManager.initialize();
        }

        this.initialized = true;
        this.modal.show();
    }
}
