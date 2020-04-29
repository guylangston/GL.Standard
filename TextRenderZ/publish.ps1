# File Version 2.0
$proj = ls *.csproj | Select-Object -First 1
$name = [System.IO.Path]::GetFileNameWithoutExtension(($proj).Name)
$outFolder = "C:\Projects\LocalNuGet"
$isPackage = $true
$isWebApp = $false
# $scp = ""
# $scppw = ""

echo "Reading: ./package-version.txt"
$verList = Get-Content ./package-version.txt
$ver = $verList[1]
$tag = $verList[4]
$framework = (Select-Xml -Path $proj -XPath "//TargetFramework" | Select-Object Node).Node.InnerText

echo "        Package: '$name'"
echo "Package-Version: '$ver' tag: '$tag'"
echo "      Framework: $framework"
echo "         Output: $outFolder"


$confirmation = Read-Host "Does the version look correct? y/n"
if ($confirmation -eq 'y') {

    if ($isPackage) {

        pushd
    
        dotnet build -c Release --no-incremental -p:WarningLevel=0  "-p:PackageVersion=$ver"
        if ($LASTEXITCODE -ne 0) {throw $LASTEXITCODE}
    
        dotnet pack -c Release "-p:PackageVersion=$ver" --no-build
        if ($LASTEXITCODE -ne 0) {throw $LASTEXITCODE}

        # Do this last in case of error
        echo "[Tagging]"
        & git tag -a $ver -m "$tag"
        if ($LASTEXITCODE -ne 0) {throw $LASTEXITCODE}
        
        
        $outFile = "$outFolder\$name.$ver.nupkg";
        copy ".\bin\Release\$name.$ver.nupkg"  $outFile
        
        echo " ==> $outFile"

        if (Test-Path ~/nuget-apikey.txt)
        {
            $apikey = Get-Content ~/nuget-apikey.txt
            nuget push  $outFile -ApiKey $apikey -Source https://www.nuget.org/
        }
        
        popd
    }
    else {
        pushd
    
        dotnet build -c Release --no-incremental -p:WarningLevel=0 
        if ($LASTEXITCODE -ne 0) {throw $LASTEXITCODE}
    
        dotnet publish -c Release --no-build
        if ($LASTEXITCODE -ne 0) {throw $LASTEXITCODE}

        $pubFolder = ".\bin\Release\$framework\publish\"

        copy -Force ./git-label.txt $pubFolder
        copy -Force ./Package-Version.txt $pubFolder

        cd $pubFolder

        echo "[Cleaning]"
        rm appsettings*.json
        rm app.db

        $zipFile = "$name--$ver.zip"
        $zip = "$outFolder/$zipFile"
        if ((Test-Path $zip)) {
            rm $zip
        }
        
        echo "[Zip] $zip"
        & 7z.exe a $zip    
        if ($LASTEXITCODE -ne 0) {throw $LASTEXITCODE}

        # Do this last in case of error
        echo "[Tagging]"
        & git tag -a $ver -m "$tag"
        if ($LASTEXITCODE -ne 0) {throw $LASTEXITCODE}

        if ($scp -ne $null){
            
            echo "[Upload]"
            & PSCP.EXE -pw $scppw $zip $scp
            if ($LASTEXITCODE -ne 0) {throw $LASTEXITCODE}
        }
        
        popd
    }

}

