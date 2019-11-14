﻿using Microsoft.AspNetCore.Components;
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

        [Parameter]
        public List<object> ChildModels { get; set; }

        [Parameter]
        public List<IValidator> ChildModelValidators { get; set; }

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

            this.GetValidators();

            this.AddValidation();
        }

        /// <summary>
        /// Try acquiring the form validator implementation from the DI.
        /// </summary>
        private void GetValidators()
        {
            if (this.Validator == null)
            {
                Validator = this.GetValidator(CurrentEditContext.Model.GetType());
            }

            if (ChildModels != null && ChildModelValidators == null)
            {
                ChildModelValidators = new List<IValidator>();

                foreach (var childModel in ChildModels)
                {
                    ChildModelValidators.Add(this.GetValidator(childModel.GetType()));
                }
            }
        }

        private IValidator GetValidator(Type formType)
        {
            var validatorType = typeof(IValidator<>);
            var formValidatorType = validatorType.MakeGenericType(formType);

            var validator = ServiceProvider.GetService(formValidatorType) as IValidator;

            if (validator == null)
            {
                throw new InvalidOperationException($"FluentValidation.IValidator<{formType.FullName}> is"
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
                var fieldID = editContext.Field(error.PropertyName);
                messages.Add(fieldID, error.ErrorMessage);
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
            var vselector = new FluentValidation.Internal.MemberNameValidatorSelector(new[] { fieldIdentifier.FieldName });

            var validatedModel= editContext.Model;
            var validator = Validator;

            if (ChildModelValidators != null) {
                foreach (IValidator childModelValidator in ChildModelValidators) {
                    if (childModelValidator.CanValidateInstancesOfType(fieldIdentifier.Model.GetType())) {
                        validatedModel = fieldIdentifier.Model;
                        validator = childModelValidator;
                        break;
                    }
                }
            }

            var vctx = new ValidationContext(validatedModel, new FluentValidation.Internal.PropertyChain(), vselector);
            var validationResults = validator.Validate(vctx);

            messages.Clear(fieldIdentifier);

            foreach (var error in validationResults.Errors)
            {
                var fieldID = new FieldIdentifier(validatedModel, error.PropertyName);
                messages.Add(fieldID, error.ErrorMessage);
            }

            editContext.NotifyValidationStateChanged();
        }
    }
}