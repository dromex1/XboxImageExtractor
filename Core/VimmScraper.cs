using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XboxImageExtractor.Core
{
    public class VimmGame
    {
        public string Title { get; set; } = string.Empty;
        public string VaultId { get; set; } = string.Empty;
        public string System { get; set; } = string.Empty; // "Xbox360" or "Xbox"
        public string Url => $"https://vimm.net/vault/{VaultId}";
    }

    public static class VimmScraper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static VimmScraper()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        /// <summary>
        /// Search Vimm's Lair directly using its search endpoint.
        /// This is the primary way to find games - it bypasses JS rendering issues.
        /// URL: https://vimm.net/vault/?p=list&q=angry
        /// </summary>
        public static async Task<List<VimmGame>> SearchGamesAsync(string query, string system)
        {
            var allGames = new List<VimmGame>();
            try
            {
                // Global search - we have to fetch everything and filter by system
                string url = $"https://vimm.net/vault/?p=list&q={Uri.EscapeDataString(query)}";
                string html = await _httpClient.GetStringAsync(url);
                var results = ParseSearchResultsFromHtml(html);
                
                string systemSlug = system; // "Xbox360", "Xbox", or "All"
                foreach (var g in results)
                {
                    if (systemSlug == "All" && (g.System == "Xbox360" || g.System == "Xbox"))
                    {
                        allGames.Add(g);
                    }
                    else if (g.System == systemSlug)
                    {
                        allGames.Add(g);
                    }
                }
            }
            catch { }
            return allGames;
        }

        private static List<VimmGame> ParseSearchResultsFromHtml(string html)
        {
            var games = new List<VimmGame>();
            // Search results have the system in the first <td>: <tr><td>Xbox360</td><td><a href= "/vault/95005">Game</a>
            var regex = new Regex(@"<tr>\s*<td[^>]*>(.*?)</td>[^<]*<td[^>]*><a[^>]*href\s*=\s*[""']/vault/(\d{4,})[""'][^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var matches = regex.Matches(html);
            var seenIds = new HashSet<string>();

            foreach (Match m in matches)
            {
                string sys = m.Groups[1].Value.Trim();
                // We only care about Xbox and Xbox360 right now
                if (sys != "Xbox" && sys != "Xbox360") continue;

                string vaultId = m.Groups[2].Value;
                string rawTitle = Regex.Replace(m.Groups[3].Value, "<.*?>", string.Empty);
                string title = System.Net.WebUtility.HtmlDecode(rawTitle).Trim();

                if (string.IsNullOrWhiteSpace(title) || title.Length < 2 || seenIds.Contains(vaultId)) continue;

                seenIds.Add(vaultId);
                games.Add(new VimmGame { Title = title, VaultId = vaultId, System = sys });
            }
            return games;
        }

        /// <summary>
        /// Fetch a single letter page from Vimm's Lair.
        /// URL: https://vimm.net/vault/?p=list&system=Xbox360&section=A
        /// </summary>
        public static async Task<List<VimmGame>> FetchLetterAsync(string letter, string system)
        {
            try
            {
                string section = letter == "#" ? "number" : letter;
                string url = $"https://vimm.net/vault/?p=list&system={system}&section={section}";
                string html = await _httpClient.GetStringAsync(url);
                return ParseGamesFromHtml(html, system);
            }
            catch
            {
                return new List<VimmGame>();
            }
        }

        /// <summary>
        /// Load all games from A-Z using the ?p=list API endpoint (not the JS-rendered pages).
        /// Reports each letter batch incrementally.
        /// </summary>
        public static async Task ScrapeGamesAsync(string system, IProgress<string> progress, IProgress<List<VimmGame>> gamesFound)
        {
            string[] letters = { "#", "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z" };

            // Seed popular games instantly so user sees something
            if (system == "Xbox360")
            {
                var seedGames = new List<VimmGame> {
                    new VimmGame { Title = "Halo 3", VaultId = "78231", System = system },
                    new VimmGame { Title = "Grand Theft Auto IV", VaultId = "78373", System = system },
                    new VimmGame { Title = "Call of Duty: Black Ops II", VaultId = "78950", System = system },
                    new VimmGame { Title = "Minecraft: Xbox 360 Edition", VaultId = "79837", System = system },
                    new VimmGame { Title = "Red Dead Redemption", VaultId = "78644", System = system },
                    new VimmGame { Title = "Forza Horizon", VaultId = "78839", System = system },
                    new VimmGame { Title = "Gears of War 3", VaultId = "78783", System = system },
                    new VimmGame { Title = "Fallout: New Vegas", VaultId = "78572", System = system }
                };
                gamesFound?.Report(seedGames);
            }

            foreach (var letter in letters)
            {
                progress?.Report($"Fetching {system} games: letter '{letter}'...");

                try
                {
                    var games = await FetchLetterAsync(letter, system);
                    if (games.Count > 0)
                    {
                        gamesFound?.Report(games);
                    }
                }
                catch (Exception ex)
                {
                    var errBatch = new List<VimmGame> {
                        new VimmGame { Title = $"[ERROR] Letter {letter}: {ex.Message}", System = system, VaultId = "ERROR" }
                    };
                    gamesFound?.Report(errBatch);
                }

                await Task.Delay(200); // Rate limit between letters
            }
        }

        private static List<VimmGame> ParseGamesFromHtml(string html, string system)
        {
            var games = new List<VimmGame>();

            // Added \s*=\s* to handle formatting inconsistencies like href= "/vault/..."
            var regex = new Regex(@"<a[^>]*href\s*=\s*[""']/vault/(\d{4,})[""'][^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var matches = regex.Matches(html);

            var seenIds = new HashSet<string>();

            foreach (Match m in matches)
            {
                string vaultId = m.Groups[1].Value;
                string rawTitle = Regex.Replace(m.Groups[2].Value, "<.*?>", string.Empty);
                string title = System.Net.WebUtility.HtmlDecode(rawTitle).Trim();

                if (string.IsNullOrWhiteSpace(title)) continue;
                if (title.Length < 2) continue;
                if (seenIds.Contains(vaultId)) continue;

                seenIds.Add(vaultId);
                games.Add(new VimmGame
                {
                    Title = title,
                    VaultId = vaultId,
                    System = system
                });
            }

            return games;
        }
    }
}
