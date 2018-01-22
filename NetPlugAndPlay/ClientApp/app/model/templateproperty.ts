import { TemplateConfiguration } from './templateconfiguration';

export interface TemplateProperty {
    id: string;
    templateConfiguration: TemplateConfiguration;
    name: string;
    value: string;
}
