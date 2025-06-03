namespace StorageService.Validators
{
    using FluentValidation;

    using StorageService.StorageGrpc;

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            this.RuleFor(request => request.Ip).NotNull().NotEmpty();
            this.RuleFor(request => request.UserId).NotNull().NotEmpty().Must(userId => Guid.TryParse(userId, out var result));
        }
    }
}
