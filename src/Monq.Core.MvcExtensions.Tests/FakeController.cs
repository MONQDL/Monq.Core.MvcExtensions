using Microsoft.AspNetCore.Mvc;
using Monq.Core.MvcExtensions.Tests.Fakes;

namespace Monq.Core.MvcExtensions.Tests;

#pragma warning disable S1186 // Methods should not be empty
public class FakeController : Controller
{
    public void MethodWithoutParams()
    {
    }

    public void MethodWithoutAttributes(object arg)
    {
    }

    public void MethodWithValidAttribute([ValidValidation] object arg, [FromBody] ValidFakeViewModel model)
    {
    }

    public void MethodWithInvalidAttribute([InvalidValidation] object arg)
    {
    }

    public void MethodWithInvalidAttributeBody([ValidValidation] object arg, [FromBody] InvalidFakeViewModel model)
    {
    }
}
