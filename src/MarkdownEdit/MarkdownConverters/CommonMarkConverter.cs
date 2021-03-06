﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using CommonMark;
using MarkdownEdit.Models;
using MarkdownEdit.Properties;

namespace MarkdownEdit.MarkdownConverters
{
    public class CommonMarkConverter : IMarkdownConverter
    {
        private readonly Func<string, string> _uriResolver = Utility.Memoize<string, string>(UriResolver);

        public string ConvertToHtml(string markdown, bool resolveUrls)
        {
            var settings = CommonMarkSettings.Default.Clone();
            if (resolveUrls) settings.UriResolver = _uriResolver;
            return CommonMark.CommonMarkConverter.Convert(markdown, settings);
        }

        private static string UriResolver(string text)
        {
            if (Regex.IsMatch(text, @"^\w+://")) return text;
            var lastOpen = Settings.Default.LastOpenFile.StripOffsetFromFileName();
            if (string.IsNullOrEmpty(lastOpen)) return text;
            var path = Path.GetDirectoryName(lastOpen);
            if (string.IsNullOrEmpty(path)) return text;
            var file = text.TrimStart('/');
            return FindAsset(path, file) ?? text;
        }

        private static string FindAsset(string path, string file)
        {
            try
            {
                var asset = Path.Combine(path, file);
                for (var i = 0; i < 20; ++i)
                {
                    if (File.Exists(asset)) return "file:///" + asset.Replace('\\', '/');
                    var parent = Directory.GetParent(path);
                    if (parent == null) break;
                    path = parent.FullName;
                    asset = Path.Combine(path, file);
                }
            }
            catch (ArgumentException)
            {
            }
            return null;
        }
    }
}