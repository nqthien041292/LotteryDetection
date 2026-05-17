import { BsDatepickerConfig, BsDaterangepickerConfig, BsLocaleService } from 'ngx-bootstrap/datepicker';
import { defineLocale } from 'ngx-bootstrap/chronos';
import {
    enGbLocale,
    arLocale,
    deLocale,
    esLocale,
    frLocale,
    itLocale,
    nlLocale,
    ptBrLocale,
    ruLocale,
    thLocale,
    trLocale,
    viLocale,
    zhCnLocale,
} from 'ngx-bootstrap/chronos';

export class NgxBootstrapDatePickerConfigService {
    static getDaterangepickerConfig(): BsDaterangepickerConfig {
        return Object.assign(new BsDaterangepickerConfig(), {
            containerClass: 'theme-default',
        });
    }

    static getDatepickerConfig(): BsDatepickerConfig {
        return Object.assign(new BsDatepickerConfig(), {
            containerClass: 'theme-default',
        });
    }

    static getDatepickerLocale(): BsLocaleService {
        let localeService = new BsLocaleService();
        localeService.use(abp.localization.currentLanguage.name);
        return localeService;
    }

    static registerNgxBootstrapDatePickerLocales(): Promise<boolean> {
        if (abp.localization.currentLanguage.name === 'en') {
            return Promise.resolve(true);
        }

        const locales: { [key: string]: any } = {
            'en': enGbLocale,
            'ar': arLocale,
            'de': deLocale,
            'en-gb': enGbLocale,
            'es': esLocale,
            'es-mx': esLocale,
            'fr': frLocale,
            'it': itLocale,
            'nl': nlLocale,
            'pt-br': ptBrLocale,
            'ru': ruLocale,
            'th': thLocale,
            'tr': trLocale,
            'vi': viLocale,
            'zh-hans': zhCnLocale,
        };

        let localeKey = abp.localization.currentLanguage.name.toLowerCase();

        if (locales[localeKey]) {
            defineLocale(localeKey, locales[localeKey]);
            return Promise.resolve(true);
        }

        return Promise.reject(`Locale ${localeKey} not found`);
    }
}
