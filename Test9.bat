@echo off
setlocal

:: Get input values
set "old=%~1"
set "new=%~2"

if not defined old (
  set /p "old=Old text (e.g. Sample): "
)
if not defined new (
  set /p "new=New text (e.g. Test): "
)

if not defined old (
  echo Old text cannot be empty.
  exit /b 1
)
if not defined new (
  echo New text cannot be empty.
  exit /b 1
)

echo.
echo Replacing "%old%" -> "%new%" in all files, folders, and file contents...
echo.

:: Run PowerShell for renaming and content replacement
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass ^
  -Command ^
  "$old='%old%'; $new='%new%';" ^
  "Get-ChildItem -Recurse -Directory | Sort-Object FullName -Descending | ForEach-Object {" ^
  "  $newName = $_.Name -replace [Regex]::Escape($old), $new;" ^
  "  if ($_.Name -ne $newName) {" ^
  "    Rename-Item -LiteralPath $_.FullName -NewName $newName -ErrorAction SilentlyContinue;" ^
  "    Write-Host '[DIR ]' $_.FullName '->' $newName" ^
  "  }" ^
  "};" ^
  "Get-ChildItem -Recurse -File | ForEach-Object {" ^
  "  $newName = $_.Name -replace [Regex]::Escape($old), $new;" ^
  "  if ($_.Name -ne $newName) {" ^
  "    Rename-Item -LiteralPath $_.FullName -NewName $newName -ErrorAction SilentlyContinue;" ^
  "    Write-Host '[FILE]' $_.FullName '->' $newName" ^
  "  }" ^
  "};" ^
  "Get-ChildItem -Recurse -File | ForEach-Object {" ^
  "  try {" ^
  "    (Get-Content $_.FullName -Raw) -replace [Regex]::Escape($old), $new | Set-Content -Encoding UTF8 $_.FullName;" ^
  "    Write-Host '[TEXT]' $_.FullName" ^
  "  } catch {}" ^
  "}"

echo.
echo All renaming and replacements completed.
endlocal
