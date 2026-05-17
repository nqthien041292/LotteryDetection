import { Component, Injector, ViewChild } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { OrganizationTreeComponent } from './organization-tree.component';
import { OrganizationUnitMembersComponent } from './organization-unit-members.component';
import { OrganizationUnitRolesComponent } from './organization-unit-roles.component';
import { IBasicOrganizationUnitInfo } from './basic-organization-unit-info';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { NgIf } from '@angular/common';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './organization-units.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        OrganizationTreeComponent,
        NgIf,
        TabsetComponent,
        TabDirective,
        OrganizationUnitMembersComponent,
        OrganizationUnitRolesComponent,
        LocalizePipe,
    ],
})
export class OrganizationUnitsComponent extends AppComponentBase {
    @ViewChild('ouMembers', { static: true }) ouMembers: OrganizationUnitMembersComponent;
    @ViewChild('ouRoles', { static: true }) ouRoles: OrganizationUnitRolesComponent;
    @ViewChild('ouTree', { static: true }) ouTree: OrganizationTreeComponent;
    organizationUnit: IBasicOrganizationUnitInfo = null;

    constructor(injector: Injector) {
        super(injector);
    }

    ouSelected(event: any): void {
        this.organizationUnit = event;
        this.ouMembers.organizationUnit = event;
        this.ouRoles.organizationUnit = event;
    }
}
