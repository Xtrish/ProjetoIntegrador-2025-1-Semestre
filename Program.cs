using Microsoft.EntityFrameworkCore;
using AtestadoMedico.Data;
using Microsoft.OpenApi.Models;
using System.IO;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AtestadoMedico API", Version = "v1" });
});

// Configuração dos bancos de dados
var usePostgres = true; // Definir como true para usar PostgreSQL, false para usar SQLite

// Configurar o Npgsql para usar UTC DateTime
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

if (usePostgres)
{
    // Usar PostgreSQL para persistência de dados
    var connectionString = "Host=localhost;Port=5432;Database=AtestadoMedicoDB;Username=postgres;Password=pi";
    Console.WriteLine($"String de conexão: {connectionString}");
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
    
    Console.WriteLine("Usando PostgreSQL como banco de dados");
}
else
{
    // Usar SQLite para persistência de dados (configuração anterior)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite("Data Source=AtestadoMedico.db"));
    
    Console.WriteLine("Usando SQLite como banco de dados");
}

// Adicionar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AtestadoMedico API v1"));
}

app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Redirecionar a rota raiz para a página inicial (index.html)
app.MapGet("/", context => {
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

// Criar usuários para teste
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    // Não aplicar migrações automaticamente por enquanto
    // context.Database.Migrate();
    
    try
    {
        // Garantir que o banco de dados existe
        context.Database.EnsureCreated();
        
        // Adicionar usuário administrador se não existir
        if (!context.Usuarios.Any(u => u.Email == "junior@gmail.com"))
        {
            context.Usuarios.Add(new AtestadoMedico.Models.Usuario
            {
                Nome = "Junior",
                Email = "junior@gmail.com",
                Senha = "junior@123",
                IsAdmin = true,
                DataCadastro = DateTime.SpecifyKind(new DateTime(2024, 3, 18, 10, 0, 0), DateTimeKind.Utc)
            });
            context.SaveChanges();
            Console.WriteLine("Usuário administrador criado: junior@gmail.com / junior@123");
        }
        
        // Adicionar usuário normal para teste
        if (!context.Usuarios.Any(u => u.Email == "usuario@teste.com"))
        {
            context.Usuarios.Add(new AtestadoMedico.Models.Usuario
            {
                Nome = "Usuário Teste",
                Email = "usuario@teste.com",
                Senha = "123456",
                IsAdmin = false,
                DataCadastro = DateTime.SpecifyKind(new DateTime(2024, 3, 18, 10, 0, 0), DateTimeKind.Utc)
            });
            context.SaveChanges();
            Console.WriteLine("Usuário de teste criado: usuario@teste.com / 123456");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao inicializar banco de dados: {ex.Message}");
    }
}

app.Run(); 






