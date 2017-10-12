# Get-ChildItem -Filter '*.ps1' -Recurse | Unblock-File

$modulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\PSAltPlugAndPlay.psd1' -Resolve
Import-Module -Name $modulePath -Force

Function InitialMess {
    $r4331Type = Get-PnPNetworkDeviceType | Where-Object { $_.name -eq 'ISR4331' }

    if($null -eq $r4331Type) {
        $r4331Type = Add-PnPNetworkDeviceType -Name 'ISR4331' -Manufacturer 'Cisco' -ProductId 'ISR4331-SEC/K9-WS'
    }

    $s2960Type = Get-PnPNetworkDeviceType | Where-Object { $_.name -eq 'C2960-24PS-TT' }

    if($null -eq $s2960Type) {
        $s2960Type = Add-PnPNetworkDeviceType -Name 'C2960-24PS-TT' -Manufacturer 'Cisco' -ProductId 'C2960-24PS-TT'
    }

    $rIntList = @(
        @{ Name = 'GigabitEthernet0/0'; Index = 0 },
        @{ Name = 'GigabitEthernet0/1'; Index = 1 }
    )

    foreach($rInt in $rIntList) {
        $rIntValue = Get-PnPNetworkDeviceTypeInterface -DeviceType $r4331Type.name | Where-Object { $_.Name -ilike $rInt.Name }

        if($null -eq $rIntValue) {
            $rIntValue = Add-PnpNetworkDeviceTypeInterface -DeviceType 'ISR4331' @rInt
        }
    }

    if($null -eq (Get-PnPNetworkDeviceTypeInterface -DeviceType $s2960Type.name | Where-Object { $_.Name -ilike 'FastEthernet0/1'})) {
        Add-PnPNetworkDeviceTypeInterfaceRange -DeviceType $s2960Type.name -Name 'FastEthernet0/1' -FirstIndex 1 -Count 24
    }
    if($null -eq (Get-PnPNetworkDeviceTypeInterface -DeviceType $s2960Type.name | Where-Object { $_.Name -ilike 'GigabitEthernet0/1'})) {
        Add-PnPNetworkDeviceTypeInterfaceRange -DeviceType $s2960Type.name -Name 'GigabitEthernet0/1' -FirstIndex 25 -Count 2
    }
}

Function Get-PnPConfigurationFile {
    Param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $configText = Get-Content -Path $Path -Raw
    $script = [ScriptBlock]::Create($configText)

    $result = Invoke-Command -ScriptBlock $script

    return $result
}

InitialMess

$hostPort = 27599
# $hostPort = 80

$configFile = Join-Path -Path $PSScriptRoot -ChildPath '.\SampleData\NavDemoStructured.config' -Resolve
$testConfig = Get-PnPConfigurationFile -Path $configFile
Invoke-PnPProcessTemplatesSection -HostPort $hostPort -Config $testConfig -ConfigPath $configFile -Force
Invoke-PnPProcessDevicesSection -HostPort $hostPort -Config $testConfig -Force
Invoke-PnPProcessConnections -HostPort $hostPort -Config $testConfig -Force -Verbose

