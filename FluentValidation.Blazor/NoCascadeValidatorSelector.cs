using FluentValidation.Internal;

namespace FluentValidation {
    internal class NoCascadeValidatorSelector : IValidatorSelector {
        public bool CanExecute(IValidationRule rule, string propertyPath, ValidationContext context) {
            return !context.IsChildContext;
        }
    }
}
