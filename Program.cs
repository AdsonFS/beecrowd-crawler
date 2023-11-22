using System;
using System.Net.Http;
using System.Web;
using HtmlAgilityPack;

class Program
{
    static async Task Main(string[] args)
    {
        // Defina o numero maximo de paginas de submissoes
        for (int page = 1; page <= 46; page++)
            await SubmissionPage(page);
    }

    static async Task SubmissionPage(int page)
    {
        string url = $"https://www.beecrowd.com.br/judge/pt/runs?answer_id=1&page={page}&sort=created&direction=asc";

        // Usando HttpClient para obter o HTML da página
        using (HttpClient client = new HttpClient())
        {
            try
            {
                client.DefaultRequestHeaders.Add("cookie", "<your_cokie>");
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

                // Obtendo o HTML da página
                string html = await client.GetStringAsync(url);

                // Usando o HtmlAgilityPack para analisar o HTML
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var links = htmlDocument.DocumentNode.SelectNodes("//a[starts-with(@href, '/judge/pt/runs/code/')]");
                foreach (var link in links)
                    await GetCode($"https://www.beecrowd.com.br{link.GetAttributeValue("href", "")}");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Erro ao fazer a solicitação HTTP: {e.Message}");
            }
        }
    }

    static async Task GetCode(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                client.DefaultRequestHeaders.Add("cookie", "<your_cokie>");
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

                // Obtendo o HTML da página
                string html = await client.GetStringAsync(url);

                // Usando o HtmlAgilityPack para analisar o HTML
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var code = htmlDocument.DocumentNode.SelectNodes("//pre[@id='code']").FirstOrDefault();
                var link = htmlDocument.DocumentNode.SelectNodes("//a[starts-with(@href, '/judge/pt/problems/view/')]");
                var problemNumber = link.FirstOrDefault()?.GetAttributeValue("href", "").Split('/').Last() ?? "error";

                if (code != null)
                {
                    string filePath = $"codes/{problemNumber}.cpp";
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
                Console.WriteLine($"Erro ao fazer a solicitação HTTP: {e.Message}");
            }
        }
    }
}
