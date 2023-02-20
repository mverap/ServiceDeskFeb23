using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServiceDesk.Models;
using ServiceDesk.Managers;
using ServiceDesk.ViewModels; 
using System.Data;
using System.IO;
//
using System.Data.Entity.Migrations;
//
using System.Web.Security;




namespace ServiceDesk.Controllers
{
    public class SupervisorController : Controller
    {
        private readonly RHAdminContext _rh = new RHAdminContext();
        private readonly ServiceDeskManager _mng = new ServiceDeskManager();
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private readonly AdminContext _adm = new AdminContext();
        private readonly SlaManager _sla = new SlaManager();
        private readonly NotificacionesManager _noti = new NotificacionesManager();
        private readonly DocumentController _doc = new DocumentController();
        //============================================================================================================================================
        public ActionResult _MenuConfiguracion()
        {
            return View();
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public ActionResult DetalleTicket(int? IdTicket, string folio, string EmployeeId, int isChild = 0)
        {
            // copiar y pegar en cualquier actionresult que requiere mandar usuario por un tubo si se intenta pasar de listo
            var userSession = Session["EmpleadoNo"].ToString(); if (userSession != EmployeeId) { return RedirectToAction("Error", "Document"); }

            ViewBag.idChild = 0;
            var hisCCount = _db.his_Ticket.Where(t => t.IdTicket == IdTicket && t.Motivo.Contains("Ticket pasó a Control de Cambios")).Count();
            if (hisCCount > 0) {
                try {
                    ViewBag.CC = _db.tbl_CC_Dashboard.Where(t => t.Ticket == IdTicket).FirstOrDefault().id; }
                catch { 
                }
            }

            if (isChild == 1)
            {

                ViewBag.idChild = IdTicket;
                var hijo = _db.tbl_VinculacionDetalle.Where(x => x.IdTicketChild == IdTicket).FirstOrDefault();
                var padre = _db.tbl_Vinculacion.Where(x => x.IdVinculacion == hijo.IdVinculacion).FirstOrDefault();
                IdTicket = padre.IdTicket;


            }
            ViewBag.Id = string.IsNullOrEmpty(folio) ? "" : folio;


            if (IdTicket != null)
            {

                //ENVIA OBJETO CON HISTORICO Y DETALLE DE TICKET
                var detalle = new DetalleSelectedTicketVm();

                if (IdTicket != null)
                {

                    //Asigna el valor del user logueado
                    var idEmp = Int32.Parse(EmployeeId);
                    var MisDatos = _db.tbl_User.Where(t => t.EmpleadoID == idEmp).FirstOrDefault();
                    string Centro = _db.cat_Centro.Where(t => t.Id == MisDatos.Centro).Select(t => t.Centro).FirstOrDefault();
                    detalle.EmployeeIdBO = EmployeeId;
                    detalle.NombreCompleto = MisDatos.NombreTecnico;
                    detalle.Correo = MisDatos.Correo;
                    detalle.Area = Centro;


                    //Valida si el ticket tiene solicitud para aprobación
                    //Estatus de Aprobaciones
                    //    1 = En Validación
                    //    2 = Validada
                    //    3 = Rechazada

                    var Aprueba = _db.tbl_TicketDetalle.Where(a => a.Id == IdTicket && a.ApruebaReasignacion == 1).FirstOrDefault();

                    if (Aprueba != null)
                    {

                        ViewBag.Aprobar = "SI";

                    }


                    var AsignacionInfo = _db.tbl_TicketDetalle.Where(a => a.Id == IdTicket).FirstOrDefault();

                    if (AsignacionInfo.TecnicoAsignado == null)
                    {
                        ViewBag.MuestraAsignacion = "SI";
                    }


                    //Valida si es subticket

                    if (AsignacionInfo.IdTicketPrincipal != null)
                    {

                        ViewBag.SubticketInfo = "SI";
                        ViewBag.SubticketId = AsignacionInfo.IdTicketPrincipal;

                    }

                    //Valida el estatus del ticket - Gestion de Botones NUEVO AYB
                    ViewBag.EdoTicket = "";

                    if (AsignacionInfo.EstatusTicket == 1)
                    {
                        ViewBag.EdoTicket = "Abierto";

                    }
                    else if (AsignacionInfo.EstatusTicket == 2)
                    {
                        ViewBag.EdoTicket = "Asignado";
                    }
                    else if (AsignacionInfo.EstatusTicket == 3)
                    {
                        ViewBag.EdoTicket = "Trabajando";
                    }
                    else if (AsignacionInfo.EstatusTicket == 4)
                    {
                        ViewBag.EdoTicket = "Resuelto";
                    }
                    else if (AsignacionInfo.EstatusTicket == 5)//GARANTÍA
                    {

                        var his = _db.his_Ticket.Where(a => a.IdTicket == IdTicket && a.EstatusTicket == 5).OrderByDescending(a => a.FechaRegistro).FirstOrDefault();

                        var TimeNow = DateTime.Now;
                        var dias = (TimeNow - his.FechaRegistro).Days;
                        var diferencia = (TimeNow - his.FechaRegistro).Hours;
                        var horas = (dias * 24) + diferencia;

                        ViewBag.HoraGarantia = horas;

                        //Validar: sí el tiempo de garantia venció, pasar a Cerrado.
                        var gpo = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == AsignacionInfo.SubCategoria).FirstOrDefault();

                        if (horas >= Convert.ToDouble(gpo.Garantia))
                        {
                            var Fingarantia = his.FechaRegistro;

                            //sacamos horas y minutos
                            var h = Math.Truncate(Convert.ToDouble(gpo.Garantia));

                            double m = (Convert.ToDouble(gpo.Garantia) - h) * 100;

                            Fingarantia = Fingarantia.AddHours(h);
                            Fingarantia = Fingarantia.AddMinutes(m);

                            //Ajustamos a que la garantia sea tiempo de resuelto mas el garantia (sin sumar hasta datetime now)


                            _db.tbl_TicketDetalle.Attach(AsignacionInfo);
                            AsignacionInfo.Estatus = "Cerrado";
                            AsignacionInfo.EstatusTicket = 6;
                            AsignacionInfo.FechaRegistro = Fingarantia;
                            _db.SaveChanges();

                            //Guardar historico
                            _mng.SetHistoricoCambioEstatus(AsignacionInfo.Id);

                        }


                    }
                    else if (AsignacionInfo.EstatusTicket == 7)//EN ESPERA
                    {
                        ViewBag.EdoTicket = "En Espera";
                        var TimeNow = DateTime.Now;
                        var dias = (TimeNow - AsignacionInfo.FechaRegistro).Days;
                        var diferencia = (TimeNow - AsignacionInfo.FechaRegistro).Hours;
                        var horas = (dias * 24) + diferencia;

                       

                    }


                    var info = _db.his_Ticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();
                    detalle.historico = info;
                    detalle.detalle = _db.VWDetalleTicket.Find(IdTicket);

                    //Archivos adjuntos
                    var dtoDw = _db.tblDocumentos.Where(a => a.IdTicket == IdTicket && a.Tipo != 5).OrderByDescending(a => a.FechaRegisto).ToList();
                    detalle.Docs = dtoDw;
                    //

                    //var his2 = _db.his_Ticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).FirstOrDefault();
                    //detalle.detalle.GrupoResolutor = his2.GrupoResolutor;                   
                    detalle.detalle.GrupoResolutor = info.FirstOrDefault().GrupoResolutor;
                    ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Activo), "Id", "Estado");
                    switch (detalle.detalle.EstatusTicket) {
                        case 2:
                            ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Id == 3), "Id", "Estado");
                            break;
                        case 3:
                            ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Id == 4 || x.Id == 7), "Id", "Estado");
                            break;
                        case 4:
                            ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Id == 5), "Id", "Estado");
                            break;
                        case 5:
                            ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Id == 6), "Id", "Estado");
                            break;
                        case 7:
                            ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Id == 3), "Id", "Estado");
                            break;
                    }

                    // Filtrar diagnosticos acorde a grupo reoslutor ---------------- START
                    string grupo = detalle.detalle.GrupoResolutor; 
                        if (grupo == null) grupo = "";
                    int idGrupoRes = _db.catGrupoResolutor.Where(t => t.Grupo == grupo).Select(t => t.Id).FirstOrDefault(); 
                    var categoriasDelGrupo = _db.cat_Categoria.Where(t => t.GrupoResolutor == idGrupoRes).Select(t => t.Id).ToArray(); 
                    var diags = _db.catDiagnosticos.Where(t => categoriasDelGrupo.Contains(t.IdCategoria));
                    ViewBag.DX = new SelectList(diags, "Diagnostico", "Diagnostico");
                    // Filtrar diagnosticos acorde a grupo reoslutor ---------------- END

                    //===========
                    ViewBag.GrupoResolutorCat = new SelectList(_db.catGrupoResolutor.Where(x => x.Activo), "Id", "Grupo");
                    ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
                    ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
                    ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.UserLst = new SelectList(_db.tbl_User.Where(x => x.Activo && x.GrupoResolutor == AsignacionInfo.GrupoResolutor).OrderBy(x=>x.Id), "Id", "NombreTecnico");

                    //Asignar lista
                    //ListSubticket

                    var slaPadre = new List<SlaTimesVm>(); bool EsHijo = false;
                    var lstSub = _db.vwDetalleSubticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();
                    //---------
                    if (isChild == 0)
                    {
                        var Vinculado = _db.tbl_Vinculacion.Where(x => x.Activo == true && x.IdTicket == IdTicket).FirstOrDefault();
                        if (Vinculado != null)
                        {
                            List<tbl_VinculacionDetalle> lstVinculacions = new List<tbl_VinculacionDetalle>();
                            var hijos = _db.tbl_VinculacionDetalle.Where(x => x.IdVinculacion == Vinculado.IdVinculacion).ToList();
                            hijos.ForEach(x =>
                            {
                                if (x.IdTicketChild != Vinculado.IdTicket)
                                {
                                    x.Fecha = detalle.detalle.FechaRegistro;
                                    x.usuario = detalle.detalle.NombreCompleto;

                                    TimeSpan? timeSpan1 = (DateTime.Now - detalle.detalle.FechaRegistro);
                                    double hours1 = 0;
                                    int minutes1 = 0;
                                    if (timeSpan1.Value.Days > 0)
                                    {
                                        hours1 = timeSpan1.Value.Days * 24 + timeSpan1.Value.Hours;
                                        minutes1 = timeSpan1.Value.Minutes;
                                    }
                                    else
                                    {
                                        hours1 = timeSpan1.Value.Hours;
                                        minutes1 = timeSpan1.Value.Minutes;
                                    }
                                    x.hours = hours1;
                                    x.minutes = minutes1;
                                    x.tiempoSLA = hours1.ToString() + ":" + minutes1.ToString();
                                    lstVinculacions.Add(x);
                                }
                            });


                            detalle.lstVinculacion = lstVinculacions;

                        }
                        else //------------ relojes SLA
                        {
                            EsHijo = true;
                            var idPadre = _db.tbl_VinculacionDetalle.Where(t => t.IdTicketChild == IdTicket).FirstOrDefault();
                            if (idPadre != null) { 
                                var hisPadre = _db.his_Ticket.Where(t => t.IdTicket == idPadre.TicketPrincipal).ToList();
                                slaPadre = _sla.GetSlaTimes(hisPadre);
                            }
                        }
                        //detalle.lstVinculacion.Add()
                        detalle.ListSubticket = lstSub;
                    }
                    
                    detalle.Slas = _sla.GetSlaTimes(info);

                    if (EsHijo) //------------ relojes SLA
                    {
                        string[] tipoSla = { "Tiempo Actual", "SLA Objetivo" };

                        foreach (var slatype in tipoSla)
                        {
                            var sh = detalle.Slas.Where(t => t.Type == slatype).FirstOrDefault();
                            var sp = slaPadre.Where(t => t.Type == slatype).FirstOrDefault();
                            if (sh != null && sp != null)
                            {
                                detalle.Slas.Where(t => t.Type == slatype).FirstOrDefault().Time = sp.Time;
                                detalle.Slas.Where(t => t.Type == slatype).FirstOrDefault().Color = sp.Color;
                                detalle.Slas.Where(t => t.Type == slatype).FirstOrDefault().Tecnico = sp.Tecnico;
                            }
                        }
                    }
                }

                return View(detalle);
            }

            return View();
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost] public JsonResult ShowEditarNivel(int id)
        {
            try
            {

                //Cambiarlo por procedure
                var data = _db.catNivelExperiencia.Where(a => a.Id == id).FirstOrDefault();

                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error:" + ex, JsonRequestBehavior.AllowGet);
            }

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost] public JsonResult SaveNivelExperiencia(string Nivel)
        {
            var result = "";
            var vm = new catNivelExperiencia();
            try
            {

                //Valida que ya existe el nivel
                vm.Nivel = Nivel;
                vm.Activo = true;
                _db.catNivelExperiencia.Add(vm);
                _db.SaveChanges();
                result = "Correcto";


            }
            catch (Exception e)
            {
                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost] public JsonResult getEditNivel(int id, string Nivel)
        {
            var result = "";
            try
            {
                //Valida que ya existe el nivel
                var info = _db.catNivelExperiencia.Where(a => a.Id == id).FirstOrDefault();

                if (info != null)
                {
                    _db.catNivelExperiencia.Attach(info);
                    info.Nivel = Nivel;
                    _db.SaveChanges();
                    result = "Correcto";
                }

            }
            catch (Exception e)
            {
                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost] public JsonResult getDeleteNivel(int id)
        {
            var result = "";
            try
            {
                //Valida que ya existe el nivel
                var info = _db.catNivelExperiencia.Where(a => a.Id == id).FirstOrDefault();

                if (info != null)
                {
                    _db.catNivelExperiencia.Attach(info);
                    info.Activo = false;
                    _db.SaveChanges();
                    result = "Correcto";
                }

            }
            catch (Exception e)
            {
                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost] public JsonResult getdataUsuario(int id)
        {
            ;
            try
            {

                var data = _db.Database.SqlQuery<InfoUsuario>("EXEC dbo.get_InfoUsuario @Id={0}", id).FirstOrDefault();

                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost] public JsonResult delExlusionCentros(int id)
        {
            // Responde la pregunta si cierto usuario está en la lista de exclusiones de filtro de centros TI
            try
            {
                var exclusion = _db.tbl_User_TI_Exclusion_Filtro_Centro.Where(t => t.EmployeeId == id).FirstOrDefault();
                if (exclusion != null)
                {

                    _db.tbl_User_TI_Exclusion_Filtro_Centro.Remove(exclusion);
                    _db.SaveChanges();
                    return Json("Exclusion Deleted", JsonRequestBehavior.AllowGet);
                }
                else { 
                    return Json("Exclusion Not Found", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json("Error, unable to delete exclusion:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost] public JsonResult addExlusionCentros(int id)
        {
            // Responde la pregunta si cierto usuario está en la lista de exclusiones de filtro de centros TI
            try
            {
                var exclusion = new tbl_User_TI_Exclusion_Filtro_Centro();
                exclusion.EmployeeId = id;
                _db.tbl_User_TI_Exclusion_Filtro_Centro.Add(exclusion);
                _db.SaveChanges();
                return Json("Exclusion Added", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error, unable to add exclusion:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost] public JsonResult checkIfExlusionCentros(int id)
        {
            // Responde la pregunta si cierto usuario está en la lista de exclusiones de filtro de centros TI
            try
            {
                string result = "";
                var data = _db.tbl_User_TI_Exclusion_Filtro_Centro.Where(t => t.EmployeeId == id).FirstOrDefault();
                result = (data != null) ? "yes" : "no";
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error, unable to check for exclusion for this employee:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost] public JsonResult checkIfSupervisor(int id)
        {
            // Responde la pregunta si cierto usuario es supervisor, usando su id como input
            try
            {
                string rol = RoldeUsuario(id);
                rol = (rol == "Supervisor")? "yes" : "no";
                return Json(rol, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        //fillCentrosSupervisor
        [HttpPost] public JsonResult fillCentrosSupervisor(int EmployeeId)
        {
            // Sube los centros que estará supervisando un supervisor de ti-soporte
            try
            {
                var User = _db.tbl_User.Where(t => t.EmpleadoID == EmployeeId).FirstOrDefault();
                var PrevRel = _db.tbl_rel_SupervisorCentros.Where(t => t.UserId == User.Id).Select(t => t.CentroId).ToList();
                var centros = _db.cat_Centro.Where(t => PrevRel.Contains(t.Id)).Select(t => t.Centro).ToArray();

                return Json(centros, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost] public JsonResult uploadRelCenter(string[] centros, int id)
        {
            // Sube los centros que estará supervisando un supervisor de ti-soporte
            try
            {
                string rol = "yes";
                var User = _db.tbl_User.Where(t => t.EmpleadoID == id).FirstOrDefault();
                var PrevRel = _db.tbl_rel_SupervisorCentros.Where(t => t.UserId == User.Id).ToList();

                if (centros != null) {
                    if (PrevRel != null)
                        foreach (var rels in PrevRel) { _db.tbl_rel_SupervisorCentros.Remove(rels); }
                    _db.SaveChanges();

                    foreach (var centro in centros) {
                        var Centro = _db.cat_Centro.Where(t => t.Centro == centro).FirstOrDefault();

                        if (Centro != null && User != null) {

                            var rel = new tbl_rel_SupervisorCentros();
                            rel.CentroId = Centro.Id;
                            rel.UserId = User.Id;
                            _db.tbl_rel_SupervisorCentros.Add(rel);
                            _db.SaveChanges();
                        }
                    }
                }
          

                return Json(rol, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost] public JsonResult getDeleteUsuario(int id)
        {

            var result = "";

            //Valida si es un Editar Usuario
            var upduser = _db.tbl_User.Where(a => a.Id == id).FirstOrDefault();

            try
            {
                using (var con = new ServiceDeskContext())
                {

                    _db.tbl_User.Attach(upduser);
                    upduser.Activo = false;
                    _db.SaveChanges();

                    result = "Correcto";

                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error:" + ex, JsonRequestBehavior.AllowGet);
            }

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost] public ActionResult GuardarUser(DetalleGestiones vm)
        {

            //Guarda los tecnicos
            var usu = new tbl_User();
            var ven = new tbl_VentanaAtencion();
            var jor = new tbl_JornadaLaboral();

            //Valida si es un Editar Usuario
            var upduser = _db.tbl_User.Where(a => a.EmpleadoID == vm.user.EmpleadoID).FirstOrDefault();
            var updatc = _db.tbl_VentanaAtencion.Where(a => a.EmpleadoID == vm.user.EmpleadoID).FirstOrDefault();
            var updjor = _db.tbl_JornadaLaboral.Where(a => a.EmpleadoID == vm.user.EmpleadoID).FirstOrDefault();

            var grupoint = Convert.ToInt32(vm.user.GrupoResolutor);
            var GrupoInfo = _db.catGrupoResolutor.Where(x => x.Id == grupoint).FirstOrDefault();

            if (upduser != null && updatc != null && updjor != null)
            {
                
                using (var con = new ServiceDeskContext())
                {

                    _db.tbl_User.Attach(usu);
                    upduser.EmpleadoID = vm.user.EmpleadoID;
                    upduser.NombreTecnico = vm.user.NombreTecnico;
                    upduser.GrupoResolutor = GrupoInfo.Grupo;
                    upduser.Centro = vm.user.Centro;
                    upduser.HoraInicioATC = vm.atc.HoraInicioATC;
                    upduser.HoraFinATC = vm.atc.HoraFinATC;
                    upduser.HoraInicioJornada = vm.lab.HoraInicioJornada;
                    upduser.HoraFinJornada = vm.lab.HoraFinJornada;
                    upduser.Correo = vm.user.Correo;
                    upduser.NivelExperiencia = vm.user.NivelExperiencia;
                    upduser.FechaRegistro = DateTime.Now;
                    upduser.Activo = true;
                    _db.SaveChanges();
                    //
                    //Guarda la ventana de servicio
                    _db.tbl_VentanaAtencion.Attach(ven);
                    updatc.EmpleadoID = vm.user.EmpleadoID;
                    updatc.Lunes = vm.atc.Lunes;
                    updatc.Martes = vm.atc.Martes;
                    updatc.Miercoles = vm.atc.Miercoles;
                    updatc.Jueves = vm.atc.Jueves;
                    updatc.Viernes = vm.atc.Viernes;
                    updatc.Sabado = vm.atc.Sabado;
                    updatc.Domingo = vm.atc.Domingo;
                    updatc.HoraInicioATC = vm.atc.HoraInicioATC;
                    updatc.HoraFinATC = vm.atc.HoraFinATC;
                    updatc.FechaRegistro = DateTime.Now;
                    _db.SaveChanges();
                    //
                    //Guarda la jornada laboral            
                    _db.tbl_JornadaLaboral.Attach(jor);
                    updjor.EmpleadoID = vm.user.EmpleadoID;
                    updjor.Lunes = vm.lab.Lunes;
                    updjor.Martes = vm.lab.Martes;
                    updjor.Miercoles = vm.lab.Miercoles;
                    updjor.Jueves = vm.lab.Jueves;
                    updjor.Viernes = vm.lab.Viernes;
                    updjor.Sabado = vm.lab.Sabado;
                    updjor.Domingo = vm.lab.Domingo;
                    updjor.HoraInicioJornada = vm.lab.HoraInicioJornada;
                    updjor.HoraFinJornada = vm.lab.HoraFinJornada;
                    updjor.FechaRegistro = DateTime.Now;
                    _db.SaveChanges();

                };


            }
            else
            {
                //Buscar su username en la tabla tbl_User del Admin Desarrollo
                var useradm = _adm.tblUser.Where(a => a.EmpleadoId == vm.user.EmpleadoID).FirstOrDefault();

                using (var con = new ServiceDeskContext())
                {

                    usu.EmpleadoID = vm.user.EmpleadoID;
                    usu.NombreTecnico = vm.user.NombreTecnico;
                    usu.GrupoResolutor = GrupoInfo.Grupo;
                    usu.Centro = vm.user.Centro;
                    usu.HoraInicioATC = vm.atc.HoraInicioATC;
                    usu.HoraFinATC = vm.atc.HoraFinATC;
                    usu.HoraInicioJornada = vm.lab.HoraInicioJornada;
                    usu.HoraFinJornada = vm.lab.HoraFinJornada;
                    usu.Correo = vm.user.Correo;
                    usu.NivelExperiencia = vm.user.NivelExperiencia;
                    usu.FechaRegistro = DateTime.Now;
                    usu.Activo = true;
                    _db.tbl_User.Add(usu);
                    _db.SaveChanges();
                    //
                    //Guarda la ventana de servicio
                    ven.EmpleadoID = vm.user.EmpleadoID;
                    ven.Lunes = vm.atc.Lunes;
                    ven.Martes = vm.atc.Martes;
                    ven.Miercoles = vm.atc.Miercoles;
                    ven.Jueves = vm.atc.Jueves;
                    ven.Viernes = vm.atc.Viernes;
                    ven.Sabado = vm.atc.Sabado;
                    ven.Domingo = vm.atc.Domingo;
                    ven.HoraInicioATC = vm.atc.HoraInicioATC;
                    ven.HoraFinATC = vm.atc.HoraFinATC;
                    ven.FechaRegistro = DateTime.Now;
                    _db.tbl_VentanaAtencion.Add(ven);
                    _db.SaveChanges();
                    //
                    //Guarda la jornada laboral            
                    jor.EmpleadoID = vm.user.EmpleadoID;
                    jor.Lunes = vm.lab.Lunes;
                    jor.Martes = vm.lab.Martes;
                    jor.Miercoles = vm.lab.Miercoles;
                    jor.Jueves = vm.lab.Jueves;
                    jor.Viernes = vm.lab.Viernes;
                    jor.Sabado = vm.lab.Sabado;
                    jor.Domingo = vm.lab.Domingo;
                    jor.HoraInicioJornada = vm.lab.HoraInicioJornada;
                    jor.HoraFinJornada = vm.lab.HoraFinJornada;
                    jor.FechaRegistro = DateTime.Now;
                    _db.tbl_JornadaLaboral.Add(jor);
                    _db.SaveChanges();

                };

            }


            return RedirectToAction("Gestiones", "Supervisor", new { User = usu.Id, EmployeeId = vm.EmployeeIdBO });

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        //DETALLE DEL TICKET VINCULADO
        public ActionResult TicketVinculado(int? IdTicket)
        {

            if (IdTicket != null)
            {

                //ENVIA OBJETO CON HISTORICO Y DETALLE DE TICKET
                var detalle = new DetalleSelectedTicketVm();

                if (IdTicket != null)
                {

                    var AsignacionInfo = _db.tbl_TicketDetalle.Where(a => a.Id == IdTicket).FirstOrDefault();

                    if (AsignacionInfo.TecnicoAsignado == null)
                    {
                        ViewBag.MuestraAsignacion = "SI";
                    }


                    var info = _db.his_Ticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();



                    detalle.historico = info;
                    detalle.detalle = _db.VWDetalleTicket.Find(IdTicket);

                    ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Activo), "Id", "Estado");
                    ViewBag.DX = new SelectList(_db.catDiagnosticos, "Diagnostico", "Diagnostico");

                    //===========
                    ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
                    ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
                    ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.UserLst = new SelectList(_db.tbl_User.Where(x => x.Activo && x.GrupoResolutor == AsignacionInfo.GrupoResolutor), "Id", "NombreTecnico");


                    var lstSub = _db.vwDetalleSubticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();


                    detalle.Slas = _sla.GetSlaTimes(info);

                    //------------ relojes SLA del padre
                    try { 
                        var slaPadre = new List<SlaTimesVm>(); 
                        var idPadre = _db.tbl_VinculacionDetalle.Where(t => t.IdTicketChild == IdTicket).FirstOrDefault().TicketPrincipal;
                        var hisPadre = _db.his_Ticket.Where(t => t.IdTicket == idPadre).ToList();
                        slaPadre = _sla.GetSlaTimes(hisPadre);
                        string[] tipoSla = { "Tiempo Actual", "SLA Objetivo" };
                        foreach (var slatype in tipoSla)
                        {
                            var sh = detalle.Slas.Where(t => t.Type == slatype).FirstOrDefault();
                            var sp = slaPadre.Where(t => t.Type == slatype).FirstOrDefault();
                            if (sh != null && sp != null)
                            {
                                detalle.Slas.Where(t => t.Type == slatype).FirstOrDefault().Time = sp.Time;
                                detalle.Slas.Where(t => t.Type == slatype).FirstOrDefault().Color = sp.Color;
                                detalle.Slas.Where(t => t.Type == slatype).FirstOrDefault().Tecnico = sp.Tecnico;
                            }
                        }}
                    catch { }
                    
                }

                return View(detalle);
            }


            return View();

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        //DETALLE DEL SUBTICKET
        public ActionResult DetalleSubticket(int? IdTicket, string EmployeeId)
        {

            if (IdTicket != null)
            {

                //ENVIA OBJETO CON HISTORICO Y DETALLE DE TICKET
                var detalle = new DetalleSelectedTicketVm();

                if (IdTicket != null)
                {
                    //Asigna el valor del user logueado
                    detalle.EmployeeIdBO = EmployeeId;

                    var AsignacionInfo = _db.tbl_TicketDetalle.Where(a => a.Id == IdTicket).FirstOrDefault();

                    if (AsignacionInfo.TecnicoAsignado == null)
                    {
                        ViewBag.MuestraAsignacion = "SI";
                    }


                    var info = _db.his_Ticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();

                    //Archivos adjuntos
                    var dtoDw = _db.tblDocumentos.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegisto).ToList();
                    detalle.Docs = dtoDw;
                    //

                    detalle.historico = info;
                    detalle.detalle = _db.VWDetalleTicket.Find(IdTicket);

                    ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Activo), "Id", "Estado");
                    ViewBag.DX = new SelectList(_db.catDiagnosticos, "Diagnostico", "Diagnostico");

                    //===========
                    ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
                    ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
                    ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.UserLst = new SelectList(_db.tbl_User.Where(x => x.Activo && x.GrupoResolutor == AsignacionInfo.GrupoResolutor), "Id", "NombreTecnico");

                    var lstSub = _db.vwDetalleSubticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();

                    //
                    if (AsignacionInfo.EstatusTicket == 5)//GARANTÍA
                    {

                        var TimeNow = DateTime.Now;
                        var dias = (TimeNow - AsignacionInfo.FechaRegistro).Days;
                        var diferencia = (TimeNow - AsignacionInfo.FechaRegistro).Hours;
                        var horas = (dias * 24) + diferencia;

                        ViewBag.HoraGarantia = horas;

                        //Validar: sí el tiempo de garantia venció, pasar a Cerrado.
                        var gpo = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == AsignacionInfo.SubCategoria).FirstOrDefault();

                        if (horas >= Convert.ToDouble(gpo.Garantia))
                        {

                            _db.tbl_TicketDetalle.Attach(AsignacionInfo);
                            AsignacionInfo.Estatus = "Cerrado";
                            AsignacionInfo.EstatusTicket = 6;
                            AsignacionInfo.FechaRegistro = DateTime.Now;
                            _db.SaveChanges();

                            //Guardar historico
                            _mng.SetHistoricoCambioEstatus(AsignacionInfo.Id);

                        }


                    }
                    else if (AsignacionInfo.EstatusTicket == 7)//EN ESPERA
                    {

                        var TimeNow = DateTime.Now;
                        var dias = (TimeNow - AsignacionInfo.FechaRegistro).Days;
                        var diferencia = (TimeNow - AsignacionInfo.FechaRegistro).Hours;
                        var horas = (dias * 24) + diferencia;

                        

                    }
                   
                    detalle.Slas = _sla.GetSlaTimes(info);


                }

                return View(detalle);
            }


            return View();

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public JsonResult CancelarTicket(int TicketId, string Motivo)
        {
            var result = "";

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            if (info != null)
            {

                info.Comentarios = Motivo;
                info.Estatus = "Cancelado";
                info.EstatusTicket = 8;
                _db.tbl_TicketDetalle.AddOrUpdate(info);
                _db.SaveChanges();


                //Se agrega en el historico
                _mng.SaveHistoricoCancelacion(TicketId);

                
                result = "Correcto";

            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -                
        [HttpPost]
        public JsonResult ValidaReasignaciones(int TicketId)
        {

            var result = "";

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            if (info.NoReasignaciones >= 10)
            {

                result = "Mayor";

            }

            return Json(result, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -                
        [HttpPost]
        public JsonResult AsignarTecnico(int TicketId, int Tecnico, int Reasigna)
        {
            var result = "";
            var NoReasignacion = 1;

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            //buscar el tecnico y guardar su id
            var user = _db.tbl_User.Where(a => a.Id == Tecnico).FirstOrDefault();

            if (Reasigna != 0)//Es reasignación
            {

                if (info != null)
                {

                    if (info.NoReapertura == 1)
                    {
                        _db.tbl_TicketDetalle.Attach(info);
                        info.TecnicoAsignadoReag = user.NombreTecnico;
                        info.IdTecnicoAsignadoReag = Tecnico;
                        info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                        info.Estatus = "Asignado";
                        info.EstatusTicket = 2;
                        _db.SaveChanges();

                        //Guardar Historico
                        _mng.SaveHistoricoUser(info);

                        result = "Correcto";

                    }
                    else if (info.NoReapertura == 2)
                    {
                        _db.tbl_TicketDetalle.Attach(info);
                        info.TecnicoAsignadoReag2 = user.NombreTecnico;
                        info.IdTecnicoAsignadoReag2 = Tecnico;
                        info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                        info.Estatus = "Asignado";
                        info.EstatusTicket = 2;
                        _db.SaveChanges();

                        //Guardar Historico
                        _mng.SaveHistoricoUser(info);

                        result = "Correcto";

                    }
                    else
                    {

                        _db.tbl_TicketDetalle.Attach(info);
                        info.TecnicoAsignado = user.NombreTecnico;
                        info.IdTecnicoAsignado = Tecnico;
                        info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                        info.Estatus = "Asignado";
                        info.EstatusTicket = 2;
                        _db.SaveChanges();

                        //Guardar Historico
                        _mng.SaveHistoricoUser(info);

                        result = "Correcto";

                    }

                }


            }
            else
            {

                if (info.NoReapertura == 1)
                {
                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignadoReag = user.NombreTecnico;
                    info.IdTecnicoAsignadoReag = Tecnico;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.Estatus = "Asignado";
                    info.EstatusTicket = 2;
                    _db.SaveChanges();

                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);

                    result = "Correcto";

                }
                else if (info.NoReapertura == 2)
                {
                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignadoReag2 = user.NombreTecnico;
                    info.IdTecnicoAsignadoReag2 =Tecnico;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.Estatus = "Asignado";
                    info.EstatusTicket = 2;
                    _db.SaveChanges();

                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);

                    result = "Correcto";

                }
                else
                {

                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignado = user.NombreTecnico;
                    info.IdTecnicoAsignado = Tecnico;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.Estatus = "Asignado";
                    info.EstatusTicket = 2;
                    _db.SaveChanges();

                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);

                    result = "Correcto";

                }

            }

            AsignarTecnicoTicketsVinculados(info); // aplicar la asignación del tecnico a tickets vinculados
            return Json(result, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -                
        [HttpPost]
        public JsonResult ApruebaAsignacion(int TicketId, int Tecnico)
        {
            var result = "";
            var NoReasignacion = 1;

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            var user = _db.tbl_User.Where(x => x.Id==Tecnico).FirstOrDefault();

            if (info != null)
            {
                //Validar si tuvo reaperturas
                if (info.NoReapertura == 1)
                {

                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignadoReag = user.NombreTecnico;
                    info.IdTecnicoAsignadoReag= Tecnico;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.ApruebaReasignacion = 2;  // 2 = Validada
                    info.MotivoCambioEstatus = "Motivo de Reasignación: " + info.MotivoReasignacion;
                    info.EstatusTicket = 2;
                    info.Estatus = "Asignado";
                    _db.SaveChanges();

                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);

                    result = "Correcto";

                }
                else if (info.NoReapertura == 2)
                {

                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignadoReag2 = user.NombreTecnico;
                    info.IdTecnicoAsignadoReag2 = Tecnico;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.ApruebaReasignacion = 2;  // 2 = Validada
                    info.MotivoCambioEstatus = "Motivo de Reasignación: " + info.MotivoReasignacion;
                    info.EstatusTicket = 2;
                    info.Estatus = "Asignado";
                    _db.SaveChanges();

                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);

                    result = "Correcto";

                }
                else
                {
                    //Es primera vez
                    //No de reasignaciones = 1
                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignado = user.NombreTecnico;
                    info.IdTecnicoAsignado= Tecnico;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.ApruebaReasignacion = 2;  // 2 = Validada
                    info.MotivoCambioEstatus = "Motivo de Reasignación: " + info.MotivoReasignacion;
                    info.EstatusTicket = 2;
                    info.Estatus = "Asignado";
                    _db.SaveChanges();

                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);

                    result = "Correcto";

                }


            }

            AsignarTecnicoTicketsVinculados(info); // aplicar la asignación del tecnico a tickets vinculados
            return Json(result, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -                
        [HttpPost]
        public JsonResult RechazaAsignacion(int TicketId)
        {
            var result = "";

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            if (info != null)
            {

                _db.tbl_TicketDetalle.Attach(info);
                info.ApruebaReasignacion = 3;  // 3 = Rechazada
                _db.SaveChanges();

                _noti.SetNotiRechazoReasignacion(TicketId);

                result = "Correcto";

            }

            return Json(result, JsonRequestBehavior.AllowGet);

        }

        public void AsignarTecnicoTicketsVinculados(tbl_TicketDetalle datos) // Poner en el historico de los tickets vinculados el hecho de que un tecnico ha sido asignado al ticket principal
        {
            

            //Validar si tiene tickets Vinculados. - SI tiene, cambiar el estatus de cada uno
            var VincTkt = _db.tbl_VinculacionDetalle.Where(a => a.TicketPrincipal == datos.Id).ToList();

            ;
            for (int i = 0; i < VincTkt.Count; i++)
            {
                //Busca el ticket vinculado en la tabla detalles
                var idticketchild = VincTkt[i].IdTicketChild;

                var busc = _db.tbl_TicketDetalle.Where(a => a.Id == idticketchild).FirstOrDefault();

                if (busc != null )
                {
                    if (busc.Id != datos.Id) { 

                        _db.tbl_TicketDetalle.Attach(busc);
                        busc.MotivoCambioEstatus = datos.MotivoCambioEstatus;
                        busc.Estatus = datos.Estatus;
                        busc.EstatusTicket = datos.EstatusTicket;
                        busc.FechaRegistro = DateTime.Now;

                        busc.IdTecnicoAsignado = datos.IdTecnicoAsignado;
                        busc.IdTecnicoAsignadoReag = datos.IdTecnicoAsignadoReag;
                        busc.IdTecnicoAsignadoReag2 = datos.IdTecnicoAsignadoReag2;
                  
                        busc.TecnicoAsignado = datos.TecnicoAsignado;
                        busc.TecnicoAsignadoReag = datos.TecnicoAsignadoReag;   
                        busc.TecnicoAsignadoReag2 = datos.TecnicoAsignadoReag2;
                    
                        _db.SaveChanges();

                        //Guarda historico
                        _mng.SetHistoricoVinculados(busc.Id);
                    }

                }

            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  - - - - - - - - - - - - - - - - - - - - - - - -                
        [HttpPost] public JsonResult RecategorizarTicket(int TicketId, int Categoria, int Subcategoria, int Centro)
        {
            var result = "";

            //1.- Traera el primer grupo asignado
            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();


            //2.- Traera el grupo de acuerdo a la subcategoria
            var matrizCat = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == Subcategoria).FirstOrDefault();


            if (info.GrupoResolutor == matrizCat.GrupoAtencion) //Si es al mismo grupo resolutor
            {

                info.Categoria = Categoria;
                info.SubCategoria = Subcategoria;
                info.Centro = Centro; //ISF
                info.GrupoResolutor = matrizCat.GrupoAtencion;
                info.EstatusTicket = info.EstatusTicket;
                info.Estatus = info.Estatus;
                _db.tbl_TicketDetalle.AddOrUpdate(info);
                _db.SaveChanges();



                //Agregarlo como nuevo registro
                var dtoHis = new his_Ticket();


                dtoHis.IdTicket = TicketId;
                dtoHis.EmpleadoID = info.EmpleadoID;
                dtoHis.TicketTercero = info.TicketTercero;
                dtoHis.Extencion = info.Extencion;
                dtoHis.NombreTercero = info.NombreTercero;
                dtoHis.Piso = info.Piso;
                dtoHis.EmailTercero = info.EmailTercero;
                dtoHis.Posicion = info.Posicion;
                dtoHis.NombreCompleto = info.NombreCompleto;
                dtoHis.Correo = info.Correo;
                dtoHis.Area = info.Area;
                dtoHis.Categoria = Categoria;
                dtoHis.Centro = info.Centro;
                dtoHis.SubCategoria = Subcategoria;
                dtoHis.DescripcionIncidencia = info.DescripcionIncidencia;
                dtoHis.PersonasAddNotificar = info.PersonasAddNotificar;
                dtoHis.GrupoResolutor = matrizCat.GrupoAtencion;
                dtoHis.Prioridad = info.Prioridad;
                dtoHis.Estatus = info.Estatus;
                dtoHis.EstatusTicket = info.EstatusTicket;
                dtoHis.Historial = false;
                dtoHis.FechaRegistro = DateTime.Now;
                dtoHis.NoReapertura = info.NoReapertura;
                dtoHis.TecnicoAsignadoReag = info.TecnicoAsignadoReag;
                dtoHis.TecnicoAsignadoReag2 = info.TecnicoAsignadoReag2;
                dtoHis.TecnicoAsignado = info.TecnicoAsignado;
                _db.his_Ticket.Add(dtoHis);
                _db.SaveChanges();


                _noti.SetNotiRecategorizacion(dtoHis);

                result = "Correcto";

            }
            else
            {

                //1.1.- Validar número de reaperturas para limpiar el nombre del Tecnico
                if (info.NoReapertura == 0)
                {

                    info.TecnicoAsignado = null;
                    info.IdTecnicoAsignado = 0;
                }
                else if (info.NoReapertura == 1)
                {
                    info.TecnicoAsignadoReag = null;
                    info.IdTecnicoAsignadoReag = 0;

                }
                else
                {
                    info.TecnicoAsignadoReag2 = null;
                    info.IdTecnicoAsignadoReag2 = 0;

                }

                //Valida si la recategorización es a otro grupo resolutor
                //De ser así, el ticket pasa como Abierto y el tecnico vacio.

                info.Categoria = Categoria;
                info.SubCategoria = Subcategoria;
                info.Centro = Centro; //ISF
                info.GrupoResolutor = matrizCat.GrupoAtencion;
                info.EstatusTicket = 1;
                info.Estatus = "Abierto";
                info.TecnicoAsignado = info.TecnicoAsignado;
                info.IdTecnicoAsignado = info.IdTecnicoAsignado;
                info.TecnicoAsignadoReag = info.TecnicoAsignadoReag;
                info.IdTecnicoAsignadoReag = info.IdTecnicoAsignadoReag;
                info.TecnicoAsignadoReag2 = info.TecnicoAsignadoReag2;
                info.IdTecnicoAsignadoReag2 = info.IdTecnicoAsignadoReag2;
                _db.tbl_TicketDetalle.AddOrUpdate(info);
                _db.SaveChanges();


                //Se agrega registro nuevo en el historico por recategorización
                var dtoHis = _db.his_Ticket.Where(a => a.IdTicket == TicketId).FirstOrDefault();

                //
                dtoHis.IdTicket = TicketId;
                dtoHis.EmpleadoID = info.EmpleadoID;
                dtoHis.TicketTercero = info.TicketTercero;
                dtoHis.Extencion = info.Extencion;
                dtoHis.NombreTercero = info.NombreTercero;
                dtoHis.Piso = info.Piso;
                dtoHis.EmailTercero = info.EmailTercero;
                dtoHis.Posicion = info.Posicion;
                dtoHis.NombreCompleto = info.NombreCompleto;
                dtoHis.Correo = info.Correo;
                dtoHis.Area = info.Area;
                dtoHis.Categoria = Categoria;//*
                dtoHis.Centro = info.Centro;
                dtoHis.SubCategoria = Subcategoria;//*
                dtoHis.DescripcionIncidencia = info.DescripcionIncidencia;
                dtoHis.PersonasAddNotificar = info.PersonasAddNotificar;
                dtoHis.GrupoResolutor = matrizCat.GrupoAtencion;//*
                dtoHis.Prioridad = matrizCat.Prioridad;//*
                dtoHis.Estatus = "Abierto";//Reapertura//*
                dtoHis.EstatusTicket = 1;//*
                dtoHis.Historial = false;//*
                dtoHis.FechaRegistro = DateTime.Now;//*
                dtoHis.NoReapertura = info.NoReapertura;
                dtoHis.TecnicoAsignado = info.TecnicoAsignado;
                dtoHis.TecnicoAsignadoReag = info.TecnicoAsignadoReag;
                dtoHis.TecnicoAsignadoReag2 = info.TecnicoAsignadoReag2;
                _db.his_Ticket.Add(dtoHis);
                _db.SaveChanges();

                _noti.SetNotiRecategorizacion(dtoHis);

                result = "Correcto";


            }



            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult ValidaSubticket(int TicketId)
        {

            var result = "";

            var info = _db.tbl_TicketDetalle.Where(a => a.IdTicketPrincipal == TicketId).ToList();

            if (info.Count() >= 5)
            {

                result = "Mayor";

            }

            return Json(result, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //Guarda Subticket
        [HttpPost] public ActionResult SetSubticket(DetalleSelectedTicketVm vm, HttpPostedFileBase upload)
        {
            var datos = new tbl_TicketDetalle();

            //Buscar la información del ticket principal
            var padre = _db.tbl_TicketDetalle.Where(a => a.Id == vm.subticket.IdTicket).FirstOrDefault();
            var TenicoCreadorDeSubticket = Int32.Parse(vm.EmployeeIdBO);// new
            var user = _db.tbl_User.Where(t => t.EmpleadoID == TenicoCreadorDeSubticket).FirstOrDefault();

            if (false)
            using (var con = new ServiceDeskContext())
            {
                datos.IdTicketPrincipal = vm.subticket.IdTicket;
                datos.EmpleadoID = padre.EmpleadoID;
                datos.TicketTercero = padre.TicketTercero;
                datos.Extencion = padre.Extencion;
                datos.NombreTercero = padre.NombreTercero;
                datos.Piso = padre.Piso;
                datos.EmailTercero = padre.EmailTercero;
                datos.ExtensionTercero = padre.ExtensionTercero;
                datos.Posicion = padre.Posicion;
                datos.NombreCompleto = padre.NombreCompleto;
                datos.Area = padre.Area;
                datos.PersonasAddNotificar = padre.PersonasAddNotificar;
                datos.Correo = padre.Correo;
                datos.Comentarios = vm.subticket.DescIncidencia;
                datos.TecnicoAsignado = padre.TecnicoAsignado;
                datos.NoReasignaciones = padre.NoReasignaciones;
                datos.MotivoReasignacion = padre.MotivoReasignacion;
                datos.ApruebaReasignacion = padre.ApruebaReasignacion;
                datos.MotivoCambioEstatus = padre.MotivoCambioEstatus;
                datos.Diagnostico = padre.Diagnostico;
                datos.Categoria = Convert.ToInt32(vm.subticket.Categoria);
                datos.SubCategoria = Convert.ToInt32(vm.subticket.Subcategoria);
                datos.Centro = Convert.ToInt32(vm.subticket.Centro);
                datos.GrupoResolutor = vm.subticket.GrupoResolutor;
                datos.Prioridad = vm.subticket.Prioridad;
                datos.DescripcionIncidencia = vm.subticket.DescIncidencia;
                datos.NoReapertura = padre.NoReapertura;
                datos.Estatus = "Abierto";
                datos.EstatusTicket = 1;
                datos.FechaRegistro = DateTime.Now;
                con.tbl_TicketDetalle.Add(datos);
                con.SaveChanges();
            }

            else
            {
                using (var con = new ServiceDeskContext())
                {
                    datos.IdTicketPrincipal = vm.subticket.IdTicket;                    // New info
                    datos.EmpleadoID = padre.EmpleadoID;
                    datos.EmpleadoID = TenicoCreadorDeSubticket;                      //  (solution friday issue)
                    datos.TicketTercero = padre.TicketTercero;                          // info copiedd to children
                    datos.Extencion = padre.Extencion;                                  // info copiedd to children
                    datos.NombreTercero = padre.NombreTercero;                          // info copiedd to children
                    datos.Piso = padre.Piso;                                            // info copiedd to children
                    datos.EmailTercero = padre.EmailTercero;                            // info copiedd to children
                    datos.ExtensionTercero = padre.ExtensionTercero;                    // info copiedd to children
                    datos.Posicion = padre.Posicion;                                    // info copiedd to children
                    datos.NombreCompleto = padre.NombreCompleto;
                    datos.NombreCompleto = user.NombreTecnico.ToUpper();          // (solution friday issue)
                    //datos.NombreCompleto = "";                                       // (solution february issue)
                    datos.Area = padre.Area;                                            // info copiedd to children
                    datos.PersonasAddNotificar = padre.PersonasAddNotificar;            // info copiedd to children
                    datos.Correo = padre.Correo;                                        // info copiedd to children
                    datos.Comentarios = padre.Comentarios;                              // info copiedd to children
                    datos.TecnicoAsignado = padre.TecnicoAsignado;
                    datos.TecnicoAsignado = null;                                     //  (solution friday issue)
                    datos.NoReasignaciones = padre.NoReasignaciones;                    // info copiedd to children
                    datos.MotivoReasignacion = padre.MotivoReasignacion;                // info copiedd to children
                    datos.ApruebaReasignacion = padre.ApruebaReasignacion;              // info copiedd to children
                    datos.MotivoCambioEstatus = padre.MotivoCambioEstatus;              // info copiedd to children
                    datos.Diagnostico = padre.Diagnostico;                              // info copiedd to children

                    datos.NoReasignaciones = 0;
                    datos.MotivoReasignacion = "";
                    datos.ApruebaReasignacion = 0;
                    datos.MotivoCambioEstatus = "";
                    datos.Diagnostico = 0;

                    datos.Categoria = Convert.ToInt32(vm.subticket.Categoria);          // New info
                    datos.SubCategoria = Convert.ToInt32(vm.subticket.Subcategoria);    // New info
                    datos.Centro = Convert.ToInt32(vm.subticket.Centro);                // New info
                    datos.GrupoResolutor = vm.subticket.GrupoResolutor;                 // New info
                    datos.Prioridad = vm.subticket.Prioridad;                           // New info
                    datos.DescripcionIncidencia = vm.subticket.DescIncidencia;          // New info
                    datos.NoReapertura = padre.NoReapertura;                            // info copiedd to children
                    datos.Estatus = "Abierto";                                          // New info
                    datos.EstatusTicket = 1;                                            // New info
                    datos.FechaRegistro = DateTime.Now;                                 // New info
                    con.tbl_TicketDetalle.Add(datos);
                    con.SaveChanges();
                }
            }

            //Agregar el guardado en el historico
            _mng.SaveHistoricoSubticket(vm, datos.Id);


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
                    var NameCarga = "SUB_" + datos.Id + "_" + upload.FileName;
                    var uploadName = Path.GetFileName(upload.FileName);
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
                        _mng.SetArchivoSubticket(datos.Id, NameCarga);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return RedirectToAction("DetalleTicket", "Supervisor", new { IdTicket = datos.IdTicketPrincipal, folio = datos.Id, EmployeeId= vm.EmployeeIdBO });
                }
                else
                {
                    ModelState.AddModelError("File", "Formato no soportado");
                    return PartialView();

                }

            }


            return RedirectToAction("DetalleTicket", "Supervisor", new { IdTicket = datos.IdTicketPrincipal, folio = datos.Id, EmployeeId = vm.EmployeeIdBO });
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public ActionResult Gestiones(string User, string EmployeeId)
        {
            var empid = 0;
            try {
                empid = Int32.Parse(EmployeeId);
            } catch { }

            ViewBag.user2 = string.IsNullOrEmpty(User) ? "" : User;
            ViewBag.Empid = EmployeeId;
            ViewBag.Rol = RoldeUsuario(Int32.Parse(EmployeeId));
            ViewBag.user = EmployeeId;

            var gpr = _db.tbl_User.Where(t => t.EmpleadoID == empid).Select(t => t.GrupoResolutor).FirstOrDefault();

            var det = new DetalleGestiones();

            det.EmployeeIdBO = EmployeeId;

            ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
            ViewBag.Grupo = new SelectList(_db.catGrupoResolutor.Where(x => x.Activo), "Id", "Grupo");
            ViewBag.Experiencia = new SelectList(_db.catNivelExperiencia.Where(x => x.Activo), "Id", "Nivel");
            ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
            ViewBag.Prioridad = new SelectList(_mng.LstPrioridad(), "Value", "Text");
            ViewBag.Solicitud = new SelectList(_mng.LstSolicitud(), "Value", "Text");
            //
            ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
            ViewBag.HoraInicio = new SelectList(_mng.LstHoraInicio(), "Value", "Text");
            ViewBag.HoraFin = new SelectList(_mng.LstHoraFin(), "Value", "Text");
            ViewBag.NivelExpLst = new SelectList(_db.catNivelExperiencia.Where(x => x.Activo), "Nivel", "Nivel");
            //
            var lstNivel = _db.catNivelExperiencia.Where(a => a.Activo == true).ToList();
            var lstCat = _db.vwDetalleCategoria.Where(a => a.Activo == true).ToList();
            var lstSubCat = _db.vwDetalleSubcategorias.Where(a => a.Activo == true).ToList();
            var lstUser = new List<vwDetalleUsuario>();
            var lstDX = new List<vwDetalleDiagnostico>();
            //var lstDX = _db.vwDetalleDiagnostico.Where(a => a.Activo == true).ToList();
            if (empid == 19237) // Daniel Fuentes tiene permisos especiales
            {
                lstDX = _db.vwDetalleDiagnostico.Where(a => a.Activo == true).ToList();
                lstUser = _db.vwDetalleUsuario.Where(a => a.Activo == true).ToList();
            }
            else { 
                //lstUser : Solo los usuarios del mismo grupo resolutor
                lstUser = _db.vwDetalleUsuario.Where(a => a.Activo == true && a.GrupoResolutor == gpr).ToList();

                //lstDx : Solo los diagnosticos pertinentes al grupo resolutor del usuario
                int idGrupoRes = _db.catGrupoResolutor.Where(t => t.Grupo == gpr).Select(t => t.Id).FirstOrDefault();
                var categoriasDelGrupo = _db.cat_Categoria.Where(t => t.GrupoResolutor == idGrupoRes).Select(t => t.Id).ToArray();
                var diagsByGrp = _db.catDiagnosticos.Where(t => categoriasDelGrupo.Contains(t.IdCategoria)).Select(t => t.Id).ToArray();
                lstDX = _db.vwDetalleDiagnostico.Where(a => a.Activo == true && diagsByGrp.Contains(a.Id)).ToList();
            }
            //

            det.diagLst = lstDX;
            det.detCategoria = lstCat;
            det.detSubcat = lstSubCat;
            det.userLst = lstUser;
            det.NivExpe = lstNivel;

            return View(det);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult AgregarDiagnostico(int cat, int Subcat, string Diag)
        {
            var result = "";

            var vm = new catDiagnosticos();

            try
            {
                vm.IdCategoria = cat;
                vm.IdSubcategoria = Subcat;
                vm.Diagnostico = Diag;
                vm.Activo = true;
                _db.catDiagnosticos.Add(vm);
                _db.SaveChanges();
                result = "Correcto";

            }
            catch (Exception e)
            {

                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult GetEditarDX(int id, int cat, int Subcat, string Diag)
        {

            var result = "";
            try
            {
                //Valida que ya existe el nivel
                var info = _db.catDiagnosticos.Where(a => a.Id == id).FirstOrDefault();

                if (info != null)
                {
                    _db.catDiagnosticos.Attach(info);
                    info.IdCategoria = cat;
                    info.IdSubcategoria = Subcat;
                    info.Diagnostico = Diag;
                    _db.SaveChanges();
                    result = "Correcto";
                }

            }
            catch (Exception e)
            {
                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult GetDeleteDX(int IdDX)
        {

            var result = "";
            try
            {
                //Valida que ya existe el nivel
                var info = _db.catDiagnosticos.Where(a => a.Id == IdDX).FirstOrDefault();

                if (info != null)
                {
                    _db.catDiagnosticos.Attach(info);
                    info.Activo = false;
                    _db.SaveChanges();
                    result = "Correcto";
                }

            }
            catch (Exception e)
            {
                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //VISTA PARA EDITAR
        [HttpPost] public JsonResult EditarDiagnostico(int id)
        {
            try
            {
                var data = _db.vwDetalleDiagnostico.Where(a => a.Id == id).FirstOrDefault();

                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult AgregarCategoria(string cat, int grupo)
        {

            var result = "";

            var vm = new cat_Categoria();

            try
            {
                vm.Categoria = cat;
                vm.GrupoResolutor = grupo;
                vm.Activo = true;
                _db.cat_Categoria.Add(vm);
                _db.SaveChanges();
                result = "Correcto";

            }
            catch (Exception e)
            {

                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);



        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //VISTA PARA EDITAR
        [HttpPost] public JsonResult ShowEditCategoria(int id)
        {
            try
            {
                var data = _db.vwDetalleCategoria.Where(a => a.Id == id).FirstOrDefault();

                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult GetEditarCategoria(int id, string cat, int grupo)
        {

            var result = "";
            try
            {
                //Valida que ya existe el nivel
                var info = _db.cat_Categoria.Where(a => a.Id == id).FirstOrDefault();

                if (info != null)
                {
                    _db.cat_Categoria.Attach(info);
                    info.Categoria = cat;
                    info.GrupoResolutor = grupo;
                    _db.SaveChanges();
                    result = "Correcto";
                }

            }
            catch (Exception e)
            {
                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult GetDeleteCategoria(int id)
        {

            var result = "";
            try
            {
                //Valida que ya existe el nivel
                var info = _db.cat_Categoria.Where(a => a.Id == id).FirstOrDefault();

                if (info != null)
                {
                    _db.cat_Categoria.Attach(info);
                    info.Activo = false;
                    _db.SaveChanges();
                    result = "Correcto";
                }

            }
            catch (Exception e)
            {
                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult AgregarSubCategoria(string subcat, int cat, string tipo, string prioridad, string sla, int nivel, string periodo)
        {
            var result = "";

            try
            {
                //Valida que ya existe el nivel
                var vm = new cat_SubCategoria();

                vm.IDCategoria = cat;
                vm.SubCategoria = subcat;
                vm.Tipo = tipo;
                vm.Prioridad = prioridad;
                vm.SLA = sla;
                vm.NivelExperiencia = nivel;
                vm.Periodo = periodo;
                vm.Activo = true;
                _db.cat_SubCategoria.Add(vm);
                _db.SaveChanges();
                result = "Correcto";


                //Guardamos en Matriz                

                //var data = _db.cat_Categoria.Where(a => a.Id == cat).FirstOrDefault();
                //var grupo = _db.catGrupoResolutor.Where(a => a.Id == data.GrupoResolutor).FirstOrDefault();
                //var niv = _db.catNivelExperiencia.Where(a => a.Id == vm.NivelExperiencia).FirstOrDefault();
                //var diagnostico = _db.catDiagnosticos.Where(a => a.Id == vm.IDCategoria).FirstOrDefault();

                //var matriz = new cat_MatrizCategoria();

                //matriz.IDCategoria = data.Id;
                //matriz.IDSubCategoria = vm.Id;
                //matriz.Incidencia = vm.Tipo;
                //matriz.GrupoAtencion = grupo.Grupo;
                //matriz.NivelExpertiz = niv.Nivel;
                //matriz.Prioridad = vm.Prioridad;
                //matriz.SLAObjetivo = vm.SLA;
                //matriz.Garantia = vm.Periodo;
                //matriz.Diagnostico = diagnostico.Diagnostico;
                //matriz.Activo = true;
                //_db.cat_MatrizCategoria.Add(matriz);
                //_db.SaveChanges();

            }
            catch (Exception e)
            {
                result = "Error";
            }

            return Json(result, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //VISTA PARA EDITAR
        [HttpPost] public JsonResult ShowEditSubcategoria(int id)
        {
            try
            {
                var data = _db.vwDetalleSubcategorias.Where(a => a.Id == id).FirstOrDefault();

                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("Error:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult GetEditSubCategoria(int id, string subcat, int cat, string tipo, string prioridad, string sla, int nivel, string periodo)
        {

            var result = "";
            try
            {
                //Valida que ya existe el nivel
                var info = _db.cat_SubCategoria.Where(a => a.Id == id).FirstOrDefault();

                if (info != null)
                {
                    _db.cat_SubCategoria.Attach(info);
                    info.IDCategoria = cat;
                    info.SubCategoria = subcat;
                    info.Tipo = tipo;
                    info.Prioridad = prioridad;
                    info.SLA = sla;
                    info.NivelExperiencia = nivel;
                    info.Periodo = periodo;
                    _db.SaveChanges();
                    result = "Correcto";

                    //Guardamos en Matriz                

                    //var data = _db.cat_Categoria.Where(a => a.Id == cat).FirstOrDefault();
                    //var grupo = _db.catGrupoResolutor.Where(a => a.Id == data.GrupoResolutor).FirstOrDefault();
                    //var niv = _db.catNivelExperiencia.Where(a => a.Id == info.NivelExperiencia).FirstOrDefault();
                    //var diagnostico = _db.catDiagnosticos.Where(a => a.Id == info.IDCategoria).FirstOrDefault();

                    //var matriz = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == info.Id).FirstOrDefault();

                    //if (matriz !=null)
                    //{

                    //    matriz.NivelExpertiz = niv.Nivel;
                    //    matriz.Prioridad = info.Prioridad;
                    //    matriz.SLAObjetivo = info.SLA;
                    //    matriz.Garantia = info.Periodo;
                    //    matriz.Diagnostico = diagnostico.Diagnostico;
                    //    matriz.Activo = true;                        
                    //    _db.SaveChanges();
                    //}

               
                }

            }
            catch (Exception e)
            {
                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult GetDeleteSubcategoria(int id)
        {

            var result = "";
            try
            {
                //Valida que ya existe el nivel
                var info = _db.cat_SubCategoria.Where(a => a.Id == id).FirstOrDefault();

                if (info != null)
                {
                    _db.cat_SubCategoria.Attach(info);
                    info.Activo = false;
                    _db.SaveChanges();
                    result = "Correcto";
                }

            }
            catch (Exception e)
            {
                result = "Error";
            }


            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //ValidaReaperturas
        [HttpPost] public JsonResult ValidaReaperturas(int TicketId)
        {

            var result = "";

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();


            if (info.NoReapertura >= 2)
            {

                result = "Mayor";

            }

            return Json(result, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //Usuario rechaza el "Resuelto"
        [HttpPost] public ActionResult GetRechazaSolucion(HttpPostedFileBase upload, string ComentariosRechazo, int? DetalleId)
        {

            var EmployeeIdBo = 0;

            if (Session["EmpleadoNo"] != null)
            {

                EmployeeIdBo = Convert.ToInt32(Session["EmpleadoNo"]);

            }
            else
            {

                return Json("Login", "Home");
            }

            var datos = new tbl_TicketDetalle();

            //Buscar la información del ticket principal
            var padre = _db.tbl_TicketDetalle.Where(a => a.Id == DetalleId).FirstOrDefault();

            using (var con = new ServiceDeskContext())
            {
                _db.tbl_TicketDetalle.Attach(padre);
                //padre.Estatus = "Trabajando";
                //padre.EstatusTicket = 3;
                padre.Estatus = "En Espera";
                padre.EstatusTicket = 7;
                padre.ComentariosRechazoSolucion = ComentariosRechazo;
                _db.SaveChanges();

                //Agregar el guardado en el historico
                _mng.SaveHistoricoRechazaSolicitud((int)DetalleId);


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
                    var NameCarga = padre.Id + "_" + upload.FileName;
                    var uploadName = Path.GetFileName(upload.FileName);
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
                        _mng.SetArchivoRechazaSolicitud(padre.Id, NameCarga);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return RedirectToAction("Index", "DashBoard", new { EmployeeId = EmployeeIdBo });
                }
                else
                {
                    ModelState.AddModelError("File", "Formato no soportado");
                    return PartialView();

                }

            }


            return RedirectToAction("Index", "DashBoard", new { EmployeeId = EmployeeIdBo });

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //Usuario Aprueba el "Resuelto"
        [HttpPost] public JsonResult GetAceptaSolucion(int TicketId)
        {
            var res = "";
            //pasa en garantia
            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            if (info != null)
            {

                _db.tbl_TicketDetalle.Attach(info);
                info.Estatus = "En Garantía";
                info.EstatusTicket = 5;
                info.NoReapertura = info.NoReapertura;
                info.FechaRegistro = DateTime.Now;
                info.MotivoCambioEstatus = "Solicitud Aprobada";
                _db.SaveChanges();

                //Se guarda en Historico
                _mng.SaveHistoricoUser(info);

                //Agregar el contador del periodo en garantía

                res = "Correcto";

            }
            return Json(res, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public FileResult ExportArchivos(int IdDoc)
        {
            string evidence = "";

            //CONSULTA EL CATALOGO QUE TRAE LA RUTA CON LA UBICACIÓN DEL DOCUMENTO ANTES GUARDADO
            var inforuta = _rh.Database.SqlQuery<RutasCargaArchivosRh>("dbo.GET_INFO_RUTAS_UPLOAD").Where(a => a.Id == 6).FirstOrDefault();
            var ruta = inforuta.Ruta;
            string fname = "";
            fname = _doc.DownloadPath(ruta);
            fname = @"\\" + ruta + "\\";                                      // AppExt
            //fname = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/")); // Condor

            //CONSULTA EL NOMBRE DEL DOCUMENTO
            var datos = _db.tblDocumentos.Where(a => a.Id == IdDoc).FirstOrDefault();

            if (datos != null)
            {
                evidence = fname + datos.Nombre;
            }

            //PROCESO PARA CREAR LA DESCARGA
            string ph = Path.GetExtension(datos.Nombre);

            Byte[] bytes = System.IO.File.ReadAllBytes(evidence);

            //using (var fileConvert = new FileStream(Server.MapPath("~/") + "File" + ph, FileMode.Create))
            using (var fileConvert = new FileStream(Server.MapPath("~/") + "File" + ph, FileMode.Open, FileAccess.ReadWrite))
            {
                fileConvert.Write(bytes, 0, bytes.Length);
                fileConvert.Flush();
            }

            return File(bytes, System.Net.Mime.MediaTypeNames.Application.Octet, datos.Nombre);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //=======================================================CATALOGOS============================================================================
        [HttpPost] public JsonResult GetGrupoResolutor(int Id)
        {

            var subcat = _db.cat_Categoria.Where(a => a.GrupoResolutor == Id).Select(a => new { a.Id, a.Categoria }).ToList();

            return Json(subcat, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public JsonResult GetCategoria(int Id)
        {

            var subcat = _db.cat_SubCategoria.Where(a => a.IDCategoria == Id).Select(a => new { a.Id, a.SubCategoria }).ToList();

            return Json(subcat, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - -
        [HttpPost] public JsonResult GetMatriz(int Id)
        {

            var mat = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == Id).ToList();

            return Json(mat, JsonRequestBehavior.AllowGet);

        }
        [HttpPost] public JsonResult GetTecnico(int Id)
        {
            //String con el grupo resolutor del Id provisto en parametro
            var strRes = _db.catGrupoResolutor.Where(pointer => pointer.Id == Id).Select(pointer => pointer.Grupo).FirstOrDefault().ToString();
            var userRs = _db.tbl_User.Where(x => x.GrupoResolutor == strRes).Select(x => new { x.EmpleadoID, x.NombreTecnico }).ToList();

            return Json(userRs, JsonRequestBehavior.AllowGet);
        }
        //=======================================================CATALOGOS============================================================================




        public ActionResult GridTickets(int EmployeeID, int? pag)
        {
            ViewBag.user = EmployeeID;
            ViewBag.Rol = RoldeUsuario(EmployeeID);
            if (pag.HasValue) ViewBag.Pagina = pag;
            var t = new TareasProgramadasVM();
            var tareas = _db.vw_TareasProgramadas.Where(pointer => pointer.SupervisorID == EmployeeID).OrderBy(pointer => pointer.Id).ToList();
            t.tareas = tareas;
            return View(t);
        }
        public ActionResult ProgramarTarea(string Usuario, string id, string EmployeeId)
        {
            // copiar y pegar en cualquier actionresult que requiere mandar usuario por un tubo si se intenta pasar de listo
            var userSession = Session["EmpleadoNo"].ToString(); if (userSession != EmployeeId) { return RedirectToAction("Error", "Document"); }

            ViewBag.user = EmployeeId;
            if (Session["EmpleadoNo"] != null) { Usuario = Session["EmpleadoNo"].ToString(); }
            else { return RedirectToAction("Login", "Home"); }
            ViewBag.Id = string.IsNullOrEmpty(id) ? "" : id;

            var vm = new TareasProgramadasEditVM();
            var NumeroPenta = Convert.ToInt32(Usuario);

            //var InfoUser = _rh.vw_DetalleEmpleado.Where(a => a.NumeroPenta == NumeroPenta).FirstOrDefault();
            //vm.nombreCompleto = InfoUser.NombreCompleto;
            //vm.id = InfoUser.NumeroPenta;
            //vm.puesto = InfoUser.Puesto;
            //vm.area = InfoUser.Area;
            //vm.correo = InfoUser.Email;
            var InfoUser = _db.vw_INFO_USER_EMPLEADOS.Where(t => t.NumeroPenta == NumeroPenta).FirstOrDefault();            
            vm.id = InfoUser.NumeroPenta;
            vm.nombreCompleto = InfoUser.NombreCompleto;
            vm.area = InfoUser.Area;
            vm.correo = InfoUser.Email;
            vm.puesto = InfoUser.Puesto;

            List<SelectListItem> Prioridad = new List<SelectListItem>();
            Prioridad.Add(new SelectListItem() { Text = "Alta", Value = "Alta" });
            Prioridad.Add(new SelectListItem() { Text = "Media", Value = "Media" });
            Prioridad.Add(new SelectListItem() { Text = "Baja", Value = "Baja" });
            List<SelectListItem> Estatus = new List<SelectListItem>();
            Estatus.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            Estatus.Add(new SelectListItem() { Text = "Asignado", Value = "Asignado" });
            Estatus.Add(new SelectListItem() { Text = "Asignación Pendiente", Value = "Asignación Pendiente" });
            List<SelectListItem> Periodo = new List<SelectListItem>();
            Periodo.Add(new SelectListItem() { Text = "Semanal", Value = "Semanal" });
            Periodo.Add(new SelectListItem() { Text = "Mensual", Value = "Mensual" });
            List<SelectListItem> DiaCardinal = new List<SelectListItem>();
            DiaCardinal.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            DiaCardinal.Add(new SelectListItem() { Text = "Primer", Value = "1" });
            DiaCardinal.Add(new SelectListItem() { Text = "Segundo", Value = "2" });
            DiaCardinal.Add(new SelectListItem() { Text = "Tercer", Value = "3" });
            DiaCardinal.Add(new SelectListItem() { Text = "Cuarto", Value = "4" });
            DiaCardinal.Add(new SelectListItem() { Text = "Quinto", Value = "5" });
            List<SelectListItem> DiadelMes = new List<SelectListItem>();
            DiadelMes.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            for (int d = 1; d <= 31; d++) { DiadelMes.Add(new SelectListItem() { Text = "Día " + d.ToString(), Value = d.ToString() }); }
            List<SelectListItem> DiadelaSemana = new List<SelectListItem>();
            DiadelaSemana.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Lunes del mes", Value = "1" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Martes del mes", Value = "2" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Miercoles del mes", Value = "3" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Jueves del mes", Value = "4" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Viernes del mes", Value = "5" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Sabado del mes", Value = "6" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Domingo del mes", Value = "7" });

            ViewBag.Prioridad = new SelectList(Prioridad, "Value", "Text");
            ViewBag.Estatus = new SelectList(Estatus, "Value", "Text");
            ViewBag.Periodo = new SelectList(Periodo, "Value", "Text");

            ViewBag.DiaCardinal = new SelectList(DiaCardinal, "Value", "Text");
            ViewBag.DiadelMes = new SelectList(DiadelMes, "Value", "Text");
            ViewBag.DiadelaSemana = new SelectList(DiadelaSemana, "Value", "Text");

            //Listas

            List<SelectListItem> Tecnicos = new List<SelectListItem>();
            Tecnicos.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            //var listOfItems = new SelectList(_db.tbl_User, "EmpleadoID", "NombreTecnico");
            //foreach (var item in listOfItems) { Tecnicos.Add(item); }

            ViewBag.Tecnico = new SelectList(Tecnicos, "Value", "Text"); //new SelectList(string.Empty, "Value", "Text");
            ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
            ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
            ViewBag.GrupoResolutor = new SelectList(_db.catGrupoResolutor.Where(x => x.Activo), "Id", "Grupo");
            ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
            ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");

            return View(vm);
        }
        public ActionResult EditarTarea(string Usuario, string id, string EmployeeId, int TareaID)
        {
            // copiar y pegar en cualquier actionresult que requiere mandar usuario por un tubo si se intenta pasar de listo
            var userSession = Session["EmpleadoNo"].ToString(); if (userSession != EmployeeId) { return RedirectToAction("Error", "Document"); }

            ViewBag.user = EmployeeId;
            if (Session["EmpleadoNo"] != null) { Usuario = Session["EmpleadoNo"].ToString(); }
            else { return RedirectToAction("Login", "Home"); }
            ViewBag.Id = string.IsNullOrEmpty(id) ? "" : id;

            var vm = new TareasProgramadasEditVM();
            var NumeroPenta = Convert.ToInt32(Usuario);

            //var InfoUser = _rh.vw_DetalleEmpleado.Where(a => a.NumeroPenta == NumeroPenta).FirstOrDefault();
            //vm.nombreCompleto = InfoUser.NombreCompleto;
            //vm.id = InfoUser.NumeroPenta;
            //vm.puesto = InfoUser.Puesto;
            //vm.area = InfoUser.Area;
            //vm.correo = InfoUser.Email;
            var InfoUser = _db.vw_INFO_USER_EMPLEADOS.Where(t => t.NumeroPenta == NumeroPenta).FirstOrDefault();
            vm.id = InfoUser.NumeroPenta;
            vm.nombreCompleto = InfoUser.NombreCompleto;
            vm.area = InfoUser.Area;
            vm.correo = InfoUser.Email;
            vm.puesto = InfoUser.Puesto;

            vm.tareas = _db.tblTareasProgramadas.Where(pointer => pointer.Id == TareaID).SingleOrDefault();

            List<SelectListItem> Prioridad = new List<SelectListItem>();
            Prioridad.Add(new SelectListItem() { Text = "Alta", Value = "Alta" });
            Prioridad.Add(new SelectListItem() { Text = "Media", Value = "Media" });
            Prioridad.Add(new SelectListItem() { Text = "Baja", Value = "Baja" });
            List<SelectListItem> Estatus = new List<SelectListItem>();
            Estatus.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            Estatus.Add(new SelectListItem() { Text = "Trabajando", Value = "Trabajando" });
            Estatus.Add(new SelectListItem() { Text = "En Espera", Value = "En Espera" });
            Estatus.Add(new SelectListItem() { Text = "Asignado", Value = "Asignado" });
            Estatus.Add(new SelectListItem() { Text = "Asignación Pendiente", Value = "Asignación Pendiente" });
            List<SelectListItem> Periodo = new List<SelectListItem>();
            Periodo.Add(new SelectListItem() { Text = "Semanal", Value = "Semanal" });
            Periodo.Add(new SelectListItem() { Text = "Mensual", Value = "Mensual" });
            List<SelectListItem> DiaCardinal = new List<SelectListItem>();
            DiaCardinal.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            DiaCardinal.Add(new SelectListItem() { Text = "Primer", Value = "1" });
            DiaCardinal.Add(new SelectListItem() { Text = "Segundo", Value = "2" });
            DiaCardinal.Add(new SelectListItem() { Text = "Tercer", Value = "3" });
            DiaCardinal.Add(new SelectListItem() { Text = "Cuarto", Value = "4" });
            DiaCardinal.Add(new SelectListItem() { Text = "Quinto", Value = "5" });
            List<SelectListItem> DiadelMes = new List<SelectListItem>();
            DiadelMes.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            for (int d = 1; d <= 31; d++) { DiadelMes.Add(new SelectListItem() { Text = "Día " + d.ToString(), Value = d.ToString() }); }
            List<SelectListItem> DiadelaSemana = new List<SelectListItem>();
            DiadelaSemana.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Lunes del mes", Value = "1" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Martes del mes", Value = "2" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Miercoles del mes", Value = "3" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Jueves del mes", Value = "4" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Viernes del mes", Value = "5" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Sabado del mes", Value = "6" });
            DiadelaSemana.Add(new SelectListItem() { Text = "Domingo del mes", Value = "7" });

            ViewBag.Prioridad = new SelectList(Prioridad, "Value", "Text");
            ViewBag.Estatus = new SelectList(Estatus, "Value", "Text");

            ViewBag.DiaCardinal = new SelectList(DiaCardinal, "Value", "Text");
            ViewBag.DiadelMes = new SelectList(DiadelMes, "Value", "Text");
            ViewBag.DiadelaSemana = new SelectList(DiadelaSemana, "Value", "Text");
            ViewBag.Periodo = new SelectList(Periodo, "Value", "Text");

            //Listas
            ViewBag.TecnicoIDmanual = vm.tareas.TecnicoID;
            ViewBag.FechaInicial = vm.tareas.FechaInicial.ToString("yyyy-MM-dd");
            ViewBag.FechaFinal = vm.tareas.FechaFinal.ToString("yyyy-MM-dd");
            ViewBag.Hora = vm.tareas.Hora.ToString("HH:mm");
            if (vm.tareas.GrupoResolutorID != 0)
            {
                var strRes = _db.catGrupoResolutor.Where(pointer => pointer.Id == vm.tareas.GrupoResolutorID).Select(pointer => pointer.Grupo).FirstOrDefault().ToString();
                ViewBag.Tecnico = new SelectList(_db.tbl_User.Where(pointer => pointer.GrupoResolutor == strRes), "EmpleadoID", "NombreTecnico");
            }
            else
            {
                List<SelectListItem> tecEmpty = new List<SelectListItem>();
                tecEmpty.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
                ViewBag.Tecnico = new SelectList(tecEmpty, "Value", "Text");
            }
            ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
            ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
            ViewBag.GrupoResolutor = new SelectList(_db.catGrupoResolutor.Where(x => x.Activo), "Id", "Grupo");
            ViewBag.SubCategoria = new SelectList(_db.cat_SubCategoria.Where(x => x.IDCategoria == vm.tareas.CategoriaID), "Id", "SubCategoria");

            ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");

            var usuario = _db.tbl_User.Where(a => a.EmpleadoID == vm.tareas.SupervisorID).FirstOrDefault();
            vm.puesto = usuario.Rol;
            vm.nombreCompleto = usuario.NombreTecnico;
            vm.area = usuario.GrupoResolutor;
            vm.correo = usuario.Correo;
            //Archivos adjuntos
            var dtoDw = _db.tblDocumentos.Where(a => a.IdTicket == TareaID && a.Extension == "Tarea").OrderByDescending(a => a.FechaRegisto).ToList();
            vm.Docs = dtoDw;


            return View(vm);
        }
        public ActionResult DetalleTarea(int TareaId, string EmployeeId)
        {
            // copiar y pegar en cualquier actionresult que requiere mandar usuario por un tubo si se intenta pasar de listo
            var userSession = Session["EmpleadoNo"].ToString(); if (userSession != EmployeeId) { return RedirectToAction("Error", "Document"); }

            ViewBag.idChild = 0;
            ViewBag.user = EmployeeId;

            //ENVIA OBJETO CON HISTORICO Y DETALLE DE TICKET
            var detalle = new TareasProgramadasHisVM();
            detalle.tareasHis = _db.vw_HistoricoTareas.Where(pointer => pointer.IdTarea == TareaId).OrderBy(m => m.FechaRegistro).ToList();
            detalle.tarea = _db.vw_TareasProgramadas.Where(pointer => pointer.Id == TareaId).FirstOrDefault();
            detalle.tblTarea = _db.tblTareasProgramadas.Where(pointer => pointer.Id == TareaId).FirstOrDefault();
            foreach (var t in detalle.tareasHis) { foreach (char c in t.Evento) t.Evento = t.Evento.Replace(" ", String.Empty); }

            //Archivos adjuntos
            var dtoDw = _db.tblDocumentos.Where(a => a.IdTicket == TareaId && a.Extension.Contains("Tarea")).OrderByDescending(a => a.FechaRegisto).ToList();
            detalle.Docs = dtoDw;

            //Tiempo Actual y Tiempo Total
            detalle.tiempoEspera = esperaTotal(detalle.tareasHis);
            detalle.tiempoTotal = tiempoTotal(detalle.tareasHis);

            //Datos usuario 
            var NumeroPenta = Convert.ToInt32(EmployeeId);
            //var InfoUser = _rh.vw_DetalleEmpleado.Where(a => a.NumeroPenta == NumeroPenta).FirstOrDefault();
            //detalle.id = InfoUser.NumeroPenta;
            //detalle.nombreCompleto = InfoUser.NombreCompleto;
            //detalle.puesto = InfoUser.Puesto;
            //detalle.area = InfoUser.Area;
            //detalle.correo = InfoUser.Email;
            var InfoUser = _db.vw_INFO_USER_EMPLEADOS.Where(t => t.NumeroPenta == NumeroPenta).FirstOrDefault();
            detalle.id = InfoUser.NumeroPenta;
            detalle.nombreCompleto = InfoUser.NombreCompleto;
            detalle.puesto = InfoUser.Puesto;
            detalle.area = InfoUser.Area;
            detalle.correo = InfoUser.Email;

            // id tarea
            var IDtarea = TareaId;
            var Taskid = detalle.tarea;
            detalle.taskid = Taskid.Id;

            //Llenar historico
            detalle.tareasHis = _db.vw_HistoricoTareas.Where(pointer => pointer.IdTarea == TareaId).OrderBy(m => m.FechaRegistro).ToList();

            return View(detalle);
        }

        //------------------------- Tareas programadas Métodos
        public FileResult ExportArchivosTarea(int IdDoc)
        {
            string evidence = "";

            ////CONSULTA EL CATALOGO QUE TRAE LA RUTA CON LA UBICACIÓN DEL DOCUMENTO ANTES GUARDADO
            var inforuta = _rh.Database.SqlQuery<RutasCargaArchivosRh>("dbo.GET_INFO_RUTAS_UPLOAD").Where(a => a.Id == 6).FirstOrDefault();
            var ruta = inforuta.Ruta;
            string ServerSavePath = "";
            ServerSavePath = _doc.DownloadPath(ruta);
            ServerSavePath = @"\\" + ruta + "\\";                              // AppExt
            //ServerSavePath = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/"));  // Condor 


            //CONSULTA EL NOMBRE DEL DOCUMENTO
            var DocFileName = _db.tblDocumentos.Where(a => a.Id == IdDoc).FirstOrDefault();

            if (DocFileName != null)
            {
                evidence = ServerSavePath + DocFileName.Nombre;
                //var ServerSavePath = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/") + NameCarga);
            }

            //PROCESO PARA CREAR LA DESCARGA
            string ph = Path.GetExtension(DocFileName.Nombre);

            Byte[] bytes = System.IO.File.ReadAllBytes(evidence);

            //using (var fileConvert = new FileStream(Server.MapPath("~/") + "File" + ph, FileMode.Create))
            using (var fileConvert = new FileStream(Server.MapPath("~/") + "File" + ph, FileMode.Open, FileAccess.ReadWrite))
            {
                fileConvert.Write(bytes, 0, bytes.Length);
                fileConvert.Flush();
            }

            return File(bytes, System.Net.Mime.MediaTypeNames.Application.Octet, DocFileName.Nombre);
        }
        public ActionResult SetTareaProgramada(TareasProgramadasEditVM vm, HttpPostedFileBase[] upload, string EmployeeId)
        {
            var tbl = vm.tareas;
            var maxId = 0;
            try { maxId = _db.tblTareasProgramadas.Max(m => m.Id); } 
            catch { }
            var newId = maxId + 1;
            vm.tareas.Id = newId; // necesario para mostrar notificación correctamente
            vm.tareas.SupervisorID = Int32.Parse(EmployeeId);
            vm.tareas.Activado_2 = false;
            var data = _mng.SetTareaProgramada(tbl);

            var Usuario = "";
            if (Session["EmpleadoNo"] != null) { Usuario = Session["EmpleadoNo"].ToString(); }
            else { return RedirectToAction("Login", "Home"); }

            if (ModelState.IsValid)
            {
                foreach (HttpPostedFileBase file in upload)
                {
                    if (file != null)
                    {
                        var extension = file.FileName.ToUpper();
                        if (extension.EndsWith(".PDF") || extension.EndsWith(".DOC") || extension.EndsWith(".DOCX") || extension.EndsWith(".DOT")
                            || extension.EndsWith(".XLS") || extension.EndsWith(".XLSX") || extension.EndsWith(".XLSM") || extension.EndsWith(".XLT")
                            || extension.EndsWith(".PPT") || extension.EndsWith(".PPTX") || extension.EndsWith(".PPS") || extension.EndsWith(".JPG")
                            || extension.EndsWith(".PNG") || extension.EndsWith(".JPEG") || extension.EndsWith(".CSV"))
                        {
                            var InputFileName = Path.GetFileName(file.FileName);
                            var NameCarga = data + "_" + file.FileName;
                            var path = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/"), NameCarga);
                            //Save file to server folder  
                            file.SaveAs(path);

                            //assigning file uploaded status to ViewBag for showing message to user.  
                            //ViewBag.UploadStatus = upload.Count().ToString() + " files uploaded successfully.";

                            //SE GUARDA EN LA RUTA QUE SERA COMPARTIDA PARA AMBAS DIRECCIONES 35 Y 36
                            var fname = @"\\10.200.154.36\uFiles\ServiceDeskV2\\" + NameCarga;
                            System.IO.File.Copy(path, fname, true);
                            System.IO.File.Delete(path);
                            _doc.Upload(NameCarga, path, true);
                            _mng.SetArchivoTarea(data, NameCarga);
                        }
                    }
                }
            }
            return RedirectToAction("GridTickets", "Supervisor", new { EmployeeId = EmployeeId });
        }
        public ActionResult DesactivarTarea(string EmployeeId, int TareaId, int? pag)
        {
            var tarea = new tbl_TareasProgramadas();
            tarea = _db.tblTareasProgramadas.Where(x => x.Id == TareaId).FirstOrDefault();
            _mng.Activar_Desactivar_TareaProgramada(tarea);
            if (pag.HasValue) ViewBag.Pagina = pag;
            return RedirectToAction("GridTickets", "Supervisor", new { EmployeeId = EmployeeId, pag = ViewBag.Pagina });
        }
        public ActionResult EliminarTarea(int TareaId, int EmployeeId, int? pag)
        {
            var tarea = _db.tblTareasProgramadas.Where(x => x.Id == TareaId).FirstOrDefault();
            if (tarea != null)
            {
                _db.tblTareasProgramadas.Remove(tarea);
                _db.SaveChanges();
            }
            if (pag.HasValue) ViewBag.Pagina = pag;
            return RedirectToAction("GridTickets", "Supervisor", new { EmployeeId = EmployeeId, pag = ViewBag.Pagina });
        }
        public ActionResult EditTareaProgramada(TareasProgramadasEditVM vm, HttpPostedFileBase[] upload, string EmployeeId)
        {
            //var regOriginal = _db.tblTareasProgramadas.Where(c => c.Id == vm.tareas.Id).FirstOrDefault();
            var tbl = vm.tareas;
            vm.tareas.SupervisorID = Int32.Parse(EmployeeId);
            var data = _mng.EditTareaProgramada(tbl);

            var Usuario = "";
            if (Session["EmpleadoNo"] != null) { Usuario = Session["EmpleadoNo"].ToString(); }
            else { return RedirectToAction("Login", "Home"); }

            if (ModelState.IsValid)
            {
                foreach (HttpPostedFileBase file in upload)
                {
                    if (file != null)
                    {
                        var extension = file.FileName.ToUpper();
                        if (extension.EndsWith(".PDF") || extension.EndsWith(".DOC") || extension.EndsWith(".DOCX") || extension.EndsWith(".DOT")
                            || extension.EndsWith(".XLS") || extension.EndsWith(".XLSX") || extension.EndsWith(".XLSM") || extension.EndsWith(".XLT")
                            || extension.EndsWith(".PPT") || extension.EndsWith(".PPTX") || extension.EndsWith(".PPS") || extension.EndsWith(".JPG")
                            || extension.EndsWith(".PNG") || extension.EndsWith(".JPEG") || extension.EndsWith(".CSV"))
                        {
                            var InputFileName = Path.GetFileName(file.FileName);
                            var NameCarga = data + "_" + file.FileName;
                            var path = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/"), NameCarga);
                            //Save file to server folder  
                            file.SaveAs(path);

                            //assigning file uploaded status to ViewBag for showing message to user.  
                            //ViewBag.UploadStatus = upload.Count().ToString() + " files uploaded successfully.";

                            //SE GUARDA EN LA RUTA QUE SERA COMPARTIDA PARA AMBAS DIRECCIONES 35 Y 36
                            var fname = @"\\10.200.154.36\uFiles\ServiceDeskV2\\" + NameCarga;
                            System.IO.File.Copy(path, fname, true);
                            System.IO.File.Delete(path);
                            _doc.Upload(NameCarga, path, true);
                            _mng.SetArchivoTarea(data, NameCarga);
                        }
                    }
                }
            }
            return RedirectToAction("GridTickets", "Supervisor", new { EmployeeId = EmployeeId });
        } //Método de edición de tareas
        public ActionResult RechazoSolucionDocumentUpload(HttpPostedFileBase[] upload, string EmployeeId, int ticketId) // tareas
        {
            if (ModelState.IsValid)
            {
                foreach (HttpPostedFileBase file in upload)
                {
                    if (file != null)
                    {
                        var extension = file.FileName.ToUpper();
                        if (extension.EndsWith(".PDF") || extension.EndsWith(".DOC") || extension.EndsWith(".DOCX") || extension.EndsWith(".DOT")
                            || extension.EndsWith(".XLS") || extension.EndsWith(".XLSX") || extension.EndsWith(".XLSM") || extension.EndsWith(".XLT")
                            || extension.EndsWith(".PPT") || extension.EndsWith(".PPTX") || extension.EndsWith(".PPS") || extension.EndsWith(".JPG")
                            || extension.EndsWith(".PNG") || extension.EndsWith(".JPEG") || extension.EndsWith(".CSV"))
                        {
                            var InputFileName = Path.GetFileName(file.FileName);
                            var NameCarga = ticketId + "_RechazoEvidencia_" + file.FileName;
                            var path = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/"), NameCarga);
                            //Save file to server folder  
                            file.SaveAs(path);

                            //assigning file uploaded status to ViewBag for showing message to user.  
                            //ViewBag.UploadStatus = upload.Count().ToString() + " files uploaded successfully.";

                            //SE GUARDA EN LA RUTA QUE SERA COMPARTIDA PARA AMBAS DIRECCIONES 35 Y 36
                            var fname = @"\\10.200.154.36\uFiles\ServiceDeskV2\\" + NameCarga;
                            System.IO.File.Copy(path, fname, true);
                            System.IO.File.Delete(path);
                            _doc.Upload(NameCarga, path, true);
                            _mng.SetArchivoTareaRechazo(ticketId, NameCarga);
                        }
                    }
                }
            }          

            return RedirectToAction("GridTickets", "Supervisor", new { EmployeeId = EmployeeId });
        } // Reechazar solución a Tarea
        public string tiempoTotal(List<vw_HistoricoTareas> histlist)
        {
            // devuelve suma de tiempos activos, suma de tiempos en que tarea estuvo como asignada
            string tiempoTotal = "";
            TimeSpan dTiemptotal = default;
            DateTime temporal = default;
            DateTime dateF = default;
            DateTime date1 = default;
            // Inicializar fechas, f1 = ultima fecha de reasignación

            // date1 = fecha de inicio de conteo, creación o reasignación
            var f = histlist.Where(m => m.Evento.Contains("Reasigna")).OrderByDescending(m => m.FechaRegistro).FirstOrDefault();
            if (f == null)
            {
                f = histlist.Where(m => m.Evento.Contains("Crea")).OrderBy(m => m.FechaRegistro).FirstOrDefault();
                date1 = f.FechaRegistro; // no hay reasignaciones, f1 = fecha de creación                
            }
            else { date1 = f.FechaRegistro; }
            temporal = date1;
            var flag = false;
            //-------------------------------------------- f1 conseguido

            // Repetición de busqueda de tiempos activos
            do
            {
                // Buscar primer "Resuelto" después de f1
                f = histlist.Where(m => m.Evento.Contains("Resuelto") && m.FechaRegistro > date1).OrderBy(m => m.FechaRegistro).FirstOrDefault();
                if (f == null) { flag = true; break; } // no fue resuelto...
                else
                {
                    dateF = f.FechaRegistro;
                    dTiemptotal += dateF - date1;

                    // Buscar si hay Rechazo después de haber sido resuelto
                    f = histlist.Where(m => m.Evento.Contains("Rechazado") && m.FechaRegistro > dateF).OrderBy(m => m.FechaRegistro).FirstOrDefault();
                    if (f == null) { flag = true; break; }
                    else
                    {
                        date1 = f.FechaRegistro;
                    }
                }
            } while (!flag);

            // Buscar si hay cancelaciones después de haber sido resuelto
            f = histlist.Where(m => m.Evento.Contains("Cancelado") && m.FechaRegistro > dateF).OrderBy(m => m.FechaRegistro).FirstOrDefault();
            if (f == null)
            { // sin cancelaciones
                // Buscar si si Cerró la tarea después de haber sido resuelta
                f = histlist.Where(m => m.Evento.Contains("Cerrado") && m.FechaRegistro > dateF).OrderBy(m => m.FechaRegistro).FirstOrDefault();
                if (f == null)
                {
                    dTiemptotal += DateTime.Now - date1;
                } // tarea no se cerró ni canceló después de haber sido resuelta, sigue abierta
                else { dTiemptotal += dateF - date1; dateF = f.FechaRegistro; } // tarea se cerró después de haber sido resuelta
            }
            else // cancelaciones encontradas 
            { dateF = f.FechaRegistro; }

            //Dar Formato de horas al string
            string fminutos = (dTiemptotal.Minutes < 9) ? "0" + dTiemptotal.Minutes : dTiemptotal.Minutes.ToString();
            string fhoras = (dTiemptotal.Hours < 9) ? "0" + dTiemptotal.Hours : dTiemptotal.Hours.ToString();
            tiempoTotal = dTiemptotal.Days + " dias " + fhoras + ":" + fminutos;

            return tiempoTotal;
        }
        public string esperaTotal(List<vw_HistoricoTareas> histlist)
        {
            string esperaTotal = "";

            // variables iniciales
            TimeSpan dEsperaTotal = default; //var usedDates = new List<DateTime>();
            var firstTime = true; var endingFlag = false;
            DateTime date1, dateF;

            // Repetición de busqueda de Esperas y Trabajandos...
            do
            {
                // Siguiente/Primer en Espera
                var f = histlist.Where(m => m.Evento.Contains("Espera")).OrderBy(m => m.FechaRegistro).FirstOrDefault();
                if (f == null)
                {
                    if (firstTime) { return esperaTotal = "No en espera"; } // Tarea nunca estuvo en espera
                    else { endingFlag = true; break; } // Final de repetición
                }
                else
                {
                    date1 = f.FechaRegistro;
                    histlist.Remove(f); //usedDates.Add(date1);

                    // Buscar primer Trabajando despues de la espera
                    f = histlist.Where(m => m.Evento.Contains("Trabajando")).OrderBy(m => m.FechaRegistro).FirstOrDefault();

                    if (f == null) { dEsperaTotal += DateTime.Today - date1; }                           // Trabajando no encontrado, Tarea en Espera rn
                    else { dateF = f.FechaRegistro; dEsperaTotal += dateF - date1; histlist.Remove(f); } // Trabajando encontrado, añadir tiempo a EsperaTotal
                    firstTime = false;
                }
            } while (!endingFlag);

            //Dar Formato de horas al string
            string fminutos = (dEsperaTotal.Minutes < 9) ? "0" + dEsperaTotal.Minutes : dEsperaTotal.Minutes.ToString();
            string fhoras = (dEsperaTotal.Hours < 9) ? "0" + dEsperaTotal.Hours : dEsperaTotal.Hours.ToString();
            esperaTotal = dEsperaTotal.Days + " dias " + fhoras + ":" + fminutos;

            return esperaTotal;
        }
        [HttpPost]
        public JsonResult ApruebaSolucion(int TareaId)
        {
            var result = "";
            var NoReasignacion = 1;

            var enfo = _db.tblTareasProgramadas.Where(a => a.Id == TareaId).FirstOrDefault();

            _db.tblTareasProgramadas.Attach(enfo);
            _mng.notif("Solución aprobada: " + TareaId, "La solución del ticket con ID " + TareaId + " ha sido aprobada.  El ticket se ha cerrado", enfo.TecnicoID);
            enfo.Estatus = "Cerrado";
            _db.SaveChanges();

            //Guardar Historico
            _mng.SaveHistorico(enfo, "Aprobación");

            result = "Correcto";
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public JsonResult RechazaSolucion(int TareaId, string motivo)
        {
            var result = "";
            var enfo = _db.tblTareasProgramadas.Where(a => a.Id == TareaId).FirstOrDefault();
            _mng.notif("Solución rechazada: " + TareaId, "La solución del ticket con ID " + TareaId + " ha sido rechazada.  Consulta los detalles en el historial del ticket.", enfo.TecnicoID);
            var historico = _db.his_TareasProgramadas.Where(m => m.IdTarea == TareaId && m.Evento.Contains("Rechazado")).ToList();
            if (historico.Count > 1)
            {
                enfo.Estatus = "Cancelado";
                _mng.SaveHistorico(enfo, "Cancelado", motivo);
            }
            else
            {
                enfo.Estatus = "Trabajando";
                _mng.SaveHistorico(enfo, "Rechazado", motivo);
            }
            _db.SaveChanges();

            result = "Correcto";
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //----------------------------------------
        public string RoldeUsuario(int EmployeeID) //String que obtiene el Rol del usuario dado su ID 
        {
            string rol = "";

            try {                
                var numemp = _adm.tblUser.Where(a => a.EmpleadoId == EmployeeID).FirstOrDefault();
                if (numemp != null) { 
                    var rols = Roles.GetRolesForUser(numemp.UserName);

                    var ptoSupRol = _mng.GetRolByPuesto(rols);

                    if (ptoSupRol.Contains("Supervisor")) { ViewBag.RoleNameUser = "Supervisor"; }
                    else if (ptoSupRol.Contains("Tecnico")) { ViewBag.RoleNameUser = "Tecnico"; }

                    if (ptoSupRol.Contains("Solicitante")) { rol = "Solicitante"; }
                    else if (ptoSupRol.Contains("Supervisor")) { rol = "Supervisor"; }
                    else if (ptoSupRol.Contains("Tecnico")) { rol = "Tecnico"; }        //-* usuario resolutor
                    else if (ptoSupRol.Contains("ServiceDesk")) { rol = "ServiceDesk"; }    //-* usuario resolutor
                    else if (ptoSupRol.Contains("Directivo")) { rol = "Directivo"; }
                    else { }
                }
            }
            catch { }

            return rol;
        }





    }
}
