import { IAjaxResponse, TokenService } from 'abp-ng2-module';
import { Component, ElementRef, EventEmitter, Injector, Output, ViewChild } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProfileServiceProxy, UpdateProfilePictureInput } from '@shared/service-proxies/service-proxies';
import { FileUploader, FileUploaderOptions, FileItem } from 'ng2-file-upload';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { ImageCroppedEvent, base64ToFile, ImageCropperComponent } from 'ngx-image-cropper';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'changeProfilePictureModal',
    templateUrl: './change-profile-picture-modal.component.html',
    imports: [AppBsModalDirective, NgIf, FormsModule, ImageCropperComponent, LocalizePipe],
})
export class ChangeProfilePictureModalComponent extends AppComponentBase {
    @ViewChild('changeProfilePictureModal', { static: true }) modal: ModalDirective;
    @ViewChild('uploadProfilePictureInput') uploadProfilePictureInput: ElementRef;

    @Output() modalSave: EventEmitter<number> = new EventEmitter<number>();

    public active = false;
    public uploader: FileUploader;
    public temporaryPictureUrl: string;
    public saving = false;
    public useGravatarProfilePicture = false;

    imageChangedEvent: any = '';
    userId: number = null;
    maxProfilePictureSizeInMB: number;
    maxProfilePictureWidth: number;
    maxProfilePictureHeight: number;

    private _uploaderOptions: FileUploaderOptions;

    constructor(
        injector: Injector,
        private _profileService: ProfileServiceProxy,
        private _tokenService: TokenService
    ) {
        super(injector);
    }

    initializeModal(): void {
        this.active = true;
        this.temporaryPictureUrl = '';
        this.useGravatarProfilePicture = this.setting.getBoolean('App.UserManagement.UseGravatarProfilePicture');
        if (!this.canUseGravatar()) {
            this.useGravatarProfilePicture = false;
        }

        this.maxProfilePictureSizeInMB = parseFloat(this.setting.get('App.UserManagement.MaxProfilePictureSizeInMB'));
        this.maxProfilePictureWidth = this.setting.getInt('App.UserManagement.MaxProfilePictureWidth');
        this.maxProfilePictureHeight = this.setting.getInt('App.UserManagement.MaxProfilePictureHeight');

        this.initFileUploader();
    }

    show(userId?: number): void {
        this.initializeModal();
        this.modal.show();
        this.userId = userId;
    }

    close(): void {
        this.active = false;
        this.imageChangedEvent = '';
        this.uploader.clearQueue();
        this.modal.hide();
    }

    fileChangeEvent(event: any): void {
        if (event.target.files[0].size > this.maxProfilePictureSizeInByte()) {
            this.message.warn(this.l('ProfilePicture_Warn_SizeLimit', this.maxProfilePictureSizeInMB));
            return;
        }

        this.uploadProfilePictureInput.nativeElement.innerText = event.target.files[0].name;

        this.imageChangedEvent = event;
    }

    imageCroppedFile(event: ImageCroppedEvent) {
        if (!event.blob) {
            return;
        }

        this.uploader.clearQueue();
        this.uploader.addToQueue([<File>event.blob]);
    }

    initFileUploader(): void {
        this._uploaderOptions = { url: AppConsts.remoteServiceBaseUrl + '/Profile/UploadProfilePictureFile' };
        this._uploaderOptions.autoUpload = false;
        this._uploaderOptions.authToken = 'Bearer ' + this._tokenService.getToken();
        this._uploaderOptions.removeAfterUpload = true;
        this.uploader = new FileUploader(this._uploaderOptions);

        this.uploader.onAfterAddingFile = (file) => {
            file.withCredentials = false;
        };

        let token = this.guid();
        this.uploader.onBuildItemForm = (fileItem: FileItem, form: any) => {
            form.append('FileType', fileItem.file.type);
            form.append('FileName', 'ProfilePicture');
            form.append('FileToken', token);
        };

        this.uploader.onSuccessItem = (item, response, status) => {
            const resp = <IAjaxResponse>JSON.parse(response);
            if (resp.success) {
                this.updateProfilePicture(token);
            } else {
                this.message.error(resp.error.message);
            }
        };
    }

    updateProfilePicture(fileToken: string): void {
        const input = new UpdateProfilePictureInput();
        input.fileToken = fileToken;

        if (this.userId) {
            input.userId = this.userId;
        }

        this.saving = true;
        this._profileService
            .updateProfilePicture(input)
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe(() => {
                abp.setting.values['App.UserManagement.UseGravatarProfilePicture'] =
                    this.useGravatarProfilePicture.toString();
                abp.event.trigger('profilePictureChanged');
                this.modalSave.emit(this.userId);
                this.close();
            });
    }

    updateProfilePictureToUseGravatar(): void {
        const input = new UpdateProfilePictureInput();
        input.useGravatarProfilePicture = this.useGravatarProfilePicture;

        this.saving = true;
        this._profileService
            .updateProfilePicture(input)
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe(() => {
                abp.setting.values['App.UserManagement.UseGravatarProfilePicture'] =
                    this.useGravatarProfilePicture.toString();
                abp.event.trigger('profilePictureChanged');
                this.close();
            });
    }

    guid(): string {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }

        return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
    }

    save(): void {
        if (this.useGravatarProfilePicture) {
            this.updateProfilePictureToUseGravatar();
        } else {
            this.uploader.uploadAll();
        }
    }

    canUseGravatar(): boolean {
        return this.setting.getBoolean('App.UserManagement.AllowUsingGravatarProfilePicture');
    }

    maxProfilePictureSizeInByte(): number {
        return this.maxProfilePictureSizeInMB * 1024 * 1024;
    }
}
