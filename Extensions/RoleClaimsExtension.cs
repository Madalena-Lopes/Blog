using Blog.Models;
using System.Security.Claims;

namespace Blog.Extensions
{
    //vamos fazer tipo um parser: transformar os objetos do tipo Role num tipo Claim
    public static class RoleClaimsExtension
    {
        public static IEnumerable<Claim> GetClaims(this User user)
        { 
            var result = new List<Claim>
            {
                new (ClaimTypes.Name, user.Email) //torna-se 1 User.Identity.Name - No 2º parametro podia ter colocado user.Id.ToString() pq o parâmetro tem q ser string (senão era guid)
            };
            result.AddRange(
                user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Slug))); //role.Slug na nossa BD tem o mesmo q role.Name, mas colocou-se este por causa do slug não ter espaços...

            return result;
        }
    }
}
