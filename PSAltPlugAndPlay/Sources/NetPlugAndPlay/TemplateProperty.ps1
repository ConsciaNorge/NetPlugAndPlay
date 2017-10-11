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

Function Remove-PnPNetworkDeviceTemplateParameter {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [Guid]$ParameterId
    )

    Process {
        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/property/' + $ParameterId)

        $requestSplat = @{
            UseBasicParsing = $true
            Uri = $uri
            Method = 'Delete'
            ContentType = 'application/json'
        }

        $result = Invoke-RestMethod @requestSplat

        return $result
    }
}

Function Get-PnPNetworkDeviceTemplateParameterById {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [Guid]$ParameterId
    )

    Process {
        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/property/' + $ParameterId)

        $requestSplat = @{
            UseBasicParsing = $true
            Uri = $uri
            Method = 'Get'
            ContentType = 'application/json'
        }

        $result = Invoke-RestMethod @requestSplat

        return $result
    }
}

Function Set-PnPNetworkDeviceTemplateParameter {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [Guid]$ParameterId,

        [Parameter()]
        [AllowEmptyString()]
        [string]$Name,

        [Parameter()]
        [AllowEmptyString()]
        [string]$Value
    )

    Begin {
        if(-not ($PSBoundParameters.Name -or $PSBoundParameters.Value)) {
            throw [System.ArgumentException]::new(
                'Either Name or Value must be specified',
                'Value'
            )
        }
    }

    Process {
        $existingValue = Get-PnPNetworkDeviceTemplateParameterById -PnPHost $PnPHost -HostPort $HostPort -ParameterId $ParameterId
        if(($null -eq $existingValue) -or $existingValue.id -ne $ParameterId) {
            throw [System.ArgumentException]::new(
                'Invalid template parameter value specified : ' + $ParameterId,
                'ParameterId'
            )
        }

        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/property/' + $ParameterId)

        if($PSBoundParameters.Name) {
            $existingValue.name = $Name
        }
        if($PSBoundParameters.Value) {
            $existingValue.value = $Value
        }

        $requestSplat = @{
            UseBasicParsing = $true
            Uri = $uri
            Method = 'Put'
            ContentType = 'application/json'
            Body = ($existingValue | ConvertTo-Json)
        }

        $result = Invoke-RestMethod @requestSplat

        return $result
    }
}

Function New-PnPNetworkDeviceTemplateParameter {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',

        [Parameter()]
        [int] $HostPort = 27599,

        [Parameter(Mandatory)]
        [Guid]$ConfigurationId,

        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Value
    )

    Process {
        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + '/api/v0/plugandplay/template/configuration/' + $ConfigurationId + '/property')

        $request = @{
            name = $Name
            value = $Value
        }

        $requestSplat = @{
            UseBasicParsing = $true
            Uri = $uri
            Method = 'Post'
            ContentType = 'application/json'
            Body = ($request | ConvertTo-Json)
        }

        $result = Invoke-RestMethod @requestSplat

        return $result
    }

}