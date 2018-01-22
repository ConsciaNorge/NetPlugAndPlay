. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Get-InterfaceNameFromParts.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Get-IPAddressFromNetworkPrefix.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Get-IPAsUInt32.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Get-NetworkFromNetworkPrefix.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Get-NetworkInterfaceParts.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Get-NetworkInterfaceRangeNames.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Get-NetworkPrefixParts.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Get-SubnetMaskFromNetworkPrefix.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Get-SubnetMaskFromPrefixLength.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Test-CollectionNullOrEmpty.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Test-IsIP.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Test-IsNetworkPrefix.ps1')
. (Join-Path -Path $PSScriptRoot -ChildPath 'HelperScripts/Test-StringsEqual.ps1')

Function Get-ListNetworkDeviceTypeInterfaceChanges
{
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ExistingDeviceType,

        [Parameter(Mandatory)]
        [PSCustomObject]$DeviceType
    )

    # If there were no interfaces and are no interfaces, then just return null
    if(
        (Test-CollectionNullOrEmpty -value $ExistingDeviceType.interfaces) -and
        (Test-CollectionNullOrEmpty -value $DeviceType.interfaces)
    ) {
        return $null
    }

    # If there were interfaces and now there are none, then remove all
    if(
        (-not (Test-CollectionNullOrEmpty -value $ExistingDeviceType.interfaces)) -and
        (Test-CollectionNullOrEmpty -value $DeviceType.interfaces)
    ) {
        return @{
            existingInterfaces = $ExistingDeviceType.interfaces
            interfaces = $null
            toAdd = $null
            toRemove = [PSCustomObject[]]$ExistingDeviceType.interfaces
            toChange = $null
        }
    }

    # Generate a list of interfaces from the new device type settings
    $interfaceList = @()
    $DeviceType.interfaces.ForEach({
        $interfaceList += Get-NetworkInterfaceRangeNames -FirstInterfaceName $_.start -FirstInterfaceIndex $_.firstIndex -Count $_.count
    })
    $existingInterfaceList = $ExistingDeviceType.interfaces

    # If there weren't any interfaces and now there are, then add them all
    if(
        (Test-CollectionNullOrEmpty -value $ExistingDeviceType.interfaces) -and
        (-not (Test-CollectionNullOrEmpty -value $interfaceList))
    ) {
        return @{
            existingInterfaces = $null
            interfaces = $interfaceList
            toAdd = [PSCustomObject[]]$interfaceList
            toRemove = $null
            toChange = $null
        }
    }

    # Identify the items which need to be added to the system
    [PSCustomObject[]]$interfacesToAdd = $interfaceList | Where-Object {
        $interface = $_
        $null -eq ($existingInterfaceList | Where-Object {
            $_.name -ilike $interface.name
        })
    }

    # Identify the items which need to be removed from the system
    [PSCustomObject[]]$interfacesToRemove = $existingInterfaceList | Where-Object {
        $interface = $_
        $null -eq ($interfaceList | Where-Object {
            $_.name -ilike $interface.name
        })
    }

    # Identify network device types to change
    [PSCustomObject[]]$interfacesToChange = $existingInterfaceList | Where-Object {
        $interface = $_
        $null -ne ($interfaceList | Where-Object {
            ($_.name -ilike $interface.name) -and
            ($_.interfaceIndex -ne $interface.interfaceIndex)
        })
    }

    # If there were no changes, return null
    if(
        ($null -eq $interfacesToAdd) -and
        ($null -eq $interfacesToRemove) -and
        ($null -eq $interfacesToChange)
    ) {
        return $null
    }

    # Return gathered data
    return @{
        existingInterfaces = $existingInterfaceList
        interfaces = $interfaceList
        toAdd = $interfacesToAdd
        toRemove = $interfacesToRemove
        toChange = $interfacesToChange
    }
}

Function Get-ListNetworkDeviceTypeChanges
{
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ExistingDeviceType,

        [Parameter(Mandatory)]
        [PSCustomObject]$DeviceType
    )

    $fieldsChanged = $()
    if(-not ($ExistingDeviceType.name -ilike $DeviceType.name)) { $fieldsChanged += 'name' }
    if(-not ($ExistingDeviceType.manufacturer -ilike $DeviceType.manufacturer)) { $fieldsChanged += 'manufacturer' }
    if(-not ($ExistingDeviceType.productId -ilike $DeviceType.productId)) { $fieldsChanges += 'productId' }

    $interfaceChanges = Get-ListNetworkDeviceTypeInterfaceChanges -ExistingDeviceType $ExistingDeviceType -DeviceType $DeviceType

    # If there were no changes, return null
    if(
        ($fieldsChanged.Count -eq 0) -and
        ($null -eq $interfaceChanges)
    ) {
        return $null
    }

    # Return gathered information
    return @{
        deviceType = $DeviceType
        existingDeviceType = $ExistingDeviceType
        changedFields = $fieldsChanged
        interfaceChanges = $interfaceChanges
    }
}

Function Get-NetworkDeviceTypeChanges
{
    Param(
        [Parameter(Mandatory)]
        [Uri]$baseUri,

        [Parameter(Mandatory)]
        [string]$fileRoot,

        [Parameter(Mandatory)]
        [string[]]$networkDevicePaths
    )

    # Read all the network device type files into an array
    [PSCustomObject[]]$networkDeviceTypes = $networkDevicePaths.ForEach({
        $fileContent = Get-Content -Path (Join-Path -Path $fileRoot -ChildPath $_) -raw
        $script = [ScriptBlock]::Create($fileContent)
        [PSCustomObject](Invoke-Command -ScriptBlock $script)
    })

    # Get the currently configured network device types
    # TODO : If this list gets too big, it could be memory and bandwidth heavy.
    $uri = [Uri]::new($baseUri, 'api/v0/plugandplay/networkdevicetype')
    $existingDeviceTypes = Invoke-RestMethod -Method Get -Uri $uri.ToString() -UseBasicParsing

    # Identify the items which need to be added to the system
    $networkDeviceTypesToAdd = $networkDeviceTypes | Where-Object {
        $productId = $_.productId
        $null -eq ($existingDeviceTypes | Where-Object {
            $_.productId -ilike $productId
        })
    }

    # Identify the items which need to be removed from the system
    $networkDeviceTypesToRemove = $existingDeviceTypes | Where-Object {
        $productId = $_.productId
        $null -eq ($networkDeviceTypes | Where-Object {
            $_.productId -ilike $productId
        })
    }

    # TODO : Identify all the items which have been modified (check interfaces for example)
    # Identify network device types to change
    $networkDeviceTypesToChange = @()
    $networkDeviceTypes | ForEach-Object {
        $deviceType = $_
        $existingDeviceType = $existingDeviceTypes | Where-Object {
            ($_.productId -ilike $deviceType.productId)
        }
        
        if($null -ne $existingDeviceType) {
            $changes = Get-ListNetworkDeviceTypeChanges -ExistingDeviceType $existingDeviceType -DeviceType $deviceType
         
            if($null -ne $changes) {
                $networkDeviceTypesToChange += $changes
            }
        }
    }
    
    if($networkDeviceTypesToChange.Count -eq 0) {
        $networkDeviceTypesToChange = $null
    }

    # Return all the gathered information
    return @{
        deviceTypes = $networkDeviceTypes
        existingDeviceTypes = $existingDeviceTypes
        toAdd = $networkDeviceTypesToAdd
        toRemove = $networkDeviceTypesToRemove
        toChange = $networkDeviceTypesToChange
    }
}

Function Get-TemplateChanges
{
    Param(
        [Parameter(Mandatory)]
        [Uri]$baseUri,

        [Parameter(Mandatory)]
        [string]$fileRoot,

        [Parameter(Mandatory)]
        [string[]]$templatePaths        
    )

    # Read each template description file and load corresponding template contents
    $templates = $templatePaths.ForEach({
        $filePath = Join-Path -Path $fileRoot -ChildPath $_
        $fileBasePath = [IO.Path]::GetDirectoryName($filePath) 
        
        $fileContent = Get-Content -Path $filePath -raw

        $script = [ScriptBlock]::Create($fileContent)
        $items = Invoke-Command -ScriptBlock $script
        $items.ForEach({
            $contentPath = Join-Path -Path $fileBasePath -ChildPath $_.path
            $templateContent = (Get-Content -Path $contentPath -raw).Trim()

            [PSCustomObject]@{
                name = $_.name
                content = $templateContent
            }
        })
    })

    # Get the currently configured templates
    # TODO : If this list gets too big, it could be memory and bandwidth heavy.
    $uri = [Uri]::new($baseUri, 'api/v0/plugandplay/template')
    $existingTemplates = Invoke-RestMethod -Method Get -Uri $uri.ToString() -UseBasicParsing

    # Identify the items which need to be added to the system
    $templatesToAdd = $templates |Where-Object {
        $name = $_.name
        $null -eq ($existingTemplates | Where-Object {
            $_.name -ilike $name
        })
    }

    # Identify the items which need to be removed from the system
    $templatesToRemove = $existingTemplates | Where-Object {
        $name = $_.name
        $null -eq ($templates | Where-Object {
            $_.name -ilike $name
        })
    }

    # Identify templates whose contents have changed
    $templatesToChange = $templates | Where-Object {
        $template = $_
        $existingTemplates | Where-Object {
            ($_.name -eq $template.name) -and
            ($_.content.Trim() -ne $template.content)
        }
    }

    # Return all the gathered information
    return @{
        templates = $templates
        existingTemplates = $existingTemplates
        toAdd = $templatesToAdd
        toRemove = $templatesToRemove
        toChange = $templatesToChange
    }
}

Function Get-Sites
{
    Param(
        [Parameter(Mandatory)]
        [string]$fileRoot,

        [Parameter(Mandatory)]
        [string[]]$sitesPaths        
    )

    # Read sites files and get a list of the individual site files
    $siteFiles = $sitesPaths.ForEach({
        $sitesFilePath = Join-Path -Path $fileRoot -ChildPath $_
        $sitesFileBasePath = [IO.Path]::GetDirectoryName($sitesFilePath) 
        
        $sitesFileContent = Get-Content -Path $sitesFilePath -raw

        $script = [ScriptBlock]::Create($sitesFileContent)
        $files = Invoke-Command -ScriptBlock $script

        $files.ForEach({
            Join-Path -Path $sitesFileBasePath -ChildPath $_
        })
    })

    # Read all the sites from their respective files
    $sites = $siteFiles.ForEach({
        $siteFileBasePath = [IO.Path]::GetDirectoryName($_) 
        
        $siteFileContent = Get-Content -Path $_ -raw

        $script = [ScriptBlock]::Create($siteFileContent)
        Invoke-Command -ScriptBlock $script
    })

    # Return all gathered information
    return $sites
}

Function Get-BoolOrFalse
{
    Param(
        $Value
    )
    if(($null -eq $value) -or ($value -eq $false)) {
        return $false
    }

    return $true
}

Function Get-NetworkDeviceTemplateParameterChanges
{
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ExistingDevice,

        [Parameter(Mandatory)]
        [PSCustomObject]$Device
    )

    # If there were no parameters before or after, then return null
    if(($null -eq $ExistingDevice.template.parameters) -and ($null -eq $Device.template.parameters)) { return $null }

    # If there were no parameters before but are now, then add all new parameters
    if($null -eq $ExistingDevice.template.parameters) { 
        return @{
            parameters = $Device.template.parameters
            existingParameters = $ExistingDevice.template.parameters
            toAdd = $Device.template.parameters
            toRemove = $null
            toChange = $null
        }
    }

    # If there were parameters before but are none now, then remove all
    if($null -eq $Device.template.parameters) { 
        return @{
            parameters = $Device.template.parameters
            existingParameters = $ExistingDevice.template.parameters
            toAdd = $null
            toRemove = $ExistingDevice.template.parameters
            toChange = $null
        }
    }

    # Identify the items which need to be added to the system
    $parametersAdded = $Device.template.parameters | Where-Object {
        $parameter = $_
        $null -eq ($ExistingDevice.template.parameters | Where-Object {
            ($_.name -ilike $parameter.name) 
        })
    }

    # Identify the items which need to be removed from the system
    # note : Explicitly skipping deviceId as it is not something to remove
    $parametersRemoved = $ExistingDevice.template.parameters | Where-Object {
        $parameter = $_
        ($_.name -ne 'deviceId') -and 
        (
            $null -eq ($Device.template.parameters | Where-Object {
                ($_.name -ilike $parameter.name) 
            })
        )
    }

    # Identify templates whose contents have changed
    # TODO : Create objects which specify the changes
    $parametersChanged = $ExistingDevice.template.parameters | Where-Object {
        $parameter = $_
        $null -ne ($Device.template.parameters | Where-Object {
            ($_.name -ilike $parameter.name) -and
            (-not ($_.value -ilike $parameter.value))
        })
    }

    # If there are no changes, then return null
    if(
        ($null -eq $parametersAdded) -and
        ($null -eq $parametersRemoved) -and
        ($null -eq $parametersChanged)
    ) {
        return $null
    }

    # Return gathered information
    return @{
        parameters = $Device.template.parameters
        existingParameters = $ExistingDevice.template.parameters
        toAdd = $parametersAdded
        toRemove = $parametersRemoved
        toChange = $parametersChanged
    }
}

Function Get-NetworkDeviceTemplateChanges
{
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ExistingDevice,

        [Parameter(Mandatory)]
        [PSCustomObject]$Device
    )

    # If there was no template and still is no template, return null
    if(
        (Test-CollectionNullOrEmpty -value $ExistingDevice.template) -and 
        (Test-CollectionNullOrEmpty -value $Device.template)
    ) { 
        return $null 
    }

    # If there was no template and now there is, add everything
    if(
        (Test-CollectionNullOrEmpty -value $ExistingDevice.template) -and 
        (-not (Test-CollectionNullOrEmpty -value $Device.template))
    ) { 
        $result = @{
            template = $Device.template
            existingTemplate = $ExistingDevice.template
            changedFields = @('name', 'description')
            parameters = $null
        }

        if($null -ne $Device.template.parameterChanges) {
            $result.template.parameters = @{
                parameters = $Device.template.parameters
                existingParameters = $null
                toAdd = $Device.template.parameters
                toRemove = $null
                toChange = $null
            }
        }

        return $result
    }

    # If there was a template and now there isn't, then remove everything
    if(
        (-not (Test-CollectionNullOrEmpty -value $ExistingDevice.template)) -and 
        (Test-CollectionNullOrEmpty -value $Device.template)
    ) { 
        $result = @{
            template = $Device.template
            existingTemplate = $ExistingDevice.template
            changedFields = @('name', 'description')
            parameters = $null
        }

        if($null -ne $ExistingDevice.template.parameters) {
            $result.template.parameterChanges = @{
                parameters = $null
                existingParameters = $ExistingDevice.template.parameters
                toAdd = $null
                toRemove = $ExisitngDevice.template.parameters
                toChange = $null
            }
        }

        return $result
    }


    # Identify changed fields
    $fieldChanged = @()
    if(($null -eq $ExistingDevice.template) -or ($null -eq $Device.template)) { $fieldChanged += 'template'; return }

    if(-not ($ExistingDevice.template.name -ilike $Device.template.name)) { $fieldChanged += 'name' }
    if(-not ($ExistingDevice.template.description -ilike $Device.template.description)) { $fieldChanged += 'description' }

    # If no fields are changed, then nullify the list    
    if(
        ($fieldChanged.Count -eq 0) 
    ) {
        $fieldChanged = $null
    }

    # Identify changed parameters
    $parameterChanges = Get-NetworkDeviceTemplateParameterChanges -ExistingDevice $ExistingDevice -Device $Device

    # If there are no changes, return null
    if(
        ($null -eq $fieldChanged) -and
        ($null -eq $parameterChanges)
    ) {
        return $null
    }

    # Return gathered changes
    return @{
        changedFields = $fieldChanged
        parameterChanges = $parameterChanges
    }
}

Function Get-NetworkDeviceDHCPExclusionChanges
{
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ExistingDevice,

        [Parameter(Mandatory)]
        [PSCustomObject]$Device
    )

    # If there were no exclusions before and there are none now, return null
    if(
        (Test-CollectionNullOrEmpty -value $ExistingDevice.dhcpExclusions) -and 
        (Test-CollectionNullOrEmpty -value $Device.dhcpExclusions)
    ) { 
        return $null 
    }

    # If there were no exclusions before add all the new exclusions
    if(
        (Test-CollectionNullOrEmpty -value $ExistingDevice.dhcpExclusions) -and 
        (-not (Test-CollectionNullOrEmpty -value $Device.dhcpExclusions))
    ) {
        return @{
            exclusions = $Device.dhcpExclusions
            existingExclusions = $ExistingDevice.dhcpExclusions
            toAdd = $Device.dhcpExclusions
            toRemove = $null
        }
    }

    # If there were are no exclusions after then remove all the old exclusions
    if(
        (-not (Test-CollectionNullOrEmpty -value $ExistingDevice.dhcpExclusions)) -and 
        (Test-CollectionNullOrEmpty -value $Device.dhcpExclusions)
    ) {
        return @{
            exclusions = $Device.dhcpExclusions
            existingExclusions = $ExistingDevice.dhcpExclusions
            toAdd = $null
            toRemove = $ExistingDevice.dhcpExclusions
        }
    }

    # Identify the items which need to be added to the system
    $exclusionsAdded = $Device.dhcpExclusions | Where-Object {
        $exclusion = $_
        $null -eq ($ExistingDevice.dhcpExclusions | Where-Object {
            ($_.start -ilike $exclusion.start) -and
            ($_.end -ilike $exclusion.end)
        })
    }

    # Identify the items which need to be removed from the system
    # note : Explicitly skipping deviceId as it is not something to remove
    $exclusionsRemoved = $ExistingDevice.dhcpExclusions | Where-Object {
        $exclusion = $_
        $null -eq ($Device.dhcpExclusions | Where-Object {
            ($_.start -ilike $exclusion.start) -and
            ($_.end -ilike $exclusion.end)
        })
    }

    if(
        ($null -eq $exclusionsAdded) -and
        ($null -eq $exclusionsRemoved)
    ) {
        return $null
    }

    return @{
        exclusions = $Device.dhcpExclusions
        existingExclusions = $ExistingDevice.dhcpExclusions
        toAdd = $exclusionsAdded
        toRemove = $exclusionsRemoved
    }
}

Function Get-NetworkDeviceChangeList
{
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ExistingDevice,

        [Parameter(Mandatory)]
        [PSCustomObject]$Device
    )

    # Identify changed fields
    $fieldChanged = @()
    if(-not ($ExistingDevice.hostname -ilike $Device.hostname)) { $fieldChanged += 'hostname' }
    if(-not ($ExistingDevice.domainName -ilike $Device.domainName)) { $fieldChanged += 'domainName' }
    if(-not ($ExistingDevice.deviceType.name -ilike $Device.deviceType)) { $fieldChanged += 'deviceType' }
    if(-not ($ExistingDevice.network -ilike (Get-NetworkFromNetworkPrefix -Prefix $Device.ipAddress))) { $fieldChanged += 'network' }
    if(-not ($ExistingDevice.ipAddress -ilike (Get-NetworkPrefixParts -Prefix $Device.ipAddress)[0])) { $fieldChanged += 'ipAddress' }
    if(-not ($ExistingDevice.description -ilike $Device.description)) { $fieldChanged += 'description' }
    if(-not ($ExistingDevice.dhcpRelay -eq (Get-BoolOrFalse -Value $Device.dhcpRelay))) { $fieldChanged += 'dhcpRelay' }
    if(-not ($ExistingDevice.dhcpTftpBootfile -ilike $Device.dhcpTftpBootfile)) { $fieldChanged += 'dhcpTftpBootfile' }

    # Identify changes to the template configuration
    $templateChanges = (Get-NetworkDeviceTemplateChanges -ExistingDevice $ExistingDevice -Device $Device)
    
    # Identify changes to the DHCP exclusions
    $exclusionChanges = (Get-NetworkDeviceDHCPExclusionChanges -ExistingDevice $ExistingDevice -Device $Device)

    # If there are no field changes nullify the list
    if(
        ($fieldChanged.Count -eq 0)
    ) {
        $fieldChanged = $null
    }

    # If there are no changes, return null
    if(
        ($null -eq $fieldChanged) -and
        ($null -eq $templateChanges) -and
        ($null -eq $exclusionChanges)
    ) {
        return $null
    }

    # Return gathered changes
    return @{
        device = $Device
        existingDevice = $ExistingDevice
        changedFields = $fieldChanged
        templateChanges = $templateChanges
        exclusionChanges = $exclusionChanges
    }
}

Function Test-NetworkDeviceChanged
{
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ExistingDevice,

        [Parameter(Mandatory)]
        [PSCustomObject]$Device
    )

    return (
        $null -eq (Get-NetworkDeviceChangeList -ExistingDevice $ExistingDevice -Device $Device)
    )
}

Function Get-NetworkDeviceChanges
{
    Param(
        [Parameter(Mandatory)]
        [Uri]$baseUri,

        [Parameter(Mandatory)]
        [PSCustomObject[]]$sites
    )

    # Gather all network devices from all sites into a single list
    $devices = $sites.ForEach({
        $_.devices
    })

    # Add standard parameters for each device which should be used with templates
    $devices.ForEach({
        if($null -ne $_.template)
        {
            $parameters = $_.template.parameters
            if($null -eq $parameters) {
                $parameters = @()
            }

            $parameters += @(
                @{ name = 'hostname';    value = $_.hostname }
                @{ name = 'domainName';  value = $_.domainName }
                @{ name = 'deviceType';  value = $_.deviceType }
            )

            if($null -ne (Get-Member -InputObject 'ipAddress')) {
                $subnetMask = Get-SubnetMaskFromNetworkPrefix -prefix $_.ipAddress

                $parameters += @{
                    name = 'subnetMask'
                    value = $subnetMask
                }
            }

            $_.template.parameters = $parameters
        }
    })

    # Get the currently configured devices
    # TODO : If this list gets too big, it could be memory and bandwidth heavy.
    $uri = [Uri]::new($baseUri, 'api/v0/plugandplay/networkdevice/report')
    $existingDevices = Invoke-RestMethod -Method Get -Uri $uri.ToString() -UseBasicParsing

    # Identify the items which need to be added to the system
    $devicesToAdd = $devices | Where-Object {
        $device = $_
        $null -eq ($existingDevices | Where-Object {
            ($_.hostname -ilike $device.hostname) -and
            ($_.domainName -ilike $device.domainName)
        })
    }

    # Identify the items which need to be removed from the system
    $devicesToRemove = $existingDevices | Where-Object {
        $device = $_
        $null -eq ($devices | Where-Object {
            ($_.hostname -ilike $device.hostname) -and
            ($_.domainName -ilike $device.domainName)
        })
    }

    # Identify templates whose contents have changed
    $devicesToChange = @()
    $devices | ForEach-Object {
        $device = $_
        $existingDevice = $existingDevices | Where-Object {
            ($_.hostname -ilike $device.hostname) -and
            ($_.domainName -ilike $device.domainName)
        }
        
        if($null -ne $existingDevice) {
            $changes = Get-NetworkDeviceChangeList -ExistingDevice $existingDevice -Device $device
            if($null -ne $changes) {
                $devicesToChange += $changes
            }
        }
    }
    
    if($devicesToChange.Count -eq 0) {
        $devicesToChange = $null
    }

    # Return all gathered information
    return @{
        networkDevices = $devices
        existingDevices = $existingDevices
        toAdd = $devicesToAdd
        toRemove = $devicesToRemove
        toChange = $devicesToChange
    }
}

Function Get-ConnectionChanges
{
    Param(
        [Parameter(Mandatory)]
        [Uri]$baseUri,

        [Parameter(Mandatory)]
        [PSCustomObject[]]$sites
    )

    # Gather all connections into a single list
    $connections = $sites.ForEach({
        $_.connections
    })

    # Get the currently configured connections
    # TODO : If this list gets too big, it could be memory and bandwidth heavy.
    $uri = [Uri]::new($baseUri, 'api/v0/plugandplay/networkdevice/uplinks')
    $existingConnections = Invoke-RestMethod -Method Get -Uri $uri.ToString() -UseBasicParsing

    # Identify the items which need to be added to the system
    $connectionsToAdd = $connections | Where-Object {
        $connection = $_
        $null -eq ($existingConnections | Where-Object {
            ($_.domainName -ilike $connection.domainName) -and
            ($_.networkDevice -ilike $connection.networkDevice) -and
            ($_.interface -ilike $connection.interface) -and
            ($_.uplinkToDevice -ilike $connection.uplinkToDevice) -and
            ($_.uplinkToInterface -ilike $connection.uplinkToInterface)
        })
    }

    # Identify the items which need to be removed from the system
    $connectionsToRemove = $existingConnections | Where-Object {
        $connection = $_
        $null -eq ($connections | Where-Object {
            ($_.domainName -ilike $connection.domainName) -and
            ($_.networkDevice -ilike $connection.networkDevice) -and
            ($_.interface -ilike $connection.interface) -and
            ($_.uplinkToDevice -ilike $connection.uplinkToDevice) -and
            ($_.uplinkToInterface -ilike $connection.uplinkToInterface)
        })
    }

    # If there have been no changes, return null
    if(
        (Test-CollectionNullOrEmpty -value $connectionsToAdd) -and
        (Test-CollectionNullOrEmpty -value $connectionsToRemove)
    ) {
        return $null
    }

    return @{
        connections = $connections
        existingConnections = $existingConnections
        toAdd = $connectionsToAdd
        toRemove = $connectionsToRemove
    }
}

function Get-TFTPFileChanges
{
    Param(
        [Parameter(Mandatory)]
        [Uri]$baseUri,

        [Parameter(Mandatory)]
        [string]$fileRoot,

        [Parameter(Mandatory)]
        [string[]]$filesPaths        
    )

    # Read files files and get a list of the individual files files
    $files = $filesPaths.ForEach({
        $filesFilePath = Join-Path -Path $fileRoot -ChildPath $_
        $filesFileBasePath = [IO.Path]::GetDirectoryName($filesFilePath) 
        
        $filesFileContent = Get-Content -Path $filesFilePath -raw

        $script = [ScriptBlock]::Create($filesFileContent)
        $files = Invoke-Command -ScriptBlock $script

        $files.ForEach({
            $contentPath = Join-Path -Path $filesFileBasePath -ChildPath $_.path
            $fileContent = (Get-Content -Path $contentPath -Raw).Trim()
            @{
                name = $_.name
                content = $fileContent
            }
        })
    })

    # Get the currently stored TFTP files
    # TODO : If this list gets too big, it could be memory and bandwidth heavy.
    $uri = [Uri]::new($baseUri, 'api/v0/tftp/files/report')
    $existingFiles = Invoke-RestMethod -Method Get -Uri $uri.ToString() -UseBasicParsing

    # Identify the items which need to be added to the system
    $filesToAdd = $files | Where-Object {
        $file = $_
        $null -eq ($existingFiles | Where-Object {
            ($_.filePath -ilike $file.name) 
        })
    }

    # Identify the items which need to be removed from the system
    $filesToRemove = $existingFiles | Where-Object {
        $file = $_
        $null -eq ($files | Where-Object {
            ($_.name -ilike $file.filePath) 
        })
    }

    # Identify items whose contents have changed
    $filesToChange = $existingFiles | Where-Object {
        $file = $_
        $null -ne ($files | Where-Object {
            ($_.name -ilike $file.filePath) -and
            ($_.content -ne $file.content.Trim())
        })
    }

    # If nothing has changed, then return null
    if(
        ($null -eq $filesToAdd) -and
        ($null -eq $filesToRemove) -and
        ($null -eq $filesToChange)
    ) {
        return $null
    }

    # Return gathered information
    return @{
        files = $files
        existingFiles = $existingFiles
        toAdd = $filesToAdd
        toRemove = $filesToRemove
        toChange = $filesToChange
    }
}

function Get-DesignChanges
{
    param(
        [Parameter(Mandatory)]
        [string]$designFile,

        [Parameter(Mandatory)]
        [Uri]$apiBaseUri
    )

    $designRoot = [IO.Path]::GetDirectoryName($designFile)

    # Load the design file
    $design = Invoke-Command -ScriptBlock ([ScriptBlock]::Create((Get-Content -Path $designFile -Raw)))
    
    # Identify changes to the network device types
    $deviceTypeChanges = Get-NetworkDeviceTypeChanges -baseUri $apiBaseUri -fileRoot $designRoot -networkDevicePaths $design.networkDeviceTypes
    
    # Identify changes to templates
    $templateChanges = Get-TemplateChanges -baseUri $apiBaseUri -fileRoot $designRoot -templatePaths $design.templates
    
    # Load the site configurations
    $sites = Get-Sites -fileRoot $designRoot -sitesPaths $design.sites
    
    # Identify changes to the network devices
    $networkDeviceChanges = Get-NetworkDeviceChanges -baseUri $apiBaseUri -Sites $sites
    
    # Identify changes to the connections between network devices
    $connectionChanges = Get-ConnectionChanges -baseUri $apiBaseUri -Sites $sites
    
    # Identify changes to TFTP files
    $tftpFilesChanges = Get-TFTPFileChanges -baseUri $apiBaseUri -fileRoot $designRoot -filesPaths $design.files

    # Return the gathered information
    return @{
        design = $design
        deviceTypeChanges = $deviceTypeChanges
        templateChanges = $templateChanges
        sites = $sites
        networkDeviceChanges = $networkDeviceChanges
        connectionChanges = $connectionChanges
        tftpFileChanges = $tftpFilesChanges
    }
}

function Show-DeviceTypeChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Information -MessageData 'Network device types'
    Write-Information -MessageData '==================================================='

    if($null -eq $changes.deviceTypeChanges) {
        Write-Information -MessageData 'No changes'
    }

    if($null -ne $changes.deviceTypeChanges.toAdd) {
        Write-Information -MessageData 'Additions'
        Write-Information -MessageData '---------------------------------'
        
        $changes.deviceTypeChanges.toAdd | ForEach-Object ({
            Write-Information -MessageData (' '  + $_.name)
        })
        Write-Information -MessageData ''
    }

    if($null -ne $changes.deviceTypeChanges.toRemove) {
        Write-Information -MessageData 'Removals'
        Write-Information -MessageData '---------------------------------'
        
        $changes.deviceTypeChanges.toRemove | ForEach-Object ({
            Write-Information -MessageData (' '  + $_.name)
        })
        Write-Information -MessageData ''
    }

    if($null -ne $changes.deviceTypeChanges.toChange) {
        Write-Information -MessageData 'Changed'
        Write-Information -MessageData '---------------------------------'
        
        $changes.deviceTypeChanges.toChange | ForEach-Object ({
            Write-Information -MessageData (' '  + $_.deviceType.name)

            $deviceType = $_.deviceType
            $existingDeviceType = $_.existingDeviceType

            if($null -ne $_.changedFields) {
                Write-Information -MessageData ('  Fields changed')
                $_.changedFields| ForEach-Object ({
                    Write-Information -MessageData ('   ' + $_ + ' from [' + $existingDeviceType.$_.Tostring() + '] to [' + $deviceType.$_.Tostring() + ']' )
                })
                Write-Information -MessageData ''
            }

            if($null -ne $_.interfaceChanges) {
                Write-Information -MessageData ('  Interfaces')

                if($null -ne $_.interfaceChanges.toAdd) {
                    Write-Information -MessageData ('   Added')
                    $_.interfaceChanges.toAdd | ForEach-Object ({
                        Write-Information -MessageData ('    ' + $_.name)
                    })
                }

                if($null -ne $_.interfaceChanges.toRemove) {
                    Write-Information -MessageData ('   Removed')
                    $_.interfaceChanges.toRemove | ForEach-Object ({
                        Write-Information -MessageData ('    ' + $_.name)
                    })
                }                

                if($null -ne $_.interfaceChanges.toChange) {
                    Write-Information -MessageData ('   Changed')

                    $interfaces = $_.interfaceChanges.interfaces
                    $existingInterfaces = $_.interfaceChanges.existingInterfaces

                    $_.interfaceChanges.toChange | ForEach-Object ({
                        $toChange = $_

                        $interface = $interfaces | Where-Object { $_.name -eq $toChange.name }
                        $existingInterface = $existingInterfaces | Where-Object { $_.name -eq $toChange.name }

                        Write-Information -MessageData ('    ' + $_.name)
                        Write-Information -MessageData ('     interfaceIndex from [' + $existingInterface.interfaceIndex.ToString() + '] to [' + $interface.interfaceIndex.ToString() + ']')
                    })
                }                
            }
        })
        Write-Information -MessageData ''
    }
}

function Show-TemplateChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Information -MessageData 'Templates'
    Write-Information -MessageData '==================================================='

    if($null -eq $changes.templateChanges) {
        Write-Information -MessageData 'No changes'
    }

    if($null -ne $changes.templateChanges.toAdd) {
        Write-Information -MessageData 'Added'

        $changes.templateChanges.toAdd | ForEach-Object ({
            Write-Information -MessageData (' ' + $_.name)
        })

        Write-Information -MessageData ''
    }

    if($null -ne $changes.templateChanges.toRemove) {
        Write-Information -MessageData 'Removed'

        $changes.templateChanges.toRemove | ForEach-Object ({
            Write-Information -MessageData (' ' + $_.name)
        })

        Write-Information -MessageData ''
    }

    if($null -ne $changes.templateChanges.toChange) {
        Write-Information -MessageData 'Changed'

        $changes.templateChanges.toChange | ForEach-Object ({
            Write-Information -MessageData (' ' + $_.name)
            # TODO : Consider mapping changes via diff for example
        })

        Write-Information -MessageData ''
    }
}

function Show-TftpFileChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Information -MessageData 'TFTP files'
    Write-Information -MessageData '==================================================='

    if($null -eq $changes.tftpFileChanges) {
        Write-Information -MessageData 'No changes'
    }

    if($null -ne $changes.tftpFileChanges.toAdd) {
        Write-Information -MessageData 'Added'

        $changes.tftpFileChanges.toAdd | ForEach-Object ({
            Write-Information -MessageData (' ' + $_.filePath)
        })

        Write-Information -MessageData ''
    }

    if($null -ne $changes.tftpFileChanges.toRemove) {
        Write-Information -MessageData 'Removed'

        $changes.tftpFileChanges.toRemove | ForEach-Object ({
            Write-Information -MessageData (' ' + $_.filePath)
        })

        Write-Information -MessageData ''
    }

    if($null -ne $changes.tftpFileChanges.toChange) {
        Write-Information -MessageData 'Changed'

        $changes.tftpFileChanges.toChange | ForEach-Object ({
            Write-Information -MessageData (' ' + $_.filePath)
            # TODO : Consider mapping changes via diff for example
        })

        Write-Information -MessageData ''
    }
}

function Show-NetworkDeviceChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Information -MessageData 'Network devices'
    Write-Information -MessageData '==================================================='

    if($null -eq $changes.networkDeviceChanges) {
        Write-Information -MessageData 'No changes'
    }

    if($null -ne $changes.networkDeviceChanges.toAdd) {
        Write-Information -MessageData 'Added'

        $changes.networkDeviceChanges.toAdd | ForEach-Object ({
            Write-Information -MessageData (' ' + $_.hostname + '.' + $_.domainName)
        })

        Write-Information -MessageData ''
    }

    if($null -ne $changes.networkDeviceChanges.toRemove) {
        Write-Information -MessageData 'Removed'

        $changes.networkDeviceChanges.toRemove | ForEach-Object ({
            Write-Information -MessageData (' ' + $_.hostname + '.' + $_.domainName)
        })

        Write-Information -MessageData ''
    }

    if($null -ne $changes.networkDeviceChanges.toChange) {
        Write-Information -MessageData 'Changed'

        $changes.networkDeviceChanges.toChange | ForEach-Object ({
            Write-Information -MessageData (' ' + $_.device.hostname + '.' + $_.device.domainName)

            $device = $_.device
            $existingDevice = $_.existingDevice

            if($null -ne $_.changedFields) {
                Write-Information -MessageData ('  Fields')
                $_.changedFields| ForEach-Object ({
                    switch($_)
                    {
                        # TODO : Handle IP address and network properly
                        'deviceType' {
                            Write-Information -MessageData ('   ' + $_ + ' from [' + $existingDevice.deviceType.name.Tostring() + '] to [' + $device.$_.Tostring() + ']' )
                        }
                        default {
                            Write-Information -MessageData ('   ' + $_ + ' from [' + $existingDevice.$_.Tostring() + '] to [' + $device.$_.Tostring() + ']' )
                        }
                    }
                })
                Write-Information -MessageData ''
            }

            if($null -ne $_.templateChanges) {
                Write-Information -MessageData ('  Template changes')
                
                if($null -ne $_.templateChanges.changedFields) {
                    Write-Information -MessageData ('   Fields')
                    $_.templateChanges.changedFields| ForEach-Object ({
                        Write-Information -MessageData ('    ' + $_ + ' from [' + $existingDevice.template.$_.Tostring() + '] to [' + $device.template.$_.Tostring() + ']' )
                    })
                    Write-Information -MessageData ''
                }

                if($null -ne $_.templateChanges.parameterChanges) {
                    Write-Information -MessageData ('   Parameters')

                    $parameters = $_.templateChanges.parameterChanges.parameters
                    $existingParameters = $_.templateChanges.parameterChanges.existingParameters

                    if($null -ne $_.templateChanges.parameterChanges.toAdd) {
                        Write-Information -MessageData ('    Added')

                        $_.templateChanges.parameterChanges.toAdd | ForEach-Object ({
                            Write-Information -MessageData ('     ' + $_.name + ' = [' + $_.value + ']' )
                        })
                    }

                    if($null -ne $_.templateChanges.parameterChanges.toRemove) {
                        Write-Information -MessageData ('    Removed')

                        $_.templateChanges.parameterChanges.toRemove | ForEach-Object ({
                            Write-Information -MessageData ('     ' + $_.name + ' = [' + $_.value + ']' )
                        })
                    }

                    if($null -ne $_.templateChanges.parameterChanges.toChange) {
                        Write-Information -MessageData ('    Changed')

                        $_.templateChanges.parameterChanges.toChange | ForEach-Object ({
                            $name = $_.name
                            $existingValue = $_.value
                            $value = ($parameters | Where-Object { $_.name -ilike $name }).value
                            
                            Write-Information -MessageData ('     ' + $_.name + ' from [' + $existingValue + '] to [' + $value + ']')
                        })
                    }
                    Write-Information -MessageData ''
                }
            }

            if($null -ne $_.exclusionChanges) {
                Write-Information -MessageData ('  DHCP Exclusion changes')

                if($null -ne $_.exclusionChanges.toAdd)
                {
                    Write-Information -MessageData ('   Added')

                    $_.exclusionChanges.toAdd | ForEach-Object ({
                        Write-Information -MessageData ('    start = [' + $_.start + '], end [' + $_.end + ']')
                    })

                    Write-Information -MessageData ''
                }

                if($null -ne $_.exclusionChanges.toRemove)
                {
                    Write-Information -MessageData ('   Removed')

                    $_.exclusionChanges.toRemove | ForEach-Object ({
                        Write-Information -MessageData ('    start = [' + $_.start + '], end [' + $_.end + ']')
                    })

                    Write-Information -MessageData ''
                }

                # TODO : Make a "To change" condition for when an exclusion range grows or shrinks
            }
        })

        Write-Information -MessageData ''
    }
}

function Show-NetworkConnectionChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Information -MessageData 'Network connections'
    Write-Information -MessageData '==================================================='

    if($null -eq $changes.connectionChanges) {
        Write-Information -MessageData ' No changes'
        return
    }

    if($null -ne $changes.connectionChanges.toAdd)
    {
        Write-Information -MessageData (' Added')

        $changes.connectionChanges.toAdd | ForEach-Object ({
            Write-Information -MessageData ('  [' + $_.networkDevice + '.' + $_.domainName + ' - ' + $_.interface + '] to [' + $_.uplinkToDevice + '.' + $_.domainName + ' - ' + $_.uplinkToInterface + ']' )
        })

        Write-Information -MessageData ''
    }

    if($null -ne $changes.connectionChanges.toRemove)
    {
        Write-Information -MessageData (' Removed')

        $changes.connectionChanges.toRemove | ForEach-Object ({
            Write-Information -MessageData ('  [' + $_.networkDevice + '.' + $_.domainName + ' - ' + $_.interface + '] to [' + $_.uplinkToDevice + '.' + $_.domainName + ' - ' + $_.uplinkToInterface + ']' )
        })

        Write-Information -MessageData ''
    }
}

function Show-Changes
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Show-DeviceTypeChanges -changes $changes
    Show-TemplateChanges -changes $changes
    Show-TftpFileChanges -changes $changes
    Show-NetworkDeviceChanges -changes $changes
    Show-NetworkConnectionChanges -changes $changes
}

function Get-RestDeviceTypeChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Debug 'Network device types'
    Write-Debug '==================================================='

    # If there are no changes, return null
    if($null -eq $changes.deviceTypeChanges) {
        Write-Debug 'No changes'
        return $null
    }

    $result = New-Object -TypeName PSCustomObject

    if($null -ne $changes.deviceTypeChanges.toAdd) {
        Write-Debug 'Added'
        
        [PSCustomObject[]]$toAdd = $changes.deviceTypeChanges.toAdd | ForEach-Object ({
            Write-Debug (' '  + $_.name)
            $_
        })
        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toAdd' -Value $toAdd
    }

    if($null -ne $changes.deviceTypeChanges.toRemove) {
        Write-Debug 'Removals'
        Write-Debug '---------------------------------'
        
        [Guid[]]$toRemove = $changes.deviceTypeChanges.toRemove | ForEach-Object ({
            Write-Debug (' '  + $_.name)
            $_.id
        })
        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toRemove' -Value $toRemove
    }

    if($null -ne $changes.deviceTypeChanges.toChange) {
        Write-Debug 'Changed'
        Write-Debug '---------------------------------'
        
        [PSCustomObject[]]$toChange = $changes.deviceTypeChanges.toChange | ForEach-Object ({
            Write-Debug (' '  + $_.deviceType.name)

            $deviceType = $_.deviceType
            $existingDeviceType = $_.existingDeviceType

            $item = New-Object -TypeName PSCustomObject
            Add-Member -InputObject $item -MemberType NoteProperty -Name 'id' -Value $existingDeviceType.id

            if($null -ne $_.changedFields) {
                Write-Debug ('  Fields changed')
                $_.changedFields| ForEach-Object ({
                    Write-Debug ('   ' + $_ + ' from [' + $existingDeviceType.$_.Tostring() + '] to [' + $deviceType.$_.Tostring() + ']' )

                    Add-Member -InputObject $item -MemberType NoteProperty -Name $_ -Value $deviceType.$_
                })
                Write-Debug ''
            }

            if($null -ne $_.interfaceChanges) {
                Write-Debug ('  Interfaces')

                $interfacesResult = New-Object -TypeName PSCustomObject

                if($null -ne $_.interfaceChanges.toAdd) {
                    Write-Debug ('   Added')
                    
                    [PSCustomObject[]]$interfacesToAdd = $_.interfaceChanges.toAdd | ForEach-Object ({
                        Write-Debug ('    ' + $_.name)
                        $_
                    })

                    Add-Member -InputObject $interfacesResult -MemberType NoteProperty -Name 'toAdd' -Value $interfacesToAdd
                }

                if($null -ne $_.interfaceChanges.toRemove) {
                    Write-Debug ('   Removed')
                    [Guid[]]$interfacesToRemove = $_.interfaceChanges.toRemove | ForEach-Object ({
                        Write-Debug ('    ' + $_.name)
                        $_.id
                    })

                    Add-Member -InputObject $interfacesResult -MemberType NoteProperty -Name 'toRemove' -Value $interfacesToRemove
                }                

                if($null -ne $_.interfaceChanges.toChange) {
                    Write-Debug ('   Changed')

                    $interfaces = $_.interfaceChanges.interfaces
                    $existingInterfaces = $_.interfaceChanges.existingInterfaces

                    [PSCustomObject[]]$interfacesToChange = $_.interfaceChanges.toChange | ForEach-Object ({
                        $toChange = $_

                        $interface = $interfaces | Where-Object { $_.name -eq $toChange.name }
                        $existingInterface = $existingInterfaces | Where-Object { $_.name -eq $toChange.name }

                        Write-Debug ('    ' + $_.name)
                        Write-Debug ('     interfaceIndex from [' + $existingInterface.interfaceIndex.ToString() + '] to [' + $interface.interfaceIndex.ToString() + ']')

                        @{
                            id = $existingInterface.id
                            name = $interface.name
                            interfaceIndex = $interface.interfaceIndex
                        }
                    })

                    Add-Member -InputObject $interfacesResult -MemberType NoteProperty -Name 'toChange' -Value $interfacesToChange
                }

                Add-Member -InputObject $item -MemberType NoteProperty -Name 'interfaces' -Value $interfacesResult
            }    
            $item    
        })

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toChange' -Value $toChange
        Write-Debug ''
    }

    return $result
}

function Get-RestTemplateChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Debug 'Templates'
    Write-Debug '==================================================='

    # If there are no changes, return
    if($null -eq $changes.templateChanges) {
        Write-Debug 'No changes'
        return $null
    }

    $result = New-Object -TypeName PSCustomObject

    if($null -ne $changes.templateChanges.toAdd) {
        Write-Debug 'Added'

        [PSCustomObject[]]$templatesToAdd = $changes.templateChanges.toAdd | ForEach-Object ({
            Write-Debug (' ' + $_.name)
            $_
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toAdd' -Value $templatesToAdd
    }

    if($null -ne $changes.templateChanges.toRemove) {
        Write-Debug 'Removed'

        [Guid[]]$templatesToRemove = $changes.templateChanges.toRemove | ForEach-Object ({
            Write-Debug (' ' + $_.name)
            $_.id
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toRemove' -Value $templatesToRemove
    }

    if($null -ne $changes.templateChanges.toChange) {
        Write-Debug 'Changed'

        [PSCustomObject[]]$templatesToChange = $changes.templateChanges.toChange | ForEach-Object ({
            Write-Debug (' ' + $_.name)
            # TODO : Consider mapping changes via diff for example

            $template = $_
            $existingTemplate = $changes.templateChanges.existingTemplates | Where-Object {
                $_.name -ilike $template.name
            }

            [PSCustomObject]@{
               id = $existingTemplate.id
               name = $existingTemplate.name
               content = $template.content
            }
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toChange' -Value $templatesToChange
    }

    return $result
}

function Get-RestTftpFileChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Debug 'TFTP files'
    Write-Debug '==================================================='

    # If there are no changes, then return null
    if($null -eq $changes.tftpFileChanges) {
        Write-Debug 'No changes'
        return $null
    }

    $result = New-Object -TypeName PSCustomObject

    if($null -ne $changes.tftpFileChanges.toAdd) {
        Write-Debug 'Added'

        [PSCustomObject[]]$tftpFilesToAdd = $changes.tftpFileChanges.toAdd | ForEach-Object ({
            Write-Debug (' ' + $_.name)
            $_
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toAdd' -Value $tftpFilesToAdd
    }

    if($null -ne $changes.tftpFileChanges.toRemove) {
        Write-Debug 'Removed'

        [Guid[]]$tftpFilesToRemove = $changes.tftpFileChanges.toRemove | ForEach-Object ({
            Write-Debug (' ' + $_.filePath)

            $_.id
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toRemove' -Value $tftpFilesToRemove
    }

    if($null -ne $changes.tftpFileChanges.toChange) {
        Write-Debug 'Changed'

        [PSCustomObject[]]$tftpFilesToChange = $changes.tftpFileChanges.toChange | ForEach-Object ({
            Write-Debug (' ' + $_.filePath)
            # TODO : Consider mapping changes via diff for example

            $existingFile = $_
            $file = $changes.tftpFileChanges.files | Where-Object {
                ($_.name -ilike $existingFile.filePath) 
            }

            @{
                id = $existingFile.id
                name = $existingFile.filePath
                content = $file.content
            }
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toChange' -Value $tftpFilesToChange
    }

    return $result
}

function Get-RestNetworkDeviceChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Debug 'Network devices'
    Write-Debug '==================================================='

    # If there are no changes, return null
    if($null -eq $changes.networkDeviceChanges) {
        Write-Debug 'No changes'
    }

    $result = New-Object -TypeName PSCustomObject

    if($null -ne $changes.networkDeviceChanges.toAdd) {
        Write-Debug 'Added'

        [PSCustomObject[]]$networkDevicesToAdd = $changes.networkDeviceChanges.toAdd | ForEach-Object ({
            Write-Debug (' ' + $_.hostname + '.' + $_.domainName)
            $_
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toAdd' -Value $networkDevicesToAdd
    }

    if($null -ne $changes.networkDeviceChanges.toRemove) {
        Write-Debug 'Removed'

        [Guid[]]$networkDevicesToRemove = $changes.networkDeviceChanges.toRemove | ForEach-Object ({
            Write-Debug (' ' + $_.hostname + '.' + $_.domainName)

            $_.id
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toRemove' -Value $networkDevicesToRemove
    }

    if($null -ne $changes.networkDeviceChanges.toChange) {
        Write-Debug 'Changed'

        [PSCustomObject[]]$networkDevicesToChange = $changes.networkDeviceChanges.toChange | ForEach-Object ({
            Write-Debug (' ' + $_.device.hostname + '.' + $_.device.domainName)

            $device = $_.device
            $existingDevice = $_.existingDevice

            $networkDeviceChanges = New-Object -TypeName PSCustomObject
            Add-Member -InputObject $networkDeviceChanges -MemberType NoteProperty -Name 'id' -Value $existingDevice.id

            if($null -ne $_.changedFields) {
                Write-Debug ('  Fields')
                $_.changedFields| ForEach-Object ({
                    switch($_)
                    {
                        # TODO : Handle IP address and network properly
                        'deviceType' {
                            Write-Debug ('   ' + $_ + ' from [' + $existingDevice.deviceType.name.Tostring() + '] to [' + $device.$_.Tostring() + ']' )
                        }
                        default {
                            Write-Debug ('   ' + $_ + ' from [' + $existingDevice.$_.Tostring() + '] to [' + $device.$_.Tostring() + ']' )
                        }
                    }
                    Add-Member -InputObject $networkDeviceChanges -MemberType NoteProperty -Name $_ -Value $device.$_
                })
                Write-Debug ''
            }

            if($null -ne $_.templateChanges) {
                Write-Debug ('  Template changes')
                
                $templateChanges = New-Object -TypeName PSCustomObject

                if($null -ne $_.templateChanges.changedFields) {
                    Write-Debug ('   Fields')
                    $_.templateChanges.changedFields| ForEach-Object ({
                        Write-Debug ('    ' + $_ + ' from [' + $existingDevice.template.$_.Tostring() + '] to [' + $device.template.$_.Tostring() + ']' )

                        Add-Member -InputObject $templateChanges -MemberType NoteProperty -Name $_ -Value $device.template.$_
                    })
                    Write-Debug ''
                }

                if($null -ne $_.templateChanges.parameterChanges) {
                    Write-Debug ('   Parameters')

                    $parameters = $_.templateChanges.parameterChanges.parameters
                    $existingParameters = $_.templateChanges.parameterChanges.existingParameters

                    $parameterChanges = New-Object -TypeName PSCustomObject

                    if($null -ne $_.templateChanges.parameterChanges.toAdd) {
                        Write-Debug ('    Added')

                        [PSCustomObject[]]$parametersToAdd = $_.templateChanges.parameterChanges.toAdd | ForEach-Object ({
                            Write-Debug ('     ' + $_.name + ' = [' + $_.value + ']' )
                            $_
                        })

                        Add-Member -InputObject $parameterChanges -MemberType NoteProperty -Name 'toAdd' -Value $parametersToAdd
                    }

                    if($null -ne $_.templateChanges.parameterChanges.toRemove) {
                        Write-Debug ('    Removed')

                        [Guid[]]$parametersToRemove = $_.templateChanges.parameterChanges.toRemove | ForEach-Object ({
                            Write-Debug ('     ' + $_.name + ' = [' + $_.value + ']' )
                            $_.id
                        })

                        Add-Member -InputObject $parameterChanges -MemberType NoteProperty -Name 'toRemove' -Value $parametersToRemove
                    }

                    if($null -ne $_.templateChanges.parameterChanges.toChange) {
                        Write-Debug ('    Changed')

                        [PSCustomObject[]]$parametersToChange = $_.templateChanges.parameterChanges.toChange | ForEach-Object ({
                            $name = $_.name
                            $existingValue = $_.value
                            $value = ($parameters | Where-Object { $_.name -ilike $name }).value
                            
                            Write-Debug ('     ' + $_.name + ' from [' + $existingValue + '] to [' + $value + ']')

                            [PSCustomObject]@{
                                id = $_.id
                                name = $name
                                value = $value
                            }
                        })

                        Add-Member -InputObject $parameterChanges -MemberType NoteProperty -Name 'toChange' -Value $parametersToChange
                    }

                    Write-Debug ''

                    Add-Member -InputObject $templateChanges -MemberType NoteProperty -Name 'parameters' -Value $parameterChanges
                }

                Add-Member -InputObject $networkDeviceChanges -MemberType NoteProperty -Name 'template' -Value $templateChanges
            }

            if($null -ne $_.exclusionChanges) {
                Write-Debug ('  DHCP Exclusion changes')

                $dhcpExclusionChanges = New-Object PSCustomObject

                if($null -ne $_.exclusionChanges.toAdd)
                {
                    Write-Debug ('   Added')

                    [PSCustomObject[]]$exclusionsToAdd = $_.exclusionChanges.toAdd | ForEach-Object ({
                        Write-Debug ('    start = [' + $_.start + '], end [' + $_.end + ']')
                        $_
                    })

                    Write-Debug ''

                    Add-Member -InputObject $dhcpExclusionChanges -MemberType NoteProperty -Name 'toAdd' -Value $exclusionsToAdd
                }

                if($null -ne $_.exclusionChanges.toRemove)
                {
                    Write-Debug ('   Removed')

                    [Guid[]]$exclusionsToRemove = $_.exclusionChanges.toRemove | ForEach-Object ({
                        Write-Debug ('    start = [' + $_.start + '], end [' + $_.end + ']')
                        $_.id
                    })

                    Write-Debug ''

                    Add-Member -InputObject $dhcpExclusionChanges -MemberType NoteProperty -Name 'toRemove' -Value $exclusionsToRemove
                }

                # TODO : Make a "To change" condition for when an exclusion range grows or shrinks

                Add-Member -InputObject $networkDeviceChanges -MemberType NoteProperty -Name 'dhcpExclusions' -Value $dhcpExclusionChanges
            }

            $networkDeviceChanges
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toChange' -Value $networkDevicesToChange
    }

    return $result
}

function Get-RestNetworkConnectionChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    Write-Debug 'Network connections'
    Write-Debug '==================================================='

    # If there are no changes, return
    if($null -eq $changes.connectionChanges) {
        Write-Debug ' No changes'
        return $null
    }

    $result = New-Object -TypeName PSCustomObject

    if($null -ne $changes.connectionChanges.toAdd)
    {
        Write-Debug (' Added')

        [PSCustomObject[]]$connectionsToAdd = $changes.connectionChanges.toAdd | ForEach-Object ({
            Write-Debug ('  [' + $_.networkDevice + '.' + $_.domainName + ' - ' + $_.interface + '] to [' + $_.uplinkToDevice + '.' + $_.domainName + ' - ' + $_.uplinkToInterface + ']' )
            $_
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toAdd' -Value $connectionsToAdd
    }

    if($null -ne $changes.connectionChanges.toRemove)
    {
        Write-Debug (' Removed')

        [PSCustomObject[]]$connectionsToRemove = $changes.connectionChanges.toRemove | ForEach-Object ({
            Write-Debug ('  [' + $_.networkDevice + '.' + $_.domainName + ' - ' + $_.interface + '] to [' + $_.uplinkToDevice + '.' + $_.domainName + ' - ' + $_.uplinkToInterface + ']' )
            $_.id
        })

        Write-Debug ''

        Add-Member -InputObject $result -MemberType NoteProperty -Name 'toRemove' -Value $connectionsToRemove
    }

    return $result
}

function Get-RestChanges
{
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    $deviceTypesRest = Get-RestDeviceTypeChanges -changes $changes
    $templatesRest = Get-RestTemplateChanges -changes $changes
    $tftpFilesRest = Get-RestTftpFileChanges -changes $changes
    $networkDeviceRest = Get-RestNetworkDeviceChanges -changes $changes
    $connectionsRest = Get-RestNetworkConnectionChanges -changes $changes

    # If no changes are detected, then return null
    if(
        ($null -eq $deviceTypesRest) -and
        ($null -eq $templatesRest) -and
        ($null -eq $tftpFilesRest) -and
        ($null -eq $networkDeviceRest) -and
        ($null -eq $connectionsRest)
    ) {
        return $null
    }

    $result = New-Object -TypeName PSCustomObject

    if ($null -ne $devicetypesrest) {
        add-member -inputobject $result -membertype noteproperty -name 'devicetypes' -value $devicetypesrest
    }
    
    if ($null -ne $templatesRest) {
        Add-Member -InputObject $result -MemberType NoteProperty -Name 'templates' -Value $templatesRest
    }
    
    if ($null -ne $tftpFilesRest) {
        Add-Member -InputObject $result -MemberType NoteProperty -Name 'tftpFiles' -Value $tftpFilesRest
    }

    if ($null -ne $networkDeviceRest) {
        Add-Member -InputObject $result -MemberType NoteProperty -Name 'networkDevices' -Value $networkDeviceRest
    }

    if ($null -ne $connectionsRest) {
        Add-Member -InputObject $result -MemberType NoteProperty -Name 'connections' -Value $connectionsRest
    }
    
    return $result
}

<#
    Description
        Scan the contents of the change tree and identify when network devices or network device interfaces
        have changed. Then identify connections that could be effected by those changes and insert add/remove
        settings for connections that haven't already been added or removed.
#>
function Update-DRC-ExtendedConnectionChanges
{
    param(
        [Parameter(Mandatory)]
        [Uri]$apiBaseUri,

        [Parameter(Mandatory)]
        [PSCustomObject]$changes
    )

    # Get a list of network devices which have changed their device types
    [PSCustomObject[]]$effectedDevices = $()
    if(
        ($null -ne $changes.networkDeviceChanges) -and
        ($null -ne $changes.networkDeviceChanges.toChange)
    ) {
        $changes.networkDeviceChanges.toChange.ForEach({
            if(
                ($null -ne $_['changedFields']) -and
                ($_.changedFields.Contains('deviceType'))
            ) {
                $effectedDevices += [PSCustomObject]$_
            }
        })
    }

    [string[]]$effectedDeviceGuids = $effectedDevices.ForEach({ $_.existingDevice.Id.ToString() })

    # Get a list of connections that are effected by network device type changes (but not interface changes)
    $requestSplat = @{
        UseBasicParsing = $true
        Uri = ([Uri]::new($apiBaseUri, 'api/v0/plugandplay/networkdevice/uplinks/bydeviceids')).ToString()
        Method = 'Post'
        ContentType = 'application/json'
        Body = ($effectedDeviceGuids | ConvertTo-Json)
    }

    $effectedConnections = Invoke-RestMethod @requestSplat 

    # If there are no dependencies return
    if(Test-CollectionNullOrEmpty -value $effectedConnections) {
        return
    }

    # Prepare to make changes
    if($null -eq $changes.connectionChanges) {
        $changes.connectionChanges = New-Object -TypeName PSCustomObject
    }

    $connectionsToAdd = [PSCustomObject[]]@()
    if($null -eq (Get-Member -InputObject $changes.connectionChanges -Name 'toAdd')) {
        Add-Member -InputObject $changes.connectionChanges -MemberType NoteProperty -Name 'toAdd' -Value $connectionsToAdd
    }
    $connectionsToAdd = $changes.connectionChanges.toAdd

    $connectionsToRemove = [PSCustomObject[]]@()
    if($null -eq (Get-Member -InputObject $changes.connectionChanges -Name 'toRemove')) {
        Add-Member -InputObject $changes.connectionChanges -MemberType NoteProperty -Name 'toRemove' -Value $connectionsToRemove
    }
    $connectionsToAdd = $changes.connectionChanges.toAdd

    # Process each change by removing the connection and readding it
    $effectedConnections.ForEach({
        $changes.connectionChanges.toRemove += $_

        $changes.connectionChanges.toAdd += [PSCustomObject[]]@{
            domainName = $_.domainName
            networkDevice = $_.networkDevice
            interface = $_.interface
            uplinkToDevice = $_.uplinkToDevice
            uplinkToInterface = $_.uplinkToInterface
        }
    })

    # TODO : Handle when the device interface indices change
}

Clear-Host
# $InformationPreference = "Continue"
$DebugPreference = "Continue"
$VerbosePreference = "SilentlyContinue"


$apiBaseUri = [Uri]::new("http://localhost:27600")
$designRoot = Join-Path -Path $PSScriptRoot -ChildPath 'SampleData'
$designFile = Join-Path -Path $designRoot -ChildPath 'Design.ps1'

$changes = Get-DesignChanges -designFile $designFile -apiBaseUri $apiBaseUri

# Show-Changes -changes $changes

Update-DRC-ExtendedConnectionChanges -apiBaseUri $apiBaseUri -changes $changes
# TODO : Implement design check here
#     Right now, the code only reads configurations in but doesn't run any design constraint checking.
#     There should never be a design posted to the server without first checking to make sure it's valid

$restChanges = Get-RestChanges -changes $changes

# TODO : PUT the rest as JSON to the server here.

$uri = ($apiBaseUri.ToString() + 'api/v0/batch')

$requestSplat = @{
    UseBasicParsing = $true
    Uri = $uri
    Method = 'Post'
    ContentType = 'application/json'
    Body = ($restChanges | ConvertTo-Json -Depth 10)
}

$result = Invoke-RestMethod @requestSplat 
