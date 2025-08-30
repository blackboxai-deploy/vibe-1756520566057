using System.Text.Json;
using ApkAntiSplit.Models;
using ApkAntiSplit.Utils;

namespace ApkAntiSplit.Core
{
    public class XapkProcessor
    {
        private readonly ApkAnalyzer _apkAnalyzer;

        public XapkProcessor()
        {
            _apkAnalyzer = new ApkAnalyzer();
        }

        public async Task<List<ApkInfo>> ProcessXapkFileAsync(string xapkPath)
        {
            if (!FileHelper.IsXapkFile(xapkPath))
                throw new ArgumentException("File is not a valid XAPK or ZIP file.", nameof(xapkPath));

            Console.WriteLine($"Processing XAPK file: {Path.GetFileName(xapkPath)}");
            Console.WriteLine($"File size: {FileHelper.FormatFileSize(FileHelper.GetFileSize(xapkPath))}");

            var tempExtractDir = FileHelper.CreateTempDirectory("xapk_extract");
            
            try
            {
                // Extract XAPK/ZIP file
                Console.WriteLine("Extracting XAPK archive...");
                FileHelper.ExtractZipFile(xapkPath, tempExtractDir);

                // Look for manifest.json (XAPK format)
                var manifestPath = Path.Combine(tempExtractDir, "manifest.json");
                XapkManifest? xapkManifest = null;

                if (File.Exists(manifestPath))
                {
                    Console.WriteLine("Found XAPK manifest, parsing...");
                    xapkManifest = await ParseXapkManifest(manifestPath);
                }

                // Find all APK files in the extracted directory
                var apkFiles = FileHelper.GetApkFiles(tempExtractDir);
                
                if (!apkFiles.Any())
                {
                    throw new InvalidOperationException("No valid APK files found in the XAPK archive.");
                }

                Console.WriteLine($"Found {apkFiles.Count} APK file(s):");
                foreach (var apk in apkFiles)
                {
                    Console.WriteLine($"  - {Path.GetFileName(apk)} ({FileHelper.FormatFileSize(FileHelper.GetFileSize(apk))})");
                }

                // Analyze each APK file
                var apkInfos = new List<ApkInfo>();
                foreach (var apkFile in apkFiles)
                {
                    Console.WriteLine($"Analyzing {Path.GetFileName(apkFile)}...");
                    var apkInfo = await _apkAnalyzer.AnalyzeApkAsync(apkFile);
                    apkInfos.Add(apkInfo);
                }

                // Sort APKs - base first, then splits
                apkInfos = apkInfos
                    .OrderBy(a => a.Type == ApkType.Base ? 0 : a.Type == ApkType.Split ? 1 : 2)
                    .ThenBy(a => a.SplitName)
                    .ToList();

                // Validate consistency
                var isValid = await _apkAnalyzer.ValidateApkConsistency(apkInfos);
                if (!isValid)
                {
                    throw new InvalidOperationException("APK files are not consistent. Cannot proceed with merging.");
                }

                // Display summary
                DisplayApkSummary(apkInfos, xapkManifest);

                return apkInfos;
            }
            finally
            {
                FileHelper.SafeDeleteDirectory(tempExtractDir);
            }
        }

        private async Task<XapkManifest> ParseXapkManifest(string manifestPath)
        {
            try
            {
                var jsonContent = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<XapkManifest>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return manifest ?? throw new InvalidOperationException("Failed to parse XAPK manifest.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse XAPK manifest: {ex.Message}", ex);
            }
        }

        private void DisplayApkSummary(List<ApkInfo> apkInfos, XapkManifest? xapkManifest)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("APK SUMMARY");
            Console.WriteLine(new string('=', 60));

            var baseApk = apkInfos.FirstOrDefault(a => a.Type == ApkType.Base);
            if (baseApk != null)
            {
                Console.WriteLine($"Package: {baseApk.PackageName}");
                Console.WriteLine($"Version: {baseApk.VersionName} (Code: {baseApk.VersionCode})");
                if (!string.IsNullOrEmpty(baseApk.MinSdkVersion))
                    Console.WriteLine($"Min SDK: {baseApk.MinSdkVersion}");
                if (!string.IsNullOrEmpty(baseApk.TargetSdkVersion))
                    Console.WriteLine($"Target SDK: {baseApk.TargetSdkVersion}");
            }

            Console.WriteLine($"\nAPK Files ({apkInfos.Count} total):");
            Console.WriteLine(new string('-', 60));

            foreach (var apk in apkInfos)
            {
                var fileName = Path.GetFileName(apk.FilePath);
                var typeStr = apk.Type switch
                {
                    ApkType.Base => "BASE",
                    ApkType.Split => "SPLIT",
                    ApkType.Config => "CONFIG",
                    _ => "UNKNOWN"
                };

                Console.WriteLine($"  {fileName,-30} {typeStr,-8} {FileHelper.FormatFileSize(apk.FileSize),-10}");
                
                if (!string.IsNullOrEmpty(apk.SplitName))
                {
                    Console.WriteLine($"    Split: {apk.SplitName}");
                }

                if (apk.Architectures.Any())
                {
                    Console.WriteLine($"    Arch: {string.Join(", ", apk.Architectures)}");
                }
            }

            var totalSize = apkInfos.Sum(a => a.FileSize);
            Console.WriteLine($"\nTotal Size: {FileHelper.FormatFileSize(totalSize)}");

            if (xapkManifest != null)
            {
                Console.WriteLine($"\nXAPK Manifest Info:");
                Console.WriteLine($"  Name: {xapkManifest.Name}");
                if (xapkManifest.ExpansionFiles.Any())
                {
                    Console.WriteLine($"  Expansion Files: {xapkManifest.ExpansionFiles.Count}");
                }
            }

            Console.WriteLine(new string('=', 60));
        }

        public void ValidateXapkStructure(string xapkPath)
        {
            if (!File.Exists(xapkPath))
                throw new FileNotFoundException($"XAPK file not found: {xapkPath}");

            if (!FileHelper.IsXapkFile(xapkPath))
                throw new ArgumentException("File is not a valid XAPK or ZIP file.");

            // Additional validation can be added here
            Console.WriteLine($"✓ XAPK file validation passed: {Path.GetFileName(xapkPath)}");
        }

        public async Task<List<string>> ExtractApkFilesAsync(string xapkPath, string outputDir)
        {
            var apkInfos = await ProcessXapkFileAsync(xapkPath);
            var extractedFiles = new List<string>();

            FileHelper.EnsureDirectoryExists(outputDir);

            var tempExtractDir = FileHelper.CreateTempDirectory("xapk_final_extract");
            try
            {
                FileHelper.ExtractZipFile(xapkPath, tempExtractDir);
                var apkFiles = FileHelper.GetApkFiles(tempExtractDir);

                foreach (var apkFile in apkFiles)
                {
                    var fileName = Path.GetFileName(apkFile);
                    var outputPath = Path.Combine(outputDir, fileName);
                    
                    File.Copy(apkFile, outputPath, overwrite: true);
                    extractedFiles.Add(outputPath);
                    
                    Console.WriteLine($"Extracted: {fileName} → {outputPath}");
                }
            }
            finally
            {
                FileHelper.SafeDeleteDirectory(tempExtractDir);
            }

            return extractedFiles;
        }
    }
}