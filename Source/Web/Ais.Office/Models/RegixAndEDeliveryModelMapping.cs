namespace Ais.Office.Models
{
    using Ais.Data.Models.Address;
    using Ais.Data.Models.Client;
    using Ais.Data.Models.EDelivery;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Nomenclature;
    using Ais.Office.ViewModels.Clients;
    using Ais.Regix.Net.Core;
    using Ais.Services.Mapping;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Extensions;
    using AutoMapper;

    using EdeliveryService;

    using Address = Ais.Data.Models.Address.Address;

    /// <summary>
    /// Class RegixModelMapping.
    /// Implements the <see cref="IHaveCustomMappings" />
    /// </summary>
    /// <seealso cref="IHaveCustomMappings" />
    public class RegixAndEDeliveryModelMapping : IHaveCustomMappings
    {
        private readonly string[] managerIds = new[] { "7", "10", "18" };

        private enum NameTypes
        {
            First,
            Sur,
            Family,
        }

        /// <summary>
        /// Creates the mappings.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public void CreateMappings(IProfileExpression configuration)
        {
            configuration.CreateMap<PermanentAddressResponseType, Address>()
                .ForMember(d => d.Province, m => m.MapFrom(s => new Nomenclature { Code = s.DistrictCode, Name = s.DistrictName }))
                .ForMember(d => d.Municipality, m => m.MapFrom(s => new Nomenclature { Code = s.MunicipalityCode, Name = s.MunicipalityName }))
                .ForMember(d => d.Settlement, m => m.MapFrom(s => new Nomenclature { Code = s.SettlementCode, Name = s.SettlementName }))
                .ForMember(d => d.Region, m => m.MapFrom(s => new Nomenclature { Code = s.CityAreaCode, Name = s.CityArea.TrimStart(s.CityAreaCode.ToCharArray()).TrimStart('_') }))
                .ForMember(d => d.BuildingNumber, m => m.MapFrom(s => s.LocationName.IsNullOrEmpty() ? s.BuildingNumber : string.Empty)) // if there is LocationName(street) it is the number of the street, not the bulding
                .ForMember(d => d.Entrance, m => m.MapFrom(s => s.Entrance))
                .ForMember(d => d.FloorNumber, m => m.MapFrom(s => s.Floor))
                .ForMember(d => d.ApartmentNumber, m => m.MapFrom(s => s.Apartment))
                .ForMember(d => d.Street, m => m.MapFrom(s => s.LocationName))
                .ForMember(d => d.StreetNumber, m => m.MapFrom(s => s.LocationName.IsNotNullOrEmpty() ? s.BuildingNumber : string.Empty))
                .ForMember(d => d.Origin, m => m.MapFrom(s => new Nomenclature { Id = EnumHelper.GetAddressOriginIdByType(Origin.GRAOPermanent) }));

            configuration.CreateMap<TemporaryAddressResponseType, Address>()
                .ForMember(d => d.Country, m => m.MapFrom(s => new Nomenclature { Code = s.CountryCode, Name = s.CountryName }))
                .ForMember(d => d.Province, m => m.MapFrom(s => new Nomenclature { Code = s.DistrictCode, Name = s.DistrictName }))
                .ForMember(d => d.Municipality, m => m.MapFrom(s => new Nomenclature { Code = s.MunicipalityCode, Name = s.MunicipalityName }))
                .ForMember(d => d.Settlement, m => m.MapFrom(s => new Nomenclature { Code = s.SettlementCode, Name = s.SettlementName }))
                .ForMember(d => d.Region, m => m.MapFrom(s => new Nomenclature { Code = s.CityAreaCode, Name = s.CityArea.TrimStart(s.CityAreaCode.ToCharArray()).TrimStart('_') }))
                .ForMember(d => d.BuildingNumber, m => m.MapFrom(s => s.LocationName.IsNullOrEmpty() ? s.BuildingNumber : string.Empty)) // if there is LocationName(street) it is the number of the street, not the bulding
                .ForMember(d => d.Entrance, m => m.MapFrom(s => s.Entrance))
                .ForMember(d => d.FloorNumber, m => m.MapFrom(s => s.Floor))
                .ForMember(d => d.ApartmentNumber, m => m.MapFrom(s => s.Apartment))
                .ForMember(d => d.Street, m => m.MapFrom(s => s.LocationName))
                .ForMember(d => d.StreetNumber, m => m.MapFrom(s => s.LocationName.IsNotNullOrEmpty() ? s.BuildingNumber : string.Empty))
                .ForMember(d => d.Origin, m => m.MapFrom(s => new Nomenclature { Id = EnumHelper.GetAddressOriginIdByType(Origin.GRAOTemp) }));

            configuration.CreateMap<DetailType, Agent>()
                .ForMember(d => d.FullName, m => m.MapFrom(s => s.Subject.Name))
                .ForMember(d => d.FirstNames, m => m.MapFrom(s => new[] { this.GetName(s.Subject.Name, NameTypes.First) }))
                .ForMember(d => d.SurNames, m => m.MapFrom(s => new[] { this.GetName(s.Subject.Name, NameTypes.Sur) }))
                .ForMember(d => d.FamilyNames, m => m.MapFrom(s => new[] { this.GetName(s.Subject.Name, NameTypes.Family) }))
                .ForMember(d => d.EgnBulstat, m => m.MapFrom(s => s.Subject.Indent));

            configuration.CreateMap<ActualStateResponseType, Client>()
                .ForMember(d => d.Type, m => m.MapFrom(s => new Nomenclature { Id = EnumHelper.GetClientTypeIdByType(ClientType.Legal) }))
                .ForMember(d => d.EgnBulstat, m => m.MapFrom(s => s.UIC))
                .ForMember(d => d.FullName, m => m.MapFrom(s => s.Company))
                .ForMember(d => d.Addresses, m => m.MapFrom(s => this.MapActualStateResponseAddressess(s)))
                .ForMember(d => d.Representatives, m => m.MapFrom(s => s.Details.Where(x => this.managerIds.Contains(x.FieldCode))));

            configuration.CreateMap<PersonDataResponseType, ClientUpsertModel>()
                         .ForMember(
                             d => d.Type,
                             m => m.MapFrom(
                                 s => new Nomenclature { Id = EnumHelper.GetClientTypeIdByType(ClientType.Physical) }))
                         .ForMember(d => d.EgnBulstat, m => m.MapFrom(s => s.EGN))
                         .ForMember(
                             d => d.FirstNames,
                             m => m.MapFrom(s => new[] { s.PersonNames.FirstName.ToString() }))
                         .ForMember(d => d.SurNames, m => m.MapFrom(s => new[] { s.PersonNames.SurName.ToString() }))
                         .ForMember(
                             d => d.FamilyNames,
                             m => m.MapFrom(s => new[] { s.PersonNames.FamilyName.ToString() }))
                         .ForMember(
                             d => d.FirstNamesLatin,
                             m => m.MapFrom(s => new[] { s.LatinNames.FirstName.ToString() }))
                         .ForMember(
                             d => d.SurNamesLatin,
                             m => m.MapFrom(s => new[] { s.LatinNames.SurName.ToString() }))
                         .ForMember(
                             d => d.FamilyNamesLatin,
                             m => m.MapFrom(s => new[] { s.LatinNames.FamilyName.ToString() }))
                         .ForMember(d => d.IsDead, m => m.MapFrom(s => s.DeathDateSpecified))
                         .ForMember(
                             d => d.DateOfDeath,
                             m =>
                             {
                                 m.PreCondition(s => s.DeathDateSpecified);
                                 m.MapFrom(s => s.DeathDate);
                             });
            configuration.CreateMap<PersonDataResponseType, Client>()
                         .ForMember(d => d.Type, m => m.MapFrom(s => new Nomenclature { Id = EnumHelper.GetClientTypeIdByType(ClientType.Physical) }))
                         .ForMember(d => d.EgnBulstat, m => m.MapFrom(s => s.EGN))
                         .ForMember(d => d.FirstNames, m => m.MapFrom(s => new[] { s.PersonNames.FirstName.ToString() }))
                         .ForMember(d => d.SurNames, m => m.MapFrom(s => new[] { s.PersonNames.SurName.ToString() }))
                         .ForMember(d => d.FamilyNames, m => m.MapFrom(s => new[] { s.PersonNames.FamilyName.ToString() }))
                         .ForMember(d => d.FirstNamesLatin, m => m.MapFrom(s => new[] { s.LatinNames.FirstName.ToString() }))
                         .ForMember(d => d.SurNamesLatin, m => m.MapFrom(s => new[] { s.LatinNames.SurName.ToString() }))
                         .ForMember(d => d.FamilyNamesLatin, m => m.MapFrom(s => new[] { s.LatinNames.FamilyName.ToString() }))
                         .ForMember(d => d.IsDead, m => m.MapFrom(s => s.DeathDateSpecified))
                         .ForMember(
                             d => d.DateOfDeath,
                             m =>
                             {
                                 m.PreCondition(s => s.DeathDateSpecified);
                                 m.MapFrom(s => s.DeathDate);
                             });

            configuration.CreateMap<DcMessageDetails, Message>()
                .ForMember(d => d.Id, m => m.Ignore())
                .ForMember(d => d.IdByOtherSystem, m => m.MapFrom(s => s.Id))
                .ForMember(d => d.ReceiverElectronicSubjectName, m => m.MapFrom(s => s.ReceiverProfile.ElectronicSubjectName))
                .ForMember(d => d.SenderElectronicSubjectName, m => m.MapFrom(s => s.SenderProfile.ElectronicSubjectName))
                .ForMember(
                    d => d.ReceiverElectronicSubjectId,
                    m => m.MapFrom(
                        s => s.ReceiverProfile != null ? s.ReceiverProfile.ElectronicSubjectId : (Guid?)null))
                .ForMember(
                    d => d.SenderElectronicSubjectId,
                    m => m.MapFrom(s => s.SenderProfile != null ? s.SenderProfile.ElectronicSubjectId : (Guid?)null));

            configuration.CreateMap<DcMessage, Message>()
                .ForMember(d => d.Id, m => m.Ignore())
                .ForMember(d => d.IdByOtherSystem, m => m.MapFrom(s => s.Id))
                .ForMember(d => d.ReceiverElectronicSubjectName, m => m.MapFrom(s => s.ReceiverProfile.ElectronicSubjectName))
                .ForMember(d => d.SenderElectronicSubjectName, m => m.MapFrom(s => s.SenderProfile.ElectronicSubjectName))
                .ForMember(
                    d => d.ReceiverElectronicSubjectId,
                    m => m.MapFrom(
                        s => s.ReceiverProfile != null ? s.ReceiverProfile.ElectronicSubjectId : (Guid?)null))
                .ForMember(
                    d => d.SenderElectronicSubjectId,
                    m => m.MapFrom(s => s.SenderProfile != null ? s.SenderProfile.ElectronicSubjectId : (Guid?)null));
        }

        private string GetName(string name, NameTypes type)
        {
            var names = name.Split(" ");
            switch (type)
            {
                case NameTypes.First:
                    {
                        return name.IsNotNullOrEmpty() && names.Length > 0 ? names[0] : string.Empty;
                    }

                case NameTypes.Sur:
                    {
                        return name.IsNotNullOrEmpty() && names.Length > 1 ? names[1] : string.Empty;
                    }

                case NameTypes.Family:
                    {
                        return name.IsNotNullOrEmpty() && names.Length > 2 ? names[2] : string.Empty;
                    }

                default:
                    return string.Empty;
            }
        }

        private List<Address> MapActualStateResponseAddressess(ActualStateResponseType response)
        {
            List<Address> result = null;

            if (response?.Seat?.Address != null && ReflectionUtils.HasNotNullOrNotDefaultProperty(response.Seat.Address))
            {
                result ??= new List<Address>();
                result.Add(new Address
                {
                    Phone = response.Seat.Contacts.Phone,
                    Email = response.Seat.Contacts.EMail,
                    Country = new Nomenclature
                    {
                        Code = response.Seat.Address.CountryCode,
                        Name = response.Seat.Address.Country,
                    },
                    Province = new Nomenclature
                    {
                        Code = response.Seat.Address.DistrictEkatte,
                        Name = response.Seat.Address.District,
                    },
                    Municipality = new Nomenclature
                    {
                        Code = response.Seat.Address.MunicipalityEkatte,
                        Name = response.Seat.Address.Municipality,
                    },
                    Settlement = new Nomenclature
                    {
                        Code = response.Seat.Address.SettlementEKATTE,
                        Name = response.Seat.Address.Settlement,
                    },
                    PostCode = response.Seat.Address.PostCode,
                    BuildingNumber = response.Seat.Address.Block,
                    Entrance = response.Seat.Address.Entrance,
                    FloorNumber = response.Seat.Address.Floor,
                    ApartmentNumber = response.Seat.Address.Apartment,
                    Street = response.Seat.Address.Street,
                    StreetNumber = response.Seat.Address.StreetNumber,
                    Origin = new Nomenclature { Id = EnumHelper.GetAddressOriginIdByType(Origin.RegisterAgencySeat) },
                });
            }

            if (response.SeatForCorrespondence != null && ReflectionUtils.HasNotNullOrNotDefaultProperty(response.SeatForCorrespondence))
            {
                result ??= new List<Address>();
                result.Add(new Address
                {
                    Country = new Nomenclature
                    {
                        Code = response.SeatForCorrespondence.CountryCode,
                        Name = response.Seat.Address.Country,
                    },
                    Province = new Nomenclature
                    {
                        Code = response.SeatForCorrespondence.DistrictEkatte,
                        Name = response.Seat.Address.District,
                    },
                    Municipality = new Nomenclature
                    {
                        Code = response.SeatForCorrespondence.MunicipalityEkatte,
                        Name = response.Seat.Address.Municipality,
                    },
                    Settlement = new Nomenclature
                    {
                        Code = response.SeatForCorrespondence.SettlementEKATTE,
                        Name = response.Seat.Address.Settlement,
                    },
                    PostCode = response.SeatForCorrespondence.PostCode,
                    BuildingNumber = response.Seat.Address.Block,
                    Entrance = response.Seat.Address.Entrance,
                    FloorNumber = response.Seat.Address.Floor,
                    ApartmentNumber = response.Seat.Address.Apartment,
                    Street = response.Seat.Address.Street,
                    StreetNumber = response.Seat.Address.StreetNumber,
                    Origin = new Nomenclature { Id = EnumHelper.GetAddressOriginIdByType(Origin.RegisterAgencySeatForCorrespondence) }
                });
            }

            return result;
        }
    }
}
