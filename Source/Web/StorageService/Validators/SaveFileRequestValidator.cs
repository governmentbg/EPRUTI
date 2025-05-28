namespace StorageService.Validators
{
    using FluentValidation;

    using StorageService.StorageGrpc;

    public class SaveFileRequestValidator : AbstractValidator<SaveFileRequest>
    {
        public SaveFileRequestValidator(IConfiguration configuration)
        {
            this.RuleFor(request => request.Request).SetValidator(new RequestValidator());
            this.RuleFor(request => request.Files).NotNull().NotEmpty();
            this.RuleForEach(request => request.Files).SetValidator(request => new SaveFileValidator(configuration, request.Request));
        }
    }
}
