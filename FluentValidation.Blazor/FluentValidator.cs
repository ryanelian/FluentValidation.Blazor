using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentValidation
{
    /// <summary>
    /// Add Fluent Validator support to an EditContext.
    /// </summary>
    public class FluentValidator : ComponentBase
    {
        [CascadingParameter]
        EditContext CurrentEditContext { get; set; }

        [Inject]
        IServiceProvider ServiceProvider { get; set; }

        IValidator Validator { set; get; }

        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(DataAnnotationsValidator)} requires a cascading " +
                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(DataAnnotationsValidator)} " +
                    $"inside an EditForm.");
            }

            this.GetValidator();
            this.AddValidation();
        }

        private void GetValidator()
        {
            var validatorType = typeof(IValidator<>);
            var formType = CurrentEditContext.Model.GetType();
            var formValidatorType = validatorType.MakeGenericType(formType);
            this.Validator = ServiceProvider.GetService(formValidatorType) as IValidator;

            if (this.Validator == null)
            {
                throw new InvalidOperationException($"FluentValidation.IValidator<{formType.FullName}> is"
                    + " not registered in the application service provider.");
            }
        }

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

        private void ValidateModel(EditContext editContext, ValidationMessageStore messages)
        {
            // ATTENTION: DO NOT USE Async Void + ValidateAsync
            // Explanation: Blazor UI will get VERY BUGGY for some reason if you do that. (Field CSS lagged behind validation)
            var validationResults = Validator.Validate(editContext.Model);

            messages.Clear();

            foreach (var error in validationResults.Errors)
            {
                var fieldID = editContext.Field(error.PropertyName);
                messages.Add(fieldID, error.ErrorMessage);
            }

            editContext.NotifyValidationStateChanged();
        }

        private void ValidateField(EditContext editContext, ValidationMessageStore messages, /*in*/ FieldIdentifier fieldIdentifier)
        {
            var vselector = new FluentValidation.Internal.MemberNameValidatorSelector(new[] { fieldIdentifier.FieldName });
            var vctx = new ValidationContext(editContext.Model, new FluentValidation.Internal.PropertyChain(), vselector);
            var validationResults = Validator.Validate(vctx);

            messages.Clear(fieldIdentifier);

            foreach (var error in validationResults.Errors)
            {
                var fieldID = editContext.Field(error.PropertyName);
                messages.Add(fieldID, error.ErrorMessage);
            }

            editContext.NotifyValidationStateChanged();
        }
    }
}