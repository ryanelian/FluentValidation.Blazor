# FluentValidation.Blazor

<a href="https://github.com/ryanelian/FluentValidation.Blazor/blob/master/README.MD">
    <img align="right" width="128" height="128" src="https://raw.githubusercontent.com/ryanelian/FluentValidation.Blazor/master/FluentValidation.Blazor/img/icon.png" />
</a>

> [Fluent Validation](https://github.com/JeremySkinner/FluentValidation)-powered Blazor component for validating standard \<EditForm> :cyclone: :white_check_mark:

[![Build Status](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_apis/build/status/ryanelian.FluentValidation.Blazor?branchName=master)](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_build/latest?definitionId=4&branchName=master) [![NuGet](https://badgen.net/nuget/v/Accelist.FluentValidation.Blazor?icon=nuget)](https://www.nuget.org/packages/Accelist.FluentValidation.Blazor) [![Accelist.FluentValidation.Blazor package in test feed in Azure Artifacts](https://feeds.dev.azure.com/ryanelian/e48e1156-bc00-4459-a15d-e8aac1c98eaa/_apis/public/Packaging/Feeds/fc2537b4-7482-4b4a-a6e1-46dab8061c18/Packages/0796d05d-fde5-430c-91b2-33b6d8c7d3c5/Badge)](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_packaging?_a=package&feed=fc2537b4-7482-4b4a-a6e1-46dab8061c18&package=0796d05d-fde5-430c-91b2-33b6d8c7d3c5&preferRelease=true)

## Install

```
dotnet add package FluentValidation
dotnet add package Accelist.FluentValidation.Blazor
```

## Getting Started

This library is a direct replacement to [the default Blazor `<DataAnnotationValidator>`](https://docs.microsoft.com/en-us/aspnet/core/blazor/forms-validation?view=aspnetcore-3.0) with **zero configuration required** :zap: in the application code base:

```html
<EditForm Model="Form">


    <FluentValidation.FluentValidator></FluentValidation.FluentValidator>


    <div class="form-group">
        <label for="email">Email</label>
        <InputText id="email" type="email" class="form-control" @bind-Value="Form.Email"></InputText>
        <ValidationMessage For="() => Form.Email"></ValidationMessage>
    </div>
    <div class="form-group">
        <button type="submit" class="btn btn-primary">
            <i class="fas fa-chevron-up"></i>
            Submit
        </button>
    </div>
</EditForm>
```

```cs
@code {
    FormModel Form = new FormModel();
}
```

```cs
public class FormModel
{
    public string Email { set; get; }
}

public class FormModelValidator : AbstractValidator<FormModel>
{
    public FormModelValidator()
    {
        RuleFor(Q => Q.Email).NotEmpty().EmailAddress().MaximumLength(255);
    }
}
```

The `<FluentValidation.FluentValidator>` component automatically detects the `Model` data type used by the parent `<EditForm>` then attempts to acquire the corresponding `FluentValidation.IValidator<T>` for that model data type.

For this reason, [in addition to coding the usual `FluentValidation.AbstractValidator<T> Fluent Validation implementation`](https://fluentvalidation.net/start), **you are required to register the `FluentValidation.IValidator<T>` implementation in the `Startup.cs` Service Provider (Dependency Injection)**:

```cs
services.AddTransient<IValidator<CreateAccountFormModel>, CreateAccountFormModelValidator>();
```

**This effectively allows you, dear programmer, to inject required services to your validation implementations for writing amazing custom validation methods!** :fire:

```cs
public class FormModelValidator : AbstractValidator<FormModel>
{
    readonly AppDbContext DB;
    readonly IServiceProvider SP;

    public FormModelValidator(AppDbContext db, IServiceProvider sp)
    {
        this.DB = db;
        this.SP = sp;

        RuleFor(Q => Q.Email).NotEmpty().EmailAddress().MaximumLength(255)
            .Must(BeUniqueEmail).WithMessage("Email address is already registered.");
    }

    bool BeUniqueEmail(string email)
    {
        var exist = DB.Account.Where(Q => Q.Email == email).Any();
        return (exist == false);
    }
}
```

## Inlined Validator

 `Validator` parameter may also be passed directly to the component to inline the `AbstractValidator` implementation instead of relying on DI mechanism:

```html
<FluentValidator Validator="Validator"></FluentValidator>
```

```cs
@code {
    FormModelValidator Validator = new FormModelValidator();
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

## FluentValidation.DependencyInjectionExtensions

```
dotnet add package FluentValidation.DependencyInjectionExtensions
```

```cs
services.AddValidatorsFromAssemblyContaining<Program>();
```

**Can be used to auto-populate all validators from current application / other project automatically!**

However, [`InjectValidator`](https://github.com/JeremySkinner/FluentValidation/blob/24aaddb4b00e3563e8d03fd6edd06f487e207e8a/src/FluentValidation.DependencyInjectionExtensions/DependencyInjectionExtensions.cs#L76-L111) extension method cannot be used because: (quoting @JeremySkinner comment)

> [Making use of InjectValidator or GetServiceProvider is only supported when using the automatic MVC integration.](https://github.com/JeremySkinner/FluentValidation/blob/24aaddb4b00e3563e8d03fd6edd06f487e207e8a/src/FluentValidation.DependencyInjectionExtensions/DependencyInjectionExtensions.cs#L64)

That said, the built-in `SetValidator` method can still be used with an injected `IValidator<T>` object instead:

```cs
public class FormModelValidator : AbstractValidator<FormModel>
{
    public FormModelValidator(IValidator<SomeProperty> somePropertyValidator)
    {
        RuleFor(Q => Q.SomeProperty).SetValidator(somePropertyValidator);
    }
}
```

## Why Not Just Use `<DataAnnotationValidator>`?

1. `<DataAnnotationValidator>` cannot use DI services at the moment...

2. ... but [`<DataAnnotationValidator>` also cannot do `AddModelError()`](https://github.com/aspnet/AspNetCore/issues/14524) like Razor Pages. Which rules out validation AFTER form submission!

3. `[ValidationAttribute]` top-level `IsValid()` method cannot be async!
