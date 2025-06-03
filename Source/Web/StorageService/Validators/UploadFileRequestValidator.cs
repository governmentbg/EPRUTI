namespace StorageService.Validators
{
    using FluentValidation;

    using StorageService.StorageGrpc;

    public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
    {
        public UploadFileRequestValidator()
        {
            this.RuleFor(request => request.File).SetValidator(new FileValidator());
            this.RuleFor(request => request.Request).SetValidator(new RequestValidator());
        }
    }
}
