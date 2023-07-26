﻿using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CMS.Helpers;

namespace Kentico.Xperience.Lucene.Examples.Utils
{
    public class HtmlSanitizer
    {
        public string SanitizeHtmlFragment(string htmlContent) {

            var parser = new HtmlParser();
            INodeList nodes = parser.ParseFragment(htmlContent, null);

            // Removes script tags
            foreach (var element in nodes.QuerySelectorAll("script"))
            {
                element.Remove();
            }

            // Removes script tags
            foreach (var element in nodes.QuerySelectorAll("style"))
            {
                element.Remove();
            }

            // Removes elements marked with the default Xperience exclusion attribute
            foreach (var element in nodes.QuerySelectorAll($"*[{"data-ktc-search-exclude"}]"))
            {
                element.Remove();
            }

            // Gets the text content of the body element
            string textContent = string.Join(" ", nodes.Select(n => n.TextContent));

            // Normalizes and trims whitespace characters
            textContent = HTMLHelper.RegexHtmlToTextWhiteSpace.Replace(textContent, " ");
            textContent = textContent.Trim();

            return textContent;
        }

        public virtual string SanitizeHtmlDocument(string htmlContent)
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
