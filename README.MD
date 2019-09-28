# FluentValidation.Blazor

<a href="https://github.com/ryanelian/FluentValidation.Blazor/blob/master/README.MD">
    <img align="right" width="128" height="128" src="https://raw.githubusercontent.com/ryanelian/FluentValidation.Blazor/master/FluentValidation.Blazor/img/icon.png" />
</a>

> [Fluent Validation](https://github.com/JeremySkinner/FluentValidation)-powered Blazor component for validating standard EditForm :cyclone: :white_check_mark:

[![Build Status](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_apis/build/status/ryanelian.FluentValidation.Blazor?branchName=master)](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_build/latest?definitionId=4&branchName=master) [![NuGet](https://badgen.net/nuget/v/Accelist.FluentValidation.Blazor?icon=nuget)](https://www.nuget.org/packages/Accelist.FluentValidation.Blazor) [![Accelist.FluentValidation.Blazor package in test feed in Azure Artifacts](https://feeds.dev.azure.com/ryanelian/e48e1156-bc00-4459-a15d-e8aac1c98eaa/_apis/public/Packaging/Feeds/fc2537b4-7482-4b4a-a6e1-46dab8061c18/Packages/0796d05d-fde5-430c-91b2-33b6d8c7d3c5/Badge)](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_packaging?_a=package&feed=fc2537b4-7482-4b4a-a6e1-46dab8061c18&package=0796d05d-fde5-430c-91b2-33b6d8c7d3c5&preferRelease=true)

## Install

```
dotnet add package FluentValidation
dotnet add package Accelist.FluentValidation.Blazor
```

## Usage

This library is a direct replacement to [the default Blazor `<DataAnnotationValidator>`](https://docs.microsoft.com/en-us/aspnet/core/blazor/forms-validation?view=aspnetcore-3.0) with **zero configuration required** :zap: in the application code base:

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

The `<FluentValidation.FluentValidator>` component automatically detects the `Model` data type used by the parent `<EditForm>` then attempts to acquire the corresponding `FluentValidation.IValidator<T>` for that model data type.

For this reason, [in addition to coding the usual `FluentValidation.AbstractValidator<T> Fluent Validation implementation`](https://fluentvalidation.net/start), **you are required to register the `FluentValidation.IValidator<T>` implementation in the Service Provider (Dependency Injection)**:

```cs
services.AddTransient<IValidator<CreateAccountFormModel>, CreateAccountFormModelValidator>();
```

**This effectively allows you, dear programmer, to inject required services to your validation implementations for writing amazing custom validation methods!** :fire:

## Inline Usage

 `Validator` parameter may also be passed directly to the component to inline the `AbstractValidator` implementation instead of relying on DI mechanism:

```html
<FluentValidator Validator="FormValidator"></FluentValidator>
```

```cs
@using FluentValidation
@code {
    class FormModelValidator : AbstractValidator<FormModel> 
    {
        public MyValidator()
        {
            RuleFor(Q => Q.Name).NotEmpty().MaximumLength(255);
        }
    }

    FormModelValidator FormValidator = new FormModelValidator();
}
```

## Asynchronous Warning

Due to buggy UI when using `async void` on `EditContext.OnValidationRequested` and `EditContext.OnFieldChanged` event handler (i.e. CSS not updating, to be reported to ASP.NET Core issue repo), **the validator is run synchronously**.

**This should not stop you from writing async validation methods because Fluent Validation will force run async methods synchronously.**

*Just something to keep in mind when coding!*

## Repeated Remote Call Warning :warning:

**By default, Fluent Validation does NOT short-circuit the validation chain on first error.**

This may cause performance issues when a validation logic hits the database / remote service multiple times in short burst due to validation triggers on field change!

To reduce the impact of repeated custom validation calls, use:

- `.Cascade(CascadeMode.StopOnFirstFailure)` after `RuleFor()` chain,

- Or set `ValidatorOptions.CascadeMode = CascadeMode.StopOnFirstFailure;` on `Program.cs` application entry point.

## Why Not Just Use `<DataAnnotationValidator>`?

1. `<DataAnnotationValidator>` cannot use DI services at the moment...

2. ... but [`<DataAnnotationValidator>` also cannot do `AddModelError()`](https://github.com/aspnet/AspNetCore/issues/14524) like Razor Pages. Which rules out validation AFTER form submission!

3. `[ValidationAttribute]` top-level `IsValid()` method cannot be async!
