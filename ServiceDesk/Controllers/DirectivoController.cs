using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.Mvc;
using ServiceDesk.Models;
using ServiceDesk.ViewModels;
//
using ServiceDesk.Managers;
using System.Data;
using System.IO;
//
using System.Data.Entity.Migrations;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using SpreadsheetLight;


namespace ServiceDesk.Controllers
{
    public class DirectivoController : Controller
    {
        private readonly RHAdminContext _rh = new RHAdminContext();
        private readonly ServiceDeskManager _mng = new ServiceDeskManager();
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private readonly DashBoardManager _mngDas = new DashBoardManager();
        private readonly ServiceDeskManager _sdmanager = new ServiceDeskManager();
        private readonly AdminContext _admin = new AdminContext();
        private readonly SlaManager _sla = new SlaManager();

        /***********Codigo Dashboard********/
        //[Dashboard]
        public ActionResult Dashboard(int EmployeeID, int type = 0)
        {
            ViewBag.user = EmployeeID;
            ViewBag.rol = RoldeUsuario(EmployeeID);
            var User = EmployeeID;
            var edoTicket = _db.cat_EstadoTicket.Where(x => x.Activo == true).ToList(); //edoTicket = lista de categorías con tickets activos
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault(); //string con info del grupo resolutor del empleado accedienco a la pagina
            var tickets = _db.tbl_TicketDetalle.Where(x => x.GrupoResolutor == strGrupoResolutor).ToList(); //lista de tickets pertinentes al grupo resolutor

            List<int> ticketsVinculados = _db.tbl_VinculacionDetalle.Where(x => x.Activo == true).Select(x => x.IdTicketChild).ToList(); //DUDA, de donde sale la variable IdTicketChild?
            List<tbl_TicketDetalle> lstTemp = new List<tbl_TicketDetalle>();
            foreach (var item in tickets)
            {
                tbl_TicketDetalle o = new tbl_TicketDetalle() { };

                if (!ticketsVinculados.Contains(item.Id))
                {
                    o = item;
                    lstTemp.Add(o);
                }
                else
                {
                    var e = _db.tbl_Vinculacion.Where(x => x.IdTicket == item.Id).FirstOrDefault();
                    if (e != null)
                    {
                        o = item;
                        lstTemp.Add(o);
                    }
                }
            };
            if (lstTemp.Count > 0) { tickets = lstTemp; }





            List<SelectListItem> filtro = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Del más antiguo al más reciente" },
                new SelectListItem { Value = "2", Text = "Del más reciente al más antiguo" },
                new SelectListItem { Value = "3", Text = "Por prioridad" }
            };
            ViewBag.filtro = filtro;

            var data = tickets.GroupBy(info => info.Estatus).Select(group => new
            {
                EstatusTicket = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.EstatusTicket);
            vmDashbordResolutor vm = new vmDashbordResolutor();
            List<ticketByEstado> ticketByEstados = new List<ticketByEstado>();
            foreach (var item in data)
            {
                var _item = edoTicket.Where(x => x.Estado == item.EstatusTicket).FirstOrDefault();

                ticketByEstado oEstados = new ticketByEstado()
                {
                    estado = item.EstatusTicket,
                    total = item.Count,
                    orden = _item.Orden
                };

                ticketByEstados.Add(oEstados);
            }
            var temp = ticketByEstados.Select(x => x.estado).ToList();
            edoTicket.Select(x => x.Estado).ToList().ForEach(x =>
            {
                if (!temp.Contains(x))
                {
                    var _item = edoTicket.Where(o => o.Estado == x).FirstOrDefault();
                    ticketByEstado oEstados = new ticketByEstado()
                    {
                        estado = x,
                        total = 0,
                        orden = _item.Orden
                    };
                    ticketByEstados.Add(oEstados);
                }
            });



            vm.lstTicket = ticketByEstados.OrderBy(x => x.orden).ToList();



            return View(vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public PartialViewResult getTicketById(string ticket, string user)
        {
            int oTicket = int.Parse(ticket);
            int empledoId = int.Parse(user);

            vmDashbordResolutor vm = new vmDashbordResolutor();
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);
            var details = _db.tbl_TicketDetalle.Where(x => x.Id == oTicket).ToList();
            if (details.Count == 0)
            {
                return PartialView("../Directivo/PartialViews/_TicketDirectivo", vm);
            }
            var p = details.FirstOrDefault().Estatus;
            details.ForEach(x =>
            {
                var opCategoria = lstCategoria.Where(c => c.Id == x.Categoria).FirstOrDefault().Categoria;
                ticketResumenResolutor o = new ticketResumenResolutor()
                {
                    categoria = opCategoria,
                    prioridad = x.Prioridad,
                    tickedID = x.Id,
                    estatus = x.Estatus
                };
                lstTicketsResumen.Add(o);
            });
            vm.lstResumenResolutor = lstTicketsResumen;
            return PartialView("../Directivo/PartialViews/_TicketDirectivo", vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public PartialViewResult GetAllTickets(string user, string type, int idFiltro = 0)
        {
            int empledoId = int.Parse(user);
            vmDashbordResolutor vm = new vmDashbordResolutor();
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();            
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true); //lista de categorías con tickets activos            
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == empledoId).Select(x => x.GrupoResolutor).SingleOrDefault(); //obtener grupo resolutr del empleado que pide la info
            //detalles = detalles de los tickets que
            //      tienen el                  estatus = type
            //      son del            grupo resolutor = strGrupoResolutor
            var details = _db.tbl_TicketDetalle.Where(x => x.Estatus == type && x.GrupoResolutor == strGrupoResolutor).ToList(); 
            //trae una lista de todos los tickets activos que esten (vinculados) // esta lista dice que los tickets padres son sus propios hijos
            List<int> ticketsVinculados = _db.tbl_VinculacionDetalle.Where(x => x.Activo == true).Select(x => x.IdTicketChild).ToList(); 
            List<tbl_TicketDetalle> lstTemp = new List<tbl_TicketDetalle>();
            // Preparar SUBs para su impresión
            foreach (var item in details) 
            {
                item.isPadre = false;
                tbl_TicketDetalle o = new tbl_TicketDetalle() { };
                var lstSubTicket = _db.vwDetalleSubticket.Where(x => x.Id == item.Id).ToList(); // originalmente x.IdTicket // Lista de detalles de las vinculaciones donde item es SUB
                //Imprimir todos los SUBs del ticket ITEM
                if (lstSubTicket.Count() > 0) //si ticket ITEM tiene SUBs
                {
                    lstSubTicket.ForEach(x => //poner todos los SUBs en lista resumen
                    {
                        int _idPrioridad = 0;
                        if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                        else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                        else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                        var opCategoria = lstCategoria.Where(c => c.Id == item.Categoria).FirstOrDefault().Categoria; //obtener id de la categoría del ticket ITEM
                        ticketResumenResolutor obj = new ticketResumenResolutor() // formatear el ticket para su vista 
                        {
                            categoria = opCategoria,
                            prioridad = x.Prioridad,
                            tickedID = x.IdTicket,
                            checkVincular = false,
                            totVinculados = 0,
                            estatus = x.Estatus,
                            isPadre = true, //moded
                            idTicketPadre = x.IdTicket,
                            idSubTicket = x.Id,
                            isSubTicket = true,
                            idPrioridad = _idPrioridad,
                            orden = 2
                        }; 
                        lstTicketsResumen.Add(obj); //Mandar SUBs a su Impresión
                    });
                }
                
                // Formatear el ticket ITEM según sea padre o hijo (vinculaciones) antes de imprimirlo
                    // Si ticket ITEM noo esta vinculado
                    if (!ticketsVinculados.Contains(item.Id)) {
                        //añadir el ticker para impresion
                        o = item;
                        o.orden = 1;
                        lstTemp.Add(o);
                    } 
                    //si el ticket ITEM esta vinculado
                    else { 
                        //obtener el campo donde informa que este ticket es ticket padre
                        var e = _db.tbl_Vinculacion.Where(x => x.IdTicket == item.Id).FirstOrDefault();
                        if (e != null) { // Si ITEM realmente es un padre, Imprimirlo
                            o = item;
                            //totVinculados = total de tickets vinculados a ticket ITEM (problema de tabla VinculacionDetalle arreglado)
                            o.totVinculados = _db.tbl_VinculacionDetalle.Where(x => x.IdVinculacion == e.IdVinculacion).Count() - 1;
                            o.isPadre = true;
                            o.orden = 1;
                            lstTemp.Add(o);
                        }
                    }
            };
            if (lstTemp.Count > 0) { details = lstTemp; }

            details.ForEach(x =>
            {
                int _idPrioridad = 0;
                if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                var opCategoria = lstCategoria.Where(c => c.Id == x.Categoria).FirstOrDefault().Categoria; //obtener id de la categoría de ticket X
                //formatear X para imprimirlo
                ticketResumenResolutor o = new ticketResumenResolutor()
                {
                    categoria = opCategoria,
                    prioridad = x.Prioridad,
                    tickedID = x.Id,
                    checkVincular = false,
                    totVinculados = x.totVinculados ?? 0,
                    estatus = x.Estatus,
                    isPadre = x.isPadre,
                    isSubTicket = false, //added. All the Subs were printed in the first foreach, so here there shouldnt be any here, necesario para siguiente foreach
                    idPrioridad = _idPrioridad,
                    orden = x.orden
                };

                //Sacar los SUBs por que ya fueron impresos, imprimir lista ya sin SUBs
                    var lstSubTicket2 = _db.vwDetalleSubticket.ToList(); //lista de detalles de tabla de relaciones SUB-MAIN
                    // por cada ticket en tabla, verificar si el ticket O está en la columna de SUBs (columna: Id)
                    foreach (var tkt in lstSubTicket2)
                        if (tkt.Id == o.tickedID)
                            o.isSubTicket = true;  //informar que o es un SUB
                    if (o.isSubTicket != true) //si O NO es SUB 
                        lstTicketsResumen.Add(o); // Imprime ticket X // Linea original imprimia todos los tickets independiente mente de si ya habían sido impresos en el primer foreach
            });
            if (idFiltro == 1)
            {
                //Del más antiguo al más reciente
                vm.lstResumenResolutor = lstTicketsResumen.OrderBy(x => x.orden).ThenBy(x => x.tickedID).OrderBy(x => x.isPadre).ToList();
            }
            else if (idFiltro == 2)
            {
                //Del más reciente al más antiguo
                vm.lstResumenResolutor = lstTicketsResumen.OrderByDescending(x => x.tickedID).OrderBy(x => x.isPadre).ToList();
            }
            else if (idFiltro == 3)
            {
                //Por prioridad
                vm.lstResumenResolutor = lstTicketsResumen.OrderBy(x => x.orden).ThenBy(x => x.idPrioridad).ToList();
            }
            else
            {
                vm.lstResumenResolutor = lstTicketsResumen;
            } 
            return PartialView("../Directivo/PartialViews/_TicketDirectivo", vm);
        }
        public ActionResult DetalleTicket(int EmployeeID,  int? IdTicket, string folio, int isChild = 0)
        {

            ViewBag.idChild = 0;
            ViewBag.rol = RoldeUsuario(EmployeeID);
            ViewBag.user = EmployeeID.ToString();

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

                    var AsignacionInfo = _db.tbl_TicketDetalle.Where(a => a.Id == IdTicket).FirstOrDefault();


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

                        ViewBag.HorasEspera = horas;


                    }


                    //Archivos adjuntos
                    var dtoDw = _db.tblDocumentos.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegisto).ToList();
                    detalle.Docs = dtoDw;
                    //

                    var info = _db.his_Ticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();
                    detalle.historico = info;
                    detalle.detalle = _db.VWDetalleTicket.Find(IdTicket);

                    var his2 = _db.his_Ticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).FirstOrDefault();

                    detalle.detalle.GrupoResolutor = his2.GrupoResolutor;

                    ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Activo), "Id", "Estado");
                    ViewBag.DX = new SelectList(_db.catDiagnosticos, "Diagnostico", "Diagnostico");

                    //===========
                    ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
                    ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
                    ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.UserLst = new SelectList(_db.tbl_User.Where(x => x.Activo), "NombreTecnico", "NombreTecnico");

                    //Asignar lista
                    //ListSubticket

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
                        //detalle.lstVinculacion.Add()
                        detalle.ListSubticket = lstSub;
                    }
                    //----JOSUE-----                    
                    TimeSpan? timeSpan = (DateTime.Now - AsignacionInfo.FechaRegistro);

                    double hours = 0;
                    int minutes = 0;
                    if (timeSpan.Value.Days > 0)
                    {
                        hours = timeSpan.Value.Days * 24 + timeSpan.Value.Hours;
                        minutes = timeSpan.Value.Minutes;
                    }
                    else
                    {
                        hours = timeSpan.Value.Hours;
                        minutes = timeSpan.Value.Minutes;
                    }

                    detalle.horas_sla = AsignacionInfo.FechaRegistro.ToString("HH:mm");
                    detalle.hours = hours < 10 ? "0" + hours.ToString() : hours.ToString();
                    detalle.minutes = minutes < 10 ? "0" + minutes.ToString() : minutes.ToString();
                    //---------
                    detalle.Slas = _sla.GetSlaTimes(info);
                }

                return View(detalle);
            }

            return View();
        }        
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -      
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
                    ViewBag.UserLst = new SelectList(_db.tbl_User.Where(x => x.Activo), "NombreTecnico", "NombreTecnico");


                    var lstSub = _db.vwDetalleSubticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();

                }

                return View(detalle);
            }


            return View();

        }
        /***********Codigo Dashboard********/




        public ActionResult EncuestaSatisfaccion()
        {
            var vm = new EncuestaDetalle();

            if (Session["EmpleadoNo"] != null)
            {
                vm.EmpleadoId = Convert.ToInt32(Session["EmpleadoNo"]);
            }
            else
            {
                return Json("Login", "Home");
            }

            var id = Request["TicketId"];
            vm.IdTicket = Convert.ToInt32(id);
            return View(vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public ActionResult SetEncuesta(EncuestaDetalle vm)
        {
            //Guarda
            _sdmanager.SetEncuesta(vm);
            return RedirectToAction("Success", "DashBoard");
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -     






        // Eliminar todo debajo de esta linea después de que este funcionando completamente el componente Reportes
        /***********Codigo MVP Grid********/
        [HttpGet]
        public ViewResult GridTickets(int EmployeeID)

        {
            ViewBag.user = EmployeeID;
            var detalle = new DetalleSelectedTicketVm();
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
            var dtoViewDetalle = _db.VWDetalleTicket.Where(pointer => pointer.GrupoResolutor == strGrupoResolutor).OrderByDescending(pointer => pointer.Id).ToList();
            detalle.ViewDetalleTickets = dtoViewDetalle;
            //Eliminar las horas antes de enviar los datos al grid 
            foreach (var ticket in detalle.ViewDetalleTickets) { 
                var fechaSinHora = ticket.FechaRegistro;
                ticket.FechaRegistro = fechaSinHora.Date;
            }
            return View(detalle);
        }
        private byte[] ConvertToBytes(int EmployeeID)
        {
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
            var list = _db.VWDetalleTicket.Where(pointer => pointer.GrupoResolutor == strGrupoResolutor).OrderByDescending(pointer => pointer.Id).ToList();
            byte[] result;

            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);

            foreach (var item in list)
            {

                sw.Write(item.Id);
                sw.WriteLine();
            }
            //return ms.ToArray();
            return System.IO.File.ReadAllBytes(@"c:\Reporte.xlsx");
            //return list.SelectMany(x => x).toArray(); 
        }
        public FileResult ExportToCSV(int EmployeeID)
        {
            byte[] fileBytes = ConvertToBytes(EmployeeID);
            string fileName = "Reporte.xls";    
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        /***********FIN CODIGO GRID MVP********/
        public ActionResult Reportes(int EmployeeID)
        {
            ViewBag.user = EmployeeID;

            return View();
        }

        /***********Codigo MVP Pie Chart********/
        public ActionResult PieChartPrioridad(int EmployeeID, DateTime? fechaInicial, DateTime? fechaFinal)
        {
            //strGrupoResolutor = grupo resolutor del empleado
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
            //tickets = detalle de tickets relacionados al strGrupoResolutor
            var tickets = _db.VWDetalleTicket.Where(x => x.GrupoResolutor == strGrupoResolutor).ToList();

            //Formateo de Fechas (cambio de DateTime? a DateTime, poner horas a 12:00:00)
            DateTime fechaTemp;
            if (fechaInicial != null)
            {
                fechaTemp = (DateTime)(fechaInicial);
                fechaInicial = fechaTemp.Date;
            }
            if (fechaFinal != null)
            {
                fechaTemp = (DateTime)(fechaFinal);
                fechaFinal = fechaTemp.Date;
            }
            //Quitar horas del campo FechaRegistro de la lista tickets 
            foreach (var item in tickets)
            {
                var fechaSinHora = item.FechaRegistro;
                item.FechaRegistro = fechaSinHora.Date;
            }
            //Eliminar tickets previos a fecha inicial  //      Filtro de fechas
            var ticketsDespuesDeFiltro = tickets.ToList();
            ticketsDespuesDeFiltro.Clear();

            foreach (var item in tickets)
            {
                if (item.FechaRegistro < fechaFinal && item.FechaRegistro > fechaInicial && fechaInicial != null && fechaFinal != null)
                {
                    ticketsDespuesDeFiltro.Add(item);//Si hay fechas: Agrega a la lista lo que haya sido registrado después de f.inicial y antes de f.final
                }
                if (fechaFinal == null || fechaInicial == null)
                {
                    ticketsDespuesDeFiltro.Add(item); //Si no hay fechas, has tu chamba normalmente
                    Console.WriteLine("debugger");
                }
            }
            tickets = ticketsDespuesDeFiltro;
            // END                                          Filtro de fechas

            var cantTickets = tickets.Count();
            var cantTickets2 = tickets.Count();
            if (cantTickets != cantTickets2)
            { }
            var data = tickets.GroupBy(info => info.Prioridad).Select(group => new
            {
                Prioridad = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.Prioridad);
            DatosReportes datos = null;
            String[] strPrioridad = new string[data.Count()];
            int[] intCount = new int[data.Count()];
            int i = 0;
            foreach (var item in data)
            {
                strPrioridad[i] = item.Prioridad;
                intCount[i] = item.Count;
                i++;
            }

            datos = new DatosReportes

            {
                Column = strPrioridad,
                Count = intCount

            };

            if (datos != null)
            {
                return View(datos);
            }
            else
                return View();
        }
        public ActionResult PieChartCentro(int EmployeeID, DateTime? fechaInicial, DateTime? fechaFinal)
        {
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
            var tickets = _db.VWDetalleTicket.Where(x => x.GrupoResolutor == strGrupoResolutor).ToList();

            //Formateo de Fechas (cambio de DateTime? a DateTime, poner horas a 12:00:00)
            DateTime fechaTemp;
            if (fechaInicial != null)
            {
                fechaTemp = (DateTime)(fechaInicial);
                fechaInicial = fechaTemp.Date;
            }
            if (fechaFinal != null)
            {
                fechaTemp = (DateTime)(fechaFinal);
                fechaFinal = fechaTemp.Date;
            }
            //Quitar horas del campo FechaRegistro de la lista tickets 
            foreach (var item in tickets)
            {
                var fechaSinHora = item.FechaRegistro;
                item.FechaRegistro = fechaSinHora.Date;
            }
            //Eliminar tickets previos a fecha inicial  //      Filtro de fechas
            var ticketsDespuesDeFiltro = tickets.ToList();
            ticketsDespuesDeFiltro.Clear();

            foreach (var item in tickets)
            {
                if (item.FechaRegistro < fechaFinal && item.FechaRegistro > fechaInicial && fechaInicial != null && fechaFinal != null)
                {
                    ticketsDespuesDeFiltro.Add(item);//Si hay fechas: Agrega a la lista lo que haya sido registrado después de f.inicial y antes de f.final
                }
                if (fechaFinal == null || fechaInicial == null)
                {
                    ticketsDespuesDeFiltro.Add(item); //Si no hay fechas, has tu chamba normalmente
                    Console.WriteLine("debugger");
                }
            }
            tickets = ticketsDespuesDeFiltro;
            // END                                          Filtro de fechas

            var data = tickets.GroupBy(info => info.Centro).Select(group => new
            {
                Centro = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.Centro);
            DatosReportes datos = null;
            String[] strPrioridad = new string[data.Count()];
            int[] intCount = new int[data.Count()];
            int i = 0;
            foreach (var item in data)
            {
                strPrioridad[i] = item.Centro.ToString();
                intCount[i] = item.Count;
                i++;
            }

            datos = new DatosReportes
            {
                Column = strPrioridad,
                Count = intCount
            };

            if (datos != null) { return View(datos); }
            else return View();
        }
        public ActionResult PieChartEstatus(int EmployeeID, DateTime? fechaInicial, DateTime? fechaFinal)
        {
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
            var tickets = _db.VWDetalleTicket.Where(x => x.GrupoResolutor == strGrupoResolutor).ToList();

            //Formateo de Fechas (cambio de DateTime? a DateTime, poner horas a 12:00:00)
            DateTime fechaTemp;
            if (fechaInicial != null)
            {
                fechaTemp = (DateTime)(fechaInicial);
                fechaInicial = fechaTemp.Date;
            }
            if (fechaFinal != null)
            {
                fechaTemp = (DateTime)(fechaFinal);
                fechaFinal = fechaTemp.Date;
            }
            //Quitar horas del campo FechaRegistro de la lista tickets 
            foreach (var item in tickets)
            {
                var fechaSinHora = item.FechaRegistro;
                item.FechaRegistro = fechaSinHora.Date;
            }
            //Eliminar tickets previos a fecha inicial  //      Filtro de fechas
            var ticketsDespuesDeFiltro = tickets.ToList();
            ticketsDespuesDeFiltro.Clear();

            foreach (var item in tickets)
            {
                if (item.FechaRegistro < fechaFinal && item.FechaRegistro > fechaInicial && fechaInicial != null && fechaFinal != null)
                {
                    ticketsDespuesDeFiltro.Add(item);//Si hay fechas: Agrega a la lista lo que haya sido registrado después de f.inicial y antes de f.final
                }
                if (fechaFinal == null || fechaInicial == null)
                {
                    ticketsDespuesDeFiltro.Add(item); //Si no hay fechas, has tu chamba normalmente
                    Console.WriteLine("debugger");
                }
            }
            tickets = ticketsDespuesDeFiltro;
            // END                                          Filtro de fechas

            var data = tickets.GroupBy(info => info.Estatus).Select(group => new
            {
                Estatus = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.Estatus);
            DatosReportes datos = null;
            String[] strColumnName = new string[data.Count()];
            int[] intCount = new int[data.Count()];
            int i = 0;
            foreach (var item in data)
            {
                strColumnName[i] = item.Estatus;
                intCount[i] = item.Count;
                i++;
            }

            datos = new DatosReportes

            {
                Column = strColumnName,
                Count = intCount

            };

            if (datos != null)
            {
                return View(datos);
            }
            else
                return View();
        }
        public ActionResult PieChartTipo(int EmployeeID, DateTime? fechaInicial, DateTime? fechaFinal)
        {
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
            //var tickets = _db.tbl_TicketDetalle.Select(x => x.SubCategoria).ToList();
            var categorias = _db.cat_MatrizCategoria.Where(a => _db.tbl_TicketDetalle.Any(b => b.SubCategoria == a.IDSubCategoria));            //instanciar, datos iniciales irrelevantes
            var tickets = _db.tbl_TicketDetalle.Where(x => x.GrupoResolutor == strGrupoResolutor).ToList();
            //Formateo de Fechas (cambio de DateTime? a DateTime, poner horas a 12:00:00)
            DateTime fechaTemp;
            if (fechaInicial != null)
            {
                fechaTemp = (DateTime)(fechaInicial);
                fechaInicial = fechaTemp.Date;
            }
            if (fechaFinal != null)
            {
                fechaTemp = (DateTime)(fechaFinal);
                fechaFinal = fechaTemp.Date;
            }
            //Quitar horas del campo FechaRegistro de la lista tickets 
            foreach (var item in tickets)
            {
                var fechaSinHora = item.FechaRegistro;
                item.FechaRegistro = fechaSinHora.Date;
            }
            //Eliminar tickets previos a fecha inicial  //      Filtro de fechas
            var IntSubCats = new List<int>();
            foreach (var item in tickets)
            {
                if (item.FechaRegistro < fechaFinal && item.FechaRegistro > fechaInicial && fechaInicial != null && fechaFinal != null)
                {
                    IntSubCats.Add(item.SubCategoria);
                }
                if (fechaFinal == null || fechaInicial == null)
                {
                    IntSubCats.Add(item.SubCategoria);
                    Console.WriteLine("debugger");
                }
            }
            // En este punto IntSubCats tiene una lista de ids de subcategorías filtrada por fechas -----------------------------------------------------------
            // END                                          Filtro de fechas
            categorias = _db.cat_MatrizCategoria.Where(Matriz => IntSubCats.Contains(Matriz.IDSubCategoria));
            Console.Write("debug");

            //-------------------------------------------------------------------------
            var data = categorias.GroupBy(info => info.Incidencia).Select(group => new
            {
                Incidencia = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.Incidencia);
            DatosReportes datos = null;
            String[] strColumnName = new string[data.Count()];
            int[] intCount = new int[data.Count()];
            int i = 0;
            foreach (var item in data)
            {
                strColumnName[i] = item.Incidencia;
                intCount[i] = item.Count;
                i++;
            }

            datos = new DatosReportes
            {
                Column = strColumnName,
                Count = intCount

            };

            if (datos != null) { return View(datos); }
            else return View();
        }
        public ActionResult PieChartExpertiz(int EmployeeID, DateTime? fechaInicial, DateTime? fechaFinal)
        {
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
            //var tickets = _db.tbl_TicketDetalle.Select(x => x.SubCategoria).ToList();
            var categorias = _db.cat_MatrizCategoria.Where(a => _db.tbl_TicketDetalle.Any(b => b.SubCategoria == a.IDSubCategoria));            //instanciar, datos iniciales irrelevantes
            var tickets = _db.tbl_TicketDetalle.Where(x => x.GrupoResolutor == strGrupoResolutor).ToList();
            //Formateo de Fechas (cambio de DateTime? a DateTime, poner horas a 12:00:00)
            DateTime fechaTemp;
            if (fechaInicial != null)
            {
                fechaTemp = (DateTime)(fechaInicial);
                fechaInicial = fechaTemp.Date;
            }
            if (fechaFinal != null)
            {
                fechaTemp = (DateTime)(fechaFinal);
                fechaFinal = fechaTemp.Date;
            }
            //Quitar horas del campo FechaRegistro de la lista tickets 
            foreach (var item in tickets)
            {
                var fechaSinHora = item.FechaRegistro;
                item.FechaRegistro = fechaSinHora.Date;
            }
            //Eliminar tickets previos a fecha inicial  //      Filtro de fechas
            var IntSubCats = new List<int>();
            foreach (var item in tickets)
            {
                if (item.FechaRegistro < fechaFinal && item.FechaRegistro > fechaInicial && fechaInicial != null && fechaFinal != null)
                {
                    IntSubCats.Add(item.SubCategoria);
                }
                if (fechaFinal == null || fechaInicial == null)
                {
                    IntSubCats.Add(item.SubCategoria);
                    Console.WriteLine("debugger");
                }
            }
            // En este punto IntSubCats tiene una lista de ids de subcategorías filtrada por fechas 
            categorias = _db.cat_MatrizCategoria.Where(Matriz => IntSubCats.Contains(Matriz.IDSubCategoria));
            Console.Write("debug");
            // END                                          FIN Filtro de fechas

            var data = categorias.GroupBy(info => info.NivelExpertiz).Select(group => new
            {
                NivelExpertiz = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.NivelExpertiz);
            DatosReportes datos = null;
            String[] strColumnName = new string[data.Count()];
            int[] intCount = new int[data.Count()];
            int i = 0;
            foreach (var item in data)
            {
                strColumnName[i] = item.NivelExpertiz;
                intCount[i] = item.Count;
                i++;
            }

            datos = new DatosReportes

            {
                Column = strColumnName,
                Count = intCount

            };

            if (datos != null)
            {
                return View(datos);
            }
            else
                return View();
        }
        public ActionResult PieChartSLA(int EmployeeID, DateTime? fechaInicial, DateTime? fechaFinal)
        {
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
            var tickets = _db.tbl_TicketDetalle.Where(ticket => ticket.GrupoResolutor.Equals(strGrupoResolutor)).ToList();
            var categorias = _db.cat_MatrizCategoria.Where(categoria => _db.tbl_TicketDetalle.Any(b => b.SubCategoria == categoria.IDSubCategoria));
            //Formateo de Fechas (cambio de DateTime? a DateTime, poner horas a 12:00:00)
            DateTime fechaTemp;
            if (fechaInicial != null)
            {
                fechaTemp = (DateTime)(fechaInicial);
                fechaInicial = fechaTemp.Date;
            }
            if (fechaFinal != null)
            {
                fechaTemp = (DateTime)(fechaFinal);
                fechaFinal = fechaTemp.Date;
            }
            //Quitar horas del campo FechaRegistro de la lista tickets 
            foreach (var item in tickets)
            {
                var fechaSinHora = item.FechaRegistro;
                item.FechaRegistro = fechaSinHora.Date;
            }
            //Eliminar tickets previos a fecha inicial  //      Filtro de fechas
            var IntSubCats = new List<int>();
            foreach (var item in tickets)
            {
                if (item.FechaRegistro < fechaFinal && item.FechaRegistro > fechaInicial && fechaInicial != null && fechaFinal != null)
                {
                    IntSubCats.Add(item.SubCategoria);
                }
                if (fechaFinal == null || fechaInicial == null)
                {
                    IntSubCats.Add(item.SubCategoria);
                    Console.WriteLine("debugger");
                }
            }
            // En este punto IntSubCats tiene una lista de ids de subcategorías filtrada por fechas -----------------------------------------------------------
            // END                                          Filtro de fechas
            categorias = _db.cat_MatrizCategoria.Where(Matriz => IntSubCats.Contains(Matriz.IDSubCategoria));
            Console.Write("debug");

            var data = categorias.GroupBy(info => info.SLAObjetivo).Select(group => new
            {
                SLAObjetivo = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.SLAObjetivo);
            DatosReportes datos = null;
            String[] strColumnName = new string[data.Count()];
            int[] intCount = new int[data.Count()];
            int i = 0;
            foreach (var item in data)
            {

                strColumnName[i] = item.SLAObjetivo;
                intCount[i] = item.Count;
                i++;
            }

            datos = new DatosReportes

            {
                Column = strColumnName,
                Count = intCount

            };

            if (datos != null)
            {
                return View(datos);
            }
            else
                return View();
        }
        //************* excel related stuff
        public FileResult DescargarExcelReportes(int EmployeeID)
        {
            // Conexión a excel
            string Test = @"C:\Users\ivan_\Documents\PC\PlantillaParaReporte.xlsx"; // ------------------- ruta de plantilla
            SLDocument sl = new SLDocument(Test);
            string savePath = @"C:\Users\ivan_\Downloads\Reportes.xlsx";            // Ruta de guardado de archivo temporal

            sl = LlenarExcel(sl);

            //sl.Save();
            sl.SaveAs(savePath);  // Guardar en ruta especifica
            byte[] fileBytes = System.IO.File.ReadAllBytes(savePath);
            string fileName = "Reportes.xlsx";
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        public SLDocument LlenarExcel(SLDocument sl)
        {
            // Obtener datos de BD
            var detalle = new DetalleSelectedTicketVm();                // SLA stuff
            var vwReportes = _db.vw_DetalleReportes.ToList();

            // Variables temporales
            string tenicoQueCerro = null;
            string incidencia = null;
            string SLAobjetivo = null;
            int? subticket = 0;
            int? noReasignaciones = 0;
            int x = 0;     //  contador de tickets
            int row = 4;   //  Fila desde la cual iniciar a escribir
            int id;
            string virgulilla = " ~ ";
            string horasSLA = "", minSLA = "";
            int minSLAint = 0;

            // Ejemplo de Cell Style Manipulation
            var style = sl.GetCellStyle("C4");
            sl.SetCellStyle("A9", style);
            // Setup styles for modification, change formating
            style = sl.CreateStyle();
            style.FormatCode = "#,##0.00";


            // Escritura  
            foreach (var ticket in vwReportes)
            {
                // Obtener tecnico que cerró el ticket
                tenicoQueCerro = ticket.TecnicoAsignadoReag2;
                if (tenicoQueCerro == null || tenicoQueCerro == "") tenicoQueCerro = ticket.TecnicoAsignadoReag;
                if (tenicoQueCerro == null || tenicoQueCerro == "") tenicoQueCerro = ticket.TecnicoAsignado;

                // Obtener datos: incidencia y SLAObjetivo
                incidencia = ticket.Incidencia;
                SLAobjetivo = ticket.SLAObjetivo;

                // Obtener datos: SLA Total en minutos                  //x = ticket.Id;
                id = vwReportes[x].Id;
                detalle.detalle = _db.VWDetalleTicket.Find(id);
                var info = _db.his_Ticket.Where(a => a.IdTicket == id).ToList(); // OrderByDescending(a => a.FechaRegistro).
                detalle.Slas = _sla.GetSlaTimes(info);
                foreach (var sLa in detalle.Slas)
                {
                    if (sLa.Type == "SLA total")
                    {
                        // Si se tienen que agregar colores guardados en los SLA poner en esta linea
                        horasSLA = sLa.Time.ToString();
                    }
                }
                minSLA = horasSLA.Substring(0, horasSLA.Length - 3);
                minSLAint = Int32.Parse(minSLA) * 60;

                // Obtener color para el style
                style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml(vwReportes[x].Color), System.Drawing.Color.Black); //cambiar style              
                LlenarTabla(sl, row, vwReportes, subticket, virgulilla, x, incidencia, tenicoQueCerro, noReasignaciones, SLAobjetivo, minSLAint, style, null);

                // Obtener datos: subtickets
                foreach (var sub in vwReportes)
                {
                    if (sub.Id == ticket.IdTicketPrincipal)
                    {
                        //LlenarTabla(sl, row, vwReportes, subticket, virgulilla, x, incidencia, tenicoQueCerro, noReasignaciones, SLAobjetivo, minSLAint, style, 1);
                    }
                    if (sub.IdTicketPrincipal == ticket.Id)
                    { //sub es padre de ticketPrincipal
                        subticket = sub.Id; row++;
                        LlenarTabla(sl, row, vwReportes, subticket, virgulilla, x, incidencia, tenicoQueCerro, noReasignaciones, SLAobjetivo, minSLAint, style, null);
                    }
                    if (sub.Id != ticket.IdTicketPrincipal && sub.IdTicketPrincipal != ticket.Id)
                    {
                    }
                }



                x++; row++;
                subticket = 0;
                noReasignaciones = 0;
                tenicoQueCerro = null;
                incidencia = null;
                SLAobjetivo = null;
                horasSLA = "";
                minSLA = "";
                minSLAint = 0;
            }
            return sl;
        }
        public SLDocument LlenarTabla(SLDocument sl, int row, List<vw_DetalleReportes> vwReportes, int? subticket, string virgulilla, int x, string incidencia, string tenicoQueCerro, int? noReasignaciones, string SLAobjetivo, int minSLAint, SLStyle style, int? colores)
        {

            sl.SetCellValue("A" + row.ToString(), vwReportes[x].Id.ToString());                           // id ticket
            if (colores == 1)
            {
                style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                sl.SetCellStyle("A" + row.ToString(), style);
            }

            if (subticket != 0 && subticket != null)
            {
                style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                sl.SetCellStyle("A" + row.ToString(), style);
                sl.SetCellStyle("B" + row.ToString(), style);
                sl.SetCellValue("B" + row.ToString(), subticket.ToString());                   // id del subticket             
            }
            else
                sl.SetCellValue("B" + row.ToString(), "");

            sl.SetCellValue("C" + row.ToString(), vwReportes[x].Area);                         // Area del solicitante
            sl.SetCellValue("D" + row.ToString(), vwReportes[x].EstatusTicket);                // Estatus Ticket 
            sl.SetCellValue("E" + row.ToString(), vwReportes[x].Categoria);                    // Categoria
            sl.SetCellValue("F" + row.ToString(), vwReportes[x].SubCategoria);                 // Subcategoría

            if (incidencia != null)
                sl.SetCellValue("G" + row.ToString(), incidencia);                             // Incidencia / Solicitante     
            else
                sl.SetCellValue("G" + row.ToString(), virgulilla);

            sl.SetCellValue("H" + row.ToString(), vwReportes[x].GrupoResolutor);               // GrupoResolutor
            sl.SetCellValue("I" + row.ToString(), vwReportes[x].Prioridad);                    // Prioridad
            sl.SetCellValue("J" + row.ToString(), vwReportes[x].DescripcionIncidencia);        // Descripción

            if (tenicoQueCerro != "" && tenicoQueCerro != null)
                sl.SetCellValue("K" + row.ToString(), tenicoQueCerro);                         // Tecnico que cerró el ticket  hay alguna manera de obtenerlo?
            else
                sl.SetCellValue("K" + row.ToString(), virgulilla);

            if (noReasignaciones != 0 && noReasignaciones != null)
                sl.SetCellValue("L" + row.ToString(), noReasignaciones.ToString());            // Numero de reasignaciones     
            else
                sl.SetCellValue("L" + row.ToString(), virgulilla);

            sl.SetCellValue("M" + row.ToString(), vwReportes[x].NoReapertura.ToString());      // Numero de reaperturas 

            if (SLAobjetivo != null)
                sl.SetCellValue("N" + row.ToString(), SLAobjetivo.ToString());                 // SLA Objetivo                 
            else
                sl.SetCellValue("N" + row.ToString(), virgulilla);

            sl.SetCellValue("O" + row.ToString(), minSLAint.ToString());                        // SLA Total minutos    

            //style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml(vwReportes[x].Color), System.Drawing.Color.Black); //cambiar style
            //sl.SetCellStyle("P" + row.ToString(), style);                   //dar style a celda row de columna P
            //sl.SetCellValue("P" + row.ToString(), "");                                 // Estatus SLA global           ?

            //sl.SetCellValue("Q" + row.ToString(), "Ligas fuera de servicio temporalmente");     // Liga Detalle de ticket   

            return sl;
        }
        public void LeerExcel(SLDocument sl)
        {

            //Ejemplo de lectura
            if (true)
            {
                //Lectura de Fila 4                                                         LECTURA
                //int iRow = 4;
                //while (!string.IsNullOrEmpty(sl.GetCellValueAsString(iRow, 1)))
                //{
                //    int Ticket                = sl.GetCellValueAsInt32(iRow, 1);
                //    string SubTicket          = sl.GetCellValueAsString(iRow, 2);
                //    string AreaSolicitante    = sl.GetCellValueAsString(iRow, 3);
                //    string EstatusTicket      = sl.GetCellValueAsString(iRow, 4);
                //    string Categoria          = sl.GetCellValueAsString(iRow, 5);
                //    string SubCategoria       = sl.GetCellValueAsString(iRow, 6);
                //    string Incidencia         = sl.GetCellValueAsString(iRow, 7);
                //    string GrupoResolutor     = sl.GetCellValueAsString(iRow, 8);
                //    string Prioridad          = sl.GetCellValueAsString(iRow, 9);
                //    string Descripcion        = sl.GetCellValueAsString(iRow, 10);
                //    string TecnicoResolutor   = sl.GetCellValueAsString(iRow, 11);
                //    int NumerodeResignaciones = sl.GetCellValueAsInt32(iRow, 12);
                //    int NumerodeReaperturas   = sl.GetCellValueAsInt32(iRow, 13);
                //    int SLAObjetivo           = sl.GetCellValueAsInt32(iRow, 14);
                //    int SLAtotal              = sl.GetCellValueAsInt32(iRow, 15);
                //    string EstatusSla         = sl.GetCellValueAsString(iRow, 16);
                //    string LigaDetalle        = sl.GetCellValueAsString(iRow, 17);

                //    string[] lineaOutput = { Ticket.ToString(), SubTicket, AreaSolicitante, EstatusTicket, Categoria,
                //        SubCategoria, Incidencia, GrupoResolutor, Prioridad, Descripcion, TecnicoResolutor,
                //        NumerodeResignaciones.ToString(), NumerodeReaperturas.ToString(), SLAObjetivo.ToString(),
                //        SLAtotal.ToString(), EstatusSla, LigaDetalle };

                //    iRow++;

                //    foreach (var line in lineaOutput) {
                //        Console.WriteLine(line);
                //    }
                //}
            }
        }


        public string RoldeUsuario(int EmployeeID) //String que obtiene el Rol del usuario dado su ID 
        {
            string rol = "";

            var numemp = _admin.tblUser.Where(a => a.EmpleadoId == EmployeeID).FirstOrDefault();
            var rols = Roles.GetRolesForUser(numemp.UserName);

            var ptoSupRol = _sdmanager.GetRolByPuesto(rols);

            if (ptoSupRol.Contains("Supervisor")) { ViewBag.RoleNameUser = "Supervisor"; }
            else if (ptoSupRol.Contains("Tecnico")) { ViewBag.RoleNameUser = "Tecnico"; }

            if (ptoSupRol.Contains("Solicitante")) { rol = "Solicitante"; }
            else if (ptoSupRol.Contains("Supervisor")) { rol = "Supervisor"; }
            else if (ptoSupRol.Contains("Tecnico")) { rol = "Tecnico"; }        //-* usuario resolutor
            else if (ptoSupRol.Contains("ServiceDesk")) { rol = "ServiceDesk"; }    //-* usuario resolutor
            else if (ptoSupRol.Contains("Directivo")) { rol = "Directivo"; }
            else { }

            return rol;
        }
    }
}