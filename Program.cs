
using AgriStreamMVC.Data;
using Microsoft.EntityFrameworkCore;
using Azure.Data.Tables;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var connectionString =
        configuration["AgriStreamStorage"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "AgriStreamStorage is not configured.");
    }

    return new TableClient(
        connectionString,
        "SensorTelemetry");
});

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AgriStreamDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
