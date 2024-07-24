using System.ComponentModel.DataAnnotations;

namespace Blog.ViewModels.Categories
{
    //Editor - para indicar q para o create e o update/edit. Caso não fosse o pretendido teria q ter 2 classes distintas na pasta ViewModels
    public class EditorCategoryViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório!")]
        [StringLength(40, MinimumLength = 3, ErrorMessage = "Este campo deve conter entre 3 e 40 caracteres.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "O slug é obrigatório!")]
        public string Slug { get; set; }
    }
}
