import { NetworkInterface } from './networkinterface'

export interface NetworkDeviceType {
    id: string;
    name: string;
    manufacturer: string;
    productId: string;
    interfaces: NetworkInterface[];
}
