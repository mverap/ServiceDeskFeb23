using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServiceDesk.Models;
using ServiceDesk.Managers;
using System.Data;
using System.IO;
using ServiceDesk.ViewModels;

namespace ServiceDesk.Controllers
{
    public class AppController : Controller
    {
        private readonly RHAdminContext _rh = new RHAdminContext();
        private readonly ServiceDeskManager _mng = new ServiceDeskManager();
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private readonly DocumentController _doc = new DocumentController();
        //============================================================================================================================================

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public ActionResult CreateTicket(string Usuario, string id, string EmployeeId)
        {
            if (Session["EmpleadoNo"] != null) { Usuario = Session["EmpleadoNo"].ToString(); }
            else { return RedirectToAction("Login", "Home"); }

            var vm = new tbl_TicketDetalle();
            var NumeroPenta = Convert.ToInt32(Usuario);

            //Cuando manda id de ticket creado
            if (id != null)
            {
                var Idint = Convert.ToInt32(id);
                var busq = _db.tbl_TicketDetalle.Where(a => a.Id == Idint).FirstOrDefault();
                vm.EmpleadoID = Convert.ToInt32(EmployeeId);
            }

            //var InfoUser = _rh.vw_DetalleEmpleado.Where(a => a.NumeroPenta == NumeroPenta).FirstOrDefault();
            var InfoUser = _db.vw_INFO_USER_EMPLEADOS.Where(t => t.NumeroPenta == NumeroPenta).FirstOrDefault();
            if (InfoUser != null)
            {
                //vm.EmpleadoID = InfoUser.NumeroPenta;
                //vm.NombreCompleto = InfoUser.NombreCompleto;
                //vm.Area = InfoUser.Area;
                //vm.Correo = InfoUser.Email;
                vm.EmpleadoID = InfoUser.NumeroPenta;
                vm.NombreCompleto = InfoUser.NombreCompleto;
                vm.Area = InfoUser.Area;
                vm.Correo = InfoUser.Email;
            }

            ViewBag.Id = string.IsNullOrEmpty(id) ? "" : id;
            //Listas
            ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
            ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
            ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
            ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");

            return View(vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //=======================================================CATALOGOS============================================================================
        [HttpPost]
        public JsonResult GetCategoria(int Id)
        {

            var subcat = _db.cat_SubCategoria.Where(a => a.IDCategoria == Id).Select(a => new { a.Id, a.SubCategoria }).ToList();

            return Json(subcat, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - -
        [HttpPost]
        public JsonResult GetMatriz(int Id)
        {

            var mat = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == Id).ToList();

            return Json(mat, JsonRequestBehavior.AllowGet);

        }
        //=======================================================CATALOGOS============================================================================
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost]
        public ActionResult SetTicket(tbl_TicketDetalle vm, HttpPostedFileBase upload)
        {
            var data = _mng.SetTicket(vm);

            var Usuario = "";

            if (Session["EmpleadoNo"] != null)
            {

                Usuario = Session["EmpleadoNo"].ToString();

            }
            else
            {

                return RedirectToAction("Login", "Home");
            }

            //Carga de Archivo 

            if (upload != null && upload.ContentLength > 0)
            {
                var extension = upload.FileName.ToUpper();


                if (extension.EndsWith(".PDF") || extension.EndsWith(".DOC") || extension.EndsWith(".DOCX") || extension.EndsWith(".DOT")
                    || extension.EndsWith(".XLS") || extension.EndsWith(".XLSX") || extension.EndsWith(".XLSM") || extension.EndsWith(".XLT")
                    || extension.EndsWith(".PPT") || extension.EndsWith(".PPTX") || extension.EndsWith(".PPS") || extension.EndsWith(".JPG")
                    || extension.EndsWith(".PNG") || extension.EndsWith(".JPEG") || extension.EndsWith(".CSV"))
                {
                    var uploadName = Path.GetFileName(upload.FileName);
                    //Nombre de la carga
                    var NameCarga = data + "_" + upload.FileName;
                    //SE GUARDA EL ARCHIVO DE MANERA LOCAL
                    var path = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/"), NameCarga);
                    upload.SaveAs(path);


                    //SE GUARDA EN LA RUTA QUE SERA COMPARTIDA PARA AMBAS DIRECCIONES 35 Y 36
                    var fname = @"\\10.200.154.36\uFiles\ServiceDeskV2\\" + NameCarga;
                    System.IO.File.Copy(path, fname, true);
                    System.IO.File.Delete(path);
                    _doc.Upload(NameCarga, path, true);
                    try
                    {

                        //CREAR LA RUTA DEL MANAGER PARA GUARDAR LA INFO EN DetalleSubTicket
                        _mng.SetArchivoCreateTicket(data, NameCarga);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return RedirectToAction("CreateTicket", "App", new { id = data });
                }
                else
                {
                    ModelState.AddModelError("File", "Formato no soportado");
                    return PartialView();

                }

            }



            return RedirectToAction("CreateTicket", "App", new { id = data, EmployeeId = Usuario });
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public ActionResult EditTicket(int? Id, string EmployeeId, string folio)
        {

            if (Session["EmpleadoNo"] != null)
            {

                EmployeeId = Session["EmpleadoNo"].ToString();

            }
            else
            {

                return RedirectToAction("Login", "Home");
            }


            ViewBag.Id = string.IsNullOrEmpty(folio) ? "" : folio;

            var data = _db.tbl_TicketDetalle.Where(a => a.Id == Id).FirstOrDefault();

            var vm = new tbl_TicketDetalle();

            //Cuando manda id de ticket editado
            if (folio != null)
            {
                var Idint = Convert.ToInt32(folio);

                var busq = _db.tbl_TicketDetalle.Where(a => a.Id == Idint).FirstOrDefault();

                vm.EmpleadoID = Convert.ToInt32(EmployeeId);

            }


            if (data != null)
            {
                var cat = _db.cat_Categoria.Where(a => a.Id == data.Categoria).FirstOrDefault();
                var subcat = _db.cat_SubCategoria.Where(a => a.Id == data.SubCategoria).FirstOrDefault();
                var cent = _db.cat_Centro.Where(a => a.Id == data.Centro).FirstOrDefault();
                var mat = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == data.SubCategoria).FirstOrDefault();

                vm.NombreTercero = data.NombreTercero;
                vm.EmailTercero = data.EmailTercero;
                vm.ExtensionTercero = data.ExtensionTercero;
                vm.DescripcionIncidencia = data.DescripcionIncidencia;
                vm.Extencion = data.Extencion;
                vm.Piso = data.Piso;
                vm.Posicion = data.Posicion;
                vm.EmpleadoID = data.EmpleadoID;
                vm.NombreCompleto = data.NombreCompleto;
                vm.Correo = data.Correo;
                vm.Area = data.Area;
                vm.Estatus = data.Estatus;
                vm.PersonasAddNotificar = data.PersonasAddNotificar;
                vm.Correo = data.Correo;
                //vm.ArchivoAdjunto = data.ArchivoAdjunto;
                //
                vm.GrupoResolutor = mat.GrupoAtencion;
                vm.Prioridad = mat.Prioridad;
                //
                ViewBag.cate = cat.Categoria;
                ViewBag.subcate = subcat.SubCategoria;
                vm.Id = data.Id;

            }

            //Listas
            ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
            ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
            ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
            ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");


            return View(vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost]
        public ActionResult SetEditTicket(tbl_TicketDetalle vm, HttpPostedFileBase upload)
        {
            var data = _mng.SetEditTicket(vm);
            var Usuario = "";

            if (Session["EmpleadoNo"] != null)
            {

                Usuario = Session["EmpleadoNo"].ToString();

            }
            else
            {

                return RedirectToAction("Login", "Home");
            }

            //Carga de Archivo             
            if (upload != null && upload.ContentLength > 0)
            {
                var extension = upload.FileName.ToUpper();


                if (extension.EndsWith(".PDF") || extension.EndsWith(".DOC") || extension.EndsWith(".DOCX") || extension.EndsWith(".DOT")
                    || extension.EndsWith(".XLS") || extension.EndsWith(".XLSX") || extension.EndsWith(".XLSM") || extension.EndsWith(".XLT")
                    || extension.EndsWith(".PPT") || extension.EndsWith(".PPTX") || extension.EndsWith(".PPS") || extension.EndsWith(".JPG")
                    || extension.EndsWith(".PNG") || extension.EndsWith(".JPEG") || extension.EndsWith(".CSV"))
                {

                    //Nombre de la carga
                    var NameCarga = data + "_" + upload.FileName;
                    var uploadName = Path.GetFileName(upload.FileName);
                    //SE GUARDA EL ARCHIVO DE MANERA LOCAL
                    var path = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/"), NameCarga);
                    upload.SaveAs(path);


                    //SE GUARDA EN LA RUTA QUE SERA COMPARTIDA PARA AMBAS DIRECCIONES 35 Y 36
                    var fname = @"\\\\10.200.154.36\uFiles\ServiceDeskV2\\" + NameCarga;
                    System.IO.File.Copy(path, fname, true);
                    System.IO.File.Delete(path);
                    _doc.Upload(NameCarga, path, true);
                    try
                    {

                        //CREAR LA RUTA DEL MANAGER PARA GUARDAR LA INFO EN DetalleSubTicket
                        _mng.SetArchivoEditTicket(data, NameCarga);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return RedirectToAction("EditTicket", "App", new { folio = data });
                }
                else
                {
                    ModelState.AddModelError("File", "Formato no soportado");
                    return PartialView();

                }

            }




            return RedirectToAction("EditTicket", "App", new { folio = data, EmployeeId = Usuario });
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //NUEVO AYB REABRIR TICKET
        public ActionResult ReabrirTicket(int? Id, string folio, string EmployeeId)
        {

            if (Session["EmpleadoNo"] != null)
            {

                EmployeeId = Session["EmpleadoNo"].ToString();

            }
            else
            {

                return RedirectToAction("Login", "Home");
            }

            ViewBag.Id = string.IsNullOrEmpty(folio) ? "" : folio;

            var data = _db.tbl_TicketDetalle.Where(a => a.Id == Id).FirstOrDefault();


            var vm = new tbl_TicketDetalle();

            //Cuando manda id de ticket a reabrir
            if (folio != null)
            {
                var Idint = Convert.ToInt32(folio);

                var busq = _db.tbl_TicketDetalle.Where(a => a.Id == Idint).FirstOrDefault();

                vm.EmpleadoID = Convert.ToInt32(EmployeeId);

            }


            if (data != null)
            {
                var cat = _db.cat_Categoria.Where(a => a.Id == data.Categoria).FirstOrDefault();
                var subcat = _db.cat_SubCategoria.Where(a => a.Id == data.SubCategoria).FirstOrDefault();
                var cent = _db.cat_Centro.Where(a => a.Id == data.Centro).FirstOrDefault();
                var mat = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == data.SubCategoria).FirstOrDefault();

                vm.NombreTercero = data.NombreTercero;
                vm.EmailTercero = data.EmailTercero;
                vm.ExtensionTercero = data.ExtensionTercero;
                vm.DescripcionIncidencia = data.DescripcionIncidencia;
                vm.Extencion = data.Extencion;
                vm.Piso = data.Piso;
                vm.Posicion = data.Posicion;
                vm.EmpleadoID = data.EmpleadoID;
                vm.NombreCompleto = data.NombreCompleto;
                vm.Correo = data.Correo;
                vm.Area = data.Area;
                vm.Estatus = data.Estatus;
                vm.PersonasAddNotificar = data.PersonasAddNotificar;
                vm.Correo = data.Correo;
                //
                vm.GrupoResolutor = mat.GrupoAtencion;
                vm.Prioridad = mat.Prioridad;
                //
                ViewBag.cate = HttpUtility.HtmlDecode(cat.Categoria);
                ViewBag.Idcate = cat.Id;
                ViewBag.subcate = HttpUtility.HtmlDecode(subcat.SubCategoria);
                ViewBag.Idsubcate = subcat.Id;
                vm.Id = data.Id;


            }

            //Listas
            ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
            ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
            ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
            ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");


            return View(vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost]
        public ActionResult SetReapertura(tbl_TicketDetalle vm, HttpPostedFileBase upload)
        {
            var data = _mng.SetReapertura(vm);

            var Usuario = "";

            if (Session["EmpleadoNo"] != null)
            {

                Usuario = Session["EmpleadoNo"].ToString();

            }
            else
            {

                return RedirectToAction("Login", "Home");
            }

            //Carga de Archivo             
            if (upload != null && upload.ContentLength > 0)
            {
                var extension = upload.FileName.ToUpper();


                if (extension.EndsWith(".PDF") || extension.EndsWith(".DOC") || extension.EndsWith(".DOCX") || extension.EndsWith(".DOT")
                    || extension.EndsWith(".XLS") || extension.EndsWith(".XLSX") || extension.EndsWith(".XLSM") || extension.EndsWith(".XLT")
                    || extension.EndsWith(".PPT") || extension.EndsWith(".PPTX") || extension.EndsWith(".PPS") || extension.EndsWith(".JPG")
                    || extension.EndsWith(".PNG") || extension.EndsWith(".JPEG") || extension.EndsWith(".CSV"))
                {
                    //Nombre de la carga
                    var NameCarga = data + "_" + upload.FileName;
                    var uploadName = Path.GetFileName(upload.FileName);
                    //SE GUARDA EL ARCHIVO DE MANERA LOCAL
                    var path = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/"), NameCarga);
                    upload.SaveAs(path);


                    //SE GUARDA EN LA RUTA QUE SERA COMPARTIDA PARA AMBAS DIRECCIONES 35 Y 36
                    var fname = @"\\\\10.200.154.36\uFiles\ServiceDeskV2\\" + NameCarga;
                    System.IO.File.Copy(path, fname, true);
                    System.IO.File.Delete(path);
                    _doc.Upload(NameCarga, path, true);
                    try
                    {

                        //CREAR LA RUTA DEL MANAGER PARA GUARDAR LA INFO EN DetalleSubTicket
                        _mng.SetArchivoReapertura(data, NameCarga);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return RedirectToAction("ReabrirTicket", "App", new { folio = data });
                }
                else
                {
                    ModelState.AddModelError("File", "Formato no soportado");
                    return PartialView();

                }

            }


            return RedirectToAction("ReabrirTicket", "App", new { folio = data, EmployeeId = Usuario });

        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -


    }
}
