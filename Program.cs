using System.Net;
using System.Text.Json;
using System.Web;
using HtmlAgilityPack;

class Program
{
    private const string UserAgent =
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36";

    private static async Task Main(string[] args)
    {
        var (cookie, language) = Usage(args);
        var extensions = await GetCodeExtension(cookie, language);

        // Repetir enquando houver próxima página
        var page = 1;
        do
        {
            Console.WriteLine($"Page {page}");
        } while (await SubmissionPage(page++, cookie, language, extensions));
    }

    static (string Cookie, string Language) Usage(string[] args)
    {
        var cookie = GetValueFromArgs(args, "cookie");
        var language = GetValueFromArgs(args, "lang");

        if (cookie == null || language == null)
        {
            var invalidArgumentsMessage = @"Invalid arguments.
Usage:
dotnet run --lang en --cookie ""csrfTokenXXXX%2Fcollect""";
            Console.WriteLine(invalidArgumentsMessage);
            // Exit with code 1 indicating invalid arguments
            Environment.Exit(1);
        }

        return (Cookie: HttpUtility.UrlDecode(cookie), language);
    }

    static async Task<bool> SubmissionPage(int page, string cookie, string lang, Dictionary<string, string> extensions)
    {
        var hasNextPage = false;
        var url =
            $"https://www.beecrowd.com.br/judge/{lang}/runs?answer_id=1&page={page}&sort=created&direction=asc";

        // Usando HttpClient para obter o HTML da página
        using var client = new HttpClient();
        try
        {
            client.DefaultRequestHeaders.Add("cookie", cookie);
            client.DefaultRequestHeaders.Add("user-agent", UserAgent);

            // Obtendo o HTML da página
            var html = await client.GetStringAsync(url);

            // Usando o HtmlAgilityPack para analisar o HTML
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            hasNextPage = htmlDocument.DocumentNode.SelectNodes("//li[@class='next disabled']").Count == 0;

            var links = htmlDocument.DocumentNode.SelectNodes(
                $"//a[starts-with(@href, '/judge/{lang}/runs/code/')]");
            // Filter out duplicates based on OuterHtml using LINQ
            var distinctLinkHref = links?
                .Where(linkNode => linkNode != null)
                .Select(linkNode => linkNode.GetAttributeValue("href", ""))
                .Distinct()
                .ToList() ?? []; // Provide an empty list if null;

            Console.WriteLine($"Requesting {distinctLinkHref.Count} problems");
            foreach (var href in distinctLinkHref)
                await GetCode($"https://www.beecrowd.com.br{href}", cookie, lang,
                    extensions);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Erro ao fazer a solicitação HTTP SubmissionPage: {e.Message}");
        }

        return hasNextPage;
    }

    static async Task GetCode(string url, string cookie, string lang, Dictionary<string, string> extensions)
    {
        using var client = new HttpClient();
        try
        {
            client.DefaultRequestHeaders.Add("cookie", cookie);
            client.DefaultRequestHeaders.Add("user-agent",
                UserAgent);

            // Obtendo o HTML da página
            var html = await client.GetStringAsync(url);

            // Usando o HtmlAgilityPack para analisar o HTML
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var code = htmlDocument.DocumentNode.SelectNodes("//pre[@id='code']").FirstOrDefault();
            var codeLanguage = code?.GetAttributeValue("class", null);
            var codeLanguageKey = codeLanguage != null ? codeLanguage.Replace("code-", "") : "";
            var codeExtension = extensions.GetValueOrDefault(codeLanguageKey, "txt");
            var link = htmlDocument.DocumentNode.SelectNodes(
                $"//a[starts-with(@href, '/judge/{lang}/problems/view/')]");
            var problemNumber = link.FirstOrDefault()?.GetAttributeValue("href", "").Split('/').Last() ?? "error";
            Console.WriteLine($"Downloading {problemNumber}.{codeExtension}");

            if (code != null)
            {
                const string directoryPath = "codes";
                // Check if the directory exists, and create it if not
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Specify the file path
                var filePath = Path.Combine(directoryPath, $"{problemNumber}.{codeExtension}");
                try
                {
                    // Escrever texto no arquivo
                    await using var writer = new StreamWriter(filePath);
                    await writer.WriteAsync(HttpUtility.HtmlDecode(code.InnerHtml));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ocorreu um erro ao escrever no arquivo: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Nenhum código encontrado na página.");
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Erro ao fazer a solicitação HTTP GetCode: {e.Message}");
        }
    }

    static async Task<Dictionary<string, string>> GetCodeExtension(string cookie, string lang)
    {
        var url = $"https://www.beecrowd.com.br/judge/{lang}/problems/view/1000";
        var extensions = new Dictionary<string, string>();

        using var client = new HttpClient();
        try
        {
            client.DefaultRequestHeaders.Add("cookie", cookie);
            client.DefaultRequestHeaders.Add("user-agent", UserAgent);

            // Obtendo o HTML da página
            var html = await client.GetStringAsync(url);

            // Usando o HtmlAgilityPack para analisar o HTML
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var options = htmlDocument.DocumentNode.SelectNodes("//select[@id='language-id']/option");
            foreach (var option in options)
            {
                // Parse the JSON string into a JsonDocument
                var jsonDocument = JsonDocument.Parse(WebUtility.HtmlDecode(option.InnerHtml));
                var code = jsonDocument.RootElement.GetProperty("id").GetInt32().ToString();
                var extension = jsonDocument.RootElement.GetProperty("extension").GetString();
                if (extension != null)
                {
                    extensions.Add(code, extension);
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Erro ao fazer a solicitação HTTP: {e.Message}");
        }

        return extensions;
    }

    static string? GetValueFromArgs(string[] args, string paramName)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals($"--{paramName}", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}