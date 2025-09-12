using System.Collections.Concurrent;

namespace markdown_app_aspnet.Utilities
{
    public class FileRegistry
    {
        private readonly ConcurrentDictionary<string, string> _files = new();

        public void AddFile(string id, string path) => _files[id] = path;

        public string? GetFile(string id) =>
            _files.TryGetValue(id, out var path) ? path : null;
    }
}
