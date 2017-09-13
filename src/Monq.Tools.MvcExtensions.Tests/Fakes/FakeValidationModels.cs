using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Tools.MvcExtensions.Tests.Fakes
{
    internal class ValidValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return true;
        }
    }

    internal class InvalidValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return "TestErrorMessage";
        }
    }

    public class InvalidFakeViewModel
    {
        [InvalidValidation]
        public int Id { get; set; }
    }

    public class ValidFakeViewModel
    {
        [ValidValidation]
        public int Id { get; set; }
    }
}
