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

:: ===== Verify .sln and folder exist =====
if not exist "%old%.sln" (
  echo Solution file "%old%.sln" not found in current directory.
  exit /b 1
)
if not exist "%old%\" (
  echo Project folder "%old%" not found in current directory.
  exit /b 1
)

:: ===== Check that old matches the real project name =====
for %%F in (*.sln) do (
  set "realName=%%~nF"
)

if /I not "%old%"=="%realName%" (
  echo Input name "%old%" does not match actual solution name "%realName%".
  echo No changes will be made.
  exit /b 0
)

echo.
echo Starting rename: "%old%" -> "%new%"
echo.

:: ===== Run PowerShell for renaming and content replacement =====
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass ^
  -Command ^
  "$old='%old%'; $new='%new%';" ^
  "$root = Get-Location;" ^
  "Rename-Item -LiteralPath (Join-Path $root ($old + '.sln')) -NewName ($new + '.sln');" ^
  "if (Test-Path (Join-Path $root $old)) { Rename-Item -LiteralPath (Join-Path $root $old) -NewName $new; }" ^
  "Get-ChildItem -Recurse -Directory | Sort-Object FullName -Descending | ForEach-Object {" ^
  "  $newName = $_.Name -replace [Regex]::Escape($old), $new;" ^
  "  if ($_.Name -ne $newName) { Rename-Item -LiteralPath $_.FullName -NewName $newName -ErrorAction SilentlyContinue; Write-Host '[DIR ]' $_.FullName '->' $newName }" ^
  "};" ^
  "Get-ChildItem -Recurse -File | ForEach-Object {" ^
  "  $newName = $_.Name -replace [Regex]::Escape($old), $new;" ^
  "  if ($_.Name -ne $newName) { Rename-Item -LiteralPath $_.FullName -NewName $newName -ErrorAction SilentlyContinue; Write-Host '[FILE]' $_.FullName '->' $newName }" ^
  "};" ^
  "Get-ChildItem -Recurse -File | ForEach-Object {" ^
  "  try { (Get-Content $_.FullName -Raw) -replace [Regex]::Escape($old), $new | Set-Content -Encoding UTF8 $_.FullName; Write-Host '[TEXT]' $_.FullName } catch {}" ^
  "}"

echo.
echo Project rename completed.
endlocal
