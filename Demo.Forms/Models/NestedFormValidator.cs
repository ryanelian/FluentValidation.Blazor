using Demo.Forms.Services;
using FluentValidation;
using System.Threading.Tasks;

namespace Demo.Forms.Models
{
    public class NestedFormValidator : AbstractValidator<NestedForm>
    {
        private readonly EmailCheckerService EmailChecker;

        public NestedFormValidator(EmailCheckerService emailCheckerService, IValidator<ChildForm> subValidator)
        {
            this.EmailChecker = emailCheckerService;

            RuleFor(Q => Q.Email)
                .NotEmpty().MinimumLength(4).MaximumLength(64).EmailAddress()
                .MustAsync(EmailAvailableAsync).WithMessage(o => $"Email {o.Email} is not available.");

            RuleFor(Q => Q.Child).SetValidator(subValidator);
            RuleForEach(Q => Q.SubArray).SetValidator(subValidator);
        }

        public async Task<bool> EmailAvailableAsync(string email, System.Threading.CancellationToken cancellationToken)
        {
            return await EmailChecker.IsAvailableAsync(email);
        }
    }

    public class FormModel3Validator : AbstractValidator<ChildForm>
    {
        public FormModel3Validator()
        {
            RuleFor(Q => Q.SubField).NotEmpty();
        }
    }
}
