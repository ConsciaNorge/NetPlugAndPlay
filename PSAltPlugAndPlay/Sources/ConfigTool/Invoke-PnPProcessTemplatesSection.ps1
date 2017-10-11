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
        Process the contents of the templates section from a config file
#>
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

    # TODO : Remove unused templates?
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

        # TODO : Better error handling
        Set-PnPProcessTemplate -Name $_.Name -Path $templatePath -Force:$Force
    })
}
