using System.Text.Json;

namespace markdown_app_aspnet.Utilities
{
    public class FileRegistry
    {
        private readonly string _registryPath;

        public FileRegistry(string registryPath)
        {
            _registryPath = registryPath;

            // Ensure directory exists
            var dir = Path.GetDirectoryName(_registryPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // Initialize registry file if missing
            if (!File.Exists(_registryPath))
                File.WriteAllText(_registryPath, "[]");
        }

        /// <summary>
        /// Register a new uploaded file
        /// </summary>
        public async Task RegisterFileAsync(string originalName, string storedName, string cleanedName)
        {
            var registryJson = await File.ReadAllTextAsync(_registryPath);
            var entries = JsonSerializer.Deserialize<List<FileEntry>>(registryJson) ?? new();

            entries.Add(new FileEntry
            {
                OriginalFile = originalName,
                StoredFile = storedName,
                CleanedFile = cleanedName,
                UploadedAt = DateTime.UtcNow
            });

            var updatedJson = JsonSerializer.Serialize(entries, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_registryPath, updatedJson);
        }

        /// <summary>
        /// Get all registered files
        /// </summary>
        public async Task<List<FileEntry>> GetAllFilesAsync()
        {
            var json = await File.ReadAllTextAsync(_registryPath);
            return JsonSerializer.Deserialize<List<FileEntry>>(json) ?? new List<FileEntry>();
        }

        /// <summary>
        /// Find a file entry by original filename
        /// </summary>
        public async Task<FileEntry?> GetFileByOriginalNameAsync(string originalName)
        {
            var files = await GetAllFilesAsync();
            return files.FirstOrDefault(f => f.OriginalFile.Equals(originalName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Find a file entry by stored filename
        /// </summary>
        public async Task<FileEntry?> GetFileByStoredNameAsync(string storedName)
        {
            var files = await GetAllFilesAsync();
            return files.FirstOrDefault(f => f.StoredFile.Equals(storedName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Represents a single file entry
        /// </summary>
        public record FileEntry
        {
            public string OriginalFile { get; init; } = "";
            public string StoredFile { get; init; } = "";
            public string CleanedFile { get; init; } = "";
            public DateTime UploadedAt { get; init; }
        }
    }
}

