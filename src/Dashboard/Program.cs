using Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure SearchService HttpClient
var searchServiceBaseUrl = builder.Configuration["SearchService:BaseUrl"]
    ?? throw new InvalidOperationException("SearchService:BaseUrl is not configured");

builder.Services.AddHttpClient<ISearchServiceClient, SearchServiceClient>(client =>
{
    client.BaseAddress = new Uri(searchServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Redirect root to Search page
app.MapGet("/", () => Results.Redirect("/Search"));

app.Run();
