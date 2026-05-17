import {
    enableProdMode,
    Injector,
    APP_INITIALIZER,
    LOCALE_ID,
    DEFAULT_CURRENCY_CODE,
    importProvidersFrom,
} from '@angular/core';
import { environment } from './environments/environment';
import { getRemoteServiceBaseUrl, appInitializerFactory, getCurrencyCode, getCurrentLanguage } from './root.module';
import { API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { PlatformLocation } from '@angular/common';
import { NgxSpinnerModule } from 'ngx-spinner';
import { BrowserModule, bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { AppModule } from './app/app.module';
import { LotteryDetectionCommonModule } from '@shared/common/common.module';
import { ServiceProxyModule } from '@shared/service-proxies/service-proxy.module';
import { withInterceptorsFromDi, provideHttpClient } from '@angular/common/http';
import { RootRoutingModule } from './root-routing.module';
import { RootComponent } from './root.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Lara from '@primeng/themes/lara';

if (environment.production) {
    enableProdMode();
}

const bootstrap = () =>
    bootstrapApplication(RootComponent, {
        providers: [
            importProvidersFrom(
                BrowserModule,
                AppModule,
                LotteryDetectionCommonModule.forRoot(),
                ServiceProxyModule,
                RootRoutingModule,
                NgxSpinnerModule
            ),
            { provide: API_BASE_URL, useFactory: getRemoteServiceBaseUrl },
            {
                provide: APP_INITIALIZER,
                useFactory: appInitializerFactory,
                deps: [Injector, PlatformLocation],
                multi: true,
            },
            {
                provide: LOCALE_ID,
                useFactory: getCurrentLanguage,
            },
            {
                provide: DEFAULT_CURRENCY_CODE,
                useFactory: getCurrencyCode,
                deps: [Injector],
            },
            provideAnimations(),
            provideHttpClient(withInterceptorsFromDi()),
            provideAnimationsAsync(),
            providePrimeNG({
                theme: {
                    preset: Lara,
                },
            }),
        ],
    });

bootstrap();
