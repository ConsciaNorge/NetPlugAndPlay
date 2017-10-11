$modulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\PSAltPlugAndPlay.psd1' -Resolve
Import-Module -Name $modulePath -Force

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













$configFile = Join-Path -Path $PSScriptRoot -ChildPath '.\SampleData\NavDemoStructured.config' -Resolve
$testConfig = Get-PnPConfigurationFile -Path $configFile
Invoke-PnPProcessTemplatesSection -Config $testConfig -ConfigPath $configFile -Force
Invoke-PnPProcessDevicesSection -Config $testConfig -Force
Invoke-PnPProcessConnections -Config $testConfig -Force -Verbose