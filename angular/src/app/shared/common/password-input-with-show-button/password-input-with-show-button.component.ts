import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIf } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'password-input-with-show-button',
    templateUrl: './password-input-with-show-button.component.html',
    imports: [FormsModule, NgIf, LocalizePipe],
})
export class PasswordInputWithShowButtonComponent {
    @Input() data: string;
    @Output() dataChange = new EventEmitter();
    isVisible = false;

    toggleVisibility(): void {
        this.isVisible = !this.isVisible;
    }

    onChange() {
        this.dataChange.emit(this.data);
    }
}
