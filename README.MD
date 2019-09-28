# FluentValidation.Blazor

> Fluent Validator component for Blazor EditForm :cyclone: :white_check_mark:

[![Build Status](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_apis/build/status/ryanelian.FluentValidation.Blazor?branchName=master)](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_build/latest?definitionId=4&branchName=master) [![NuGet](https://badgen.net/nuget/v/Accelist.FluentValidation.Blazor?icon=nuget)](https://www.nuget.org/packages/Accelist.FluentValidation.Blazor) 

## Install

```
dotnet add package FluentValidation
dotnet add package Accelist.FluentValidation.Blazor
```

## Usage

This is a direct replacement for `<DataAnnotationValidator>` with no additional configuration required for the application code base:

```html
<EditForm Model="Form">


    <FluentValidation.FluentValidator></FluentValidation.FluentValidator>


    <div class="form-group">
        <label for="name">Name</label>
        <InputText id="name" class="form-control" @bind-Value="Form.Name"></InputText>
        <ValidationMessage For="() => Form.Name"></ValidationMessage>
    </div>
    <div class="form-group">
        <label for="email">Email</label>
        <InputText id="email" type="email" class="form-control" @bind-Value="Form.Email"></InputText>
        <ValidationMessage For="() => Form.Email"></ValidationMessage>
    </div>
    <div class="form-group">
        <label for="phone_number">Phone Number</label>
        <InputText id="phone_number" type="tel" class="form-control" @bind-Value="Form.PhoneNumber"></InputText>
        <ValidationMessage For="() => Form.PhoneNumber"></ValidationMessage>
    </div>
    <div class="form-group">
        <label for="password">Password</label>
        <InputText id="password" type="password" class="form-control" @bind-Value="Form.Password"></InputText>
        <ValidationMessage For="() => Form.Password"></ValidationMessage>
    </div>
    <div class="form-group">
        <label for="password_verify">Verify Password</label>
        <InputText id="password_verify" type="password" class="form-control" @bind-Value="Form.VerifyPassword"></InputText>
        <ValidationMessage For="() => Form.VerifyPassword"></ValidationMessage>
    </div>
    <div class="form-group">
        <button type="submit" class="btn btn-primary">
            <i class="fas fa-chevron-up"></i>
            Submit
        </button>
    </div>
</EditForm>
```

This component automatically detect the `Model` data type used by the parent `EditForm` and attempts to acquire the `FluentValidation.IValidator<T>` for the model data type.

For this reason, [in addition to writing the usual `FluentValidation.AbstractValidator<T> Fluent Validation implementation`](https://fluentvalidation.net/start), you are required to register the `FluentValidation.IValidator<T>` implementation in the Dependency Injection Service Provider:

```cs
services.AddTransient<IValidator<CreateAccountFormModel>, CreateAccountFormModelValidator>();
```

**This allows you, dear programmer, to inject required services to your validation implementation for writing amazing custom validation methods!**

## Inline Usage

You may also pass `Validator` parameter directly to the component to inline the `AbstractValidator` implementation instead of relying on Dependency Injection mechanism:

```html
<FluentValidator Validator="FormValidator"></FluentValidator>
```

```cs
@using FluentValidation;
@code {
    class FormModelValidator : AbstractValidator<FormModel> {
        public MyValidator()
        {
            RuleFor(Q => Q.Name).NotEmpty().MaximumLength(255);
        }
    }

    FormModelValidator FormValidator = new FormModelValidator();
}
```

## Asynchronous Warning

Due to buggy UI when using `async void` on `CurrentEditContext.OnValidationRequested` and `CurrentEditContext.OnFieldChanged` event handler (i.e. CSS not updating, to be reported to ASP.NET Core issue repo), the validator is run synchronously.

**This should not stop you from writing asynchronous validation methods because Fluent Validator will force run asynchronous method synchronously when called synchronously.**

*Just something to keep in mind when coding!*

## Repeated Remote Call Warning :warning:

**By default, Fluent Validation does not short circuit the validation chain on first error.**

This may cause problem when a validation hits the database / remote service multiple times in short burst due to validation triggers on field change!

To reduce the impact of repeated custom validation calls, use:

- `.Cascade(CascadeMode.StopOnFirstFailure)` after `RuleFor()` chain,

- Or set `ValidatorOptions.CascadeMode = CascadeMode.StopOnFirstFailure;` on `Program.cs` application entry point.
