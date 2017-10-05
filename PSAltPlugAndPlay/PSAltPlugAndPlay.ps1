
Function Get-PnPNetworkDeviceType {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,
        
        [Parameter()]
        [Guid] $Id = [Guid]::Empty
    )

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevicetype')

    if($Id -ne [Guid]::Empty) {
        $uri += ('/' + $Id.ToString())
    }

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri 
        Method = 'Get'
        ContentType = 'application/json'
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Add-PnPNetworkDeviceType {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $Manufacturer,

        [Parameter(Mandatory)]
        [string] $ProductId
    )

    $requestBody = @{
        name = $Name
        manufacturer = $Manufacturer
        productId = $ProductId
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevicetype')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Post'
        ContentType = 'application/json'
        Body = ($requestBody | ConvertTo-Json)
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Get-PnPNetworkDeviceTypeInterface {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $DeviceType,

        [Parameter()]
        [Guid] $Id = [Guid]::Empty
    )

    $networkDeviceType = Get-PnPNetworkDeviceType | Where-Object { $_.Name -ilike $DeviceType }
    if (
        ($null -eq $networkDeviceType) -or
        ($networkDeviceType.PSObject.Properties -match 'Count')
    ) {
        throw [System.ArgumentException]::new('Cannot resolve network device type ''' + $DeviceType + '''')
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevicetype/' + $networkDeviceType.id + '/interface')

    if($Id -ne [Guid]::Empty) {
        $uri += ('/' + $Id.ToString())
    }

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Get'
        ContentType = 'application/json'
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Add-PnPNetworkDeviceTypeInterface {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $DeviceType,

        [Parameter(Mandatory)]
        [string] $Name, 

        [Parameter(Mandatory)]
        [int] $Index
    )

    $networkDeviceType = Get-PnPNetworkDeviceType | Where-Object { $_.Name -ilike $DeviceType }
    if (
        ($null -eq $networkDeviceType) -or
        ($networkDeviceType.PSObject.Properties -match 'Count')
    ) {
        throw [System.ArgumentException]::new('Cannot resolve network device type ''' + $DeviceType + '''')
    }

    $requestBody = @{
        deviceType = @{
            id = $networkDeviceType.id
        }
        name = $Name
        interfaceIndex = $Index
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevicetype/' + $networkDeviceType.id + '/interface')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Post'
        ContentType = 'application/json'
        Body = ($requestBody | ConvertTo-Json)
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Add-PnPNetworkDeviceTypeInterfaceRange {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $DeviceType,

        [Parameter(Mandatory)]
        [string] $Name, 

        [Parameter(Mandatory)]
        [int] $FirstIndex,

        [Parameter(Mandatory)]
        [int] $Count
    )

    $networkDeviceType = Get-PnPNetworkDeviceType | Where-Object { $_.Name -ilike $DeviceType }
    if (
        ($null -eq $networkDeviceType) -or
        ($networkDeviceType.PSObject.Properties -match 'Count')
    ) {
        throw [System.ArgumentException]::new('Cannot resolve network device type ''' + $DeviceType + '''')
    }

    $requestBody = @{
        deviceType = @{
            id = $networkDeviceType.id
        }
        name = $Name
        firstIndex = $FirstIndex
        count = $Count
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevicetype/' + $networkDeviceType.id + '/interface/range')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Post'
        ContentType = 'application/json'
        Body = ($requestBody | ConvertTo-Json)
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Get-PnPNetworkDevice {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,
        
        [Parameter()]
        [Guid] $Id = [Guid]::Empty
    )

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevice')

    if($Id -ne [Guid]::Empty) {
        $uri += ('/' + $Id.ToString())
    }

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri 
        Method = 'Get'
        ContentType = 'application/json'
    }

    [PSCustomObject[]]$result = Invoke-RestMethod @requestSplat

    return $result
}

Function Add-PnPNetworkDevice {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string]$Hostname,

        [Parameter(Mandatory)]
        [string]$DomainName,

        [Parameter(Mandatory)]
        [string]$DeviceType,

        [Parameter()]
        [string]$Description,

        [Parameter()]
        [string]$IPAddress
    )

    $networkDeviceType = Get-PnPNetworkDeviceType | Where-Object { $_.Name -ilike $DeviceType }
    if (
        ($null -eq $networkDeviceType) -or
        ($networkDeviceType.PSObject.Properties -match 'Count')
    ) {
        throw [System.ArgumentException]::new('Cannot resolve network device type ''' + $DeviceType + '''')
    }

    $requestBody = @{
        deviceType = @{
            id = $networkDeviceType.id
        }
        hostName = $Hostname
        domainName = $DomainName
        description = $Description
        ipAddress = $IPAddress
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevice')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Post'
        ContentType = 'application/json'
        Body = ($requestBody | ConvertTo-Json)
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Get-PnPNetworkDeviceUplink {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $DomainName,

        [Parameter(Mandatory)]
        [string] $NetworkDevice
    )

    $downlinkDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort | 
        Where-Object {
            ($_.domainName -eq $DomainName) -and
            ($_.hostname -eq $NetworkDevice)
        }

    if ($null -eq $downlinkDevice) {
        throw [System.ArgumentException]::new('Failed to find network device ' + $NetworkDevice + '.' + $DomainName)
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevice/' + $downlinkDevice.id + '/uplink')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Get'
        ContentType = 'application/json'
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Add-PnPNetworkDeviceUplink {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $DomainName,

        [Parameter(Mandatory)]
        [string] $NetworkDevice,

        [Parameter(Mandatory)]
        [string] $Interface,

        [Parameter(Mandatory)]
        [string] $UplinkToDevice,

        [Parameter(Mandatory)]
        [string] $UplinkToInterface
    )

    $downlinkDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort | 
        Where-Object {
            ($_.domainName -eq $DomainName) -and
            ($_.hostname -eq $NetworkDevice)
        }

    if ($null -eq $downlinkDevice) {
        throw [System.ArgumentException]::new('Failed to find network device ' + $NetworkDevice + '.' + $DomainName)
    }

    $downlinkInterface = Get-PnPNetworkDeviceTypeInterface -PnPHost $PnPHost -HostPort $HostPort -DeviceType $downlinkDevice.deviceType.name |
        Where-Object {
            $_.name -ilike $Interface
        }

    if ($null -eq $downlinkInterface) {
        throw [System.ArgumentException]::new('Failed to find interface on network device ' + $NetworkDevice + ' with a name of ' + $Interface)
    }

    $uplinkDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort | 
        Where-Object {
            ($_.domainName -eq $DomainName) -and
            ($_.hostname -eq $UplinkToDevice)
        }

    if ($null -eq $downlinkDevice) {
        throw [System.ArgumentException]::new('Failed to find uplink network device ' + $UplinkToDevice + '.' + $DomainName)
    }

    $uplinkInterface = Get-PnPNetworkDeviceTypeInterface -PnPHost $PnPHost -HostPort $HostPort -DeviceType $uplinkDevice.deviceType.name |
        Where-Object {
            $_.name -ilike $UplinkToInterface
        }

    if ($null -eq $uplinkInterface) {
        throw [System.ArgumentException]::new('Failed to find interface on uplink network device ' + $UplinkToDevice + ' with a name of ' + $UplinkToInterface)
    }

    $requestBody = @{
        networkDevice = @{
            id = $downlinkDevice.id
        }
        interfaceIndex = $downlinkInterface.interfaceIndex
        connectedToDevice = @{
            id = $uplinkDevice.id
        }
        connectedToInterfaceIndex = $uplinkInterface.interfaceIndex
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevice/' + $downlinkDevice.id + '/uplink')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Post'
        ContentType = 'application/json'
        Body = ($requestBody | ConvertTo-Json)
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Get-PnPTemplate {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter()]
        [Guid] $Id = [Guid]::Empty
    )

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template')

    if($Id -ne [Guid]::Empty) {
        $uri += ('/' + $Id.ToString())
    }

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Get'
        ContentType = 'application/json'
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Add-PnPTemplate {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter()]
        [string] $Content,

		[Parameter()]
		[string] $Path
    )

	if(-not [string]::IsNullOrEmpty($Path)) {
		$Content = (Get-Content -Path $demoTemplatePath) -join "`n"
	}

    if([string]::IsNullOrEmpty($Name) -or [string]::IsNullOrEmpty($Content)) {
        throw [System.ArgumentException]::new('Either name or content is empty')
    }
    
    $requestBody = @{
        name = $Name
        content = $Content
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Post'
        ContentType = 'application/json'
        Body = ($requestBody | ConvertTo-Json)
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Get-PnPNetworkDeviceTemplate {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $Template
    )

    $pnpTemplate = Get-PnPTemplate -PnPHost $PnPHost -HostPort $HostPort | Where-Object { $_.name -ilike $Template }
    if($null -eq $pnpTemplate) {
        throw [System.ArgumentException]::new("Invalid template specified " + $Template)
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/' + $pnpTemplate.id + '/configuration')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Get'
        ContentType = 'application/json'
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Set-PnPNetworkDeviceTemplate {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $NetworkDevice,

        [Parameter(Mandatory)]
        [string] $Template,

        [Parameter()]
        [PSCustomObject[]] $TemplateParameters,

        [Parameter(Mandatory)]
        [string] $Description
    )

    $pnpDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort | Where-Object { ($_.hostname + '.' + $_.domainName) -ilike $NetworkDevice }
    if($null -eq $pnpDevice) {
        throw [System.ArgumentException]::new("Invalid network device specified " + $NetworkDevice)
    }

    $pnpTemplate = Get-PnPTemplate -PnPHost $PnPHost -HostPort $HostPort | Where-Object { $_.name -ilike $Template }
    if($null -eq $pnpTemplate) {
        throw [System.ArgumentException]::new("Invalid template specified " + $Template)
    }

    $requestBody = @{
        template = @{
            id = $pnpTemplate.id
        }
        networkDevice = @{
            id = $pnpDevice.id
        }
        description = $Description
        properties = $TemplateParameters
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/' + $pnpTemplate.id + '/configuration')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Post'
        ContentType = 'application/json'
        Body = ($requestBody | ConvertTo-Json)
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Get-PnPNetworkDeviceTemplateParameters {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string] $NetworkDevice,

        [Parameter(Mandatory)]
        [string] $Template
    )

    $pnpDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort | Where-Object { ($_.hostname + '.' + $_.domainName) -ilike $NetworkDevice }
    if($null -eq $pnpDevice) {
        throw [System.ArgumentException]::new("Invalid network device specified " + $NetworkDevice)
    }

    $pnpTemplate = Get-PnPTemplate -PnPHost $PnPHost -HostPort $HostPort | Where-Object { $_.name -ilike $Template }
    if($null -eq $pnpTemplate) {
        throw [System.ArgumentException]::new("Invalid template specified " + $Template)
    }

    $templateConfiguration = Get-PnPNetworkDeviceTemplate -PnPHost $PnPHost -HostPort $HostPort -Template $Template | 
        Where-Object {
            $_.networkDevice.id -ilike $pnpDevice.id
        }
    if($null -eq $templateConfiguration) {
        throw [System.ArgumentException]::new("No template configuration found")
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/' + $pnpTemplate.id + '/configuration/' + $templateConfiguration.id + '/property')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Get'
        ContentType = 'application/json'
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Get-PnPNetworkDeviceConfiguration {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string]$NetworkDevice
    )

    $pnpDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort | Where-Object { ($_.hostname + '.' + $_.domainName) -ilike $NetworkDevice }
    if($null -eq $pnpDevice) {
        throw [System.ArgumentException]::new("Invalid network device specified " + $NetworkDevice)
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/networkdevice/' + $pnpDevice.id + '/configuration')

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Get'
        ContentType = 'application/json'
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}
