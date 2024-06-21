using Microsoft.OpenApi.Models;
using Progressive.WAM.Webguard.Client.Core;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// add configuration
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json");

var confBuilder = new ConfigurationBuilder().AddConfiguration(builder.Configuration);
var finalConfig = confBuilder.Build() as IConfiguration;

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DuckCreekManuscriptBrokerAPI", Version = "v1" });
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

builder.Logging.AddJsonConsole();

// ----------------------------------------------------------------
// legacy Webguard URL - THIS WORKS w/ the legacy URL!
// ----------------------------------------------------------------
//builder.Services.AddTransient<FactoryAuthorizationHandler>(service => new FactoryAuthorizationHandler(finalConfig["WebguardClientAuthenticationURLLegacy"]));
//builder.Services.AddHttpClient("BrokerHttpClient").AddHttpMessageHandler<FactoryAuthorizationHandler>();

// -----------------------------------------------------------------
// token.oauth2 URL - JWT
// -----------------------------------------------------------------
//builder.Services.AddTransient<JwtFactoryAuthorizationHandler>(service => new JwtFactoryAuthorizationHandler(finalConfig["WebguardClientAuthenticationURL"], null, null));
//builder.Services.AddHttpClient("BrokerHttpClient").AddHttpMessageHandler<JwtFactoryAuthorizationHandler>();

// -----------------------------------------------------------------
// token.oauth2 URL - Kerberos
// -----------------------------------------------------------------
string url = finalConfig["WebguardClientAuthenticationURL"] ?? "";
builder.Services.AddTransient<AuthorizationHandlerBase>(service => new KerberosJwtFactoryAuthorizationHandler(url));
builder.Services.AddHttpClient("BrokerHttpClient", client =>
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddHttpMessageHandler<AuthorizationHandlerBase>();
// end kerberos

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();