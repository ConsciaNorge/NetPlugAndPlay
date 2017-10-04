import { NetworkDevice } from './networkdevice';

export interface NetworkDeviceLink {
    id: string;
    networkDevice: NetworkDevice;
    interfaceIndex: number;
    connectedToDevice: NetworkDevice;
    connectedToInterfaceIndex: number;
}
