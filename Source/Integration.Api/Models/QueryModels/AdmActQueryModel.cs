namespace Integration.Api.Models.QueryModels
{
    public class AdmActQueryModel
    {
        public string RegNumber { get; set; }

        public DateTime RegDate { get; set; }

        public IssuerQueryModel Issuer { get; set; }
    }
}
