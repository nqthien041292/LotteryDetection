import { Component, Injector, Input } from '@angular/core';
import { PermissionTreeEditModel } from '@app/admin/shared/permission-tree-edit.model';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ArrayToTreeConverterService } from '@shared/utils/array-to-tree-converter.service';
import { TreeDataHelperService } from '@shared/utils/tree-data-helper.service';
import { FlatPermissionDto } from '@shared/service-proxies/service-proxies';
import { TreeNode } from 'primeng/api';
import { forEach as _forEach, remove as _remove } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { TreeModule } from 'primeng/tree';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'permission-tree',
    templateUrl: './permission-tree.component.html',
    imports: [FormsModule, TreeModule, LocalizePipe],
})
export class PermissionTreeComponent extends AppComponentBase {
    @Input() singleSelect: boolean;
    @Input() disableCascade: boolean;

    treeData: any;
    selectedPermissions: TreeNode[] = [];
    filter = '';

    constructor(
        private _arrayToTreeConverterService: ArrayToTreeConverterService,
        private _treeDataHelperService: TreeDataHelperService,
        injector: Injector
    ) {
        super(injector);
    }

    set editData(val: PermissionTreeEditModel) {
        this.setTreeData(val.permissions);
        this.setSelectedNodes(val.grantedPermissionNames);
    }

    setTreeData(permissions: FlatPermissionDto[]) {
        this.treeData = this._arrayToTreeConverterService.createTree(
            permissions,
            'parentName',
            'name',
            null,
            'children',
            [
                {
                    target: 'key',
                    source: 'name',
                },
                {
                    target: 'key',
                    source: 'name',
                },
                {
                    target: 'label',
                    source: 'displayName',
                },
                {
                    target: 'expandedIcon',
                    value: 'fa fa-folder-open text-warning',
                },
                {
                    target: 'collapsedIcon',
                    value: 'fa fa-folder text-warning',
                },
                {
                    target: 'expanded',
                    value: true,
                },
            ]
        );
    }

    setSelectedNodes(grantedPermissionNames: string[]) {
        this.selectedPermissions = [];
        _forEach(grantedPermissionNames, (permission) => {
            let item = this._treeDataHelperService.findNode(this.treeData, { data: { name: permission } });
            if (item) {
                this.selectedPermissions.push(item);
            }
        });
    }

    getGrantedPermissionNames(): string[] {
        if (!this.selectedPermissions || !this.selectedPermissions.length) {
            return [];
        }

        let permissionNames = [];

        for (let i = 0; i < this.selectedPermissions.length; i++) {
            permissionNames.push(this.selectedPermissions[i].data.name);
        }

        return permissionNames;
    }

    nodeSelect(event) {
        if (this.singleSelect) {
            this.selectedPermissions = [event.node];
            return;
        }

        if (this.disableCascade) {
            return;
        }

        let parentNode = this._treeDataHelperService.findParent(this.treeData, {
            data: { name: event.node.data.name },
        });

        while (parentNode != null) {
            this.selectedPermissions.push(parentNode);
            parentNode = this._treeDataHelperService.findParent(this.treeData, {
                data: { name: parentNode.data.name },
            });
        }
    }

    onNodeUnselect(event) {
        if (this.disableCascade) {
            return;
        }

        let childrenNodes = this._treeDataHelperService.findChildren(this.treeData, {
            data: { name: event.node.data.name },
        });
        childrenNodes.push(event.node.data.name);
        _remove(this.selectedPermissions, (x) => childrenNodes.indexOf(x.data.name) !== -1);
    }

    filterPermissions(event): void {
        this.filterPermission(this.treeData, this.filter);
    }

    filterPermission(nodes, filterText): any {
        _forEach(nodes, (node) => {
            const matchesFilter = node.data.displayName.toLowerCase().includes(filterText.toLowerCase());

            if (matchesFilter) {
                node.styleClass = '';
                this.showParentNodes(node);

                if (node.children) {
                    _forEach(node.children, (child) => {
                        child.styleClass = '';
                    });
                }
            } else {
                node.styleClass = 'hidden-tree-node';

                if (node.children) {
                    const childMatches = this.filterPermission(node.children, filterText);

                    if (childMatches) {
                        node.styleClass = '';
                    }
                }
            }
        });

        return nodes.some((n) => !n.styleClass || n.styleClass === '');
    }

    showParentNodes(node): void {
        if (!node.parent) {
            return;
        }

        node.parent.styleClass = '';
        this.showParentNodes(node.parent);
    }
}
