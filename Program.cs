using Blog;
using Blog.Data;
using Blog.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
ConfigureAuthentication(builder);
ConfigureMvc(builder);
ConfigureServices(builder);

//Documentar a API
builder.Services.AddEndpointsApiExplorer(); //adiciona o swagger - APIs minimas
builder.Services.AddSwaggerGen(); //gera o código da interface (HTML) do swagger

var app = builder.Build();
LoadConfiguration(app);

//colocar pela ordem q se pensa q a aplicação vai ser executada
app.UseHttpsRedirection(); //tag q permite fazer redirecionamentos http -> https
app.UseAuthentication(); //1º - quem sou
app.UseAuthorization(); //2º - o que posso fazer
app.MapControllers();
app.UseStaticFiles(); //para o servidor conseguir renderizar arquivos estáticos: imagens, js, css, html. Apenas a título didático. Não usar arquivos estáticos dentro da API. Fazer antes por exemplo upload de imagem para o Azure.
app.UseResponseCompression(); //A parte de compressão só deve funcionar em produção. Em Debug até coloca mais 1 nico por causa de alterar a informação do Header (PostMan). 

if (app.Environment.IsDevelopment())
{
    //Documentar a API
    app.UseSwagger(); //por padrão não se coloca em produção, mas tb não tem problema. Depende da necessidade q temos.
    app.UseSwaggerUI();
    
    Console.WriteLine("Ambiente de desenvolvimento!");    
}
app.Run();


void LoadConfiguration(WebApplication app)
{
    //app.Configuration: .GetSection(), .GetConnectionString(), .GetValue()
    Configuration.JwtKey = app.Configuration.GetValue<string>("JwtKey");
    Configuration.ApiKeyName = app.Configuration.GetValue<string>("ApiKeyName");
    Configuration.ApiKey = app.Configuration.GetValue<string>("ApiKey");
    
    Configuration.UrlImages = app.Configuration.GetValue<string>("UrlImages");
    
    var smtp = new Configuration.SmtpConfiguration();
    app.Configuration.GetSection("Smtp").Bind(smtp); //Bind passa as definiçoes diretamente! O ASP.NET vai converter o JSON da secão smtp e preencher com as propriedades no objeto smtp 
    Configuration.Smtp = smtp;
}

void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var key = Encoding.ASCII.GetBytes(Configuration.JwtKey);
    builder.Services.AddAuthentication(x =>
    { //indica o esquema de autenticação
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; //esquema de autenticação 
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; //desafio
    }).AddJwtBearer(x =>
    { //indicar como é q ele vai desencriptar o token - autorização: parâmetros do token
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true, //validar chave de assinatura
            IssuerSigningKey = new SymmetricSecurityKey(key), //validá-la com chave simética -> o mesmo método q usamos no TokenService.cs para conseguir desencriptar a chave
            ValidateIssuer = false,
            ValidateAudience = false //estas 2 linhas para já não interessam pq estamos a trabalhar com 1 modelo com autenticação de 1 API. DefaultChallengeScheme e estas seriam necessárias por exemplo num parque com multiplas APIs
        };
    });
}

void ConfigureMvc(WebApplicationBuilder builder)
{
    builder.Services.AddMemoryCache(); //funcionalidade fantastica incluida no ASP.NET. Não precisa de dor de cabeça para procurar pacote.
    //Compressão de resposta
    builder.Services.AddResponseCompression(options =>
    {
        //options.Providers.Add<BrotliCompressionProvider>(); 
        options.Providers.Add<GzipCompressionProvider>(); //dos + populares
        //options.Providers.Add<CustomCompressionProvider>();
    });
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.SmallestSize; //CompressionLevel.Optimal;
    });
    builder
        .Services
        .AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true; //com esta linha de código, por exemplo na CategoryController.cs já sou obrigada a fazer a validação do ModelState pq aqui estou a desligar esta validação automática do [ApiController] 
        })
        .AddJsonOptions(x =>
        { 
            x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; //ignorar ciclos subsequentes (*2)
            x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault; //esta é para não renderizar objetos nulos por exemplo. vai simplesmente ignorar.
        });
}

void ConfigureServices(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<BlogDataContext>( options => options.UseSqlServer(connectionString));//Se usar o DataContext usar sempre o AddDbContext - suporte nativo!!
    builder.Services.AddTransient<TokenService>(); //(*1) só vamos usar 1 pedaço específico da aplicação. Por isso pode ser este. (Resolve o erro: No service for type 'Blog.Services.TokenService' has been registered.)
    builder.Services.AddTransient<EmailService>();
    /* life time - tempo de vida
    builder.Services.AddTransient(); //vai criar sempre um novo, incluindo se tiver filhos -> new TokenService
    builder.Services.AddScoped(); //vai durar por Transação (reutiliza). Ao fazer uma requisição por ex ao fazer login vai criar um NEW REQUEST (1234), vai gerar 1 TokenService. Se dentro do meu método Login tivesse uma chamada a um outro método q tb precisasse do [FromService] então o AddScoped vai ver se já existe um TokenService e ao detetar existência, usa-o sem criar novo (reaproveita). No final da requisição (no método principal - ex login) por exemplo com retun Ok(), mata a requisição e consequentemente o TokenService.   
    builder.Services.AddSingleton(); //Singleton -> 1 por App! Vai carregar para a memória e vai ser sempre o mesmo até a aplicação morrer.
    */
}