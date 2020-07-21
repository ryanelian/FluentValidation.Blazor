using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Forms.Models
{
    public class NestedForm
    {
        public string Email { set; get; }

        public ChildForm Child { set; get; }

        public List<ChildForm> SubArray { set; get; }

        public MustNotValidate NotAForm { set; get; }
    }

    public class ChildForm
    {
        public string SubField { set; get; }
    }

    public class MustNotValidate
    {
        public string ShouldNotValidate { set; get; }
    }
}
