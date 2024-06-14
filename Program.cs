using SolaceWebClient.Components;
using SolaceWebClient.Services;
using Blazored.Toast;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<SolacePublishService>();
builder.Services.AddSingleton<SolaceSubscribeService>();

builder.Services.AddSingleton<QueueBrowserService>();

builder.Services.AddBlazoredToast();
builder.Services.AddHttpClient<SEMPService>();
builder.Services.AddScoped<SEMPService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
