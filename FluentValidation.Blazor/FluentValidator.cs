using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FluentValidation
{
    /// <summary>
    /// Add Fluent Validator support to an EditContext.
    /// </summary>
    public class FluentValidator : ComponentBase, IDisposable
    {
        /// <summary>
        /// Inherited object from the FormEdit component.
        /// </summary>
        [CascadingParameter]
        EditContext CurrentEditContext { get; set; }

        /// <summary>
        /// Enable access to the ASP.NET Core Service Provider / DI.
        /// </summary>
        [Inject]
        IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// The AbstractValidator object for the corresponding form Model object type.
        /// </summary>
        [Parameter]
        public IValidator Validator { set; get; }

        /// <summary>
        /// The AbstractValidator objects mapping for each children / nested object validators.
        /// </summary>
        [Parameter]
        public Dictionary<Type, IValidator> ChildValidators { set; get; } = new Dictionary<Type, IValidator>();

        /// <summary>
        /// Isolate scoped DbContext to this component.
        /// </summary>
        public IServiceScope ServiceScope { get; private set; }

        /// <summary>
        /// Attach to parent EditForm context enabling validation.
        /// </summary>
        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(DataAnnotationsValidator)} requires a cascading " +
                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(DataAnnotationsValidator)} " +
                    $"inside an EditForm.");
            }

            this.ServiceScope = ServiceProvider.CreateScope();

            if (this.Validator == null)
            {
                this.SetFormValidator();
            }

            this.AddValidation();
        }

        /// <summary>
        /// Try setting the EditContext form model typed validator implementation from the DI.
        /// </summary>
        private void SetFormValidator()
        {
            var formType = CurrentEditContext.Model.GetType();
            this.Validator = GetTypedValidator(formType);
            if (this.Validator == null)
            {
                throw new InvalidOperationException($"FluentValidation.IValidator<{formType.FullName}> is"
                    + " not registered in the application service provider.");
            }
        }

        /// <summary>
        /// Try acquiring the typed validator implementation from the DI.
        /// </summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        private IValidator GetTypedValidator(Type modelType)
        {
            var validatorType = typeof(IValidator<>);
            var formValidatorType = validatorType.MakeGenericType(modelType);
            return ServiceScope.ServiceProvider.GetService(formValidatorType) as IValidator;
        }

        /// <summary>
        /// Add form validation logic handlers.
        /// </summary>
        private void AddValidation()
        {
            var messages = new ValidationMessageStore(CurrentEditContext);

            // Perform object-level validation on request
            CurrentEditContext.OnValidationRequested +=
                (sender, eventArgs) => ValidateModel((EditContext)sender, messages);

            // Perform per-field validation on each field edit
            CurrentEditContext.OnFieldChanged +=
                (sender, eventArgs) => ValidateField(CurrentEditContext, messages, eventArgs.FieldIdentifier);
        }

        /// <summary>
        /// Validate the whole form and trigger client UI update.
        /// </summary>
        /// <param name="editContext"></param>
        /// <param name="messages"></param>
        private void ValidateModel(EditContext editContext, ValidationMessageStore messages)
        {
            // ATTENTION: DO NOT USE Async Void + ValidateAsync here
            // Explanation: Blazor UI will get VERY BUGGY for some reason if you do that. (Field CSS lagged behind validation)
            var validationResults = Validator.Validate(editContext.Model);

            messages.Clear();

            var modelGraphCache = new Dictionary<string, object>();
            foreach (var error in validationResults.Errors)
            {
                var (propertyValue, propertyName) = EvalObjectProperty(editContext.Model, error.PropertyName, modelGraphCache);
                if (propertyValue != null)
                {
                    var fieldID = new FieldIdentifier(propertyValue, propertyName);
                    messages.Add(fieldID, error.ErrorMessage);
                }
            }

            editContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Get object property value by string path separated by dot, supports array (IList) syntax.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="propertyPath"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        private (object propertyValue, string propertyName) EvalObjectProperty(object model, string propertyPath, Dictionary<string, object> cache)
        {
            if (propertyPath.Contains(".") == false)
            {
                return (model, propertyPath);
            }

            // FluentValidation Error PropertyName can be something like "ObjectA.ObjectB.PropertyX"
            // However, Blazor does NOT recognize nested FieldIdentifier.
            // Instead, the FieldIdentifier is assigned to the object in question. (Model + Property Name)
            // Therefore, we need to traverse the object graph to acquire them!
            var modelObjectPath = "";
            var objectParts = propertyPath.Split('.');
            var fieldName = objectParts[objectParts.Length - 1];
            for (var i = 0; i < objectParts.Length - 1; i++)
            {
                var propertyName = objectParts[i];
                bool isArray = false;
                int arrayIndex = 0;
                if (propertyName.Contains("[") && propertyName.Contains("]"))
                {
                    // propertyName = "A[22]" --> ["A", "22"]
                    var indexedPropertyName = propertyName.Split('[', ']');
                    propertyName = indexedPropertyName[0];
                    isArray = true;
                    arrayIndex = int.Parse(indexedPropertyName[1]);
                }

                // Constructing model object path here allows capturing the same array objects without the index!
                if (string.IsNullOrEmpty(modelObjectPath))
                {
                    modelObjectPath = propertyName;
                }
                else
                {
                    modelObjectPath += "." + propertyName;
                }

                // Locally cache objects found along the way to prevent slow multiple reflection method calls
                // For Example: large array of 1000 elements will only use reflection on that array object once!
                if (cache.ContainsKey(modelObjectPath))
                {
                    model = cache[modelObjectPath];
                }
                else
                {
                    model = model.GetType().GetProperty(propertyName)?.GetValue(model);
                    cache[modelObjectPath] = model;
                }

                if (isArray && model is IList array)
                {
                    // System.Array implements IList https://docs.microsoft.com/en-us/dotnet/api/system.array?view=netcore-3.0
                    model = array[arrayIndex];
                }

                if (model == null)
                {
                    break;
                }
            }

            return (model, fieldName);
        }

        /// <summary>
        /// Validate a single field and trigger client UI update.
        /// </summary>
        /// <param name="editContext"></param>
        /// <param name="messages"></param>
        /// <param name="fieldIdentifier"></param>
        private void ValidateField(EditContext editContext, ValidationMessageStore messages, in FieldIdentifier fieldIdentifier)
        {
            var fieldValidator = Validator;
            if (fieldIdentifier.Model != editContext.Model)
            {
                var modelType = fieldIdentifier.Model.GetType();
                if (ChildValidators.ContainsKey(modelType))
                {
                    fieldValidator = ChildValidators[modelType];
                }
                else
                {
                    fieldValidator = GetTypedValidator(modelType);
                    ChildValidators[modelType] = fieldValidator;
                }
                if (fieldValidator == null)
                {
                    // Should not error / just fail silently for classes not supposed to be validated.
                    return;
                }
            }

            var vselector = new FluentValidation.Internal.MemberNameValidatorSelector(new[] { fieldIdentifier.FieldName });
            var vctx = new ValidationContext(fieldIdentifier.Model, new FluentValidation.Internal.PropertyChain(), vselector);
            var validationResults = fieldValidator.Validate(vctx);

            messages.Clear(fieldIdentifier);

            foreach (var error in validationResults.Errors)
            {
                messages.Add(fieldIdentifier, error.ErrorMessage);
            }

            editContext.NotifyValidationStateChanged();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    ServiceScope.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.

                // Set large fields to null.
                ServiceScope = null;
                Validator = null;
                ChildValidators = null;

                disposedValue = true;
            }
        }

        // Override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FluentValidator()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // Uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}