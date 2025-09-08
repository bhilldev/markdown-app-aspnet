using System.Text.RegularExpressions;
using Markdig;

namespace markdown_app_aspnet.Utilities
{
    public static class MarkdownCleaner
    {
        public static string CleanMarkdown(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

            // Remove fenced code blocks ```code```
            markdown = Regex.Replace(markdown, @"```[\s\S]*?```", "", RegexOptions.Multiline);

            // Remove inline code `code`
            markdown = Regex.Replace(markdown, @"`[^`]*`", "");

            // Convert remaining Markdown to plain text
            var pipeline = new MarkdownPipelineBuilder().Build();
            string plainText = Markdown.ToPlainText(markdown, pipeline);

            return plainText;
        }

        public static string CleanMarkdownFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Markdown file not found", filePath);

            string markdown = File.ReadAllText(filePath);
            return CleanMarkdown(markdown);
        }
    }
}

