using Microsoft.EntityFrameworkCore;
using Topder.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<TopderContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"))
);

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 404)
    {
        context.Request.Path = "/NotFound";
        await next();
    }

    if (context.Response.StatusCode == 400)
    {
        context.Request.Path = "/BadRequest";
        await next();
    }
});

app.MapGet("/NotFound", () =>
{
    var content = File.ReadAllText("Views/NotFound.cshtml");
    return Results.Content(content, "text/html");
});


app.MapGet("/BadRequest", () =>
{
    var content = File.ReadAllText("Views/BadRequest.cshtml");
    return Results.Content(content, "text/html");
});


app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.Run();
