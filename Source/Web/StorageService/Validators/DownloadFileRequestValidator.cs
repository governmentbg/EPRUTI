namespace StorageService.Validators
{
    using System.Text.RegularExpressions;

    using FluentValidation;

    using StorageService.StorageGrpc;

    public class DownloadFileRequestValidator : AbstractValidator<DownloadFileRequest>
    {
        public DownloadFileRequestValidator(IConfiguration configuration)
        {
            this.RuleFor(request => request.Request).SetValidator(new RequestValidator());
            this.RuleFor(request => request.Ids).NotNull().NotEmpty().When(request => request.IdentifierCase == DownloadFileRequest.IdentifierOneofCase.Ids);
            this.RuleForEach(request => request.Ids.Id).Must(id => Guid.TryParse(id, out var result)).When(request => request.IdentifierCase == DownloadFileRequest.IdentifierOneofCase.Ids);
            this.RuleFor(request => request.Paths).NotNull().NotEmpty().When(request => request.IdentifierCase == DownloadFileRequest.IdentifierOneofCase.Paths);
            this.RuleForEach(request => request.Paths.Path)
                .Must(item => Path.GetDirectoryName(item)?.Contains("..") != true)
                .Matches(request => new Regex(string.Format(configuration.GetValue<string>("TempPathRegex"), configuration.GetValue<string>("TempDirectory"), request.Request.UserId), RegexOptions.IgnoreCase))
                .When(request => request.IdentifierCase == DownloadFileRequest.IdentifierOneofCase.Paths);
        }
    }
}
