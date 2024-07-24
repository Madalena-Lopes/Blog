using Blog.Attributes;
using Microsoft.AspNetCore.Mvc;

//Helth Check - Exame de saúde - o q precisamos de pensar para as nossas APIs. Por exemplo para saber se a pagina/api está offline...se está boa de saude. Pingar a url.
// do lado da api devemos ter um endpoint = rota = url, onde se possa "pingar a url" = request = "requisição" = "pedido",
// e receber um status: 200=ok ou 500 se estiver fora/offline (se não responder 200, em princípio está em baixo)
namespace Blog.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController: ControllerBase
    {
        [HttpGet("")]  //[HttpGet("health-check")] algumas empresas podem ter alguma convenção tipo isto
        //[ApiKey] //tb podia ser em cima no controller. Para permitir login por ApiKey
        public IActionResult Get(
            [FromServices] IConfiguration config)
        {
            /* retornar um objeto anónimo com uma mensagem seria deste tipo. A mensagem é depois convertida em JSON.
            return Ok(new
            {
                fruta = "banana"
            });*/
            var env = config.GetValue<string>("Env");
            return Ok(new 
            {
                environment = env
            });
        }
    }
}
