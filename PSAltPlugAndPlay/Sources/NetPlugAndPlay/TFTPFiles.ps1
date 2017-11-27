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

Function Get-TFTPFiles {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 80,

        [Parameter()]
        [string] $FilePath
    )


    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/tftp/files')

    if(-not [string]::IsNullOrEmpty($FilePath)) {
        $uri += '?filePath=' + [System.Web.HttpUtility]::UrlEncode($FilePath)
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

Function Remove-TFTPFile {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 80,

        [Parameter(Mandatory)]
        [Guid] $Id
    )
    
    $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/tftp/files/' + $id.ToString())
    
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

Function Set-TFTPFile {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',        

        [Parameter()]
        [int] $HostPort = 80,

        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter(Mandatory)]
        [string] $Content
    )

    # $networkDeviceType = Get-PnPNetworkDeviceType -PnPHost $PnPHost -HostPort $HostPort | Where-Object { $_.Name -ilike $DeviceType }
    # if (
    #     ($null -eq $networkDeviceType) -or
    #     ($networkDeviceType.PSObject.Properties -match 'Count')
    # ) {
    #     throw [System.ArgumentException]::new('Cannot resolve network device type ''' + $DeviceType + '''')
    # }

    $existingEntry = Get-TFTPFiles -PnPHost $PnPHost -HostPort $HostPort -FilePath $FilePath
    if($null -eq $existingEntry)
    {
        $requestBody = @{
            filePath = $FilePath
            content = $Content
        }

        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/tftp/files')

        $requestSplat = @{
            UseBasicParsing = $true
            Uri = $uri
            Method = 'Post'
            ContentType = 'application/json'
            Body = ($requestBody | ConvertTo-Json)
        }

        $result = Invoke-RestMethod @requestSplat

        return $result
    } else {
        $requestBody = @{
            filePath = $FilePath
            content = $Content
        }

        $uri = ('http://' + $PnPHost + ':' + $HostPort.ToString() + $sitePrefix + '/api/v0/tftp/files/' + $existingEntry.id.ToString())

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
}
