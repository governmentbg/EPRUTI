namespace StorageService.Validators
{
    using FluentValidation;

    using StorageService.StorageGrpc;

    public class FileValidator : AbstractValidator<File>
    {
        public FileValidator()
        {
            this.RuleFor(file => file.Name).NotEmpty().NotEmpty();
            this.RuleFor(file => file.Length).GreaterThan(0);
            this.RuleFor(file => file.Content).NotNull().NotEmpty();
            this.RuleFor(file => file.Content.Length).GreaterThan(0);
            this.RuleFor(file => file.Chunk.FileUniqueId).NotNull().NotEmpty().When(file => file.Chunk != null);
            this.RuleFor(file => file.Chunk.Index).GreaterThanOrEqualTo(0).LessThan(file => file.Chunk.Total).When(file => file.Chunk != null);
            this.RuleFor(file => file.Chunk.Total).GreaterThan(file => file.Chunk.Index).When(file => file.Chunk != null);
            this.RuleFor(file => file.Chunk.Length).GreaterThan(0).When(file => file.Chunk != null);
        }
    }
}
