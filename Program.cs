using System.Net;
using System.Text.Json;
using System.Web;
using HtmlAgilityPack;

class Program
{

    static async Task Main(string[] args)
    {
        var (Cookie , Language ) = Usage(args);
        var Extensions = await GetCodeExtension(Cookie, Language);
        
        // Defina o numero maximo de paginas de submissoes
        for (int page = 1; page <= 46; page++)
            await SubmissionPage(page, Cookie, Language, Extensions);
    }

    static (string Cookie, string Language) Usage(string[] args)
    {
        var Cookie = GetValueFromArgs(args, "cookie");
        var Language = GetValueFromArgs(args, "lang");

        if (Cookie == null || Language == null)
        {
            throw new ArgumentException("Usage --cookie \"csrfTokenXXXX%2Fcollect\" --lang en");
        }

        return (Cookie: HttpUtility.UrlDecode(Cookie), Language: Language);
    }

    static async Task SubmissionPage(int page, string cookieString, string lang, Dictionary<string, string> extensions)
    {
        string url = $"https://www.beecrowd.com.br/judge/{lang}/runs?answer_id=1&page={page}&sort=created&direction=asc";

        // Usando HttpClient para obter o HTML da página
        using (HttpClient client = new HttpClient())
        {
            try
            {
                client.DefaultRequestHeaders.Add("cookie", cookieString);
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

                // Obtendo o HTML da página
                string html = await client.GetStringAsync(url);

                // Usando o HtmlAgilityPack para analisar o HTML
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var links = htmlDocument.DocumentNode.SelectNodes($"//a[starts-with(@href, '/judge/{lang}/runs/code/')]");
                foreach (var link in links)
                    await GetCode($"https://www.beecrowd.com.br{link.GetAttributeValue("href", "")}", cookieString, lang, extensions);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Erro ao fazer a solicitação HTTP SubmissionPage: {e.Message}");
            }
        }
    }

    static async Task GetCode(string url, string cookieString, string lang, Dictionary<string, string> extensions)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                client.DefaultRequestHeaders.Add("cookie", cookieString);
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

                // Obtendo o HTML da página
                string html = await client.GetStringAsync(url);

                // Usando o HtmlAgilityPack para analisar o HTML
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var code = htmlDocument.DocumentNode.SelectNodes("//pre[@id='code']").FirstOrDefault();
                var codeLanguage = code?.GetAttributeValue("class", null);
                var codeLanguageNumber = codeLanguage != null ? codeLanguage.Replace("code-", "") : "";
                var codeExtension = extensions.ContainsKey(codeLanguageNumber) ? extensions[codeLanguageNumber] : "txt";
                var link = htmlDocument.DocumentNode.SelectNodes($"//a[starts-with(@href, '/judge/{lang}/problems/view/')]");
                var problemNumber = link.FirstOrDefault()?.GetAttributeValue("href", "").Split('/').Last() ?? "error";

                if (code != null)
                {
                    var directoryPath = "codes";
                    // Check if the directory exists, and create it if not
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Specify the file path
                    string filePath = Path.Combine(directoryPath, $"{problemNumber}.{codeExtension}") ;
                    try
                    {
                        // Escrever texto no arquivo
                        using (StreamWriter writer = new StreamWriter(filePath))
                        {
                            writer.Write(HttpUtility.HtmlDecode(code.InnerHtml));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ocorreu um erro ao escrever no arquivo: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Nenhum codigo encontrado na página.");
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Erro ao fazer a solicitação HTTP GetCode: {e.Message}");
            }
        }
    }

    static async Task<Dictionary<string, string>> GetCodeExtension(string cookieString, string lang)
    {
        var url = $"https://www.beecrowd.com.br/judge/{lang}/problems/view/1000";
        var extensions = new Dictionary<string, string>();

        using (HttpClient client = new HttpClient())
        {
            try
            {
                client.DefaultRequestHeaders.Add("cookie", cookieString);
                client.DefaultRequestHeaders.Add("user-agent",
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

                // Obtendo o HTML da página
                string html = await client.GetStringAsync(url);

                // Usando o HtmlAgilityPack para analisar o HTML
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var options = htmlDocument.DocumentNode.SelectNodes("//select[@id='language-id']/option");
                foreach (var option in options)
                {
                    // Parse the JSON string into a JsonDocument
                    var jsonDocument = JsonDocument.Parse( WebUtility.HtmlDecode(option.InnerHtml) ) ;
                    var code = jsonDocument.RootElement.GetProperty("id").GetInt32().ToString();
                    var extension = jsonDocument.RootElement.GetProperty("extension").GetString();
                    if (code != null && extension != null)
                    {
                        extensions.Add(code,extension);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Erro ao fazer a solicitação HTTP: {e.Message}");
            }

            return extensions;
        }
    }
    
    static string? GetValueFromArgs(string[] args, string paramName)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals($"--{paramName}", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
