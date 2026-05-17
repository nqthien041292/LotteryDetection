import { Injectable } from '@angular/core';
import { AbpHttpInterceptor, MessageService } from 'abp-ng2-module';
import { HttpResponse } from '@angular/common/http';
import { Observable, switchMap, throwError } from 'rxjs';

@Injectable({
    providedIn: 'root',
})
export class ZeroTemplateHttpInterceptor extends AbpHttpInterceptor {
    private messageService: MessageService = new MessageService();

    protected handleErrorResponse(error: any): Observable<never> {
        if (!(error.error instanceof Blob)) {
            this.messageService.error('Server is unreachable');
            return throwError(error);
        }

        return this.configuration.blobToText(error.error).pipe(
            switchMap((json) => {
                const errorBody = json === '' || json === null ? {} : JSON.parse(json);
                const errorResponse = new HttpResponse({
                    headers: error.headers,
                    status: error.status,
                    body: errorBody,
                });

                let ajaxResponse = this.configuration.getAbpAjaxResponseOrNull(errorResponse);

                if (ajaxResponse != null) {
                    this.configuration.handleAbpResponse(errorResponse, ajaxResponse);
                } else {
                    this.configuration.handleNonAbpErrorResponse(errorResponse);
                }

                return throwError(error);
            })
        );
    }
}
