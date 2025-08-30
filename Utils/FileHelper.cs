using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace ApkAntiSplit.Utils
{
    public static class FileHelper
    {
        public static bool IsXapkFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".xapk" || extension == ".zip";
        }

        public static bool IsApkFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".apk";
        }

        public static string CreateTempDirectory(string prefix = "apk_antisplit")
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        public static void SafeDeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;

            try
            {
                // Remove read-only attributes
                RemoveReadOnlyAttributes(path);
                Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete temporary directory {path}: {ex.Message}");
            }
        }

        private static void RemoveReadOnlyAttributes(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            
            foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    file.Attributes &= ~FileAttributes.ReadOnly;
                }
            }
            
            foreach (var dir in directoryInfo.GetDirectories("*", SearchOption.AllDirectories))
            {
                if (dir.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    dir.Attributes &= ~FileAttributes.ReadOnly;
                }
            }
        }

        public static long GetFileSize(string filePath)
        {
            if (!File.Exists(filePath))
                return 0;
            return new FileInfo(filePath).Length;
        }

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public static string CalculateMD5(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static void ExtractZipFile(string zipPath, string extractPath)
        {
            using var archive = ZipFile.OpenRead(zipPath);
            
            foreach (var entry in archive.Entries)
            {
                // Skip directories
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                var destinationPath = Path.Combine(extractPath, entry.FullName);
                var destinationDir = Path.GetDirectoryName(destinationPath);
                
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }

        public static bool ValidateApkFile(string apkPath)
        {
            if (!File.Exists(apkPath))
                return false;

            try
            {
                using var archive = ZipFile.OpenRead(apkPath);
                
                // Check for required APK files
                var hasManifest = archive.Entries.Any(e => e.FullName == "AndroidManifest.xml");
                var hasClasses = archive.Entries.Any(e => e.FullName.StartsWith("classes") && e.FullName.EndsWith(".dex"));
                
                return hasManifest && hasClasses;
            }
            catch
            {
                return false;
            }
        }

        public static List<string> GetApkFiles(string directory)
        {
            if (!Directory.Exists(directory))
                return new List<string>();

            return Directory.GetFiles(directory, "*.apk", SearchOption.AllDirectories)
                .Where(ValidateApkFile)
                .ToList();
        }

        public static void CopyDirectory(string sourceDir, string destDir, bool recursive = true)
        {
            var dir = new DirectoryInfo(sourceDir);
            
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, overwrite: true);
            }

            if (recursive)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder();
            
            foreach (var c in fileName)
            {
                if (!invalidChars.Contains(c))
                {
                    sanitized.Append(c);
                }
                else
                {
                    sanitized.Append('_');
                }
            }
            
            return sanitized.ToString();
        }
    }
}