using Markdig;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ELOR.PurpleBlog
{
    internal class Program
    {
        const string INDEX_MD_FILE_NAME = "index.md";
        const string INDEX_HTML_FILE_NAME = "index.html";
        const string POSTS_JSON_FILE_NAME = "posts.json";
        const string DEFAULT_INDEX_TEMPLATE = "<!-- DOCTYPE html --><html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>{{blogname}}</title><meta name=\"description\" content=\"{{blogdesc}}\"><meta name=\"robots\" content=\"index, follow\"><link rel=\"stylesheet\" type=\"text/css\" href=\"style.css\"/></head><body><main><header>{{blogname}}</header><div class=\"postmeta\">{{blogdesc}}</div><content id=\"index\">{{content}}</content><footer>Created by <a target=\"_blank\" href=\"https://github.com/Elorucov/PurpleBlog\">PurpleBlog</a></footer></main></body></html>";
        const string DEFAULT_POST_TEMPLATE = "<!-- DOCTYPE html --><html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>{{title}} | {{blogname}}</title><meta name=\"description\" content=\"{{summary}}\"><meta name=\"robots\" content=\"index, follow\"><link rel=\"stylesheet\" type=\"text/css\" href=\"../style.css\"/></head><body><main><header>{{title}}</header><div class=\"postmeta\">{{published}} • on <a href=\"../\">{{blogname}}</a></div><content>{{content}}</content><footer>Created by <a target=\"_blank\" href=\"https://github.com/Elorucov/PurpleBlog\">PurpleBlog</a></footer></main></body></html>";

        static readonly string[] _folderNamesForIgnore = [".git"];
        static readonly string[] _requiredMetadataProps = ["title", "summary", "published"];
        static readonly string[] _deniedMetadataProps = ["blogname", "blogdesc", "content", "stylesheet"];

        static string _inputPath;
        static string _outputPath;
        static string _blogName;
        static string _blogDescription;
        static string _indexTemplate;
        static string _postTemplate;

        static void Main(string[] args)
        {
            var ver = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split("+")[0];
            Console.WriteLine($"PurpleBlog v{ver} by Elchin Orujov (https://elor.top)");
            Console.WriteLine($"A tool for converting Markdown files to HTML pages with template.");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            Dictionary<string, string> arguments = new Dictionary<string, string>();
            foreach (var arg in args)
            {
                if (!arg.StartsWith("-")) PrintInstructionAndQuit();

                var keyValue = arg.Substring(1).Split('=');
                if (keyValue.Length < 2) PrintInstructionAndQuit();

                arguments.TryAdd(keyValue[0], keyValue[1]);
            }

            if (!arguments.TryGetValue("i", out _inputPath)) PrintInstructionAndQuit();
            Console.WriteLine("The search for index.md files will be performed in \"{0}\"", _inputPath);

            if (!arguments.TryGetValue("o", out _outputPath)) PrintInstructionAndQuit();
            Console.WriteLine("HTML pages will be saved in \"{0}\"", _outputPath);

            if (!arguments.TryGetValue("n", out _blogName)) PrintInstructionAndQuit();
            Console.WriteLine("Blog name is {0}", _blogName);

            if (!arguments.TryGetValue("d", out _blogDescription)) PrintInstructionAndQuit();
            Console.WriteLine("Blog description is: {0}", _blogDescription);

            _indexTemplate = DEFAULT_INDEX_TEMPLATE;
            if (arguments.TryGetValue("it", out string indexTemplatePath))
            {
                _indexTemplate = GetTemplate(indexTemplatePath, DEFAULT_INDEX_TEMPLATE);
                Console.WriteLine("Loaded template for the main index.html page from {0}", indexTemplatePath);
            }

            _postTemplate = DEFAULT_POST_TEMPLATE;
            if (arguments.TryGetValue("pt", out string postTemplatePath))
            {
                _postTemplate = GetTemplate(postTemplatePath, DEFAULT_POST_TEMPLATE);
                Console.WriteLine("Loaded template for the posts page from {0}", postTemplatePath);
            }

            try
            {
                Console.WriteLine();
                DoAsync().Wait();
                Console.WriteLine($"Task completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An unhandled error has occured!");
                Console.WriteLine("0x{0}: {1}", ex.HResult.ToString("x8"), ex.Message);
            }
        }

        private static void PrintInstructionAndQuit()
        {
            Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} -i=/input/path -o=/output/path -n=\"Blog name\" -d=\"Blog description\"");
            Console.WriteLine();
            Console.WriteLine("Required arguments:");
            Console.WriteLine("-i: Path to the folder containing folders with the index.md file.");
            Console.WriteLine("-o: Path to the folder where HTML pages will be saved.");
            Console.WriteLine("-n: Value of this argument will replace the {{blogname}} tag in the template. The value should be the blog name.");
            Console.WriteLine("-d: Value of this argument will replace the {{blogdesc}} tag in the template. The value should be the blog description.");
            Console.WriteLine();
            Console.WriteLine("Optional arguments:");
            Console.WriteLine("-it: Path to the file with the HTML template for the home page (that contains links to posts). The template must be contains these tags: {{blogname}}, {{blogdesc}} and {{content}}");
            Console.WriteLine("-pt: Path to the file with the HTML template for the posts page. The template must be contains these tags: {{blogname}}, {{title}}, {{summary}}, {{published}} and {{content}}");
            Environment.Exit(0x75757575);
        }

        private static string GetTemplate(string path, string defaultTemplate)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot read template file: {0}. Error 0x{1}: {2}", path, ex.HResult.ToString("x8"), ex.Message);
            }
            return defaultTemplate;
        }

        private static async Task DoAsync()
        {
            var indexFiles = await GetIndexFilesPathAsync(_inputPath);
            List<BlogPost> posts = new List<BlogPost>();

            foreach (var mdFilePath in indexFiles)
            {
                try
                {
                    Console.Write("{0}... ", mdFilePath);

                    string mdFileContent = await File.ReadAllTextAsync(mdFilePath);
                    var metadata = ParseAndGetMetadata(mdFileContent, out int metadataLength);
                    metadata.Add("blogname", _blogName);

                    int start = metadataLength + 1;
                    int end = mdFileContent.Length;
                    string content = mdFileContent[start..end];

                    MarkdownParserContext context = new MarkdownParserContext();
                    var pipeline = new MarkdownPipelineBuilder().UseEmphasisExtras().UsePipeTables().Build();
                    var contentHtml = Markdown.ToHtml(content, pipeline);
                    string result = WrapIntoTemplate(contentHtml, _postTemplate, metadata);

                    var mdFilePathSeparated = mdFilePath.Split(Path.DirectorySeparatorChar);
                    string postFutureLinkName = mdFilePathSeparated[mdFilePathSeparated.Length - 2];
                    string postHtmlFolderPath = Path.Combine(_outputPath, postFutureLinkName);
                    Directory.CreateDirectory(postHtmlFolderPath);
                    File.WriteAllText(Path.Combine(postHtmlFolderPath, INDEX_HTML_FILE_NAME), result);

                    if (!metadata.TryGetValue("hidden", out string hiddenProp) || hiddenProp == "false")
                    {
                        DateTime publishDate = DateTime.Parse(metadata["published"]);
                        posts.Add(new BlogPost(postFutureLinkName, metadata["title"], metadata["summary"], publishDate));
                    }

                    Console.WriteLine("OK.");
                }
                catch (ApplicationException aex)
                {
                    Console.WriteLine("FAIL! ({0})", aex.Message);
                }
            }

            // Creating JSON with posts info
            Console.Write("Creating posts JSON file... ");
            using var jsonFileStream = File.CreateText(Path.Combine(_outputPath, POSTS_JSON_FILE_NAME));
            await JsonSerializer.SerializeAsync(jsonFileStream.BaseStream, posts);
            await jsonFileStream.FlushAsync();
            Console.WriteLine("OK.");

            // Creating index file with posts links
            Console.Write("Creating main index.html with links to posts... ");
            string indexFile = MakeIndexHtmlContent(posts, _indexTemplate);
            File.WriteAllText(Path.Combine(_outputPath, INDEX_HTML_FILE_NAME), indexFile);
            Console.WriteLine("OK.");
        }

        private static async Task<List<string>> GetIndexFilesPathAsync(string folderPath)
        {
            var childFolders = Directory.EnumerateDirectories(folderPath);
            List<string> filteredChildFolders = childFolders.Where(p => !_folderNamesForIgnore.Contains(p.Split(Path.DirectorySeparatorChar).Last())).ToList();

            List<string> indexFilesPath = new List<string>(filteredChildFolders.Count);
            foreach (var childFolder in CollectionsMarshal.AsSpan(filteredChildFolders))
            {
                string indexFilePath = Path.Combine(childFolder, INDEX_MD_FILE_NAME);
                if (!File.Exists(indexFilePath)) continue;
                indexFilesPath.Add(indexFilePath);
            }

            return indexFilesPath;
        }

        private static Dictionary<string, string> ParseAndGetMetadata(string content, out int length)
        {
            if (string.IsNullOrWhiteSpace(content)) throw new ApplicationException("File is empty.");

            Dictionary<string, string> properties = new Dictionary<string, string>();
            length = 0;
            string[] openingCharacters = { "---", "---\r" };

            var contentSpan = content.AsSpan();
            var split = contentSpan.Split("\n");

            bool parsingInProcess = false;
            long rowsCount = 0;
            foreach (var chunk in split)
            {
                var row = content[chunk.Start..chunk.End];

                if (rowsCount == 0)
                {
                    if (!openingCharacters.Contains(row)) throw new ApplicationException("Metadata section's opening characters not found at start of file.");
                    parsingInProcess = true;
                    rowsCount++;
                    continue;
                }

                if (openingCharacters.Contains(row))
                {
                    parsingInProcess = false;
                    rowsCount++;
                    length = chunk.End.Value;
                    break;
                }

                (string property, string value) = GetMetadataProperty(row);

                if (_deniedMetadataProps.Contains(property)) throw new ApplicationException($"File does not have \"{property}\" property in metadata. It's for internal use only.");

                properties.Add(property, value);
                rowsCount++;
            }

            if (parsingInProcess) throw new ApplicationException("Unexpected end of metadata section.");
            if (properties.Count == 0) throw new ApplicationException("File does not contains metadata.");

            var missingPropertirs = _requiredMetadataProps.Except(properties.Keys);
            if (missingPropertirs.Count() > 0) throw new ApplicationException($"File does not contains required metadata properties: {string.Join(", ", missingPropertirs)}");

            return properties;
        }

        private static (string property, string value) GetMetadataProperty(string row)
        {
            var span = row.AsSpan().Split(':');
            span.MoveNext();

            string property = row[span.Current.Start..span.Current.End].Trim();
            if (!span.MoveNext()) throw new ApplicationException($"Invalid row in metadata: \"{row}\"");

            string value = row[span.Current.Start..span.Current.End].Trim();
            return (property, value);
        }

        private static string WrapIntoTemplate(string htmlContent, string template, Dictionary<string, string> metadata)
        {
            metadata.TryAdd("content", htmlContent);

            var formattedMetadataKeys = metadata.Keys.Select(m => "{{" + m + "}}").ToList();

            string result = Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
            {
                var key = match.Groups[1].Value;
                metadata.TryGetValue(key, out var value);

                if (key == "published")
                {
                    if (!DateTime.TryParse(value, out var publishDate)) throw new ApplicationException("The value of \"published\" metadata property is incorrect.");
                    value = publishDate.ToString("M") + " " + publishDate.Year;
                }

                return value ?? match.Value;
            });

            result = Regex.Replace(result, "<table>", "<table cellpadding=\"0\" cellspacing=\"0\">");
            return result;
        }

        private static string MakeIndexHtmlContent(List<BlogPost> posts, string template)
        {
            StringBuilder sb = new StringBuilder();
            var postsByYear = posts.OrderByDescending(p => p.PublishDate).GroupBy(p => p.PublishDate.Year);

            foreach (var yearPosts in postsByYear)
            {
                sb.Append($"<h2>{yearPosts.Key}</h2>");
                foreach (var post in yearPosts)
                {
                    sb.Append(string.Format("<p><a href=\"{0}\">{1}</a> <span>{2}</span><div class=\"summary\">{3}</div></p>", post.RelativeUrl, post.Title, post.PublishDate.ToString("M/d"), post.Summary));
                }
            }

            string htmlContent = sb.ToString();

            Dictionary<string, string> metadata = new Dictionary<string, string>();
            metadata.Add("blogname", _blogName);
            metadata.Add("blogdesc", _blogDescription);
            return WrapIntoTemplate(htmlContent, template, metadata);
        }
    }
}