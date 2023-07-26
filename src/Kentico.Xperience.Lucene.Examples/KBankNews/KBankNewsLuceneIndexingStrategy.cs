﻿using CMS.Core;
using CMS.DocumentEngine;
using Kentico.Xperience.Lucene.Examples.KBankNews;
using Kentico.Xperience.Lucene.Examples.Utils;

namespace Kentico.Xperience.Lucene.Services.Implementations
{
    public class KBankNewsLuceneIndexingStrategy : DefaultLuceneIndexingStrategy
    {
        private static readonly string[] ContentFields = new string[] {
            nameof(KBankNewsSearchModel.Title),
            nameof(KBankNewsSearchModel.Summary),
            nameof(KBankNewsSearchModel.NewsText),
        };
        public override Task<object> OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
        {
            var result = foundValue;
            if (propertyName == nameof(KBankNewsSearchModel.AllContent))
            {
                var htmlSanitizer = Service.Resolve<HtmlSanitizer>();
                result = string.Join(" ", ContentFields
                    .Select(f => node.GetStringValue(f, ""))
                    .Select(s => htmlSanitizer.SanitizeHtmlFragment(s))
                    );
            }
            return Task.FromResult(result);
        }
    }
}
