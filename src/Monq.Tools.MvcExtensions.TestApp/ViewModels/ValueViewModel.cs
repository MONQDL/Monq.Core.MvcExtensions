using System.ComponentModel.DataAnnotations;

namespace Monq.Tools.MvcExtensions.TestApp.ViewModels
{
    public class ValueViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Требуется указать название.")]
        [MaxLength(5, ErrorMessage = "Длинна не больше 5 символов.")]
        [EmailAddress]
        public string Name { get; set; }
    }
}
