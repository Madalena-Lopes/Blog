using Blog.Extensions;
using Blog.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Blog.Services
{
    public class TokenService
    {
        public string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();//manipulador de token. neste caso vamos trab com o jwt, mas existem vários
            var key = Encoding.ASCII.GetBytes(Configuration.JwtKey); //chave da configuração em array de bytes pronto para passar para o tokenHandler 
            var claims = user.GetClaims();
            var tokenDescriptor = new SecurityTokenDescriptor
            { //8h - duração do token e depois tem q autenticar novamente para aceder à aplicação. Não usar um tempo mt alto pq entretanto mt coisa muda
                /*Subject = new ClaimsIdentity(new Claim[]
                { 
                    new (ClaimTypes.Name, "andrebaltieri"), //User.Identity.Name
                    new (ClaimTypes.Role, "user"), //User.IsInRole
                    new (ClaimTypes.Role, "admin"),
                    new ("fruta","banana")
                }),*/
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature) //jwt.io -  analisar o token Algoritmo: HS256
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
