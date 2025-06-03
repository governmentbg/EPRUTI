namespace StorageService.Validators
{
    using System.Text.RegularExpressions;

    using FluentValidation;

    using StorageService.StorageGrpc;

    public class SaveFileValidator : AbstractValidator<SaveFile>
    {
        public SaveFileValidator(IConfiguration configuration, Request request)
        {
            this.RuleFor(file => file.Id).NotNull().NotEmpty().Must(id => Guid.TryParse(id, out _));
            this.RuleFor(file => file.ObjectId).NotNull().NotEmpty();
            this.RuleFor(file => file.ObjectType).NotNull().NotEmpty();
            this.RuleFor(file => file.Name).NotNull().NotEmpty();
            this.RuleFor(file => file.Path)
                .NotNull()
                .NotEmpty()
                .Must(item => Path.GetDirectoryName(item)?.Contains("..") != true)
                .Matches(_ => new Regex(string.Format(configuration.GetValue<string>("TempPathRegex"), configuration.GetValue<string>("TempDirectory"), request.UserId), RegexOptions.IgnoreCase));
        }
    }
}
