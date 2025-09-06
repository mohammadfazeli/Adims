@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem ========= گرفتن ورودی =========
set "old=%~1"
set "new=%~2"

if not defined old (
  set /p "old=متنِ موجود در نام‌ها که باید جایگزین شود [پیش‌فرض: Sample]: "
)
if not defined old set "old=Sample"

if not defined new (
  set /p "new=متنِ جایگزین (مثلاً Test): "
)

if not defined new (
  echo جایگزین خالی نمي‌تواند باشد.
  exit /b 1
)

echo.
echo در حال تغییر نام همه فایل‌ها و فولدرها در "%cd%"
echo جستجو براي "%old%" و جايگزيني با "%new%"
echo.

rem ========= مرحله ۱: rename فولدرها از عمق بیشتر به کمتر =========
for /f "delims=" %%D in ('dir /ad /b /s ^| sort /R') do (
  set "name=%%~nxD"
  set "newname=!name:%old%=%new%!"
  if /I not "!name!"=="!newname!" (
    pushd "%%~dpD" >nul
    if exist "!newname!" (
      echo [SKIP] فولدر "%%~nxD" -> "!newname!"  (نام مقصد موجود است)
    ) else (
      echo [DIR ] "%%~nxD" -> "!newname!"
      ren "%%~nxD" "!newname!"
    )
    popd >nul
  )
)

rem ========= مرحله ۲: rename فایل‌ها =========
for /f "delims=" %%F in ('dir /a-d /b /s') do (
  set "path=%%F"
  set "name=%%~nxF"
  set "dir=%%~dpF"
  set "newname=!name:%old%=%new%!"
  if /I not "!name!"=="!newname!" (
    pushd "!dir!" >nul
    if exist "!newname!" (
      echo [SKIP] فايل "!name!" -> "!newname!"  (نام مقصد موجود است)
    ) else (
      echo [FILE] "!name!" -> "!newname!"
      ren "!name!" "!newname!"
    )
    popd >nul
  )
)

echo.
echo همه تغییر نام‌ها انجام شد.
endlocal
