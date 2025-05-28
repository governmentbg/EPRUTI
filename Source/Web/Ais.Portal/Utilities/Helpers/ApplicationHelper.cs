namespace Ais.Portal.Utilities.Helpers
{
    using global::Ais.Data.Models.Attachment;
    using global::Ais.Data.Models.Document;

    /// <summary>
    /// Class ApplicationHelper.
    /// </summary>
    public static class ApplicationHelper
    {
        public static bool IsAttachmentTypeRequired(AttachmentType attachmentType, InDocument application, Guid? roleId = null)
        {
            switch (attachmentType.Required.Type)
            {
                case RequiredType.Required:
                case RequiredType.RequiredFileOrLink:
                    return true;

                case RequiredType.RequiredWithRecipient:
                    return application.Applicants?.Any(item => item.Recipient != null && item.Author != null) == true;

                case RequiredType.RequiredByRole:
                    {
                        return roleId.HasValue && attachmentType.Required.Roles?.Any(item => item.Id == roleId.Value) == true;
                    }
            }

            return false;
        }
    }
}
