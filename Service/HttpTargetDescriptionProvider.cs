using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using kPassKeep.Model;
using kPassKeep.Util;

namespace kPassKeep.Service
{

    public class TargetDescription
    {
        public Target Target { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IEnumerable<BitmapImage> Icons { get; set; }
    }

    public class HttpTargetDescriptionProvider
    {

        public static async Task<TargetDescription> GetDescription(Target target)
        {
            var uriString = NormalizeUri(target.Uri);
            if (String.IsNullOrEmpty(uriString))
            {
                return null;
            }
            var document = await LoadDocument(uriString);
            if (document == null)
            {
                return null;
            }
            var title = ExtractTitle(document);
            var description = ExtractDescription(document);
            var icons = await ExtractIcons(document);

            Serilog.Log.Verbose("Extracted {Uri} target title: {Title}, description: {Description}", uriString, title, description);

            return new TargetDescription
            {
                Target = target,
                Title = title,
                Description = description,
                Icons = icons
            };
        }

        private static async Task<IDocument> LoadDocument(string uriString)
        {
            var config = Configuration.Default.WithDefaultLoader();
            try
            {
                var doc = await BrowsingContext.New(config).OpenAsync(uriString);
                Serilog.Log.Verbose("Document {PassedUri} resolved to {ResolvedUri} loaded: {Doc:l}", uriString, doc.DocumentUri, doc.DocumentElement.OuterHtml);
                return doc;
            }
            catch (Exception ex)
            {
                Serilog.Log.Verbose(ex, "Document {PassedUri} resolved to {ResolvedUri} failed to load");
                return null;
            }
        }

        private static string NormalizeUri(string uriString)
        {
            if (!uriString.StartsWith("http://") && !uriString.StartsWith("https://"))
            {
                return "http://" + uriString;
            }
            return uriString;
        }

        private static string ExtractTitle(IDocument doc)
        {
            if (!String.IsNullOrWhiteSpace(doc.Title))
            {
                return doc.Title;
            }
            var meta = doc.QuerySelectorAll("meta");
            var titles = meta
                .Where(e => e.HasAttribute("name") && e.HasAttribute("content"))
                .Where(e => "application-name".Equals(e.Attributes["name"].Value))
                .Select(e => e.Attributes["content"].Value)
                .Concat
                (
                    meta
                    .Where(e => e.HasAttribute("property") && e.HasAttribute("content"))
                    .Where(e => "og:title".Equals(e.Attributes["property"].Value)
                             || "twitter:title".Equals(e.Attributes["property"].Value))
                    .Select(e => e.Attributes["content"].Value)
                );
            return titles.FirstOrDefault();
        }

        private static string ExtractDescription(IDocument doc)
        {
            var meta = doc.QuerySelectorAll("meta");
            var description = meta
                .Where(e => e.HasAttribute("name") && e.HasAttribute("content"))
                .Where(e => "description".Equals(e.Attributes["name"].Value))
                .Select(e => e.Attributes["content"].Value)
                .Concat
                (
                    meta
                    .Where(e => e.HasAttribute("property") && e.HasAttribute("content"))
                    .Where(e => "og:description".Equals(e.Attributes["property"].Value)
                             || "twitter:description".Equals(e.Attributes["property"].Value))
                    .Select(e => e.Attributes["content"].Value)
                );
            return description.FirstOrDefault();
        }

        private static async Task<IEnumerable<BitmapImage>> ExtractIcons(IDocument doc)
        {
            var links = doc.QuerySelectorAll("link");
            var icons = links
                .Where(e => e.HasAttribute("rel") && e.HasAttribute("href"))
                .Where(e => "shortcut icon".Equals(e.Attributes["rel"].Value)
                         || "icon".Equals(e.Attributes["rel"].Value)
                         || "apple-touch-icon".Equals(e.Attributes["rel"].Value))
                .Select(e => e.Attributes["href"].Value)
                .Concat
                (
                    doc.QuerySelectorAll("meta")
                    .Where(e => e.HasAttribute("property") && e.HasAttribute("content"))
                    .Where(e => "og:image".Equals(e.Attributes["property"].Value)
                             || "twitter:image".Equals(e.Attributes["property"].Value))
                    .Select(e => e.Attributes["content"].Value)
                );
            var tasks = "/favicon.ico".Yield()
                .Concat(icons)
                .Select(async e =>
                {
                    using (var client = new WebClient())
                    {
                        try
                        {
                            return await client.DownloadDataTaskAsync(new Uri(new Uri(doc.BaseUri), e));
                        } catch (Exception)
                        {
                            return null;
                        }
                    }
                })
                .ToArray();
            var images = await new TaskFactory().ContinueWhenAll(tasks, t => t);
            return images
                .Select(e => e.Result)
                .Where(e => e != null)
                .Select(e => {
                    using (var s = new MemoryStream(e))
                    {
                        try
                        {
                            BitmapImage bi = new BitmapImage();
                            bi.BeginInit();
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.StreamSource = s;
                            bi.EndInit();
                            return bi;
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    }
                })
                .Where(e => e != null);
        }
    }
}
