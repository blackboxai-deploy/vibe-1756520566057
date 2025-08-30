@echo off
REM APK Anti-Split Tool - Sample Usage Examples
REM Make sure the tool is compiled and available in your PATH

echo.
echo ======================================================
echo APK Anti-Split Tool - Sample Usage Examples
echo ======================================================
echo.

REM Example 1: Basic merge with default output
echo Example 1: Basic merge with default output
echo Command: ApkAntiSplit.exe MyGame.xapk
echo.
REM ApkAntiSplit.exe MyGame.xapk

REM Example 2: Merge with custom output path
echo Example 2: Merge with custom output path
echo Command: ApkAntiSplit.exe MyGame.xapk --output "C:\Games\MyGame_merged.apk"
echo.
REM ApkAntiSplit.exe MyGame.xapk --output "C:\Games\MyGame_merged.apk"

REM Example 3: Display APK information only
echo Example 3: Display APK information only
echo Command: ApkAntiSplit.exe MyGame.xapk --info
echo.
REM ApkAntiSplit.exe MyGame.xapk --info

REM Example 4: Extract APKs without merging
echo Example 4: Extract APKs without merging
echo Command: ApkAntiSplit.exe MyGame.xapk --extract "C:\ExtractedAPKs"
echo.
REM ApkAntiSplit.exe MyGame.xapk --extract "C:\ExtractedAPKs"

REM Example 5: Merge with custom keystore
echo Example 5: Merge with custom keystore
echo Command: ApkAntiSplit.exe MyGame.xapk --keystore "release.jks" --output "signed_game.apk"
echo.
REM ApkAntiSplit.exe MyGame.xapk --keystore "release.jks" --output "signed_game.apk"

REM Example 6: Verbose output for debugging
echo Example 6: Verbose output for debugging
echo Command: ApkAntiSplit.exe MyGame.xapk --verbose
echo.
REM ApkAntiSplit.exe MyGame.xapk --verbose

REM Example 7: Specify Android SDK path
echo Example 7: Specify Android SDK path
echo Command: ApkAntiSplit.exe MyGame.xapk --android-sdk "C:\Android\Sdk"
echo.
REM ApkAntiSplit.exe MyGame.xapk --android-sdk "C:\Android\Sdk"

REM Example 8: Batch processing multiple files
echo Example 8: Batch processing multiple files
echo.
echo for %%f in (*.xapk) do (
echo     echo Processing %%f...
echo     ApkAntiSplit.exe "%%f" --output "merged_%%~nf.apk"
echo )
echo.

echo ======================================================
echo To run these examples, uncomment the lines above and
echo make sure you have:
echo 1. The ApkAntiSplit.exe in your PATH
echo 2. Android SDK installed and configured
echo 3. Sample XAPK files to test with
echo ======================================================
echo.

pause