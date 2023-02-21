//antiguo texto
using ServiceDesk.Managers;
using ServiceDesk.Models; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using ServiceDesk.ViewModels;
using System.Data.Entity.Migrations;
//


namespace ServiceDesk.Controllers
{
    public class DashBoardController : Controller
    {
        // GET: DashBoard
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private DashBoardManager _mng = new DashBoardManager();
        private readonly SlaManager _sla = new SlaManager();
        private readonly AdminContext _admin = new AdminContext();
        private readonly ServiceDeskManager _sdmanager = new ServiceDeskManager();
        private readonly NotificacionesManager _noti = new NotificacionesManager();
        //============================================================================================================================================
        public ActionResult Index(int EmployeeId,int type = 0)
        {
            // copiar y pegar en cualquier actionresult que requiere mandar usuario por un tubo si se intenta pasar de listo
            var userSession = Int32.Parse(Session["EmpleadoNo"].ToString()); if (userSession != EmployeeId) { return RedirectToAction("Error", "Document"); }

            ViewBag.user = EmployeeId;
            ViewBag.Rol = RoldeUsuario(EmployeeId);
            var usuario = EmployeeId;
            var _type = Request["type"];
            var _ticket = Request["ticket"];

            vmDashboard vm = new vmDashboard();
            List<ticket> lst = _db.Database.SqlQuery<ticket>("EXEC dbo.GET_TICKETS_BY_EMPLOYEE_ID @EmpleadoId={0},@EstatusId={1}", 
                usuario,type).ToList();
            List<ticket> lstOutput = new List<ticket>();
            foreach (var ls in lst)
            {
                //string horasString = ls.hours;
                //if (horasString == "*") { }
                //var horas = Int32.Parse(ls.hours);
                //var minutos = Int32.Parse(ls.minutes);
                //ls.tiempoTranscurrido = horas.ToString("00") + ":" + minutos.ToString("00");
                ;
                //ls.tiempoTranscurrido = ls.hours + ":" + ls.minutes;
                int? ticketprincipal = _db.tbl_TicketDetalle.Where(t => t.Id == ls.noTicket).Select(t => t.IdTicketPrincipal).FirstOrDefault();
                if (ticketprincipal != null)
                {
                    //lstOutput.Add(ls); 
                    ls.subticket = false;
                }
                else { ls.subticket = true; }
            }
            //lst = lstOutput;

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

            //Poner aquí validación de la Encuesta de Satisfacción
            if (oTicket.EstatusTicket == 6) //Cerrado
            {

                var infoEnc = _db.EncuestaDetalle.Where(a => a.IdTicket == ticket).FirstOrDefault();

                if (infoEnc == null) //No contesto encuesta
                {
                    ViewBag.ContestoEncuesta = "NO";

                }


            }

            //Validar: sí el tiempo de garantia venció, pasar a Cerrado.
            if (oTicket.EstatusTicket == 5)//GARANTÍA
            {

                var TimeNow = DateTime.Now;
                var dias = (TimeNow - oTicket.FechaRegistro).Days;
                var diferencia = (TimeNow - oTicket.FechaRegistro).Hours;
                var horas = (dias * 24) + diferencia;

                ViewBag.HoraGarantia = horas;

                var gpo = _db.cat_MatrizCategoria.Where(a => a.IDSubCategoria == oTicket.SubCategoria).FirstOrDefault();

                if (horas >= Convert.ToDouble(gpo.Garantia))
                {

                    _db.tbl_TicketDetalle.Attach(oTicket);
                    oTicket.Estatus = "Cerrado";
                    oTicket.EstatusTicket = 6;
                    oTicket.FechaRegistro = DateTime.Now;
                    _db.SaveChanges();

                    //Guardar historico
                    _sdmanager.SetHistoricoCambioEstatus(oTicket.Id);

                }

            }

            detailTicket o = new detailTicket() { ticket = oTicket };

            if (Session["EmpleadoNo"] != null)
            {

                o.EmployeeidBO = Session["EmpleadoNo"].ToString();

            }

            //DANIEL FUENTES
            var info = _db.his_Ticket.Where(a => a.IdTicket == ticket).OrderByDescending(a => a.FechaRegistro).ToList();
            o.Slas = _sla.GetSlaTimes(info);
            // Formato de garantia en cuenta regresiva .iv
            var slagarantia = o.Slas.Where(t => t.Type == "En Garantia").FirstOrDefault();
            if (slagarantia != null)
            {
                var garantia = slagarantia.Time;
                if (garantia != null) { 
                    var garantiaTotal = lstSubcategoria.Where(x => x.Id == oTicket.SubCategoria).FirstOrDefault().Periodo;
                    var TiempoPasadoEnGarantia = 0; var hrSLAgar = 0; var minSLAgar = 0; var slagar = 0;
                    var TiempoEstimadoGarantia = Int32.Parse(garantiaTotal);
                    try 
                    { // if both are singular number format
                        TiempoPasadoEnGarantia = Int32.Parse(garantia);
                        //if (TiempoEstimadoGarantia < 10) { } else { }
                        garantia = (TiempoEstimadoGarantia - TiempoPasadoEnGarantia).ToString() + ":00";
                    } 
                    catch
                    {// if garantia is in clock format
                        TiempoEstimadoGarantia = TiempoEstimadoGarantia * 60; // TiempoEstimadoGarantia a minutos

                        hrSLAgar = Int32.Parse(garantia.Split(':')[0]);     // obtener x de XX:YY
                        minSLAgar = Int32.Parse(garantia.Split(':')[1]);    // obtener y de XX:YY
                        slagar = (hrSLAgar * 60) + minSLAgar;               // garantia en minutos

                        int total = TiempoEstimadoGarantia - slagar;        // cuenta regresiva de garantia en minutos

                        hrSLAgar = total / 60;
                        minSLAgar = total % 60;

                        garantia = (minSLAgar < 10) ? hrSLAgar + ":0" + minSLAgar : hrSLAgar + ":" + minSLAgar;// cuenta regresiva en clock format
                    }
                    o.Slas.Where(t => t.Type == "En Garantia").FirstOrDefault().Time = garantia;
                }
            }

            o.his = info;

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
        public PartialViewResult GetModal(string type)
        {

            string _title = "";
            string view = "../DashBoard/PartialViews/";
            switch (type)
            {
                case "1":
                    _title = " Motivo de cancelación del ticket";
                    view += "_CancelarTicket";
                    break;
                case "4":
                    _title = "";
                    view += "_ResueltoTicket";
                    break;
                case "5":
                    _title = "Motivo de reapertura de ticket";
                    view += "_GarantiaTicket";
                    break;
            }
            ViewData["title"] = _title;
            return PartialView(view);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        [HttpPost]
        public JsonResult putEstatus(int Type = 0, int IdTicket = 0, string Motivo = "")
        {
            string result = "";
            try
            {
                switch (Type)
                {
                    case 1:
                        Type = 8;
                      result =  _mng.putEstatus(Type, IdTicket, Motivo);
                    break;
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("ERROR:" + ex, JsonRequestBehavior.AllowGet);
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public ActionResult Resolutor(int EmployeeId,int type = 0) // Mostrar conteo de tickets
        {
            vmDashbordResolutor vm = new vmDashbordResolutor();

            //crear el filtro
            List<SelectListItem> filtro = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Del más antiguo al más reciente" },
                new SelectListItem { Value = "2", Text = "Del más reciente al más antiguo" },
                new SelectListItem { Value = "3", Text = "Por prioridad" }
            };
            ViewBag.filtro = filtro;            

            // copiar y pegar en cualquier actionresult que requiere mandar usuario por un tubo si se intenta pasar de listo
            var userSession = Int32.Parse(Session["EmpleadoNo"].ToString()); if (userSession != EmployeeId) { return RedirectToAction("Error", "Document"); }

            ViewBag.user = EmployeeId;
            string rol = RoldeUsuario(EmployeeId); ViewBag.Rol = rol;
            var userid = EmployeeId;
            var edoTicket = _db.cat_EstadoTicket.Where(x => x.Activo == true).ToList();

            //SE FILTRAN POR GRUPO RESOLUTOR
            var grupInfo = _db.tbl_User.Where(a => a.EmpleadoID == EmployeeId).FirstOrDefault();
            //var tickets = _db.tbl_TicketDetalle.Where(x => x.GrupoResolutor == grupInfo.GrupoResolutor).ToList();//----- original
            if (grupInfo == null) { return RedirectToAction("UsuarioNoExiste", "Document"); }
            ViewBag.grupo = grupInfo.GrupoResolutor;
            ;
            var user = _db.tbl_User.Where(t => t.EmpleadoID == EmployeeId).FirstOrDefault(); // necesario para el filtro de centros
            int idTecnico = user.Id;
            var tickets = new List<tbl_TicketDetalle>();
            int[] Centros = new int[99];


            //LLENAR CONTADOR DE TICKETS   _VERSION PREVIA START-------------------------------------------------------------- -
            if (ViewBag.Rol == "Tecnico")
            {
                // Filtrar por centro if TI-Soporte _ISF
                // verificar que usuario no esté en lista de usuarios excluidos del filtro
                var Usuarios_Excluidos_Filtro_Centro = _db.tbl_User_TI_Exclusion_Filtro_Centro.Select(t => t.EmployeeId).ToList();
                if (grupInfo.GrupoResolutor == "TI-Soporte" && !Usuarios_Excluidos_Filtro_Centro.Contains(EmployeeId))
                {
                    // Llenado de tickets CON filtro
                    tickets = _db.tbl_TicketDetalle.Where(t => t.GrupoResolutor == grupInfo.GrupoResolutor && t.Centro == grupInfo.Centro &&
                                (t.IdTecnicoAsignado == idTecnico && t.IdTecnicoAsignadoReag == null && t.IdTecnicoAsignadoReag2 == null) ||
                                (t.IdTecnicoAsignadoReag == idTecnico && t.IdTecnicoAsignadoReag2 == null) ||
                                (t.IdTecnicoAsignadoReag2 == idTecnico)
                            ).ToList();
                }
                else
                {
                    tickets = _db.tbl_TicketDetalle.Where(t => t.GrupoResolutor == grupInfo.GrupoResolutor &&
                            (t.IdTecnicoAsignado == idTecnico && t.IdTecnicoAsignadoReag == null && t.IdTecnicoAsignadoReag2 == null) ||
                            (t.IdTecnicoAsignadoReag == idTecnico && t.IdTecnicoAsignadoReag2 == null) ||
                            (t.IdTecnicoAsignadoReag2 == idTecnico)
                        ).ToList();
                    ;
                }
            }
            else
            {
                if (grupInfo.GrupoResolutor == "TI-Soporte")
                {
                    // Filtrar por centro if TI-Soporte _ISF
                    Centros = _db.tbl_rel_SupervisorCentros.Where(t => t.UserId == idTecnico).Select(t => t.CentroId).ToArray(); // lista de centros
                    Centros = Centros.Concat(new int[] { user.Centro }).ToArray(); // añadir centro al que pertenece en caso de que no fuera agregado antes
                    tickets = _db.tbl_TicketDetalle.Where(x => x.GrupoResolutor == grupInfo.GrupoResolutor && Centros.Contains(x.Centro)).ToList(); // añadir tickets del mismo grupo resolutor en los centros listados


                }
                else
                {
                    tickets = _db.tbl_TicketDetalle.Where(x => x.GrupoResolutor == grupInfo.GrupoResolutor).ToList();
                    ;
                }

                //if (grupInfo.GrupoResolutor == "TI-Soporte")
                //    tickets = _db.tbl_TicketDetalle.Where(x => x.GrupoResolutor == grupInfo.GrupoResolutor && x.Centro == grupInfo.Centro).ToList();                
                //else
                // tickets = _db.tbl_TicketDetalle.Where(x => x.GrupoResolutor == grupInfo.GrupoResolutor).ToList();
            }
            //==============================//


            //Valida el rol del usuario=====================
            var numemp = _admin.tblUser.Where(a => a.EmpleadoId == EmployeeId).FirstOrDefault();
            var rols = Roles.GetRolesForUser(numemp.UserName);

            var ptoSupRol = _sdmanager.GetRolByPuesto(rols);
            if (ptoSupRol.Contains("Supervisor")) { ViewBag.RoleNameUser = "Supervisor"; }
            else if (ptoSupRol.Contains("Tecnico")) { ViewBag.RoleNameUser = "Tecnico"; }

            //============================================== EXCLUIR HIJOS DEL CONTEO, para apagar exclusión comentar de aquí a "{ tickets = lstTemp; }"
            List<int> ticketsVinculados = _db.tbl_VinculacionDetalle.Where(x => x.Activo == true).Select(x => x.IdTicketChild).ToList(); // lista de hijos y padres
            List<tbl_TicketDetalle> lstTemp = new List<tbl_TicketDetalle>();
            foreach (var item in tickets)
            {
                var str = item.Estatus;
                tbl_TicketDetalle o = new tbl_TicketDetalle() { };

                if (!ticketsVinculados.Contains(item.Id)) // añadir tickets sin vinculaciones a TEMP
                {
                    o = item;
                    lstTemp.Add(o);
                }
                else
                {
                    var e = _db.tbl_Vinculacion.Where(x => x.IdTicket == item.Id).FirstOrDefault(); // añadir padres a TEMP
                    if (e != null)
                    {
                        o = item;
                        lstTemp.Add(o);
                    }
                }
            };
            if (lstTemp.Count > 0) { tickets = lstTemp; } // remplazar "tickets" con lista Tickets_Sin_Vinculación y Tickets_Padres

            var data = tickets.GroupBy(info => info.Estatus)
            .Select(group => new
            {
                EstatusTicket = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.EstatusTicket);
            //------------------------------------------------------


            if (ptoSupRol.Contains("Supervisor")) { vm.RolNameBO = "Supervisor"; }
            else if (ptoSupRol.Contains("Tecnico")) { vm.RolNameBO = "Tecnico"; }
            ;
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
            ;
            edoTicket.Select(x => x.Estado).ToList().ForEach(x =>
            {//---------------------
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

            var tareas = _db.tblTareasProgramadas.Where(x => x.TecnicoID == EmployeeId && x.Activado_2 == true).OrderBy(x => x.Estatus).ToList();
            int asignado = 0, abierto = 0, cerrado = 0, trabajando = 0, resuelto = 0, cancelado = 0, espera = 0;
            foreach (var tarea in tareas)
            {
                if (tarea.Estatus == "Asignado") { asignado++; }
                if (tarea.Estatus == "En Espera") { espera++; }
                if (tarea.Estatus == "Asignacion Pendiente") { abierto++; }
                if (tarea.Estatus == "Cerrado") { cerrado++; }
                if (tarea.Estatus == "Trabajando") { trabajando++; }
                if (tarea.Estatus == "Resuelto") { resuelto++; }
                if (tarea.Estatus == "Cancelado") { cancelado++; }
            }
            int ticketsAbiertos = 0;
            // Filtrar por centro if TI-Soporte _ISF
            if (grupInfo.GrupoResolutor == "TI-Soporte")
            {
                var Usuarios_Excluidos_Filtro_Centro = _db.tbl_User_TI_Exclusion_Filtro_Centro.Select(t => t.EmployeeId).ToList();
                if (ViewBag.Rol == "Tecnico")
                // los tecnicos solo pueden ver los tickets de su propip centro si no estan en la LISTA UEFC
                {
                    if (!Usuarios_Excluidos_Filtro_Centro.Contains(EmployeeId))
                    {
                        ticketsAbiertos = _db.tbl_TicketDetalle.Where(x =>
                            x.GrupoResolutor == grupInfo.GrupoResolutor &&
                            x.Centro == grupInfo.Centro &&
                            x.Estatus == "Abierto").Count();
                    }
                    else
                    {
                        ticketsAbiertos = _db.tbl_TicketDetalle.Where(x =>
                            x.GrupoResolutor == grupInfo.GrupoResolutor &&
                            x.Estatus == "Abierto").Count();
                    }

                }
                else // los supervisores pueden ver los de varios centros
                    ticketsAbiertos = _db.tbl_TicketDetalle.Where(x =>
                    x.GrupoResolutor == grupInfo.GrupoResolutor &&
                    x.Estatus == "Abierto" &&
                    Centros.Contains(x.Centro)).Count();
            }
            else
            {
                ticketsAbiertos = _db.tbl_TicketDetalle.Where(x =>
                x.GrupoResolutor == grupInfo.GrupoResolutor &&
                x.Estatus == "Abierto").Count();
            }
            foreach (var edo in vm.lstTicket)
            {
               // if (edo.estado == "Abierto") { edo.total += abierto; } //-- quitar cuando filtro esté listo
                if (edo.estado == "Abierto")
                {
                    ticketsAbiertos += abierto;
                    edo.total = ticketsAbiertos;
                }
                if (edo.estado == "Abierto") { edo.total += abierto; }
                if (edo.estado == "En Espera") { edo.total += espera; }
                if (edo.estado == "Asignado") { edo.total += asignado; }
                if (edo.estado == "Cerrado") { edo.total += cerrado; }
                if (edo.estado == "Trabajando") { edo.total += trabajando; }
                if (edo.estado == "Resuelto") { edo.total += resuelto; }
                if (edo.estado == "Cancelado") { edo.total += cancelado; }
            }

            return View(vm);
        }
        public ActionResult Resolutor1(int EmployeeId, int type = 0) // Mostrar conteo de tickets optimizado 
        {
            vmDashbordResolutor vm = new vmDashbordResolutor(); // view model for the page

            //crear el filtro 
            List<SelectListItem> filtro = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Del más antiguo al más reciente" },
                new SelectListItem { Value = "2", Text = "Del más reciente al más antiguo" },
                new SelectListItem { Value = "3", Text = "Por prioridad" }
            };

            // copiar y pegar en cualquier actionresult que requiere mandar usuario por un tubo si se intenta pasar de listo
            var userSession = Int32.Parse(Session["EmpleadoNo"].ToString()); if (userSession != EmployeeId) { return RedirectToAction("Error", "Document"); }
            string rol = RoldeUsuario(EmployeeId); ViewBag.Rol = rol;
            var grupInfo = _db.tbl_User.Where(a => a.EmpleadoID == EmployeeId).FirstOrDefault();
            if (grupInfo == null) { return RedirectToAction("UsuarioNoExiste", "Document"); }
            int[] Centros = new int[99];


            // LLENAR CONTADOR DE TICKETS   _VERSION OPTIMIZADA START
            string Query = "";
            if (rol == "Tecnico")
            {
                Query = "EXEC dbo.GetCountTicketsForTecnico @EmpleadoId=" + EmployeeId;

                // Checar filtro y agregarlo al query de ser necesario 
                var Usuarios_Excluidos_Filtro_Centro = _db.tbl_User_TI_Exclusion_Filtro_Centro.Select(t => t.EmployeeId).ToList();
                if (grupInfo.GrupoResolutor == "TI-Soporte" && !Usuarios_Excluidos_Filtro_Centro.Contains(EmployeeId)) 
                    Query += ", @Type=" + 1;
            }
            else // rol == supervisor
            {
                Query = "EXEC dbo.GetCountTicketsForSupervisor @EmpleadoId=" + EmployeeId;

                if (grupInfo.GrupoResolutor == "TI-Soporte")
                {
                    //Check filtro centros if supervisor has been added
                    var centrFilter = _db.tbl_rel_SupervisorCentros.Where(t => t.UserId == grupInfo.Id).FirstOrDefault();
                    if (centrFilter != null) { 
                        Query = "EXEC dbo.GetCountTicketsForSupervisorMultiCentro @EmpleadoId=" + EmployeeId;
                    }                    
                }
            }

            List<ticketByEstado> lst = New_List_Estados();                                      // create list full of 0's
            List<ticketByEstado> lst2 = _db.Database.SqlQuery<ticketByEstado>(Query).ToList();  // get actual values
            foreach (var edo in lst)                                                            // change the 0's for actual values
            {
                var edoObtenido = lst2.Where(t => t.orden == edo.orden).FirstOrDefault();
                if (edoObtenido != null) edo.total  = edoObtenido.total; else edo.total = 0;
            }
            vm.lstTicket = lst; // print 

            // ADD tareas TO THE COUNTER
            if (true) { 
                var tareas = _db.tblTareasProgramadas.Where(x => x.TecnicoID == EmployeeId && x.Activado_2 == true).OrderBy(x => x.Estatus).ToList();
                int asignado = 0, abierto = 0, cerrado = 0, trabajando = 0, resuelto = 0, cancelado = 0, espera = 0;
                foreach (var tarea in tareas)
                {
                    if (tarea.Estatus == "Asignado")                { asignado++; }
                    if (tarea.Estatus == "En Espera")               { espera++; }
                    if (tarea.Estatus == "Asignacion Pendiente")    { abierto++; }
                    if (tarea.Estatus == "Cerrado")                 { cerrado++; }
                    if (tarea.Estatus == "Trabajando")              { trabajando++; }
                    if (tarea.Estatus == "Resuelto")                { resuelto++; }
                    if (tarea.Estatus == "Cancelado")               { cancelado++; }
                }
                foreach (var edo in vm.lstTicket)
                {
                    //if (edo.estado == "Abierto") { edo.total += abierto; } //-- quitar cuando filtro esté listo
                    //if (edo.estado == "Abierto")
                    //{
                    //    ticketsAbiertos += abierto;
                    //    edo.total = ticketsAbiertos;
                    //}
                    if (edo.estado == "Abierto")    { edo.total += abierto; }
                    if (edo.estado == "En Espera")  { edo.total += espera; }
                    if (edo.estado == "Asignado")   { edo.total += asignado; }
                    if (edo.estado == "Cerrado")    { edo.total += cerrado; }
                    if (edo.estado == "Trabajando") { edo.total += trabajando; }
                    if (edo.estado == "Resuelto")   { edo.total += resuelto; }
                    if (edo.estado == "Cancelado")  { edo.total += cancelado; }
                }
            }

            ViewBag.filtro = filtro;
            ViewBag.user = EmployeeId;
            ViewBag.grupo = grupInfo.GrupoResolutor;
            return View(vm);
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public PartialViewResult getTicketTecnicoById(string ticket, string user ) 
        {
            int oTicket = int.Parse(ticket);      
            int empledoId = int.Parse(user);

            vmDashbordResolutor vm = new vmDashbordResolutor();
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);
            var details = _db.tbl_TicketDetalle.Where(x => x.Id == oTicket).ToList();
            if (details.Count == 0) {
                return PartialView("../DashBoard/PartialViews/_TicketTecnico", vm);
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
            return PartialView("../DashBoard/PartialViews/_TicketTecnico", vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public PartialViewResult getTicketById(string ticket, string user , string type="") 
        {
            int oTicket = int.Parse(ticket);      
            int empledoId = int.Parse(user);

            

            vmDashbordResolutor vm = new vmDashbordResolutor();
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);
            //var details = _db.tbl_TicketDetalle.Where(x => x.EmpleadoID == empledoId && x.Id == oTicket).ToList();
            var details = _db.tbl_TicketDetalle.Where(x =>  x.Id == oTicket).ToList();
            if (details.Count == 0) {

                if (type == "usuario")
                {
                    return PartialView("../DashBoard/PartialViews/_DetailTicket", vm);

                }
                else 
                {
                
                return PartialView("../DashBoard/PartialViews/_TicketResolutor", vm);
                }

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
            return PartialView("../DashBoard/PartialViews/_TicketResolutor", vm);


        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public PartialViewResult GetResolutorTickets(string user, string type, int idFiltro = 0)
        {


            int empledoId = int.Parse(user);
            string rol = RoldeUsuario(empledoId);
            ViewBag.rol = rol;
            vmDashbordResolutor vm = new vmDashbordResolutor();
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);

            //SE FILTRAN POR GRUPO RESOLUTOR//
            var userint = Convert.ToInt32(user);
            var grupInfo = _db.tbl_User.Where(a => a.EmpleadoID == userint).FirstOrDefault();
            var details = _db.tbl_TicketDetalle.Where(x => x.Estatus == type && x.GrupoResolutor == grupInfo.GrupoResolutor).ToList();
            ;
            if (grupInfo.GrupoResolutor == "TI-Soporte") {
                // Filtrar por centro if TI-Soporte _ISF            
                // El filtrado de centros es distinto de tecnicos a Supervisores
                // En la tabla tbl_User_TI_Exclusion_Filtro_Centro
                //              estan los Tecnicos a los que el filtro no se les debe aplicar
                // En la tabla tbl_rel_SupervisorCentros
                //              estan los supervisores y los centros que les toca supervisar  
                if (rol == "Tecnico") { 
                    // verificar que usuario no esté en lista de usuarios excluidos del filtro
                    var Usuarios_Excluidos_Filtro_Centro = _db.tbl_User_TI_Exclusion_Filtro_Centro.Select(t => t.EmployeeId).ToList(); 
                    if (! Usuarios_Excluidos_Filtro_Centro.Contains(empledoId)) details = details.Where(t => t.Centro == grupInfo.Centro).ToList();
                }
                else 
                if (rol == "Supervisor") { 
                    var Centros = _db.tbl_rel_SupervisorCentros.Where(t => t.UserId == grupInfo.Id).Select(t => t.CentroId).ToArray(); // lista de centros
                    // Si el supervisor no está en la tabla solo le toca supervisar su centro
                    // Si el supervisor está en la tabla, asegurarse que también le toque supervisar el suyo propio
                    if (Centros != null)
                    {
                        Centros = Centros.Concat(new int[] { grupInfo.Centro }).ToArray(); // añadir centro al que pertenece en caso de que no fuera agregado antes
                        details = details.Where(t => t.GrupoResolutor == "TI-Soporte" && Centros.Contains(t.Centro)).ToList();
                    }
                    else {
                        details = details.Where(t => t.GrupoResolutor == "TI-Soporte" && t.Centro == grupInfo.Centro).ToList();
                    }
                }
            }

            if (rol == "Tecnico" && type != "Abierto") // SOLO MOSTRAR TICKETS QUE LE CONCIERNEN AL TECNICO
            {
                int idTecnico = _db.tbl_User.Where(t => t.EmpleadoID == userint).Select(t => t.Id).FirstOrDefault();

                details = details.Where(t =>
                 (t.IdTecnicoAsignado == idTecnico && t.IdTecnicoAsignadoReag == null && t.IdTecnicoAsignadoReag2 == null) ||
                 (t.IdTecnicoAsignadoReag == idTecnico && t.IdTecnicoAsignadoReag2 == null) ||
                 (t.IdTecnicoAsignadoReag2 == idTecnico)
                 ).ToList();
            }

            //---- AÑADIR VINCULADOS Y SUBTICKETS: 
            List<int> ticketsVinculados = _db.tbl_VinculacionDetalle.Where(x => x.Activo == true).Select(x => x.IdTicketChild).ToList();
            var lstSubTickets = _db.vwDetalleSubticket.ToList();
            List<tbl_TicketDetalle> lstTemp = new List<tbl_TicketDetalle>();
            foreach (var item in details)
            {
                item.isPadre = false;
                tbl_TicketDetalle o = new tbl_TicketDetalle() { };
                var lstSubTicket = lstSubTickets.Where(x => x.Id == item.Id).ToList(); // previamente una llamada
                if (lstSubTicket.Count() > 0)
                {
                    lstSubTicket.ForEach(x =>
                    {
                        int _idPrioridad = 0;
                        if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                        else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                        else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                        var opCategoria = lstCategoria.Where(c => c.Id == item.Categoria).FirstOrDefault().Categoria;
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
                            idPrioridad = _idPrioridad,
                            orden = 2
                        };
                        lstTicketsResumen.Add(obj);
                        obj.isSubTicket = true;
                    });
                }
                if (!ticketsVinculados.Contains(item.Id))
                {
                    o = item; // añadir tickets que no están vinculados a otros (hijos)
                    o.orden = 1;
                    lstTemp.Add(o);

                }
                else
                {
                    var e = _db.tbl_Vinculacion.Where(x => x.IdTicket == item.Id).FirstOrDefault();
                    if (e != null)
                    {
                        o = item;
                        o.totVinculados = _db.tbl_VinculacionDetalle.Where(x => x.IdVinculacion == e.IdVinculacion).Count() - 1;
                        o.isPadre = true;
                        o.orden = 1;
                        lstTemp.Add(o); //añadir los padres
                    }
                }
            };
            if (lstTemp.Count > 0) { details = lstTemp; }
            //---- AÑADIR VINCULADOS Y SUBTICKETS: END


            ;
            var lstSubTicket2 = _db.vwDetalleSubticket.Select(t => t.Id).ToList();
            details.ForEach(x => {
                int VARIABLE_De_Prueba = x.Id;
                int _idPrioridad = 0;
                if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                var opCategoria = lstCategoria.Where(c => c.Id == x.Categoria).FirstOrDefault().Categoria;
                ticketResumenResolutor o = new ticketResumenResolutor()
                {
                    categoria = opCategoria,
                    prioridad = x.Prioridad,
                    tickedID = x.Id,
                    checkVincular = false,
                    totVinculados = x.totVinculados ?? 0,
                    estatus = x.Estatus,
                    isPadre = x.isPadre,
                    isSubTicket = false, //
                    idPrioridad = _idPrioridad,
                    orden = x.orden
                };
                
                // por cada ticket en tabla de SUBs y MAINs verificar si el ticket o está en la columna de hijos (Id)
                if (lstSubTicket2.Contains(o.tickedID)) { 
                    o.isSubTicket = true; } //informar que o es un hijo
                //foreach (var tkt in lstSubTicket2)
                //{
                //    if (tkt.Id == o.tickedID) { o.isSubTicket = true;  } 
                //    //informar que o es un hijo
                //}
                if (o.isSubTicket != true) //si "ticket o" NO es hijo 
                    lstTicketsResumen.Add(o); //imprimir o
            });//imprime a los hijos
            //vm.lstResumenResolutor = lstResumenResolutor ORDER BY X
            if (idFiltro == 1)
            {
                //Del más antiguo al más reciente
                vm.lstResumenResolutor = lstTicketsResumen.OrderBy(x => x.orden).ThenBy(x => x.tickedID).ToList();
            }
            else if (idFiltro == 2)
            {
                //Del más reciente al más antiguo
                vm.lstResumenResolutor = lstTicketsResumen.OrderBy(x => x.orden).ThenByDescending(x => x.tickedID).ToList();
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

            var tareas = new List<vw_TareasProgramadas>();
            if (rol == "Supervisor")
            {
                // Añadir tareas creaadas por este supervisor... añadir las creadas por otros supervisores también?
                //tareas = _db.vw_TareasProgramadas.Where(x => x.SupervisorID == empledoId && x.Estatus == type && x.Activado == true ).ToList();                     // SCHEDULER DESACTIVADO
                tareas = _db.vw_TareasProgramadas.Where(x => x.EmpleadoID == empledoId && x.Estatus == type && x.Activado == true && x.Activado_2 == true).ToList();  // SCHEDULER ACTIVADO
            }
            else
            {
                // --------------------------------------------------------------------------------------------------------------------------------------------
                //tareas = _db.vw_TareasProgramadas.Where(x => x.EmpleadoID == empledoId && x.Estatus == type && x.Activado == true).ToList();                          // SCHEDULER DESACTIVADO
                tareas = _db.vw_TareasProgramadas.Where(x => x.EmpleadoID == empledoId && x.Activado == true && x.Estatus == type && x.Activado_2 == true).ToList();    // SCHEDULER ACTIVADO
            }

            vm.listaTareas = tareas;
            foreach (var tarea in vm.listaTareas)
            { //-- 
                string priori = tarea.Prioridad;
                foreach (char c in priori) priori = priori.Replace(" ", String.Empty);
                tarea.Prioridad = priori;
            }
            return PartialView("../DashBoard/PartialViews/_TicketResolutor", vm);
        } // mostrar tickets en el dashboard      
        public PartialViewResult GetResolutorTickets1(string user, string type, int idFiltro = 0)
        {
            int empledoId = int.Parse(user);
            string rol = RoldeUsuario(empledoId);
            ViewBag.rol = rol;
            vmDashbordResolutor vm = new vmDashbordResolutor();
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);

            //SE FILTRAN POR GRUPO RESOLUTOR//
            var userint = Convert.ToInt32(user);
            var grupInfo = _db.tbl_User.Where(a => a.EmpleadoID == userint).FirstOrDefault();
            var details = _db.tbl_TicketDetalle.Where(x => x.Estatus == type && x.GrupoResolutor == grupInfo.GrupoResolutor).ToList();
            ;
            if (grupInfo.GrupoResolutor == "TI-Soporte")
            {
                // Filtrar por centro if TI-Soporte _ISF            
                // El filtrado de centros es distinto de tecnicos a Supervisores
                // En la tabla tbl_User_TI_Exclusion_Filtro_Centro
                //              estan los Tecnicos a los que el filtro no se les debe aplicar
                // En la tabla tbl_rel_SupervisorCentros
                //              estan los supervisores y los centros que les toca supervisar  
                if (rol == "Tecnico")
                {
                    // verificar que usuario no esté en lista de usuarios excluidos del filtro
                    var Usuarios_Excluidos_Filtro_Centro = _db.tbl_User_TI_Exclusion_Filtro_Centro.Select(t => t.EmployeeId).ToList();
                    if (!Usuarios_Excluidos_Filtro_Centro.Contains(empledoId)) details = details.Where(t => t.Centro == grupInfo.Centro).ToList();
                }
                else
                if (rol == "Supervisor")
                {
                    var Centros = _db.tbl_rel_SupervisorCentros.Where(t => t.UserId == grupInfo.Id).Select(t => t.CentroId).ToArray(); // lista de centros
                    // Si el supervisor no está en la tabla solo le toca supervisar su centro
                    // Si el supervisor está en la tabla, asegurarse que también le toque supervisar el suyo propio
                    if (Centros != null)
                    {
                        Centros = Centros.Concat(new int[] { grupInfo.Centro }).ToArray(); // añadir centro al que pertenece en caso de que no fuera agregado antes
                        details = details.Where(t => t.GrupoResolutor == "TI-Soporte" && Centros.Contains(t.Centro)).ToList();
                    }
                    else
                    {
                        details = details.Where(t => t.GrupoResolutor == "TI-Soporte" && t.Centro == grupInfo.Centro).ToList();
                    }
                }
            }

            if (rol == "Tecnico" && type != "Abierto") // SOLO MOSTRAR TICKETS QUE LE CONCIERNEN AL TECNICO
            {
                int idTecnico = _db.tbl_User.Where(t => t.EmpleadoID == userint).Select(t => t.Id).FirstOrDefault();

                details = details.Where(t =>
                 (t.IdTecnicoAsignado == idTecnico && t.IdTecnicoAsignadoReag == null && t.IdTecnicoAsignadoReag2 == null) ||
                 (t.IdTecnicoAsignadoReag == idTecnico && t.IdTecnicoAsignadoReag2 == null) ||
                 (t.IdTecnicoAsignadoReag2 == idTecnico)
                 ).ToList();
            }

            //---- AÑADIR VINCULADOS Y SUBTICKETS: 
            List<int> ticketsVinculados = _db.tbl_VinculacionDetalle.Where(x => x.Activo == true).Select(x => x.IdTicketChild).ToList();
            var lstSubTickets = _db.vwDetalleSubticket.ToList();
            List<tbl_TicketDetalle> lstTemp = new List<tbl_TicketDetalle>();
            foreach (var item in details)
            {
                item.isPadre = false;
                tbl_TicketDetalle o = new tbl_TicketDetalle() { };
                var lstSubTicket = lstSubTickets.Where(x => x.Id == item.Id).ToList(); // previamente una llamada
                if (lstSubTicket.Count() > 0)
                {
                    lstSubTicket.ForEach(x =>
                    {
                        int _idPrioridad = 0;
                        if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                        else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                        else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                        var opCategoria = lstCategoria.Where(c => c.Id == item.Categoria).FirstOrDefault().Categoria;
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
                            idPrioridad = _idPrioridad,
                            orden = 2
                        };
                        lstTicketsResumen.Add(obj);
                        obj.isSubTicket = true;
                    });
                }
                if (!ticketsVinculados.Contains(item.Id))
                {
                    o = item; // añadir tickets que no están vinculados a otros (hijos)
                    o.orden = 1;
                    lstTemp.Add(o);

                }
                else
                {
                    var e = _db.tbl_Vinculacion.Where(x => x.IdTicket == item.Id).FirstOrDefault();
                    if (e != null)
                    {
                        o = item;
                        o.totVinculados = _db.tbl_VinculacionDetalle.Where(x => x.IdVinculacion == e.IdVinculacion).Count() - 1;
                        o.isPadre = true;
                        o.orden = 1;
                        lstTemp.Add(o); //añadir los padres
                    }
                }
            };
            if (lstTemp.Count > 0) { details = lstTemp; }
            //---- AÑADIR VINCULADOS Y SUBTICKETS: END


            ;
            var lstSubTicket2 = _db.vwDetalleSubticket.Select(t => t.Id).ToList();
            details.ForEach(x => {
                int VARIABLE_De_Prueba = x.Id;
                int _idPrioridad = 0;
                if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                var opCategoria = lstCategoria.Where(c => c.Id == x.Categoria).FirstOrDefault().Categoria;
                ticketResumenResolutor o = new ticketResumenResolutor()
                {
                    categoria = opCategoria,
                    prioridad = x.Prioridad,
                    tickedID = x.Id,
                    checkVincular = false,
                    totVinculados = x.totVinculados ?? 0,
                    estatus = x.Estatus,
                    isPadre = x.isPadre,
                    isSubTicket = false, //
                    idPrioridad = _idPrioridad,
                    orden = x.orden
                };

                // por cada ticket en tabla de SUBs y MAINs verificar si el ticket o está en la columna de hijos (Id)
                if (lstSubTicket2.Contains(o.tickedID))
                {
                    o.isSubTicket = true;
                } //informar que o es un hijo
                //foreach (var tkt in lstSubTicket2)
                //{
                //    if (tkt.Id == o.tickedID) { o.isSubTicket = true;  } 
                //    //informar que o es un hijo
                //}
                if (o.isSubTicket != true) //si "ticket o" NO es hijo 
                    lstTicketsResumen.Add(o); //imprimir o
            });//imprime a los hijos
            //vm.lstResumenResolutor = lstResumenResolutor ORDER BY X
            if (idFiltro == 1)
            {
                //Del más antiguo al más reciente
                vm.lstResumenResolutor = lstTicketsResumen.OrderBy(x => x.orden).ThenBy(x => x.tickedID).ToList();
            }
            else if (idFiltro == 2)
            {
                //Del más reciente al más antiguo
                vm.lstResumenResolutor = lstTicketsResumen.OrderBy(x => x.orden).ThenByDescending(x => x.tickedID).ToList();
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

            var tareas = new List<vw_TareasProgramadas>();
            if (rol == "Supervisor")
            {
                // Añadir tareas creaadas por este supervisor... añadir las creadas por otros supervisores también?
                //tareas = _db.vw_TareasProgramadas.Where(x => x.SupervisorID == empledoId && x.Estatus == type && x.Activado == true ).ToList();                     // SCHEDULER DESACTIVADO
                tareas = _db.vw_TareasProgramadas.Where(x => x.EmpleadoID == empledoId && x.Estatus == type && x.Activado == true && x.Activado_2 == true).ToList();  // SCHEDULER ACTIVADO
            }
            else
            {
                // --------------------------------------------------------------------------------------------------------------------------------------------
                //tareas = _db.vw_TareasProgramadas.Where(x => x.EmpleadoID == empledoId && x.Estatus == type && x.Activado == true).ToList();                          // SCHEDULER DESACTIVADO
                tareas = _db.vw_TareasProgramadas.Where(x => x.EmpleadoID == empledoId && x.Activado == true && x.Estatus == type && x.Activado_2 == true).ToList();    // SCHEDULER ACTIVADO
            }

            vm.listaTareas = tareas;
            foreach (var tarea in vm.listaTareas)
            { //-- 
                string priori = tarea.Prioridad;
                foreach (char c in priori) priori = priori.Replace(" ", String.Empty);
                tarea.Prioridad = priori;
            }
            return PartialView("../DashBoard/PartialViews/_TicketResolutor", vm);
        } // mostrar tickets en el dashboard OPTOMIZADO
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public PartialViewResult dtaVincular(int[] list, string user) 
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
            //int empledoId = int.Parse(user);
            //vmDashbordResolutor vm = new vmDashbordResolutor();
            //vm.totTicketsVinculados = list.Length;
            //List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();
            //var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);
            //var details = _db.tbl_TicketDetalle.Where(x => x.EmpleadoID == empledoId).ToList();
            //details.ForEach(x => { 
            //    if (list.Contains(x.Id)) {
            //        var opCategoria = lstCategoria.Where(c => c.Id == x.Categoria).FirstOrDefault().Categoria;
            //        ticketResumenResolutor o = new ticketResumenResolutor()
            //        {
            //            categoria = opCategoria,
            //            prioridad = x.Prioridad,
            //            tickedID = x.Id,
            //            estatus = x.Estatus
            //        };
            //        lstTicketsResumen.Add(o);
            //    } 
            //});


            //vm.lstResumenResolutor = lstTicketsResumen;

            //return PartialView("../DashBoard/PartialViews/_Vincular", vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public JsonResult VincularTicket(int ticketPadre, int[] lstTIcket, string user) 
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
                if (item != ticketPadre)
                { 
                    var tpadre = _db.tbl_TicketDetalle.Where(t => t.Id == ticketPadre).FirstOrDefault();
                    var thijo = _db.tbl_TicketDetalle.Where(t => t.Id == item).FirstOrDefault();
                    thijo.Categoria = tpadre.Categoria;
                    thijo.SubCategoria = tpadre.SubCategoria;
                    //thijo.Centro = tpadre.Centro;
                }
                _db.SaveChanges();
            }

            return Json("OK", JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -   
        public ActionResult Tecnico(int EmployeeId, int type = 0)
        {
            ViewBag.user = EmployeeId;
            ViewBag.Rol = RoldeUsuario(EmployeeId);
            var User = EmployeeId;
            var tecnico = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeId && x.Activo == true).FirstOrDefault();
            var edoTicket = _db.cat_EstadoTicket.Where(x => x.Activo == true).ToList();

            var tickets = _db.tbl_TicketDetalle.Where(x => x.TecnicoAsignado == tecnico.NombreTecnico).ToList();

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
        public PartialViewResult GetTecnicoTickets(string user, string type, int idFiltro = 0)
        {
            int empledoId = int.Parse(user);
            var tecnico = _db.tbl_User.Where(x => x.EmpleadoID == empledoId && x.Activo == true).FirstOrDefault();
            var edoTicket = _db.cat_EstadoTicket.Where(x => x.Activo == true).ToList();

            vmDashbordResolutor vm = new vmDashbordResolutor();
            List<ticketResumenResolutor> lstTicketsResumen = new List<ticketResumenResolutor>();
            var lstCategoria = _db.cat_Categoria.Where(x => x.Activo == true);
            var details = _db.tbl_TicketDetalle.Where(x => x.Estatus == type && x.TecnicoAsignado == tecnico.NombreTecnico).ToList();
           

            List<int> ticketsVinculados = _db.tbl_VinculacionDetalle.Where(x => x.Activo == true).Select(x => x.IdTicketChild).ToList();
            List<tbl_TicketDetalle> lstTemp = new List<tbl_TicketDetalle>();
            foreach (var item in details)
            {
                item.isPadre = false;
                tbl_TicketDetalle o = new tbl_TicketDetalle() { };
                var lstSubTicket = _db.vwDetalleSubticket.Where(x => x.IdTicket == item.Id).ToList();
                if (lstSubTicket.Count() > 0)
                {
                    lstSubTicket.ForEach(x =>
                    {
                        int _idPrioridad = 0;
                        if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                        else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                        else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                        var opCategoria = lstCategoria.Where(c => c.Id == item.Categoria).FirstOrDefault().Categoria;
                        ticketResumenResolutor obj = new ticketResumenResolutor()
                        {
                            categoria = opCategoria,
                            prioridad = x.Prioridad,
                            tickedID = x.Id,
                            checkVincular = false,
                            totVinculados = 0,
                            estatus = x.Estatus,
                            isPadre = true,
                            idTicketPadre = x.IdTicket,
                            isSubTicket = true,
                            idSubTicket = x.Id,
                            idPrioridad = _idPrioridad,
                            orden = 2
                        };
                        lstTicketsResumen.Add(obj);
                    });
                }
                if (!ticketsVinculados.Contains(item.Id))
                {
                    o = item;
                    o.orden = 1;
                    lstTemp.Add(o);

                }
                else
                {
                    var e = _db.tbl_Vinculacion.Where(x => x.IdTicket == item.Id).FirstOrDefault();
                    if (e != null)
                    {
                        o = item;
                        o.totVinculados = _db.tbl_VinculacionDetalle.Where(x => x.IdVinculacion == e.IdVinculacion).Count() - 1;
                        o.isPadre = true;
                        o.orden = 1;
                        lstTemp.Add(o);
                    }
                }
            };
            if (lstTemp.Count > 0) { details = lstTemp; }

            details.ForEach(x => {
                int _idPrioridad = 0;
                if (x.Prioridad == "Alto") { _idPrioridad = 1; }
                else if (x.Prioridad == "Medio") { _idPrioridad = 2; }
                else if (x.Prioridad == "Baja") { _idPrioridad = 3; }
                var opCategoria = lstCategoria.Where(c => c.Id == x.Categoria).FirstOrDefault().Categoria;
                ticketResumenResolutor o = new ticketResumenResolutor()
                {
                    categoria = opCategoria,
                    prioridad = x.Prioridad,
                    tickedID = x.Id,
                    checkVincular = false,
                    totVinculados = x.totVinculados ?? 0,
                    estatus = x.Estatus,
                    isPadre = x.isPadre,
                    idPrioridad = _idPrioridad,
                    orden = x.orden
                };
                lstTicketsResumen.Add(o);
            });
            if (idFiltro == 1)
            {
                vm.lstResumenResolutor = lstTicketsResumen.OrderBy(x => x.orden).ThenBy(x => x.tickedID).ToList();
            }
            else if (idFiltro == 2)
            {
                vm.lstResumenResolutor = lstTicketsResumen.OrderBy(x => x.orden).ThenByDescending(x => x.tickedID).ToList();
            }
            else if (idFiltro == 3)
            {
                vm.lstResumenResolutor = lstTicketsResumen.OrderBy(x => x.orden).ThenBy(x => x.idPrioridad).ToList();
            }
            else
            {
                vm.lstResumenResolutor = lstTicketsResumen;
            }
            //vm.lstResumenResolutor =
            var tareas = _db.vw_TareasProgramadas.Where(x => x.EmpleadoID == empledoId).ToList();//------------------------------
            vm.listaTareas = tareas;
            foreach (var tarea in vm.listaTareas)
            { //--------------------------------------------------------verificar por que se guardan asi las prioridades
                string priori = tarea.Prioridad;
                foreach (char c in priori) priori = priori.Replace(" ", String.Empty);
                tarea.Prioridad = priori;
            }
            return PartialView("../DashBoard/PartialViews/_TicketResolutor", vm);
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        public ActionResult Success()
        {
            return View();
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -        
        //ENCUESTA AYB
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
        public List<ticketByEstado> New_List_Estados() {
            var lista = new List<ticketByEstado>();

            var edos = _db.cat_EstadoTicket.Where(t => t.Activo == true).ToList();
            foreach (var edo in edos) {
                var lista_element = new ticketByEstado();
                lista_element.estado = edo.Estado;
                lista_element.orden = edo.Orden;
                lista_element.total = 0;
                lista.Add(lista_element);
            }

            return lista;
        }

        public void ActualizarRol(List<string> rol, int employeeId) {
            var user = _db.tbl_User.Where(t => t.EmpleadoID == employeeId).FirstOrDefault();
            string nuevo_rol = "";

            //orden inverso al de home
            if (rol.Contains("Directivo")) { nuevo_rol = "Directivo";  }
            else if (rol.Contains("ServiceDesk")) { nuevo_rol = "ServiceDesk"; }
            else if (rol.Contains("Técnico")) { nuevo_rol = "Tecnico"; }
            else if (rol.Contains("Tecnico")) { nuevo_rol = "Tecnico"; }
            else if (rol.Contains("Supervisor")) { nuevo_rol = "Supervisor"; }
            else if(rol.Contains("Solicitante")) { nuevo_rol = "Solicitante"; }

            user.Rol = nuevo_rol;

            _db.tbl_User.AddOrUpdate(user);
            _db.SaveChanges();
        }
        public string RoldeUsuario(int EmployeeID) //String que obtiene el Rol del usuario dado su ID 
        {
            string rol = "";

            var numemp = _admin.tblUser.Where(a => a.EmpleadoId == EmployeeID).FirstOrDefault();
            var rols = Roles.GetRolesForUser(numemp.UserName);

            var ptoSupRol = _sdmanager.GetRolByPuesto(rols);

            if (ptoSupRol.Contains("Supervisor")) { ViewBag.RoleNameUser = "Supervisor"; }
            else if (ptoSupRol.Contains("Tecnico")) { ViewBag.RoleNameUser = "Tecnico"; }

            if (ptoSupRol.Contains("Solicitante"))      { rol = "Solicitante";  }
            else if (ptoSupRol.Contains("Supervisor"))  { rol = "Supervisor";   }
            else if (ptoSupRol.Contains("Tecnico"))     { rol = "Tecnico";      }    //-* usuario resolutor
            else if (ptoSupRol.Contains("ServiceDesk")) { rol = "ServiceDesk";  }    //-* usuario resolutor
            else if (ptoSupRol.Contains("Directivo"))   { rol = "Directivo";    }
            else { }

            return rol;
        }

    }
}
