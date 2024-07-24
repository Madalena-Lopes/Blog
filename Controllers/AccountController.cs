using Blog.Data;
using Blog.Extensions;
using Blog.Models;
using Blog.Services;
using Blog.ViewModels;
using Blog.ViewModels.Accounts;
using Microsoft.AspNetCore.Authorization;


//using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;
using System.Text.RegularExpressions;

namespace Blog.Controllers
{
    //[Authorize] //Autorizar aqui é para o controller todo
    [ApiController]
    public class AccountController: ControllerBase
    {
        /*private readonly TokenService _tokenService; //INJECÇÃO DE DEPENDÊNCIA

        public AccountController(TokenService tokenService) //criação de dependência - INJECÇÃO DE DEPENDÊNCIA
        {
            _tokenService = tokenService;
        }


        [HttpPost("v1/login")]
        public IActionResult Login()
        {
            //var tokenService = new TokenService();
            var token = _tokenService.GenerateToken(null);

            return Ok(token);
        }
        */

        //estou a dizer q precisa de um TokenService (criei 1 dependência), 
        //mas ainda não disse em lado nehum como é q ele vai resolver essa dependência (*1)
        //[AllowAnonymous] //no método Login específicamente quero permitir anónimos,ie, permitir não estar autenticado (<> [Authorize])
        [HttpPost("v1/accounts/login")] // -> NEW REQUEST (1234)
        public async Task<IActionResult> Login(
            [FromBody] LoginViewModel model,
            [FromServices] BlogDataContext context,
            [FromServices] TokenService tokenService) //Isto é interpretado da mesma maneira q o q está a ser feito no construtor e na declaração acima
        {
            //var token = tokenService.GenerateToken(null);
            //return Ok(token);
            if (!ModelState.IsValid)
                return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

            var user = await context
                .Users
                .AsNoTracking()
                .Include(x => x.Roles) //pq vou precisar para gerar os claims q vão no nosso token
                .FirstOrDefaultAsync(x => x.Email == model.Email);

            if (user == null)
                return StatusCode(401, new ResultViewModel<string>("Utilizador ou senha inválidos")); //Não se diz o utilizador não existe. Omite-se essa informação. Segurança. Boa prática ser mais genérico aqui.
          
            if (!PasswordHasher.Verify(user.PasswordHash, model.Password))
                return StatusCode(401, new ResultViewModel<string>("Utilizador ou senha inválidos"));

            try
            {
                var token = tokenService.GenerateToken(user);
                return Ok(new ResultViewModel<string>(token,null)); //como o token é uma string, passo "null" nos erros para usar o construtor certo
            }
            catch 
            {
                return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no servidor!"));
            }
        }

        /* Senha Forte: G2B257X&D%1HF9B8!FB@M1}^5   
         * 
         * Armazenar as senhas:
         *  1 - Encriptar a senha no mínimo (mtas vezes usada pelo utilizador em vários sítios)
         */


        //atualizar a imagem do utilizador
        [Authorize] //vai ter q autenticar-se pq só pode alterar a imagem dele (de mais ninguém)
        [HttpPost("v1/accounts/upload-image")] //url
        public async Task<IActionResult> UploadImage(
           [FromBody] UploadImageViewModel model,
           [FromServices] BlogDataContext context) 
        { 
            var fileName = $"{Guid.NewGuid().ToString()}.jpg"; //guid para gerar sempre com um nome diferente e não coincidir com 1 tentativa q tenha ficado na cache
            var data = new Regex(@"^data:image\/[a-z]+;base64,").Replace(model.Base64Image, "");
            var bytes = Convert.FromBase64String(data); //facilitar para o FrontEnd sempre q possivel

            try //se recebesse 1 []byte já só precisava daqui para baixo
            {
                await System.IO.File.WriteAllBytesAsync($"wwwroot/images/{fileName}", bytes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no servidor"));
            }

            var user = await context
                .Users
                .FirstOrDefaultAsync(x => x.Email == User.Identity.Name); //mesma regra q colocamos no ReleCalimsExtension.cs

            if (user == null)
                return NotFound(new ResultViewModel<Category>("Utilizador não encontrado!"));

            user.Image = $"{Configuration.UrlImages}{fileName}"; //Colocar no configurationspara: não correr o risco de passar com url aqui para produção.
            try
            {
                context.Users.Update(user);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no servidor"));
            }

            return Ok(new ResultViewModel<string>("Imagem alterada com sucesso!", null));
        }

        [HttpPost("v1/accounts/")]
        public async Task<IActionResult> Post(
            [FromBody] RegisterViewModel model,
            [FromServices] EmailService emailService,
            [FromServices] BlogDataContext context)
        { 
            if (!ModelState.IsValid)
                return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Slug = model.Email.Replace("@", "-").Replace(".", "-")
                //se quisesse receber a pass da tela teria q colocar aqui a linha PasswordHash = model.PasswordHash
            };

            var password = PasswordGenerator.Generate(25);
            user.PasswordHash = PasswordHasher.Hash(password); //encriptar a senha(pass) - hasheada. Usa a datahora e por isso fica diferente a cada vez q execute

            try
            {
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                emailService.Send(
                    user.Name,
                    user.Email, 
                    "Bem vindo ao Blog!", 
                    $"A sua password é <strong>{password}</strong>");

                //dynamic pq não quisemos criar 1 ViewModel só para este retorno
                return Ok(new ResultViewModel<dynamic>(new
                {
                    user = user.Email//,
                    //password //o mesmo q password = password. O C# assume o nome do parâmetro. Só para testes pq o q devia fazer aqui era mandar o email com a pass.
                }));
            }
            catch (DbUpdateException)
            {
                return StatusCode(400, new ResultViewModel<string>("05X99 - Este email já está registado!"));
            }                
            catch (Exception ) 
            {
                return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no servidor!"));
            }
        }

        



        /*
        //assim o atributo é só para o método
        [Authorize(Roles = "user")] //Authorize verifica se fez login, mas só um utilizador com Roles user pode aceder a este método
        [HttpGet("v1/user")]
        public IActionResult GetUser() => Ok(User.Identity.Name);

        [Authorize(Roles = "author")] //só um utilizador com Roles author pode aceder a este método     
        [HttpGet("v1/author")]
        public IActionResult GetAuthor() => Ok(User.Identity.Name);

        [Authorize(Roles = "admin")]
        [HttpGet("v1/admin")]
        public IActionResult GetAdmin() => Ok(User.Identity.Name);
        */
    }
}
