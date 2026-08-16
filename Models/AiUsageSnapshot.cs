using System;

namespace aidaAlternative.Models
{
    public sealed record AiUsageSnapshot(
        float? ClaudeFiveHourUsedPercent,
        DateTime? ClaudeFiveHourResetsAt,
        float? ClaudeSevenDayUsedPercent,
        DateTime? ClaudeSevenDayResetsAt,
        string? ScopedWeeklyLabel = null,
        float? ScopedWeeklyUsedPercent = null,
        DateTime? ScopedWeeklyResetsAt = null);
}
