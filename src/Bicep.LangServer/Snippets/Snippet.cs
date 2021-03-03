﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bicep.Core.Parsing;

namespace Bicep.LanguageServer.Snippets
{
    public sealed class Snippet
    {
        private static readonly Regex PlaceholderPattern = new Regex(@"\$({(?<index>\d+):(?<name>\w+)}|(?<index>\d+))", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public Snippet(string text)
        {
            var matches = PlaceholderPattern.Matches(text);

            this.Text = text;
            this.Placeholders = matches
                .Select(CreatePlaceholder)
                .OrderBy(p=>p.Index)
                .ToImmutableArray();
        }

        public string Text { get; }

        // placeholders ordered by index
        public ImmutableArray<SnippetPlaceholder> Placeholders { get; }

        public string Format(Func<Snippet, SnippetPlaceholder, string?> placeholderCallback)
        {
            // we will be performing multiple string replacements
            // better to do it in-place
            var buffer = new StringBuilder(this.Text);

            // to avoid recomputing spans, we will perform the replacements in reverse order by span position
            foreach (var placeholder in this.Placeholders.OrderByDescending(p => p.Span.Position))
            {
                // remove original placeholder
                buffer.Remove(placeholder.Span.Position, placeholder.Span.Length);

                // get the replacement string from the callback
                string? replacement = placeholderCallback(this, placeholder);

                // insert the replacement (if any)
                if (string.IsNullOrEmpty(replacement) == false)
                {
                    buffer.Insert(placeholder.Span.Position, replacement);
                }
            }

            return buffer.ToString();
        }

        public string FormatDocumentation() => Format((snippet, placeholder) => placeholder.Name);

        private static SnippetPlaceholder CreatePlaceholder(Match match)
        {
            var name = match.Groups["name"].Value;
            if (string.IsNullOrEmpty(name))
            {
                name = null;
            }

            return new SnippetPlaceholder(
                index: int.Parse(match.Groups["index"].Value),
                name: name,
                span: new TextSpan(match.Index, match.Length));
        }
    }
}
