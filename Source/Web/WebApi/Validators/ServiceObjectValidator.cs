namespace WebApi.Validators
{
    using FluentValidation;

    using global::Ais.Data.Models.Service.Object;

    public class ServiceObjectValidator : AbstractValidator<ServiceObject>
    {
        public ServiceObjectValidator()
        {
            this.RuleFor(file => file.Type).NotNull();
            this.RuleFor(file => file.Id).NotNull().NotEmpty().MaximumLength(100);
            this.RuleFor(file => file.Number).MaximumLength(256);
            this.RuleFor(file => file.Title).MaximumLength(500);
            this.RuleFor(file => file.ShortDescription).MaximumLength(1000);
            this.RuleFor(file => file.Description).MaximumLength(1000);
        }
    }
}
