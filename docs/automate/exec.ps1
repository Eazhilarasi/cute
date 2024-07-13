. .\docs\automate\Get-Screen.ps1
[Console]::CursorVisible = $false
cls && dotnet run --project .\source\Cute\Cute.csproj -- -help && Get-Screen .\docs\images\help.png -CropBottom 550
cls && dotnet run --project .\source\Cute\Cute.csproj -- login -help && Get-Screen .\docs\images\login.png -CropBottom 850
[Console]::CursorVisible = $true
