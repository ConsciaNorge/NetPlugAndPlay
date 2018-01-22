import { TemplateProperty } from './templateproperty';

export interface NetworkDeviceTemplate {
    id: string;
    name: string;
    description: string;
    parameters: TemplateProperty[];
}
