using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Blog.Extensions
{
    public static class ModelStateExtension
    {
        //a palavra "this" torna este método um método de Extensão!
        //e simplesmente com isto já vai aparecer o.GetErrors no Post do CategoryController.cs
        //como se estivesse dentro do ModelStateDictionary
        //ie, sempre q chamar um ModelStateDictionary e fizer ponto vai-me disponibilizar este método GetErrors q implementamos aqui
        /*public static List<string> GetErrors(this ModelStateDictionary modelSate)
        {
            var result = new List<string>();
            foreach (var item in modelSate.Values)
            { 
                foreach (var error in item.Errors)
                    result.Add(error.ErrorMessage);              
            }
            return result;
        }*/

        //R# - ReSharper sugere transformar o de cima nisto:       
        public static List<string> GetErrors(this ModelStateDictionary modelSate)
        {
            var result = new List<string>();
            foreach (var item in modelSate.Values)                           
                result.AddRange(item.Errors.Select(error => error.ErrorMessage)); //LINQ-expression

            return result;
        }
    }
}
