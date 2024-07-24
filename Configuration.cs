namespace Blog; //no C#10 foi removido a necessidade do uso de {}

public static class Configuration
{
    //Chave jwt - criação de um hash - TOKEN - Json Web Token - chave encriptada e qd for desencriptada vai estar no formato JSON
    public static string JwtKey = "ZmVkYWY3ZDg4NjNiNDhlMTk3YjkyODdkNDkyYjcwOGU=";
    public static string ApiKeyName = "api_key"; //parâmetro q vai ser passado na requisição, para saber q vai estar autenticado por este modo de ApiKey
    public static string ApiKey = "curso_api_IlTevUM/z0ey3NwCV/unWg=="; //temos q ter cuidado com esta parte de segurança para ninguém descobrir esta senha

    public static string UrlImages = "https://localhost:0000/images/";//sem porta porque pode variar

    public static SmtpConfiguration Smtp = new();


    public class SmtpConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; } = 25;
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}

 