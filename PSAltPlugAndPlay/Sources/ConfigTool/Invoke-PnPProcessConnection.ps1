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

Function Invoke-PnPProcessConnection {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',

        [Parameter()]
        [int] $HostPort = 80,

        [Parameter(Mandatory)]
        [PSCustomObject]$Connection,

        [Parameter()]
        [switch]$Force
    )

    Process {
        $hostParams = @{
            PnPHost = $PnPHost
            HostPort = $HostPort
        }

        $networkDevice = Get-PnPNetworkDevice @hostParams -DomainName $Connection.domainName -Hostname $Connection.networkDevice
        if($null -eq $networkDevice) {
            throw [System.ArgumentException]::new(
                'Failed to find network device ' + $Connection.domainName + '.' + $Connection.networkDevice
            )
        }

        $networkDeviceType = Get-PnPNetworkDeviceType @hostParams -Id $networkDevice.deviceType.id
        if($null -eq $networkDeviceType) {
            throw [System.IO.IOException]::new(
                'Failed to get network device type ' + $networkDevice.deviceType.id
            )
        }

        $downlinkInterface = Get-PnPNetworkDeviceTypeInterface @hostParams -DeviceType $networkDeviceType.name |
            Where-Object { $_.name -ilike $Connection.interface }

        if($null -eq $downlinkInterface) {
            throw [System.ArgumentException]::new(
                'Failed to find network interface ' + $Connection.interface + ' on device type ' + $networkDeviceType.name
            )
        }

        $uplinkNetworkDevice = Get-PnPNetworkDevice @hostParams -DomainName $Connection.domainName -Hostname $Connection.uplinkToDevice
        if($null -eq $uplinkNetworkDevice) {
            throw [System.ArgumentException]::new(
                'Failed to find uplink network device ' + $Connection.domainName + '.' + $Connection.uplinkNetworkDevice
            )
        }

        $uplinkNetworkDeviceType = Get-PnPNetworkDeviceType @hostParams -Id $uplinkNetworkDevice.deviceType.id
        if($null -eq $uplinkNetworkDeviceType) {
            throw [System.IO.IOException]::new(
                'Failed to get uplink network device type ' + $uplinkNetworkDevice.deviceType.id
            )
        }

        $uplinkInterface = Get-PnPNetworkDeviceTypeInterface @hostParams -DeviceType $uplinkNetworkDeviceType.name |
            Where-Object { $_.name -ilike $Connection.uplinkToInterface }

        if($null -eq $uplinkInterface) {
            throw [System.ArgumentException]::new(
                'Failed to find uplink network interface ' + $Connection.interface + ' on device type ' + $uplinkNetworkDeviceType.name
            )
        }

        $existingConnection = Get-PnPNetworkDeviceUplink @hostParams -DomainName $Connection.domainName -NetworkDevice $Connection.networkDevice | 
            Where-Object {
                ($_.connectedToDevice.id -eq $uplinkNetworkDevice.id) -and
                ($_.connectedToInterfaceIndex -eq $uplinkInterface.interfaceIndex)
            }

        if($null -eq $existingConnection) {
            Write-Verbose -Message (
                'Connection from ' + $networkDevice.hostname + '.' + $networkDevice.domainName + ' ' + $downlinkInterface.name +
                ' to ' + $uplinkNetworkDevice.hostname + '.' + $uplinkNetworkDevice.domainName + ' ' + $uplinkInterface.name +
                ' does not exist. Creating.'
            )

            $addDeviceSplat = @{
                DomainName = $networkDevice.domainName
                NetworkDevice = $networkDevice.hostName
                Interface = $downlinkInterface.name
                UplinkToDevice = $uplinkNetworkDevice.hostname
                UplinkToInterface = $uplinkInterface.name
            }

            $link = Add-PnPNetworkDeviceUplink @hostParams @addDeviceSplat

            if($null -eq $link) {
                throw [System.IO.IOException]::new(
                    'Failed to create connection from ' + $networkDevice.hostname + '.' + $networkDevice.domainName + ' ' + $downlinkInterface.name +
                    ' to ' + $uplinkNetworkDevice.hostname + '.' + $uplinkNetworkDevice.domainName + ' ' + $uplinkInterface.name
                )
            }
        }
    }
}
