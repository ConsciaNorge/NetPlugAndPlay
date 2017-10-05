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
