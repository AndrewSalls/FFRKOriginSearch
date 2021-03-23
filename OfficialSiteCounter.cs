using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace FFRKOriginSearch
{
    public static class OfficialSiteCounter
    {
        public static readonly string HeroAbilities = "HERO_ABILITIES";

        public static readonly string baseSiteName = "https://ffrkstrategy.gamematome.jp/game/951/wiki/Character";

        public static readonly string soulBreakCategory = "/game/951/wiki/Soul%20Break%20List";
        public static readonly string recordMateriaCategory = "/game/951/wiki/Record%20Materia%20List";
        public static readonly string legendMateriaCategory = "/game/951/wiki/Legend%20Materia";

        public static readonly string heroAbilitySiteName = "https://ffrkstrategy.gamematome.jp/game/951/wiki/Ability_Hero%20Abilities";
        public static readonly string heroAbilityCategory = "/game/951/wiki/Ability_Hero%20Abilities";

        private static int _count;
        private static int _total;

        private static readonly SemaphoreSlim _mutex = new SemaphoreSlim(32);

        public static async Task<IDictionary<string, (int, IList<string>)>> GatherSoulBreaksAsync()
        {
            _count = 0;
            _total = 0;

            HtmlDocument site = new HtmlWeb() { OverrideEncoding = Encoding.UTF8 }.Load(baseSiteName);

            string currentRealm = "Core Classes";
            ConcurrentDictionary<string, (int, IList<string>)> output = new ConcurrentDictionary<string, (int, IList<string>)>();
            List<Task> parallelActions = new List<Task>(); 

            foreach (HtmlNode row in site.DocumentNode.QuerySelector("[itemprop='articleBody']").Descendants("tr").Skip(1))
            {
                IEnumerable<string> parts = row.Descendants("a").Select(n => n.InnerText);

                if (!currentRealm.Equals(parts.Last()))
                {
                    Console.WriteLine($"{currentRealm} loading started...");
                    currentRealm = parts.Last();
                }
                _total += 1;

                //F*cking Y'shtola. Also, should never fail because keys are only added, not removed
                parallelActions.Add(Task.Run(async () => {
                    await _mutex.WaitAsync();
                    try
                    {
                        output.TryAdd(HttpUtility.HtmlDecode(parts.First()), (_count, GetSoulBreaksFor(parts.First(), parts.Last())));
                    }
                    finally
                    {
                        _mutex.Release();
                    }
                }));
            }
            Console.WriteLine("Beyond loading started...");

            Console.WriteLine("\nLoading characters:");

            await Task.WhenAll(parallelActions);

            Console.WriteLine("\n\nHero Abilities loading started...");
            GetHeroAbilities(output);

            return output;
        }

        private static IList<string> GetSoulBreaksFor(string characterName, string realm)
        {
            IList<string> output = new List<string>();

            HtmlDocument subsite = new HtmlWeb() { OverrideEncoding = Encoding.UTF8 }.Load(baseSiteName + "_" + Uri.EscapeDataString(realm) + "_" + Uri.EscapeDataString(HttpUtility.HtmlDecode(characterName)));

            foreach (HtmlNode node in subsite.DocumentNode.QuerySelector("div[itemprop='articleBody']").QuerySelectorAll($"a[href*='{soulBreakCategory}']"))
                output.Add(Regex.Replace(HttpUtility.HtmlDecode(node.InnerText), "\\(.+\\)", "").Trim());

            foreach (HtmlNode node in subsite.DocumentNode.QuerySelector("div[itemprop='articleBody']").QuerySelectorAll($"a[href*='{recordMateriaCategory}']"))
                output.Add(Regex.Replace(HttpUtility.HtmlDecode(node.InnerText), "\\(.+\\)", "").Trim());

            foreach (HtmlNode node in subsite.DocumentNode.QuerySelector("div[itemprop='articleBody']").QuerySelectorAll($"a[href*='{legendMateriaCategory}']"))
                output.Add(Regex.Replace(HttpUtility.HtmlDecode(node.InnerText), "\\(.+\\)", "").Trim());

            Console.Write($"\r{++_count}/{_total}");
            return output;
        }

        private static void GetHeroAbilities(IDictionary<string, (int, IList<string>)> output)
        {
            HtmlDocument abilitySite = new HtmlWeb() { OverrideEncoding = Encoding.UTF8 }.Load(heroAbilitySiteName);
            foreach (HtmlNode node in abilitySite.DocumentNode.QuerySelector("div[itemprop='articleBody']").QuerySelectorAll($"a[href*='{heroAbilityCategory}']"))
                output[FixName(Regex.Replace(HttpUtility.HtmlDecode(node.InnerText), ".+\\((.+)\\)", "$1").Trim(), node.InnerText)].Item2.Add(Regex.Replace(HttpUtility.HtmlDecode(node.InnerText), "\\(.+\\)", "").Trim());
        }

        private static string FixName(string inner, string fullText)
        {
            switch(fullText)
            {
                case "Paladin Force(Cecil)":
                    return "Cecil (Paladin)";
                case "Darkness(Cecil)":
                    return "Cecil (Dark Knight)";
                default:
                    return inner;
            }
        }
    }
}
