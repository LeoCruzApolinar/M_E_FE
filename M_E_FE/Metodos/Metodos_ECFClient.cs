using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace M_E_FE.Metodos
{
    public class Metodos_ECFClient
    {

        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> EnviarFacturaElectronicaAsync(XmlDocument xmlDocument, string token)
        {
            try
            {
                // URL del servicio
                string url = $"https://ecf.dgii.gov.do/CerteCF/Recepcion/api/FacturasElectronicas";

                // Crear la solicitud HTTP
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    // Configurar los encabezados
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Crear el contenido multipart/form-data
                    var content = new MultipartFormDataContent();

                    // Convertir el XmlDocument a un ByteArrayContent
                    var xmlString = xmlDocument.OuterXml;
                    var xmlContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(xmlString));
                    xmlContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                    string eNCFValue = Metodos_XML.ObtenerEtiqutaXMl(xmlDocument, "//eNCF");
                    content.Add(xmlContent, "xml", $"130987076{eNCFValue}.xml");

                    // Asignar el contenido a la solicitud
                    request.Content = content;

                    // Enviar la solicitud y obtener la respuesta
                    using (var response = await client.SendAsync(request))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Error: {response.StatusCode}, Detalles: {responseBody}");
                        }

                        return responseBody;
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return $"Error: {ex.Message}";
            }
        }

        public static async Task<XmlDocument> ObtenerSemillaAsync()
        {
            var response = await client.GetAsync("https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            // Cargar el contenido de la respuesta en un XmlDocument
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseContent);

            // Firmar el documento XML
            XmlDocument signedXmlDoc = Metodos_XML.FirmarConCertificado(xmlDoc);

            return signedXmlDoc;
        }

        public static async Task<string> ValidarSemillaAsync(XmlDocument xmlDoc)
        {
            // Convertir el XmlDocument a una cadena XML
            string xmlContent;
            using (var stringWriter = new System.IO.StringWriter())
            {
                using (var xmlTextWriter = XmlWriter.Create(stringWriter))
                {
                    xmlDoc.WriteTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    xmlContent = stringWriter.GetStringBuilder().ToString();
                }
            }

            using var content = new MultipartFormDataContent();
            var xmlFileContent = new StringContent(xmlContent);
            xmlFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            content.Add(xmlFileContent, "xml", "semilla.xml");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/ValidarSemilla")
            {
                Content = content
            };

            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"HTTP Error: {response.StatusCode}, Reason: {response.ReasonPhrase}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<string> ConsultarEstadoAsync(string trackid, string token)
        {
            try
            {
                // URL del servicio
                string url = $"https://ecf.dgii.gov.do/CerteCF/ConsultaResultado/api/Consultas/Estado?trackid={trackid}";

                // Crear la solicitud HTTP
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    // Configurar los encabezados
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Enviar la solicitud y obtener la respuesta
                    using (var response = await client.SendAsync(request))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Error: {response.StatusCode}, Detalles: {responseBody}");
                        }

                        return responseBody;
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return $"Error: {ex.Message}";
            }
        }

        public static async Task<string> EnviarAprobacionComercialAsync(XmlDocument xmlDocument, string token)
        {
            try
            {
                // URL del servicio, ajusta el entorno según corresponda
                string url = $"https://ecf.dgii.gov.do/CerteCF/AprobacionComercial/api/AprobacionComercial";

                // Crear la solicitud HTTP
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    // Configurar los encabezados
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Crear el contenido multipart/form-data
                    var content = new MultipartFormDataContent();

                    // Convertir el XmlDocument a un ByteArrayContent
                    var xmlString = xmlDocument.OuterXml;
                    var xmlContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(xmlString));
                    xmlContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

                    // Obtener etiquetas del XML para formar el nombre del archivo
                    var st = $"{Metodos_XML.ObtenerEtiqutaXMl(xmlDocument, "//RNCComprador")}{Metodos_XML.ObtenerEtiqutaXMl(xmlDocument, "//eNCF")}.xml";
                    content.Add(xmlContent, "xml", st);

                    // Asignar el contenido a la solicitud
                    request.Content = content;

                    // Enviar la solicitud y obtener la respuesta
                    using (var response = await client.SendAsync(request))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Error: {response.StatusCode}, Detalles: {responseBody}");
                        }

                        return responseBody;
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return $"Error: {ex.Message}";
            }
        }


    }
}
