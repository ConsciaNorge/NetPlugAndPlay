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
        ($result.added.Count -gt 0) -and
        ($result.changed.Count -gt 0) -and
        ($result.removed.Count -gt 0)) {
        return $result
    }

    return $null
}
