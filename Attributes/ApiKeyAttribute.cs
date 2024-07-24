using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Blog.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter //filtro de acção - enquanto a acção estiver a ser executada: vou poder deixá-la passar ou interceptá-la
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context, 
            ActionExecutionDelegate next) //localhost:5001?api_Key=CHAVE
        {
            if (!context.HttpContext.Request.Query.TryGetValue(Configuration.ApiKeyName, out var extratedApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "ApiKey não encontrada!"
                };
                return;
            }

            if (!Configuration.ApiKey.Equals(extratedApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 403,
                    Content = "Acesso não autorizado!"
                };
                return;
            }

            //Se deu tudo certo até aqui 
            await next(); //aguarda de forma assíncrona a conclusão da tarefa
        }
    }
}
