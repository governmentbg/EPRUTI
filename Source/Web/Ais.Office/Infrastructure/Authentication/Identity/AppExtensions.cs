namespace Ais.Office.Infrastructure.Authentication.Identity
{
    using System.Xml.Linq;

    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Class AppExtensions.
    /// Implements the <see cref="ITfoxtec.Identity.Saml2.Schemas.Extensions" />
    /// </summary>
    /// <seealso cref="ITfoxtec.Identity.Saml2.Schemas.Extensions" />
    public class AppExtensions : ITfoxtec.Identity.Saml2.Schemas.Extensions
    {
        private static readonly XName EgovNamespaceNameX = XNamespace.Xmlns + "egovbga";
        private static readonly Uri EgovNamespace = new Uri("urn:bg:egov:eauth:2.0:saml:ext");
        private static readonly XNamespace EgovNamespaceX = XNamespace.Get(EgovNamespace.OriginalString);

        public AppExtensions(IConfiguration configuration)
        {
            this.Element.Add(GetXContent(configuration));
        }

        /// <summary>
        /// Gets the element with value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns>XElement.</returns>
        private static XElement GetElementWithValue(string name, string value = null)
        {
            var element = new XElement(EgovNamespaceX + name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                element.Add(value);
            }

            return element;
        }

        /// <summary>
        /// Gets the content of the x.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>IEnumerable&lt;XObject&gt;.</returns>
        private static XObject GetXContent(IConfiguration configuration)
        {
            var ss = new XElement(
                EgovNamespaceX + "RequestedService",
                new XAttribute(EgovNamespaceNameX, EgovNamespace.OriginalString),
                GetElementWithValue("Service", configuration["Saml2:ServiceExt"]),
                GetElementWithValue("Provider", configuration["Saml2:ProviderExt"]),
                GetElementWithValue("LevelOfAssurance", configuration["Saml2:LevelOfAssuranceExt"]));

            return ss;
        }
    }
}
