@echo off
setlocal

:: ===== Get user inputs =====
set "old=%~1"
set "new=%~2"

if not defined old (
  set /p "old=Old project name (e.g. Sample): "
)
if not defined new (
  set /p "new=New project name (e.g. Test): "
)

if not defined old (
  echo Old name cannot be empty.
  exit /b 1
)
if not defined new (
  echo New name cannot be empty.
  exit /b 1
)

:: ===== Check current folder name =====
for %%I in ("%cd%") do set "currentFolder=%%~nxI"

if /I not "%currentFolder%"=="%old%" (
  echo Current folder name "%currentFolder%" does not match "%old%".
  echo No changes will be made.
  exit /b 0
)

:: ===== Check .sln file exists =====
if not exist "%old%.sln" (
  echo Solution file "%old%.sln" not found in current folder.
  echo No changes will be made.
  exit /b 0
)

:: Save batch file name (to exclude it later)
set "thisScript=%~nx0"

echo.
echo Starting rename in "%cd%"
echo Project "%old%" -> "%new%"
echo.

:: ===== Run PowerShell =====
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass ^
  -Command ^
  "$old='%old%'; $new='%new%'; $exclude='%thisScript%';" ^
  "$root = Get-Location;" ^
  "Rename-Item -LiteralPath (Join-Path $root ($old + '.sln')) -NewName ($new + '.sln');" ^
  "Get-ChildItem -Recurse -Directory | Sort-Object FullName -Descending | ForEach-Object {" ^
  "  $newName = $_.Name -replace [Regex]::Escape($old), $new;" ^
  "  if ($_.Name -ne $newName) { Rename-Item -LiteralPath $_.FullName -NewName $newName -ErrorAction SilentlyContinue; Write-Host '[DIR ]' $_.FullName '->' $newName }" ^
  "};" ^
  "Get-ChildItem -Recurse -File | ForEach-Object {" ^
  "  if ($_.Name -ieq $exclude) { return }" ^
  "  $newName = $_.Name -replace [Regex]::Escape($old), $new;" ^
  "  if ($_.Name -ne $newName) { Rename-Item -LiteralPath $_.FullName -NewName $newName -ErrorAction SilentlyContinue; Write-Host '[FILE]' $_.FullName '->' $newName }" ^
  "};" ^
  "Get-ChildItem -Recurse -File | ForEach-Object {" ^
  "  if ($_.Name -ieq $exclude) { return }" ^
  "  try { (Get-Content $_.FullName -Raw) -replace [Regex]::Escape($old), $new | Set-Content -Encoding UTF8 $_.FullName; Write-Host '[TEXT]' $_.FullName } catch {}" ^
  "}"

echo.
echo Project rename completed (script file was excluded).
endlocal
