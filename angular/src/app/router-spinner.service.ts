import { Injectable } from '@angular/core';
import { Router, NavigationStart, NavigationEnd } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';

@Injectable({
    providedIn: 'root',
})
export class RouterSpinnerService {
    constructor(
        private router: Router,
        private spinnerService: NgxSpinnerService
    ) {
        this.router.events.subscribe((event) => {
            if (event instanceof NavigationStart) {
                this.spinnerService.show();
            }
            if (event instanceof NavigationEnd) {
                const metaTag = document.querySelector('meta[property="og:url"]');
                if (metaTag) {
                    metaTag.setAttribute('content', window.location.href);
                }
                spinnerService.hide();
            }
        });
    }
}
