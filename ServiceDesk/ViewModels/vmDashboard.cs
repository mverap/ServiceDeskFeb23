﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceDesk.Models; 

namespace ServiceDesk.ViewModels
{

    public class vmDashboard
    {
        public string type { get; set; }
        public List<ticket> Tickets = new List<ticket>();

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class ticket
    {
        public int noTicket { get; set; }
        public string categoria { get; set; }
        public string subcategoria { get; set; }
        public string GrupoResolutor { get; set; }
        public string tiempoTranscurrido { get; set; }
        public string minutes { get; set; }
        public string hours { get; set; }
        public string Estatus { get; set; }
        public string Color { get; set; }
        public bool subticket { get; set; } 

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class detailTicket
    {
        public tbl_TicketDetalle ticket = new tbl_TicketDetalle();
        //public List<hisDetail> his = new List<hisDetail>();
        public List<his_Ticket> his { get; set; }

        //CODE BY THE GLORY EMPEROR WILL
        public List<SlaTimesVm> Slas { get; set; }
        public VWDetalleTicket detalle { get; set; }
        public List<tblDocumentos> Docs { get; set; }
        public string type { get; set; }
        public string Centro { get; set; }
        public string Subcategoria { get; set; }
        public string Categoria { get; set; }
        public string horas_sla { get; set; }
        public string minutes { get; set; }
        public string hours { get; set; }
        public string EmployeeidBO { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    //public class SlaTimesVm
    //{
    //    public string Time { get; set; }
    //    public int Order { get; set; }
    //    public string Type { get; set; }
    //    public bool Enable { get; set; }
    //    public string Color { get; set; }
    //    public string Tecnico { get; set; }
    //}
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class hisDetail
    {
        public string nombre { get; set; }
        public DateTime fechaReg { get; set; }
        public string correo { get; set; }
        public string estatus { get; set; }
        public bool? file { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class vmDashbordResolutor
    {
        public int totTicketsVinculados { get; set; }
        public List<ticketByEstado> lstTicket = new List<ticketByEstado>();
        public List<ticketResumenResolutor> lstResumenResolutor = new List<ticketResumenResolutor>();
        public List<vw_TareasProgramadas> listaTareas = new List<vw_TareasProgramadas>();

        //AYB
        public string RolNameBO { get; set; }
    }

    public class ticketByEstado
    {
        public string estado { get; set; }
        public int total { get; set; }
        public int orden { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class ticketResumenResolutor
    {
        // Campo --------------------------------   // tabla Origen
        public int tickedID { get; set; }           // vw ticket 
        public string categoria { get; set; }       // vw ticket
        public string prioridad { get; set; }       // vw ticket

        // if prioridad = "Alto" then idPrioridad = 1... "Medio" = 2, "Baja" = 3
        public int idPrioridad { get; set; }        // HARD CODED
        public string estatus { get; set; }         // vw ticket
        public bool checkVincular { get; set; }     // ? check, always false?
        public int totVinculados { get; set; }      // 
        public bool isPadre { get; set; }           //
        public bool isSubTicket { get; set; }       //
        public int idTicketPadre { get; set; }      //
        public int idSubTicket { get; set; }        //
        public int orden { get; set; }              //
        public int? EmployeeAsignado { get; set; }  // new vw ticket
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
}