using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Toast;
using BlazorSpinner;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Portal.LUNA.App;
using Portal.LUNA.App.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AuthorizationCookieHandler>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, AuthenticationManager>();
builder.Services.AddScoped(sp => (AuthenticationManager)sp.GetRequiredService<AuthenticationStateProvider>());

builder.Services.AddHttpClient("API", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<AuthorizationCookieHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));
builder.Services.AddScoped<API>();
builder.Services.AddScoped<SpinnerService>();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();
builder.Services.AddBlazoredModal();

await builder.Build().RunAsync();
