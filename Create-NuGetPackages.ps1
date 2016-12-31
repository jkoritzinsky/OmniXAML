[CmdletBinding()]
Param(
	[Parameter(Mandatory=$True)]
	[string]$version	
)

$csprojs = Get-ChildItem -Filter "*.csproj" -Recurse

foreach ($proj in $csprojs)
{

    $jsonPath = ($proj.DirectoryName) + '\project.json'
    $json = Get-Item $jsonPath

    $content = Get-Content $json.FullName
    $contains = $content -match 'packOptions'
    if ($contains) 
    {
        CreatePackage $proj $version
    }
}

function CreatePackage($file, $version)
{
    Write-Host 'Building package inside ' $file.DirectoryName
    & 'nuget.exe' 'pack' $file.FullName -Version $version
}