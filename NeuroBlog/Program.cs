var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<UserState>();

// HttpClient pointed at this same origin (the server hosts both API and WASM),
// with a handler that stamps every request with the current username.
builder.Services.AddScoped(sp =>
{
    var user = sp.GetRequiredService<UserState>();
    var handler = new UsernameHeaderHandler(user) { InnerHandler = new HttpClientHandler() };
    return new HttpClient(handler) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});

builder.Services.AddScoped<BlogApi>();

await builder.Build().RunAsync();
