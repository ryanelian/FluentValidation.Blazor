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

        public FormModel3 FormModel3 { set; get; }

        public List<FormModel3> SubArray { set; get; }

        public MustNotValidate NotAForm { set; get; }
    }

    public class FormModel3
    {
        public string SubField { set; get; }
    }

    public class MustNotValidate
    {
        public string ShouldNotValidate { set; get; }
    }

    public class FormModel2Validator : AbstractValidator<FormModel2>
    {
        private readonly EmailCheckerService EmailChecker;

        public FormModel2Validator(EmailCheckerService emailCheckerService, IValidator<FormModel3> subValidator)
        {
            this.EmailChecker = emailCheckerService;

            RuleFor(Q => Q.Email).Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().MinimumLength(4).MaximumLength(64).EmailAddress()
                .MustAsync(EmailAvailableAsync).WithMessage(o => $"Email {o.Email} is not available.");

            RuleFor(Q => Q.FormModel3).SetValidator(subValidator);
            RuleForEach(Q => Q.SubArray).SetValidator(subValidator);
        }

        public async Task<bool> EmailAvailableAsync(string email, System.Threading.CancellationToken cancellationToken)
        {
            return await EmailChecker.IsAvailableAsync(email);
        }
    }

    public class FormModel3Validator : AbstractValidator<FormModel3>
    {
        public FormModel3Validator()
        {
            RuleFor(Q => Q.SubField).NotEmpty();
        }
    }
}
