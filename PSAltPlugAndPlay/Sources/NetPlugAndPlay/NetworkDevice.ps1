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

Function Get-PnPNetworkDevice {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 80,
        
        [Parameter()]
        [Guid] $Id = [Guid]::Empty,

        [Parameter()]
        [string]$Hostname,

        [Parameter()]
        [string]$DomainName
    )

    Process {
        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/plugandplay/networkdevice')

        if($Id -ne [Guid]::Empty) {
            $uri += ('/' + $Id.ToString())
        }

        $parameters = @()
        if(-not [string]::IsNullOrEmpty($Hostname)) {
            $parameters += 'hostname=' + $Hostname;
        }
        if(-not [string]::IsNullOrEmpty($DomainName)) {
            $parameters += 'domainName=' + $DomainName
        }
        if($parameters.Count -gt 0) {
            $uri += '?' + ($parameters -join '&')
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
}

Function Add-PnPNetworkDevice {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 80,

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

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/plugandplay/networkdevice')

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

Function Set-PnPNetworkDevice {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 80,

        [Parameter()]
        [Guid]$Id = [Guid]::Empty,

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

    if($Id -eq [Guid]::Empty) {
        $networkDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort -Hostname $Hostname -DomainName $DomainName
        if($null -eq $networkDevice) {
            throw [System.Exception]::new(
                'Failed to get existing network device object for : ' + $Hostname + '.' + $DomainName
            )
        }
        Write-Debug -Message (
            'Received id : ' + $Id + ' for device : ' + $Hostname + '.' + $DomainName
        )
        $Id = $networkDevice.id
    } 

    $networkDeviceType = Get-PnPNetworkDeviceType | Where-Object { $_.Name -ilike $DeviceType }
    if (
        ($null -eq $networkDeviceType) -or
        ($networkDeviceType.PSObject.Properties -match 'Count')
    ) {
        throw [System.ArgumentException]::new('Cannot resolve network device type ''' + $DeviceType + '''')
    }

    $requestBody = @{
        id = $Id
        deviceType = @{
            id = $networkDeviceType.id
        }
        hostName = $Hostname
        domainName = $DomainName
        description = $Description
        ipAddress = $IPAddress
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/plugandplay/networkdevice/' + $id)

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Put'
        ContentType = 'application/json'
        Body = ($requestBody | ConvertTo-Json)
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Remove-PnPNetworkDevice {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 80,

        [Parameter(Mandatory)]
        [string] $DomainName,

        [Parameter(Mandatory)]
        [string] $Hostname
    )

    $networkDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort -Hostname $Hostname -DomainName $DomainName
    if($null -eq $networkDevice) {
        Write-Debug -Message ('Device ' + $Hostname + '.' + $DomainName + ' not found')
    }

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/plugandplay/networkdevice/' + $networkDevice.id)

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Delete'
        ContentType = 'application/json'
        Body = ($requestBody | ConvertTo-Json)
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}

Function Get-PnPNetworkDeviceTemplates {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 80,

        [Parameter(Mandatory)]
        [string] $DomainName,

        [Parameter(Mandatory)]
        [string] $Hostname
    )

    Process {
        $networkDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort -Hostname $Hostname -DomainName $DomainName
        if($null -eq $networkDevice) {
            Write-Debug -Message ('Device ' + $Hostname + '.' + $DomainName + ' not found')
        }

        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/plugandplay/networkdevice/' + $networkDevice.id + '/template')

        $requestSplat = @{
            UseBasicParsing = $true
            Uri = $uri 
            Method = 'Get'
            ContentType = 'application/json'
        }

        [PSCustomObject[]]$result = Invoke-RestMethod @requestSplat

        return $result
    }
}
