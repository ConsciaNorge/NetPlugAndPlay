import { NetworkDevice } from './networkdevice';
import { Template } from './template';
import { TemplateProperty } from './templateproperty';

export interface TemplateConfiguration {
    id: string;
    template: Template;
    networkDevice: NetworkDevice;
    description: string;
    properties: TemplateProperty[];
}
