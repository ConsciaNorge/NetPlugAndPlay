import { NetworkDeviceType } from './networkdevicetype';

export interface NetworkInterface {
    id: string;
    deviceType: NetworkDeviceType[];
    name: string;
    interfaceIndex: number;
}
