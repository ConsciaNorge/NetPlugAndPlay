import { NetworkDeviceType } from './networkdevicetype'
import { NetworkDeviceLink } from './networkdevicelink'

export interface NetworkDevice {
    id: string;
    deviceType: NetworkDeviceType;

    hostname: string;
    domainName: string;
    description: string;
    ipAddress: string;
    uplinks: NetworkDeviceLink[];
}
