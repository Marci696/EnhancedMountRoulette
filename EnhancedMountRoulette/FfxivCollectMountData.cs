using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EnhancedMountRoulette;

/// <summary>
/// Own% / Patch metadata from the FFXIV Collect public API, cached on disk.
/// </summary>
public static class FfxivCollectMountData
{
    private const string ApiUrl = "https://ffxivcollect.com/api/mounts";
    private const string CacheFileName = "ffxiv-collect-mounts.json";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private static Dictionary<uint, MountCollectInfo> byMountId = new();

    public static MountCollectInfo? Get(uint mountId) => byMountId.TryGetValue(mountId, out var info) ? info : null;

    /// <summary>
    /// Loads the last successful cache, then refreshes from the API once.
    /// On fetch failure, keeps the previously loaded cache and logs to the plugin log only.
    /// </summary>
    public static async Task InitializeAsync()
    {
        LoadCacheFromDisk();

        try
        {
            var fetched = await FetchFromApiAsync().ConfigureAwait(false);
            byMountId = fetched;
            SaveCacheToDisk(fetched);
            Plugin.Log.Information("Loaded mount Own%/Patch data from FFXIV Collect ({Count} mounts).", fetched.Count);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(
                ex,
                "Failed to retrieve mount Own%/Patch data from FFXIV Collect; using cached data ({Count} mounts).",
                byMountId.Count
            );
        }
    }

    private static async Task<Dictionary<uint, MountCollectInfo>> FetchFromApiAsync()
    {
        await using var stream = await HttpClient.GetStreamAsync(ApiUrl).ConfigureAwait(false);
        var response = await JsonSerializer.DeserializeAsync<CollectMountsResponse>(stream, JsonOptions)
            .ConfigureAwait(false);

        if (response?.Results is not { Count: > 0 })
        {
            throw new InvalidOperationException("FFXIV Collect mounts response was empty or invalid.");
        }

        var result = new Dictionary<uint, MountCollectInfo>(response.Results.Count);
        foreach (var mount in response.Results)
        {
            result[mount.Id] = MountCollectInfo.FromApi(mount.Owned, mount.Patch);
        }

        return result;
    }

    private static void LoadCacheFromDisk()
    {
        try
        {
            var path = GetCachePath();
            if (!File.Exists(path))
            {
                return;
            }

            var json = File.ReadAllText(path);
            var cache = JsonSerializer.Deserialize<CollectMountsCache>(json, JsonOptions);
            if (cache?.Mounts is not { Count: > 0 })
            {
                return;
            }

            byMountId = new Dictionary<uint, MountCollectInfo>(cache.Mounts.Count);
            foreach (var entry in cache.Mounts)
            {
                byMountId[entry.Id] = MountCollectInfo.FromApi(entry.Owned, entry.Patch);
            }

            Plugin.Log.Debug("Loaded cached FFXIV Collect mount data ({Count} mounts).", byMountId.Count);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to load cached FFXIV Collect mount data.");
            byMountId = new Dictionary<uint, MountCollectInfo>();
        }
    }

    private static void SaveCacheToDisk(Dictionary<uint, MountCollectInfo> mounts)
    {
        try
        {
            var cache = new CollectMountsCache
            {
                Mounts = new List<CachedMountEntry>(mounts.Count),
            };

            foreach (var (id, info) in mounts)
            {
                cache.Mounts.Add(
                    new CachedMountEntry
                    {
                        Id = id,
                        Owned = info.OwnedDisplay,
                        Patch = info.Patch,
                    }
                );
            }

            var path = GetCachePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(cache, JsonOptions));
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to save FFXIV Collect mount data cache.");
        }
    }

    private static string GetCachePath() => Path.Combine(
        Plugin.PluginInterface.GetPluginConfigDirectory(),
        CacheFileName
    );

    private sealed class CollectMountsResponse
    {
        [JsonPropertyName("results")]
        public List<CollectMountDto>? Results { get; set; }
    }

    private sealed class CollectMountDto
    {
        [JsonPropertyName("id")]
        public uint Id { get; set; }

        [JsonPropertyName("owned")]
        public string? Owned { get; set; }

        [JsonPropertyName("patch")]
        public string? Patch { get; set; }
    }

    private sealed class CollectMountsCache
    {
        [JsonPropertyName("mounts")]
        public List<CachedMountEntry>? Mounts { get; set; }
    }

    private sealed class CachedMountEntry
    {
        [JsonPropertyName("id")]
        public uint Id { get; set; }

        [JsonPropertyName("owned")]
        public string? Owned { get; set; }

        [JsonPropertyName("patch")]
        public string? Patch { get; set; }
    }
}

public sealed record MountCollectInfo(string OwnedDisplay, float? OwnedPercent, string? Patch)
{
    public static MountCollectInfo FromApi(string? owned, string? patch)
    {
        var display = string.IsNullOrWhiteSpace(owned) ? "—" : owned.Trim();
        float? percent = null;

        if (!string.IsNullOrWhiteSpace(owned))
        {
            var numeric = owned.Trim().TrimEnd('%').Trim();
            if (float.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                percent = parsed;
            }
        }

        return new MountCollectInfo(
            display,
            percent,
            string.IsNullOrWhiteSpace(patch) ? null : patch.Trim()
        );
    }

    public static int ComparePatch(string? left, string? right)
    {
        if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
        {
            return 0;
        }

        if (string.IsNullOrEmpty(left))
        {
            return 1;
        }

        if (string.IsNullOrEmpty(right))
        {
            return -1;
        }

        var leftParts = left.Split('.');
        var rightParts = right.Split('.');
        var max = Math.Max(leftParts.Length, rightParts.Length);

        for (var i = 0; i < max; i++)
        {
            var leftValue = i < leftParts.Length
                && int.TryParse(leftParts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                    ? l
                    : 0;
            var rightValue = i < rightParts.Length
                && int.TryParse(rightParts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r)
                    ? r
                    : 0;

            var comparison = leftValue.CompareTo(rightValue);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
