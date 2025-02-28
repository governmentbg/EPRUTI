namespace Ais.Office.Utilities.Helpers
{
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Document;

    /// <summary>
    /// Class ApplicationHelper.
    /// </summary>
    public static class ApplicationHelper
    {
        public static bool IsAttachmentTypeRequired(AttachmentType attachmentType, InDocument application)
        {
            switch (attachmentType.Required.Type)
            {
                case RequiredType.Required:
                case RequiredType.RequiredFileOrLink:
                    return true;

                case RequiredType.RequiredWithRecipient:
                    return application.Applicants?.Any(
                        item => item.Recipient != null && item.Author != null) == true;
            }

            return false;
        }
    }
}
