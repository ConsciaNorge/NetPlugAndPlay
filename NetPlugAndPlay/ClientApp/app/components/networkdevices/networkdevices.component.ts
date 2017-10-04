import { Component, Inject } from '@angular/core';
import { Http } from '@angular/http';

import { NetworkDevice } from '../../model/networkdevice'

@Component({
    selector: 'networkdevices',
    templateUrl: './networkdevices.component.html'
})
export class NetworkDevicesComponent {
    public networkDevices: NetworkDevice[];

    constructor(http: Http, @Inject('BASE_URL') baseUrl: string) {
        http.get(baseUrl + 'api/v0/plugandplay/networkdevice').subscribe(result => {
            this.networkDevices  = result.json() as NetworkDevice[];
        }, error => console.error(error));
    }
}


