using HtmlAgilityPack;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseURL = "https://www.utmn.ru";
            var fileTypes = new List<string> { ".pdf" };

            Crawler crawler = new Crawler(baseURL, fileTypes);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            crawler.Run();
            watch.Stop();
            var elapsed = watch.ElapsedMilliseconds;
            Console.WriteLine("\nTime elapsed: " + elapsed);

            Console.WriteLine("Links count: " + crawler.Links.Count);
            foreach (var type in crawler.Files.Keys)
            {
                Console.WriteLine("Files of type [" + type + "] count: " + crawler.Files[type].Count);
            }
        }
    }

    class Link
    {
        public string URL;
        public bool IsChecked;

        public Link(string URL)
        {
            this.URL = URL;
            this.IsChecked = false;
        }

        public bool IsFileOf(string type)
        {
            string fileName = this.URL.Split("/").Last().ToLower();
            return fileName.Contains(type);
        }
    }

    class Crawler
    {
        public string BaseURL;
        public List<Link> Links;
        public Dictionary<string, List<Link>> Files;
        public List<string> TargetTypes;
        public const int MAX_RECURSION_DEPTH = 2;
        private HtmlWeb _web;

        public Crawler(string baseURL, List<string> types)
        {
            this.BaseURL = baseURL;
            this.Links = new List<Link>();
            this.TargetTypes = types;
            this.Files = new Dictionary<string, List<Link>>();
            foreach (var type in types)
            {
                this.Files.Add(type, new List<Link>());
            }
            this._web = new HtmlWeb();
        }

        public void Run()
        {
            Link baseLink = new Link(BaseURL);
            Visit(baseLink, 0);
        }

        private void Visit(Link link, int recursionDepth)
        {
            if (recursionDepth <= Crawler.MAX_RECURSION_DEPTH)
            {
                if (link.URL.StartsWith("/"))
                {
                    link.URL = this.BaseURL + link.URL;
                }
                if (!link.URL.StartsWith(BaseURL) || link.URL.StartsWith("mailto") || !link.URL.StartsWith("https"))
                {
                    return;
                }

                if (link.URL.Contains("www") && !link.URL.StartsWith("https"))
                {
                    return;
                }

                if (link.URL.EndsWith("/"))
                {
                    link.URL.TrimEnd('/');
                }

                if (!this.Links.Exists(x => x.URL == link.URL))
                {
                    this.Links.Add(link);
                    Console.WriteLine("LINK\t\t" + link.URL);////////////////////////////
                    link.IsChecked = true;

                    var doc = _web.Load(link.URL);
                    var linkNodes = doc.DocumentNode.SelectNodes("//a");
                    var imageNodes = doc.DocumentNode.SelectNodes("//img");

                    if (imageNodes is not null)
                    {
                        CollectImages(imageNodes);
                    }

                    if (linkNodes is not null)
                    {
                        foreach (var node in linkNodes)
                        {
                            Link newLink = new Link(node.GetAttributeValue("href", ""));

                            if (newLink.URL.Split("/").Last().Contains("."))
                            {
                                if (newLink.URL.StartsWith("/"))
                                {
                                    newLink.URL = BaseURL + newLink.URL;
                                }
                                if (!newLink.URL.StartsWith(BaseURL))
                                {
                                    break;
                                }
                                // If type not in the list then link is skipped
                                foreach (var type in this.Files.Keys)
                                {
                                    if (newLink.IsFileOf(type))
                                    {
                                        if (!this.Files[type].Exists(x => x.URL == newLink.URL))
                                        {
                                            this.Files[type].Add(newLink);
                                            Console.WriteLine("FILE [" + type + "]\t" + newLink.URL);///////////////////////////////
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                Visit(newLink, recursionDepth + 1);
                            }
                        }
                    }
                }
            }
        }

        private void CollectImages(HtmlNodeCollection nodes)
        {
            foreach (var node in nodes)
            {
                Link newLink = new Link(node.GetAttributeValue("src", ""));
                string imageName = newLink.URL.Split("/").Last();
                string imageType = "." + imageName.Split(".").Last().ToLower();

                if (newLink.URL.StartsWith("/"))
                {
                    newLink.URL = this.BaseURL + newLink.URL;
                }

                if (!newLink.URL.StartsWith(BaseURL))
                {
                    break;
                }

                if (this.Files.Keys.Contains(imageType))
                {
                    if (!this.Files[imageType].Exists(x => x.URL == newLink.URL))
                    {
                        this.Files[imageType].Add(newLink);
                        Console.WriteLine("FILE [" + imageType + "]\t" + newLink.URL);/////////////////////////////
                    }
                }
                else
                {
                    this.Files.Add(imageType, new List<Link>() { newLink });
                    Console.WriteLine("FILE [" + imageType + "]\t" + newLink.URL);///////////////////////////////
                }
            }
        }
    }
}
