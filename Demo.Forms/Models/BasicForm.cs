using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Forms.Models
{
    public class BasicForm
    {
        public string Name { set; get; }
    }

    public class BasicFormValidator : AbstractValidator<BasicForm>
    {
        public BasicFormValidator()
        {
            RuleFor(Q => Q.Name).NotEmpty().MinimumLength(2).MaximumLength(8);
        }
    }
}
