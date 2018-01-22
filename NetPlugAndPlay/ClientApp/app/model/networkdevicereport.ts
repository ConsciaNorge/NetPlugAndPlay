import { NetworkDevice } from './networkdevice';
import { NetworkDeviceTemplate } from './networkdevicetemplate';

export interface NetworkDeviceReport extends NetworkDevice {
    template: NetworkDeviceTemplate;
}
