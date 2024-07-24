using Blog.Models;

namespace Blog.ViewModels.Posts
{
    public class ListPostsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public string Category { get; set; } //aqui digo q quero q a Category seja convertida para string e a de baixo tb
        public string Author { get; set; }
    }
}
