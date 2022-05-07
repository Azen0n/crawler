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
            Console.WriteLine($"\nRecursion depth: {Crawler.MAX_RECURSION_DEPTH}");
            Console.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds / 1000.0:.00} sec");

            Console.WriteLine($"Links count: {crawler.Links.Count}");
            foreach (var type in crawler.Files.Keys)
            {
                Console.WriteLine($"Files of type [{type}] count: {crawler.Files[type].Count}");
            }
        }
    }

    class Link
    {
        public string URL;

        public Link(string URL)
        {
            this.URL = URL;
        }

        public bool IsFileOf(string type)
        {
            string fileName = this.URL.Split("/").Last().ToLower();
            return fileName.Contains(type);
        }

        public void AdjustURL(string BaseURL)
        {
            if (URL.StartsWith("/"))
                URL = BaseURL + URL;

            if (URL.EndsWith("/"))
                URL.TrimEnd('/');
        }

        public bool IsValid(string BaseURL)
        {
            if (!URL.StartsWith(BaseURL) || URL.StartsWith("mailto") || !URL.StartsWith("https"))
                return false;

            if (URL.Contains("www") && !URL.StartsWith("https"))
                return false;

            if (URL.StartsWith("//"))
                return false;

            return true;
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
                link.AdjustURL(BaseURL);

                if (link.IsValid(BaseURL) && !this.Links.Exists(x => x.URL == link.URL))
                {
                    this.Links.Add(link);
                    Console.WriteLine($"LINK\t\t{link.URL}");

                    var doc = _web.Load(link.URL);
                    var linkNodes = doc.DocumentNode.SelectNodes("//a");
                    var imageNodes = doc.DocumentNode.SelectNodes("//img");

                    if (imageNodes is not null)
                    {
                        CollectImages(imageNodes);
                    }

                    if (linkNodes is not null)
                    {
                        List<Task> tasks = new List<Task>();
                        foreach (var node in linkNodes)
                        {
                            Link newLink = new Link(node.GetAttributeValue("href", ""));

                            if (newLink.URL.Split("/").Last().Contains("."))
                            {
                                newLink.AdjustURL(BaseURL);

                                if (!newLink.IsValid(BaseURL))
                                    break;



                                // If type not in the list then link is skipped
                                foreach (var type in this.Files.Keys)
                                {
                                    if (newLink.IsFileOf(type))
                                    {
                                        if (!this.Files[type].Exists(x => x.URL == newLink.URL))
                                        {
                                            this.Files[type].Add(newLink);
                                            Console.WriteLine($"FILE [{type}]\t{newLink.URL}");
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                Task task = Task.Factory.StartNew(() => Visit(newLink, recursionDepth + 1));
                                tasks.Add(task);
                            }
                        }
                        Task.WaitAll(tasks.ToArray());
                    }
                }
            }
        }

        private void CollectImages(HtmlNodeCollection nodes)
        {
            List<Task> tasks = new List<Task>();
            foreach (var node in nodes)
            {
                Task task = Task.Factory.StartNew(() => TaskStuff(node));
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
        }

        private void TaskStuff(HtmlNode node)
        {
            Link newLink = new Link(node.GetAttributeValue("src", ""));
            string imageName = newLink.URL.Split("/").Last();
            string imageType = "." + imageName.Split(".").Last().ToLower();

            newLink.AdjustURL(BaseURL);

            if (newLink.IsValid(BaseURL))
            {
                if (this.Files.Keys.Contains(imageType))
                {
                    if (!this.Files[imageType].Exists(x => x.URL == newLink.URL))
                    {
                        this.Files[imageType].Add(newLink);
                        Console.WriteLine($"FILE [{imageType}]\t{newLink.URL}");
                    }
                }
                else
                {
                    this.Files.Add(imageType, new List<Link>() { newLink });
                    Console.WriteLine($"FILE [{imageType}]\t{newLink.URL}");
                }
            }
        }
    }
}
