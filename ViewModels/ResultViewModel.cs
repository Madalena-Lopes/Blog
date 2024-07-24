namespace Blog.ViewModels
{
    //Padronização dos resultados da aplicação - para q o FrontEnd saiba o q esperar em cada caso
    public class ResultViewModel<T>
    {
        public ResultViewModel(T data, List<string> errors)
        {
            Data = data;
            Errors = errors;
        }

        public ResultViewModel(T data) //só recebe os dados se deu certo
        {
            Data = data;
        }

        public ResultViewModel(List<string> errors) //só recebe se houve vários erros
        {
            Errors = errors;
        }

        public ResultViewModel(string error) //só recebe 1 erro
        {
            Errors.Add(error);
        }

        public T Data { get; private set; }
        public List<string> Errors { get; private set; } = new(); //= new List<string>(); -> já não é preciso q ele consegue sozinho identificar o tipo!
    }
}
