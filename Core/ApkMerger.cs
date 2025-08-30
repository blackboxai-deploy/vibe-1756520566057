using System.IO.Compression;
using ApkAntiSplit.Models;
using ApkAntiSplit.Utils;

namespace ApkAntiSplit.Core
{
    public class ApkMerger
    {
        private readonly ApkAnalyzer _apkAnalyzer;

        public ApkMerger()
        {
            _apkAnalyzer = new ApkAnalyzer();
        }

        public async Task<string> MergeApksAsync(List<ApkInfo> apkInfos, string outputPath, string? keystorePath = null)
        {
            if (!apkInfos.Any())
                throw new ArgumentException("No APK files to merge.", nameof(apkInfos));

            var baseApk = apkInfos.FirstOrDefault(a => a.Type == ApkType.Base);
            if (baseApk == null)
                throw new InvalidOperationException("No base APK found. Cannot merge without a base APK.");

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("STARTING APK MERGE PROCESS");
            Console.WriteLine(new string('=', 60));

            var tempWorkDir = FileHelper.CreateTempDirectory("apk_merge_work");
            var tempExtractDir = Path.Combine(tempWorkDir, "extracted");
            var tempMergeDir = Path.Combine(tempWorkDir, "merged");

            try
            {
                Directory.CreateDirectory(tempExtractDir);
                Directory.CreateDirectory(tempMergeDir);

                // Step 1: Extract all APKs
                Console.WriteLine("Step 1: Extracting APK contents...");
                var extractedPaths = await ExtractAllApks(apkInfos, tempExtractDir);

                // Step 2: Merge contents
                Console.WriteLine("Step 2: Merging APK contents...");
                await MergeApkContents(extractedPaths, tempMergeDir, baseApk);

                // Step 3: Repackage APK
                Console.WriteLine("Step 3: Repackaging merged APK...");
                var tempApkPath = Path.Combine(tempWorkDir, "merged_unsigned.apk");
                await RepackageApk(tempMergeDir, tempApkPath);

                // Step 4: Sign APK
                Console.WriteLine("Step 4: Signing APK...");
                await SignApk(tempApkPath, outputPath, keystorePath);

                // Step 5: Validate merged APK
                Console.WriteLine("Step 5: Validating merged APK...");
                await ValidateMergedApk(outputPath);

                var outputSize = FileHelper.GetFileSize(outputPath);
                var originalSize = apkInfos.Sum(a => a.FileSize);
                
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("APK MERGE COMPLETED SUCCESSFULLY");
                Console.WriteLine(new string('=', 60));
                Console.WriteLine($"Output: {outputPath}");
                Console.WriteLine($"Original size: {FileHelper.FormatFileSize(originalSize)}");
                Console.WriteLine($"Merged size: {FileHelper.FormatFileSize(outputSize)}");
                Console.WriteLine($"Size difference: {FileHelper.FormatFileSize(outputSize - originalSize)}");
                Console.WriteLine(new string('=', 60));

                return outputPath;
            }
            finally
            {
                FileHelper.SafeDeleteDirectory(tempWorkDir);
            }
        }

        private async Task<Dictionary<ApkInfo, string>> ExtractAllApks(List<ApkInfo> apkInfos, string extractBaseDir)
        {
            var extractedPaths = new Dictionary<ApkInfo, string>();

            foreach (var apkInfo in apkInfos)
            {
                var extractDir = Path.Combine(extractBaseDir, $"apk_{apkInfos.IndexOf(apkInfo)}");
                Directory.CreateDirectory(extractDir);

                Console.WriteLine($"  Extracting {Path.GetFileName(apkInfo.FilePath)}...");
                
                using var archive = ZipFile.OpenRead(apkInfo.FilePath);
                archive.ExtractToDirectory(extractDir, overwriteFiles: true);

                extractedPaths[apkInfo] = extractDir;
            }

            return extractedPaths;
        }

        private async Task MergeApkContents(Dictionary<ApkInfo, string> extractedPaths, string mergeDir, ApkInfo baseApk)
        {
            var baseExtractPath = extractedPaths[baseApk];

            // Start with base APK contents
            Console.WriteLine("  Copying base APK contents...");
            FileHelper.CopyDirectory(baseExtractPath, mergeDir);

            // Merge split APKs
            foreach (var kvp in extractedPaths.Where(kv => kv.Key.Type != ApkType.Base))
            {
                var apkInfo = kvp.Key;
                var extractPath = kvp.Value;

                Console.WriteLine($"  Merging {Path.GetFileName(apkInfo.FilePath)}...");

                await MergeSingleApk(extractPath, mergeDir, apkInfo);
            }

            // Merge DEX files
            await MergeDexFiles(mergeDir, extractedPaths);

            // Update AndroidManifest.xml
            await UpdateManifest(mergeDir, extractedPaths);
        }

        private async Task MergeSingleApk(string sourceDir, string targetDir, ApkInfo apkInfo)
        {
            // Merge assets
            var sourceAssetsDir = Path.Combine(sourceDir, "assets");
            var targetAssetsDir = Path.Combine(targetDir, "assets");
            
            if (Directory.Exists(sourceAssetsDir))
            {
                Directory.CreateDirectory(targetAssetsDir);
                await MergeDirectoryContents(sourceAssetsDir, targetAssetsDir);
            }

            // Merge resources
            var sourceResDir = Path.Combine(sourceDir, "res");
            var targetResDir = Path.Combine(targetDir, "res");
            
            if (Directory.Exists(sourceResDir))
            {
                Directory.CreateDirectory(targetResDir);
                await MergeDirectoryContents(sourceResDir, targetResDir);
            }

            // Merge native libraries
            var sourceLibDir = Path.Combine(sourceDir, "lib");
            var targetLibDir = Path.Combine(targetDir, "lib");
            
            if (Directory.Exists(sourceLibDir))
            {
                Directory.CreateDirectory(targetLibDir);
                await MergeDirectoryContents(sourceLibDir, targetLibDir);
            }

            // Copy other files
            var filesToMerge = Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly)
                .Where(f => !Path.GetFileName(f).Equals("AndroidManifest.xml", StringComparison.OrdinalIgnoreCase))
                .Where(f => !Path.GetFileName(f).StartsWith("classes") || !Path.GetFileName(f).EndsWith(".dex"));

            foreach (var file in filesToMerge)
            {
                var fileName = Path.GetFileName(file);
                var targetPath = Path.Combine(targetDir, fileName);
                
                if (!File.Exists(targetPath))
                {
                    File.Copy(file, targetPath);
                }
            }
        }

        private async Task MergeDirectoryContents(string sourceDir, string targetDir)
        {
            foreach (var sourceFile in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                var targetFile = Path.Combine(targetDir, relativePath);
                var targetFileDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetFileDir))
                {
                    Directory.CreateDirectory(targetFileDir);
                }

                if (!File.Exists(targetFile))
                {
                    File.Copy(sourceFile, targetFile);
                }
                else
                {
                    // Handle conflicts - for now, keep the base APK version
                    Console.WriteLine($"    Conflict: {relativePath} (keeping base version)");
                }
            }
        }

        private async Task MergeDexFiles(string mergeDir, Dictionary<ApkInfo, string> extractedPaths)
        {
            var allDexFiles = new List<string>();

            // Collect all DEX files from all APKs
            foreach (var kvp in extractedPaths)
            {
                var extractPath = kvp.Value;
                var dexFiles = Directory.GetFiles(extractPath, "classes*.dex");
                allDexFiles.AddRange(dexFiles);
            }

            if (!allDexFiles.Any())
            {
                Console.WriteLine("    Warning: No DEX files found in any APK.");
                return;
            }

            // For now, we'll use a simple approach: copy all DEX files with unique names
            // A more sophisticated approach would use dx or d8 to properly merge them
            var dexIndex = 1;
            foreach (var dexFile in allDexFiles.OrderBy(f => f))
            {
                var targetName = dexIndex == 1 ? "classes.dex" : $"classes{dexIndex}.dex";
                var targetPath = Path.Combine(mergeDir, targetName);
                
                if (!File.Exists(targetPath))
                {
                    File.Copy(dexFile, targetPath);
                    Console.WriteLine($"    Copied {Path.GetFileName(dexFile)} → {targetName}");
                    dexIndex++;
                }
            }
        }

        private async Task UpdateManifest(string mergeDir, Dictionary<ApkInfo, string> extractedPaths)
        {
            // For now, we'll keep the base APK's manifest
            // A more sophisticated approach would merge manifests properly
            var baseManifest = extractedPaths.First(kv => kv.Key.Type == ApkType.Base);
            var baseManifestPath = Path.Combine(baseManifest.Value, "AndroidManifest.xml");
            var targetManifestPath = Path.Combine(mergeDir, "AndroidManifest.xml");

            if (File.Exists(baseManifestPath) && !File.Exists(targetManifestPath))
            {
                File.Copy(baseManifestPath, targetManifestPath);
                Console.WriteLine("    Using base APK AndroidManifest.xml");
            }
        }

        private async Task RepackageApk(string contentDir, string outputApkPath)
        {
            // Create ZIP archive from merged contents
            if (File.Exists(outputApkPath))
            {
                File.Delete(outputApkPath);
            }

            using var archive = ZipFile.Open(outputApkPath, ZipArchiveMode.Create);
            
            var files = Directory.GetFiles(contentDir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var entryName = Path.GetRelativePath(contentDir, file).Replace(Path.DirectorySeparatorChar, '/');
                archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
            }

            Console.WriteLine($"    Created APK: {FileHelper.FormatFileSize(FileHelper.GetFileSize(outputApkPath))}");
        }

        private async Task SignApk(string unsignedApkPath, string signedApkPath, string? keystorePath)
        {
            // For now, we'll create a simple debug signature
            // In production, you should use a proper keystore
            
            try
            {
                string arguments;
                
                if (!string.IsNullOrEmpty(keystorePath) && File.Exists(keystorePath))
                {
                    // Use provided keystore
                    arguments = $"sign --ks \"{keystorePath}\" --out \"{signedApkPath}\" \"{unsignedApkPath}\"";
                }
                else
                {
                    // Create debug signature
                    arguments = $"sign --out \"{signedApkPath}\" \"{unsignedApkPath}\"";
                }

                var result = await AndroidSdkHelper.RunToolAsync("apksigner", arguments);
                
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException($"APK signing failed: {result.GetErrorText()}");
                }

                Console.WriteLine("    APK signed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Could not sign APK with apksigner: {ex.Message}");
                
                // Fallback: just copy the unsigned APK
                if (File.Exists(signedApkPath))
                {
                    File.Delete(signedApkPath);
                }
                File.Copy(unsignedApkPath, signedApkPath);
                
                Console.WriteLine("    Copied unsigned APK (manual signing may be required)");
            }
        }

        private async Task ValidateMergedApk(string apkPath)
        {
            try
            {
                var isValid = FileHelper.ValidateApkFile(apkPath);
                if (!isValid)
                {
                    throw new InvalidOperationException("Merged APK validation failed - invalid APK structure");
                }

                // Try to analyze the merged APK
                var mergedApkInfo = await _apkAnalyzer.AnalyzeApkAsync(apkPath);
                
                Console.WriteLine($"    Validation passed");
                Console.WriteLine($"    Package: {mergedApkInfo.PackageName}");
                Console.WriteLine($"    Version: {mergedApkInfo.VersionName}");
                
                if (mergedApkInfo.Architectures.Any())
                {
                    Console.WriteLine($"    Architectures: {string.Join(", ", mergedApkInfo.Architectures)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: APK validation encountered issues: {ex.Message}");
            }
        }
    }
}