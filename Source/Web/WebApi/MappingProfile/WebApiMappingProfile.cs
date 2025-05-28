using AutoMapper;

public class WebApiMappingProfile : Profile
{
    public WebApiMappingProfile()
    {
        this.CreateMap<WebApi.Model.Journal.ActionType, Ais.Data.Models.Journal.ActionType>().ReverseMap();
        this.CreateMap<WebApi.Model.Journal.Action, WebApi.Model.Journal.BaseAction>().ReverseMap();
        this.CreateMap<WebApi.Model.Journal.Action, Ais.Data.Models.Journal.Action>().ReverseMap();
        this.CreateMap<WebApi.Model.Journal.Object, Ais.Data.Models.Journal.Object>().ReverseMap();
        this.CreateMap<Ais.Data.Models.User.User, WebApi.Model.User.User>();
        this.CreateMap<Ais.Data.Models.User.Role, WebApi.Model.User.Role>();
        this.CreateMap<Ais.Data.Models.User.Activity, WebApi.Model.User.Activity>();
        this.CreateMap<Ais.Data.Models.User.ApiSearchResult, WebApi.Model.User.ByEgnSearchResult>();
        this.CreateMap<Ais.Data.Models.QueryModels.AdmAct.ApiAdmActSearchResultModel, WebApi.Model.OutAdmAct.OutAdmActSearchResult>();
        this.CreateMap<Ais.Data.Models.Nomenclature.Nomenclature, WebApi.Model.Nomenclature.Nomenclature>();
        this.CreateMap<Ais.Data.Models.Nomenclature.NomenclatureTable, WebApi.Model.Nomenclature.NomenclatureTable>();
    }
}
