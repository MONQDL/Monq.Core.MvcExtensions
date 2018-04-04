using Monq.Tools.MvcExtensions.Filters;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monq.Tools.MvcExtensions.TestApp.ViewModels
{
    public class RecursiveViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Требуется указать название.")]
        public string Name { get; set; }

        [Required]
        public IEnumerable<SubViewModel> SubCollection { get; set; }

        public SubObjectViewModel SubObject { get; set; }
    }

    public class SubViewModel
    {
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Id должен быть положительным числом")]
        public long Id { get; set; }

        [Required(ErrorMessage = "Требуется указать название.")]
        public string Name { get; set; }
    }

    public class SubObjectViewModel
    {
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Id должен быть положительным числом")]
        public long Id { get; set; }

        [Required(ErrorMessage = "Требуется указать размер.")]
        [Range(1, long.MaxValue, ErrorMessage = "Размер должен быть положительным числом")]
        public long Capacity { get; set; }
    }
}