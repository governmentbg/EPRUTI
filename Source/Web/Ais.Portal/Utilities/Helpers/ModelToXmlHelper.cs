namespace Ais.Portal.Utilities.Helpers
{
    using System.Xml.Serialization;

    using global::Ais.Data.Models.OutAdmAct;

    using Newtonsoft.Json;

    /// <summary>
    /// Class ModelToXmlHelper. Serialize and deserialize dynamic models to/from xml files.
    /// </summary>
    public static class ModelToXmlHelper
    {
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
    }
}
