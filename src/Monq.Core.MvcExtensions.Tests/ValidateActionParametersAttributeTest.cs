using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Monq.Core.MvcExtensions.Tests.Fakes;
using Monq.Core.MvcExtensions.Validation;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Monq.Core.MvcExtensions.Tests
{
    public class ValidateActionParametersAttributeTest
    {
        readonly ModelStateDictionary _modelStateDictionary = new();
        readonly RouteData _routeData = new();

        ActionExecutingContext CreateActionExecutingContext(MethodInfo methodInfo, Dictionary<string, object> actionArguments = null)
        {
            var httpContext = new DefaultHttpContext();
            var controller = new FakeController
            {
                ControllerContext = new ControllerContext(),
                //ObjectValidator = new DefaultObjectValidator(metadataProvider, validationProviders),
                //MetadataProvider = metadataProvider
            };

            var actionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = methodInfo
            };

            var actionContext = new ActionContext(
                httpContext,
                _routeData,
                actionDescriptor,
                _modelStateDictionary);

            var actionExecutingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments ?? new Dictionary<string, object>(),
                controller);

            return actionExecutingContext;
        }

        [Fact(DisplayName = "ValidateActionParams - без параметров.")]
        public void OnActionExecuting_NoParameters_NoModelError()
        {
            var sut = new ValidateActionParametersAttribute();
            var actionExecutingContext = CreateActionExecutingContext(typeof(FakeController).GetMethod("MethodWithoutParams"));

            // Act
            sut.OnActionExecuting(actionExecutingContext);

            Assert.Equal(0, actionExecutingContext.ModelState.ErrorCount);
        }

        [Fact(DisplayName = "ValidateActionParams - без атрибутов валидации.")]
        public void OnActionExecuting_ParameterWithoutValidationAttributes_NoModelError()
        {
            var sut = new ValidateActionParametersAttribute();

            var actionExecutingContext = CreateActionExecutingContext(
                typeof(FakeController).GetMethod("MethodWithoutAttributes")
            );

            // Act
            sut.OnActionExecuting(actionExecutingContext);

            Assert.Equal(0, actionExecutingContext.ModelState.ErrorCount);
        }

        [Fact(DisplayName = "ValidateActionParams - валидная модель.")]
        public void OnActionExecuting_ParameterValidValidationAttributes_ModelErrorAdded()
        {
            var sut = new ValidateActionParametersAttribute();

            var actionExecutingContext = CreateActionExecutingContext(typeof(FakeController).GetMethod("MethodWithValidAttribute"));

            // Act
            sut.OnActionExecuting(actionExecutingContext);

            Assert.Equal(0, actionExecutingContext.ModelState.ErrorCount);
        }

        [Fact(DisplayName = "ValidateActionParams - невалидная модель типа query.")]
        public void OnActionExecuting_ParameterInvalidValidationAttributes_NoModelError()
        {
            var sut = new ValidateActionParametersAttribute();

            var actionExecutingContext = CreateActionExecutingContext(typeof(FakeController).GetMethod("MethodWithInvalidAttribute"));

            // Act
            sut.OnActionExecuting(actionExecutingContext);

            Assert.Equal(1, actionExecutingContext.ModelState.ErrorCount);
            Assert.Contains("TestErrorMessage", actionExecutingContext.ModelState.First().Value.Errors.First().ErrorMessage);
        }

        [Fact(DisplayName = "ValidateActionParams - невалидная модель типа body.", Skip = "Нужно правильно сконфигурировать контроллер, чтобы он выполнил валидацию модели.")]
        public void OnActionExecuting_ParameterInvalidFromBodyAttribute_NoModelError()
        {
            var sut = new ValidateActionParametersAttribute();
            var actionArguments = new Dictionary<string, object>
            {
                { "arg", new object() },
                { "model", new InvalidFakeViewModel() }
            };
            var actionExecutingContext = CreateActionExecutingContext(typeof(FakeController).GetMethod("MethodWithInvalidAttributeBody"),
                actionArguments);

            // Act
            sut.OnActionExecuting(actionExecutingContext);

            Assert.Equal(1, actionExecutingContext.ModelState.ErrorCount);
            Assert.Contains("TestErrorMessage", actionExecutingContext.ModelState.First().Value.Errors.First().ErrorMessage);
        }
    }
}