using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServiceDesk.Models;
using ServiceDesk.ViewModels;
using System.Web.Security;
//
using ServiceDesk.Managers;
using System.Data;
using System.IO;
//
using System.Data.Entity.Migrations;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using SpreadsheetLight;
using System.Collections;

namespace ServiceDesk.Controllers
{
    public class ReportesController : Controller
    {
        private readonly RHAdminContext _rh = new RHAdminContext();
        private readonly ServiceDeskManager _mng = new ServiceDeskManager();
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private readonly DashBoardManager _mngDas = new DashBoardManager(); 
        private readonly SlaManager _sla = new SlaManager();
        private readonly AdminContext _admin = new AdminContext();
        ServiceDeskManager _sdmanager = new ServiceDeskManager();


        public DatosReportes PiedataPrioridad(List<tbl_TicketDetalle> tickets)
        {
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
                if (strPrioridad[i] == null) strPrioridad[i] = "Prioridad no establecida";
                intCount[i] = item.Count;
                i++;
            }

            datos = new DatosReportes
            {
                Column = strPrioridad,
                Count = intCount
            };

            return datos;
        }

        public DatosReportes PiedataCentro(List<tbl_TicketDetalle> tickets)
        {
            var Centros = _db.cat_Centro.ToList();

            // Poner todos los tickets sin centro en un mismo centro "Centro Borrado"
            var CentrosExistentes = Centros.Select(t => t.Id);
            foreach (var ticket in tickets)
            {
                if (!CentrosExistentes.Contains(ticket.Centro)) { ticket.Centro = 0; }
            }

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
                if (item.Centro == 0) { strPrioridad[i] = "Centro Borrado"; }
                else { strPrioridad[i] = Centros.Where(c => c.Id == item.Centro).FirstOrDefault().Centro; }
                intCount[i] = item.Count;
                i++;
            }

            datos = new DatosReportes
            {
                Column = strPrioridad,
                Count = intCount
            };

            return datos;
        }
        public DatosReportes PiedataEstatus(List<tbl_TicketDetalle> tickets)
        {
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


            return datos;
        }
        public DatosReportes PiedataTipo(List<tbl_TicketDetalle> tickets)
        {
            var categorias = new List<cat_MatrizCategoria>();

            var IntSubCats = new List<int>();
            foreach (var item in tickets) { IntSubCats.Add(item.SubCategoria); }
            // En este punto IntSubCats tiene una lista de ids de subcategorías filtrada por fechas -----------------------------------------------------------
            categorias = _db.cat_MatrizCategoria.Where(Matriz => IntSubCats.Contains(Matriz.IDSubCategoria)).ToList();
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


            return datos;
        }
        public DatosReportes PiedataExpertiz(List<tbl_TicketDetalle> tickets)
        {
            var categorias = new List<cat_MatrizCategoria>();

            var List_Int_Subcat = new List<int>();
            foreach (var item in tickets) { List_Int_Subcat.Add(item.SubCategoria); }
            // En este punto IntSubCats tiene una lista de ids de subcategorías filtrada por fechas 
            categorias = _db.cat_MatrizCategoria.Where(Matriz => List_Int_Subcat.Contains(Matriz.IDSubCategoria)).ToList();

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


            return datos;
        }
        public DatosReportes PiedataSLA1(List<tbl_TicketDetalle> tickets)
        {
            var detalle = new DetalleSelectedTicketVm();

            int ticketsOutOfSLA = 0;
            int ticketsInsidSLA = 0;
            int id; string TimeCurrentSLA = "", TimeObjetiveSLA = "", hrSLAact = "", minSLAact = "", hrSLAObj = "", minSLAObj = "";
            foreach (var ticket in tickets)
            {
                // obtener sla total
                {
                    id = ticket.Id;
                    detalle.detalle = _db.VWDetalleTicket.Where(z => z.Id == id).FirstOrDefault();
                    //var info = _db.his_Ticket.Where(a => a.IdTicket == id).ToList();
                    var info = _db.his_Ticket.Where(a => a.IdTicket == id).ToList();
                    foreach (var his in info)
                    {
                        if (his.TecnicoAsignado == "") his.TecnicoAsignado = null;
                        else
                        if (his.TecnicoAsignado == "Test autoasignación") his.TecnicoAsignado = null;
                        if (his.TecnicoAsignadoReag == "") his.TecnicoAsignadoReag = null;
                        if (his.TecnicoAsignadoReag2 == "") his.TecnicoAsignadoReag2 = null;
                    }
                    detalle.Slas = _sla.GetSlaTimes(info);
                    int timeObjetivo = 0, timeActual = 0;

                    // obtener tiempos del ticket actual
                    foreach (var sLa in detalle.Slas)
                    {
                        if (sLa.Type == "SLA Objetivo") { TimeObjetiveSLA = sLa.Time.ToString(); }
                        if (sLa.Type == "SLA total") { TimeCurrentSLA = sLa.Time.ToString(); }
                    }

                    // tiempo en formato "248:51"   o   "-30:-5" "-33:-14" "2:5"
                    // conversión tiempo en formato reloj "248:51" a formato numero entero representando cantidad total de minutos
                    hrSLAObj = TimeObjetiveSLA.Split(':')[0];
                    minSLAObj = TimeObjetiveSLA.Split(':')[1];
                    timeObjetivo = (Int32.Parse(hrSLAObj) * 60) + Int32.Parse(minSLAObj);

                    hrSLAact = TimeCurrentSLA.Split(':')[0];
                    minSLAact = TimeCurrentSLA.Split(':')[1];
                    timeActual = (Int32.Parse(hrSLAact) * 60) + Int32.Parse(minSLAact);

                    var inor = "";
                    if (timeActual > 0) // De ser TimeActual menor que 0, es un ticket con información corrupta, no tomar en cuenta
                        if (timeActual >= timeObjetivo) { ticketsOutOfSLA++; inor = "out"; }
                        else { ticketsInsidSLA++; inor = "in"; }

                }
            }

            DatosReportes datos = null;
            String[] strColumnName = new string[2];
            int[] intCount = new int[2];

            strColumnName[0] = "Tickets fuera de SLA";
            intCount[0] = ticketsOutOfSLA;

            strColumnName[1] = "Tickets dentro de SLA";
            intCount[1] = ticketsInsidSLA;

            datos = new DatosReportes
            {
                Column = strColumnName,
                Count = intCount
            };


            return datos;
        }
        public DatosReportes PiedataSLA(List<tbl_TicketDetalle> tickets) // version optimizada 2
        {
            int ticketsOutOfSLA = 0;
            int ticketsInsidSLA = 0;
            var categorias = _db.cat_MatrizCategoria.ToList();
            var historiales = _db.his_Ticket.ToList();
            var usuarios = _db.tbl_User.ToList();
            var ventana = _db.tbl_VentanaAtencion.ToList();
            foreach (var ticket in tickets)
            {
                string inn = "";
                var historial = historiales.Where(t => t.IdTicket == ticket.Id).ToList();
                if (_sla.inTime(historial, categorias, usuarios, ventana)) 
                {
                    ticketsInsidSLA += 1;
                    inn = "in time";
                }
                else 
                { 
                    ticketsOutOfSLA += 1;
                    inn = "out of time";
                }
                System.Diagnostics.Debug.WriteLine("SLA {2} de {3} / id{0}: id{1}", ticket.Id, inn, ticketsInsidSLA + ticketsOutOfSLA, tickets.Count());
            }

            DatosReportes datos = null;
            String[] strColumnName = new string[2];
            int[] intCount = new int[2];

            strColumnName[0] = "Tickets fuera de SLA";
            intCount[0] = ticketsOutOfSLA;

            strColumnName[1] = "Tickets dentro de SLA";
            intCount[1] = ticketsInsidSLA;

            datos = new DatosReportes
            {
                Column = strColumnName,
                Count = intCount
            };

            return datos;
        }
        public DatosReportes PiedataResolutor(List<tbl_TicketDetalle> tickets)
        {

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


            return datos;
        }
        public DatosReportes PiedataEncuesta(List<tbl_TicketDetalle> tickets)
        {
            // lista de encuestas
            var encuest = new List<EncuestaDetalle>();

            var ticketsCerrados = 0;
            var List_IdTickets_grpResol = new List<int>();

            //encuest = _db.EncuestaDetalle.Where(f => f.GrupoResolutor == strGrupoResolutor).ToList();
            foreach (var ticket in tickets)
            {
                if (ticket.Estatus == "Cerrado") { ticketsCerrados++; }
                //if (ticket.GrupoResolutor == strGrupoResolutor) { List_IdTickets_grpResol.Add(ticket.Id); }
                List_IdTickets_grpResol.Add(ticket.Id);
            }
            encuest = _db.EncuestaDetalle.Where(tabla => List_IdTickets_grpResol.Contains(tabla.IdTicket)).ToList();


            // Preparar formato de datos para el pie
            var cantEncuest = encuest.Count();

            DatosReportes datos = null;
            String[] strEncu = new string[2];
            int[] intCount = new int[2];

            strEncu[0] = "Encuestas contestadas";
            intCount[0] = cantEncuest;

            strEncu[1] = "Tickets Cerrados sin encuestar";
            intCount[1] = ticketsCerrados;

            datos = new DatosReportes
            {
                Column = strEncu,
                Count = intCount
            };


            return datos;
        }
        public DatosReportes PiedataCalidad(List<tbl_TicketDetalle> tickets)
        {
            // lista de encuestas
            var encuest = new List<EncuestaDetalle>();

            // obtener tickets filtrados por fecha (y resolutor si es necesario)
            var List_IdTickets_grpResol = new List<int>();
            foreach (var ticket in tickets) List_IdTickets_grpResol.Add(ticket.Id);
            //llenar lista encuesta con solo los tickets que pasaron el filtro fecha y el filtro resolutor
            encuest = _db.EncuestaDetalle.Where(tabla => List_IdTickets_grpResol.Contains(tabla.IdTicket)).ToList();

            // Preparar formato de datos para el pie
            var data = encuest.GroupBy(info => info.CalificaServicio).Select(group => new {
                Calificacion = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.Calificacion);
            DatosReportes datos = null;
            String[] strCalif = new string[data.Count()];
            int[] intCount = new int[data.Count()];
            int i = 0;
            foreach (var item in data)
            {
                strCalif[i] = item.Calificacion;
                intCount[i] = item.Count;
                i++;
            }
            datos = new DatosReportes
            {
                Column = strCalif,
                Count = intCount
            };

            return datos;
        }


        public ActionResult PieChartPrioridad(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        {
            var tickets = FiltrarTickets(EmployeeID, fechaInici, fechaFinal);

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
                if (strPrioridad[i] == null) strPrioridad[i] = "Prioridad no establecida";
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
        public ActionResult PieChartCentro(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        {
            var tickets = FiltrarTickets(EmployeeID, fechaInici, fechaFinal);
            var Centros = _db.cat_Centro.ToList();

            // Poner todos los tickets sin centro en un mismo centro "Centro Borrado"
            var CentrosExistentes = Centros.Select(t => t.Id);
            foreach (var ticket in tickets) {
                if (!CentrosExistentes.Contains(ticket.Centro)) { ticket.Centro = 0; }    
            }

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
                if (item.Centro == 0) { strPrioridad[i] = "Centro Borrado"; }
                else { strPrioridad[i] = Centros.Where(c => c.Id == item.Centro).FirstOrDefault().Centro; }
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
        public ActionResult PieChartEstatus(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        {
            var tickets = FiltrarTickets(EmployeeID, fechaInici, fechaFinal);

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

            if (datos != null) { return View(datos); }
            else return View();
        }
        public ActionResult PieChartTipo(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        {
            var categorias = new List<cat_MatrizCategoria>();
            var tickets = FiltrarTickets(EmployeeID, fechaInici, fechaFinal);

            var IntSubCats = new List<int>();
            foreach (var item in tickets) { IntSubCats.Add(item.SubCategoria); }
            // En este punto IntSubCats tiene una lista de ids de subcategorías filtrada por fechas -----------------------------------------------------------
            categorias = _db.cat_MatrizCategoria.Where(Matriz => IntSubCats.Contains(Matriz.IDSubCategoria)).ToList();
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
        public ActionResult PieChartExpertiz(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        {
            var categorias = new List<cat_MatrizCategoria>();
            var tickets = FiltrarTickets(EmployeeID, fechaInici, fechaFinal);

            var List_Int_Subcat = new List<int>();
            foreach (var item in tickets) { List_Int_Subcat.Add(item.SubCategoria); }
            // En este punto IntSubCats tiene una lista de ids de subcategorías filtrada por fechas 
            categorias = _db.cat_MatrizCategoria.Where(Matriz => List_Int_Subcat.Contains(Matriz.IDSubCategoria)).ToList();

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
        public ActionResult PieChartSLA(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        {
            var detalle = new DetalleSelectedTicketVm();
            var tickets = FiltrarTickets(EmployeeID, fechaInici, fechaFinal);

            int ticketsOutOfSLA = 0;
            int ticketsInsidSLA = 0;
            int id; string TimeCurrentSLA = "", TimeObjetiveSLA = "", hrSLAact = "", minSLAact = "", hrSLAObj= "", minSLAObj= "";
            foreach (var ticket in tickets) {
                // obtener sla total
                {
                    id = ticket.Id;
                    detalle.detalle = _db.VWDetalleTicket.Where(z=>z.Id == id).FirstOrDefault();
                    //var info = _db.his_Ticket.Where(a => a.IdTicket == id).ToList();
                    var info = _db.his_Ticket.Where(a => a.IdTicket == id ).ToList();
                    foreach (var his in info) {
                        if (his.TecnicoAsignado == "")      his.TecnicoAsignado = null;
                        else
                        if (his.TecnicoAsignado == "Test autoasignación")      his.TecnicoAsignado = null;
                        if (his.TecnicoAsignadoReag == "")  his.TecnicoAsignadoReag = null;
                        if (his.TecnicoAsignadoReag2 == "") his.TecnicoAsignadoReag2 = null;
                    }
                    detalle.Slas = _sla.GetSlaTimes(info);
                    int timeObjetivo = 0, timeActual = 0;

                    // obtener tiempos del ticket actual
                    foreach (var sLa in detalle.Slas) { 
                        if (sLa.Type == "SLA Objetivo")  { TimeObjetiveSLA = sLa.Time.ToString();  }  
                        if (sLa.Type == "SLA total") { TimeCurrentSLA = sLa.Time.ToString(); } 
                    }

                    // tiempo en formato "248:51"   o   "-30:-5" "-33:-14" "2:5"
                    // conversión tiempo en formato reloj "248:51" a formato numero entero representando cantidad total de minutos
                    hrSLAObj = TimeObjetiveSLA.Split(':')[0];
                    minSLAObj = TimeObjetiveSLA.Split(':')[1];
                    timeObjetivo = (Int32.Parse(hrSLAObj) * 60) + Int32.Parse(minSLAObj);    

                    hrSLAact = TimeCurrentSLA.Split(':')[0];
                    minSLAact = TimeCurrentSLA.Split(':')[1];
                    timeActual = (Int32.Parse(hrSLAact) * 60) + Int32.Parse(minSLAact);

                    var inor = "";
                    if (timeActual > 0) // De ser TimeActual menor que 0, es un ticket con información corrupta, no tomar en cuenta
                    if (timeActual >= timeObjetivo) { ticketsOutOfSLA++; inor = "out"; } 
                    else                            { ticketsInsidSLA++; inor = "in"; }

                }
            }

            DatosReportes datos = null;
            String[] strColumnName = new string[2];
            int[] intCount = new int[2];

            strColumnName[0] = "Tickets fuera de SLA";
            intCount[0] = ticketsOutOfSLA;

            strColumnName[1] = "Tickets dentro de SLA";
            intCount[1] = ticketsInsidSLA;

            datos = new DatosReportes {
                Column = strColumnName,
                Count = intCount
            };

            if (datos != null) { return View(datos); }
            else return View();
        }
        public ActionResult PieChartResolutor(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        {
            var tickets = FiltrarTickets(0, fechaInici, fechaFinal);

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
        public ActionResult PieChartEncuesta(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        {
            // lista de encuestas
            var encuest = new List<EncuestaDetalle>();
            var tickets = FiltrarTickets(EmployeeID, fechaInici, fechaFinal);

            var ticketsCerrados = 0;
            var List_IdTickets_grpResol = new List<int>();

            //encuest = _db.EncuestaDetalle.Where(f => f.GrupoResolutor == strGrupoResolutor).ToList();
            foreach (var ticket in tickets) { 
                if (ticket.Estatus == "Cerrado") { ticketsCerrados++; }
                //if (ticket.GrupoResolutor == strGrupoResolutor) { List_IdTickets_grpResol.Add(ticket.Id); }
                List_IdTickets_grpResol.Add(ticket.Id);  
            }
            encuest = _db.EncuestaDetalle.Where(tabla => List_IdTickets_grpResol.Contains(tabla.IdTicket)).ToList();
            

            // Preparar formato de datos para el pie
            var cantEncuest = encuest.Count();

            DatosReportes datos = null;
            String[] strEncu = new string[2];
            int[] intCount = new int[2];

            strEncu[0] = "Encuestas contestadas";
            intCount[0] = cantEncuest;

            strEncu[1] = "Tickets Cerrados sin encuestar";
            intCount[1] = ticketsCerrados;

            datos = new DatosReportes
            {
                Column = strEncu,
                Count = intCount
            };

            if (datos != null) { return View(datos); }
            else return View();
        }
        public ActionResult PieChartCalidad(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        {
            // lista de encuestas
            var encuest = new List<EncuestaDetalle>();
            var tickets = FiltrarTickets(EmployeeID, fechaInici, fechaFinal);

            // obtener tickets filtrados por fecha (y resolutor si es necesario)
            var List_IdTickets_grpResol = new List<int>();
            foreach (var ticket in tickets) List_IdTickets_grpResol.Add(ticket.Id);
            //llenar lista encuesta con solo los tickets que pasaron el filtro fecha y el filtro resolutor
            encuest = _db.EncuestaDetalle.Where(tabla => List_IdTickets_grpResol.Contains(tabla.IdTicket)).ToList();

            // Preparar formato de datos para el pie
            var data = encuest.GroupBy(info => info.CalificaServicio).Select(group => new {
                Calificacion = group.Key,
                Count = group.Count()
            }).OrderBy(x => x.Calificacion);
            DatosReportes datos = null;
            String[] strCalif = new string[data.Count()];
            int[] intCount = new int[data.Count()];
            int i = 0;
            foreach (var item in data) {
                strCalif[i] = item.Calificacion;
                intCount[i] = item.Count;
                i++;
            }
            datos = new DatosReportes {
                Column = strCalif,
                Count = intCount
            };
            if (datos != null) return View(datos); 
            else return View();
        }
        public ActionResult PieChartVacio(int EmployeeID)
        {
            ViewBag.user = EmployeeID;
            return View();
        }
        
        public List<tbl_TicketDetalle> FiltrarTickets(int EmployeeID, DateTime? fechaInici, DateTime? fechaFinal)
        { 
            // Proprciona tickets filtrados por fechas, ids, o grupo reslutor, dependiendo del id del usuario viendo los pies
            // proporcionar 0 como id evita todos los filtros

            List<tbl_TicketDetalle> tickets = new List<tbl_TicketDetalle>();
            //Filtro de Fechas 
            DateTime fechaTemp; DateTime fechaO = DateTime.Today; DateTime fechaF = DateTime.Today;
            if (fechaInici != null) { fechaTemp = (DateTime)(fechaInici); fechaO = fechaTemp.Date; }
            if (fechaFinal != null) { fechaTemp = (DateTime)(fechaFinal); fechaF = fechaTemp.Date; }


            if (SkipResolutorFilter(EmployeeID)) // Servicedesk, id = 0, Directivos, no aplicar filtros (solo filtro de fechas)
            { 
                if (fechaFinal == null && fechaInici == null) { tickets = _db.tbl_TicketDetalle.ToList(); } else
                if (fechaFinal != null && fechaInici == null) { tickets = _db.tbl_TicketDetalle.Where(f => f.FechaRegistro <= fechaF).ToList(); } else
                if (fechaFinal == null && fechaInici != null) { tickets = _db.tbl_TicketDetalle.Where(f => f.FechaRegistro >= fechaO).ToList(); } else
                if (fechaFinal != null && fechaInici != null)
                {
                    if (fechaFinal == fechaInici) fechaF = fechaF.AddDays(1);
                    tickets = _db.tbl_TicketDetalle.Where(f => f.FechaRegistro >= fechaO && f.FechaRegistro <= fechaF).ToList();
                }
            }
            else { //Tecnicos y Supervisores 
                if (RoldeUsuario(EmployeeID) == "Tecnico") {  //Tecnicos  (solo ven lo relacionado a sus personal stats)
                    // Get id of resolutor
                    var resolutorId = getResolutorId(EmployeeID);
                    if (fechaFinal == null && fechaInici == null) { 
                        tickets = _db.tbl_TicketDetalle.Where(f => 
                            (f.IdTecnicoAsignadoReag2 == resolutorId) ||
                            (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == resolutorId) ||
                            (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == null && f.IdTecnicoAsignado == resolutorId)
                        ).ToList(); 
                    } 
                    else
                    if (fechaFinal != null && fechaInici == null) { 
                        tickets = _db.tbl_TicketDetalle.Where(f => f.FechaRegistro <= fechaF && 
                            ((f.IdTecnicoAsignadoReag2 == resolutorId) ||
                            (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == resolutorId) ||
                            (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == null && f.IdTecnicoAsignado == resolutorId))
                        ).ToList(); 
                    } 
                    else
                    if (fechaFinal == null && fechaInici != null) { 
                        tickets = _db.tbl_TicketDetalle.Where(f => f.FechaRegistro >= fechaO && 
                            ((f.IdTecnicoAsignadoReag2 == resolutorId) ||
                            (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == resolutorId) ||
                            (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == null && f.IdTecnicoAsignado == resolutorId))
                        ).ToList(); } 
                    else
                    if (fechaFinal != null && fechaInici != null)
                    {
                        if (fechaFinal == fechaInici) fechaF = fechaF.AddDays(1);
                        tickets = _db.tbl_TicketDetalle.Where(f => f.FechaRegistro >= fechaO && f.FechaRegistro <= fechaF && 
                            ((f.IdTecnicoAsignadoReag2 == resolutorId) ||
                            (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == resolutorId) ||
                            (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == null && f.IdTecnicoAsignado == resolutorId))
                        ).ToList();
                    }
                }
                else {
                    // supervisores (solo ven lo relacionado a su grupo resolutor)
                    String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
                    if (fechaFinal == null && fechaInici == null) { tickets = _db.tbl_TicketDetalle.Where(f => f.GrupoResolutor == strGrupoResolutor).ToList(); } else
                    if (fechaFinal != null && fechaInici == null) { tickets = _db.tbl_TicketDetalle.Where(f => f.FechaRegistro <= fechaF && f.GrupoResolutor == strGrupoResolutor).ToList(); } else
                    if (fechaFinal == null && fechaInici != null) { tickets = _db.tbl_TicketDetalle.Where(f => f.FechaRegistro >= fechaO && f.GrupoResolutor == strGrupoResolutor).ToList(); } else
                    if (fechaFinal != null && fechaInici != null)
                    {
                        if (fechaFinal == fechaInici) fechaF = fechaF.AddDays(1);
                        tickets = _db.tbl_TicketDetalle.Where(f => f.FechaRegistro >= fechaO && f.FechaRegistro <= fechaF && f.GrupoResolutor == strGrupoResolutor).ToList();
                    }
                }
            }

            return tickets;
        }


        public ActionResult Graficos2(int EmployeeID)
        {
            ViewBag.user = EmployeeID;
            ViewBag.Rol = RoldeUsuario(EmployeeID);
            return View();
        }

        public ActionResult Graficos(int EmployeeID, DateTime? fechaInicial, DateTime? fechaFinal)
        {
            ViewBag.user = EmployeeID;
            ViewBag.Rol = RoldeUsuario(EmployeeID);
            ViewBag.fechaInicial = (fechaInicial != null) ? fechaInicial?.ToString("d")  : "";
            ViewBag.fechaFinal   = (fechaFinal != null)   ? fechaFinal?.ToString("d")  : "";
            GraphicInfo GraInfo = new GraphicInfo();
            List<tbl_TicketDetalle> tickets = FiltrarTickets(EmployeeID, fechaInicial, fechaFinal);

            var prioridad = PiedataPrioridad(tickets);
            var centro = PiedataCentro(tickets);
            var estatus = PiedataEstatus(tickets);
            var tipo = PiedataTipo(tickets);
            var expertiz = PiedataExpertiz(tickets);
            var resolutor = PiedataResolutor(tickets);
            var sla = PiedataSLA(tickets);
            var encuesta = PiedataEncuesta(tickets);
            var calidad = PiedataCalidad(tickets);

            GraInfo.column_prioridad = formatearColumnas(prioridad.Column, prioridad.Count);
            GraInfo.count_prioridad= prioridad.Count;
            GraInfo.column_centro = formatearColumnas(centro.Column, centro.Count);
            GraInfo.count_centro = centro.Count;
            GraInfo.column_estatus = formatearColumnas(estatus.Column, estatus.Count);
            GraInfo.count_estatus = estatus.Count;

            GraInfo.column_tipo = formatearColumnas(tipo.Column, tipo.Count);
            GraInfo.count_tipo = tipo.Count;
            GraInfo.column_expertiz = formatearColumnas(expertiz.Column, expertiz.Count);
            GraInfo.count_expertiz = expertiz.Count;
            GraInfo.column_resolutor = formatearColumnas(resolutor.Column, resolutor.Count);
            GraInfo.count_resolutor = resolutor.Count;

            GraInfo.column_sla = formatearColumnas(sla.Column, sla.Count);
            GraInfo.count_sla = sla.Count;
            GraInfo.column_encuesta = formatearColumnas(encuesta.Column, encuesta.Count);
            GraInfo.count_encuesta = encuesta.Count;
            GraInfo.column_calidad = formatearColumnas(calidad.Column, calidad.Count);
            GraInfo.count_calidad = calidad.Count;

            return View(GraInfo);
        }
        public string[] formatearColumnas(string[] column, int[] count) {
            // formateo de comlumnas de gráficos 
            int total = 0;
            foreach (var entero in count) {
                total += entero;
            }
            for (int i = 0; i < column.Length; i++) {
                float number = count[i];
                float percentage = (number / total);
                column[i] = column[i] +" (" + (int)(percentage * 100) + "%)";
                if (column[i] == "") column[i] = "Sin definir";
                if (column[i].Contains("\n"))
                column[i] = column[i].Replace("\r\n", "_");                
                ;
            }
            return column;
        }
        public ActionResult GridTickets(int EmployeeID, int pageNumber = 1)
        {
            ViewBag.user = EmployeeID;
            ViewBag.Rol = RoldeUsuario(EmployeeID);
            int pageSize = 5;
            int totalElements = 0;
            int totalPages = 0;

            ViewBag.pageNumber = pageNumber;

            var detalle = new DetalleSelectedTicketVm();
            // obtener tickets filtrados por grupo resolutor
            String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();

            List<VWDetalleTicket> dtoViewDetalle = new List<VWDetalleTicket>();

            if (ViewBag.Rol.Contains("Tecnico"))
            {
                //var idtecnico = _db.vwDetalleUsuario.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.Id).FirstOrDefault();
                var idtecnico = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.Id).FirstOrDefault();

                dtoViewDetalle = _db.VWDetalleTicket.
                    Where(pointer => 
                    (
                    (pointer.IdTecnicoAsignado      == idtecnico && pointer.IdTecnicoAsignadoReag  == null)  ||
                    (pointer.IdTecnicoAsignadoReag  == idtecnico && pointer.IdTecnicoAsignadoReag2 == null)  ||
                    (pointer.IdTecnicoAsignadoReag2 == idtecnico)
                    ) &&
                    pointer.GrupoResolutor == strGrupoResolutor
                    )
                    .OrderByDescending(pointer => pointer.Id)
                    .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                    .ToList();

                totalElements = _db.VWDetalleTicket.
                    Where(pointer =>
                    (
                    (pointer.IdTecnicoAsignado == idtecnico && pointer.IdTecnicoAsignadoReag == null) ||
                    (pointer.IdTecnicoAsignadoReag == idtecnico && pointer.IdTecnicoAsignadoReag2 == null) ||
                    (pointer.IdTecnicoAsignadoReag2 == idtecnico)
                    ) &&
                    pointer.GrupoResolutor == strGrupoResolutor
                    )
                    .Count();
            }
            else
            if (ViewBag.Rol.Contains("Supervisor"))
            {
                //dtoViewDetalle = _db.VWDetalleTicket.Where(t => t.GrupoResolutor == strGrupoResolutor).OrderByDescending(pointer => pointer.Id).ToList();

                dtoViewDetalle = _db.VWDetalleTicket
                    .Where(t => t.GrupoResolutor == strGrupoResolutor)
                    .OrderByDescending(pointer => pointer.Id)
                    .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                    .ToList();

                totalElements = _db.VWDetalleTicket
                    .Where(t => t.GrupoResolutor == strGrupoResolutor)
                    .Count();
            }

            totalPages = 1 + (totalElements / pageSize);
            ViewBag.totalPages = totalPages;

            detalle.ViewDetalleTickets = dtoViewDetalle;
            //Eliminar las horas antes de enviar los datos al grid 
            foreach (var ticket in detalle.ViewDetalleTickets)
            {
                var fechaSinHora = ticket.FechaRegistro;
                ticket.FechaRegistro = fechaSinHora.Date;

                ticket.TecnicoAsignado = (ticket.TecnicoAsignadoReag != null) ?  ticket.TecnicoAsignadoReag  : ticket.TecnicoAsignado;
                ticket.TecnicoAsignado = (ticket.TecnicoAsignadoReag2 != null) ? ticket.TecnicoAsignadoReag2 : ticket.TecnicoAsignado;
            }
            return View(detalle);
        }
        public ActionResult GridEncuesta(int EmployeeID, int pageNumber = 1)
        {
            string rol = RoldeUsuario(EmployeeID);
            ViewBag.user = EmployeeID;
            ViewBag.Rol = rol;
            ViewBag.pageNumber = pageNumber;
            int pageSize = 5;
            int totalElements = 0;
            int totalPages = 0;
            var detalle = new DetalleSelectedTicketVm();

            var encuesta = _db.EncuestaDetalle.Select(p => p.IdTicket).ToList();

            // obtener tickets (filtrar de ser necesario)
            List<VWDetalleTicket> dtoViewDetalle = new List<VWDetalleTicket>();
            if (rol == "ServiceDesk")
            {
                dtoViewDetalle = _db.VWDetalleTicket
                    .Where(t => encuesta.Contains(t.Id))
                    .OrderByDescending(pointer => pointer.Id)
                    .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                    .ToList();

                totalElements = _db.VWDetalleTicket
                    .Where(t => encuesta.Contains(t.Id))
                    .Count();
                ;
            }
            else
            {
                if (rol == "Tecnico")
                {
                    var resolutorId = getResolutorId(EmployeeID);
                    dtoViewDetalle = _db.VWDetalleTicket.Where(f =>
                        (f.IdTecnicoAsignadoReag2 == resolutorId) ||
                        (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == resolutorId) ||
                        (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == null && f.IdTecnicoAsignado == resolutorId) 
                        && encuesta.Contains(f.Id))                        
                        .OrderByDescending(f => f.Id)
                        .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                        .ToList();

                    totalElements = _db.VWDetalleTicket.Where(f =>
                        (f.IdTecnicoAsignadoReag2 == resolutorId) ||
                        (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == resolutorId) ||
                        (f.IdTecnicoAsignadoReag2 == null && f.IdTecnicoAsignadoReag == null && f.IdTecnicoAsignado == resolutorId)
                        && encuesta.Contains(f.Id))
                        .Count();
                }
                else // supervisores
                {
                    String strGrupoResolutor = _db.tbl_User.Where(x => x.EmpleadoID == EmployeeID).Select(x => x.GrupoResolutor).SingleOrDefault();
                    var ticketsDeGRes = _db.tbl_TicketDetalle.Where(x => x.GrupoResolutor == strGrupoResolutor).ToList().Select(d => d.Id); // lista de (int) tickets pertencientes a grupo resolutor
                    dtoViewDetalle = _db.VWDetalleTicket.Where(p => 
                        ticketsDeGRes.Contains(p.Id) && encuesta.Contains(p.Id)) 
                        .OrderByDescending(pointer => pointer.Id)
                        .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                        .ToList(); // obtener info de tickets de lista anterior
                    totalElements = _db.VWDetalleTicket.Where(p => 
                        ticketsDeGRes.Contains(p.Id) && encuesta.Contains(p.Id)) 
                        .Count();
                }
            }


            totalPages = 1 + (totalElements / pageSize);
            ViewBag.totalPages = totalPages;

            // Filtrar los tickets que no tienen encuesta
            //var dtoTEMP = new List<VWDetalleTicket>();
            //foreach (var ticket in dtoViewDetalle)
            //{
            //    if (encuesta.Contains(ticket.Id))
            //    {
            //        dtoTEMP.Add(ticket);
            //    }
            //}
            //dtoViewDetalle.Clear();
            //dtoViewDetalle = dtoTEMP;

            //Añadir resultados de encuesta a la lista de tickets
            var encuesta2 = _db.EncuestaDetalle.OrderBy(x => x.IdTicket).ToList();

            detalle.ViewDetalleTickets = dtoViewDetalle;
            //Eliminar las horas antes de enviar los datos al grid y añadir resultados de encuesta
            foreach (var ticket in detalle.ViewDetalleTickets)
            {
                var fechaSinHora = ticket.FechaRegistro;
                ticket.FechaRegistro = fechaSinHora.Date;
                foreach (var t in encuesta2)
                {
                    if (t.IdTicket == ticket.Id)
                    {
                        ticket.Extencion = t.CalificaServicio;
                        ticket.GrupoResolutor = t.Comentario;
                        break;
                    }
                }
            }
            return View(detalle);
        }
        // ----------- Descargar Excel Reportes
        public FileResult Descargar_Excel_Reportes(int EmployeeID, DateTime? fechaInicial, DateTime? fechaFinal)
        {
            // Conexión a excel
            SLDocument spreadsheet = new SLDocument();         // Crea una tabla
            MemoryStream ms = new MemoryStream();       // Crea un lugar para guardar la tabla antes de descargarla

            spreadsheet = add_Layout_Reportes(spreadsheet);
            spreadsheet = fill_Excel_Reportes(spreadsheet, fechaInicial, fechaFinal, EmployeeID);

            spreadsheet.SaveAs(ms); // preparar spreadsheet para descarga
            ms.Position = 0;        // resetear posición

            return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reportes.xlsx");
        }
        public SLDocument add_Layout_Reportes(SLDocument spreadsheet) // Agrega un Layout al excel proporcionado
        {
            // Crear Estilos
            var Sverd = spreadsheet.CreateStyle(); Sverd.FormatCode = "#,##0.00";
            Sverd.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#87FFAE"), System.Drawing.Color.Black); //verde
            var Samar = spreadsheet.CreateStyle(); Samar.FormatCode = "#,##0.00";
            Samar.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#FAEE95"), System.Drawing.Color.Black); //amarillo
            var Snara = spreadsheet.CreateStyle(); Snara.FormatCode = "#,##0.00";
            Snara.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#FFDAAE"), System.Drawing.Color.Black); //naranja
            var Sazul = spreadsheet.CreateStyle(); Sazul.FormatCode = "#,##0.00";
            Sazul.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#5CE6D8"), System.Drawing.Color.Black); //azul

            // Set Estilos
            for (int i = 1; i < 14; i++) { spreadsheet.SetCellStyle(2, i, Sverd); }
            spreadsheet.SetCellStyle(2, 3, Samar);
            spreadsheet.SetCellStyle(2, 11, Snara);
            spreadsheet.SetCellStyle(2, 12, Sazul);
            spreadsheet.SetCellStyle(2, 13, Sazul);
            spreadsheet.SetCellStyle(2, 14, Snara);
            spreadsheet.SetCellStyle(2, 15, Snara);
            spreadsheet.SetCellStyle(2, 16, Snara);
            spreadsheet.SetColumnWidth("A2", 8);
            spreadsheet.SetColumnWidth("C2", 30);
            spreadsheet.SetColumnWidth("D2", 8);
            spreadsheet.SetColumnWidth("F2", 30);
            spreadsheet.SetColumnWidth("G2", 15);
            spreadsheet.SetColumnWidth("H2", 15);
            spreadsheet.SetColumnWidth("J2", 30);
            spreadsheet.SetColumnWidth("K2", 30);
            spreadsheet.SetColumnWidth("O2", 15);
            spreadsheet.SetColumnWidth("P2", 45);

            // Escribir plantilla
            spreadsheet.SetCellValue("A2", "Ticket");
            spreadsheet.SetCellValue("B2", "Subticket");
            spreadsheet.SetCellValue("C2", "Area Solicitante");
            spreadsheet.SetCellValue("D2", "Estatus");
            spreadsheet.SetCellValue("E2", "Categoría");
            spreadsheet.SetCellValue("F2", "Subcategoría");
            spreadsheet.SetCellValue("G2", "Incidencia / Solicitud");
            spreadsheet.SetCellValue("H2", "Grupo Resolutor");
            spreadsheet.SetCellValue("I2", "Prioridad");
            spreadsheet.SetCellValue("J2", "Descripción");
            spreadsheet.SetCellValue("K2", "Tecnico Resolutor");
            spreadsheet.SetCellValue("L2", "Reasignaciones");
            spreadsheet.SetCellValue("M2", "Reaperturas");
            spreadsheet.SetCellValue("N2", "SLA objetivo (hr)");
            spreadsheet.SetCellValue("O2", "SLA total (hr)");
            return spreadsheet;
        }
        public SLDocument fill_Excel_Reportes(SLDocument spreadsheet, DateTime? fechaInici, DateTime? fechaFinal, int EmployeeId) // Arreglo de hijos hecho en Reportes repetir en Encuestas-------------------------
        {
            var detalle = new DetalleSelectedTicketVm();                    // tabla con datos de SLAs
            var vwReportes = new List<vw_DetalleReportes>();                // tabla con datos de tickets
            //string[] estados = _db.cat_EstadoTicket.Select(t => t.Estado).ToArray();
            //Filtro de  de Fechas
            string GrupoResolutor = _db.tbl_User.Where(t => t.EmpleadoID == EmployeeId).Select(t => t.GrupoResolutor).FirstOrDefault();
            DateTime fechaTemp; DateTime fechaO = DateTime.Today; DateTime fechaF = DateTime.Today;
            if (fechaInici != null) { fechaTemp = (DateTime)(fechaInici); fechaO = fechaTemp.Date; }
            if (fechaFinal != null) { fechaTemp = (DateTime)(fechaFinal); fechaF = fechaTemp.Date; }
            if (fechaFinal == null && fechaInici == null) { vwReportes = _db.vw_DetalleReportes.Where(f => f.GrupoResolutor == GrupoResolutor).OrderBy(c => c.Id).ToList(); }
            else
            if (fechaFinal != null && fechaInici == null) { vwReportes = _db.vw_DetalleReportes.Where(f => f.FechaRegistro <= fechaF && f.GrupoResolutor == GrupoResolutor).OrderBy(c => c.Id).ToList(); }
            else
            if (fechaFinal == null && fechaInici != null) { vwReportes = _db.vw_DetalleReportes.Where(f => f.FechaRegistro >= fechaO && f.GrupoResolutor == GrupoResolutor).OrderBy(c => c.Id).ToList(); }
            else
            if (fechaFinal != null && fechaInici != null)
            {
                fechaF = fechaF.AddDays(1);  vwReportes = _db.vw_DetalleReportes.Where(f => f.FechaRegistro >= fechaO && f.FechaRegistro <= fechaF && f.GrupoResolutor == GrupoResolutor).OrderBy(c => c.Id).ToList();
            }
            // SI PIDEN FILTRO DE TECNICO PONER AQUI


            // Variables temporales
            string tenicoQueCerro = null;
            string incidencia = null;
            string SLAobjetivo = null;
            string Estatus = null;
            int? subticket = 0;
            int? noReasignaciones = 0;
            int x = 0;     //  contador de tickets
            int row = 3;   //  Fila desde la cual iniciar a escribir
            int id;
            string virgulilla = "  ";
            string horasSLA = "", minSLA = "";
            int minSLAint = 0;
            int hashkeyPadre = 0, hashkeyHijo = 0;

            // Setup styles for modification, change formating
            var style = spreadsheet.CreateStyle();
            style.FormatCode = "#,##0.00";

            Hashtable ListadePadres = new Hashtable();

            //Verificar padres e hijos
            // key = son, value = father
            foreach (var ticket in vwReportes)
            {
                if (ticket.IdTicketPrincipal != null)
                {
                    //ListadePadres.Add(hashkeyPadre, ticket.IdTicketPrincipal);
                    ListadePadres.Add(ticket.Id, ticket.IdTicketPrincipal);
                    hashkeyHijo++;
                    hashkeyPadre++;
                }
            }

            // Escritura  
            foreach (var ticket in vwReportes)
            {

                // Obtener datos
                {
                    // Obtener tecnico que cerró el ticket
                    tenicoQueCerro = ticket.TecnicoAsignadoReag2;
                    if (tenicoQueCerro == null || tenicoQueCerro == "") tenicoQueCerro = ticket.TecnicoAsignadoReag;
                    if (tenicoQueCerro == null || tenicoQueCerro == "") tenicoQueCerro = ticket.TecnicoAsignado;

                    // Obtener datos: incidencia y SLAObjetivo
                    incidencia = ticket.Incidencia;
                    SLAobjetivo = ticket.SLAObjetivo;

                    // Obtener datos: SLA Total en minutos
                    {
                        id = vwReportes[x].Id;
                        detalle.detalle = _db.VWDetalleTicket.Where(t => t.Id == id).FirstOrDefault();
                        Estatus = detalle.detalle.Estatus;
                        var info = _db.his_Ticket.Where(a => a.IdTicket == id).ToList();
                        detalle.Slas = _sla.GetSlaTimes(info);
                        var hrSLAObj = ""; var minSLAObj = "";
                        foreach (var sLa in detalle.Slas)
                        {
                            if (sLa.Type == "SLA total")
                            {
                                // Si se tienen que agregar colores guardados en los SLA poner en esta linea
                                horasSLA = sLa.Time.ToString();
                            }
                        }
                        //minSLA    = horasSLA.Substring(0, horasSLA.Length - 3);
                        //minSLAint = Int32.Parse(minSLA) * 60; //elefante
                        hrSLAObj  = horasSLA.Split(':')[0];
                        minSLAObj = horasSLA.Split(':')[1];
                        minSLAint = (Int32.Parse(hrSLAObj) * 60) + Int32.Parse(minSLAObj); // manda tiempo en formato de minutos
                    }

                    // Obtener color para el style
                    style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml(vwReportes[x].Color), System.Drawing.Color.Black); //cambiar style                       
                }

                //Verificar si es padre, de serlo se imprime primero el padre y luego el hijo
                int flag = 0;
                if (ListadePadres.ContainsValue(ticket.Id))
                {
                    //Imprimir el padre
                    flag = 1;
                    ExcelDataReporteria NuevaColumna = new ExcelDataReporteria();
                    NuevaColumna.EmpleadoID = EmployeeId;
                    NuevaColumna.ticketID = ticket.Id;  
                    NuevaColumna.spreadsheet = spreadsheet;
                    NuevaColumna.row = row;
                    NuevaColumna.vwReportes = vwReportes;
                    NuevaColumna.subticket = subticket;
                    NuevaColumna.virgulilla = virgulilla;
                    NuevaColumna.x = x;
                    NuevaColumna.incidencia = incidencia;
                    NuevaColumna.tenicoQueCerro = tenicoQueCerro;
                    NuevaColumna.noReasignaciones = noReasignaciones;
                    NuevaColumna.SLAobjetivo = SLAobjetivo;
                    NuevaColumna.style = style;
                    NuevaColumna.minSLAint = minSLAint;
                    NuevaColumna.flag = flag;
                    NuevaColumna.Estatus = Estatus;
                    Llenar_Columna_Reportes(NuevaColumna);
                    //Llenar_Columna_Reportes(ticket.Id, spreadsheet, row, vwReportes, subticket, virgulilla, x, incidencia, tenicoQueCerro, noReasignaciones, SLAobjetivo, minSLAint, style, flag, Estatus);
                    row++;
                    // imprimir hijos
                    row = Imprimir_Hijos_Reportes(ticket.Id, spreadsheet, row, ListadePadres, EmployeeId);
                    flag = 0;
                }
                if (!ListadePadres.ContainsValue(ticket.Id) && !ListadePadres.ContainsKey(ticket.Id))
                {
                    flag = 0;
                    // Imprimir no padre no hijo
                    ExcelDataReporteria NuevaColumna = new ExcelDataReporteria();
                    NuevaColumna.EmpleadoID = EmployeeId;
                    NuevaColumna.ticketID = ticket.Id;
                    NuevaColumna.spreadsheet = spreadsheet;
                    NuevaColumna.row = row;
                    NuevaColumna.vwReportes = vwReportes;
                    NuevaColumna.subticket = subticket;
                    NuevaColumna.virgulilla = virgulilla;
                    NuevaColumna.x = x;
                    NuevaColumna.incidencia = incidencia;
                    NuevaColumna.tenicoQueCerro = tenicoQueCerro;
                    NuevaColumna.noReasignaciones = noReasignaciones;
                    NuevaColumna.SLAobjetivo = SLAobjetivo;
                    NuevaColumna.style = style;
                    NuevaColumna.minSLAint = minSLAint;
                    NuevaColumna.flag = flag;
                    NuevaColumna.Estatus = Estatus;
                    Llenar_Columna_Reportes(NuevaColumna);
                    //Llenar_Columna_Reportes(ticket.Id, spreadsheet, row, vwReportes, subticket, virgulilla, x, incidencia, tenicoQueCerro, noReasignaciones, SLAobjetivo, minSLAint, style, flag, Estatus);
                    row++;
                }

                // Resetear temporales
                x++;
                subticket = 0;
                noReasignaciones = 0;
                tenicoQueCerro = null;
                incidencia = null;
                SLAobjetivo = null;
                Estatus = null;
                horasSLA = "";
                minSLA = "";
                minSLAint = 0;
            }
            return spreadsheet;
        }
        private int Imprimir_Hijos_Reportes(int ticketID, SLDocument spreadsheet, int row, Hashtable listadePadres, int EmployeeId)
        {
            int PADRE = 0, HIJO = 0;
            var vwReportes = new List<vw_DetalleReportes>();       // tabla con datos de tickets
            var detalle = new DetalleSelectedTicketVm();           // tabla con datos de SLAs
            var style = spreadsheet.CreateStyle();
            style.FormatCode = "#,##0.00";

            //Instancias temporales
            string tenicoQueCerro = null;
            string incidencia = null;
            string SLAobjetivo = null;
            string Estatus = null;
            int? subticket = 0;
            int? noReasignaciones = 0;
            int x = 0;
            int id;
            string virgulilla = "  ";
            string horasSLA = "", minSLA = "";
            int minSLAint = 0;
            int hashkeyPadre = 0, hashkeyHijo = 0;
            foreach (DictionaryEntry relacion in listadePadres)
            {
                //definir Padre e Hijo en la relación
                PADRE = Int32.Parse(relacion.Value.ToString());
                HIJO = Int32.Parse(relacion.Key.ToString());
                x = 0;
                //Si el Padre es el ticketID
                if (PADRE == ticketID)
                {
                    // Obtener datos
                    {
                        vwReportes = _db.vw_DetalleReportes.Where(f => f.Id == HIJO).ToList();

                        // Obtener tecnico que cerró el ticket
                        tenicoQueCerro = vwReportes[0].TecnicoAsignadoReag2;
                        if (tenicoQueCerro == null || tenicoQueCerro == "") tenicoQueCerro = vwReportes[0].TecnicoAsignadoReag;
                        if (tenicoQueCerro == null || tenicoQueCerro == "") tenicoQueCerro = vwReportes[0].TecnicoAsignado;

                        // Obtener datos: incidencia y SLAObjetivo
                        incidencia = vwReportes[0].Incidencia;
                        SLAobjetivo = vwReportes[0].SLAObjetivo;

                        // Obtener datos: SLA Total en minutos
                        {
                            id = vwReportes[x].Id;
                            //detalle.detalle =  id);
                            detalle.detalle = _db.VWDetalleTicket.Where(t => t.Id == id).FirstOrDefault();

                            Estatus = detalle.detalle.Estatus;
                            var info = _db.his_Ticket.Where(a => a.IdTicket == id).ToList();
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
                        }

                        // Obtener color para el style
                        style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml(vwReportes[x].Color), System.Drawing.Color.Black); //cambiar style                       
                    }

                    ExcelDataReporteria NuevaColumna = new ExcelDataReporteria();
                    NuevaColumna.EmpleadoID = EmployeeId;
                    NuevaColumna.ticketID = PADRE;
                    NuevaColumna.spreadsheet = spreadsheet;
                    NuevaColumna.row = row;
                    NuevaColumna.vwReportes = vwReportes;
                    NuevaColumna.subticket = HIJO;
                    NuevaColumna.virgulilla = virgulilla;
                    NuevaColumna.x = x;
                    NuevaColumna.incidencia = incidencia;
                    NuevaColumna.tenicoQueCerro = tenicoQueCerro;
                    NuevaColumna.noReasignaciones = noReasignaciones;
                    NuevaColumna.SLAobjetivo = SLAobjetivo;
                    NuevaColumna.minSLAint = minSLAint;
                    NuevaColumna.style = style;
                    NuevaColumna.flag = 1;
                    NuevaColumna.Estatus = Estatus;
                    Llenar_Columna_Reportes(NuevaColumna);
                    //Llenar_Columna_Reportes(PADRE, spreadsheet, row, vwReportes, HIJO, virgulilla, x, incidencia, tenicoQueCerro, noReasignaciones, SLAobjetivo, minSLAint, style, 1, Estatus);
                    row++;
                    x++;
                }
            }
            return row;
        }
        


        public SLDocument Llenar_Columna_Reportes(ExcelDataReporteria datos)
        {

            datos.spreadsheet.SetCellValue("A" + datos.row.ToString(), datos.ticketID.ToString());                           // id ticket
            if (datos.colores == 1)
            {
                datos.style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                datos.spreadsheet.SetCellStyle("A" + datos.row.ToString(), datos.style);
            }

            if (datos.subticket != 0 && datos.subticket != null)
            {
                datos.style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                datos.spreadsheet.SetCellStyle("A" + datos.row.ToString(), datos.style);
                datos.spreadsheet.SetCellStyle("B" + datos.row.ToString(), datos.style);
                datos.spreadsheet.SetCellValue("B" + datos.row.ToString(), datos.subticket.ToString());                   // id del subticket             
            }
            else
                datos.spreadsheet.SetCellValue("B" + datos.row.ToString(), "");

            datos.spreadsheet.SetCellValue("C" + datos.row.ToString(), datos.vwReportes[datos.x].Area);                         // Area del solicitante

            //string estado = "";
            //switch (vwReportes[x].EstatusTicket)
            //{
            //    case 1: estado = "Abierto"; break;
            //    case 2: estado = "Asignado"; break;
            //    case 3: estado = "Trabajando"; break;
            //    case 4: estado = "Resuelto"; break;
            //    case 5: estado = "En Garantía"; break;
            //    case 6: estado = "Cerrado"; break;
            //    case 7: estado = "En Espera"; break;
            //    case 8: estado = "Cancelado"; break;
            //    default: estado = "A problem has occured"; break;

            //}
            datos.spreadsheet.SetCellValue("D" + datos.row.ToString(), datos.Estatus);                                     // Estatus Ticket 
            datos.spreadsheet.SetCellValue("E" + datos.row.ToString(), datos.vwReportes[datos.x].Categoria);                    // Categoria
            datos.spreadsheet.SetCellValue("F" + datos.row.ToString(), datos.vwReportes[datos.x].SubCategoria);                 // Subcategoría

            if (datos.incidencia != null)
                datos.spreadsheet.SetCellValue("G" + datos.row.ToString(), datos.incidencia);                             // Incidencia / Solicitante     
            else
                datos.spreadsheet.SetCellValue("G" + datos.row.ToString(), datos.virgulilla);

            datos.spreadsheet.SetCellValue("H" + datos.row.ToString(), datos.vwReportes[datos.x].GrupoResolutor);               // GrupoResolutor
            datos.spreadsheet.SetCellValue("I" + datos.row.ToString(), datos.vwReportes[datos.x].Prioridad);                    // Prioridad
            datos.spreadsheet.SetCellValue("J" + datos.row.ToString(), datos.vwReportes[datos.x].DescripcionIncidencia);        // Descripción

            if (datos.tenicoQueCerro != "" && datos.tenicoQueCerro != null)
                datos.spreadsheet.SetCellValue("K" + datos.row.ToString(), datos.tenicoQueCerro);                         // Tecnico que cerró el ticket  hay alguna manera de obtenerlo?
            else
                datos.spreadsheet.SetCellValue("K" + datos.row.ToString(), datos.virgulilla);

            if (datos.noReasignaciones != 0 && datos.noReasignaciones != null)
                datos.spreadsheet.SetCellValue("L" + datos.row.ToString(), datos.noReasignaciones.ToString());            // Numero de reasignaciones     
            else
                datos.spreadsheet.SetCellValue("L" + datos.row.ToString(), datos.virgulilla);

            if (datos.vwReportes[datos.x].NoReapertura > 0)
                datos.spreadsheet.SetCellValue("M" + datos.row.ToString(), datos.vwReportes[datos.x].NoReapertura.ToString());      // Numero de reaperturas 

            if (datos.SLAobjetivo != null)
                datos.spreadsheet.SetCellValue("N" + datos.row.ToString(), datos.SLAobjetivo.ToString() + ":00");                 // SLA Objetivo                 
            else
                datos.spreadsheet.SetCellValue("N" + datos.row.ToString(), datos.virgulilla);

            datos.spreadsheet.SetCellValue("O" + datos.row.ToString(), (datos.minSLAint / 60).ToString() + ":" + (datos.minSLAint % 60).ToString("00"));                        // SLA Total en horas    

            //style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml(vwReportes[x].Color), System.Drawing.Color.Black); //cambiar style
            //sl.SetCellStyle("P" + row.ToString(), style);                   //dar style a celda row de columna P
            //sl.SetCellValue("P" + row.ToString(), "");                                 // Estatus SLA global           ?

            //"https://localhost:44318/Reportes/DetalleTicket?IdTicket=" + ticketID.ToString());     // Liga Detalle de ticket  
            //"http://condor3752.startdedicated.com/Tecnico/DetalleTicket?IdTicket="
            //https://appext2.pentafon.com/ServiceDeskV2/Supervisor/DetalleTicket?IdTicket=
            //+ "&EmployeeId=" + datos.EmpleadoID

            //string url = "http://condor3752.startdedicated.com/Tecnico/DetalleTicket?IdTicket=";
            string url = "https://appext2.pentafon.com/ServiceDeskV2/Tecnico/DetalleTicket?IdTicket=";
            if (datos.subticket != 0 && datos.subticket != null)
                datos.spreadsheet.InsertHyperlink("P" + datos.row.ToString(), SLHyperlinkTypeValues.Url, url + datos.subticket.ToString() );
            else
                datos.spreadsheet.InsertHyperlink("P" + datos.row.ToString(), SLHyperlinkTypeValues.Url, url + datos.ticketID.ToString() );

            return datos.spreadsheet;
        }

        // ----------- Descargar Excel Encuestas
        public FileResult Descargar_Excel_Encuestas(int EmployeeID, DateTime? fechaInicial, DateTime? fechaFinal)
        {
            // Conexión a excel
            SLDocument spreadsheet = new SLDocument();         // Crea una tabla
            MemoryStream ms = new MemoryStream();       //Crea un lugar para guardar la tabla antes de descargarla

            spreadsheet = add_Layout_Encuestas(spreadsheet);
            spreadsheet = fill_Excel_Encuestas(spreadsheet, fechaInicial, fechaFinal);

            spreadsheet.SaveAs(ms); // preparar spreadsheet para descarga
            ms.Position = 0;        // resetear posición

            return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReportesEncuestas.xlsx");
        }
        public SLDocument add_Layout_Encuestas(SLDocument spreadsheet) // Agrega un Layout al excel proporcionado
        {
            // Crear Estilos
            var Sverd = spreadsheet.CreateStyle(); Sverd.FormatCode = "#,##0.00";
            Sverd.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#87FFAE"), System.Drawing.Color.Black); //verde
            var Samar = spreadsheet.CreateStyle(); Samar.FormatCode = "#,##0.00";
            Samar.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#FAEE95"), System.Drawing.Color.Black); //amarillo
            var Snara = spreadsheet.CreateStyle(); Snara.FormatCode = "#,##0.00";
            Snara.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#FFDAAE"), System.Drawing.Color.Black); //naranja
            var Sazul = spreadsheet.CreateStyle(); Sazul.FormatCode = "#,##0.00";
            Sazul.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#5CE6D8"), System.Drawing.Color.Black); //azul

            // Set Estilos
            for (int i = 1; i < 8; i++) { spreadsheet.SetCellStyle(2, i, Sverd); }
            for (int i = 8; i < 13; i++) { spreadsheet.SetCellStyle(2, i, Samar); }
            spreadsheet.SetColumnWidth("A2", 8);
            spreadsheet.SetColumnWidth("C2", 15);
            spreadsheet.SetColumnWidth("D2", 30);
            spreadsheet.SetColumnWidth("E2", 13);
            spreadsheet.SetColumnWidth("F2", 16);

            spreadsheet.SetColumnWidth("H2", 15);
            spreadsheet.SetColumnWidth("I2", 15);
            spreadsheet.SetColumnWidth("J2", 15);
            spreadsheet.SetColumnWidth("K2", 15);
            spreadsheet.SetColumnWidth("L2", 15);

            // Escribir plantilla
            spreadsheet.SetCellValue("A2", "Ticket");
            spreadsheet.SetCellValue("B2", "Subticket");
            spreadsheet.SetCellValue("C2", "Categoría");
            spreadsheet.SetCellValue("D2", "Subcategoría");
            spreadsheet.SetCellValue("E2", "Incidencia / Solicitud");
            spreadsheet.SetCellValue("F2", "Grupo Resolutor");
            spreadsheet.SetCellValue("G2", "Prioridad");
            spreadsheet.SetCellValue("H2", "Muy Insatisfecho");
            spreadsheet.SetCellValue("I2", "Insatisfecho");
            spreadsheet.SetCellValue("J2", "Neutral");
            spreadsheet.SetCellValue("K2", "Satisfecho");
            spreadsheet.SetCellValue("L2", "Muy Satisfecho");
            return spreadsheet;
        }
        public SLDocument fill_Excel_Encuestas(SLDocument spreadsheet, DateTime? fechaInici, DateTime? fechaFinal)
        {
            // Obtener datos de BD          
            var vwReportes = new List<vw_DetalleReportes>();
            var reportes_encuestados = new List<vw_DetalleReportes>();
            var encuest = new List<EncuestaDetalle>();

            //Formateo de Fechas (cambio de DateTime? a DateTime, poner horas a 12:00:00)
            DateTime fechaTemp;
            DateTime fechaO = DateTime.Today;
            DateTime fechaF = DateTime.Today;
            if (fechaInici != null) { fechaTemp = (DateTime)(fechaInici); fechaO = fechaTemp.Date; }
            if (fechaFinal != null) { fechaTemp = (DateTime)(fechaFinal); fechaF = fechaTemp.Date; }
            if (fechaFinal == null && fechaInici == null)
            {
                encuest = _db.EncuestaDetalle.ToList();
                vwReportes = _db.vw_DetalleReportes.ToList();
            }
            if (fechaFinal != null && fechaInici == null)
            {
                encuest = _db.EncuestaDetalle.Where(f => f.FechaRegistro <= fechaF).ToList();
                vwReportes = _db.vw_DetalleReportes.Where(f => f.FechaRegistro <= fechaF).ToList();
            }
            if (fechaFinal == null && fechaInici != null)
            {
                encuest = _db.EncuestaDetalle.Where(f => f.FechaRegistro >= fechaO).ToList();
                vwReportes = _db.vw_DetalleReportes.Where(f => f.FechaRegistro >= fechaO).ToList();
            }
            if (fechaFinal != null && fechaInici != null)
            {
                fechaF = fechaF.AddDays(1);
                encuest = _db.EncuestaDetalle.Where(f => f.FechaRegistro >= fechaO && f.FechaRegistro <= fechaF).ToList();
                vwReportes = _db.vw_DetalleReportes.Where(f => f.FechaRegistro >= fechaO && f.FechaRegistro <= fechaF).ToList();
            }

            // Variables temporales
            string incidencia = null;
            int? subticket = 0;
            int x = 0;     //  contador de tickets
            int row = 3;   //  Fila desde la cual iniciar a escribir
            int id; int calif = 0;
            string virgulilla = " ~ ";
            int hashkeyPadre = 0, hashkeyHijo = 0;

            // Setup styles for modification, change formating
            var style = spreadsheet.CreateStyle();
            style.FormatCode = "#,##0.00";

            Hashtable ListadePadres = new Hashtable();

            List<int> encuestados = new List<int>(); //obtener lista de encuestados
            foreach (var ticket in encuest) { encuestados.Add(ticket.IdTicket); }

            //quitar de Reportes los tickets no encuestados      //vwReportes trae ya filtrados los tickets que se supone tiene que ver usuario en cuestion
            foreach (var ticket in vwReportes) { 
                if (encuestados.Contains(ticket.Id)) { 
                    reportes_encuestados.Add(ticket); 
                }
            }
            vwReportes.Clear();
            vwReportes = reportes_encuestados;

            //Verificar padres e hijos          // key = son, value = father
            foreach (var ticket in vwReportes)
            {
                //if (encuestados.Contains(ticket.Id)) { }
                if (ticket.IdTicketPrincipal != null)
                {
                    ListadePadres.Add(ticket.Id, ticket.IdTicketPrincipal);
                    hashkeyHijo++;
                    hashkeyPadre++;
                }
            }

            // Obtención y Escritura  
            foreach (var ticket in vwReportes)
            {
                // Obtener datos
                {
                    // Obtener datos: incidencia y SLAObjetivo
                    incidencia = ticket.Incidencia;
                    // Obtener color para el style
                    style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml(vwReportes[x].Color), System.Drawing.Color.Black); //cambiar style
                    // Obtener la calificacion                                                                                                                                     Log^2!!!!
                    foreach (var tikt in encuest)
                    {
                        if (tikt.IdTicket == ticket.Id)
                        {
                            calif = 0;
                            switch (tikt.CalificaServicio)
                            {
                                case "Muy Insatisfecho": calif = 1; break;
                                case "Insatisfecho": calif = 2; break;
                                case "Neutral": calif = 3; break;
                                case "Satisfecho": calif = 4; break;
                                case "Muy Satisfecho": calif = 5; break;
                            }
                        }
                    }

                    int[] ticketsEncuestados = encuest.Select(t => t.IdTicket).ToArray();
                    if (ticketsEncuestados.Contains(ticket.Id)) {
                        //
                        Console.WriteLine();
                    }
                }

                //Escribir datos
                int flag = 0;
                if (ListadePadres.ContainsValue(ticket.Id))
                {
                    //Imprimir el padre
                    flag = 1;
                    ExcelDataReporteria NuevaColumna = new ExcelDataReporteria();
                    NuevaColumna.ticketID = ticket.Id;
                    NuevaColumna.spreadsheet = spreadsheet;
                    NuevaColumna.row = row;
                    NuevaColumna.vwReportes = vwReportes;
                    NuevaColumna.subticket = subticket;
                    NuevaColumna.virgulilla = virgulilla;
                    NuevaColumna.x = x;
                    NuevaColumna.incidencia = incidencia;
                    NuevaColumna.calif = calif;
                    NuevaColumna.style = style;
                    NuevaColumna.flag = flag;
                    Llenar_Columna_Encuestas(NuevaColumna);
                    //Llenar_Columna_Encuestas(ticket.Id, spreadsheet, row, vwReportes, subticket, virgulilla, x, incidencia, calif, style, flag);
                    row++;

                    NuevaColumna.row = row;
                    NuevaColumna.ListadePadres = ListadePadres;
                    row = Imprimir_Hijos_Encuestas(NuevaColumna);
                    //row = Imprimir_Hijos_Encuestas(ticket.Id, spreadsheet, row, vwReportes, virgulilla, x, incidencia, calif, style, flag, ListadePadres);
                    flag = 0;
                }
                if (!ListadePadres.ContainsValue(ticket.Id) && !ListadePadres.ContainsKey(ticket.Id))
                {
                    flag = 0;
                    // Imprimir no padre no hijo
                    ExcelDataReporteria NuevaColumna = new ExcelDataReporteria();
                    NuevaColumna.ticketID = ticket.Id;
                    NuevaColumna.spreadsheet = spreadsheet;
                    NuevaColumna.row = row;
                    NuevaColumna.vwReportes = vwReportes;
                    NuevaColumna.subticket = subticket;
                    NuevaColumna.virgulilla = virgulilla;
                    NuevaColumna.x = x;
                    NuevaColumna.incidencia = incidencia;
                    NuevaColumna.calif = calif;
                    NuevaColumna.style = style;
                    NuevaColumna.flag = flag;
                    Llenar_Columna_Encuestas(NuevaColumna);

                    //Llenar_Columna_Encuestas(ticket.Id, spreadsheet, row, vwReportes, subticket, virgulilla, x, incidencia, calif, style, flag);
                    row++;
                }

                // Resetear temporales
                x++;
                subticket = 0;
                incidencia = null;
            }
            return spreadsheet;
        }


        private int Imprimir_Hijos_Encuestas(ExcelDataReporteria datos)
        {
            int PADRE = 0, HIJO = 0;
            foreach (DictionaryEntry relacion in datos.ListadePadres)
            {

                //definir Padre e Hijo en la relación
                PADRE = Int32.Parse(relacion.Value.ToString());
                HIJO = Int32.Parse(relacion.Key.ToString());

                //Si el Padre es el ticketID
                if (PADRE == datos.ticketID)
                {
                    ExcelDataReporteria NuevaColumna = new ExcelDataReporteria();
                    NuevaColumna.ticketID = PADRE;
                    NuevaColumna.spreadsheet = datos.spreadsheet;
                    NuevaColumna.row = datos.row;
                    NuevaColumna.vwReportes = datos.vwReportes;
                    NuevaColumna.subticket = HIJO;
                    NuevaColumna.virgulilla = datos.virgulilla;
                    NuevaColumna.x = datos.x;
                    NuevaColumna.incidencia = datos.incidencia;
                    NuevaColumna.calif = datos.calif;
                    NuevaColumna.style = datos.style;
                    NuevaColumna.colores = datos.colores;
                    Llenar_Columna_Encuestas(NuevaColumna);

                    //Llenar_Columna_Encuestas(PADRE, spreadsheet, row, vwReportes, HIJO, virgulilla, x, incidencia, calif, style, colores);
                    datos.row++;
                }
            }
            return datos.row;
        }

        public SLDocument Llenar_Columna_Encuestas(ExcelDataReporteria datos)
        {
            datos.spreadsheet.SetCellValue("A" + datos.row.ToString(), datos.ticketID.ToString());                           // id ticket
            if (datos.colores == 1)
            {
                datos.style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                datos.spreadsheet.SetCellStyle("A" + datos.row.ToString(), datos.style);
            }

            if (datos.subticket != 0 && datos.subticket != null)
            {
                datos.style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                datos.spreadsheet.SetCellStyle("A" + datos.row.ToString(), datos.style);
                datos.spreadsheet.SetCellStyle("B" + datos.row.ToString(), datos.style);
                datos.spreadsheet.SetCellValue("B" + datos.row.ToString(), datos.subticket.ToString());                   // id del subticket             
            }
            else
                datos.spreadsheet.SetCellValue("B" + datos.row.ToString(), "");

            datos.spreadsheet.SetCellValue("C" + datos.row.ToString(), datos.vwReportes[datos.x].Categoria);
            datos.spreadsheet.SetCellValue("D" + datos.row.ToString(), datos.vwReportes[datos.x].SubCategoria);

            if (datos.incidencia != null)
                datos.spreadsheet.SetCellValue("E" + datos.row.ToString(), datos.incidencia);
            else
                datos.spreadsheet.SetCellValue("E" + datos.row.ToString(), datos.virgulilla);

            datos.spreadsheet.SetCellValue("F" + datos.row.ToString(), datos.vwReportes[datos.x].GrupoResolutor);               // GrupoResolutor
            datos.spreadsheet.SetCellValue("G" + datos.row.ToString(), datos.vwReportes[datos.x].Prioridad);                    // Prioridad

            switch (datos.calif)
            {
                case 1: datos.spreadsheet.SetCellValue("H" + datos.row.ToString(), 1); break;
                case 2: datos.spreadsheet.SetCellValue("I" + datos.row.ToString(), 1); break;
                case 3: datos.spreadsheet.SetCellValue("J" + datos.row.ToString(), 1); break;
                case 4: datos.spreadsheet.SetCellValue("K" + datos.row.ToString(), 1); break;
                case 5: datos.spreadsheet.SetCellValue("L" + datos.row.ToString(), 1); break;
                default: break;
            }

            return datos.spreadsheet;
        }

        public int getIdOfTicketResolutor(int ticketId) {
            int? usuario = 0;

            var ticket = _db.tbl_TicketDetalle.Where(c=> c.Id == ticketId).FirstOrDefault();

            usuario = (ticket.IdTecnicoAsignadoReag == null && ticket.IdTecnicoAsignadoReag2 == null) ? ticket.IdTecnicoAsignado: usuario;
            usuario = (ticket.IdTecnicoAsignadoReag != null && ticket.IdTecnicoAsignadoReag2 == null) ? ticket.IdTecnicoAsignadoReag : usuario;
            usuario = (ticket.IdTecnicoAsignadoReag != null && ticket.IdTecnicoAsignadoReag2 != null) ? ticket.IdTecnicoAsignadoReag2 : usuario;

            return (int)usuario;
        }
        public int getResolutorId(int EmployeeId)
        {
            var users = _db.tbl_User.Where(c => c.EmpleadoID == EmployeeId).Select(c => c.Id).FirstOrDefault();
            return users;
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
        public bool SkipResolutorFilter(int EmployeeID)
        {
            bool filtro = false;

            if (EmployeeID != 0)
            {
                if (RoldeUsuario(EmployeeID).Contains("ervice")) { filtro = true; }
                if (RoldeUsuario(EmployeeID).Contains("irectivo")) { filtro = true; }
            }
            else { 
                filtro = true;
            }

            return filtro;
        }
    }
}


/*          private int Imprimir_Hijos_Encuestas(int ticketID, SLDocument spreadsheet, int row, List<vw_DetalleReportes> vwReportes, string virgulilla, int x, string incidencia, int calif, SLStyle style, int? colores, Hashtable listadePadres)
        {
            int PADRE = 0, HIJO = 0;
            foreach (DictionaryEntry relacion in listadePadres)
            {

                //definir Padre e Hijo en la relación
                PADRE = Int32.Parse(relacion.Value.ToString());
                HIJO = Int32.Parse(relacion.Key.ToString());

                //Si el Padre es el ticketID
                if (PADRE == ticketID)
                {
                    ExcelDataReporteria NuevaColumna = new ExcelDataReporteria();
                    NuevaColumna.ticketID = PADRE;
                    NuevaColumna.spreadsheet = spreadsheet;
                    NuevaColumna.row = row;
                    NuevaColumna.vwReportes = vwReportes;
                    NuevaColumna.subticket = HIJO;
                    NuevaColumna.virgulilla = virgulilla;
                    NuevaColumna.x = x;
                    NuevaColumna.incidencia = incidencia;
                    NuevaColumna.calif = calif;
                    NuevaColumna.style = style;
                    NuevaColumna.colores = colores;
                    Llenar_Columna_Encuestas(NuevaColumna);

                    //Llenar_Columna_Encuestas(PADRE, spreadsheet, row, vwReportes, HIJO, virgulilla, x, incidencia, calif, style, colores);
                    row++;
                }
            }
            return row;
        }
 *  
 *  
 *  
 *  Llenar_Columna_Encuestas original
 *          public SLDocument Llenar_Columna_Encuestas(int ticketID, SLDocument spreadsheet, int row, List<vw_DetalleReportes> vwReportes, int? subticket, string virgulilla, int x, string incidencia, int calif, SLStyle style, int? colores)
        {
            spreadsheet.SetCellValue("A" + row.ToString(), ticketID.ToString());                           // id ticket
            if (colores == 1)
            {
                style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                spreadsheet.SetCellStyle("A" + row.ToString(), style);
            }

            if (subticket != 0 && subticket != null)
            {
                style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                spreadsheet.SetCellStyle("A" + row.ToString(), style);
                spreadsheet.SetCellStyle("B" + row.ToString(), style);
                spreadsheet.SetCellValue("B" + row.ToString(), subticket.ToString());                   // id del subticket             
            }
            else
                spreadsheet.SetCellValue("B" + row.ToString(), "");

            spreadsheet.SetCellValue("C" + row.ToString(), vwReportes[x].Categoria);
            spreadsheet.SetCellValue("D" + row.ToString(), vwReportes[x].SubCategoria);

            if (incidencia != null)
                spreadsheet.SetCellValue("E" + row.ToString(), incidencia);
            else
                spreadsheet.SetCellValue("E" + row.ToString(), virgulilla);

            spreadsheet.SetCellValue("F" + row.ToString(), vwReportes[x].GrupoResolutor);               // GrupoResolutor
            spreadsheet.SetCellValue("G" + row.ToString(), vwReportes[x].Prioridad);                    // Prioridad

            switch (calif)
            {
                case 1:
                    spreadsheet.SetCellValue("H" + row.ToString(), 1);
                    break;
                case 2:
                    spreadsheet.SetCellValue("I" + row.ToString(), 1);
                    break;
                case 3:
                    spreadsheet.SetCellValue("J" + row.ToString(), 1);
                    break;
                case 4:
                    spreadsheet.SetCellValue("K" + row.ToString(), 1);
                    break;
                case 5:
                    spreadsheet.SetCellValue("L" + row.ToString(), 1);
                    break;
                default:
                    break;
            }

            return spreadsheet;
        }

 * 
 * 
 * 
 * 
 * Llenar_Columna_Reportes original
 *         public SLDocument Llenar_Columna_Reportes(int ticketID, SLDocument spreadsheet, int row, List<vw_DetalleReportes> vwReportes, int? subticket, string virgulilla, int x, string incidencia, string tenicoQueCerro, int? noReasignaciones, string SLAobjetivo, int minSLAint, SLStyle style, int? colores, string Estatus)
        {

            spreadsheet.SetCellValue("A" + row.ToString(), ticketID.ToString());                           // id ticket
            if (colores == 1)
            {
                style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                spreadsheet.SetCellStyle("A" + row.ToString(), style);
            }

            if (subticket != 0 && subticket != null)
            {
                style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml("#E1FFB4"), System.Drawing.Color.Black);
                spreadsheet.SetCellStyle("A" + row.ToString(), style);
                spreadsheet.SetCellStyle("B" + row.ToString(), style);
                spreadsheet.SetCellValue("B" + row.ToString(), subticket.ToString());                   // id del subticket             
            }
            else
                spreadsheet.SetCellValue("B" + row.ToString(), "");

            spreadsheet.SetCellValue("C" + row.ToString(), vwReportes[x].Area);                         // Area del solicitante

            //string estado = "";
            //switch (vwReportes[x].EstatusTicket)
            //{
            //    case 1: estado = "Abierto"; break;
            //    case 2: estado = "Asignado"; break;
            //    case 3: estado = "Trabajando"; break;
            //    case 4: estado = "Resuelto"; break;
            //    case 5: estado = "En Garantía"; break;
            //    case 6: estado = "Cerrado"; break;
            //    case 7: estado = "En Espera"; break;
            //    case 8: estado = "Cancelado"; break;
            //    default: estado = "A problem has occured"; break;

            //}
            spreadsheet.SetCellValue("D" + row.ToString(), Estatus);                                     // Estatus Ticket 
            spreadsheet.SetCellValue("E" + row.ToString(), vwReportes[x].Categoria);                    // Categoria
            spreadsheet.SetCellValue("F" + row.ToString(), vwReportes[x].SubCategoria);                 // Subcategoría

            if (incidencia != null)
                spreadsheet.SetCellValue("G" + row.ToString(), incidencia);                             // Incidencia / Solicitante     
            else
                spreadsheet.SetCellValue("G" + row.ToString(), virgulilla);

            spreadsheet.SetCellValue("H" + row.ToString(), vwReportes[x].GrupoResolutor);               // GrupoResolutor
            spreadsheet.SetCellValue("I" + row.ToString(), vwReportes[x].Prioridad);                    // Prioridad
            spreadsheet.SetCellValue("J" + row.ToString(), vwReportes[x].DescripcionIncidencia);        // Descripción

            if (tenicoQueCerro != "" && tenicoQueCerro != null)
                spreadsheet.SetCellValue("K" + row.ToString(), tenicoQueCerro);                         // Tecnico que cerró el ticket  hay alguna manera de obtenerlo?
            else
                spreadsheet.SetCellValue("K" + row.ToString(), virgulilla);

            if (noReasignaciones != 0 && noReasignaciones != null)
                spreadsheet.SetCellValue("L" + row.ToString(), noReasignaciones.ToString());            // Numero de reasignaciones     
            else
                spreadsheet.SetCellValue("L" + row.ToString(), virgulilla);

            if (vwReportes[x].NoReapertura > 0)
                spreadsheet.SetCellValue("M" + row.ToString(), vwReportes[x].NoReapertura.ToString());      // Numero de reaperturas 

            if (SLAobjetivo != null)
                spreadsheet.SetCellValue("N" + row.ToString(), SLAobjetivo.ToString() + ":00");                 // SLA Objetivo                 
            else
                spreadsheet.SetCellValue("N" + row.ToString(), virgulilla);

            spreadsheet.SetCellValue("O" + row.ToString(), (minSLAint / 60).ToString() + ":" + (minSLAint % 60).ToString("00"));                        // SLA Total en horas    

            //style.Fill.SetPattern(PatternValues.Solid, System.Drawing.ColorTranslator.FromHtml(vwReportes[x].Color), System.Drawing.Color.Black); //cambiar style
            //sl.SetCellStyle("P" + row.ToString(), style);                   //dar style a celda row de columna P
            //sl.SetCellValue("P" + row.ToString(), "");                                 // Estatus SLA global           ?

            //spreadsheet.SetCellValue("P" + row.ToString(), "https://localhost:44318/Reportes/DetalleTicket?IdTicket=" + ticketID.ToString());     // Liga Detalle de ticket  
            if (subticket != 0 && subticket != null)
                spreadsheet.InsertHyperlink("P" + row.ToString(), SLHyperlinkTypeValues.Url, "http://condor3752.startdedicated.com/Tecnico/DetalleTicket?IdTicket=" + subticket.ToString());
            else
                spreadsheet.InsertHyperlink("P" + row.ToString(), SLHyperlinkTypeValues.Url, "http://condor3752.startdedicated.com/Tecnico/DetalleTicket?IdTicket=" + ticketID.ToString());

            return spreadsheet;
        }
 */

//    Ticket
//    Subticcket
//    Categoría
//    Subcategoría
//    Indicdencia solicitud
//    grupo resolutor
//    propridad
// muy insatisfecho     insatisfecho    neutral     satisfecho      muy satisfecho
//                                                      1
//                                          1
//                                                      1
