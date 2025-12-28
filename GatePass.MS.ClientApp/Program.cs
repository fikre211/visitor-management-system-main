using GatePass.MS.Application;
using GatePass.MS.ClientApp.Controllers;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.ClientApp.Middleware;
using GatePass.MS.ClientApp.Service;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddHttpClient<SmsService>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// Replace your existing RazorPages registration:
builder.Services
    .AddRazorPages()
    .AddRazorRuntimeCompilation()
    .AddRazorPagesOptions(options =>
    {
        // Login
        options.Conventions.AddAreaPageRoute(
            areaName: "Identity",
            pageName: "/Account/Login",
            route: "{companyName}/Identity/Account/Login");

        // Logout (so the form posts to /{companyName}/Identity/Account/Logout)
        options.Conventions.AddAreaPageRoute(
            areaName: "Identity",
            pageName: "/Account/Logout",
            route: "{companyName}/Identity/Account/Logout");

        options.Conventions.AddAreaPageRoute(
                areaName: "Identity",
                pageName: "/Account/Manage/Index",
                route: "{companyName}/Identity/Account/Manage");

        options.Conventions.AddAreaPageRoute(
                areaName: "Identity",
                pageName: "/Account/Register",
                route: "{companyName}/Identity/Account/Register");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/RegisterConfirmation",
               route: "{companyName}/Identity/Account/RegisterConfirmation");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/ResendEmailConfirmation",
               route: "{companyName}/Identity/Account/ResendEmailConfirmation");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/ResetPassword",
               route: "{companyName}/Identity/Account/ResetPassword");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/ResetPasswordConfirmation",
               route: "{companyName}/Identity/Account/ResetPasswordConfirmation");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/Manage",
               route: "{companyName}/Identity/Account/ResetPassword");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/ForgotPassword",
               route: "{companyName}/Identity/Account/ForgotPassword");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/AccessDenied",
               route: "{companyName}/Identity/Account/AccessDenied");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/ConfirmEmail",
               route: "{companyName}/Identity/Account/ConfirmEmail");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/ConfirmEmailChange",
               route: "{companyName}/Identity/Account/ConfirmEmailChange");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/ExternalLogin",
               route: "{companyName}/Identity/Account/ExternalLogin");

        options.Conventions.AddAreaPageRoute(
               areaName: "Identity",
               pageName: "/Account/ForgotPasswordConfirmation",
               route: "{companyName}/Identity/Account/ForgotPasswordConfirmation");

        options.Conventions.AddAreaPageRoute(
              areaName: "Identity",
              pageName: "/Account/Lockout",
              route: "{companyName}/Identity/Account/Lockout");

        options.Conventions.AddAreaPageRoute(
              areaName: "Identity",
              pageName: "/Account/RegisterConfirmation",
              route: "{companyName}/Identity/Account/RegisterConfirmation");

        options.Conventions.AddAreaPageRoute(
              areaName: "Identity",
              pageName: "/Account/Manage/ChangePassword",
              route: "{companyName}/Identity/Account/Manage/ChangePassword");
    });


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(365);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(1); // Set inactivity timeout
    options.SlidingExpiration = true; // Reset the timer on user activity
    options.Cookie.HttpOnly = true; // Ensure the cookie is HTTP only for security
});
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<GatePass.MS.ClientApp.Controllers.IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<DbInitializer>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICurrentCompany, CurrentCompany>();

builder.Services.AddScoped<UserActivityService>();
builder.Services.AddScoped<GuestActivityService>();

builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

var emailConfig = configuration.GetSection("EmailSettings").Get<EmailSettings>();
builder.Services.AddSingleton(emailConfig);
builder.Services.Configure<SmsSettings>(configuration.GetSection("SmsSettings"));



// Localization configuration
var supportedCultures = new[]
{
    new CultureInfo("en-US"),
   new CultureInfo("am-ET"),
    new CultureInfo("om-ET")

};

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.SeedAsync();

    var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
    var maxFailedAccessAttemptsString = await settingService.GetSettingValueAsync("MaxFailedAccessAttempts");
    if (!int.TryParse(maxFailedAccessAttemptsString, out int maxFailedAccessAttempts))
    {
        throw new InvalidOperationException("Invalid value for MaxFailedAccessAttempts");
    }

    var identityOptions = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;
    identityOptions.Lockout.MaxFailedAccessAttempts = maxFailedAccessAttempts;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();
app.UseNoCacheHeaders();
app.UseAuthentication();
app.UseAuthorization();
//Register RequestLocalizationOptions
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);
// <-- add this line -->
app.UseMiddleware<GatePass.MS.ClientApp.Middleware.CompanyResolutionMiddleware>();
app.MapControllerRoute(
    name: "default_with_company_name",
    pattern: "{companyName}/{controller=Home}/{action=Index}/{id?}");
// instead of just app.MapRazorPages();
app.MapRazorPages();
app.Run();
