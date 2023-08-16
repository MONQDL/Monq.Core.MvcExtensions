using Microsoft.AspNetCore.Mvc;
using Monq.Core.MvcExtensions.Filters;

namespace Monq.Core.MvcExtensions.TestApp.Controllers
{
    [Route("api/array-input")]
    public class ArrayInputController : Controller
    {
        public ArrayInputController()
        {
        }

        [HttpGet]
        [ArrayInput("arr")]
        public ActionResult<string[]> Get([FromQuery] string[] arr)
        {
            return arr;
        }
    }
}
