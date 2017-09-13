using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Monq.Tools.MvcExtensions.Filters;
using System.ComponentModel.DataAnnotations;
using Monq.Tools.MvcExtensions.Validation;
using Monq.Tools.MvcExtensions.TestApp.ViewModels;

namespace Monq.Tools.MvcExtensions.TestApp.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody, Required]ValueViewModel value)
        {
            return Ok(value);
        }

        // PUT api/values/5
        [HttpPut("{id}"), ValidateActionParameters]
        public IActionResult Put([Range(1, int.MaxValue)]int id, [FromBody]ValueViewModel value)
        {
            return Ok(value);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
