using Blog.Data;
using Blog.Extensions;
using Blog.Models;
using Blog.ViewModels;
using Blog.ViewModels.Categories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Blog.Controllers
{
    [ApiController]
    public class CategoryController: ControllerBase
    {
        //convenção Rest nas Apis- estilo microsoft: aqui é sempre no plural e sem assêntos. No caso de UserRole ficaria: "user-roles" 
        //[HttpGet("categorias")] podia ter as 2 rotas, mas vamos deixar apenas 1 para não causar confusão
        [HttpGet("v1/categories")] //localhost:PORT/v1/categories
        public async Task<IActionResult> GetAsync(
            [FromServices] IMemoryCache cache,
            [FromServices] BlogDataContext context)
        { 
            try
            {
                //var categories = await context.Categories.ToListAsync();
                //Com cache: se já existir no cache vai usar (até ao tempo de duração da cache) senão vai ler à BD
                var categories = cache.GetOrCreate("CategoriesCache", entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3); //cache q vai durar por 1 hora
                    return GetCategories(context);
                });
                
                //return Ok(categories);
                return Ok(new ResultViewModel<List<Category>>(categories)); //aqui em caso de erro, vai ser vazio por padrão
            }
            catch //(Exception ex)
            {
                //return StatusCode(500, "05XE05 - Falha interna no servidor!");
                return StatusCode(500, new ResultViewModel<List<Category>>("05XE05 - Falha interna no servidor!"));
            }
        }

        private List<Category> GetCategories(BlogDataContext context)
        {
            return context.Categories.AsNoTracking().ToList(); 
        }

        //Nota: sempre que possivel usar async = "paralelização das tarefas(=Task)" e await = "aguardar pela execução" pq alivia o processador/servidor!

        //Versionamento: suponhamos q por necessidade de mais 1 parâmetro Id
        //importante para versões antigas não deixarem de funcionar enquando as pessoas não atualizam: ou pq estão sem rede...  
        //às vezes dão um tempo de 3 meses para as pessoas poderem atualizar a versão e só aí é q a v1 poderia sair do projecto.
        /*[HttpGet("v2/categories")] //localhost:PORT/v2/categories
        public IActionResult Get2(
            [FromServices] BlogDataContext context)
        {
            var categories = context.Categories.ToList();
            return Ok(categories);
        }
        */

        //obter a categoria com o id
        [HttpGet("v1/categories/{id:int}")] //localhost:PORT/v1/categories
        public async Task<IActionResult> GetByIdAsync(
            [FromRoute]int id,
            [FromServices] BlogDataContext context)
        {
            try
            {
                var category = await context
                .Categories
                .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                    return NotFound(new ResultViewModel<Category>("Conteúdo não encontrado!"));

                return Ok(new ResultViewModel<Category>(category));
            }
            catch //(Exception ex)
            {
                return StatusCode(500, new ResultViewModel<Category>("05XE04 - Falha interna no servidor!"));
            }
        }

        //criar 1 categoria
        //CreateCategoryViewModel - modelo baseado em visualização. Como temos a indicação [ApiController] (sem ela não dá) estava a obrigar a colocar "posts": null que não faz mt sentido.
        //Sem [ApiController] não obrigava e funcionava bem com Category. Nesse caso não precisa da category e onde está "category" era "model".
        //TUDO O Q SE PODER FACILITAR PARA O FRONT-END DEVEMOS FACILITAR! 
        //EditorCategoryViewModel - Create foi alterado para Editor pq a ideia,neste caso, é a mesma tanto para o Create como para o Edit/Update (Senão tinham q ter 2 classes separadas)
        [HttpPost("v1/categories")] 
        public async Task<IActionResult> PostAsync(
            [FromBody] EditorCategoryViewModel model,
            [FromServices] BlogDataContext context)
        {
            //este código não precisa pq o ASP.NET agora trata disto por padrão => pq tenho o atributo[ApiController] nesta class.
            //!\Os controladores de API Web não precisarão verificar ModelState.IsValid se eles tiverem o atributo[ApiController].            
            //ModelState - item disponibilizado pelo ASP.NET sempre que um modelo é passado como [FromBody].
            // E valida as nossas anotaçoes na class EditorCategoryViewModel
            if (!ModelState.IsValid)
                return BadRequest(new ResultViewModel<Category>(ModelState.GetErrors()));

            try
            {
                var category = new Category
                {
                    Id = 0,
                    //Posts = null //não precisa
                    Name = model.Name,
                    Slug = model.Slug.ToLower(),
                };
                await context.Categories.AddAsync(category);
                await context.SaveChangesAsync();

                return Created($"v1/categories/{category.Id}", new ResultViewModel<Category>(category)); //Created é só para o POST!
            }
            catch(DbUpdateException) //sempre dos + específicos para os + genéricos
            {
                //return BadRequest("Não foi possível criar a categoria"); //BadRequest é erro de requisição inválida
                return StatusCode(500, new ResultViewModel<Category>("05XE9 - Não foi possível criar a categoria!")); //erros 500 não vale a pena detalhar muito, até porque pode ter alguma informação sensível no erro...
            }
            catch //(Exception ex)
            {
                return StatusCode(500, new ResultViewModel<Category>("05XE10 - Falha interna no servidor!"));
            }
        }

        //atualizar 1 categoria
        [HttpPut("v1/categories/{id:int}")]
        public async Task<IActionResult> PutAsync(
            [FromRoute] int id,
            [FromBody] EditorCategoryViewModel model,
            [FromServices] BlogDataContext context)
        {
            try
            {
                var category = await context
                .Categories
                .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                    return NotFound(new ResultViewModel<Category>("Conteúdo não encontrado!"));

                category.Name = model.Name;
                category.Slug = model.Slug;

                context.Categories.Update(category); //Update não tem async por isso não tem await
                await context.SaveChangesAsync();

                return Ok(new ResultViewModel<Category>(category));
            }
            catch (DbUpdateException) //sempre dos + específicos para os + genéricos
            {                
                return StatusCode(500, new ResultViewModel<Category>("05XE8 - Não foi possível atualizar a categoria!")); //erros 500 não vale a pena detalhar muito, até porque pode ter alguma informação sensível no erro...
            }
            catch //(Exception ex)
            {
                return StatusCode(500, new ResultViewModel<Category>("05XE11 - Falha interna no servidor!"));
            }

        }

        //excluir 1 categoria
        [HttpDelete("v1/categories/{id:int}")]
        public async Task<IActionResult> DeleteAsync(
            [FromRoute] int id,
            [FromServices] BlogDataContext context)
        {
            try
            {
                var category = await context
                .Categories
                .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                    return NotFound(new ResultViewModel<Category>("Conteúdo não encontrado!"));

                context.Categories.Remove(category); //não tem async por isso não tem await
                await context.SaveChangesAsync();

                return Ok(new ResultViewModel<Category>(category));
            }
            catch(DbUpdateException) //sempre dos + específicos para os + genéricos
            {
                //return BadRequest("Não foi possível criar a categoria"); //BadRequest é erro de requisição inválida
                return StatusCode(500, new ResultViewModel<Category>("05XE7 - Não foi possível remover a categoria!")); //erros 500 não vale a pena detalhar muito, até porque pode ter alguma informação sensível no erro...
            }
            catch //(Exception ex)
            {
                return StatusCode(500,  new ResultViewModel<Category>("05XE12 - Falha interna no servidor!"));
            }
        }
    }
}
