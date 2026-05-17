import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { ChangeProfilePictureModalComponent } from './change-profile-picture-modal.component';

@NgModule({
    imports: [CommonModule, AppSharedModule, ChangeProfilePictureModalComponent],
    exports: [ChangeProfilePictureModalComponent],
})
export class ChangeProfilePictureModalModule {}
