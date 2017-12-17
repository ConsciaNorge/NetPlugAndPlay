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

<#
    .SYNOPSIS
        Generates a list of Cisco interface names and SNMP indices for a range
    .PARAMETER FirstInterfaceName
        The first interface name to build from
    .PARAMETER FirstInterfaceIndex
        The first SNMP interface index value to count from
    .PARAMETER Count
        The number of interfaces to generate names and indices for
#>
Function Get-NetworkInterfaceRangeNames
{
    Param(
        [Parameter(Mandatory)]
        [string]$FirstInterfaceName,

        [Parameter(Mandatory)]
        [int]$FirstInterfaceIndex,

        [Parameter(Mandatory)]
        [int]$Count
    )

    $firstInterfaceParts = Get-NetworkInterfaceParts -Name $FirstInterfaceName
    
    $result = @()
    for($i=0; $i -lt $Count; $i++) {
        $result += @{
            interfaceIndex = ($FirstInterfaceIndex + $i)
            name = (Get-InterfaceNameFromParts -Parts $firstInterfaceParts -Offset $i)
        }
    }

    return $result
}
