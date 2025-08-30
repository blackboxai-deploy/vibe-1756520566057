using System.Xml.Linq;

namespace ApkAntiSplit.Models
{
    public class ApkInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string VersionName { get; set; } = string.Empty;
        public int VersionCode { get; set; }
        public string MinSdkVersion { get; set; } = string.Empty;
        public string TargetSdkVersion { get; set; } = string.Empty;
        public ApkType Type { get; set; }
        public string SplitName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public List<string> Architectures { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
        public List<string> Features { get; set; } = new List<string>();
    }

    public enum ApkType
    {
        Base,
        Split,
        Config
    }

    public class XapkManifest
    {
        public string PackageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string VersionName { get; set; } = string.Empty;
        public int VersionCode { get; set; }
        public List<XapkSplit> SplitApks { get; set; } = new List<XapkSplit>();
        public List<XapkExpansion> ExpansionFiles { get; set; } = new List<XapkExpansion>();
    }

    public class XapkSplit
    {
        public string File { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }

    public class XapkExpansion
    {
        public string File { get; set; } = string.Empty;
        public bool InstallLocation { get; set; }
    }

    public class AndroidManifestInfo
    {
        public string PackageName { get; set; } = string.Empty;
        public string VersionName { get; set; } = string.Empty;
        public int VersionCode { get; set; }
        public string MinSdkVersion { get; set; } = string.Empty;
        public string TargetSdkVersion { get; set; } = string.Empty;
        public string CompileSdkVersion { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
        public List<string> Features { get; set; } = new List<string>();
        public List<ActivityInfo> Activities { get; set; } = new List<ActivityInfo>();
        public List<ServiceInfo> Services { get; set; } = new List<ServiceInfo>();
        public List<ReceiverInfo> Receivers { get; set; } = new List<ReceiverInfo>();
        public ApplicationInfo Application { get; set; } = new ApplicationInfo();
    }

    public class ActivityInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public List<string> IntentFilters { get; set; } = new List<string>();
        public bool Exported { get; set; }
    }

    public class ServiceInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool Exported { get; set; }
    }

    public class ReceiverInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool Exported { get; set; }
        public List<string> IntentFilters { get; set; } = new List<string>();
    }

    public class ApplicationInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        public bool Debuggable { get; set; }
        public bool AllowBackup { get; set; } = true;
    }
}