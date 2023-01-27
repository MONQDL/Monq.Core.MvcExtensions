using Microsoft.AspNetCore.Mvc;
using Monq.Core.MvcExtensions.Filters;
using Monq.Core.MvcExtensions.TestApp.ViewModels;

namespace Monq.Core.MvcExtensions.TestApp.Controllers
{
    [Route("api/field-mask")]
    public class FieldMaskController : Controller
    {
        public FieldMaskController()
        {
        }

        [HttpGet]
        [FieldMask]
        public ActionResult<ValueViewModel> Get([FromQuery] string[] fieldMask)
        {
            return new ValueViewModel()
            {
                Capacity = 1,
                StringElementId = "parent",
                Enabled = true,
                ChildEnum = new[]
                {
                    new ValueViewModel
                    {
                        Capacity = 2,
                        Id = 2,
                        Enabled = true
                    }
                },
                Child = new()
                {
                    Capacity = 2,
                    StringElementId = "child",
                    Enabled = false,
                },
            };
        }
    }
}
