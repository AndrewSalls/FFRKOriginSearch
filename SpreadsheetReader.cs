using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FFRKOriginSearch
{
    public class SpreadsheetReader
    {
        private enum FileType
        {
            MissingSoulBreaks, NamesWithPeriods, SoulBreakTypes, Realms
        }

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        public static readonly string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        public static readonly string ApplicationName = "FFRK Soul Break Origin Completion Search";
        public static readonly string spreadsheetId = "1tsFgIq2laBI75YME-NQ7RwzpqXJ1OnAhC0gsQ0rBiBk";

        public static async Task Main(string[] _)
        {
            Console.WriteLine("Loading Character Info...");
            IDictionary<string, (int, IList<string>)> soulBreakList = await OfficialSiteCounter.GatherSoulBreaksAsync();
            RemoveDuplicateSoulBreaks(soulBreakList);
            Application.Run(new VisualForm(soulBreakList));
        }

        public static void RemoveDuplicateSoulBreaks(IDictionary<string, (int, IList<string>)> siteSoulBreaks)
        {
            UserCredential credential;
            using (FileStream stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "Goldrew Amp",
                    CancellationToken.None,
                    new FileDataStore("token.json", true)).Result;
            }

            SheetsService service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            SpreadsheetsResource.ValuesResource.GetRequest sheet;
            ValueRange response;
            string name, sb;

            Console.WriteLine("\nBeginning Spreadsheet Scan...");

            foreach (string realm in ReadResourceFile(FileType.Realms))
            {
                sheet = service.Spreadsheets.Values.Get(spreadsheetId, realm);
                sheet.MajorDimension = SpreadsheetsResource.ValuesResource.GetRequest.MajorDimensionEnum.COLUMNS;
                response = sheet.Execute();

                if (response != null && response.Values != null && response.Values.Count > 0)
                {
                    for (int row = 0; row < response.Values.Count; row += 1)
                    {
                        if (response.Values[row].Count <= 0)
                            continue;

                        name = response.Values[row][0].ToString();

                        for (int col = 1; col < response.Values[row].Count; col += 1)
                        {
                            sb = FormatSoulBreakName(response.Values[row][col].ToString());
                            
                            if (siteSoulBreaks.ContainsKey(name) && siteSoulBreaks[name].Item2.Contains(sb))
                                siteSoulBreaks[name].Item2.Remove(sb);
                            else if (siteSoulBreaks.ContainsKey(OfficialSiteCounter.HeroAbilities) && siteSoulBreaks[OfficialSiteCounter.HeroAbilities].Item2.Contains(sb))
                                siteSoulBreaks[OfficialSiteCounter.HeroAbilities].Item2.Remove(sb);
                            else if(!ReadMissingSiteSoulBreaks().Contains((name, sb)))
                                Console.WriteLine($"{response.Values[row][0]}'s \"{sb}\" was found on spreadsheet, but was not found on the FFRK website.");
                        }
                    }
                }
                else
                    Console.WriteLine($"Realm {realm} is empty.");
            }
        }

        private static string FormatSoulBreakName(string unformattedName)
        {
            string soulBreakList = string.Join("|", ReadResourceFile(FileType.SoulBreakTypes));
            string[] periodNameList = ReadResourceFile(FileType.NamesWithPeriods);

            for (int i = 0; i < periodNameList.Length; i += 1)
                if (unformattedName.StartsWith(periodNameList[i]))
                    return Regex.Replace(periodNameList[i], "^(" + soulBreakList + "): (.+)", "$2");

            return Regex.Replace(unformattedName.Split('\n')[0], "^(" + soulBreakList + "): ([^.]+)\\..+", "$2").Trim();
        }

        private static IList<(string, string)> ReadMissingSiteSoulBreaks()
        {
            return ReadResourceFile(FileType.MissingSoulBreaks).Select(s => (s.Split('-').First().Trim(), s.Split('-').Last().Trim())).ToList();
        }

        private static string[] ReadResourceFile(FileType type)
        {
            switch(type)
            {
                case FileType.MissingSoulBreaks:
                    return File.ReadAllLines("ModData" + Path.DirectorySeparatorChar + "FFRKSiteMissingSoulBreaks.txt");
                case FileType.NamesWithPeriods:
                    return File.ReadAllLines("ModData" + Path.DirectorySeparatorChar + "NamesWithPeriodsInThem.txt");
                case FileType.Realms:
                    return File.ReadAllLines("ModData" + Path.DirectorySeparatorChar + "SpreadsheetRealms.txt");
                case FileType.SoulBreakTypes:
                    return File.ReadAllLines("ModData" + Path.DirectorySeparatorChar + "SoulBreakTypes.txt");
                default:
                    throw new ArgumentException("Invalid File Type Requested");
            }
        }
    }
}