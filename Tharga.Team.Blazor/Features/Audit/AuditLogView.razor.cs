using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Features.Audit;

public partial class AuditLogView : ComponentBase
{
    [Inject] private IServiceProvider ServiceProvider { get; init; }
    [Inject] private ITeamService TeamService { get; init; }
    [Inject] private NotificationService NotificationService { get; init; }
    [Inject] private IJSRuntime JS { get; init; }

    [Parameter] public string TeamKey { get; set; }
    [Parameter] public AuditCallerType? RestrictCallerType { get; set; }

    private const int ChartQueryLimit = 5000;

    private CompositeAuditLogger _auditLogger;
    private bool _auditLoggerMissing;
    private bool? _mongoAvailable;
    private IReadOnlyList<AuditEntry> _entries = Array.Empty<AuditEntry>();
    private IReadOnlyList<AuditEntry> _chartEntries = Array.Empty<AuditEntry>();
    private int _totalCount;
    private int _pageSize = 8;
    private RadzenDataGrid<AuditEntry> _grid;
    private bool _initialLoadDone;

    // Caller name resolution
    internal Dictionary<string, string> _callerNameCache = new(StringComparer.OrdinalIgnoreCase);

    // Top-bar filters
    private string _datePeriod = "today";
    private IEnumerable<string> _filterTeams = Enumerable.Empty<string>();
    private IEnumerable<string> _filterSources = Enumerable.Empty<string>();
    private IEnumerable<string> _filterFeatures = Enumerable.Empty<string>();
    private IEnumerable<string> _filterActions = Enumerable.Empty<string>();
    private IEnumerable<AuditEventType> _filterEventTypes = Enumerable.Empty<AuditEventType>();
    private IEnumerable<bool> _filterSuccess = Enumerable.Empty<bool>();
    private string _timeGrouping = "hourly";

    // Dynamic filter options
    private List<TeamInfo> _teams = new();
    private List<string> _sources = new();
    private List<string> _features = new();
    private List<string> _actions = new();
    internal static readonly int[] PageSizeOptionsValues = [8, 16, 32, 64];
    internal static readonly AuditEventType[] EventTypeOptions = Enum.GetValues<AuditEventType>();

    protected override async Task OnInitializedAsync()
    {
        _auditLogger = ServiceProvider.GetService<CompositeAuditLogger>();
        if (_auditLogger == null)
        {
            _auditLoggerMissing = true;
            return;
        }

        try
        {
            await _auditLogger.QueryAsync(new AuditQuery { Take = 1 });
            _mongoAvailable = true;
        }
        catch
        {
            _mongoAvailable = false;
        }

        if (_mongoAvailable != true) return;

        if (string.IsNullOrEmpty(TeamKey))
        {
            await foreach (var team in TeamService.GetTeamsAsync())
            {
                _teams.Add(new TeamInfo(team.Key, team.Name));
            }
        }

        // Load distinct values for filter options
        var recentResult = await _auditLogger.QueryAsync(new AuditQuery
        {
            TeamKey = TeamKey,
            From = DateTime.UtcNow.AddDays(-30),
            Take = ChartQueryLimit
        });
        _features = recentResult.Items.Where(e => e.Feature != null).Select(e => e.Feature).Distinct().OrderBy(f => f).ToList();
        _actions = recentResult.Items.Where(e => e.Action != null).Select(e => e.Action).Distinct().OrderBy(a => a).ToList();
        _sources = recentResult.Items.Select(e => e.CallerSource.ToString()).Distinct().OrderBy(s => s).ToList();

        await BuildCallerNameCacheAsync();
    }

    private async Task BuildCallerNameCacheAsync()
    {
        var userService = ServiceProvider.GetService<IUserService>();
        if (userService != null)
        {
            await foreach (var user in userService.GetAsync())
            {
                if (!string.IsNullOrEmpty(user.Identity))
                    _callerNameCache.TryAdd(user.Identity, user.EMail ?? user.Identity);
                if (!string.IsNullOrEmpty(user.EMail))
                    _callerNameCache.TryAdd(user.EMail, user.EMail);
            }
        }
    }

    internal string GetCallerDisplayName(AuditEntry entry)
    {
        if (string.IsNullOrEmpty(entry.CallerIdentity)) return "";
        return _callerNameCache.TryGetValue(entry.CallerIdentity, out var name) ? name : entry.CallerIdentity;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_mongoAvailable == true && _grid != null && !_initialLoadDone)
        {
            _initialLoadDone = true;
            await _grid.Reload();
        }
    }

    private async Task OnFilterChanged()
    {
        if (_grid != null)
        {
            _grid.Reset();
            await _grid.FirstPage(true);
        }
    }

    private async Task OnLoadData(LoadDataArgs args)
    {
        try
        {
            var query = BuildQuery(args.Skip ?? 0, args.Top ?? _pageSize, args.OrderBy, args.Filters);
            var result = await _auditLogger.QueryAsync(query);
            _entries = result.Items;
            _totalCount = result.TotalCount;

            if (!_initialLoadDone)
            {
                _initialLoadDone = true;
                _chartEntries = Array.Empty<AuditEntry>();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Query failed", ex.Message);
        }
    }

    private async Task OnTabChange(int tabIndex)
    {
        if (tabIndex > 0 && !_chartEntries.Any())
        {
            await LoadChartDataAsync();
        }
    }

    private AuditQuery BuildQuery(int skip = 0, int take = 0, string orderBy = null, IEnumerable<FilterDescriptor> filters = null)
    {
        // Extract in-grid filter values
        string callerFilter = null;
        string methodFilter = null;
        if (filters != null)
        {
            foreach (var f in filters)
            {
                if (f.Property == nameof(AuditEntry.CallerIdentity) && f.FilterValue is string cv && !string.IsNullOrWhiteSpace(cv))
                    callerFilter = cv;
                else if (f.Property == nameof(AuditEntry.MethodName) && f.FilterValue is string mv && !string.IsNullOrWhiteSpace(mv))
                    methodFilter = mv;
            }
        }

        var (from, to) = GetDateRange();
        var teamKeys = _filterTeams?.ToArray();
        var features = _filterFeatures?.ToArray();
        var actions = _filterActions?.ToArray();
        var eventTypes = _filterEventTypes?.ToArray();
        var sources = _filterSources?.ToArray();

        // Map source strings to enum for CallerSource filter
        AuditCallerSource? callerSource = null;
        AuditCallerSource[] callerSources = null;
        if (sources is { Length: > 0 })
        {
            callerSources = sources
                .Select(s => Enum.TryParse<AuditCallerSource>(s, out var v) ? v : (AuditCallerSource?)null)
                .Where(v => v != null)
                .Select(v => v.Value)
                .ToArray();
            if (callerSources.Length == 1)
            {
                callerSource = callerSources[0];
                callerSources = null;
            }
            else if (callerSources.Length == 0)
            {
                callerSources = null;
            }
        }

        // Parse sort from Radzen's OrderBy string, e.g. "Timestamp desc"
        string sortField = null;
        var sortDesc = true;
        if (!string.IsNullOrEmpty(orderBy))
        {
            var parts = orderBy.Split(' ', 2);
            sortField = parts[0];
            sortDesc = parts.Length < 2 || parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        }

        // Success filter from top bar
        var successValues = _filterSuccess?.ToArray();
        bool? success = null;
        if (successValues is { Length: 1 })
            success = successValues[0];

        return new AuditQuery
        {
            TeamKey = TeamKey,
            TeamKeys = teamKeys is { Length: > 0 } ? teamKeys : null,
            Features = features is { Length: > 0 } ? features : null,
            Actions = actions is { Length: > 0 } ? actions : null,
            EventTypes = eventTypes is { Length: > 0 } ? eventTypes : null,
            CallerSource = callerSource,
            CallerType = RestrictCallerType,
            CallerIdentity = callerFilter,
            MethodName = methodFilter,
            Success = success,
            From = from,
            To = to,
            Skip = skip,
            Take = take > 0 ? take : _pageSize,
            SortField = sortField,
            SortDescending = sortDesc
        };
    }

    private (DateTime? from, DateTime? to) GetDateRange()
    {
        var now = DateTime.UtcNow;
        return _datePeriod switch
        {
            "today" => (DateTime.Today.ToUniversalTime(), null),
            "week" => (DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek).ToUniversalTime(), null),
            "month" => (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), null),
            _ => (null, null)
        };
    }

    private async Task LoadChartDataAsync()
    {
        try
        {
            var result = await _auditLogger.QueryAsync(BuildQuery(take: ChartQueryLimit));
            _chartEntries = result.Items;
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Chart data failed", ex.Message);
        }
    }

    // Chart helpers
    public class ChartItem { public string Label { get; set; } public int Count { get; set; } }
    public class ChartValue { public string Period { get; set; } public double Value { get; set; } }
    public class ChartCount { public string Period { get; set; } public int Count { get; set; } }

    private List<ChartCount> GetCallsOverTime()
    {
        var grouped = _timeGrouping == "hourly"
            ? _chartEntries.GroupBy(e => e.Timestamp.ToLocalTime().ToString("MM-dd HH:00"))
            : _chartEntries.GroupBy(e => e.Timestamp.ToLocalTime().ToString("yyyy-MM-dd"));
        return grouped.OrderBy(g => g.Key).Select(g => new ChartCount { Period = g.Key, Count = g.Count() }).ToList();
    }

    private List<ChartItem> GetSuccessFailure() => new()
    {
        new ChartItem { Label = "Success", Count = _chartEntries.Count(e => e.Success) },
        new ChartItem { Label = "Failure", Count = _chartEntries.Count(e => !e.Success) }
    };

    private List<ChartItem> GetByFeature() =>
        _chartEntries.Where(e => e.Feature != null).GroupBy(e => e.Feature)
            .Select(g => new ChartItem { Label = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(10).ToList();

    private List<ChartItem> GetTopCallers() =>
        _chartEntries.Where(e => e.CallerIdentity != null).GroupBy(e => GetCallerDisplayName(e))
            .Select(g => new ChartItem { Label = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(10).ToList();

    private List<ChartValue> GetResponseTimeOverTime()
    {
        var grouped = _timeGrouping == "hourly"
            ? _chartEntries.GroupBy(e => e.Timestamp.ToLocalTime().ToString("MM-dd HH:00"))
            : _chartEntries.GroupBy(e => e.Timestamp.ToLocalTime().ToString("yyyy-MM-dd"));
        return grouped.OrderBy(g => g.Key).Select(g => new ChartValue { Period = g.Key, Value = g.Average(e => e.DurationMs) }).ToList();
    }

    private List<ChartValue> GetResponseTimeByFeature() =>
        _chartEntries.Where(e => e.Feature != null).GroupBy(e => e.Feature)
            .Select(g => new ChartValue { Period = g.Key, Value = g.Average(e => e.DurationMs) }).OrderByDescending(x => x.Value).Take(10).ToList();

    private List<AuditEntry> GetSlowest() =>
        _chartEntries.OrderByDescending(e => e.DurationMs).Take(10).ToList();

    private async Task ExportAsync(string format)
    {
        try
        {
            var result = await _auditLogger.QueryAsync(BuildQuery(take: 100_000));
            var exportEntries = result.Items;

            if (!exportEntries.Any())
            {
                NotificationService.Notify(NotificationSeverity.Warning, "No data to export");
                return;
            }

            string content;
            string mimeType;
            string fileName;

            if (format == "json")
            {
                content = System.Text.Json.JsonSerializer.Serialize(exportEntries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                mimeType = "application/json";
                fileName = $"audit-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                var includeTeam = string.IsNullOrEmpty(TeamKey);
                sb.AppendLine(includeTeam
                    ? "Timestamp,Team,Caller,CallerID,Source,Feature,Action,Method,Duration,Success,EventType,Scope,ScopeResult,ErrorMessage"
                    : "Timestamp,Caller,CallerID,Source,Feature,Action,Method,Duration,Success,EventType,Scope,ScopeResult,ErrorMessage");
                foreach (var e in exportEntries)
                {
                    var team = includeTeam ? $"{Escape(e.TeamKey)}," : "";
                    var callerName = Escape(GetCallerDisplayName(e));
                    sb.AppendLine($"{e.Timestamp:O},{team}{callerName},{Escape(e.CallerIdentity)},{e.CallerSource},{Escape(e.Feature)},{Escape(e.Action)},{Escape(e.MethodName)},{e.DurationMs},{e.Success},{e.EventType},{Escape(e.ScopeChecked)},{e.ScopeResult},{Escape(e.ErrorMessage)}");
                }
                content = sb.ToString();
                mimeType = "text/csv";
                fileName = $"audit-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var base64 = Convert.ToBase64String(bytes);
            await JS.InvokeVoidAsync("eval", $"{{const a=document.createElement('a');a.href='data:{mimeType};base64,{base64}';a.download='{fileName}';a.click();}}");
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Export failed", ex.Message);
        }
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private string GetTeamName(string teamKey)
    {
        if (string.IsNullOrEmpty(teamKey)) return "";
        var team = _teams.Find(t => t.Key == teamKey);
        return team?.Name ?? teamKey;
    }

    private class TeamInfo
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public TeamInfo(string key, string name) { Key = key; Name = name; }
    }
}
