import { Component, Inject } from '@angular/core';
import { Http } from '@angular/http';

import { NetworkDeviceType } from '../../model/networkdevicetype'

@Component({
    selector: 'networkdevicetypes',
    templateUrl: './networkdevicetypes.component.html'
})
export class NetworkDeviceTypesComponent {
    public networkDeviceTypes: NetworkDeviceType[];

    constructor(http: Http, @Inject('BASE_URL') baseUrl: string) {
        http.get(baseUrl + 'api/v0/plugandplay/networkdevicetype').subscribe(result => {
            this.networkDeviceTypes = result.json() as NetworkDeviceType[];
        }, error => console.error(error));
    }
}
