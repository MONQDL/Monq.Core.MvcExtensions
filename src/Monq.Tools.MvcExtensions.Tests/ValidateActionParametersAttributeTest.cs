using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Monq.Tools.MvcExtensions.Tests.Fakes;
using Monq.Tools.MvcExtensions.Validation;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Monq.Tools.MvcExtensions.Tests
{
    public class ValidateActionParametersAttributeTest
    {
        readonly ModelStateDictionary modelStateDictionary = new ModelStateDictionary();
        readonly RouteData routeData = new RouteData();

        private ActionExecutingContext CreateActionExecutingContext(MethodInfo methodInfo, Dictionary<string, object> actionArguments = null)
        {
            var httpContext = new DefaultHttpContext();

            // TODO: for real this is how we configure controller?
            var detailsProviders = new IMetadataDetailsProvider[] { new DefaultValidationMetadataProvider() };

            var validationProviders = new List<IModelValidatorProvider> { new DefaultModelValidatorProvider() };
            var compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders);
            var metadataProvider = new DefaultModelMetadataProvider(compositeDetailsProvider);
            var controller = new FakeController();
            controller.ControllerContext = new ControllerContext();
            controller.ObjectValidator = new DefaultObjectValidator(metadataProvider, validationProviders);
            controller.MetadataProvider = metadataProvider;

            var actionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = methodInfo
            };

            var actionContext = new ActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                modelStateDictionary);

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
            var actionArguments = new Dictionary<string, object>();
            actionArguments.Add("arg", new object());
            actionArguments.Add("model", new InvalidFakeViewModel());
            var actionExecutingContext = CreateActionExecutingContext(typeof(FakeController).GetMethod("MethodWithInvalidAttributeBody"),
                actionArguments);

            // Act
            sut.OnActionExecuting(actionExecutingContext);

            Assert.Equal(1, actionExecutingContext.ModelState.ErrorCount);
            Assert.Contains("TestErrorMessage", actionExecutingContext.ModelState.First().Value.Errors.First().ErrorMessage);
        }
    }
}