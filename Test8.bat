@echo off
setlocal

:: گرفتن ورودی‌ها
set "old=%~1"
set "new=%~2"

if not defined old (
  set /p "old=متن قدیمی (مثلاً Sample): "
)
if not defined new (
  set /p "new=متن جدید (مثلاً Test): "
)

if not defined old (
  echo متن قدیمی نباید خالی باشد.
  exit /b 1
)
if not defined new (
  echo متن جدید نباید خالی باشد.
  exit /b 1
)

echo.
echo در حال تغییر "%old%" -> "%new%" در همه فایل‌ها، فولدرها و محتوا...
echo.

:: اجرای PowerShell
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
echo همه تغییرها انجام شد.
endlocal
