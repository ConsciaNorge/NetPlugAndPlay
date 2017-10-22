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
        Reads the contents of a template file, then creates or updates the record on the database server

    .PARAMETER Name
        The name of the template as it is referenced elsewhere in the configuration file

    .PARAMETER Path
        The path of the template file to read and update
#>
Function Set-PnPProcessTemplate {
    Param(
        [Parameter()]
        [string] $PnPHost = 'localhost',

        [Parameter()]
        [int] $HostPort = 80,
        
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter()]
        [switch]$Force
    )

    Begin {
        if (-not (Test-Path -Path $Path)) {
            throw [System.IO.FileNotFoundException]::new(
                'Failed to find specificed template file',
                $Path
            )
        }
    }

    Process {
        $hostParams = @{
            PnPHost = $PnPHost
            HostPort = $HostPort
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

        $existingTemplate = Get-PnPTemplate @hostParams -Name $Name
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

            $template = Set-PnPTemplate @hostParams -Id $existingTemplate.id -Name $Name -Content $content
        
            return $template

        } else {
            Write-Verbose -Message (
                'Template with name "' + $Name + '" does not exist. Creating...'
            )
            $template = Add-PnPTemplate @hostParams -Name $Name -Content $content

            return $template
        }
    }
}
