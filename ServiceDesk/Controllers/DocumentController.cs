
using System.IO;
using System.Web.Mvc;


namespace ServiceDesk.Controllers
{
    public class DocumentController : Controller
    {
        [HandleError]
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult NotFound()
        {
            ViewBag.Rol = "Solicitante";
            return View();
        }
        public ActionResult Error()
        {
            ViewBag.Rol = "Solicitante";
            return View();
        }
        public ActionResult UsuarioNoExiste()
        {
            ViewBag.NoBar = "Block";
            ViewBag.Rol = "Solicitante";
            return View();
        }
        public int ServerC() {
            // AppExt = 1
            // Condor = 2
            return 2;
        }
        public void Upload(string NameCarga, string path, bool b) {
            //SE GUARDA EN LA RUTA QUE SERA COMPARTIDA PARA AMBAS DIRECCIONES 35 Y 36 ORIGINAL WORDS
            //var fname = @"\\10.200.154.36\uFiles\ServiceDeskV2\\" + NameCarga;
            //System.IO.File.Copy(path, fname, true);
            //System.IO.File.Delete(path);

            // Copia al IP, luego borra de la dirección temporal en el server
            //if (ServerC() == 1)
            //{
            //    var fname = @"\\\\10.200.154.36\uFiles\ServiceDeskV2\\" + NameCarga;
            //    System.IO.File.Copy(path, fname, b);
            //    System.IO.File.Delete(path);
            //}
        }
        public string DownloadPath(string ruta) {
            string fname = "";
            if (ServerC() == 1)
            {
                fname = @"\\" + ruta + "\\";                                   
            }
            else
            {
                //fname = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/"));   
            }
            return fname;
        }
    }
}