import { Component, Inject } from '@angular/core';
import { Http } from '@angular/http';

import { NetworkDeviceReport } from '../../model/networkdevicereport'

interface TestConfigurationParameterViewModel
{
    name: string;
    value: string;
}

interface TestConfigurationViewModel {
    hostname: string;
    domainName: string;
    templateContent: string;
    generatedContent: string;
    parameters: TestConfigurationParameterViewModel[];
}

@Component({
    selector: 'networkdevices',
    templateUrl: './networkdevices.component.html'
})
export class NetworkDevicesComponent {
    public networkDevices: NetworkDeviceReport[];
    private baseUrl: string;
    public previewTemplate: TestConfigurationViewModel;

    constructor(private http: Http, @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;

        http.get(baseUrl + 'api/v0/plugandplay/networkdevice/report').subscribe(result => {
            this.networkDevices = result.json() as NetworkDeviceReport[];
        }, error => console.error(error));
    }

    testTemplate(device: NetworkDeviceReport) { 
        this.http.get(this.baseUrl + 'api/v0/plugandplay/networkdevice/' + device.id + '/configuration').subscribe(result => {
            this.previewTemplate = result.json() as TestConfigurationViewModel;
        }, error => console.error(error));
    }
}


