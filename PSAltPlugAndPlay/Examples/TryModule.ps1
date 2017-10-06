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

Function Set-PnPProcessTemplate {
    Param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter()]
        [switch]$Force
    )

    if (-not (Test-Path -Path $Path)) {
        throw [System.IO.FileNotFoundException]::new(
            'Failed to find specificed template file',
            $Path
        )
    }

    $content = '';
    try {
        $content = [System.IO.File]::ReadAllText($Path);
    } catch {
        throw [System.Exception]::new(
            'Failed to open template file specified : ' + $Path,
            $_.Exception
        )
    }

    if([string]::IsNullOrWhiteSpace($content)) {
        throw [System.Exception]::new(
            'Content of template file is empty. : ' + $Path
        )
    }

    $existingTemplate = Get-PnPTemplate -Name $Name
    if($null -ne $existingTemplate) {
        if(-not $Force) {
            throw [System.ArgumentException]::new(
                'Attempting to set a template, but the template already exists. Use -force if it is desired to overwrite the existing template',
                'Force'
            )
        }

        Write-Verbose -Message (
            'Existing template "' + $Name + '" already exists with id ' + $existingTemplate.id
        )

        if(
            ($existingTemplate.name -eq $name) -and 
            ($existingTemplate.content -eq $content)
        ) {
            Write-Verbose -Message (
                'Existing template ' + $existingTemplate.id + ' is already set the the content of ' + $Path + '. Returning without making changes'
            )
            return
        }

        if($existingTemplate.name -ne $name) {
            Write-Debug -Message (
                'Template ' + $existingTemplate.id + ' will change names from "' + $existingTemplate.name + '" to "' + $Name + "'"
            )
        }

        if($existingTemplate.content -ne $content) {
            Write-Debug -Message (
                'Content of template ' + $existingTemplate + ' will change'
            )
        }

        $template = Set-PnPTemplate -Id $existingTemplate.id -Name $Name -Content $content
        
        return $template

    } else {
        Write-Verbose -Message (
            'Template with name "' + $Name + '" does not exist. Creating...'
        )
        $template = Add-PnPTemplate -Name $Name -Content $content

        return $template
    }
}

Function Invoke-PnPProcessTemplatesSection {
    Param(
        [Parameter(Mandatory)]
        [PSObject]$Config,

        [Parameter(Mandatory)]
        [string]$ConfigPath,

        [Parameter()]
        [switch]$Force
    )

    $absoluteConfigPath = $ConfigPath
    if(-not [System.IO.Path]::IsPathRooted($absoluteConfigPath)) {
        $absoluteConfigPath = Join-Path -Path $PSScriptRoot -ChildPath $ConfigPath -Resolve
    }

    if(-not (Test-Path -Path $absoluteConfigPath -PathType Leaf)) {
        throw [System.ArgumentException]::new(
            'Provided ConfigPath is not a file',
            $ConfigPath
        )
    }

    $configRoot = [System.IO.Path]::GetDirectoryName($absoluteConfigPath)

    $Config.templates.ForEach({
        $templatePath = $_.path 
        if(-not [System.IO.Path]::IsPathRooted($templatePath)) {
            $templatePath = Join-Path -Path $configRoot -ChildPath $_.path
        }

        if(-not (Test-Path -Path $templatePath -PathType Leaf)) {
            throw [System.IO.FileNotFoundException]::new(
                'Template "' + $_.name + '" specifies file [' + $_.path + '] which either does not exist or is not a valid file',
                $templatePath
            )
        }

        Set-PnPProcessTemplate -Name $_.Name -Path $templatePath -Force:$Force
    })
}

Function Get-ChangedTemplateParameters {
    Param(
        [Parameter(Mandatory)]
        #[AllowNullAttribute()]
        [PSCustomObject[]]$OldParameters,

        [Parameter(Mandatory)]
        #[AllowNullAttribute()]
        [PSCustomObject[]]$NewParameters
    )

    $result = [PSCustomObject]@{
        removed = [PSCustomObject[]]@()
        changed = [PSCustomObject[]]@()
        added = [PSCustomObject[]]@()
    }

    foreach($oldParam in $OldParameters) {
        $newParam = $NewParameters | Where-Object { $_.name -eq $oldParam.name }
        if($null -eq $newParam) {
            $result.removed += $oldParam
        } else {
            if($oldParam.value -ne $newParam.value) {
                $result.changed += [PSCustomObject]@{
                    id = $oldParam.id
                    name = $oldParam.name
                    oldValue = $oldParam.value
                    newValue = $newParam.value
                }
            }
        }
    }

    foreach($newParam in $NewParameters) {
        $oldParam = $OldParameters | Where-Object { $_.name -eq $newParam.name }
        if($null -eq $oldParam) {
            $result.added += $newParam
        }
    }

    if(
        ($result.added.Count -eq 0) -and
        ($result.changed.Count -eq 0) -and
        ($result.removed.Count -eq 0)) {
        return $null
    }

    return $result
}

Function Invoke-ProcessPnPDevice {
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$DeviceConfig,

        [Parameter()]
        [switch]$Force
    )

    if(
        (-not (Get-Member -InputObject 'hostname'))
    ) {
        throw [System.ArgumentException]::new(
            'No hostname specified',
            'DeviceConfig'
        )        
    }

    if(
        (-not (Get-Member -InputObject 'domainName')) 
    ) {
        throw [System.ArgumentException]::new(
            'No domain name for host (' + $DeviceConfig.hostname + ') specified',
            'DeviceConfig'
        )        
    }

    if(
        (-not (Get-Member -InputObject 'deviceType')) 
    ) {
        throw [System.ArgumentException]::new(
            'No deviceType for host (' + $DeviceConfig.hostname + ') specified',
            'DeviceConfig'
        )        
    }

    # TODO : Validate ipAddress as network prefix

    $networkDevice = Get-PnPNetworkDevice -Hostname $DeviceConfig.hostname -DomainName $DeviceConfig.domainName
    if($null -eq $networkDevice) {
        Write-Verbose -Message (
            'Device ' + $DeviceConfig.hostname + '.' + $DeviceConfig.domainName + ' does not exist, creating'
        )

        $deviceType = Get-PnPNetworkDeviceType -Name $DeviceConfig.deviceType
        if($null -eq $deviceType) {
            throw [System.ArgumentException]::new(
                'Invalid device type specified for host : ' + $DeviceConfig.hostName + '. Has it been defined?',
                'DeviceConfig'
            )
        }

        $ipAddress = ''
        if($null -ne (Get-Member -InputObject 'ipAddress')) {
            $ipAddress = Get-IPAddressFromNetworkPrefix -Prefix $DeviceConfig.ipAddress
            #Add-Member -InputObject $addSplat -MemberType NoteProperty  -Name 'ipAddress' -Value $ipAddress            
        }

        $addSplat = [PSObject]@{
            Hostname        = $DeviceConfig.hostname 
            DomainName      = $DeviceConfig.domainName
            Description     = $DeviceConfig.description
            DeviceType      = $DeviceConfig.deviceType
            IPAddress       = $ipAddress
        }

        $networkDevice = Add-PnPNetworkDevice @addSplat
        if($null -ne $networkDevice) {
            Write-Verbose -Message (
                'Added new network device : ' + $DeviceConfig.hostname + '.' + $DeviceConfig.domainName
            )
        }
    } else {
        $updateNeeded = $false

        if(-not (Test-StringsEqual -left $networkDevice.hostname -right $DeviceConfig.hostname)) {
            $updateNeeded = $true
            Write-Debug -Message (
                'Hostname changed from ' + $networkDevice.hostname + ' to ' + $DeviceConfig.hostname + ', update needed'
            )
        }

        if(-not (Test-StringsEqual -left $networkDevice.domainName -right $DeviceConfig.domainName)) {
            $updateNeeded = $true
            Write-Debug -Message (
                'Domain name changed from ' + $networkDevice.domainName + ' to ' + $DeviceConfig.domainName + ', update needed'
            )
        }

        if(-not (Test-StringsEqual -left $networkDevice.description -right $DeviceConfig.description)) {
            $updateNeeded = $true
            Write-Debug -Message (
                'Description changed from ' + $networkDevice.description+ ' to ' + $DeviceConfig.description + ', update needed'
            )
        }

        $ipAddress = ''
        if($null -ne (Get-Member -InputObject 'ipAddress')) {
            $ipAddress = Get-IPAddressFromNetworkPrefix -Prefix $DeviceConfig.ipAddress
        }

        if($networkDevice.ipAddress -ne $ipAddress) {
            $updateNeeded = $true
            Write-Debug -Message (
                'IP address changed from ' + $networkDevice.ipAddress + ' to ' + $ipAddress + ', update needed'
            )
        }

        if($updateNeeded) {
            Write-Verbose -Message (
                'Changes detected to network device ' + $networkDevice.hostname + ', updating record'
            )
        }

        
        $updateSplat = @{
            Id              = $networkDevice.id
            Hostname        = $DeviceConfig.hostname 
            DomainName      = $DeviceConfig.domainName
            Description     = $DeviceConfig.description
            DeviceType      = $DeviceConfig.deviceType
            IPAddress       = $ipAddress
        }

        $networkDevice = Set-PnPNetworkDevice @updateSplat
    }

    if($null -eq $networkDevice) {
        throw [System.Exception]::new(
            'Could not create or update network device record for ' + $DeviceConfig.hostname + ' for unknown reason, contact support'
        )
    }

    $networkDevice | Write-Verbose

    $deviceTemplates = Get-PnPNetworkDeviceTemplates -Hostname $DeviceConfig.hostname -DomainName $DeviceConfig.domainName

    $parameters = $DeviceConfig.template.parameters
    $parameters += @(
        @{ name = 'deviceId';    value = $networkDevice.id }
        @{ name = 'hostname';    value = $networkDevice.hostname }
        @{ name = 'domainName';  value = $networkDevice.domainName }
        @{ name = 'deviceType';  value = $networkDevice.deviceType.name }
    )

    if($null -ne (Get-Member -InputObject 'ipAddress')) {
        $subnetMask = Get-SubnetMaskFromNetworkPrefix -prefix $DeviceConfig.ipAddress

        $parameters += @{
            name = 'subnetMask'
            value = $subnetMask
        }
    }

    if($null -eq $deviceTemplates) {
        Write-Debug -Message (
            'There is no device template currently stored for ' + $networkDevice.hostName
        )

        if($null -ne $DeviceConfig.template) {
            Write-Verbose -Message (
                'A new template (' + $DeviceConfig.template.name + ') is specified for network device : ' + $networkDevice.hostName
            )

            $template = Get-PnPTemplate -Name $DeviceConfig.template.name
            if($null -eq $template) {
                throw [System.Exception]::new(
                    'Template (' + $DeviceConfig.template.name + ') referenced by host ' + $DeviceConfig.hostname + ' is not present.'
                )
            }

            $setNetworkDeviceTemplateSplat = @{
                NetworkDevice        = ($networkDevice.hostname + '.' + $networkDevice.domainName) 
                Template             = $DeviceConfig.template.name 
                TemplateParameters   = $parameters
                Description          = $DeviceConfig.template.description
            }

            $networkDeviceTemplate = Set-PnPNetworkDeviceTemplate @setNetworkDeviceTemplateSplat
        }

    } elseif($deviceTemplates.Count > 1) {
        throw [System.Exception]::new(
            'There is more than one template configured for ' + $networkDevice.hostName
        )
    } else {
        Write-Debug -Message (
            'Template ' + $deviceTemplates.template.name + ' is configured for device ' + $networkDevice.hostname
        )

        $updateNeeded = $false

        if(-not (Test-StringsEqual -left $deviceTemplates.name -right $DeviceConfig.template.name)) {
            $updateNeeded = $true
            Write-Debug -Message (
                'Device (' + $networkDevice.hostname + ') template changed, update needed'
            )
        }

        if(-not (Test-StringsEqual -left $deviceTemplates.description -right $DeviceConfig.template.description)) {
            $updateNeeded = $true
            Write-Debug -Message (
                'Device (' + $networkDevice.hostname + ') template configuration description changed, update needed'
            )
        }

        if(-not (Test-StringsEqual -left $deviceTemplates.name -right $DeviceConfig.template.name)) {
            $updateNeeded = $true
            Write-Debug -Message (
                'Device (' + $networkDevice.hostname + ') template changed, update needed'
            )
        }

        Write-Verbose -Message (
            'There have been changes detected to the template, making changes now'
        )

        # TODO : You idiot!!! Did you forget to change the template?

        $parameterChanges = Get-ChangedTemplateParameters -OldParameters $deviceTemplates.properties -NewParameters $DeviceConfig.template.parameters

        if($null -eq $parameterChanges) {
            Write-Debug -Message (
                'There have been no changes to the template parameters'
            )
        } else {
            Write-Debug -Message 'Removing template parameters'
            foreach($removedItem in $result.removed) {
                Write-Debug -Message (
                    'Removing template parameter : ' + $removedItem.name
                )
            }
            #Remove-PnP
        }
    }
}

Function Invoke-PnPProcessDevicesSection {
    Param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Config,

        [Parameter()]
        [switch]$Force
    )

    $Config.devices.ForEach({
        Invoke-ProcessPnPDevice -DeviceConfig $_ -Force:$Force   
    })
}


$configFile = Join-Path -Path $PSScriptRoot -ChildPath '.\SampleData\NavDemoStructured.config' -Resolve
$testConfig = Get-PnPConfigurationFile -Path $configFile
Invoke-PnPProcessTemplatesSection -Config $testConfig -ConfigPath $configFile -Force
Invoke-PnPProcessDevicesSection -Config $testConfig -Force -Verbose
