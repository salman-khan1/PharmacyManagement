@echo off
echo ========================================
echo Pharmacy Management System Build
echo ========================================

echo.
echo [1/4] Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% NEQ 0 goto ERROR

echo.
echo [2/4] Building solution...
dotnet build --no-restore --configuration Release
if %ERRORLEVEL% NEQ 0 goto ERROR

echo.
echo [3/4] Running unit tests...
dotnet test --no-build --verbosity normal

echo.
echo [4/4] Publishing application...
if exist "publish" rmdir /s /q publish
dotnet publish src\PharmacyManagement.UI\PharmacyManagement.UI.csproj --no-build --configuration Release --output publish --self-contained false
if %ERRORLEVEL% NEQ 0 goto ERROR

echo.
echo ========================================
echo Build completed successfully!
echo Published to: publish\
echo ========================================
goto END

:ERROR
echo.
echo Build failed!
exit /b 1

:END
