
Function Get-NetworkInterfaceParts
{
    Param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    $interfaceNameExpression = [Regex]::new('((?<name>[A-Za-z]+[A-Za-z0-9]*[A-Za-z])(((?<indices>[0-9])+/?)+)(\.(?<subinterface>[0-9]+))?)')

    $m = $interfaceNameExpression.Match($Name);
    if(
        ($null -eq $m) -or
        (-not $m.Success)
    ) {
        return $null
    }

    $subinterface = 0
    if($m.Groups['subinterface'].Success) { 
        $subinterface = [Convert]::ToInt32($m.Groups['subinterface'].Value) 
    }

    return @{
        name = $m.Groups['name'].Value
        indices = $m.Groups['indices'].Captures.ForEach({ [Convert]::ToInt32($_.Value) })
        hasSubinterface = $m.Groups['subinterface'].Success
        subinterface = $subinterface
    }
}

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


Get-NetworkInterfaceRangeNames -FirstInterfaceName 'GigabitEthernet1/2/3/1.1234' -FirstInterfaceIndex 0 -Count 24