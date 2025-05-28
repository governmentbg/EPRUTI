namespace Ais.Office.Utilities.Helpers
{
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Cryptography.Xml;
    using System.Xml;
    using System.Xml.Serialization;

    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.OutAdmAct;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Class ModelToXmlHelper. Serialize and deserialize dynamic models to/from xml files.
    /// </summary>
    public static class ModelToXmlHelper
    {
        /// <summary>
        /// Hash algorithm Url to type mapping
        /// </summary>
        private static readonly Dictionary<string, Func<HashAlgorithm>> HashAlgorithmMapping =
    new Dictionary<string, Func<HashAlgorithm>>(StringComparer.OrdinalIgnoreCase)
            {
                        { "http://www.w3.org/2001/04/xmlenc#sha256", () => SHA256.Create() },
                        { "SHA256", () => SHA256.Create() },
                        { "http://www.w3.org/2000/09/xmldsig#sha1", () => SHA1.Create() },
                        { "SHA1", () => SHA1.Create() },
                        { "http://www.w3.org/2001/04/xmlenc#sha512", () => SHA512.Create() },
                        { "SHA512", () => SHA512.Create() }
            };

        /// <summary>
        /// Canonization method Url to type mapping
        /// </summary>
        private static readonly Dictionary<string, Func<Transform>> CanonicalizationMethods = new()
        {
            { "http://www.w3.org/TR/2001/REC-xml-c14n-20010315", () => new XmlDsigC14NTransform() },
            { "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments", () => new XmlDsigC14NWithCommentsTransform() },
            { "http://www.w3.org/2001/10/xml-exc-c14n#", () => new XmlDsigExcC14NTransform() },
            { "http://www.w3.org/2001/10/xml-exc-c14n#WithComments", () => new XmlDsigExcC14NWithCommentsTransform() }
        };

        /// <summary>
        /// Serialize byte array to file.
        /// </summary>
        /// <param name="array"> The byte array</param>
        /// <param name="isEN"> Culture </param>
        /// <returns>
        /// An <see cref="IFormFile"/> containing the serialized XML representation of the model.
        /// </returns>
        public static Task<(IFormFile formFile, MemoryStream memoryStream)> SerializeByteArrayToXmlFileAsync(byte[] array, bool isEN = false)
        {
            var memoryStream = new MemoryStream(array);
            var cultureSuffix = isEN ? "_EN" : string.Empty;
            var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", $"modelXML{cultureSuffix}_{Guid.NewGuid()}.xml")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/xml",
            };

            return Task.FromResult((formFile as IFormFile, memoryStream));
        }

        /// <summary>
        /// Serialize byte array to file.
        /// </summary>
        /// <param name="array"> The byte array.</param>
        /// <param name="signatureB64"> The signature as B64.</param>
        /// <param name="signMethod"> The signing method, comes from System.Security.Cryptography.Xml.SignedXml class.</param>
        /// <param name="canonicalizationMethod"> The canonicalization method, come from System.Security.Cryptography.Xml.SignedXml class</param>
        /// <param name="digestMethod"> The digest method, come from System.Security.Cryptography.Xml.SignedXml class</param>
        /// <param name="publicCert"> The public part of the client certificate.</param>
        /// <returns>
        /// An <see cref="IFormFile"/> containing the serialized XML representation of the model.
        /// </returns>
        public static Task<byte[]> AppendExternalSignatureToByteArrayAsync(byte[] array, string signatureB64, string signMethod, string canonicalizationMethod, string digestMethod, X509Certificate2 publicCert)
        {
            var xml = new XmlDocument { PreserveWhitespace = true };
            using (var xmlStream = new MemoryStream(array))
            {
                using var xmlReader = XmlReader.Create(xmlStream);
                xml.Load(xmlReader);
            }

            AppendExternalSignature(xml, signatureB64, signMethod, canonicalizationMethod, digestMethod, publicCert);

            using var outputStream = new MemoryStream();
            xml.Save(outputStream);
            return Task.FromResult(outputStream.ToArray());
        }

        public static bool ValidateXmlSignature(XmlDocument xmlDoc)
        {
            // Load the signature from the XML document
            SignedXml signedXml = new SignedXml(xmlDoc);

            // Find the Signature node
            XmlNodeList signatureNodeList = xmlDoc.GetElementsByTagName("Signature");
            if (signatureNodeList.Count == 0)
            {
                throw new InvalidOperationException("No Signature element found in the XML document.");
            }

            // Load the signature element
            signedXml.LoadXml((XmlElement)signatureNodeList[0]);

            // Retrieve the public key from the XML document (from KeyInfo element)
            XmlNodeList keyInfoNodeList = xmlDoc.GetElementsByTagName("KeyInfo");
            if (keyInfoNodeList.Count == 0)
            {
                throw new InvalidOperationException("No KeyInfo element found in the XML document.");
            }

            // Assuming the certificate is inside the KeyInfo -> X509Data -> X509Certificate element
            XmlElement keyInfoElement = (XmlElement)keyInfoNodeList[0];
            XmlNodeList x509CertificateNodes = keyInfoElement.GetElementsByTagName("X509Certificate");
            if (x509CertificateNodes.Count == 0)
            {
                throw new InvalidOperationException("No X509Certificate element found in the KeyInfo section.");
            }

            // Decode the certificate from the XML and load it
            string certificateBase64 = x509CertificateNodes[0].InnerText;
            byte[] certificateBytes = Convert.FromBase64String(certificateBase64);
            X509Certificate2 certificate = new X509Certificate2(certificateBytes);

            // Verify the signature
            return signedXml.CheckSignature(certificate, true);
        }

        /// <summary>
        /// Compare db and current model to see if its necessary to make version file.
        /// </summary>
        /// <typeparam name="T">The type of the model to be compared.</typeparam>
        /// <param name="dbModel"> The dbModel.</param>
        /// <param name="currentModel"> The currentModel.</param>
        /// <param name="ignoreAttributes"> The attributes if any would like to be ignored.</param>
        /// <returns>
        /// An <see cref="bool"/> If models are the same as boolean.
        /// </returns>
        public static bool DbAndCurrentModelAreEqual<T>(T dbModel, T currentModel, string[] ignoreAttributes = null)
        {
            if (dbModel == null || currentModel == null)
            {
                return false;
            }

            // get model nomenclature properties
            List<string> modelNomenclatures = GetNomenclatureProperties(typeof(T), string.Empty, new HashSet<Type>());

            // Serialize models
            var dbJson = JsonConvert.SerializeObject(dbModel);
            var currentJson = JsonConvert.SerializeObject(currentModel);

            // Parse to Jtoken for comparison
            JToken dbtoken = JToken.Parse(dbJson);
            JToken currentToken = JToken.Parse(currentJson);

            // Oly leave id in nomenclature objects since for the current model they will only have id and nothing else from dropdown selection
            NormalizeNomenclatures(dbtoken, modelNomenclatures);
            NormalizeNomenclatures(currentToken, modelNomenclatures);

            // Remove fields that can change even if no user changes are made. Made exactl for object OutAdmAct. If helper is used for other models it should be made more dynamic
            NormalizeFluentProperties(dbtoken);
            NormalizeFluentProperties(currentToken);

            if (ignoreAttributes != null)
            {
                foreach (var attribute in ignoreAttributes)
                {
                    var token1Attr = dbtoken.SelectToken(attribute);
                    var token2Attr = currentToken.SelectToken(attribute);
                    token1Attr?.Parent?.Remove();
                    token2Attr?.Parent?.Remove();
                }
            }

            return JToken.DeepEquals(dbtoken, currentToken);
        }

        /// <summary>
        /// Deserialize back to model from xml stream.
        /// </summary>
        /// <typeparam name="T">The type of the model to be deserialized.</typeparam>
        /// <param name="xmlStream"> The file stream.</param>
        /// <returns>
        /// An <see cref="T"/> The model after its deserialized.
        /// </returns>
        public static async Task<T> DeserializeXmlFromStreamAsync<T>(Stream xmlStream)
        {
            if (xmlStream == null || xmlStream.Length == 0)
            {
                throw new ArgumentException("Stream is null or empty", nameof(xmlStream));
            }

            xmlStream.Seek(0, SeekOrigin.Begin);

            var serializer = new XmlSerializer(typeof(XmlWrapper<T>));
            using var reader = new StreamReader(xmlStream);
            var wrapper = (XmlWrapper<T>)await Task.Run(() => serializer.Deserialize(reader)) ?? throw new InvalidOperationException("FailToDeserializeXml");

            if (string.IsNullOrEmpty(wrapper.Data))
            {
                throw new InvalidOperationException("MissingXmlData");
            }

            return (T)JsonConvert.DeserializeObject(wrapper.Data, typeof(T), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto, // Auto resolves concrete types
            })!;
        }

        /// <summary>
        /// Convert model data to byte array.
        /// </summary>
        /// <param name="model"> The generic model.</param>
        /// <typeparam name="T">The type of the model to be converted.</typeparam>
        /// <returns>
        /// An <see cref="byte[]"/> The byte array.
        /// </returns>
        public static async Task<byte[]> ConvertModelToByteArrayAsync<T>(T model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var wrapper = new XmlWrapper<T>
            {
                Data = JsonConvert.SerializeObject(model, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All, // Ensures $type metadata is included
                }),
            };

            using var memoryStream = new MemoryStream();
            XmlSerializer xmlSerializer = new(typeof(XmlWrapper<T>));

            await using (var xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true, Async = true }))
            {
                xmlSerializer.Serialize(xmlWriter, wrapper);
            }

            return memoryStream.ToArray(); // Convert the stream content to byte array
        }

        public static async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream)
        {
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Append signature to xml.
        /// </summary>
        /// <param name="xmlDoc"> The original xml.</param>
        /// <param name="signatureB64"> The signature as B64.</param>
        /// <param name="signMethod"> The sign method.</param>
        /// <param name="canonizationMethod"> The canonization method.</param>
        /// <param name="digestMethod"> The digest method.</param>
        /// <param name="publicCert"> The public part of the client certificate.</param>
        private static void AppendExternalSignature(XmlDocument xmlDoc, string signatureB64, string signMethod, string canonizationMethod, string digestMethod, X509Certificate2 publicCert)
        {
            var baseSignatureUrl = "http://www.w3.org/2000/09/xmldsig#";

            // Create the Signature element
            XmlElement signatureElement = xmlDoc.CreateElement("Signature", baseSignatureUrl);

            // Create the SignedInfo element
            XmlElement signedInfoElement = xmlDoc.CreateElement("SignedInfo", baseSignatureUrl);

            // CanonicalizationMethod (using the input parameter)
            XmlElement canonicalizationMethodElement = xmlDoc.CreateElement("CanonicalizationMethod", baseSignatureUrl);
            canonicalizationMethodElement.SetAttribute("Algorithm", canonizationMethod);
            signedInfoElement.AppendChild(canonicalizationMethodElement);

            // SignatureMethod
            XmlElement signatureMethodElement = xmlDoc.CreateElement("SignatureMethod", baseSignatureUrl);
            signatureMethodElement.SetAttribute("Algorithm", signMethod);
            signedInfoElement.AppendChild(signatureMethodElement);

            // Create the Reference element
            XmlElement referenceElement = xmlDoc.CreateElement("Reference", baseSignatureUrl);
            referenceElement.SetAttribute("URI", string.Empty);

            // Transforms (Canonicalization)
            XmlElement transformsElement = xmlDoc.CreateElement("Transforms", baseSignatureUrl);
            XmlElement transformElement = xmlDoc.CreateElement("Transform", baseSignatureUrl);
            transformElement.SetAttribute("Algorithm", canonizationMethod);
            transformsElement.AppendChild(transformElement);
            referenceElement.AppendChild(transformsElement);

            // DigestMethod & DigestValue
            XmlElement digestMethodElement = xmlDoc.CreateElement("DigestMethod", baseSignatureUrl);
            digestMethodElement.SetAttribute("Algorithm", digestMethod);
            referenceElement.AppendChild(digestMethodElement);

            string digestValue = CalculateDigest(xmlDoc, canonizationMethod, digestMethod);

            XmlElement digestValueElement = xmlDoc.CreateElement("DigestValue", baseSignatureUrl);
            digestValueElement.InnerText = digestValue;
            referenceElement.AppendChild(digestValueElement);

            // Append Reference to SignedInfo
            signedInfoElement.AppendChild(referenceElement);

            // Apply canonicalization to SignedInfo
            byte[] canonicalSignedInfo = CanonicalizeElement(signedInfoElement, canonizationMethod);

            // Append SignedInfo to Signature
            signatureElement.AppendChild(signedInfoElement);

            // SignatureValue
            XmlElement signatureValueElement = xmlDoc.CreateElement("SignatureValue", baseSignatureUrl);
            signatureValueElement.InnerText = signatureB64;
            signatureElement.AppendChild(signatureValueElement);

            // KeyInfo & Certificate
            XmlElement keyInfoElement = xmlDoc.CreateElement("KeyInfo", baseSignatureUrl);
            XmlElement x509DataElement = xmlDoc.CreateElement("X509Data", baseSignatureUrl);
            XmlElement x509CertificateElement = xmlDoc.CreateElement("X509Certificate", baseSignatureUrl);
            x509CertificateElement.InnerText = Convert.ToBase64String(publicCert.RawData);
            x509DataElement.AppendChild(x509CertificateElement);
            keyInfoElement.AppendChild(x509DataElement);
            signatureElement.AppendChild(keyInfoElement);

            // Append Signature to Document
            xmlDoc.DocumentElement.AppendChild(signatureElement);
        }

        private static string CalculateDigest(XmlDocument xmlDoc, string canonicalizationMethod, string digestMethod)
        {
            if (!CanonicalizationMethods.TryGetValue(canonicalizationMethod, out var transformFactory))
            {
                throw new ArgumentException($"Unsupported canonicalization method: {canonicalizationMethod}", nameof(canonicalizationMethod));
            }

            Transform tf = transformFactory();

            // Serialize XmlElement to MemoryStream
            using (var memoryStream = SerializeXmlElementToMemoryStream(xmlDoc.DocumentElement))
            {
                // Load the input for the transform
                tf.LoadInput(memoryStream);
            }

            // Perform canonicalization
            using var stream = (Stream)tf.GetOutput();
            using var memoryStream2 = new MemoryStream();
            stream.CopyTo(memoryStream2);
            byte[] canonicalizedBytes = memoryStream2.ToArray();

            // Select the appropriate hash algorithm based on the digest method
            if (!HashAlgorithmMapping.TryGetValue(digestMethod, out var createHashAlgorithm))
            {
                throw new ArgumentException($"Unsupported hash algorithm: {digestMethod}", nameof(digestMethod));
            }

            // Compute the hash of the canonicalized content
            using var hashAlgorithm = createHashAlgorithm();
            byte[] hashBytes = hashAlgorithm.ComputeHash(canonicalizedBytes);
            return Convert.ToBase64String(hashBytes);
        }

        private static byte[] CanonicalizeElement(XmlElement element, string canonicalizationMethod)
        {
            // Validate and retrieve the appropriate transform factory based on the canonicalization method
            if (!CanonicalizationMethods.TryGetValue(canonicalizationMethod, out var transformFactory))
            {
                throw new ArgumentException($"Unsupported canonicalization method: {canonicalizationMethod}", nameof(canonicalizationMethod));
            }

            // Initialize the transform
            Transform tf = transformFactory();

            // Serialize the XmlElement to a MemoryStream
            using (var memoryStream = SerializeXmlElementToMemoryStream(element))
            {
                // Load the input for the transform
                tf.LoadInput(memoryStream);
            }

            // Perform canonicalization
            using var stream = (Stream)tf.GetOutput();
            using var memoryStream2 = new MemoryStream();
            stream.CopyTo(memoryStream2);
            return memoryStream2.ToArray();
        }

        private static MemoryStream SerializeXmlElementToMemoryStream(XmlElement element)
        {
            // Create a MemoryStream to hold the serialized XML
            var memoryStream = new MemoryStream();

            // Define XmlWriterSettings with appropriate settings
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true, // Omit XML declaration if not needed
                Indent = false // Disable indentation for canonicalization
            };

            // Create an XmlWriter with the specified settings
            using (var writer = XmlWriter.Create(memoryStream, settings))
            {
                // Write the XmlElement to the XmlWriter
                element.WriteTo(writer);
            }

            // Reset the memory stream position to the beginning
            memoryStream.Position = 0;

            return memoryStream;
        }

        private static void NormalizeFluentProperties(JToken token)
        {
            // leave only Id since they can change based on if they come from db or client side
            var attachments = token.SelectTokens("$..Attachments[*]").ToList();
            foreach (var attachment in attachments)
            {
                if (attachment is JObject attachmentObj && attachmentObj["Id"] != null)
                {
                    attachmentObj.Replace(new JObject { ["Id"] = attachmentObj["Id"] });
                }
            }

            // remove since they dynamically change
            var toDelete = new List<string>() { "UniqueId", "RegisterType" };
            foreach (var item in toDelete)
            {
                var tokens = token.SelectTokens($"$..{item}").ToList();
                foreach (var tk in tokens)
                {
                    tk?.Parent?.Remove();
                }
            }

            // Unify so theres no difference between with or without time offset
            var regDates = token.SelectTokens("$..RegDate").ToList();
            foreach (var regDate in regDates)
            {
                if (regDate.Type == JTokenType.Date)
                {
                    regDate.Replace(regDate.Value<DateTime>().ToUniversalTime().ToString("O"));
                }
                else if (regDate.Type == JTokenType.String)
                {
                    if (DateTimeOffset.TryParse(regDate.ToString(), out var parsedDate))
                    {
                        regDate.Replace(parsedDate.ToUniversalTime().ToString("O"));
                    }
                }
            }
        }

        private static void NormalizeNomenclatures(JToken token, List<string> objectNomenclatures)
        {
            foreach (var propertyName in objectNomenclatures)
            {
                // Select the token corresponding to the property name
                var nomenclatureTokens = token.SelectTokens($"$..{propertyName}").ToList();

                foreach (var nomenclatureToken in nomenclatureTokens)
                {
                    // Check if the token is a JObject and contains the "Id" field
                    if (nomenclatureToken is JObject nomenclatureObj && nomenclatureObj["Id"] != null)
                    {
                        // Replace the object with just the "Id" field
                        nomenclatureObj.Replace(new JObject { ["Id"] = nomenclatureObj["Id"] });
                    }
                }
            }
        }

        private static List<string> GetNomenclatureProperties(Type type, string parentPath, HashSet<Type> visitedTypes, bool isFirstCall = true)
        {
            var result = new List<string>();

            if (isFirstCall)
            {
                visitedTypes.Clear();
            }

            // Prevent circular references
            if (visitedTypes.Contains(type))
            {
                return result;
            }

            visitedTypes.Add(type);  // Mark this type as visited

            foreach (var property in type.GetProperties())
            {
                Type propType = property.PropertyType;
                string fullPath = string.IsNullOrEmpty(parentPath) ? property.Name : $"{parentPath}.{property.Name}";

                // Check if the property type is Nomenclature
                if (propType == typeof(Nomenclature))
                {
                    result.Add(fullPath);
                }
                else if (!propType.IsPrimitive && propType != typeof(string) && propType != typeof(DateTime) && propType.IsClass)
                {
                    // Recursively check for nested objects (but avoid revisiting types)
                    result.AddRange(GetNomenclatureProperties(propType, fullPath, visitedTypes, false)); // recursive call, set isFirstCall to false
                }
            }

            return result;
        }
    }
}
