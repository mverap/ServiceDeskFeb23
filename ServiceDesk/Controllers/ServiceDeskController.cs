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



namespace ServiceDesk.Controllers
{
    public class ServiceDeskController : Controller
    {
        private readonly RHAdminContext _rh = new RHAdminContext();
        private readonly ServiceDeskManager _mng = new ServiceDeskManager();
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private readonly DashBoardManager _mngDas = new DashBoardManager();
        private readonly SlaManager _sla = new SlaManager();
        private readonly NotificacionesManager _noti = new NotificacionesManager();
        private readonly DocumentController _doc = new DocumentController();
        //============================================================================================================================================
        public ActionResult Index(int type = 0)
        {

            var User = "";


            if (Session["EmpleadoNo"] != null)
            {

                User = Session["EmpleadoNo"].ToString();

            }
            else
            {

                return RedirectToAction("Login", "Home");
            }

            var _type = Request["type"];
            var _ticket = Request["ticket"];
            vmDashboard vm = new vmDashboard();
            List<ticket> lst = new List<ticket>();
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);
            if (type == 0 && _type == null)
            {
                type = 1;
            };
            if (type == 3) { type = 8; };
            if (type == 2) { type = 6; };
            List<tbl_TicketDetalle> lstTicket = new List<tbl_TicketDetalle>();
            if (type == 1)
            {
                lstTicket = _db.tbl_TicketDetalle.Where(x => x.EmpleadoID == 5505 &&
               (
                x.EstatusTicket == 1 || x.EstatusTicket == 2 || x.EstatusTicket == 5 ||
                x.EstatusTicket == 3 || x.EstatusTicket == 7 || x.EstatusTicket == 4)
                ).ToList();
            }
            else
            {
                lstTicket = _db.tbl_TicketDetalle.Where(x => x.EmpleadoID == 5505 && x.EstatusTicket == type).ToList();
            }

            lstTicket.ForEach(x => {
                var oCategoria = lstCategoria.Where(z => z.Id == x.Categoria).FirstOrDefault().Categoria;
                TimeSpan? timeSpan = (DateTime.Now - x.FechaRegistro);
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

                string horas_sla = x.FechaRegistro.ToString("HH:mm");
                ticket t = new ticket()
                {
                    categoria = oCategoria,
                    noTicket = x.Id,
                    GrupoResolutor = x.GrupoResolutor,
                    tiempoTranscurrido = horas_sla,
                    minutes = minutes < 10 ? "0" + minutes.ToString() : minutes.ToString(),
                    hours = hours < 10 ? "0" + hours.ToString() : hours.ToString(),
                    Estatus = x.Estatus
                };

                lst.Add(t);
            });
            vm.Tickets = lst;
            vm.type = _type ?? "1";
            return View(vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public PartialViewResult Getdetail(int ticket)
        {
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);
            var lstCentro = _db.cat_Centro.Where(x => x.Activo == true);
            var lstSubcategoria = _db.cat_SubCategoria.Where(x => x.Activo == true);

            var oTicket = _db.tbl_TicketDetalle.Where(x => x.Id == ticket).FirstOrDefault();

            //Validar: sí el tiempo de garantia venció, pasar a Cerrado.

            detailTicket o = new detailTicket() { ticket = oTicket };

            if (Session["EmpleadoNo"] != null)
            {

                o.EmployeeidBO = Session["EmpleadoNo"].ToString();

            }

            //DANIEL FUENTES
            var info = _db.his_Ticket.Where(a => a.IdTicket == ticket).OrderByDescending(a => a.FechaRegistro).ToList();
            o.Slas = _sla.GetSlaTimes(info);

            o.his = info;

            //o.his = info.OrderBy(x => x.FechaRegistro).ToList();

            //JOSUE
            o.Categoria = lstCategoria.Where(x => x.Id == oTicket.Categoria).FirstOrDefault().Categoria;
            o.Centro = lstCentro.Where(x => x.Id == oTicket.Centro).FirstOrDefault().Centro;
            o.Subcategoria = lstSubcategoria.Where(x => x.Id == oTicket.SubCategoria).FirstOrDefault().SubCategoria;


            //AYB - Archivos Adjuntos
            var dtoDw = _db.tblDocumentos.Where(a => a.IdTicket == ticket).OrderByDescending(a => a.FechaRegisto).ToList();
            o.Docs = dtoDw;
            //AYB - Archivos Adjuntos

            //AYB view detalle
            var det = _db.VWDetalleTicket.Where(a => a.Id == ticket).FirstOrDefault();
            o.detalle = det;
            //AYB view detalle

            var his2 = _db.his_Ticket.Where(a => a.IdTicket == ticket).OrderByDescending(a => a.FechaRegistro).FirstOrDefault();

            o.detalle.GrupoResolutor = his2.GrupoResolutor;

            return PartialView("../DashBoard/PartialViews/_DetailTicket", o);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
     
        public ActionResult Dashboard(int EmployeeId, int type = 0)
        {
            ViewBag.user = EmployeeId;
            // var User = id;
            var edoTicket = _db.cat_EstadoTicket.Where(x => x.Activo == true).ToList();
            var tickets = _db.tbl_TicketDetalle.ToList();

            List<int> ticketsVinculados = _db.tbl_VinculacionDetalle.Where(x => x.Activo == true).Select(x => x.IdTicketChild).ToList();
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

            var data = tickets.GroupBy(info => info.Estatus)
            .Select(group => new
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
            edoTicket.Select(x => x.Estado).ToList().ForEach(x => {
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
        public PartialViewResult getTicketById(string ticket)
        {
            int oTicket = int.Parse(ticket);


            vmDashbordResolutor vm = new vmDashbordResolutor();
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);
            var details = _db.tbl_TicketDetalle.Where(x => x.Id == oTicket).ToList();
            if (details.Count == 0)
            {
                return PartialView("../ServiceDesk/PartialViews/_TicketResolutor", vm);
            }
            var p = details.FirstOrDefault().Estatus;
            details.ForEach(x => {
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
            //vm.lstResumenResolutor = 
            return PartialView("../ServiceDesk/PartialViews/_TicketResolutor", vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public PartialViewResult GetAllTickets(string type, int idFiltro = 0)
        {
            // Esta clase toma todos los tickets que tengan Estatus type, y los llama item
            // y añade a una lista temporal listTemp:
            // -Los hijos de cada Item
            // -Cada Item si es Padre   ///Esto debe ser el que esta roto, por que si quito el segundo foreach ya no aparecen padres, solo hijos
            // -Cada Item si no es padre ni es hijo
            // Después toma todos los 
            vmDashbordResolutor vm = new vmDashbordResolutor(); //vista en la que se mostraran los tickets al final
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>(); //lista que se pasara a la vista vm
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true); //lista llena de todos los tickets activos?
            //lista llena de tickets y sus detalles si estos tickets tienen el Estatus type
            var details = _db.tbl_TicketDetalle.Where(x => x.Estatus == type).ToList();

            //lista de ids de tickets (int) sacados de la tabla VinculacionDetalle, columna idTicketChild, solo tickets activos
            //en otrsa palabras: ticketsVinculados = lista de tickets que son hijos
            List<int> ticketsVinculados = _db.tbl_VinculacionDetalle.Where(x => x.Activo == true).Select(x => x.IdTicketChild).ToList();
            List<tbl_TicketDetalle> lstTemp = new List<tbl_TicketDetalle>(); //lista temporal
            foreach (var item in details)
            {
                item.isPadre = false;//ticket item ahora no es padre
                tbl_TicketDetalle o = new tbl_TicketDetalle() { };//lista temporal o
                //traer lista de tickets que son subtickets del ticket item
                var lstSubTicket = _db.vwDetalleSubticket.Where(x => x.Id == item.Id).ToList(); //--------------------
                                                                                                //si la lista de tickets que son subtickets de item existe entonces: 
                                                                                                //Añadir hijos de ITEM a la lista temporal
                if (lstSubTicket.Count() > 0)
                {
                    //por cada ticket subticket de item
                    lstSubTicket.ForEach(x =>
                    {
                        //analizar la prioridad del ticket x     (ticket x es subticket de ticketPadre item)
                        int _idPrioridad = 0;
                        if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                        else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                        else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                        // opCategoria = la categoría del ticketPadre item
                        var opCategoria = lstCategoria.Where(c => c.Id == item.Categoria).FirstOrDefault().Categoria;
                        //crear un objeto con los siguientes datos del ticketHijo x
                        ticketResumenResolutor obj = new ticketResumenResolutor()
                        {
                            categoria = opCategoria,
                            prioridad = x.Prioridad,
                            //tickedID = x.IdTicket,
                            tickedID = x.Id,
                            checkVincular = false,
                            totVinculados = 0,
                            estatus = x.Estatus,
                            isPadre = true,
                            idTicketPadre = x.IdTicket,
                            isSubTicket = true,
                            idSubTicket = x.Id,
                            idPrioridad = _idPrioridad
                        };
                        //añadir ticketHijo x a lista Resumen
                        lstTicketsResumen.Add(obj);
                    });
                }

                //Verificar si el Item esta en la lista de padres e hijos, luego añadirlo a la lista temporal
                //Item será añadido a la lista temporal solo si "es padre" o si "no es ni padre ni hijo"
                //Si Item NO esta en la lista de padres e hijos: añadir ticket Item a lista temporal
                if (!ticketsVinculados.Contains(item.Id))
                {
                    o = item;
                    lstTemp.Add(o);
                }
                else //Si Item esta es la lista de padres e hijos
                {
                    //la vinculación donde el padre sea Item: guardala en e
                    var e = _db.tbl_Vinculacion.Where(x => x.IdTicket == item.Id).FirstOrDefault();
                    //siempre y cuando dicha fila exista:  /  Si Item es Padre
                    if (e != null)
                    {
                        o = item; //guardar en un o un temporal de Item
                                  //cambiar el total de tickets vinculados a Item: no entiendo como tho
                        o.totVinculados = _db.tbl_VinculacionDetalle.Where(x => x.IdVinculacion == e.IdVinculacion).Count() - 1;
                        o.isPadre = true; //Item es considerado padre ahora
                        lstTemp.Add(o);
                    }
                }
            };
            //si la lista temporal existe, eliminar lista details y llenarla con la lista temporal
            //  lista temporal: tickets que son padres o hijos ++++ error 
            if (lstTemp.Count > 0) { details = lstTemp; }

            //por cada ticket en details
            details.ForEach(x =>
            {
                int _idPrioridad = 0;
                if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                // opcategoria = categoria del hijo
                var opCategoria = lstCategoria.Where(c => c.Id == x.Categoria).FirstOrDefault().Categoria;
                //objeto hijo temporal para añadirlo
                ticketResumenResolutor o = new ticketResumenResolutor()
                {
                    categoria = opCategoria,
                    prioridad = x.Prioridad,
                    tickedID = x.Id,
                    checkVincular = false,
                    //totVinculados = x.totVinculados ?? 0, //linea comentada durante depuración, volver a poner
                    estatus = x.Estatus,
                    isPadre = x.isPadre,
                    idPrioridad = _idPrioridad
                };
                if (x.totVinculados == null)
                { //si no tiene tickets vinculados entonces agregar a lista.. If creado durante depuración, quitar y dejar contenido sin if
                }
                ////añadir hijos
                //lstTicketsResumen.Add(o);

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

            //vm.lstResumenResolutor = 
            return PartialView("../DashBoard/PartialViews/_TicketResolutor", vm);
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public JsonResult VincularTicket(int ticketPadre, int[] lstTIcket)
        {

            tbl_Vinculacion padre = new tbl_Vinculacion()
            {
                Activo = true,
                FReg = DateTime.Now,
                IdTicket = ticketPadre
            };

            _db.tbl_Vinculacion.Add(padre);
            _db.SaveChanges();


            foreach (var item in lstTIcket)
            {
                tbl_VinculacionDetalle hijo = new tbl_VinculacionDetalle()
                {
                    Activo = true,
                    FReg = DateTime.Now,
                    IdTicketChild = item,
                    IdVinculacion = padre.IdVinculacion,
                    TicketPrincipal = ticketPadre
                };
                _db.tbl_VinculacionDetalle.Add(hijo);
                _db.SaveChanges();


                ////Agregar relación de Líder - Tickets Vinculados - AYB
                //rel_TicketsVinculados vinc = new rel_TicketsVinculados()
                //{
                //    TicketPrincipal = ticketPadre,
                //    TicketVinculado = item,

                //};
                //_db.rel_TicketsVinculados.Add(vinc);
                //_db.SaveChanges();




            }


            return Json("OK", JsonRequestBehavior.AllowGet);
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public PartialViewResult dtaVincular(int[] list)
        {
            // int empledoId = int.Parse(user);
            vmDashbordResolutor vm = new vmDashbordResolutor();
            vm.totTicketsVinculados = list.Length;
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);
            var details = _db.tbl_TicketDetalle.ToList();
            details.ForEach(x => {
                if (list.Contains(x.Id))
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
                }
            });


            vm.lstResumenResolutor = lstTicketsResumen;

            return PartialView("../DashBoard/PartialViews/_Vincular", vm);
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        

        //============================================================================================================================================
        public ActionResult DetalleTicket(int EmployeeId, int? IdTicket, string folio, int isChild = 0)
        {

            var Employeeid = "";
            ViewBag.user = EmployeeId;

            if (Session["EmpleadoNo"] != null)
            {

                Employeeid = Session["EmpleadoNo"].ToString();

            }
            else
            {

                return RedirectToAction("Login", "Home");
            }
            ViewBag.user = EmployeeId;
            ViewBag.idChild = 0;

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

                        var TimeNow = DateTime.Now;
                        var dias = (TimeNow - AsignacionInfo.FechaRegistro).Days;
                        var diferencia = (TimeNow - AsignacionInfo.FechaRegistro).Hours;
                        var horas = (dias * 24) + diferencia;

                        

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
                    switch (detalle.detalle.EstatusTicket)
                    {
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
                    ViewBag.DX = new SelectList(_db.catDiagnosticos, "Diagnostico", "Diagnostico");

                    //===========
                    ViewBag.GrupoResolutorCat = new SelectList(_db.catGrupoResolutor.Where(x => x.Activo), "Id", "Grupo");
                    ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
                    ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
                    ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.UserLst = new SelectList(_db.tbl_User.Where(x => x.Activo & x.GrupoResolutor == AsignacionInfo.GrupoResolutor), "Id", "NombreTecnico");

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
        public JsonResult RecategorizarTicket(int TicketId, int Categoria, int Subcategoria)
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
                //De ser así, el ticket pasa como Abierto y el tecnico vacio.

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
            //--------------------- add here?
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
        public JsonResult ApruebaAsignacion(int TicketId, string Tecnico)
        {
            var result = "";
            var NoReasignacion = 1;

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            if (info != null)
            {
                //Validar si tuvo reaperturas
                if (info.NoReapertura == 1)
                {

                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignadoReag = Tecnico;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.ApruebaReasignacion = 2;  // 2 = Validada
                    _db.SaveChanges();

                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);

                    result = "Correcto";

                }
                else if (info.NoReapertura == 2)
                {

                    _db.tbl_TicketDetalle.Attach(info);
                    info.TecnicoAsignadoReag2 = Tecnico;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.ApruebaReasignacion = 2;  // 2 = Validada
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
                    info.TecnicoAsignado = Tecnico;
                    info.NoReasignaciones = info.NoReasignaciones + NoReasignacion;
                    info.ApruebaReasignacion = 2;  // 2 = Validada
                    _db.SaveChanges();

                    //Guardar Historico
                    _mng.SaveHistoricoUser(info);

                    result = "Correcto";

                }


            }


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
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        //Guarda Subticket
        [HttpPost]
        public ActionResult SetSubticket(DetalleSelectedTicketVm vm, HttpPostedFileBase upload)
        {
            var datos = new tbl_TicketDetalle();

            //Buscar la información del ticket principal
            var padre = _db.tbl_TicketDetalle.Where(a => a.Id == vm.subticket.IdTicket).FirstOrDefault();
            var TenicoCreadorDeSubticket = Int32.Parse(vm.EmployeeIdBO);
            var user = _db.tbl_User.Where(t => t.EmpleadoID == TenicoCreadorDeSubticket).FirstOrDefault();

            if (false)
            {
                //using (var con = new ServiceDeskContext())
                //{
                //    datos.IdTicketPrincipal = vm.subticket.IdTicket;
                //    datos.EmpleadoID = padre.EmpleadoID;
                //    datos.TicketTercero = padre.TicketTercero;
                //    datos.Extencion = padre.Extencion;
                //    datos.NombreTercero = padre.NombreTercero;
                //    datos.Piso = padre.Piso;
                //    datos.EmailTercero = padre.EmailTercero;
                //    datos.ExtensionTercero = padre.ExtensionTercero;
                //    datos.Posicion = padre.Posicion;
                //    datos.NombreCompleto = padre.NombreCompleto;
                //    datos.Area = padre.Area;
                //    datos.PersonasAddNotificar = padre.PersonasAddNotificar;
                //    datos.Correo = padre.Correo;
                //    datos.Comentarios = padre.Comentarios;
                //    datos.TecnicoAsignado = padre.TecnicoAsignado;
                //    datos.NoReasignaciones = padre.NoReasignaciones;
                //    datos.MotivoReasignacion = padre.MotivoReasignacion;
                //    datos.ApruebaReasignacion = padre.ApruebaReasignacion;
                //    datos.MotivoCambioEstatus = padre.MotivoCambioEstatus;
                //    datos.Diagnostico = padre.Diagnostico;
                //    datos.Categoria = Convert.ToInt32(vm.subticket.Categoria);
                //    datos.SubCategoria = Convert.ToInt32(vm.subticket.Subcategoria);
                //    datos.Centro = Convert.ToInt32(vm.subticket.Centro);
                //    datos.GrupoResolutor = vm.subticket.GrupoResolutor;
                //    datos.Prioridad = vm.subticket.Prioridad;
                //    datos.DescripcionIncidencia = vm.subticket.DescIncidencia;
                //    datos.NoReapertura = padre.NoReapertura;
                //    datos.Estatus = "Abierto";
                //    datos.EstatusTicket = 1;
                //    datos.FechaRegistro = DateTime.Now;
                //    con.tbl_TicketDetalle.Add(datos);
                //    con.SaveChanges();
                //}
            }
            else {

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
                    var fname = @"\\f\uFiles\ServiceDeskV2\\" + NameCarga; // ???
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

                    return RedirectToAction("DetalleTicket", "Supervisor", new { IdTicket = datos.IdTicketPrincipal, folio = datos.Id });
                }
                else
                {
                    ModelState.AddModelError("File", "Formato no soportado");
                    return PartialView();

                }

            }


            return RedirectToAction("DetalleTicket", "Supervisor", new { IdTicket = datos.IdTicketPrincipal, folio = datos.Id });
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public ActionResult Gestiones(string User)
        {

            ViewBag.user = string.IsNullOrEmpty(User) ? "" : User;

            var det = new DetalleGestiones();

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
            var lstUser = _db.vwDetalleUsuario.Where(a => a.Activo == true).ToList();
            var lstNivel = _db.catNivelExperiencia.Where(a => a.Activo == true).ToList();
            var lstDX = _db.vwDetalleDiagnostico.Where(a => a.Activo == true).ToList();
            var lstCat = _db.vwDetalleCategoria.Where(a => a.Activo == true).ToList();
            var lstSubCat = _db.vwDetalleSubcategorias.Where(a => a.Activo == true).ToList();
            //

            det.diagLst = lstDX;
            det.detCategoria = lstCat;
            det.detSubcat = lstSubCat;
            det.userLst = lstUser;
            det.NivExpe = lstNivel;

            return View(det);
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

                    var his2 = _db.his_Ticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).FirstOrDefault();

                    detalle.detalle.GrupoResolutor = his2.GrupoResolutor;

                    ViewBag.EstadoTicket = new SelectList(_db.cat_EstadoTicket.Where(x => x.Activo), "Id", "Estado");
                    ViewBag.DX = new SelectList(_db.catDiagnosticos, "Diagnostico", "Diagnostico");

                    //===========
                    ViewBag.Categoria = new SelectList(_db.cat_Categoria.Where(x => x.Activo), "Id", "Categoria");
                    ViewBag.SubCategoria = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.Centro = new SelectList(_db.cat_Centro.Where(x => x.Activo), "Id", "Centro");
                    ViewBag.Matriz = new SelectList(string.Empty, "Value", "Text");
                    ViewBag.UserLst = new SelectList(_db.tbl_User.Where(x => x.Activo && x.GrupoResolutor == AsignacionInfo.GrupoResolutor), "NombreTecnico", "NombreTecnico");

                    var lstSub = _db.vwDetalleSubticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();


                    detalle.Slas = _sla.GetSlaTimes(info);

                }

                return View(detalle);
            }


            return View();

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        //DETALLE DEL SUBTICKET
        public ActionResult DetalleSubticket(int? IdTicket)
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
                    var lstSub = _db.vwDetalleSubticket.Where(a => a.IdTicket == IdTicket).OrderByDescending(a => a.FechaRegistro).ToList();

                    //
                    if (AsignacionInfo.EstatusTicket == 5)//GARANTÍA
                    {

                        var TimeNow = DateTime.Now;
                        var dias = (TimeNow - AsignacionInfo.FechaRegistro).Days;
                        var diferencia = (TimeNow - AsignacionInfo.FechaRegistro).Hours;
                        var horas = (dias * 24) + diferencia;

                       

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

                        ViewBag.HorasEspera = horas;

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
        // - - - - - - - - -
        [HttpPost]
        public JsonResult GetMatriz(int Id)
        {

            var mat = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == Id).ToList();

            return Json(mat, JsonRequestBehavior.AllowGet);

        }
        //=======================================================CATALOGOS============================================================================







        // DELETE BELOW AFTER ENTIRETY OF  REPORTES COMPONENT IS FUNCTIONAL

        /***********Codigo MVP Pie Chart********/
        public ActionResult Reportes(int EmployeeID)
        {
            ViewBag.user = EmployeeID;

            return View();
        }
        public ActionResult PieChartPrioridad(DateTime? fechaInicial, DateTime? fechaFinal)
        {
            //tickets = detalle de tickets relacionados al strGrupoResolutor
            var tickets = _db.VWDetalleTicket.ToList();

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
        public ActionResult PieChartCentro(DateTime? fechaInicial, DateTime? fechaFinal)
        {
            var tickets = _db.VWDetalleTicket.ToList();

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
        public ActionResult PieChartEstatus(DateTime? fechaInicial, DateTime? fechaFinal)
        {
            var tickets = _db.VWDetalleTicket.ToList();

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
        public ActionResult PieChartTipo(DateTime? fechaInicial, DateTime? fechaFinal)
        {
            //var tickets = _db.tbl_TicketDetalle.Select(x => x.SubCategoria).ToList();
            var categorias = _db.cat_MatrizCategoria.Where(a => _db.tbl_TicketDetalle.Any(b => b.SubCategoria == a.IDSubCategoria));            //instanciar, datos iniciales irrelevantes
            var tickets = _db.tbl_TicketDetalle.ToList();
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
        public ActionResult PieChartExpertiz(DateTime? fechaInicial, DateTime? fechaFinal)
        {
            //var tickets = _db.tbl_TicketDetalle.Select(x => x.SubCategoria).ToList();
            var categorias = _db.cat_MatrizCategoria.Where(a => _db.tbl_TicketDetalle.Any(b => b.SubCategoria == a.IDSubCategoria));            //instanciar, datos iniciales irrelevantes
            var tickets = _db.tbl_TicketDetalle.ToList();
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
        public ActionResult PieChartSLA(DateTime? fechaInicial, DateTime? fechaFinal)
        {
            var tickets = _db.tbl_TicketDetalle.ToList();
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
        public ActionResult PieChartResolutor(DateTime? fechaInicial, DateTime? fechaFinal)
        {
            //tickets = detalle de tickets relacionados al strGrupoResolutor
            var tickets = _db.VWDetalleTicket.ToList();

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
            var data = tickets.GroupBy(info => info.GrupoResolutor).Select(group => new
            {
                Resolutor = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.Resolutor);
            DatosReportes datos = null;
            String[] strResolutor = new string[data.Count()];
            int[] intCount = new int[data.Count()];
            int i = 0;
            foreach (var item in data)
            {
                strResolutor[i] = item.Resolutor;
                intCount[i] = item.Count;
                i++;
            }

            datos = new DatosReportes
            {
                Column = strResolutor,
                Count = intCount

            };

            if (datos != null)
            {
                return View(datos);
            }
            else
                return View();
        }
        /***********FIN CODIGO Pie Chart MVP********/

        /***********Codigo MVP Grid********/
        //                                                 
        [HttpGet]
        public ViewResult GridTickets(int EmployeeId)
        {
            ViewBag.user = EmployeeId;
            var detalle = new DetalleSelectedTicketVm();
            //String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeId).Select(x => x.GrupoResolutor).SingleOrDefault();
            var dtoViewDetalle = _db.VWDetalleTicket.OrderByDescending(pointer => pointer.Id).ToList();
            detalle.ViewDetalleTickets = dtoViewDetalle;
            //Eliminar las horas antes de enviar los datos al grid 
            foreach (var ticket in detalle.ViewDetalleTickets)
            {
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

    }
}
