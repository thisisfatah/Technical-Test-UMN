using System.IO;
using System.Text.RegularExpressions;

namespace Aquascape
{
    public enum RuntimeImportKind
    {
        Fish,
        Trash
    }

    public readonly struct RuntimeImportDescriptor
    {
        public RuntimeImportDescriptor(RuntimeImportKind kind, string typeId, string timestamp, string filePath, string fileName)
        {
            Kind = kind;
            TypeId = typeId;
            Timestamp = timestamp;
            FilePath = filePath;
            FileName = fileName;
        }

        public RuntimeImportKind Kind { get; }
        public string TypeId { get; }
        public string Timestamp { get; }
        public string FilePath { get; }
        public string FileName { get; }
    }

    public static class RuntimeImportParser
    {
        private static readonly Regex Pattern = new(
            "^(FISH|TRASH)_([A-Z0-9]+)_(\\d{14})\\.png$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryParse(string filePath, out RuntimeImportDescriptor descriptor, out string failureReason)
        {
            var fileName = Path.GetFileName(filePath);
            var match = Pattern.Match(fileName);
            if (!match.Success)
            {
                descriptor = default;
                failureReason = $"Invalid naming format: {fileName}";
                return false;
            }

            var kind = string.Equals(match.Groups[1].Value, "FISH", System.StringComparison.OrdinalIgnoreCase)
                ? RuntimeImportKind.Fish
                : RuntimeImportKind.Trash;

            descriptor = new RuntimeImportDescriptor(
                kind,
                match.Groups[2].Value.ToUpperInvariant(),
                match.Groups[3].Value,
                filePath,
                fileName);

            failureReason = string.Empty;
            return true;
        }
    }
}
