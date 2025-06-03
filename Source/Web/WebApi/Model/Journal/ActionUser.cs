namespace WebApi.Model.Journal
{
    public class ActionUser
    {
        public Guid Id { get; set; }

        public string UserName { get; set; }

        public Guid? GroupId { get; set; }

        public Guid[] RoleIds { get; set; }
    }
}
