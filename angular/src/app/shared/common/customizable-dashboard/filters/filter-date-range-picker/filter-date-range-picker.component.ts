import { Component, Injector } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DateTime } from 'luxon';
import { BsDaterangepickerInputDirective, BsDaterangepickerDirective } from 'ngx-bootstrap/datepicker';
import { DateRangePickerLuxonModifierDirective } from '../../../../../../shared/utils/date-time/date-range-picker-luxon-modifier.directive';
import { LuxonFormatPipe } from '../../../../../../shared/utils/luxon-format.pipe';

@Component({
    selector: 'app-filter-date-range-picker',
    templateUrl: './filter-date-range-picker.component.html',
    styleUrls: ['./filter-date-range-picker.component.css'],
    imports: [
        BsDaterangepickerInputDirective,
        BsDaterangepickerDirective,
        DateRangePickerLuxonModifierDirective,
        LuxonFormatPipe,
    ],
})
export class FilterDateRangePickerComponent extends AppComponentBase {
    date: Date;
    selectedDateRange: DateTime[] = [
        this._dateTimeService.getStartOfDayMinusDays(7),
        this._dateTimeService.getEndOfDay(),
    ];

    constructor(
        injector: Injector,
        private _dateTimeService: DateTimeService
    ) {
        super(injector);
    }

    onChange() {
        abp.event.trigger('app.dashboardFilters.dateRangePicker.onDateChange', this.selectedDateRange);
    }
}
