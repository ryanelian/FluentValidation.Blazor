using Demo.Services;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.Models
{
    public class FormModel2
    {
        public string Email { set; get; }
    }

    public class FormModel2Validator : AbstractValidator<FormModel2>
    {
        private readonly EmailCheckerService EmailChecker;

        public FormModel2Validator(EmailCheckerService emailCheckerService)
        {
            this.EmailChecker = emailCheckerService;

            RuleFor(Q => Q.Email).Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().MinimumLength(4).MaximumLength(64).EmailAddress()
                .MustAsync(EmailAvailableAsync).WithMessage(o => $"Email {o.Email} is not available.");
        }

        public async Task<bool> EmailAvailableAsync(string email, System.Threading.CancellationToken cancellationToken)
        {
            return await EmailChecker.IsAvailableAsync(email);
        }
    }
}
