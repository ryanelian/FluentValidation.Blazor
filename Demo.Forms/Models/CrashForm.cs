using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.Forms.Models
{
    public class CrashForm
    {
        public string Name { set; get; }
    }

    public class CrashFormModelValidator : AbstractValidator<CrashForm>
    {
        public CrashFormModelValidator()
        {
            RuleFor(Q => Q.Name).Must(Crash);
        }

        public bool Crash(string value)
        {
            throw new InvalidOperationException("https://www.youtube.com/watch?v=843Q4wK5Hs8");
        }
    }
}
