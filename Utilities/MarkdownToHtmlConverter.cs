using Markdig;

namespace markdown_app_aspnet.Utilities
{
    public static class MarkdownToHtmlConverter
    {
        /// <summary>
        /// Converts a Markdown string into a full HTML document.
        /// </summary>
        public static string ConvertToHtml(string markdown, string title = "Converted Markdown")
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            string htmlFragment = Markdown.ToHtml(markdown, pipeline);

            // Wrap in full HTML page
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            max-width: 800px;
            margin: 2rem auto;
            padding: 1rem;
        }}
        pre {{
            background: #f4f4f4;
            padding: 0.75rem;
            border-radius: 4px;
            overflow-x: auto;
        }}
        code {{
            background: #eee;
            padding: 2px 4px;
            border-radius: 3px;
        }}
    </style>
</head>
<body>
    {htmlFragment}
</body>
</html>";
        }

        /// <summary>
        /// Converts a Markdown file into an HTML file and saves it.
        /// </summary>
        public static void ConvertFileToHtml(string markdownFilePath, string outputHtmlPath, string title = "Converted Markdown")
        {
            if (!File.Exists(markdownFilePath))
                throw new FileNotFoundException("Markdown file not found", markdownFilePath);

            string markdown = File.ReadAllText(markdownFilePath);
            string fullHtml = ConvertToHtml(markdown, title);

            File.WriteAllText(outputHtmlPath, fullHtml);
        }
    }
}

