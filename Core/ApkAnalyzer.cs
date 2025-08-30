using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ApkAntiSplit.Models;
using ApkAntiSplit.Utils;

namespace ApkAntiSplit.Core
{
    public class ApkAnalyzer
    {
        public async Task<ApkInfo> AnalyzeApkAsync(string apkPath)
        {
            if (!File.Exists(apkPath))
                throw new FileNotFoundException($"APK file not found: {apkPath}");

            var apkInfo = new ApkInfo
            {
                FilePath = apkPath,
                FileSize = FileHelper.GetFileSize(apkPath)
            };

            try
            {
                // Extract basic information using aapt
                await ExtractApkInfoWithAapt(apkInfo);
                
                // Determine APK type
                DetermineApkType(apkInfo);
                
                // Extract architectures
                await ExtractArchitectures(apkInfo);
                
                return apkInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not fully analyze APK {Path.GetFileName(apkPath)}: {ex.Message}");
                
                // Fallback: try to determine basic info from filename
                ExtractInfoFromFilename(apkInfo);
                
                return apkInfo;
            }
        }

        private async Task ExtractApkInfoWithAapt(ApkInfo apkInfo)
        {
            try
            {
                var result = await AndroidSdkHelper.RunToolAsync("aapt", $"dump badging \"{apkInfo.FilePath}\"");
                
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException($"aapt failed: {result.GetErrorText()}");
                }

                var output = result.GetOutputText();
                
                // Parse package information
                var packageMatch = Regex.Match(output, @"package: name='([^']+)' versionCode='(\d+)' versionName='([^']+)'");
                if (packageMatch.Success)
                {
                    apkInfo.PackageName = packageMatch.Groups[1].Value;
                    apkInfo.VersionCode = int.Parse(packageMatch.Groups[2].Value);
                    apkInfo.VersionName = packageMatch.Groups[3].Value;
                }

                // Parse SDK versions
                var sdkVersionMatch = Regex.Match(output, @"sdkVersion:'(\d+)'");
                if (sdkVersionMatch.Success)
                {
                    apkInfo.MinSdkVersion = sdkVersionMatch.Groups[1].Value;
                }

                var targetSdkMatch = Regex.Match(output, @"targetSdkVersion:'(\d+)'");
                if (targetSdkMatch.Success)
                {
                    apkInfo.TargetSdkVersion = targetSdkMatch.Groups[1].Value;
                }

                // Parse permissions
                var permissionMatches = Regex.Matches(output, @"uses-permission: name='([^']+)'");
                foreach (Match match in permissionMatches)
                {
                    apkInfo.Permissions.Add(match.Groups[1].Value);
                }

                // Parse features
                var featureMatches = Regex.Matches(output, @"uses-feature: name='([^']+)'");
                foreach (Match match in featureMatches)
                {
                    apkInfo.Features.Add(match.Groups[1].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not extract APK info with aapt: {ex.Message}");
            }
        }

        private void DetermineApkType(ApkInfo apkInfo)
        {
            var fileName = Path.GetFileNameWithoutExtension(apkInfo.FilePath).ToLowerInvariant();
            
            if (fileName == "base" || fileName == apkInfo.PackageName)
            {
                apkInfo.Type = ApkType.Base;
            }
            else if (fileName.StartsWith("split.") || fileName.StartsWith("split_"))
            {
                apkInfo.Type = ApkType.Split;
                apkInfo.SplitName = fileName;
            }
            else if (fileName.StartsWith("config."))
            {
                apkInfo.Type = ApkType.Config;
                apkInfo.SplitName = fileName;
            }
            else
            {
                // Default to base if we can't determine
                apkInfo.Type = ApkType.Base;
            }
        }

        private async Task ExtractArchitectures(ApkInfo apkInfo)
        {
            try
            {
                var result = await AndroidSdkHelper.RunToolAsync("aapt", $"list \"{apkInfo.FilePath}\"");
                
                if (result.IsSuccess)
                {
                    var output = result.GetOutputText();
                    var libMatches = Regex.Matches(output, @"lib/([^/]+)/");
                    
                    foreach (Match match in libMatches)
                    {
                        var arch = match.Groups[1].Value;
                        if (!apkInfo.Architectures.Contains(arch))
                        {
                            apkInfo.Architectures.Add(arch);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not extract architectures: {ex.Message}");
            }
        }

        private void ExtractInfoFromFilename(ApkInfo apkInfo)
        {
            var fileName = Path.GetFileNameWithoutExtension(apkInfo.FilePath);
            
            // Try to extract package name from common patterns
            var patterns = new[]
            {
                @"^([a-z]+(?:\.[a-z]+)+)\.v?(\d+(?:\.\d+)*)",
                @"^([a-z]+(?:\.[a-z]+)+)_v?(\d+(?:\.\d+)*)",
                @"^([a-z]+(?:\.[a-z]+)+)-v?(\d+(?:\.\d+)*)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(fileName.ToLowerInvariant(), pattern);
                if (match.Success)
                {
                    apkInfo.PackageName = match.Groups[1].Value;
                    apkInfo.VersionName = match.Groups[2].Value;
                    break;
                }
            }

            // Determine type from filename
            DetermineApkType(apkInfo);
        }

        public async Task<AndroidManifestInfo> ExtractManifestAsync(string apkPath)
        {
            var tempDir = FileHelper.CreateTempDirectory("manifest_extract");
            
            try
            {
                // Extract AndroidManifest.xml using aapt
                var manifestPath = Path.Combine(tempDir, "AndroidManifest.xml");
                var result = await AndroidSdkHelper.RunToolAsync("aapt", 
                    $"dump xmltree \"{apkPath}\" AndroidManifest.xml");
                
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException($"Failed to extract manifest: {result.GetErrorText()}");
                }

                return ParseManifestFromAaptOutput(result.GetOutputText());
            }
            finally
            {
                FileHelper.SafeDeleteDirectory(tempDir);
            }
        }

        private AndroidManifestInfo ParseManifestFromAaptOutput(string aaptOutput)
        {
            var manifest = new AndroidManifestInfo();
            var lines = aaptOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Parse package name
                if (trimmedLine.Contains("package="))
                {
                    var packageMatch = Regex.Match(trimmedLine, @"package=""([^""]+)""");
                    if (packageMatch.Success)
                    {
                        manifest.PackageName = packageMatch.Groups[1].Value;
                    }
                }
                
                // Parse version code
                if (trimmedLine.Contains("versionCode="))
                {
                    var versionCodeMatch = Regex.Match(trimmedLine, @"versionCode=""(\d+)""");
                    if (versionCodeMatch.Success)
                    {
                        manifest.VersionCode = int.Parse(versionCodeMatch.Groups[1].Value);
                    }
                }
                
                // Parse version name
                if (trimmedLine.Contains("versionName="))
                {
                    var versionNameMatch = Regex.Match(trimmedLine, @"versionName=""([^""]+)""");
                    if (versionNameMatch.Success)
                    {
                        manifest.VersionName = versionNameMatch.Groups[1].Value;
                    }
                }
                
                // Parse SDK versions
                if (trimmedLine.Contains("minSdkVersion="))
                {
                    var minSdkMatch = Regex.Match(trimmedLine, @"minSdkVersion=""(\d+)""");
                    if (minSdkMatch.Success)
                    {
                        manifest.MinSdkVersion = minSdkMatch.Groups[1].Value;
                    }
                }
                
                if (trimmedLine.Contains("targetSdkVersion="))
                {
                    var targetSdkMatch = Regex.Match(trimmedLine, @"targetSdkVersion=""(\d+)""");
                    if (targetSdkMatch.Success)
                    {
                        manifest.TargetSdkVersion = targetSdkMatch.Groups[1].Value;
                    }
                }
            }
            
            return manifest;
        }

        public async Task<bool> ValidateApkConsistency(List<ApkInfo> apks)
        {
            if (!apks.Any())
                return false;

            var baseApk = apks.FirstOrDefault(a => a.Type == ApkType.Base);
            if (baseApk == null)
            {
                Console.WriteLine("Error: No base APK found in the package.");
                return false;
            }

            var packageName = baseApk.PackageName;
            var versionCode = baseApk.VersionCode;

            // Check all APKs have the same package name and version code
            foreach (var apk in apks)
            {
                if (!string.IsNullOrEmpty(apk.PackageName) && apk.PackageName != packageName)
                {
                    Console.WriteLine($"Error: Package name mismatch. Expected: {packageName}, Found: {apk.PackageName} in {Path.GetFileName(apk.FilePath)}");
                    return false;
                }

                if (apk.VersionCode != 0 && apk.VersionCode != versionCode)
                {
                    Console.WriteLine($"Error: Version code mismatch. Expected: {versionCode}, Found: {apk.VersionCode} in {Path.GetFileName(apk.FilePath)}");
                    return false;
                }
            }

            Console.WriteLine($"✓ All APKs validated for package: {packageName} (version: {baseApk.VersionName})");
            return true;
        }
    }
}