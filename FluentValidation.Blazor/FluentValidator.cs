using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FluentValidation
{
    /// <summary>
    /// Add Fluent Validator support to an EditContext.
    /// </summary>
    public class FluentValidator : ComponentBase
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
            IValidator validator = ServiceProvider.GetService(formValidatorType) as IValidator;
            if (validator == null)
            {
                throw new InvalidOperationException($"FluentValidation.IValidator<{modelType.FullName}> is"
                    + " not registered in the application service provider.");
            }

            return validator;
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
            // ATTENTION: DO NOT USE Async Void + ValidateAsync
            // Explanation: Blazor UI will get VERY BUGGY for some reason if you do that. (Field CSS lagged behind validation)
            var validationResults = Validator.Validate(editContext.Model);

            messages.Clear();

            foreach (var error in validationResults.Errors)
            {
                var model = editContext.Model;
                var fieldName = error.PropertyName;

                // FluentValidation Error PropertyName can be something like "ObjectA.ObjectB.PropertyX"
                // However, Blazor does NOT recognize nested FieldIdentifier.
                // Instead, the FieldIdentifier is assigned to the object in question. (Model + Property Name)
                // Therefore, we need to traverse the object graph to acquire them!
                if (fieldName.Contains("."))
                {
                    var objectParts = fieldName.Split('.');
                    fieldName = objectParts[objectParts.Length - 1];
                    for (var i = 0; i < objectParts.Length - 1; i++)
                    {
                        var propertyName = objectParts[i];
                        int? arrayIndex = null;
                        if (propertyName.Contains("[") && propertyName.Contains("]"))
                        {
                            var indexedPropertyName = propertyName.Split('[', ']');
                            propertyName = indexedPropertyName[0];
                            arrayIndex = int.Parse(indexedPropertyName[1]);
                        }

                        model = model?.GetType().GetProperty(propertyName)?.GetValue(model);
                        if (arrayIndex != null && model is IList array)
                        {
                            // System.Array implements IList https://docs.microsoft.com/en-us/dotnet/api/system.array?view=netcore-3.0
                            model = array[arrayIndex.Value];
                        }

                        if (model == null)
                        {
                            break;
                        }
                    }
                }

                if (model != null)
                {
                    var fieldID = new FieldIdentifier(model, fieldName);
                    messages.Add(fieldID, error.ErrorMessage);
                }
            }

            editContext.NotifyValidationStateChanged();
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
                if (ChildValidators.ContainsKey(modelType) == false)
                {
                    ChildValidators[modelType] = GetTypedValidator(modelType);
                }
                fieldValidator = ChildValidators[modelType];
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
    }
}