using System.CommandLine;
using ApkAntiSplit.Core;
using ApkAntiSplit.Utils;

namespace ApkAntiSplit
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = CreateRootCommand();
            return await rootCommand.InvokeAsync(args);
        }

        static RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand("APK Anti-Split Tool - Merge split APK files from XAPK archives into a single APK");

            // Input file argument
            var inputArgument = new Argument<FileInfo>(
                name: "input",
                description: "Input XAPK or ZIP file containing split APKs");

            // Output option
            var outputOption = new Option<FileInfo?>(
                name: "--output",
                description: "Output path for the merged APK file")
            {
                ArgumentHelpName = "output.apk"
            };
            outputOption.AddAlias("-o");

            // Keystore option
            var keystoreOption = new Option<FileInfo?>(
                name: "--keystore",
                description: "Keystore file for APK signing (optional)")
            {
                ArgumentHelpName = "keystore.jks"
            };
            keystoreOption.AddAlias("-k");

            // Verbose option
            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Enable verbose logging")
            {
                ArgumentHelpName = "verbose"
            };
            verboseOption.AddAlias("-v");

            // Info option
            var infoOption = new Option<bool>(
                name: "--info",
                description: "Display APK information without merging")
            {
                ArgumentHelpName = "info"
            };
            infoOption.AddAlias("-i");

            // Extract option
            var extractOption = new Option<DirectoryInfo?>(
                name: "--extract",
                description: "Extract APK files to directory without merging")
            {
                ArgumentHelpName = "directory"
            };
            extractOption.AddAlias("-e");

            // Android SDK path option
            var sdkPathOption = new Option<DirectoryInfo?>(
                name: "--android-sdk",
                description: "Android SDK path (if not in standard location)")
            {
                ArgumentHelpName = "sdk-path"
            };

            rootCommand.AddArgument(inputArgument);
            rootCommand.AddOption(outputOption);
            rootCommand.AddOption(keystoreOption);
            rootCommand.AddOption(verboseOption);
            rootCommand.AddOption(infoOption);
            rootCommand.AddOption(extractOption);
            rootCommand.AddOption(sdkPathOption);

            rootCommand.SetHandler(async (inputFile, outputFile, keystoreFile, verbose, info, extractDir, sdkPath) =>
            {
                try
                {
                    await ExecuteCommand(inputFile, outputFile, keystoreFile, verbose, info, extractDir, sdkPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    if (verbose)
                    {
                        Console.WriteLine($"Stack trace: {ex}");
                    }
                    Environment.Exit(1);
                }
            }, inputArgument, outputOption, keystoreOption, verboseOption, infoOption, extractOption, sdkPathOption);

            return rootCommand;
        }

        static async Task ExecuteCommand(
            FileInfo inputFile,
            FileInfo? outputFile,
            FileInfo? keystoreFile,
            bool verbose,
            bool info,
            DirectoryInfo? extractDir,
            DirectoryInfo? sdkPath)
        {
            // Display banner
            DisplayBanner();

            // Validate input file
            if (!inputFile.Exists)
            {
                throw new FileNotFoundException($"Input file not found: {inputFile.FullName}");
            }

            // Set Android SDK path if provided
            if (sdkPath?.Exists == true)
            {
                AndroidSdkHelper.AndroidSdkPath = sdkPath.FullName;
            }

            // Check Android SDK availability for merge operations
            if (!info && extractDir == null)
            {
                try
                {
                    AndroidSdkHelper.ValidateAndroidSdk();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Android SDK Error: {ex.Message}");
                    Console.WriteLine("\nFor info-only mode, use: --info");
                    Console.WriteLine("For extraction-only mode, use: --extract <directory>");
                    Environment.Exit(1);
                }
            }

            var processor = new XapkProcessor();

            try
            {
                // Process XAPK file
                var apkInfos = await processor.ProcessXapkFileAsync(inputFile.FullName);

                // Info mode - just display information
                if (info)
                {
                    DisplayDetailedInfo(apkInfos, verbose);
                    return;
                }

                // Extract mode - extract APKs to directory
                if (extractDir != null)
                {
                    if (!extractDir.Exists)
                    {
                        extractDir.Create();
                    }

                    var extractedFiles = await processor.ExtractApkFilesAsync(inputFile.FullName, extractDir.FullName);
                    
                    Console.WriteLine($"\nExtracted {extractedFiles.Count} APK file(s) to: {extractDir.FullName}");
                    foreach (var file in extractedFiles)
                    {
                        Console.WriteLine($"  {Path.GetFileName(file)}");
                    }
                    return;
                }

                // Merge mode - merge APKs into single APK
                var outputPath = outputFile?.FullName ?? GenerateOutputPath(inputFile.FullName);
                var keystorePath = keystoreFile?.FullName;

                var merger = new ApkMerger();
                var mergedApkPath = await merger.MergeApksAsync(apkInfos, outputPath, keystorePath);

                Console.WriteLine($"\n✅ Success! Merged APK created at: {mergedApkPath}");
                Console.WriteLine("\n📱 Installation Instructions:");
                Console.WriteLine($"1. Transfer the merged APK to your Android device");
                Console.WriteLine($"2. Enable 'Install from unknown sources' in device settings");
                Console.WriteLine($"3. Install the APK: {Path.GetFileName(mergedApkPath)}");
                
                if (string.IsNullOrEmpty(keystorePath))
                {
                    Console.WriteLine("\n⚠️  Note: APK was signed with debug certificate.");
                    Console.WriteLine("   For production use, provide a proper keystore with --keystore option.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to process XAPK file: {ex.Message}", ex);
            }
        }

        static void DisplayBanner()
        {
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    APK Anti-Split Tool                      ║");
            Console.WriteLine("║              Merge Split APKs into Single APK               ║");
            Console.WriteLine("║                         v1.0.0                              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
        }

        static void DisplayDetailedInfo(List<Models.ApkInfo> apkInfos, bool verbose)
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("DETAILED APK INFORMATION");
            Console.WriteLine(new string('=', 80));

            var baseApk = apkInfos.FirstOrDefault(a => a.Type == Models.ApkType.Base);
            if (baseApk != null)
            {
                Console.WriteLine($"📦 Package: {baseApk.PackageName}");
                Console.WriteLine($"🏷️  Version: {baseApk.VersionName} (Code: {baseApk.VersionCode})");
                if (!string.IsNullOrEmpty(baseApk.MinSdkVersion))
                    Console.WriteLine($"📱 Min SDK: API {baseApk.MinSdkVersion}");
                if (!string.IsNullOrEmpty(baseApk.TargetSdkVersion))
                    Console.WriteLine($"🎯 Target SDK: API {baseApk.TargetSdkVersion}");
            }

            Console.WriteLine($"\n📁 APK Components ({apkInfos.Count} files):");
            Console.WriteLine(new string('-', 80));

            foreach (var apk in apkInfos)
            {
                var fileName = Path.GetFileName(apk.FilePath);
                var typeStr = apk.Type switch
                {
                    Models.ApkType.Base => "📦 BASE",
                    Models.ApkType.Split => "🔧 SPLIT",
                    Models.ApkType.Config => "⚙️  CONFIG",
                    _ => "❓ UNKNOWN"
                };

                Console.WriteLine($"{typeStr,-12} {fileName,-35} {FileHelper.FormatFileSize(apk.FileSize),-10}");
                
                if (!string.IsNullOrEmpty(apk.SplitName))
                {
                    Console.WriteLine($"             └─ Split Name: {apk.SplitName}");
                }

                if (apk.Architectures.Any())
                {
                    Console.WriteLine($"             └─ Architectures: {string.Join(", ", apk.Architectures)}");
                }

                if (verbose && apk.Permissions.Any())
                {
                    Console.WriteLine($"             └─ Permissions: {apk.Permissions.Count} declared");
                    foreach (var permission in apk.Permissions.Take(3))
                    {
                        Console.WriteLine($"                • {permission}");
                    }
                    if (apk.Permissions.Count > 3)
                    {
                        Console.WriteLine($"                ... and {apk.Permissions.Count - 3} more");
                    }
                }
            }

            var totalSize = apkInfos.Sum(a => a.FileSize);
            Console.WriteLine($"\n📊 Total Size: {FileHelper.FormatFileSize(totalSize)}");

            // Architecture summary
            var allArchs = apkInfos.SelectMany(a => a.Architectures).Distinct().ToList();
            if (allArchs.Any())
            {
                Console.WriteLine($"🏗️  Supported Architectures: {string.Join(", ", allArchs)}");
            }

            Console.WriteLine(new string('=', 80));
        }

        static string GenerateOutputPath(string inputPath)
        {
            var directory = Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            return Path.Combine(directory, $"{fileNameWithoutExt}_merged.apk");
        }
    }
}