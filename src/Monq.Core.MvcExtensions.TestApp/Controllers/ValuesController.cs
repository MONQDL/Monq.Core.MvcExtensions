﻿using Microsoft.AspNetCore.Mvc;
using Monq.Core.MvcExtensions.TestApp.ViewModels;
using Monq.Core.MvcExtensions.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monq.Core.MvcExtensions.TestApp.Controllers;

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

    [HttpPost, ValidateActionParameters]
    public IActionResult Post([FromBody] ValueViewModel value)
    {
        return Ok(value);
    }

    // PUT api/values/5
    [HttpPut("{id}"), ValidateActionParameters]
    public IActionResult Put([Range(1, int.MaxValue)] int id, [FromBody] ValueViewModel value)
    {
        return Ok(value);
    }

    // DELETE api/values/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }

    [HttpPatch("{id}"), ValidateActionParameters]
    public IActionResult Patch(int id, [FromBody] ValuePatchViewModel value)
    {
        return Ok(id);
    }

    [HttpPost("body"), ValidateActionParameters]
    public IActionResult PostWithId([FromBody] long id)
    {
        return Ok(id);
    }

    [HttpPost("recursive"), ValidateActionParameters]
    public IActionResult PostRecursiveValidationModel([FromBody] RecursiveViewModel value)
    {
        return Ok(value);
    }
}
