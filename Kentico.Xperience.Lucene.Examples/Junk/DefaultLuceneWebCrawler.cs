using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CMS.DocumentEngine;
using CMS.Helpers;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kentico.Xperience.Lucene.Services.Implementations
{
    public class DefaultLuceneWebCrawler
    {
        protected HttpClient _httpClient { get; }
        public DefaultLuceneWebCrawler()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> CrawlNode(TreeNode node)
        {
            var url = DocumentURLProvider.GetAbsoluteUrl(node);
            url = url.Replace("http://", "https://");
            return await CrawlPage(url);
        }

        public async Task<string> CrawlPage(string url)
        {
            var response = await _httpClient.GetAsync(url);

            var html = await response.Content.ReadAsStringAsync();

            return ProcessContent(html);
        }

        public virtual string ProcessContent(string htmlContent)
        {
            var parser = new HtmlParser();
            IHtmlDocument doc = parser.ParseDocument(htmlContent);
            IHtmlElement body = doc.Body;

            // Removes script tags
            foreach (var element in body.QuerySelectorAll("script"))
            {
                element.Remove();
            }

            // Removes script tags
            foreach (var element in body.QuerySelectorAll("style"))
            {
                element.Remove();
            }

            // Removes elements marked with the default Xperience exclusion attribute
            foreach (var element in body.QuerySelectorAll($"*[{"data-ktc-search-exclude"}]"))
            {
                element.Remove();
            }

            // Gets the text content of the body element
            string textContent = body.TextContent;

            // Normalizes and trims whitespace characters
            textContent = HTMLHelper.RegexHtmlToTextWhiteSpace.Replace(textContent, " ");
            textContent = textContent.Trim();

            return textContent;

        }
    }
}
