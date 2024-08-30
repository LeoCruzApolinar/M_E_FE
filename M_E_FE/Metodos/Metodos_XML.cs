using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Serialization;
using System.Text;

namespace M_E_FE.Metodos
{
    public class Metodos_XML
    {
        public static dynamic ObtenerTipoGeneral<T>(string codigo)
        {
            if (codigo == null)
            {
                return default(T);
            }

            foreach (var field in typeof(T).GetFields())
            {
                var attribute = (System.Xml.Serialization.XmlEnumAttribute)Attribute.GetCustomAttribute(field, typeof(System.Xml.Serialization.XmlEnumAttribute));
                if (attribute != null && attribute.Name == codigo)
                {
                    return (T)field.GetValue(null);
                }
            }
            return default(T);
        }

        public static XmlDocument AgregarFechaHoraFirma(XmlDocument xmlInput)
        {
            if (xmlInput.DocumentElement == null)
            {
                throw new ArgumentException("El documento XML no tiene un elemento raíz.");
            }

            // Crear un nuevo nodo para <FechaHoraFirma>
            XmlElement fechaHoraFirmaElement = xmlInput.CreateElement("FechaHoraFirma");
            string fechaHoraFirma = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            fechaHoraFirmaElement.InnerText = fechaHoraFirma;

            // Agregar el nuevo nodo al documento XML
            xmlInput.DocumentElement.AppendChild(fechaHoraFirmaElement);

            return xmlInput;
        }

        public static XmlDocument FirmarConCertificado(XmlDocument xmlDoc)
        {
            try
            {
                string passCert = "eureka2024";
                string pathCert = @"E:\Proyectos\M_E_FE\M_E_FE\Recursos\4301019_identity.p12";
                if (!File.Exists(pathCert)) throw new Exception("El certificado para firma no existe");
                var cert = new X509Certificate2(pathCert, passCert, X509KeyStorageFlags.Exportable);
                var exportedKeyMaterial = cert.PrivateKey.ToXmlString(true);
                var key = new RSACryptoServiceProvider(new CspParameters(24));
                key.PersistKeyInCsp = false;
                key.FromXmlString(exportedKeyMaterial);
                SignedXml signedXml = new SignedXml(xmlDoc);
                signedXml.SigningKey = key;
                signedXml.SignedInfo.SignatureMethod =
                "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                // Se agrega la referencia del algoritmo de firma utilizado.
                Reference reference = new Reference();
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
                reference.Uri = "";
                signedXml.AddReference(reference);
                // Se agrega la información del certificado utilizado para la firma.
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;
                // Generate the signature.
                signedXml.ComputeSignature();
                //Obtenemos la representación del XML firmado y la guardamos en un XmlElement object
                XmlElement xmlFirmaDigital = signedXml.GetXml();
                //Adicionamos el elemento de la firma al documento XML.
                xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlFirmaDigital, true));
                return xmlDoc;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string ObtenerEtiqutaXMl(XmlDocument xmlDocument, string xPath)
        {
            // Busca la etiqueta eNCF en el documento XML
            XmlNode eNCFNode = xmlDocument.SelectSingleNode(xPath);

            // Verifica si se encontró la etiqueta eNCF y devuelve su valor
            if (eNCFNode != null)
            {
                return eNCFNode.InnerText;
            }
            else
            {
                // Si no se encuentra la etiqueta, devuelve un valor nulo o un mensaje de error
                return null; // o "Etiqueta eNCF no encontrada"
            }
        }

        public static XmlDocument GenerarAprobacionComercial(XmlDocument xmlDocument)
        {
            ACECF aCECF = new ACECF()
            {
                DetalleAprobacionComercial = new ACECFDetalleAprobacionComercial()
                {
                    Version = ObtenerTipoGeneral<versionType>(ObtenerEtiqutaXMl(xmlDocument, "//Version")),
                    RNCEmisor = ObtenerEtiqutaXMl(xmlDocument, "//RNCEmisor"),
                    eNCF = ObtenerEtiqutaXMl(xmlDocument, "//eNCF"),
                    FechaEmision = ObtenerEtiqutaXMl(xmlDocument, "//FechaEmision"),
                    MontoTotal = TryParseDecimal(ObtenerEtiqutaXMl(xmlDocument, "//MontoTotal")),
                    RNCComprador = ObtenerEtiqutaXMl(xmlDocument, "//RNCComprador"),
                    Estado = ObtenerTipoGeneral<EstadoType>(ObtenerEtiqutaXMl(xmlDocument, "//Estado")),
                    DetalleMotivoRechazo = ObtenerEtiqutaXMl(xmlDocument, "//DetalleMotivoRechazo"),
                    FechaHoraAprobacionComercial = ObtenerEtiqutaXMl(xmlDocument, "//FechaHoraAprobacionComercial")
                }
            };

            // Serializar el objeto ACECF a XML
            XmlSerializer serializer = new XmlSerializer(typeof(ACECF));

            // Configurar XmlWriterSettings para codificación UTF-8
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false, // No omitir la declaración XML
                Encoding = Encoding.UTF8    // Codificación UTF-8
            };

            // Usar StringWriterWithEncoding para especificar la codificación UTF-8
            using (var stringWriter = new StringWriterWithEncoding(Encoding.UTF8))
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                // Serializar el objeto ACECF
                serializer.Serialize(xmlWriter, aCECF);
                xmlWriter.Flush();
                string xmlOutput = stringWriter.ToString();

                // Cargar el XML resultante en un XmlDocument
                XmlDocument xmlDocumentA = new XmlDocument();
                xmlDocumentA.LoadXml(xmlOutput);

                return xmlDocumentA;
            }
        }

        public class StringWriterWithEncoding : StringWriter
        {
            private readonly Encoding encoding;

            public StringWriterWithEncoding(Encoding encoding)
            {
                this.encoding = encoding;
            }

            public override Encoding Encoding => encoding;
        }

        public static decimal TryParseDecimal(string val)
        {
            if (decimal.TryParse(val, out var result))
            {
                result = Math.Round(result, 3);
                return  result;
            }
            else
            {
                // Manejar el caso en que la clave no se encuentra o el valor no es un decimal válido
                // Puede lanzar una excepción, devolver un valor predeterminado o registrar una advertencia
                return default;
            }
        }
    }
}
