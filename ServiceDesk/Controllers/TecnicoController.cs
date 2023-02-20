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

namespace ServiceDesk.Controllers
{
    public class TecnicoController : Controller
    {
        private readonly RHAdminContext _rh = new RHAdminContext();
        private readonly ServiceDeskManager _mng = new ServiceDeskManager();
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private readonly SlaManager _sla = new SlaManager();
        private readonly NotificacionesManager _noti = new NotificacionesManager();
        private readonly SupervisorController _spr = new SupervisorController();
        private readonly DocumentController _doc = new DocumentController();
        //============================================================================================================================================
        public ActionResult DetalleTicket(int? IdTicket, string folio, string EmployeeId, string Respuesta)
        {
            // copiar y pegar en cualquier actionresult que requiere mandar usuario por un tubo si se intenta pasar de listo
            var userSession = Session["EmpleadoNo"].ToString(); if (userSession != EmployeeId) { return RedirectToAction("Error", "Document"); }

            ViewBag.Id = string.IsNullOrEmpty(folio) ? "" : folio;

            if (Respuesta == "Error") { ViewBag.Mensaje = "SI"; }


            if (IdTicket != null)
            {

                //ENVIA OBJETO CON HISTORICO Y DETALLE DE TICKET
                var detalle = new DetalleSelectedTicketVm();

                if (IdTicket != null)
                {

                    var hisCCount = _db.his_Ticket.Where(t => t.IdTicket == IdTicket && t.Motivo.Contains("Ticket pasó a Control de Cambios")).Count();
                    if (hisCCount > 0)
                    {
                        try { ViewBag.CC = _db.tbl_CC_Dashboard.Where(t => t.Ticket == IdTicket).FirstOrDefault().id; } catch { }
                    }

                    //Asigna el valor del user logueado
                    if (EmployeeId != null) {
                        var idEmp = Int32.Parse(EmployeeId);
                        var MisDatos = _db.tbl_User.Where(t => t.EmpleadoID == idEmp).FirstOrDefault();
                        string Centro = _db.cat_Centro.Where(t => t.Id == MisDatos.Centro).Select(t => t.Centro).FirstOrDefault(); 
                        detalle.EmployeeIdBO = EmployeeId;
                        detalle.NombreCompleto = MisDatos.NombreTecnico;
                        detalle.Correo = MisDatos.Correo;
                        detalle.Area = Centro;
                    }


                    var ticket = _db.tbl_TicketDetalle.Where(a => a.Id == IdTicket).FirstOrDefault();

                    if (ticket == null) return RedirectToAction("NotFound", "Error");

                    if (ticket.TecnicoAsignado == null && ticket.TecnicoAsignadoReag == null && ticket.TecnicoAsignadoReag2 == null) { ViewBag.Regleta = "No muestra"; }
                    
                    //Ticket no ha sido asignado entonces muestra los botones de asignación
                    if (ticket.TecnicoAsignado == null) { ViewBag.MuestraAsignacion = "SI"; }

                    //Valida si se aprobó la reasignación o fue rechazada
                    //Estatus de Aprobaciones
                    //    1 = En Validación
                    //    2 = Validada
                    //    3 = Rechazada
                    if (ticket.ApruebaReasignacion == 1) { ViewBag.SoliciRechazada = "NO"; }


                    //Valida si es subticket
                    if (ticket.IdTicketPrincipal != null)
                    {
                        ViewBag.SubticketInfo = "SI";
                        ViewBag.SubticketId = ticket.IdTicketPrincipal;
                    }


                    //Valida el estatus del ticket - Gestion de Botones NUEVO AYB
                    ViewBag.EdoTicket = "";
                    var estadosTck = _db.cat_EstadoTicket.Where(a => a.Activo).ToList();

                    if (ticket.EstatusTicket == 1)
                    {
                        ViewBag.EdoTicket = "Abierto";
                        ViewBag.EstadoTicket = new SelectList(estadosTck.Where(x => x.Id == 7 || x.Id == 3), "Id", "Estado");

                    }
                    else if (ticket.EstatusTicket == 2)
                    {
                        ViewBag.EdoTicket = "Asignado";
                        ViewBag.EstadoTicket = new SelectList(estadosTck.Where(x => x.Id == 7 || x.Id == 3), "Id", "Estado");
                    }
                    else if (ticket.EstatusTicket == 3)
                    {
                        ViewBag.EdoTicket = "Trabajando";
                        ViewBag.EstadoTicket = new SelectList(estadosTck.Where(x => x.Id == 7 || x.Id == 4), "Id", "Estado");
                    }
                    else if (ticket.EstatusTicket == 4)
                    {
                        ViewBag.EdoTicket = "Resuelto";

                    }
                    else if (ticket.EstatusTicket == 5)//GARANTÍA
                    {

                        var his = _db.his_Ticket.Where(a => a.IdTicket == IdTicket && a.EstatusTicket == 5).OrderByDescending(a => a.FechaRegistro).FirstOrDefault();

                        var TimeNow = DateTime.Now;
                        var dias = (TimeNow - his.FechaRegistro).Days;
                        var diferencia = (TimeNow - his.FechaRegistro).Hours;
                        var horas = (dias * 24) + diferencia;

                        ViewBag.HoraGarantia = horas;

                        //Validar: sí el tiempo de garantia venció, pasar a Cerrado.
                        var gpo = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == ticket.SubCategoria).FirstOrDefault();

                        if (horas >= Convert.ToDouble(gpo.Garantia))
                        {
                            var Fingarantia = his.FechaRegistro;

                            //sacamos horas y minutos
                            var h = Math.Truncate(Convert.ToDouble(gpo.Garantia));

                            double m = (Convert.ToDouble(gpo.Garantia) - h)*100;

                            Fingarantia = Fingarantia.AddHours(h);
                            Fingarantia = Fingarantia.AddMinutes(m);

                            //Ajustamos a que la garantia sea tiempo de resuelto mas el garantia (sin sumar hasta datetime now)


                            _db.tbl_TicketDetalle.Attach(ticket);
                            ticket.Estatus = "Cerrado";
                            ticket.EstatusTicket = 6;
                            ticket.FechaRegistro = Fingarantia;
                            _db.SaveChanges();

                            //Guardar historico
                            _mng.SetHistoricoCambioEstatus(ticket.Id);

                        }

                    }
                    else if (ticket.EstatusTicket == 7)//EN ESPERA
                    {

                        ViewBag.EdoTicket = "En Espera";
                        ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Id == 3), "Id", "Estado");

                        var TimeNow = DateTime.Now;
                        var dias = (TimeNow - ticket.FechaRegistro).Days;
                        var diferencia = (TimeNow - ticket.FechaRegistro).Hours;
                        var horas = (dias * 24) + diferencia;

                      


                    }


                    //Archivos adjuntos
                    var dtoDw = _db.tblDocumentos.Where(a => a.IdTicket == IdTicket && a.Tipo != 5).OrderByDescending(a => a.FechaRegisto).ToList(); // tipo 5 = documentos de tareas programadas
                    detalle.Docs = dtoDw; 

                    var historico = _db.his_Ticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();
                    detalle.historico = historico;

                    detalle.detalle = _db.VWDetalleTicket.Find(IdTicket);

                    var his2 = historico.FirstOrDefault();

                    detalle.detalle.GrupoResolutor = his2.GrupoResolutor;

                    //Agregar solo diagnosticos relacionados con la subcategoría del ticket
                    int subcat = ticket.SubCategoria;
                    var diags = new SelectList(_db.catDiagnosticos.Where(x => x.Activo && x.IdSubcategoria == ticket.SubCategoria), "Id", "Diagnostico");
                    if (diags.Count() != 0)
                    {
                        ViewBag.DX = diags;
                        ViewBag.DXcont = diags.Count();
                    }
                    else 
                    {
                        ViewBag.DXcont = 0;

                        // Filtrar diagnosticos acorde a grupo reoslutor ---------------- START
                        string grupo = detalle.detalle.GrupoResolutor; 
                            if (grupo == null) grupo = "";
                        int idGrupoRes = _db.catGrupoResolutor.Where(t => t.Grupo == grupo).Select(t => t.Id).FirstOrDefault(); 
                        var categoriasDelGrupo = _db.cat_Categoria.Where(t => t.GrupoResolutor == idGrupoRes).Select(t => t.Id).ToArray();
                        var diagsByGrp = _db.catDiagnosticos.Where(t => categoriasDelGrupo.Contains(t.IdCategoria));
                        ViewBag.DX = new SelectList(diagsByGrp, "Diagnostico", "Diagnostico");
                        // Filtrar diagnosticos acorde a grupo reoslutor ---------------- END
                        //ViewBag.DX = new SelectList(_db.catDiagnosticos.Where(x => x.Activo), "Id", "Diagnostico"); // agregar empty diagnostic...
                    }

                    //===========
                    ViewBag.GrupoResolutorCat = new SelectList(_db.catGrupoResolutor.Where(x => x.Activo), "Id", "Grupo");
                    ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
                    ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
                    ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.UserLst = new SelectList(_db.tbl_User.Where(x => x.Activo && x.GrupoResolutor == ticket.GrupoResolutor), "Id", "NombreTecnico");

                    //Asignar lista
                    //ListSubticket

                    var lstSub = _db.vwDetalleSubticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();

                    detalle.ListSubticket = lstSub;
                    var slaPadre = new List<SlaTimesVm>(); bool EsHijo = false;
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
                    else
                    {//------------ relojes SLA

                        EsHijo = true;
                        var idPadre = _db.tbl_VinculacionDetalle.Where(t => t.IdTicketChild == IdTicket).FirstOrDefault();
                        if (idPadre != null)
                        {
                            var hisPadre = _db.his_Ticket.Where(t => t.IdTicket == idPadre.TicketPrincipal).ToList();
                            slaPadre = _sla.GetSlaTimes(hisPadre);
                        }
                    }


                    detalle.ListSubticket = lstSub;

                    detalle.Slas = _sla.GetSlaTimes(historico);

                    if (EsHijo)
                    {//------------ relojes SLA
                        string[] tipoSla = { "Tiempo Actual", "SLA Objetivo" };

                        foreach (var slatype in tipoSla) { 
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
                    ViewBag.DX = new SelectList(_db.catDiagnosticos.Where(x => x.Activo), "Diagnostico", "Diagnostico");

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
                    //

                    //----JOSUE-----                    
                    //TimeSpan? timeSpan = (DateTime.Now - AsignacionInfo.FechaRegistro);

                    //double hours = 0;
                    //int minutes = 0;
                    //if (timeSpan.Value.Days > 0)
                    //{
                    //    hours = timeSpan.Value.Days * 24 + timeSpan.Value.Hours;
                    //    minutes = timeSpan.Value.Minutes;
                    //}
                    //else
                    //{
                    //    hours = timeSpan.Value.Hours;
                    //    minutes = timeSpan.Value.Minutes;
                    //}

                    //detalle.horas_sla = AsignacionInfo.FechaRegistro.ToString("HH:mm");
                    //detalle.hours = hours < 10 ? "0" + hours.ToString() : hours.ToString();
                    //detalle.minutes = minutes < 10 ? "0" + minutes.ToString() : minutes.ToString();
                    //---------
                    detalle.Slas = _sla.GetSlaTimes(info);


                }

                return View(detalle);
            }


            return View();

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public JsonResult ValidaSubticket(int TicketId)
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
        [HttpPost]
        public ActionResult SetSubticket(DetalleSelectedTicketVm vm, HttpPostedFileBase upload)
        {
            var datos = new tbl_TicketDetalle();
            

            //Buscar la información del ticket principal
            var padre = _db.tbl_TicketDetalle.Where(a => a.Id == vm.subticket.IdTicket).FirstOrDefault();
            var TenicoCreadorDeSubticket = Int32.Parse(vm.EmployeeIdBO);// new
            var user = _db.tbl_User.Where(t => t.EmpleadoID == TenicoCreadorDeSubticket).FirstOrDefault();

            if (false) { 
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
                datos.NombreCompleto = padre.TecnicoAsignado.ToUpper();          // (solution friday issue)
                datos.NombreCompleto = "";                                       // (solution february issue)
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

                    var uploadName = Path.GetFileName(upload.FileName);
                    //Nombre de la carga
                    var NameCarga = "SUB_" + datos.Id + "_" + upload.FileName;
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

                    return RedirectToAction("DetalleTicket", "Tecnico", new { IdTicket = datos.IdTicketPrincipal, folio = datos.Id, EmployeeId = vm.EmployeeIdBO });
                }
                else
                {
                    ModelState.AddModelError("File", "Formato no soportado");
                    return PartialView();

                }

            }


            return RedirectToAction("DetalleTicket", "Tecnico", new { IdTicket = datos.IdTicketPrincipal, folio = datos.Id, EmployeeId = vm.EmployeeIdBO });
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public JsonResult AutoAsignacion(int TicketId)
        {
            var EmployeeId = 0;

            if (Session["EmpleadoNo"] != null)
            {

                EmployeeId = Convert.ToInt32(Session["EmpleadoNo"]);

            }
            else
            {

                return Json("Login", "Home");
            }


            var res = "";
            var NoReasignacion = 1;

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            var userInfo = _db.tbl_User.Where(a => a.EmpleadoID == EmployeeId).FirstOrDefault();


            if (info != null)
            {

                //Validar si tuvo reasignaciones
                if (info.NoReapertura == 1)
                {
                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignadoReag = userInfo.NombreTecnico;
                    info.IdTecnicoAsignadoReag = userInfo.Id;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.Estatus = "Asignado";
                    info.EstatusTicket = 2;
                    _db.SaveChanges();


                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);

                }
                else if (info.NoReapertura == 2)
                {

                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignadoReag2 = userInfo.NombreTecnico;
                    info.IdTecnicoAsignadoReag2 = userInfo.Id;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.Estatus = "Asignado";
                    info.EstatusTicket = 2;
                    _db.SaveChanges();

                }
                else
                {
                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignado = userInfo.NombreTecnico;
                    info.IdTecnicoAsignado = userInfo.Id;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.Estatus = "Asignado";
                    info.EstatusTicket = 2;
                    _db.SaveChanges();


                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);
                }

                res = "Correcto";

            }

            _spr.AsignarTecnicoTicketsVinculados(info); // aplicar la asignación del tecnico a tickets vinculados

            return Json(res, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public JsonResult SolicitaReasignacion(int TicketId, string Motivo)
        {

            //Estatus de Aprobaciones
            //    1 = En Validación
            //    2 = Validada
            //    3 = Rechazada


            var res = "";

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            if (info != null)
            {

                _db.tbl_TicketDetalle.Attach(info);
                info.MotivoReasignacion = Motivo;
                info.NoReasignaciones = info.NoReasignaciones;
                info.ApruebaReasignacion = 1; // 1 = En Validación
                _db.SaveChanges();

                res = "Correcto";

                var dtoHis = _db.his_Ticket.Where(a => a.IdTicket == TicketId).FirstOrDefault();

                _noti.SetNotiSolicitudReasignacion(dtoHis);

            }


            return Json(res, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public JsonResult RecategorizarTicket(int TicketId,  int Categoria, int Subcategoria)
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
                //De ser así, el ticket se modifica y pasa como Abierto y el tecnico vacio.

                info.Categoria = Categoria;
                info.SubCategoria = Subcategoria;
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


            _spr.Notif_Recategorizacion_de_Ticket(matrizCat.GrupoAtencion, TicketId, "tecnico");
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public ActionResult SetCambioEstatus(DetalleSelectedTicketVm vm, HttpPostedFileBase upload)
        {
            var Resp = "OK";
            //Antes de cambiar el estatus, valida si tiene subtickets
            var datos = _db.tbl_TicketDetalle.Where(a => a.Id == vm.detalle.Id).FirstOrDefault();

            if (datos.IdTicketPrincipal == null) //Es ticket principal
            {

                //Valida que tenga subtickets : Si tiene, validar que ya esten cerrados : De lo contrario no podrá Resolver el ticket
                if (vm.detalle.EstadoTicketTecnico == 4) //RESUELTO
                {
                    var subInfo = _db.tbl_TicketDetalle.Where(a => a.IdTicketPrincipal == datos.Id).ToList();

                    if (subInfo.Count > 0)//Tiene subtickets
                    {
                        var subCerrados = subInfo.Where(a => a.EstatusTicket == 6).ToList();

                        var totalSub = subInfo.Count;
                        var totalCerradosSub = subCerrados.Count;

                        if (totalCerradosSub == totalSub) //Se cierra Ticket
                        {
                            var nameEstatus = _db.cat_EstadoTicket.Where(a => a.Id == vm.detalle.EstadoTicketTecnico).FirstOrDefault();

                            if (datos != null)
                            {
                                _db.tbl_TicketDetalle.Attach(datos);
                                datos.MotivoCambioEstatus = vm.detalle.MotivoCambioEstatus;
                                datos.Estatus = nameEstatus.Estado;
                                datos.Diagnostico = vm.detalle.DXTecnico;
                                datos.EstatusTicket = vm.detalle.EstadoTicketTecnico;
                                datos.FechaRegistro = DateTime.Now;
                                _db.SaveChanges();

                                //Guarda historico
                                _mng.SetHistoricoCambioEstatus(datos.Id);

                            }

                            //Validar si tiene tickets Vinculados. - SI tiene, cambiar el estatus de cada uno
                            var VincTkt = _db.tbl_VinculacionDetalle.Where(a => a.TicketPrincipal == datos.Id).ToList();

                            ;
                            for (int i = 0; i < VincTkt.Count; i++)
                            {
                                //Busca el ticket vinculado en la tabla detalles
                                var idticketchild = VincTkt[i].IdTicketChild;

                                var busc = _db.tbl_TicketDetalle.Where(a => a.Id == idticketchild).FirstOrDefault();


                                if (busc != null)
                                {
                                    if (busc.Id != datos.Id) { // -------------* 
                                        _db.tbl_TicketDetalle.Attach(busc);
                                        busc.MotivoCambioEstatus = vm.detalle.MotivoCambioEstatus;
                                        busc.Estatus = nameEstatus.Estado;
                                        busc.Diagnostico = vm.detalle.DXTecnico;
                                        busc.EstatusTicket = vm.detalle.EstadoTicketTecnico;
                                        busc.FechaRegistro = DateTime.Now;
                                        _db.SaveChanges();

                                        //Guarda historico
                                        _mng.SetHistoricoVinculados(busc.Id);

                                    }


                                }


                            }
                        }
                        else 
                        {
                            //No se puede cerrar
                            Resp = "Error";

                        }

                        

                    }
                    else //No tiene subtickets
                    {
                        var nameEstatus = _db.cat_EstadoTicket.Where(a => a.Id == vm.detalle.EstadoTicketTecnico).FirstOrDefault();

                        if (datos != null)
                        {
                            _db.tbl_TicketDetalle.Attach(datos);
                            datos.MotivoCambioEstatus = vm.detalle.MotivoCambioEstatus;
                            datos.Estatus = nameEstatus.Estado;
                            datos.Diagnostico = vm.detalle.DXTecnico;
                            datos.EstatusTicket = vm.detalle.EstadoTicketTecnico;
                            datos.FechaRegistro = DateTime.Now;
                            _db.SaveChanges();

                            //Guarda historico
                            _mng.SetHistoricoCambioEstatus(datos.Id);

                        }

                        //Validar si tiene tickets Vinculados. - SI tiene, cambiar el estatus de cada uno
                        var VincTkt = _db.tbl_VinculacionDetalle.Where(a => a.TicketPrincipal == datos.Id).ToList();
                        ;
                        for (int i = 0; i < VincTkt.Count; i++)
                        {
                            //Busca el ticket vinculado en la tabla detalles
                            var idticketchild = VincTkt[i].IdTicketChild;

                            var busc = _db.tbl_TicketDetalle.Where(a => a.Id == idticketchild).FirstOrDefault();


                            if (busc != null)
                            {
                                if (busc.Id != datos.Id) { 
                                    _db.tbl_TicketDetalle.Attach(busc);
                                    busc.MotivoCambioEstatus = vm.detalle.MotivoCambioEstatus;
                                    busc.Estatus = nameEstatus.Estado;
                                    busc.Diagnostico = vm.detalle.DXTecnico;
                                    busc.EstatusTicket = vm.detalle.EstadoTicketTecnico;
                                    busc.FechaRegistro = DateTime.Now;
                                    _db.SaveChanges();

                                    //Guarda historico
                                    _mng.SetHistoricoVinculados(busc.Id);

                                }


                            }


                        }

                    }


                }
                else //Cambia a otro estatus diferente a RESUELTO 
                {
                    
                    var nameEstatus = _db.cat_EstadoTicket.Where(a => a.Id == vm.detalle.EstadoTicketTecnico).FirstOrDefault();

                    if (datos != null)
                    {
                        _db.tbl_TicketDetalle.Attach(datos);
                        datos.MotivoCambioEstatus = vm.detalle.MotivoCambioEstatus;
                        datos.Estatus = nameEstatus.Estado;
                        datos.Diagnostico = vm.detalle.DXTecnico;
                        datos.EstatusTicket = vm.detalle.EstadoTicketTecnico;
                        datos.FechaRegistro = DateTime.Now;
                        _db.SaveChanges();

                        //Guarda historico
                        _mng.SetHistoricoCambioEstatus(datos.Id);

                    }

                    //Validar si tiene tickets Vinculados. - SI tiene, cambiar el estatus de cada uno
                    var VincTkt = _db.tbl_VinculacionDetalle.Where(a => a.TicketPrincipal == datos.Id).ToList();


                    for (int i = 0; i < VincTkt.Count; i++)
                    {
                        //Busca el ticket vinculado en la tabla detalles
                        var idticketchild = VincTkt[i].IdTicketChild;

                        var busc = _db.tbl_TicketDetalle.Where(a => a.Id == idticketchild).FirstOrDefault();


                        if (busc != null)
                        {
                            if (busc.Id != datos.Id)
                            {
                                _db.tbl_TicketDetalle.Attach(busc);
                                busc.MotivoCambioEstatus = vm.detalle.MotivoCambioEstatus;
                                busc.Estatus = nameEstatus.Estado;
                                busc.Diagnostico = vm.detalle.DXTecnico;
                                busc.EstatusTicket = vm.detalle.EstadoTicketTecnico;
                                busc.FechaRegistro = DateTime.Now;
                                _db.SaveChanges();

                                //Guarda historico
                                _mng.SetHistoricoVinculados(busc.Id);
                                
                            }                               


                        }


                    }

                }

                

            }
            else  //Es subticket
            {
                var nameEstatus = _db.cat_EstadoTicket.Where(a => a.Id == vm.detalle.EstadoTicketTecnico).FirstOrDefault();

                if (datos != null)
                {
                    _db.tbl_TicketDetalle.Attach(datos);
                    datos.MotivoCambioEstatus = vm.detalle.MotivoCambioEstatus;
                    datos.Estatus = nameEstatus.Estado;
                    datos.Diagnostico = vm.detalle.DXTecnico;
                    datos.EstatusTicket = vm.detalle.EstadoTicketTecnico;
                    datos.FechaRegistro = DateTime.Now;
                    _db.SaveChanges();

                    //Guarda historico
                    _mng.SetHistoricoCambioEstatus(datos.Id);

                }

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
                    var NameCarga = datos.Id + "_" + upload.FileName;
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
                        _mng.SetArchivoTecnico(datos.Id, NameCarga);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return RedirectToAction("DetalleTicket", "Tecnico", new { IdTicket = datos.Id, EmployeeId = vm.EmployeeIdBO });
                }
                else
                {
                    ModelState.AddModelError("File", "Formato no soportado");
                    return PartialView();

                }

            }

            int UserId = Int32.Parse(vm.EmployeeIdBO);
            string rol = _spr.RoldeUsuario(UserId);

            return RedirectToAction("DetalleTicket", rol, new { IdTicket = datos.Id, EmployeeId = vm.EmployeeIdBO , Respuesta = Resp});
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        //=======================================================CATALOGOS============================================================================
        [HttpPost]
        public JsonResult GetGrupoResolutor(int Id)
        {

            var subcat = _db.cat_Categoria.Where(a => a.GrupoResolutor == Id).Select(a => new { a.Id, a.Categoria }).ToList();

            return Json(subcat, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public JsonResult GetCategoria(int Id)
        {

            var subcat = _db.cat_SubCategoria.Where(a => a.IDCategoria == Id).Select(a => new { a.Id, a.SubCategoria }).ToList();

            return Json(subcat, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public JsonResult GetMatriz(int Id)
        {

            var mat = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == Id).ToList();

            return Json(mat, JsonRequestBehavior.AllowGet);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        //=======================================================CATALOGOS============================================================================
        
        public ActionResult DetalleTarea(int TareaId, string EmployeeId)
        {

            // copiar y pegar en cualquier actionresult que requiere mandar usuario por un tubo si se intenta pasar de listo
            var userSession = Session["EmpleadoNo"].ToString(); if (userSession != EmployeeId) { return RedirectToAction("Error", "Document"); }

            ViewBag.idChild = 0;
            ViewBag.user = EmployeeId;

            //ENVIA OBJETO CON HISTORICO Y DETALLE DE Tarea
            var detalle = new TareasProgramadasHisVM();
            detalle.tareasHis = _db.vw_HistoricoTareas.Where(pointer => pointer.IdTarea == TareaId).ToList();
            detalle.tarea = _db.vw_TareasProgramadas.Where(pointer => pointer.Id == TareaId).FirstOrDefault();
            detalle.tblTarea = _db.tblTareasProgramadas.Where(pointer => pointer.Id == TareaId).FirstOrDefault();
            //Archivos adjuntos
            var dtoDw = _db.tblDocumentos.Where(a => a.IdTicket == TareaId && a.Tipo == 5).OrderByDescending(a => a.FechaRegisto).ToList();
            detalle.Docs = dtoDw;

            //Tiempo Actual y Tiempo Total
            detalle.tiempoEspera = _spr.esperaTotal(detalle.tareasHis);
            detalle.tiempoTotal = _spr.tiempoTotal(detalle.tareasHis);

            detalle.tareasHis = _db.vw_HistoricoTareas.Where(pointer => pointer.IdTarea == TareaId).OrderBy(x => x.FechaRegistro).ToList();

            foreach (var t in detalle.tareasHis) { foreach (char c in t.Evento) t.Evento = t.Evento.Replace(" ", String.Empty); }

            //Datos usuario 
            var NumeroPenta = Convert.ToInt32(EmployeeId);
            //var InfoUser = _rh.vw_DetalleEmpleado.Where(a => a.NumeroPenta == NumeroPenta).FirstOrDefault();
            //detalle.nombreCompleto = InfoUser.NombreCompleto;
            //detalle.id = InfoUser.NumeroPenta;
            //detalle.puesto = InfoUser.Puesto;
            //detalle.area = InfoUser.Area;
            //detalle.correo = InfoUser.Email;
            var InfoUser = _db.vw_INFO_USER_EMPLEADOS.Where(t => t.NumeroPenta == NumeroPenta).FirstOrDefault();
            detalle.id = InfoUser.NumeroPenta;
            detalle.nombreCompleto = InfoUser.NombreCompleto;
            detalle.puesto = InfoUser.Puesto;
            detalle.area = InfoUser.Area;
            detalle.correo = InfoUser.Email;

            //// id tarea
            var IDtarea = TareaId;
            var Taskid = _db.vw_TareasProgramadas.Where(e => e.Id == TareaId).FirstOrDefault();
            detalle.taskid = Taskid.Id;
            ViewBag.Msg = "";
            ViewBag.EdoTicket = detalle.tarea.Estatus;
            List<SelectListItem> Estatus = new List<SelectListItem>();
            switch (detalle.tarea.Estatus)
            {
                case "Asignacion Pendiente":
                    ViewBag.Msg = "Esta Tarea no ha sido asignada a ti todavía";
                    break;
                case "Asignado":
                    Estatus.Add(new SelectListItem() { Text = "En Espera", Value = "En Espera" });
                    Estatus.Add(new SelectListItem() { Text = "Trabajando", Value = "Trabajando" });
                    break;
                case "Trabajando":
                    Estatus.Add(new SelectListItem() { Text = "En Espera", Value = "En Espera" });
                    Estatus.Add(new SelectListItem() { Text = "Resuelto", Value = "Resuelto" });
                    break;
                case "En Espera":
                    Estatus.Add(new SelectListItem() { Text = "Trabajando", Value = "Trabajando" });
                    break;
                case "Resuelto":
                    ViewBag.Msg = "Solución a esta Tarea no ha sido aprobada todavía";
                    break;
                default:
                    ViewBag.Msg = "Error 003: Algo ha salido mal";
                    break;
            }
            ViewBag.Estatus = new SelectList(Estatus, "Value", "Text");

            /*Codigo MVP 23/06/22
             Se agrega datos al combobox de diagnostico obtenidos de la tabla [cat_Diagnosticos]
             */
            ViewBag.Diagnostico = new SelectList(_db.catDiagnosticos.Where(a => a.Activo == true), "Id", "Diagnostico");

            return View(detalle);
        }

        public ActionResult SetCambioEstatusTarea(TareasProgramadasHisVM vm, HttpPostedFileBase[] upload, int TareaId, string EmployeeId) {
            var table = new tbl_TareasProgramadas();
            table =  _db.tblTareasProgramadas.Where(x => x.Id == TareaId).SingleOrDefault();
            //var tbl = vm.tarea;
            table.Estatus = vm.tblTarea.Estatus;
            table.Diagnostico = vm.tblTarea.Diagnostico;
            if (vm.tblTarea.Diagnostico != null) 
            {
                var intdiag = Int32.Parse(vm.tblTarea.Diagnostico);
                table.Diagnostico = _db.catDiagnosticos.Where(a => a.Id == intdiag).Select(a => a.Diagnostico).FirstOrDefault();                
            }
            table.Observaciones = vm.tblTarea.Observaciones;
            var data = _mng.CambioEstatusTareaProgramada(table);

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

            ViewBag.rol = "";
            return RedirectToAction("DetalleTarea", "Tecnico", new { EmployeeId = EmployeeId, TareaId = TareaId});
        }

    }
}
