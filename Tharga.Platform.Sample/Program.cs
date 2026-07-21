using Radzen;
using Tharga.Mcp;
using Tharga.MongoDB;
using Tharga.Platform.Mcp;
using Tharga.Platform.Sample.Components;
using Tharga.Platform.Sample.Framework;
using Tharga.Platform.Sample.Framework.Team;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;
using Tharga.Team.MongoDB;
using Tharga.Team.Service.Audit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddRadzenCookieThemeService();

builder.AddThargaPlatform(o =>
{
    o.Blazor.Title = "Tharga Platform Sample";
    o.Blazor.RegisterTeamService<TeamService, UserService, TeamMember>();
    o.Blazor.AutoCreateFirstTeam = false;
    o.Blazor.AllowTeamCreation = true;
    o.Blazor.AddClaimsEnricher<DeveloperRoleEnricher>();
    o.Blazor.Consent.ShowToggle = true;

    // Cross-team visibility: grant the consent roles the teams:read system scope, so a Developer sees
    // every team (not just their own) with a badge showing what each has consented to. Discovery only —
    // access inside a team still depends on that team's consent.
    o.Blazor.Consent.Roles = ["Developer"];
    o.Blazor.Consent.GrantTeamsRead = true;

    // Demo: localize the team menu strings (here to Swedish). A real app would bridge to its content system.
    o.Blazor.AddTextProvider<SampleMenuTextProvider>();

    // Advanced mode unlocks the full API key UI (access level, roles, scope overrides, tags).
    o.ApiKey.AdvancedMode = true;

    // Register scopes so the scope-override picker and Custom (no-base-scope) keys have something to grant.
    o.ConfigureScopes = scopes =>
    {
        // A spread across access levels so option-(b) pickers show a mix of inherited (disabled) and addable
        // scopes. Remember: Owner/Administrator inherit ALL scopes — to see addable ones, test against a
        // lower-level member or an AccessLevel.Custom key.
        scopes.Register("orders:read", AccessLevel.Viewer, "View orders and order details.");
        scopes.Register("orders:write", AccessLevel.User, "Create and edit orders.");
        scopes.Register("orders:refund", AccessLevel.Administrator, "Issue refunds on orders.");
        scopes.Register("valuegroup:read", AccessLevel.Viewer, "Read value groups.");
        scopes.Register("content:load", AccessLevel.Viewer, "Load published content.");
        scopes.Register("content:publish", AccessLevel.User, "Publish content to live.");
        scopes.Register("pim:manage", AccessLevel.Administrator, "Manage the product information catalog.");
        scopes.Register("firewall:open", AccessLevel.Administrator); // no description — shows no tooltip
        scopes.Register("reports:export", AccessLevel.User, "Export reports to file.");
        scopes.Register("billing:manage", AccessLevel.Administrator, "Manage billing and invoices.");
    };

    // Demo tenant roles (bundles of scopes). Assign these to members; their scopes resolve live now that
    // the role->scope linkage is fixed.
    o.ConfigureTenantRoles = roles =>
    {
        roles.Register("Editor", ["orders:write", "content:publish", "reports:export"], "Content editors — manage orders and publish content.");
        roles.Register("Support", ["orders:read", "valuegroup:read"]); // no description — tooltip shows scopes only
    };

    // Demo: let team admins define their own custom roles at runtime (see the /roles page → TenantRoleManager).
    o.EnableDynamicRoles = true;

    // Demo system scopes (global capabilities for system API keys; separate from team scopes).
    o.ConfigureSystemScopes = scopes =>
    {
        scopes.Register(SystemTeamScopes.Read, "See every team (cross-team discovery).");
        scopes.Register("system:metrics:read", "Read infrastructure metrics.");
        scopes.Register("mcp:discover", "Discover MCP tools and resources.");
    };

    // Map app/global roles to system scopes — a Developer user gains these as claims (team-independent).
    // Note teams:read is NOT listed here: Consent.GrantTeamsRead adds it on top of this mapping, which is
    // the composition case (Map would throw on an already-mapped role; the toolkit-side grant merges).
    o.ConfigureSystemRoles = roles =>
    {
        roles.Map("Developer", "system:metrics:read", "mcp:discover", "apikey:manage", "audit:read");
    };

    // Logger | MongoDB so the audit entries are both logged and queryable by AuditLogView — the default
    // is Logger-only, which leaves the /audit page empty.
    o.Audit = new AuditOptions { StorageMode = AuditStorageMode.Logger | AuditStorageMode.MongoDB };
});

// Demo: attach host-defined metadata to every audit entry (visible in the /audit detail row).
builder.Services.AddThargaAuditEnricher<SampleAuditEnricher>();

builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddPlatform();
});

builder.AddMongoDB();

builder.Services.AddScoped<AppUserAdminService>();

builder.Services.AddThargaTeamRepository(o =>
{
    o.RegisterUserRepository<UserEntity>();
    o.RegisterTeamRepository<TeamEntity, TeamMember>();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseThargaPlatform();
app.UseThargaMcp();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
