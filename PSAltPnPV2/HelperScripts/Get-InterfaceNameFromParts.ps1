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
        Combines the parts of a Cisco interface name into a string
    .PARAMETER Parts
        the parts which combine to make an interface name
    .PARAMETER Offset
        how many interface numbers to increase the output by
    .PARAMETER IncludeSubinterfaces
        Flag to decide whether to include the subinterface in the output
#>
Function Get-InterfaceNameFromParts
{
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Parts,

        [Parameter()]
        [int]$Offset=0,

        [Parameter()]
        [switch]$IncludeSubinterface=$false
    )

    $indices = $Parts.indices.ForEach({ $_ })
    $indices[$indices.Count - 1] += $Offset

    $subinterface = [string]::Empty
    if($IncludeSubinterface -and $Parts.hasSubinterface) {
        $subinterface = '.' + $Parts.subinterface.ToString()
    }

    return (
        $Parts.name +
        ([string]::Join('/', $indices.ForEach({$_.ToString()}))) +
        $subinterface
    )
}
