using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceDesk.Models;

namespace ServiceDesk.ViewModels
{
    //============================================================================================================================================
    public class AppVm
    {        
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class NotificacionVm {
        public int Id { get; set; } 
        public string Motivo { get; set; }
        public string Mensaje { get; set; } 
        public string Fecha { get; set; }
        public string Color { get; set; }     
        public int Visto { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class NotificacionesVm {
        public int NumeroNotificaciones { get; set; }
        public List<NotificacionVm> Notificaciones { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    //============================================================================================================================================
}