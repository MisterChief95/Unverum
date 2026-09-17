using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Unverum;

public enum GameFilter
{
    DBFZ,
    DBSZ,
    MHOJ2,
    GBVS,
    GBVSR,
    GGS,
    JF,
    KHIII,
    SN,
    ToA,
    DS,
    IM,
    SMTV,
    KOFXV,
    DNF
}
public enum FeedFilter
{
    Featured,
    Recent,
    Popular,
    None
}
public enum TypeFilter
{
    Mods,
    WiPs,
    Sounds
}
public static class FeedGenerator
{
    private static Dictionary<string, GameBananaModList>? feed;
    public static bool error;
    public static Exception? exception;
    public static GameBananaModList? CurrentFeed;
    public static double GetHeader(this HttpResponseMessage request, string key)
    {
        if (!request.Headers.TryGetValues(key, out var keys))
            return -1;
        return double.Parse(keys.First());
    }
    public static void ClearCache()
    {
        feed?.Clear();
    }
    public static async Task GetFeed(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, bool nsfw, string search, bool zs, bool colorz)
    {
        error = false;
        feed ??= [];
        // Remove oldest key if more than 15 pages are cached
        if (feed.Count > 15)
            feed.Remove(feed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key);
        using var httpClient = new HttpClient();
        var requestUrl = GenerateUrl(page, game, type, filter, category, subcategory, perPage, nsfw, search, zs, colorz);
        if (feed.TryGetValue(requestUrl, out var cachedFeed) && cachedFeed.IsValid)
        {
            CurrentFeed = cachedFeed;
            return;
        }
        CurrentFeed = new() { Records = [] };
        try
        {
            var response = await httpClient.GetAsync(requestUrl);
            var records = JsonSerializer.Deserialize<ObservableCollection<GameBananaRecord>>(await response.Content.ReadAsStringAsync()) ?? [];
            if (game == GameFilter.DBSZ)
            {
                if (zs)
                {
                    // Remove all records from the JSON (ZeroSpark) category
                    foreach (var record in records.Where(x => x.Category.ID == 33583).ToList())
                        records.Remove(record);
                }
                if (colorz)
                {
                    // Remove all records in the Skins category that has ColorZ in the title
                    foreach (var record in records.Where(x => x.Category.ID == 32453
                    && x.Title.Contains("ColorZ", StringComparison.InvariantCultureIgnoreCase)).ToList())
                        records.Remove(record);
                }
            }
            CurrentFeed.Records = records;
            // Get record count from header
            var numRecords = response.GetHeader("X-GbApi-Metadata_nRecordCount");
            if (numRecords != -1)
            {
                var totalPages = Math.Ceiling(numRecords / Convert.ToDouble(perPage));
                if (totalPages == 0)
                    totalPages = 1;
                CurrentFeed.TotalPages = totalPages;
            }
        }
        catch (Exception e)
        {
            error = true;
            exception = e;
            return;
        }
        feed[requestUrl] = CurrentFeed;
    }
    private static string GenerateUrl(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, bool nsfw, string search, bool zs, bool colorz)
    {
        // Base
        var url = "https://gamebanana.com/apiv6/";
        url += type switch
        {
            TypeFilter.Mods => "Mod/",
            TypeFilter.Sounds => "Sound/",
            TypeFilter.WiPs => "Wip/",
            _ => ""
        };
        // Different starting endpoint if requesting all mods instead of specific category
        if (search != null)
            url += $"ByName?_sName=*{search}*&_idGameRow=";
        else if (category.ID != null)
            url += "ByCategory?";
        else
            url += "ByGame?_aGameRowIds[]=";
        if (category.ID == null)
            url += game switch
            {
                GameFilter.DBFZ => "6246&",
                GameFilter.DBSZ => "21179&",
                GameFilter.MHOJ2 => "11605&",
                GameFilter.GBVS => "8897&",
                GameFilter.GBVSR => "19552&",
                GameFilter.GGS => "11534&",
                GameFilter.JF => "7019&",
                GameFilter.KHIII => "9219&",
                GameFilter.SN => "12028&",
                GameFilter.ToA => "13821&",
                GameFilter.DS => "14246&",
                GameFilter.IM => "14247&",
                GameFilter.SMTV => "14768&",
                GameFilter.KOFXV => "15769&",
                GameFilter.DNF => "16693&",
                _ => ""
            };
        // Consistent args
        url += $"_csvProperties=_sName,_sModelName,_sProfileUrl,_aSubmitter,_tsDateUpdated,_tsDateAdded,_aPreviewMedia,_sText,_sDescription,_aCategory,_aRootCategory,_aGame,_nViewCount," +
            $"_nLikeCount,_nDownloadCount,_aFiles,_aModManagerIntegrations,_bIsNsfw,_aAlternateFileSources&_nPerpage={perPage}";
        if (!nsfw)
            url += "&_aArgs[]=_sbIsNsfw = false";
        // Sorting filter
        url += filter switch
        {
            FeedFilter.Recent => "&_sOrderBy=_tsDateUpdated,DESC",
            FeedFilter.Featured => "&_aArgs[]=_sbWasFeatured = true& _sOrderBy=_tsDateAdded,DESC",
            FeedFilter.Popular => "&_sOrderBy=_nDownloadCount,DESC",
            _ => ""
        };
        // Choose subcategory or category
        if (subcategory.ID != null)
            url += $"&_aCategoryRowIds[]={subcategory.ID}";
        else if (category.ID != null)
            url += $"&_aCategoryRowIds[]={category.ID}";

        // Get page number
        url += $"&_nPage={page}";
        // Make url unique for SZ exclusion filters
        if (game == GameFilter.DBSZ)
        {
            if (zs)
                url += "&zs";
            if (colorz)
                url += "&colorz";
        }
        return url;
    }
}
