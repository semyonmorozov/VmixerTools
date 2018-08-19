using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;

namespace PressSharper
{
    public class Blog
    {
        private static readonly XNamespace WordpressNamespace = "http://wordpress.org/export/1.2/";
        private static readonly XNamespace DublinCoreNamespace = "http://purl.org/dc/elements/1.1/";
        private static readonly XNamespace ContentNamespace = "http://purl.org/rss/1.0/modules/content/";
        private static readonly XNamespace ExcerptNamespace = "http://wordpress.org/export/1.2/excerpt/";

        private XElement channelElement;

        public string Title { get; set; }
        public string Description { get; set; }
        public IEnumerable<Author> Authors { get; set; }
        public IEnumerable<Attachment> Attachments { get; set; }

        public Blog(string xml)
            : this(XDocument.Parse(xml))
        {
        }

        public Blog(XDocument doc)
        {
            this.Authors = Enumerable.Empty<Author>();
            this.Attachments = Enumerable.Empty<Attachment>();

            this.InitializeChannelElement(doc);

            if (channelElement == null)
            {
                throw new XmlException("Missing channel element.");
            }

            this.Initialize();
        }

        private void InitializeChannelElement(XDocument document)
        {
            var rssRootElement = document.Root;
            if (rssRootElement == null)
            {
                throw new XmlException("No document root.");
            }

            this.channelElement = rssRootElement.Element("channel");
        }

        private void Initialize()
        {
            this.InitializeTitle();
            this.InitializeDescription();
            this.InitializeAuthors();
            this.InitializeAttachments();
        }

        private void InitializeTitle()
        {
            this.Title = this.GetBasicProperty("title");
        }

        private void InitializeDescription()
        {
            this.Description = this.GetBasicProperty("description");
        }

        private string GetBasicProperty(string elementName)
        {
            var element = this.channelElement.Element(elementName);
            if (element == null)
            {
                throw new XmlException(string.Format("Missing {0}.", elementName));
            }

            return element.Value;
        }

        private void InitializeAuthors()
        {
            this.Authors = this.channelElement.Descendants(WordpressNamespace + "author")
                .Select(ParseAuthorElement);
        }

        private static Author ParseAuthorElement(XElement authorElement)
        {
            var authorIdElement = authorElement.Element(WordpressNamespace + "author_id");
            var authorUsernameElement = authorElement.Element(WordpressNamespace + "author_login");
            var authorEmailElement = authorElement.Element(WordpressNamespace + "author_email");
            var authorDisplayNameElement = authorElement.Element(WordpressNamespace + "author_display_name");

            if (authorIdElement == null || authorUsernameElement == null || authorEmailElement == null ||
                authorDisplayNameElement == null)
            {
                throw new XmlException("Unable to parse malformed author.");
            }

            var author = new Author
            {
                Id = int.Parse(authorIdElement.Value),
                Username = authorUsernameElement.Value,
                Email = authorEmailElement.Value,
                DisplayName = authorDisplayNameElement.Value
            };

            return author;
        }

        private void InitializeAttachments()
        {
            this.Attachments = this.channelElement.Elements("item")
                .Where(e => this.IsAttachmentItem(e))
                .Select(ParseAttachmentElement);
        }

        public IEnumerable<Post> GetPosts()
        {
            return this.channelElement.Elements("item")
                .Where(e => this.IsPostItem(e) && this.IsPublished(e))
                .Select(ParsePostElement);
        }

        public IEnumerable<Page> GetPages()
        {
            return this.channelElement.Elements("item")
                .Where(e => IsPageItem(e) && IsPublished(e))
                .Select(ParsePageElement);
        }

        public IEnumerable<Receipt> GetReceipts()
        {
            return this.channelElement.Elements("item")
                .Where(IsReceiptItem)
                .Select(ParseReceiptElement);
        }

        public IEnumerable<Aroma> GetAromas()
        {
            return this.channelElement.Elements("item")
                .Where(IsAromaItem)
                .Select(ParseAromaElement);
        }

        public IEnumerable<ImageLink> GetImageLinks()
        {
            return this.channelElement.Elements("item")
                .Where(IsImageLinkItem)
                .Select(ParseImageLinkElement);
        }

        private bool IsImageLinkItem(XElement arg)
        {
            //TODO
            return true;
        }

        private bool IsAromaItem(XElement xElement)
        {
            //TODO
            return true;
        }

        private bool IsReceiptItem(XElement xElement)
        {
            //TODO
            return true;
        }

        private bool IsPostItem(XElement itemElement)
        {
            return itemElement?.Element(WordpressNamespace + "post_type")?.Value == "post";
        }

        private bool IsPageItem(XElement itemElement)
        {
            return itemElement?.Element(WordpressNamespace + "post_type")?.Value == "page";
        }

        private bool IsAttachmentItem(XElement itemElement)
        {
            return itemElement?.Element(WordpressNamespace + "post_type")?.Value == "attachment";
        }

        private bool IsPublished(XElement itemElement)
        {
            return itemElement?.Element(WordpressNamespace + "status")?.Value == "publish";
        }

        private Attachment ParseAttachmentElement(XElement attachmentElement)
        {
            var attachmentIdElement = attachmentElement.Element(WordpressNamespace + "post_id");
            var attachmentTitleElement = attachmentElement.Element("title");
            var attachmentUrlElement = attachmentElement.Element(WordpressNamespace + "attachment_url");

            if (attachmentIdElement == null ||
                attachmentTitleElement == null ||
                attachmentUrlElement == null)
            {
                throw new XmlException("Unable to parse malformed attachment.");
            }

            var attachment = new Attachment()
            {
                Id = int.Parse(attachmentIdElement.Value),
                Title = attachmentTitleElement.Value,
                Url = attachmentUrlElement.Value
            };

            return attachment;
        }

        private Post ParsePostElement(XElement postElement)
        {
            var postTitleElement = postElement.Element("title");
            var postUsernameElement = postElement.Element(DublinCoreNamespace + "creator");
            var postBodyElement = postElement.Element(ContentNamespace + "encoded");
            var postPublishDateElement = postElement.Element(WordpressNamespace + "post_date");
            var postSlugElement = postElement.Element(WordpressNamespace + "post_name");

            if (postTitleElement == null ||
                postUsernameElement == null ||
                postBodyElement == null ||
                postPublishDateElement == null ||
                postSlugElement == null)
            {
                throw new XmlException("Unable to parse malformed post.");
            }

            var postExcerptElement = postElement.Element(ExcerptNamespace + "encoded");

            var post = new Post
            {
                Author = this.GetAuthorByUsername(postUsernameElement.Value),
                Body = postBodyElement.Value,
                Excerpt = postExcerptElement?.Value,
                PublishDate = DateTime.Parse(postPublishDateElement.Value),
                Slug = postSlugElement.Value,
                Title = postTitleElement.Value
            };

            // get categories and tags
            var wpCategoriesElements = postElement.Elements("category");
            foreach (var wpCategory in wpCategoriesElements)
            {
                var domainAttribute = wpCategory.Attribute("domain");
                if (domainAttribute == null)
                {
                    throw new XmlException("Unable to parse malformed wordpress categorization.");
                }

                if (domainAttribute.Value == "category")
                {
                    post.Categories.Add(new Category
                    {
                        Slug = wpCategory.Attribute("nicename")?.Value,
                        Name = wpCategory.Value
                    });
                }
                else if (domainAttribute.Value == "post_tag")
                {
                    post.Tags.Add(new Tag
                    {
                        Slug = wpCategory.Attribute("nicename")?.Value,
                        Name = wpCategory.Value
                    });
                }
            }

            // get featured image
            var postMetaElements = postElement.Elements(WordpressNamespace + "postmeta");
            foreach (var postMeta in postMetaElements)
            {
                var metaKeyElement = postMeta.Element(WordpressNamespace + "meta_key");
                if (metaKeyElement?.Value == "_thumbnail_id")
                {
                    var metaValueElement = postMeta.Element(WordpressNamespace + "meta_value");
                    if (metaValueElement?.Value != null)
                    {
                        int attachmentId = int.Parse(metaValueElement.Value);
                        post.FeaturedImage = this.GetAttachmentById(attachmentId);
                        break;
                    }
                }
            }

            return post;
        }

        private Receipt ParseReceiptElement(XElement receiptElement)
        {
            var receiptId = receiptElement.Element(WordpressNamespace + "post_id")?.Value;
            
            try
            {
                var name = receiptElement.Element("title")?.Value;
                var description = GetPostmetaValue(receiptElement, "preview_text");
                var infusion = GetPostmetaValue(receiptElement, "infusion");
                var pg = GetPostmetaValue(receiptElement, "pg");
                var vg = GetPostmetaValue(receiptElement, "vg");
                var ratio = GetPostmetaValue(receiptElement, "vg_pg");

                var flavorIds = Enumerable.Range(1, 15).Select(i => GetPostmetaValue(receiptElement, $"aroma_{i}"))
                    .Where(i => !string.IsNullOrEmpty(i)).ToArray();
                var flavorQuantitys = Enumerable.Range(1, 15)
                    .Select(i => GetPostmetaValue(receiptElement, $"aroma_quantity_{i}"))
                    .Where(i => !string.IsNullOrEmpty(i)).ToArray();

                var aromas = Enumerable.Range(0, flavorIds.Length)
                    .ToDictionary(i => new Aroma
                        {
                            Id = flavorIds[i].Split('\"')[1]
                        },
                        i => double.Parse(flavorQuantitys[i]));

                return new Receipt
                {
                    Id = int.Parse(receiptId),
                    Name = name,
                    Description = description,
                    Infusion = infusion,
                    Pg = double.Parse(pg),
                    Vg = double.Parse(vg),
                    Ratio = ratio,
                    Aromas = aromas
                };
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(receiptId);
                Console.WriteLine(e);
                Console.ResetColor();
            }

            return null;
        }

        private Aroma ParseAromaElement(XElement aromaElement)
        {
            var title = aromaElement.Element("title")?.Value;
            var id = aromaElement.Element(WordpressNamespace + "post_id")?.Value;
            return new Aroma
            {
                Id = id,
                Name = title
            };
        }

        private ImageLink ParseImageLinkElement(XElement imageLinkItem)
        {
            var name = imageLinkItem.Element("guid")?.Value.Split('/').Last();
            int receiptId = int.Parse(imageLinkItem.Element(WordpressNamespace +"post_parent")?.Value);
            return new ImageLink
            {
                ReceiptId = receiptId,
                Name = name
            };
        }

        private static string GetPostmetaValue(XContainer postmeta, string key)
        {
            return postmeta.Elements(WordpressNamespace + "postmeta")
                .FirstOrDefault(m => m.Element(WordpressNamespace + "meta_key")?.Value == key)?
                .Element(WordpressNamespace + "meta_value")?.Value;
        }

        private Page ParsePageElement(XElement pageElement)
        {
            var pageIdElement = pageElement.Element(WordpressNamespace + "post_id");
            var pageParentIdElement = pageElement.Element(WordpressNamespace + "post_parent");
            var pageTitleElement = pageElement.Element("title");
            var pageUsernameElement = pageElement.Element(DublinCoreNamespace + "creator");
            var pageBodyElement = pageElement.Element(ContentNamespace + "encoded");
            var pagePublishDateElement = pageElement.Element(WordpressNamespace + "post_date");
            var pageSlugElement = pageElement.Element(WordpressNamespace + "post_name");

            if (pageIdElement == null ||
                pageParentIdElement == null ||
                pageTitleElement == null ||
                pageUsernameElement == null ||
                pageBodyElement == null ||
                pagePublishDateElement == null ||
                pageSlugElement == null)
            {
                throw new XmlException("Unable to parse malformed page.");
            }

            var page = new Page
            {
                Id = int.Parse(pageIdElement.Value),
                ParentId = pageParentIdElement.Value != "0" ? int.Parse(pageParentIdElement.Value) : (int?) null,
                Author = this.GetAuthorByUsername(pageUsernameElement.Value),
                Body = pageBodyElement.Value,
                PublishDate = DateTime.Parse(pagePublishDateElement.Value),
                Slug = pageSlugElement.Value,
                Title = pageTitleElement.Value
            };

            return page;
        }

        private Author GetAuthorByUsername(string username)
        {
            return this.Authors.FirstOrDefault(a => a.Username == username);
        }

        private Attachment GetAttachmentById(int attachmentId)
        {
            return this.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        }
    }
}