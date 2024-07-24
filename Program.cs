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
builder.Services.AddSwaggerGen(); //gera o c�digo da interface (HTML) do swagger

var app = builder.Build();
LoadConfiguration(app);

//colocar pela ordem q se pensa q a aplica��o vai ser executada
app.UseHttpsRedirection(); //tag q permite fazer redirecionamentos http -> https
app.UseAuthentication(); //1� - quem sou
app.UseAuthorization(); //2� - o que posso fazer
app.MapControllers();
app.UseStaticFiles(); //para o servidor conseguir renderizar arquivos est�ticos: imagens, js, css, html. Apenas a t�tulo did�tico. N�o usar arquivos est�ticos dentro da API. Fazer antes por exemplo upload de imagem para o Azure.
app.UseResponseCompression(); //A parte de compress�o s� deve funcionar em produ��o. Em Debug at� coloca mais 1 nico por causa de alterar a informa��o do Header (PostMan). 

if (app.Environment.IsDevelopment())
{
    //Documentar a API
    app.UseSwagger(); //por padr�o n�o se coloca em produ��o, mas tb n�o tem problema. Depende da necessidade q temos.
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
    app.Configuration.GetSection("Smtp").Bind(smtp); //Bind passa as defini�oes diretamente! O ASP.NET vai converter o JSON da sec�o smtp e preencher com as propriedades no objeto smtp 
    Configuration.Smtp = smtp;
}

void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var key = Encoding.ASCII.GetBytes(Configuration.JwtKey);
    builder.Services.AddAuthentication(x =>
    { //indica o esquema de autentica��o
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; //esquema de autentica��o 
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; //desafio
    }).AddJwtBearer(x =>
    { //indicar como � q ele vai desencriptar o token - autoriza��o: par�metros do token
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true, //validar chave de assinatura
            IssuerSigningKey = new SymmetricSecurityKey(key), //valid�-la com chave sim�tica -> o mesmo m�todo q usamos no TokenService.cs para conseguir desencriptar a chave
            ValidateIssuer = false,
            ValidateAudience = false //estas 2 linhas para j� n�o interessam pq estamos a trabalhar com 1 modelo com autentica��o de 1 API. DefaultChallengeScheme e estas seriam necess�rias por exemplo num parque com multiplas APIs
        };
    });
}

void ConfigureMvc(WebApplicationBuilder builder)
{
    builder.Services.AddMemoryCache(); //funcionalidade fantastica incluida no ASP.NET. N�o precisa de dor de cabe�a para procurar pacote.
    //Compress�o de resposta
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
            options.SuppressModelStateInvalidFilter = true; //com esta linha de c�digo, por exemplo na CategoryController.cs j� sou obrigada a fazer a valida��o do ModelState pq aqui estou a desligar esta valida��o autom�tica do [ApiController] 
        })
        .AddJsonOptions(x =>
        { 
            x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; //ignorar ciclos subsequentes (*2)
            x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault; //esta � para n�o renderizar objetos nulos por exemplo. vai simplesmente ignorar.
        });
}

void ConfigureServices(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<BlogDataContext>( options => options.UseSqlServer(connectionString));//Se usar o DataContext usar sempre o AddDbContext - suporte nativo!!
    builder.Services.AddTransient<TokenService>(); //(*1) s� vamos usar 1 peda�o espec�fico da aplica��o. Por isso pode ser este. (Resolve o erro: No service for type 'Blog.Services.TokenService' has been registered.)
    builder.Services.AddTransient<EmailService>();
    /* life time - tempo de vida
    builder.Services.AddTransient(); //vai criar sempre um novo, incluindo se tiver filhos -> new TokenService
    builder.Services.AddScoped(); //vai durar por Transa��o (reutiliza). Ao fazer uma requisi��o por ex ao fazer login vai criar um NEW REQUEST (1234), vai gerar 1 TokenService. Se dentro do meu m�todo Login tivesse uma chamada a um outro m�todo q tb precisasse do [FromService] ent�o o AddScoped vai ver se j� existe um TokenService e ao detetar exist�ncia, usa-o sem criar novo (reaproveita). No final da requisi��o (no m�todo principal - ex login) por exemplo com retun Ok(), mata a requisi��o e consequentemente o TokenService.   
    builder.Services.AddSingleton(); //Singleton -> 1 por App! Vai carregar para a mem�ria e vai ser sempre o mesmo at� a aplica��o morrer.
    */
}