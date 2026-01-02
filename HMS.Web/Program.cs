using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HMS.Web.Client.Pages;
using HMS.Web.Components;
using HMS.Web.Components.Account;
using HMS.Web.Data;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Ensure roles are included in the claims
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// DAL Services
builder.Services.AddTransient<HMS.Web.DAL.DatabaseHelper>();
builder.Services.AddScoped<HMS.Web.DAL.PatientRepository>();
builder.Services.AddScoped<HMS.Web.DAL.DoctorRepository>();
builder.Services.AddScoped<HMS.Web.DAL.DoctorShiftRepository>();
builder.Services.AddScoped<HMS.Web.DAL.AppointmentRepository>();
builder.Services.AddScoped<HMS.Web.DAL.ReportRepository>();
builder.Services.AddScoped<HMS.Web.DAL.PrescriptionRepository>();
builder.Services.AddScoped<HMS.Web.DAL.BillingRepository>();
builder.Services.AddScoped<HMS.Web.DAL.OperationRepository>();
builder.Services.AddScoped<HMS.Web.DAL.SupportRepository>();
builder.Services.AddScoped<HMS.Web.DAL.NotificationRepository>();
builder.Services.AddScoped<HMS.Web.DAL.StaffRepository>();
builder.Services.AddScoped<HMS.Web.DAL.FacilityRepository>();
builder.Services.AddScoped<HMS.Web.DAL.FinanceRepository>();

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HMS.Web.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Initialize Database
HMS.Web.Data.DbInitializer.Initialize(app.Services);

// Seed User Accounts (Run once to create test accounts)
// Comment out after first run to avoid duplicate attempts
using (var scope = app.Services.CreateScope())
{
    try
    {
        await HMS.Web.SeedUsers.UserAccountSeeder.SeedUserAccounts(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"User seeding skipped or failed: {ex.Message}");
    }
}

app.Run();
