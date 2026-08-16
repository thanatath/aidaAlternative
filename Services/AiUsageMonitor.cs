using aidaAlternative.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace aidaAlternative.Services
{
    public sealed class AiUsageMonitor
    {
        // ponytail: /api/oauth/usage rate-limits hard — polling it every 60s next to a running Claude Code
        // gets a permanent 429 and the panel stays empty. 5 min is under the limit; retry sooner only on failure.
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(60);
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private AiUsageSnapshot snapshot = new(null, null, null, null);
        private DateTime nextRefreshUtc = DateTime.MinValue;

        public AiUsageSnapshot Snapshot => snapshot;

        public void RefreshIfDue()
        {
            if (DateTime.UtcNow < nextRefreshUtc) return;

            nextRefreshUtc = DateTime.UtcNow.Add(RefreshInterval);
            _ = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            try
            {
                if (ReadAccessToken() is not string token) { Retry("no accessToken in ~/.claude/.credentials.json"); return; }

                using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/api/oauth/usage");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("anthropic-beta", "oauth-2025-04-20");
                using var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode) { Retry($"HTTP {(int)response.StatusCode}"); return; }

                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var fiveHour = GetObject(document.RootElement, "five_hour");
                var sevenDay = GetObject(document.RootElement, "seven_day");
                var scoped = FindScopedWeekly(document.RootElement);
                snapshot = new AiUsageSnapshot(
                    GetFloat(fiveHour, "utilization"),
                    GetTime(fiveHour, "resets_at"),
                    GetFloat(sevenDay, "utilization"),
                    GetTime(sevenDay, "resets_at"),
                    GetString(GetObject(GetObject(scoped, "scope"), "model"), "display_name"),
                    GetFloat(scoped, "percent"),
                    GetTime(scoped, "resets_at"));
            }
            catch (Exception ex)
            {
                // ponytail: offline or expired token — keep showing the last snapshot
                Retry(ex.Message);
            }
        }

        private void Retry(string reason)
        {
            // ponytail: WinExe has no console — append next to the exe. Swap for a real logger if anything else needs one.
            try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "aiusage.log"), $"{DateTime.Now:s} refresh failed: {reason}{Environment.NewLine}"); } catch { }
            nextRefreshUtc = DateTime.UtcNow.Add(RetryInterval);
        }

        private static string? ReadAccessToken()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", ".credentials.json");
            if (!File.Exists(path)) return null;

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return GetString(GetObject(document.RootElement, "claudeAiOauth"), "accessToken");
        }

        // ponytail: first weekly_scoped entry only — Claude lists per-model weekly caps (Fable, Opus…) here,
        // the flat seven_day_* fields are null. Add a loop if two scoped caps ever need showing at once.
        private static JsonElement FindScopedWeekly(JsonElement root)
        {
            if (root.TryGetProperty("limits", out var limits) && limits.ValueKind == JsonValueKind.Array)
                foreach (var limit in limits.EnumerateArray())
                    if (GetString(limit, "kind") == "weekly_scoped") return limit;
            return default;
        }

        private static JsonElement GetObject(JsonElement parent, string name) => parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) ? value : default;

        private static string? GetString(JsonElement entry, string name) => entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static float? GetFloat(JsonElement entry, string name) => entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty(name, out var value) && value.TryGetSingle(out var number) ? number : null;

        private static DateTime? GetTime(JsonElement entry, string name) => DateTimeOffset.TryParse(GetString(entry, name), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var time) ? time.UtcDateTime : null;
    }
}
