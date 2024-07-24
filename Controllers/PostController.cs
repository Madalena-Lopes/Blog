using Blog.Data;
using Blog.Models;
using Blog.ViewModels;
using Blog.ViewModels.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blog.Controllers
{
    [ApiController] //pq não temos uma rota base/padrão
    public class PostController: ControllerBase
    {
        [HttpGet("v1/posts")]
        public async Task<IActionResult> GetAsync(
            [FromServices] BlogDataContext context,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 25) //registos por página q o FrontEnd pretender
        {
            try
            {
                //return Ok(await context.Posts.ToListAsync());
                var count = await context.Posts.AsNoTracking().CountAsync(); //isto deve ser ponderado se ajuda o FrontEnd pq é + 1 query
                var posts = await context
                    .Posts
                    .AsNoTracking()
                    .Include(x => x.Category)
                    .Include(x=> x.Author)
                    //teste 3 - (*2) usando as linhas AddJsonOptions no ConfigureMvc do Programs.cs - ignorando ciclos. Só com isto já não precisa de fazer .Select (traz os dados todos)
                    .Select (x => new ListPostsViewModel
                    {  
                        Id = x.Id,
                        Title = x.Title,
                        Slug = x.Slug,
                        LastUpdateDate = x.LastUpdateDate,
                        Category = x.Category.Name,
                        Author = $"{x.Author.Name} ({x.Author.Email})" ,
                    }) //teste 2 - Com ViewModel - esta opção só por si, sem a indicação (*2) no Programs já não tem o problema de ciclos
                    /*.Select (x => new { 
                        x.Id,
                        x.Title,
                    }) //teste 1 - listar só o q quero sem recuso a ViewModel */
                    
                    //paginação
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    
                    .OrderByDescending(x => x.LastUpdateDate)
                    .ToListAsync(); //executa a query no banco. Por isso o Select é antes para dizer q não quero q traga todos os campos

                //return Ok(posts);
                //o Balta acha q ajuda mt o FrontEnd
                //dynamic pq estamos a criar 1 objeto anónimo. Não temos um ViewModel. Podia criar.
                return Ok(new ResultViewModel<dynamic>(new
                {
                    total = count,
                    page,
                    pageSize,
                    posts
                }));
            }
            catch 
            {
                return StatusCode(500, new ResultViewModel<List<Post>>("05XE05 - Falha interna no servidor!"));
            }
        }


        [HttpGet("v1/posts/{id:int}")] //GetByIdAsync
        public async Task<IActionResult> DetailsAsync(                
            [FromServices] BlogDataContext context,
            [FromRoute] int id) 
        {
            try
            {
                var post = await context
                .Posts
                .AsNoTracking()
                .Include(x => x.Author) //INNER JOIN User
                .ThenInclude(x => x.Roles) //Usar com moderação. Executa um subselect. Evitar! (1 user tem IList<Role> Roles)
                .Include(x => x.Category) //INNER JOIN Category
                .FirstOrDefaultAsync(x => x.Id == id);

                if (post == null)
                    return NotFound(new ResultViewModel<Post>("Conteúdo não encontrado!"));

                return Ok(new ResultViewModel<Post>(post));
            }
            catch //(Exception ex)
            {
                return StatusCode(500, new ResultViewModel<Post>("05XE04 - Falha interna no servidor!"));
            }
        }


        //Listar todos os posts de uma categoria (ex backend/frontend/mobile/fullstack)
        //(url) convenção na listagem de subitems: v1/posts/category/{filtro categoria} 
        [HttpGet("v1/posts/category/{category}")] 
        public async Task<IActionResult> GetByCategoryAsync(
            [FromRoute] string category,
            [FromServices] BlogDataContext context,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 25)
        {
            try
            {
                var count = await context.Posts.AsNoTracking().CountAsync(); //isto deve ser ponderado se ajuda o FrontEnd pq é + 1 query
                var posts = await context
                    .Posts
                    .AsNoTracking()
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .Where(x => x.Category.Slug == category) //Antes do select. Funciona pq tem o include da Category na linha anterior. 
                    .Select(x => new ListPostsViewModel
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Slug = x.Slug,
                        LastUpdateDate = x.LastUpdateDate,
                        Category = x.Category.Name,
                        Author = $"{x.Author.Name} ({x.Author.Email})",
                    }) 
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .OrderByDescending(x => x.LastUpdateDate)
                    .ToListAsync(); 

                return Ok(new ResultViewModel<dynamic>(new
                {
                    total = count,
                    page,
                    pageSize,
                    posts
                }));
            }
            catch
            {
                return StatusCode(500, new ResultViewModel<List<Post>>("05XE05 - Falha interna no servidor!"));
            }
        }

        [HttpGet("v1/posts/author/{author}")]
        public async Task<IActionResult> GetByAuthorAsync(
            [FromRoute] string author,
            [FromServices] BlogDataContext context,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 25)
        {
            try
            {
                var count = await context.Posts.AsNoTracking().CountAsync(); //isto deve ser ponderado se ajuda o FrontEnd pq é + 1 query
                var posts = await context
                    .Posts
                    .AsNoTracking()
                    .Include(x => x.Author)
                    //.Include(x => x.Category) notar q não precisa para o ViewModel, mas poderia para a condição where 
                    .Where(x => x.Author.Slug == author || x.Author.Email == author) 
                    .Select(x => new ListPostsViewModel
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Slug = x.Slug,
                        LastUpdateDate = x.LastUpdateDate,
                        Category = x.Category.Name,
                        Author = $"{x.Author.Name} ({x.Author.Email})",
                    })
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .OrderByDescending(x => x.LastUpdateDate)
                    .ToListAsync();

                return Ok(new ResultViewModel<dynamic>(new
                {
                    total = count,
                    page,
                    pageSize,
                    posts
                }));
            }
            catch
            {
                return StatusCode(500, new ResultViewModel<List<Post>>("05XE05 - Falha interna no servidor!"));
            }
        }
    }    
}
