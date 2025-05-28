namespace WebApi.Model.User
{
    using Ais.Data.Models.User;
    using Ais.Services.Mapping;

    public class User : IMapFrom<Ais.Data.Models.User.User>
    {
        public Guid Id { get; set; }

        public UserType Type { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string FullName { get; set; }

        public Role[] Roles { get; set; }

        public UserStatusType UserStatus { get; set; }
    }
}
