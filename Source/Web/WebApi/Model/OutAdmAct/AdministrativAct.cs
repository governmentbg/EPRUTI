namespace WebApi.Model.OutAdmAct
{
    using global::Ais.Data.Models.Issuer;
    using global::Ais.Data.Models.ModelVersion;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.OutAdmAct;
    using global::Ais.Data.Models.OutAdmAct.OutAdmActObject;
    using global::Ais.Data.Models.OutAdmAct.OutAdmActState;

    public class AdministrativAct
    {
        public string RegNumber { get; set; }

        public OutAdmActObject Object { get; set; }

        public string LegalGrounds { get; set; }

        public Nomenclature RegisterType { get; set; }

        public AdmActStateUpsertModel StateUpsertModel { get; set; }

        public Issuer Issuer { get; set; }

        public Nomenclature DocActualityStatus { get; set; }

        public List<ConnectedAdmAct> ConnectedAdmActs { get; set; }

        public List<ModelVersion> ModelVersions { get; set; }
    }
}
