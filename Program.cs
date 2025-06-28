using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using NewKnowledgeAPI.Services;
using Knowledge.Services;
using NewKnowledgeAPI.Q.Questions;
using NewKnowledgeAPI.Q.Categories;
using NewKnowledgeAPI.A.Answers;
using NewKnowledgeAPI.A.Groups;
using NewKnowledgeAPI.History;
using NewKnowledgeAPI.HistoryFilter;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; // TODO ubaci u controller

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Add services to the container.
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(options =>
            {
                builder.Configuration.Bind("AzureAd", options);
                options.Events = new JwtBearerEvents();

                /// <summary>
                /// Below you can do extended token validation and check for additional claims, such as:
                ///
                /// - check if the caller's tenant is in the allowed tenants list via the 'tid' claim (for multi-tenant applications)
                /// - check if the caller's account is homed or guest via the 'acct' optional claim
                /// - check if the caller belongs to right roles or groups via the 'roles' or 'groups' claim, respectively
                ///
                /// Bear in mind that you can do any of the above checks within the individual routes and/or controllers as well.
                /// For more information, visit: https://docs.microsoft.com/azure/active-directory/develop/access-tokens#validate-the-user-has-permission-to-access-this-data
                /// </summary>

                //options.Events.OnTokenValidated = async context =>
                //{
                //    string[] allowedClientApps = { /* list of client ids to allow */ };

                //    string clientappId = context?.Principal?.Claims
                //        .FirstOrDefault(x => x.Type == "azp" || x.Type == "appid")?.Value;

                //    if (!allowedClientApps.Contains(clientappId))
                //    {
                //        throw new System.Exception("This client is not authorized");
                //    }
                //};
            }, options => { builder.Configuration.Bind("AzureAd", options); });


//builder.Services.AddCors(options =>
//{
//    options.AddPolicy(name: MyAllowSpecificOrigins,
//                      policy =>
//                      {
//                          policy.WithOrigins("https://slavkopar.github.io/knowledge-cosmos")
//                            .AllowAnyHeader()
//                            .AllowAnyMethod();
//                      });
//});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddCors(o => o.AddPolicy("default", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
}));


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureSwaggerGen(setup =>
{
    setup.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Weather Forecasts",
        Version = "v1"
    });
});

/*
builder.Services.AddIdentity<IdentityUser, IdentityRole>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = GoogleDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
        .AddGoogle(options =>
        {
            options.ClientId = "[MyGoogleClientId]";
            options.ClientSecret = "[MyGoogleSecretKey]";
        });
*/


//builder.Services.AddAuthorization(config =>
//{
//    config.AddPolicy("AuthZPolicy", policy =>
//        policy.RequireRole("Knowledge.Read"));
//});

builder.Services.AddMemoryCache();

builder.Services.AddResponseCaching();

// Register database service
builder.Services.AddSingleton<DbService>();

// Register AI and search services
builder.Services.AddSingleton<IOpenAIEmbeddingService, OpenAIEmbeddingService>();
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();
builder.Services.AddScoped<ISearchEnhancementService, SearchEnhancementService>();

// Register background service for automatic embedding generation
builder.Services.AddEmbeddingBackgroundService();

// Register domain services
builder.Services.AddScoped<QuestionService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<AnswerService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<HistoryService>();
builder.Services.AddScoped<HistoryFilterService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
    IdentityModelEventSource.ShowPII = true;
}
else
{
    app.UseHsts();
}


app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("default"); // UseCors must be called before UseResponseCaching 

app.UseResponseCaching();

app.UseAuthentication();
//app.UseCors(MyAllowSpecificOrigins);
app.UseAuthorization();

var vectorSearchService = app.Services.GetRequiredService<IVectorSearchService>();
await vectorSearchService.InitializeAsync();

app.MapControllers();

app.Run();
