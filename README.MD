# FluentValidation.Blazor

> Fluent Validator component for Blazor EditForm :white_check_mark: :arrows_clockwise:

[![Build Status](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_apis/build/status/ryanelian.FluentValidation.Blazor?branchName=master)](https://dev.azure.com/ryanelian/FluentValidation.Blazor/_build/latest?definitionId=4&branchName=master)

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

## Asynchronous Warning

Due to buggy UI when using `async void` on `CurrentEditContext.OnValidationRequested` and `CurrentEditContext.OnFieldChanged` event handler (i.e. CSS not updating, to be reported to ASP.NET Core issue repo), the validator is run synchronously.

**This should not stop you from writing asynchronous validation methods because Fluent Validator will force run asynchronous method synchronously when called synchronously.**

*Just something to keep in mind when coding!*
