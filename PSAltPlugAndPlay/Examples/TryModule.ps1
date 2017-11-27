# Get-ChildItem -Filter '*.ps1' -Recurse | Unblock-File

$hostPort = 27600
# $hostPort = 80

$modulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\PSAltPlugAndPlay.psd1' -Resolve
Import-Module -Name $modulePath -Force

Function InitialMess {
    $r4331Type = Get-PnPNetworkDeviceType -PnPHost "localhost" -HostPort $hostPort | Where-Object { $_.name -eq 'ISR4331' }

    if($null -eq $r4331Type) {
        $r4331Type = Add-PnPNetworkDeviceType -PnPHost "localhost" -HostPort $hostPort -Name 'ISR4331' -Manufacturer 'Cisco' -ProductId 'ISR4331-SEC/K9-WS'
    }

    $r1841Type = Get-PnPNetworkDeviceType -PnPHost "localhost" -HostPort $hostPort | Where-Object { $_.name -eq 'C1841' }

    if($null -eq $r1841Type) {
        $r1841Type = Add-PnPNetworkDeviceType -PnPHost "localhost" -HostPort $hostPort -Name 'C1841' -Manufacturer 'Cisco' -ProductId 'CISCO1841'
    }

    $s2960Type = Get-PnPNetworkDeviceType -PnPHost "localhost" -HostPort $hostPort | Where-Object { $_.name -eq 'C2960-24PS-TT' }

    if($null -eq $s2960Type) {
        $s2960Type = Add-PnPNetworkDeviceType -PnPHost "localhost" -HostPort $hostPort -Name 'C2960-24PS-TT' -Manufacturer 'Cisco' -ProductId 'C2960-24PS-TT'
    }

    $rIntList = @(
        @{ Name = 'GigabitEthernet0/0'; Index = 0 },
        @{ Name = 'GigabitEthernet0/1'; Index = 1 }
    )

    foreach($rInt in $rIntList) {
        $rIntValue = Get-PnPNetworkDeviceTypeInterface -PnPHost "localhost" -HostPort $hostPort -DeviceType $r4331Type.name | Where-Object { $_.Name -ilike $rInt.Name }

        if($null -eq $rIntValue) {
            $rIntValue = Add-PnpNetworkDeviceTypeInterface -PnPHost "localhost" -HostPort $hostPort -DeviceType 'ISR4331' @rInt
        }
    }

    if($null -eq (Get-PnPNetworkDeviceTypeInterface -PnPHost "localhost" -HostPort $hostPort -DeviceType $r1841Type.name | Where-Object { $_.Name -ilike 'FastEthernet0/0'})) {
        Add-PnPNetworkDeviceTypeInterfaceRange -PnPHost "localhost" -HostPort $hostPort -DeviceType $r1841Type.name -Name 'FastEthernet0/0' -FirstIndex 1 -Count 2
    }

    if($null -eq (Get-PnPNetworkDeviceTypeInterface -PnPHost "localhost" -HostPort $hostPort -DeviceType $s2960Type.name | Where-Object { $_.Name -ilike 'FastEthernet0/1'})) {
        Add-PnPNetworkDeviceTypeInterfaceRange -PnPHost "localhost" -HostPort $hostPort -DeviceType $s2960Type.name -Name 'FastEthernet0/1' -FirstIndex 1 -Count 24
    }
    if($null -eq (Get-PnPNetworkDeviceTypeInterface -PnPHost "localhost" -HostPort $hostPort -DeviceType $s2960Type.name | Where-Object { $_.Name -ilike 'GigabitEthernet0/1'})) {
        Add-PnPNetworkDeviceTypeInterfaceRange -PnPHost "localhost" -HostPort $hostPort -DeviceType $s2960Type.name -Name 'GigabitEthernet0/1' -FirstIndex 25 -Count 2
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

$configFile = Join-Path -Path $PSScriptRoot -ChildPath '.\SampleData\NavDemoStructured.config' -Resolve
$unprovisionedConfigFile = Join-Path -Path $PSScriptRoot -ChildPath '.\SampleData\unprovisioned.config.txt' -Resolve
$testConfig = Get-PnPConfigurationFile -Path $configFile
Invoke-PnPProcessTemplatesSection -HostPort $hostPort -Config $testConfig -ConfigPath $configFile -Force
Invoke-PnPProcessDevicesSection -HostPort $hostPort -Config $testConfig -Force
Invoke-PnPProcessConnections -HostPort $hostPort -Config $testConfig -Force -Verbose
Set-TFTPFile -HostPort $hostPort -FilePath 'unprovisioned.config.txt' -Content (Get-Content -Path $unprovisionedConfigFile -Raw)

