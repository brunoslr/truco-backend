var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Truco Mineiro API",
        Version = "v1",
        Description = "A REST API for the Truco Mineiro card game",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Truco Mineiro Team",
            Email = "support@trucomineiro.com"
        }
    });
    
    // Include XML comments in Swagger documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

// Register domain services
builder.Services.AddSingleton<TrucoMineiro.API.Domain.Interfaces.IGameRepository, TrucoMineiro.API.Domain.Repositories.InMemoryGameRepository>();
builder.Services.AddScoped<TrucoMineiro.API.Domain.Interfaces.IHandResolutionService, TrucoMineiro.API.Domain.Services.HandResolutionService>();
builder.Services.AddScoped<TrucoMineiro.API.Domain.Interfaces.ITrucoRulesEngine, TrucoMineiro.API.Domain.Services.TrucoRulesEngine>();
builder.Services.AddScoped<TrucoMineiro.API.Domain.Interfaces.IScoreCalculationService, TrucoMineiro.API.Domain.Services.ScoreCalculationService>();
builder.Services.AddScoped<TrucoMineiro.API.Domain.Interfaces.IAIPlayerService, TrucoMineiro.API.Domain.Services.AIPlayerService>();
builder.Services.AddScoped<TrucoMineiro.API.Domain.Interfaces.IGameStateManager, TrucoMineiro.API.Domain.Services.GameStateManager>();
builder.Services.AddScoped<TrucoMineiro.API.Domain.Interfaces.IGameFlowService, TrucoMineiro.API.Domain.Services.GameFlowService>();
builder.Services.AddScoped<TrucoMineiro.API.Domain.Interfaces.IGameFlowReactionService, TrucoMineiro.API.Domain.Services.GameFlowReactionService>();

// Register application services
builder.Services.AddScoped<TrucoMineiro.API.Services.GameService>();
builder.Services.AddSingleton<TrucoMineiro.API.Services.MappingService>();

// Register event system
builder.Services.AddSingleton<TrucoMineiro.API.Domain.Events.IEventPublisher, TrucoMineiro.API.Domain.Services.InMemoryEventPublisher>();

// Register background services
builder.Services.AddHostedService<TrucoMineiro.API.Services.GameCleanupService>();

// Add CORS for the React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "https://localhost:5173", "https://localhost:3000") // React app URLs
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Allow credentials if needed
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Truco Mineiro API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Apply CORS before other middleware
app.UseCors("AllowReactApp");

// Only redirect to HTTPS in production or when not dealing with CORS preflight
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
