<#
This code is written and maintained by Darren R. Starr from Conscia Norway AS.
License :
Copyright (c) 2017 Conscia Norway AS
Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the "Software"), 
to deal in the Software without restriction, including without limitation 
the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in 
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#>

Function Invoke-ProcessPnPDevice {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',

        [Parameter()]
        [int] $HostPort = 80,
        
        [Parameter(Mandatory)]
        [PSCustomObject]$DeviceConfig,

        [Parameter()]
        [switch]$Force
    )

    Begin {
        if(
            (-not (Get-Member -InputObject 'hostname'))
        ) {
            throw [System.ArgumentException]::new(
                'No hostname specified',
                'DeviceConfig'
            )        
        }

        if(
            (-not (Get-Member -InputObject 'domainName')) 
        ) {
            throw [System.ArgumentException]::new(
                'No domain name for host (' + $DeviceConfig.hostname + ') specified',
                'DeviceConfig'
            )        
        }

        if(
            (-not (Get-Member -InputObject 'deviceType')) 
        ) {
            throw [System.ArgumentException]::new(
                'No deviceType for host (' + $DeviceConfig.hostname + ') specified',
                'DeviceConfig'
            )        
        }

        # TODO : Validate ipAddress as network prefix
    }

    Process {
        $hostParams = @{
            PnPHost = $PnPHost
            HostPort = $HostPort
        }

        $networkDevice = Get-PnPNetworkDevice @hostParams -Hostname $DeviceConfig.hostname -DomainName $DeviceConfig.domainName
        if($null -eq $networkDevice) {
            Write-Verbose -Message (
                'Device ' + $DeviceConfig.hostname + '.' + $DeviceConfig.domainName + ' does not exist, creating'
            )

            $deviceType = Get-PnPNetworkDeviceType @hostParams -Name $DeviceConfig.deviceType
            if($null -eq $deviceType) {
                throw [System.ArgumentException]::new(
                    'Invalid device type specified for host : ' + $DeviceConfig.hostName + '. Has it been defined?',
                    'DeviceConfig'
                )
            }

            $ipAddress = ''
            $network = ''
            if($null -ne (Get-Member -InputObject 'ipAddress')) {
                $ipAddress = Get-IPAddressFromNetworkPrefix -Prefix $DeviceConfig.ipAddress
                $network = Get-NetworkFromNetworkPrefix -Prefix $DeviceConfig.ipAddress
            }

            $dhcpRelay = $false
            if($null -ne $DeviceConfig.dhcpRelay) {
                $dhcpRelay = $DeviceConfig.dhcpRelay
            }

            $addSplat = [PSObject]@{
                Hostname        = $DeviceConfig.hostname 
                DomainName      = $DeviceConfig.domainName
                Description     = $DeviceConfig.description
                DeviceType      = $DeviceConfig.deviceType
                IPAddress       = $ipAddress
                Network         = $network
                DhcpRelay       = $dhcpRelay
                DhcpExclusions  = $DeviceConfig.dhcpExclusions
                DhcpTftpBootfile = $DeviceConfig.dhcpTftpBootfile
            }

            $networkDevice = Add-PnPNetworkDevice @hostParams @addSplat
            if($null -ne $networkDevice) {
                Write-Verbose -Message (
                    'Added new network device : ' + $DeviceConfig.hostname + '.' + $DeviceConfig.domainName
                )
            }
        } else {
            $updateNeeded = $false

            if(-not (Test-StringsEqual -left $networkDevice.hostname -right $DeviceConfig.hostname)) {
                $updateNeeded = $true
                Write-Debug -Message (
                    'Hostname changed from ' + $networkDevice.hostname + ' to ' + $DeviceConfig.hostname + ', update needed'
                )
            }

            if(-not (Test-StringsEqual -left $networkDevice.domainName -right $DeviceConfig.domainName)) {
                $updateNeeded = $true
                Write-Debug -Message (
                    'Domain name changed from ' + $networkDevice.domainName + ' to ' + $DeviceConfig.domainName + ', update needed'
                )
            }

            if(-not (Test-StringsEqual -left $networkDevice.description -right $DeviceConfig.description)) {
                $updateNeeded = $true
                Write-Debug -Message (
                    'Description changed from ' + $networkDevice.description+ ' to ' + $DeviceConfig.description + ', update needed'
                )
            }

            $ipAddress = ''
            $network = ''
            if($null -ne (Get-Member -InputObject 'ipAddress')) {
                $ipAddress = Get-IPAddressFromNetworkPrefix -Prefix $DeviceConfig.ipAddress
                $network = Get-NetworkFromNetworkPrefix -Prefix $DeviceConfig.ipAddress
            }

            if($networkDevice.ipAddress -ne $ipAddress) {
                $updateNeeded = $true
                Write-Debug -Message (
                    'IP address changed from ' + $networkDevice.ipAddress + ' to ' + $ipAddress + ', update needed'
                )
            }

            if($networkDevice.network -ne $network) {
                $updateNeeded = $true
                Write-Debug -Message (
                    'Network changed from ' + $networkDevice.network  + ' to ' + $network + ', update needed'
                )
            }

            $dhcpRelay = $false
            if($null -ne $DeviceConfig.dhcpRelay) {
                $dhcpRelay = $DeviceConfig.dhcpRelay
            }

            if($dhcpRelay -ne $networkDevice.dhcpRelay) {
                $updateNeeded = $true
                Write-Debug -Message (
                    'DHCP relay status has changed from ' + $networkDevice.dhcpRelay.ToString() + ' to ' + $dhcpRelay.ToString()
                )
            }

            if($updateNeeded) {
                Write-Verbose -Message (
                    'Changes detected to network device ' + $networkDevice.hostname + ', updating record'
                )
            }

            # TODO : Compare DHCP exclusions

            $updateSplat = @{
                Id              = $networkDevice.id
                Hostname        = $DeviceConfig.hostname 
                DomainName      = $DeviceConfig.domainName
                Description     = $DeviceConfig.description
                DeviceType      = $DeviceConfig.deviceType
                IPAddress       = $ipAddress
                Network         = $network
                DhcpRelay       = $dhcpRelay
                DhcpExclusions  = $DeviceConfig.dhcpExclusions
                DhcpTftpBootfile = $DeviceConfig.dhcpTftpBootfile
            }

            $networkDevice = Set-PnPNetworkDevice @hostParams @updateSplat
        }

        if($null -eq $networkDevice) {
            throw [System.Exception]::new(
                'Could not create or update network device record for ' + $DeviceConfig.hostname + ' for unknown reason, contact support'
            )
        }

        $networkDevice | Write-Verbose

        $deviceTemplates = Get-PnPNetworkDeviceTemplates @hostParams -Hostname $DeviceConfig.hostname -DomainName $DeviceConfig.domainName

        $parameters = $DeviceConfig.template.parameters
        $parameters += @(
            @{ name = 'deviceId';    value = $networkDevice.id }
            @{ name = 'hostname';    value = $networkDevice.hostname }
            @{ name = 'domainName';  value = $networkDevice.domainName }
            @{ name = 'deviceType';  value = $networkDevice.deviceType.name }
        )

        if($null -ne (Get-Member -InputObject 'ipAddress')) {
            $subnetMask = Get-SubnetMaskFromNetworkPrefix -prefix $DeviceConfig.ipAddress

            $parameters += @{
                name = 'subnetMask'
                value = $subnetMask
            }
        }

        if($null -eq $deviceTemplates) {
            Write-Debug -Message (
                'There is no device template currently stored for ' + $networkDevice.hostName
            )

            if($null -ne $DeviceConfig.template) {
                Write-Verbose -Message (
                    'A new template (' + $DeviceConfig.template.name + ') is specified for network device : ' + $networkDevice.hostName
                )

                $template = Get-PnPTemplate @hostParams -Name $DeviceConfig.template.name
                if($null -eq $template) {
                    throw [System.Exception]::new(
                        'Template (' + $DeviceConfig.template.name + ') referenced by host ' + $DeviceConfig.hostname + ' is not present.'
                    )
                }

                $setNetworkDeviceTemplateSplat = @{
                    NetworkDevice        = ($networkDevice.hostname + '.' + $networkDevice.domainName) 
                    Template             = $DeviceConfig.template.name 
                    TemplateParameters   = $parameters
                    Description          = $DeviceConfig.template.description
                }

                $networkDeviceTemplate = Set-PnPNetworkDeviceTemplate @hostParams @setNetworkDeviceTemplateSplat
            }

        } elseif($deviceTemplates.Count > 1) {
            throw [System.Exception]::new(
                'There is more than one template configured for ' + $networkDevice.hostName
            )
        } else {
            Write-Debug -Message (
                'Template ' + $deviceTemplates.template.name + ' is configured for device ' + $networkDevice.hostname
            )

            $updateNeeded = $false

            if(-not (Test-StringsEqual -left $deviceTemplates.template.name -right $DeviceConfig.template.name)) {
                Write-Debug -Message (
                    'Device (' + $networkDevice.hostname + ') template changed, update needed'
                )

                # The entire template changed, there's no point in looking at anything else

                Write-Verbose -Message (
                    'The template name has changed from ' + $deviceTemplates.template.name + ' to ' + $DeviceConfig.template.name + ', deleting the current template configuration and replacing it.'
                )

                # TODO : Handle errors on device configuration delete 
                Remove-PnPNetworkDeviceConfiguration -TemplateId $deviceTemplates.template.id -ConfigurationId $deviceTemplates.id | Out-Null

                Write-Verbose -Message (
                    'Creating device template configuration for ' + $DeviceConfig.template.name
                )

                $template = Get-PnPTemplate @hostParams -Name $DeviceConfig.template.name
                if($null -eq $template) {
                    throw [System.Exception]::new(
                        'Template (' + $DeviceConfig.template.name + ') referenced by host ' + $DeviceConfig.hostname + ' is not present.'
                    )
                }

                $setNetworkDeviceTemplateSplat = @{
                    NetworkDevice        = ($networkDevice.hostname + '.' + $networkDevice.domainName) 
                    Template             = $DeviceConfig.template.name 
                    TemplateParameters   = $parameters
                    Description          = $DeviceConfig.template.description
                }

                $networkDeviceTemplate = Set-PnPNetworkDeviceTemplate @hostParams @setNetworkDeviceTemplateSplat

                Write-Verbose -Message (
                    'Created device configuration template ' + $DeviceConfig.template.name
                )

                $deviceTemplates = [PSCustomObject[]] @($networkDeviceTemplate)
            } elseif(-not (Test-StringsEqual -left $deviceTemplates.description -right $DeviceConfig.template.description)) {
                $updateNeeded = $true
                Write-Debug -Message (
                    'Device (' + $networkDevice.hostname + ') template configuration description changed, update needed'
                )

                $setNetworkDeviceTemplateSplat = @{
                    ConfigurationId      = $deviceTemplates.id
                    Template             = $deviceTemplates.template.name
                    Description          = $DeviceConfig.template.description
                }

                $networkDeviceTemplate = Set-PnPNetworkDeviceTemplate @setNetworkDeviceTemplateSplat

                Write-Verbose -Message (
                    'Changed network device template ' + $DeviceConfig.template.name
                )

                $deviceTemplates = [PSCustomObject[]] @($networkDeviceTemplate)
            }

            $parameterChanges = Get-ChangedTemplateParameters -OldParameters $deviceTemplates.properties -NewParameters $DeviceConfig.template.parameters

            if($null -eq $parameterChanges) {
                Write-Debug -Message (
                    'There have been no changes to the template parameters'
                )
            } else {
                Write-Debug -Message 'Removing template parameters'
                foreach($removedItem in $parameterChanges.removed) {
                    Write-Debug -Message (
                        'Removing template parameter : ' + $removedItem.name
                    )
                    # TODO : Add error handling to delete process
                    Remove-PnPNetworkDeviceTemplateParameter @hostParams -ParameterId $removedItem.id | Out-Null
                }

                Write-Debug -Message 'Changing template parameters'
                foreach($changedItem in $parameterChanges.changed) {
                    Write-Debug -Message (
                        'Changing template parameter : ' + $changedItem.name + ' from ' + $changedItem.oldValue + ' to ' + $changedItem.newValue
                    )
                    $result = Set-PnPNetworkDeviceTemplateParameter -ParameterId $changedItem.id -Value $changedItem.newValue
                    # Add additional verification that the returned result is actually changed
                    if($null -eq $result) {
                        throw [System.Exception]::new(
                            'Failed to change template parameter : ' + $changedItem.name + ' from ' + $changedItem.oldValue + ' to ' + $changedItem.newValue
                        )
                    }
                }

                Write-Debug -Message 'Adding new template parameters'
                foreach($addedItem in $parameterChanges.added) {
                    Write-Debug -Message (
                        'Adding template parameter "' + $addedItem.name + '" to ' + $deviceTemplates.id + ' with value : ' + $addedItem.value
                    )
                    $result = New-PnPNetworkDeviceTemplateParameter @hostParams -ConfigurationId $deviceTemplates.id -Name $addedItem.name -Value $addedItem.value
                    # Add additional verification that the returned result is actually changed
                    if($null -eq $result) {
                        throw [System.Exception]::new(
                            'Failed to add template parameter : ' + $addedItem.name + ' value ' + $addedItem.value
                        )
                    }
                }
            }
        }
    }
}
