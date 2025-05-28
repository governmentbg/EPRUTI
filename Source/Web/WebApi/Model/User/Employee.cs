namespace WebApi.Model.User
{
    using Ais.Services.Mapping;

    public class Employee : IMapFrom<Ais.Data.Models.Employee.Employee>
    {
        public Guid Id { get; set; }

        public string FullName { get; set; }
    }
}
