using economy.Components;

var market = new economy.Models.Market();

for (int i = 0; i < 500; i++)
{
    market.Step();
    //Console.ReadLine();
    Console.Write(market.Prices["iron"]);
    Console.Write(", " + market.Volume["iron"]);
    Console.Write(", " + market.Prices["crafter"]);
    Console.Write(", " + market.Volume["crafter"]);
    Console.WriteLine();
}


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
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
    .AddInteractiveServerRenderMode();



app.Run();

