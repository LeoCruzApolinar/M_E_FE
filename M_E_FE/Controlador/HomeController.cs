using System.IO;
using System.Threading.Tasks;
using M_E_FE.Metodos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Xml;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace M_E_FE.Controlador
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        [HttpPost]
        [Route("FEC")]
        public async Task<IActionResult> EchoString(/*IFormFile file*/)
        {
            List<string> res = new List<string>();
            List<string> resa = new List<string>();

            XmlDocument token = await Metodos_ECFClient.ObtenerSemillaAsync();
            string json;
            json = await Metodos_ECFClient.ValidarSemillaAsync(token);

            //if (file == null || file.Length == 0)
            //{
            //    return BadRequest("No se ha enviado ningún archivo o el archivo está vacío.");
            //}

            // Ruta de la carpeta donde buscar los archivos XML
            string folderPath = @"E:\Proyectos\M_I_FE\M_I_FE\Temp";

            // Obtén todas las rutas de los archivos XML en la carpeta
            List<string> xmlFiles = GetSortedXmlFilePaths(folderPath);

            foreach (string a in xmlFiles)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(a);

                // Supongamos que Metodos_XML.AgregarFechaHoraFirma y Metodos_XML.FirmarConCertificado
                // toman y devuelven cadenas XML.
                xmlDoc = Metodos_XML.AgregarFechaHoraFirma(xmlDoc);
                xmlDoc = Metodos_XML.FirmarConCertificado(xmlDoc);

                string respuesta = await Metodos_ECFClient.EnviarFacturaElectronicaAsync(xmlDoc, GetTokenFromJson(json));
                //string respuesta = @"{""trackId"":""06d6c9b6-57cb-432a-b4d3-d0cddf3f357c"",""codigo"":""3"",""estado"":""En Proceso"",""rnc"":""130987076"",""encf"":""E310000000001"",""secuenciaUtilizada"":false,""fechaRecepcion"":""7/8/2024 1:28:04 AM"",""mensajes"":[{""valor"":"""",""codigo"":0}]}";

                string respuestaB = await Metodos_ECFClient.ConsultarEstadoAsync(GettrackIdFromJson(respuesta), GetTokenFromJson(json));
                res.Add(respuestaB);
            }

            return Ok("respuestaB");
        }

        [HttpPost]
        [Route("FEAC")]
        public async Task<IActionResult> AprobacionComercial(IFormFile file)
        {

            if (file == null || file.Length == 0)
            {
                return BadRequest("No se ha enviado ningún archivo o el archivo está vacío.");
            }

            XmlDocument xmlDoc = new XmlDocument();
            // Cargar el contenido del archivo en memoria usando un Stream.
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0; // Restablecer la posición del stream para su lectura.

                // Cargar el XML desde el stream.
                xmlDoc.Load(stream);
            }

            xmlDoc = Metodos_XML.GenerarAprobacionComercial(xmlDoc);
            xmlDoc = Metodos_XML.FirmarConCertificado(xmlDoc);

            XmlDocument token = await Metodos_ECFClient.ObtenerSemillaAsync();
            string json;
            json = await Metodos_ECFClient.ValidarSemillaAsync(token);

            Metodos_ECFClient.EnviarAprobacionComercialAsync(xmlDoc, GetTokenFromJson(json));


            return Ok("respuestaB");
        }

        public static string GetTokenFromJson(string jsonString)
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("token", out JsonElement tokenElement))
                {
                    return tokenElement.GetString();
                }
                else
                {
                    throw new Exception("Token not found in JSON");
                }
            }
        }

        public static string GettrackIdFromJson(string jsonString)
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("trackId", out JsonElement tokenElement))
                {
                    return tokenElement.GetString();
                }
                else
                {
                    throw new Exception("Token not found in JSON");
                }
            }
        }

        public static List<string> GetSortedXmlFilePaths(string folderPath)
        {
            // Asegúrate de que la carpeta existe
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"La carpeta {folderPath} no existe.");
            }

            // Obtén todas las rutas de archivos XML en la carpeta
            string[] files = Directory.GetFiles(folderPath, "correctedXmlOutput_*.xml");

            // Ordena los archivos por número en el nombre
            var sortedFiles = files
                .Select(file => new
                {
                    Path = file,
                    Number = ExtractNumberFromFileName(file)
                })
                .OrderBy(x => x.Number)
                .Select(x => x.Path)
                .ToList();

            return sortedFiles;
        }
        private static int ExtractNumberFromFileName(string fileName)
        {
            // Usa una expresión regular para extraer el número del nombre del archivo
            var match = Regex.Match(Path.GetFileName(fileName), @"correctedXmlOutput_(\d+)\.xml");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }
    }
}
