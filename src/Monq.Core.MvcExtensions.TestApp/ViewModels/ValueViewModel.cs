using DelegateDecompiler;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monq.Core.MvcExtensions.TestApp.ViewModels
{
    public class ValueViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Требуется указать название.")]
        [MaxLength(5, ErrorMessage = "Длинна не больше 5 символов.")]
        [EmailAddress]
        public string Name { get; set; }

        [Required(ErrorMessage = "Требуется указать размер.")]
        [Range(0, int.MaxValue, ErrorMessage = "Размер должен быть положительным числом")]
        public int Capacity { get; set; }

        public bool Enabled { get; set; } = true;

        public long ZabbixId { get; set; }
        public long ElementId { get; set; }
        public string StringElementId { get; set; }

        public ValueViewModel Child { get; set; }

        public IEnumerable<ValueViewModel> ChildEnum { get; set; }

        [Computed]
        public long ComputedProp => Id + Capacity;

        [Computed]
        public long ComputedPropWithTime => Id + Capacity + DateTimeOffset.Now.ToUnixTimeSeconds();
    }
}