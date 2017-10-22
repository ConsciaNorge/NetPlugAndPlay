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

Function Get-PnPNetworkDeviceTypeInterface {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 80,

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

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/plugandplay/networkdevicetype/' + $networkDeviceType.id + '/interface')

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
        [int] $HostPort = 80,

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

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/plugandplay/networkdevicetype/' + $networkDeviceType.id + '/interface')

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
        [int] $HostPort = 80,

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

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/plugandplay/networkdevicetype/' + $networkDeviceType.id + '/interface/range')

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

