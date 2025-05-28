namespace Integration.Api.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    using Ais.Data.Models.Base;
    using Ais.Data.Models.Client;
    using Ais.Data.Models.User;
    using Ais.Services.Mapping;
    using Ais.Utilities.Attributes;
    using AutoMapper;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class ClientUpsertModel.
    /// Implements the <see cref="Guid" />
    /// Implements the <see cref="IHaveCustomMappings" />
    /// </summary>
    /// <seealso cref="Guid" />
    /// <seealso cref="IHaveCustomMappings" />
    public class ClientUpsertModel : DbModel<Guid?>, IHaveCustomMappings, IValidatableObject
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [CustomDisplay("ApplicantType")]
        public global::Ais.Data.Models.Nomenclature.Nomenclature Type { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>The name of the user.</value>
        [CustomDisplay("UserName")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the first names.
        /// </summary>
        /// <value>The first names.</value>
        [CustomDisplay("FirstName")]
        public List<string> FirstNames { get; set; }

        /// <summary>
        /// Gets or sets the sur names.
        /// </summary>
        /// <value>The sur names.</value>
        [CustomDisplay("SurName")]
        public List<string> SurNames { get; set; }

        /// <summary>
        /// Gets or sets the family names.
        /// </summary>
        /// <value>The family names.</value>
        [CustomDisplay("FamilyName")]
        public List<string> FamilyNames { get; set; }

        /// <summary>
        /// Gets or sets the alias.
        /// </summary>
        /// <value>The alias.</value>
        [CustomDisplay("Alias")]
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the first names latin.
        /// </summary>
        /// <value>The first names latin.</value>
        [CustomDisplay("FirstNameLatin")]
        public List<string> FirstNamesLatin { get; set; }

        /// <summary>
        /// Gets or sets the sur names latin.
        /// </summary>
        /// <value>The sur names latin.</value>
        [CustomDisplay("SurNameLatin")]
        public List<string> SurNamesLatin { get; set; }

        /// <summary>
        /// Gets or sets the family names latin.
        /// </summary>
        /// <value>The family names latin.</value>
        [CustomDisplay("FamilyNameLatin")]
        public List<string> FamilyNamesLatin { get; set; }

        /// <summary>
        /// Gets or sets the alias latin.
        /// </summary>
        /// <value>The alias latin.</value>
        [CustomDisplay("AliasLatin")]
        public string AliasLatin { get; set; }

        /// <summary>
        /// Gets or sets the addresses.
        /// </summary>
        /// <value>The addresses.</value>
        [CustomDisplay("Address")]
        public List<Ais.Data.Models.Address.Address> Addresses { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is LNCH.
        /// </summary>
        /// <value><c>true</c> if this instance is LNCH; otherwise, <c>false</c>.</value>
        [CustomDisplay("IsLnch")]
        public bool IsLnch { get; set; }

        /// <summary>
        /// Gets or sets the egn bulstat.
        /// </summary>
        /// <value>The egn bulstat.</value>
        [CustomDisplay("EgnBulstat")]
        public string EgnBulstat { get; set; }

        /// <summary>
        /// Gets or sets the home country.
        /// </summary>
        /// <value>The home country.</value>
        [CustomDisplay("HomeCountry")]
        public global::Ais.Data.Models.Nomenclature.Nomenclature HomeCountry { get; set; }

        /// <summary>
        /// Gets or sets the place abroad.
        /// </summary>
        /// <value>The place abroad.</value>
        [CustomDisplay("PlaceAbroad")]
        public string PlaceAbroad { get; set; }

        /// <summary>
        /// Gets or sets the date of birth.
        /// </summary>
        /// <value>The date of birth.</value>
        [CustomDisplay("DateOfBirth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Gets or sets the type of the register.
        /// </summary>
        /// <value>The type of the register.</value>
        [CustomDisplay("RegisterType")]
        public global::Ais.Data.Models.Nomenclature.Nomenclature RegisterType { get; set; }

        /// <summary>
        /// Gets or sets the register identifier.
        /// </summary>
        /// <value>The register identifier.</value>
        [CustomDisplay("RegisterIdentifier")]
        public string RegisterIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the register number.
        /// </summary>
        /// <value>The register number.</value>
        [CustomDisplay("RegisterNumber")]
        public string RegisterNumber { get; set; }

        /// <summary>
        /// Gets or sets the register addition data.
        /// </summary>
        /// <value>The register addition data.</value>
        [CustomDisplay("RegisterAdditionData")]
        public string RegisterAdditionData { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is self registered.
        /// </summary>
        /// <value><c>true</c> if this instance is self registered; otherwise, <c>false</c>.</value>
        public bool IsSelfRegistered { get; set; }

        /// <summary>
        /// Gets a value indicating whether [accept general terms].
        /// </summary>
        /// <value><c>true</c> if [accept general terms]; otherwise, <c>false</c>.</value>
        public bool AcceptGeneralTerms => true;

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <value>The full name.</value>
        [CustomDisplay("FullName")]
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>The email.</value>
        [CustomDisplay("Email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the identifier number.
        /// </summary>
        /// <value>The identifier number.</value>
        [CustomDisplay("IdNumber")]
        public string IdNumber { get; set; }

        /// <summary>
        /// Gets or sets the type of the identifier.
        /// </summary>
        /// <value>The type of the identifier.</value>
        [CustomDisplay("IdType")]
        public string IdType { get; set; }

        /// <summary>
        /// Gets or sets the representatives.
        /// </summary>
        /// <value>The representatives.</value>
        public List<Agent> Representatives { get; set; }

        [CustomDisplay("DenialOfElectronicServices")]
        public bool DenialOfElectronicServices { get; set; }

        public bool CanRequestGCCAServices { get; set; }

        [CustomDisplay("SendChangePasswordEmail")]
        public bool SendChangePasswordEmail { get; set; }

        [CustomDisplay("WithoutEgnBulstat")]
        public bool WithoutEgnBulstat { get; set; }

        [CustomDisplay("Division")]
        public bool Division { get; set; }

        [CustomDisplay("IsDead")]
        public bool IsDead { get; set; }

        [CustomDisplay("DateOfDeath")]
        [DataType(DataType.Date)]
        public DateTime? DateOfDeath { get; set; }

        /// <summary>
        /// Creates the mappings.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public void CreateMappings(IProfileExpression configuration)
        {
            configuration.CreateMap<ClientUpsertModel, Agent>()
                         .ForMember(
                             d => d.User,
                             s => s.MapFrom(
                                 m => new ClientUser
                                 {
                                     UserName = m.UserName,
                                     Email = m.Email
                                 }));

            configuration.CreateMap<ClientUpsertModel, Client>()
                         .ForMember(
                             d => d.User,
                             s => s.MapFrom(
                                 m => new ClientUser
                                 {
                                     Id = m.UserId,
                                     UserName = m.UserName,
                                     Email = m.Email
                                 }));

            configuration.CreateMap<Client, ClientUpsertModel>()
                         .ForMember(d => d.UserId, s => s.MapFrom(m => m.User != null ? m.User.Id : null))
                         .ForMember(d => d.UserName, s => s.MapFrom(m => m.User != null ? m.User.UserName : null))
                         .ForMember(d => d.Email, s => s.MapFrom(m => m.User != null ? m.User.Email : null));
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var localizer = validationContext.GetRequiredService<IStringLocalizer>();
            ////if (!this.DenialOfElectronicServices && string.IsNullOrWhiteSpace(this.Email))
            ////{
            ////    yield return new ValidationResult(string.Format(localizer["Required"], localizer["Email"]), new[] { "Email" });
            ////}

            ////if (!this.DenialOfElectronicServices && string.IsNullOrWhiteSpace(this.UserName))
            ////{
            ////    yield return new ValidationResult(string.Format(localizer["Required"], localizer["UserName"]), new[] { "UserName" });
            ////}

            if (this.Representatives?.Any(x => x.Quality == null || x.Quality?.Type?.Id == default || x.Quality?.Type?.Id == default(Guid)) == true)
            {
                yield return new ValidationResult(localizer["PleaseAddQualityToAllRepresentatives"], new[] { "Representatives" });
            }

            var defaultAddressCount = this.Addresses?.Count(address => address.Default);
            if (defaultAddressCount > 1)
            {
                yield return new ValidationResult(localizer["MoreThanOneDefaultAddressErrorMessage"], null);
            }

            if (this.Type?.Id.HasValue != true)
            {
                yield return new ValidationResult(string.Format(localizer["Required"], localizer["Type"]), new[] { "Type" });
            }
            ////else
            ////{
            ////    var type = EnumHelper.GetClientTypeById(this.Type.Id.Value);
            ////    switch (type)
            ////    {
            ////        case ClientType.Physical:
            ////        case ClientType.PhysicalWithBulstat:

            ////            if (this.EgnBulstat.IsNullOrEmpty() && !this.WithoutEgnBulstat)
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["EgnBulstat"]), new[] { "EgnBulstat" });
            ////            }

            ////            if (this.FirstNames.IsNullOrEmpty())
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["FirstName"]), new[] { "FirstNames" });
            ////            }

            ////            if (this.FamilyNames.IsNullOrEmpty())
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["FamilyName"]), new[] { "FamilyNames" });
            ////            }

            ////            break;
            ////        case ClientType.Legal:

            ////            if (this.EgnBulstat.IsNullOrEmpty() && !this.WithoutEgnBulstat)
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["EgnBulstat"]), new[] { "EgnBulstat" });
            ////            }

            ////            if (this.FullName.IsNullOrEmpty())
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["FullName"]), new[] { "FullName" });
            ////            }

            ////            break;
            ////        case ClientType.ForeignPhysical:
            ////            if (this.FirstNamesLatin.IsNullOrEmpty())
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["FirstNameLatin"]), new[] { "FirstNamesLatin" });
            ////            }

            ////            if (this.FamilyNamesLatin.IsNullOrEmpty())
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["FamilyNameLatin"]), new[] { "FamilyNamesLatin" });
            ////            }

            ////            if (this.HomeCountry?.Id.HasValue != true)
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["HomeCountry"]), new[] { "HomeCountry" });
            ////            }

            ////            if (this.DateOfBirth.HasValue != true)
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["DateOfBirth"]), new[] { "DateOfBirth" });
            ////            }

            ////            break;
            ////        case ClientType.ForeignLegal:
            ////            if (this.FullName.IsNullOrEmpty())
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["FullName"]), new[] { "FullName" });
            ////            }

            ////            if (this.HomeCountry?.Id.HasValue != true)
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["HomeCountry"]), new[] { "HomeCountry" });
            ////            }

            ////            if (this.PlaceAbroad.IsNullOrEmpty())
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["PlaceAbroad"]), new[] { "PlaceAbroad" });
            ////            }

            ////            if (this.RegisterType?.Id.HasValue != true)
            ////            {
            ////                yield return new ValidationResult(string.Format(localizer["Required"], localizer["RegisterType"]), new[] { nameof(this.RegisterType) });
            ////            }
            ////            else if (this.RegisterType.Id == EnumHelper.GetRegisterTypeIdByType(ForeignLegalRegisterType.NameAndNumber))
            ////            {
            ////                if (this.RegisterNumber.IsNullOrEmpty())
            ////                {
            ////                    yield return new ValidationResult(string.Format(localizer["Required"], localizer["RegisterNumber"]), new[] { "RegisterNumber" });
            ////                }
            ////            }

            ////            break;
            ////    }
            ////}
        }
    }
}
