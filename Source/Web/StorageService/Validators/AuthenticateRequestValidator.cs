namespace StorageService.Validators
{
    using FluentValidation;

    using StorageService.AuthenticationGrpc;

    public class AuthenticateRequestValidator : AbstractValidator<SignInRequest>
    {
        public AuthenticateRequestValidator()
        {
            this.RuleFor(request => request.Username).NotNull().NotEmpty();
            this.RuleFor(request => request.Password).NotNull().NotEmpty();
        }
    }
}
