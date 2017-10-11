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

        [Parameter()]
        [string] $NetworkDevice,

        [Parameter()]
        [Guid] $NetworkDeviceId=[Guid]::Empty,

        [Parameter()]
        [string] $Template,

        [Parameter()]
        [PSCustomObject[]] $TemplateParameters,

        [Parameter()]
        [string] $Description,

        [Parameter()]
        [Guid] $ConfigurationId=[Guid]::Empty
    )

    if($ConfigurationId -ne [Guid]::Empty) {
        $configuration = Get-PnPNetworkDeviceConfiguration -PnPHost $PnPHost -HostPort $HostPort -ConfigurationId $ConfigurationId
        if($null -eq $configuration) {
            throw [System.ArgumentException]::new(
                'Invalid template specified ' + $ConfigurationId,
                'ConfigurationId'
            )
        }
        
        # TODO : Consider expanding to support changing properties here as well.
        $requestBody = @{
            id = $configuration.id
            description = $description
        }

        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/configuration/' + $configuration.id)

        $requestSplat = @{
            UseBasicParsing = $true
            Uri = $uri
            Method = 'Put'
            ContentType = 'application/json'
            Body = ($requestBody | ConvertTo-Json)
        }

        $result = Invoke-RestMethod @requestSplat

        return $result
    } elseif(-not [string]::IsNullOrEmpty($NetworkDevice -and -not [string]::IsNullOrEmpty($Template))) {
        $pnpDevice = $null
        if($NetworkDeviceId -ne [Guid]::Empty) {
            if(-not [string]::IsNullOrEmpty($NetworkDevice)) {
                throw [System.ArgumentException]::new(
                    'Either NetworkDevice or NetworkDeviceId should be specified, but not both'
                )
            }

            $pnpDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort -Id $NetworkDeviceId
        } elseif(-not [string]::IsNullOrEmpty($NetworkDevice)) {
            $pnpDevice = Get-PnPNetworkDevice -PnPHost $PnPHost -HostPort $HostPort | Where-Object { ($_.hostname + '.' + $_.domainName) -ilike $NetworkDevice }
        } else {
            throw [System.ArgumentException]::new(
                'Either NetworkDevice or NetworkDeviceId should be specified, but not both'
            )
        }
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

    throw [System.ArgumentException]::new(
        'Either ConfigurationId or Template and NetworkDevice or NetworkDeviceId must be specified'
    )
}

Function Get-PnPNetworkDeviceConfiguration {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter()]
        [string]$NetworkDevice,

        [Parameter()]
        [Guid]$ConfigurationId=[Guid]::Empty
    )

    $pnpDevice = $null
    if($ConfigurationId -ne [Guid]::Empty) {
        if(-not [string]::IsNullOrEmpty($NetworkDevice)) {
            throw [System.ArgumentException]::new(
                'Either ConfigurationId or NetworkDevice must be specified, but not both'
            )
        }

        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/configuration/' + $ConfigurationId)

        $requestSplat = @{
            UseBasicParsing = $true
            Uri = $uri
            Method = 'Get'
            ContentType = 'application/json'
        }

        $result = Invoke-RestMethod @requestSplat

        return $result
    } elseif([string]::IsNullOrEmpty($NetworkDevice)) {
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
    } else {
        throw [System.ArgumentException]::new(
            'Either ConfigurationId or NetworkDevice must be specified, but not both'
        )
    }
}

Function Remove-PnPNetworkDeviceConfiguration {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [string]$TemplateId,

        [Parameter(Mandatory)]
        [string]$ConfigurationId
    )

    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/' + $TemplateId + '/configuration/' + $ConfigurationId)

    $requestSplat = @{
        UseBasicParsing = $true
        Uri = $uri
        Method = 'Delete'
        ContentType = 'application/json'
    }

    $result = Invoke-RestMethod @requestSplat

    return $result
}
