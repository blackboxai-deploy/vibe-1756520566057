using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ApkAntiSplit.Utils
{
    public static class AndroidSdkHelper
    {
        private static string? _androidSdkPath;
        private static string? _buildToolsPath;

        public static string? AndroidSdkPath
        {
            get
            {
                if (_androidSdkPath == null)
                {
                    _androidSdkPath = FindAndroidSdk();
                }
                return _androidSdkPath;
            }
            set => _androidSdkPath = value;
        }

        public static string? BuildToolsPath
        {
            get
            {
                if (_buildToolsPath == null && AndroidSdkPath != null)
                {
                    _buildToolsPath = FindLatestBuildTools();
                }
                return _buildToolsPath;
            }
            set => _buildToolsPath = value;
        }

        public static bool IsAndroidSdkAvailable()
        {
            return AndroidSdkPath != null && BuildToolsPath != null;
        }

        private static string? FindAndroidSdk()
        {
            // Common Android SDK locations
            var possiblePaths = new List<string>();

            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                possiblePaths.AddRange(new[]
                {
                    Path.Combine(userProfile, "AppData", "Local", "Android", "Sdk"),
                    @"C:\Android\Sdk",
                    @"C:\Program Files\Android\Sdk",
                    @"C:\Program Files (x86)\Android\Sdk"
                });
            }
            // macOS
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                possiblePaths.AddRange(new[]
                {
                    Path.Combine(userHome, "Library", "Android", "sdk"),
                    Path.Combine(userHome, "Android", "Sdk"),
                    "/usr/local/android-sdk"
                });
            }
            // Linux
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                possiblePaths.AddRange(new[]
                {
                    Path.Combine(userHome, "Android", "Sdk"),
                    "/opt/android-sdk",
                    "/usr/local/android-sdk"
                });
            }

            // Check environment variables
            var envPath = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT") ??
                         Environment.GetEnvironmentVariable("ANDROID_HOME");
            if (!string.IsNullOrEmpty(envPath))
            {
                possiblePaths.Insert(0, envPath);
            }

            // Find the first valid SDK path
            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path) && Directory.Exists(Path.Combine(path, "build-tools")))
                {
                    return path;
                }
            }

            return null;
        }

        private static string? FindLatestBuildTools()
        {
            if (AndroidSdkPath == null)
                return null;

            var buildToolsDir = Path.Combine(AndroidSdkPath, "build-tools");
            if (!Directory.Exists(buildToolsDir))
                return null;

            // Get all build tools versions and sort them
            var versions = Directory.GetDirectories(buildToolsDir)
                .Select(Path.GetFileName)
                .Where(v => !string.IsNullOrEmpty(v))
                .OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Find the latest version that has the required tools
            foreach (var version in versions)
            {
                var versionPath = Path.Combine(buildToolsDir, version!);
                if (HasRequiredTools(versionPath))
                {
                    return versionPath;
                }
            }

            return null;
        }

        private static bool HasRequiredTools(string buildToolsPath)
        {
            var requiredTools = new[] { "aapt2", "d8", "apksigner" };
            var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";

            return requiredTools.All(tool =>
                File.Exists(Path.Combine(buildToolsPath, tool + extension)));
        }

        public static string GetToolPath(string toolName)
        {
            if (BuildToolsPath == null)
                throw new InvalidOperationException("Android SDK not found. Please install Android SDK and build tools.");

            var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
            var toolPath = Path.Combine(BuildToolsPath, toolName + extension);

            if (!File.Exists(toolPath))
                throw new FileNotFoundException($"Android SDK tool '{toolName}' not found at {toolPath}");

            return toolPath;
        }

        public static async Task<ProcessResult> RunToolAsync(string toolName, string arguments, string? workingDirectory = null)
        {
            var toolPath = GetToolPath(toolName);
            return await RunProcessAsync(toolPath, arguments, workingDirectory);
        }

        public static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, string? workingDirectory = null)
        {
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            var outputBuilder = new List<string>();
            var errorBuilder = new List<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputBuilder.Add(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    errorBuilder.Add(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder,
                StandardError = errorBuilder
            };
        }

        public static void ValidateAndroidSdk()
        {
            if (!IsAndroidSdkAvailable())
            {
                throw new InvalidOperationException(
                    "Android SDK not found. Please install Android SDK and set ANDROID_SDK_ROOT or ANDROID_HOME environment variable.\n" +
                    "Required tools: aapt2, d8, apksigner\n" +
                    "Download from: https://developer.android.com/studio");
            }

            Console.WriteLine($"Android SDK found: {AndroidSdkPath}");
            Console.WriteLine($"Build tools: {BuildToolsPath}");
        }
    }

    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public List<string> StandardOutput { get; set; } = new();
        public List<string> StandardError { get; set; } = new();

        public bool IsSuccess => ExitCode == 0;

        public string GetOutputText() => string.Join(Environment.NewLine, StandardOutput);
        public string GetErrorText() => string.Join(Environment.NewLine, StandardError);
    }
}