namespace markdown_app_aspnet.Utilities
{
    public class FileRegistry
    {
        private readonly string _registryPath;

        public FileRegistry(string registryPath)
        {
            _registryPath = registryPath;
            Directory.CreateDirectory(Path.GetDirectoryName(_registryPath)!);

            // Create registry file if it doesn't exist
            if (!File.Exists(_registryPath))
            {
                File.WriteAllText(_registryPath, "[]");
            }
        }

        public async Task RegisterFileAsync(string originalName, string storedName, string cleanedName)
        {
            var registryJson = await File.ReadAllTextAsync(_registryPath);
            var entries = System.Text.Json.JsonSerializer.Deserialize<List<FileEntry>>(registryJson) ?? new();

            entries.Add(new FileEntry
            {
                OriginalFile = originalName,
                StoredFile = storedName,
                CleanedFile = cleanedName,
                UploadedAt = DateTime.UtcNow
            });

            var updatedJson = System.Text.Json.JsonSerializer.Serialize(entries, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_registryPath, updatedJson);
        }

        public record FileEntry
        {
            public string OriginalFile { get; init; } = "";
            public string StoredFile { get; init; } = "";
            public string CleanedFile { get; init; } = "";
            public DateTime UploadedAt { get; init; }
        }
    }
}

