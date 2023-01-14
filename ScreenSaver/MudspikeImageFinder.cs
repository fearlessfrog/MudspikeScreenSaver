/*
 *  Mudspike Screen Saver
 *  https://opensource.org/licenses/MIT
 */
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MudspikeScreenSaver
{
    public struct MudspikeImageFinderResult
    {
        public string url;
        public string author;
        public string topic_slug;
        public string created_at;
    }

    internal class MudspikeImageFinder
    {
        HttpClient httpClient;

        public MudspikeImageFinder()
        {
            httpClient = new HttpClient();
        }

        public MudspikeImageFinderResult FindCoolImage(string query = "screens") //  e.g. @PaulRix tags:x-plane
        {
            // Build out the Discoure search API url - https://docs.discourse.org/#tag/Search
            var builder = new UriBuilder("https://forums.mudspike.com/search.json");

            // To vary the images, pick a random year for the before query facet
            var rand = new Random();
            var year = rand.Next(17, 23);
            var before = $"20{year.ToString("D2")}-{rand.Next(1, 12).ToString("D2")}-01";            
            builder.Query = $"q=before:{before} with:images order:likes {query} #screens-aars"; // tags:screensaver etc
            var url = builder.ToString();
            Console.WriteLine($"Search URL: {url}");

            try { 
                // We're on the wait timer thread anyway..
                var content = httpClient.GetStringAsync(url).GetAwaiter().GetResult();

                var document = JsonDocument.Parse(content);
                JsonElement root = document.RootElement;
                JsonElement grouped_search_result = root.GetProperty("grouped_search_result");
                if (grouped_search_result.ValueKind != JsonValueKind.Null)
                {
                    JsonElement post_ids = grouped_search_result.GetProperty("post_ids");
                    Console.WriteLine($"Posts with {query}: {post_ids}");

                    // Pick a random post in the results to vary it up a bit?                    
                    var post_id = post_ids[rand.Next(post_ids.GetArrayLength() / 2)];                    
                    url = $"https://forums.mudspike.com/posts/{post_id}.json";
                    Console.WriteLine($"Post chosen: {url}");

                    content = httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                    
                    // There's no good way to get an image from a post, so spelunk the cooked property
                    document = JsonDocument.Parse(content);
                    root = document.RootElement;
                    JsonElement cooked = root.GetProperty("cooked");
                    JsonElement display_username = root.GetProperty("username");
                    JsonElement topic_slug = root.GetProperty("topic_slug");
                    JsonElement created_at = root.GetProperty("created_at");                    
                    //Console.WriteLine("Cooked: " + cooked.GetString());
                    Console.WriteLine("Topic slug: " + topic_slug.GetString());

                    // Good grief, .NET needs a built in HTML DOM parser
                    Regex regx = new Regex("href=\\\"https://uploads.*?(?= )", RegexOptions.IgnoreCase);
                    MatchCollection matches = regx.Matches(cooked.GetString());                    
                    var match = matches[rand.Next(0, matches.Count)];                    
                    var image_url = match.Value;

                    // Slice off the href= quote bits without using a proper HTML parse dependency
                    image_url = image_url.Substring(6, image_url.Length - 7);
                    Console.WriteLine("Image url: " + image_url);

                    // The results of the search, posts and image finding..
                    MudspikeImageFinderResult res = new MudspikeImageFinderResult
                    {
                        url = image_url,
                        author = display_username.GetString(),
                        topic_slug = topic_slug.GetString(),
                        created_at = created_at.GetString()
                    };
                    return res;    
                }
                else
                {
                    Console.WriteLine("No posts found, returning default image");
                }                                
            
            }            
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return DefaultImage();
            }            

            return DefaultImage();
        }

        public MudspikeImageFinderResult DefaultImage()
        {
            // Plain logo as nothing else to show
            MudspikeImageFinderResult res = new MudspikeImageFinderResult
            {
                url = "https://forums.mudspike.com/uploads/default/original/2X/c/c9aad5dd351c0ab3dc1750a8957f73c67fb91b91.jpg",
                topic_slug = "mudspike-screensaver",
                author = "fearlessfrog",
                created_at = "2023-01-13T22:00:10"
            };            
            return res;                           
        }
    }
}
