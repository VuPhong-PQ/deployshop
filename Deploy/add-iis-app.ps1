# Script add Virtual Application /shop into IIS
$logFile = "C:\shop\Deploy\iis-add.log"
"Starting IIS Application configuration at $(Get-Date)" | Out-File $logFile -Encoding UTF8

$AppName = "shop"
$PhysicalPath = "C:\shop\dist"
$AppPoolName = "ShopAppPool"

try {
    Import-Module WebAdministration -ErrorAction Stop
    "Imported WebAdministration module successfully" | Out-File $logFile -Append -Encoding UTF8

    # Tu dong tim Web Site dang chay tren Port 80
    $allSites = Get-Website
    "Found sites: $(($allSites | ForEach-Object { $_.Name }) -join ', ')" | Out-File $logFile -Append -Encoding UTF8

    $site = $allSites | Where-Object {
        $_.Bindings.Collection | Where-Object { $_.bindingInformation -like '*:80:*' -or $_.bindingInformation -like '*:80' }
    } | Select-Object -First 1

    if (-not $site) {
        $site = $allSites[0]
    }

    $SiteName = $site.Name
    "Selected Site Name for Port 80: '$SiteName'" | Out-File $logFile -Append -Encoding UTF8

    # 1. Create AppPool
    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
        New-Item -Path "IIS:\AppPools\$AppPoolName" -Force | Out-Null
        Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
        "Created AppPool $AppPoolName" | Out-File $logFile -Append -Encoding UTF8
    } else {
        "AppPool $AppPoolName already exists" | Out-File $logFile -Append -Encoding UTF8
    }

    # 2. Create or update WebApplication
    $AppPath = "IIS:\Sites\$SiteName\$AppName"
    if (-not (Test-Path $AppPath)) {
        New-WebApplication -Site $SiteName -Name $AppName -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName -Force | Out-Null
        "Created WebApplication $AppName under site '$SiteName'" | Out-File $logFile -Append -Encoding UTF8
    } else {
        Set-ItemProperty -Path $AppPath -Name "physicalPath" -Value $PhysicalPath
        Set-ItemProperty -Path $AppPath -Name "applicationPool" -Value $AppPoolName
        "Updated WebApplication $AppName under site '$SiteName'" | Out-File $logFile -Append -Encoding UTF8
    }

    Restart-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    "SUCCESS: Configured IIS Virtual Application /shop successfully under '$SiteName'!" | Out-File $logFile -Append -Encoding UTF8
    Write-Host "HOAN TAT THIET LAP VIRTUAL APP /shop DUOI SITE '$SiteName'!" -ForegroundColor Green

} catch {
    "ERROR: $($_.Exception.Message)" | Out-File $logFile -Append -Encoding UTF8
    Write-Host "LOI: $($_.Exception.Message)" -ForegroundColor Red

    # Fallback directly using appcmd with all sites
    $appcmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    if (Test-Path $appcmd) {
        $siteList = & $appcmd list site /text:name
        $targetSite = $siteList[0]
        "Appcmd fallback target site: '$targetSite'" | Out-File $logFile -Append -Encoding UTF8

        & $appcmd add apppool /name:"$AppPoolName" /managedRuntimeVersion:"" 2>&1 | Out-File $logFile -Append -Encoding UTF8
        & $appcmd add app /site.name:"$targetSite" /path:"/$AppName" /physicalPath:"$PhysicalPath" /applicationPool:"$AppPoolName" 2>&1 | Out-File $logFile -Append -Encoding UTF8
        & $appcmd set app /app.name:"$targetSite/$AppName" /applicationPool:"$AppPoolName" /physicalPath:"$PhysicalPath" 2>&1 | Out-File $logFile -Append -Encoding UTF8
        "SUCCESS appcmd.exe completed!" | Out-File $logFile -Append -Encoding UTF8
    }
}
