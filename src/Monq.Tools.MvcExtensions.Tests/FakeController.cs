using Microsoft.AspNetCore.Mvc;
using Monq.Tools.MvcExtensions.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Tools.MvcExtensions.Tests
{
    public class FakeController : Controller
    {
        public void MethodWithoutParams()
        {
        }

        public void MethodWithoutAttributes(object arg)
        {
        }

        public void MethodWithValidAttribute([ValidValidation]object arg, [FromBody]ValidFakeViewModel model)
        {
        }

        public void MethodWithInvalidAttribute([InvalidValidation]object arg)
        {
        }

        public void MethodWithInvalidAttributeBody([ValidValidation]object arg, [FromBody]InvalidFakeViewModel model)
        {
        }
    }
}
