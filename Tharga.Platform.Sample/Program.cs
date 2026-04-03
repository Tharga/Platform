using Radzen;
using Tharga.MongoDB;
using Tharga.Platform.Sample.Components;
using Tharga.Platform.Sample.Framework.Team;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;
using Tharga.Team.MongoDB;
using Tharga.Team.Service.Audit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

builder.AddThargaPlatform(o =>
{
    o.Blazor.Title = "Tharga Platform Sample";
    o.Blazor.RegisterTeamService<TeamService, UserService, TeamMember>();
    o.Blazor.AutoCreateFirstTeam = false;
    o.Blazor.AllowTeamCreation = true;

    //o.Blazor.ShowScopeOverrides = true;
    //o.Blazor.ShowMemberRoles = true;

    //o.ConfigureScopes = scopes =>
    //{
    //    scopes.Register("orders:read", AccessLevel.Viewer);
    //    scopes.Register("orders:write", AccessLevel.Administrator);
    //};

    //o.ConfigureTenantRoles = roles =>
    //{
    //    roles.Register("Editor", ["orders:read", "orders:write"]);
    //};

    o.Audit = new AuditOptions();
});

builder.AddMongoDB();

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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
