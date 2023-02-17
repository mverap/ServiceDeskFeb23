using ServiceDesk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ServiceDesk.Managers
{
    public class DashBoardManager
    {
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        public  string putEstatus(int Type, int IdTicket, string Motivo = "")
        {
            string result = "";
            var _estatus =   _db.cat_EstadoTicket.Where(x => x.Id == Type).FirstOrDefault().Estado;
           var ticket = _db.tbl_TicketDetalle.Where(x => x.Id == IdTicket).FirstOrDefault();
            ticket.Estatus = _estatus;
            ticket.EstatusTicket = Type;
            his_Ticket his = new his_Ticket();
            his.IdTicket = ticket.Id;
            his.EmpleadoID = ticket.EmpleadoID;
            his.TicketTercero = ticket.TicketTercero;
            his.Extencion = ticket.Extencion;
            his.NombreTercero = ticket.NombreTercero;
            his.Piso = ticket.Piso;
            his.EmailTercero = ticket.EmailTercero;
            his.ExtensionTercero = ticket.ExtensionTercero;
            his.Posicion = ticket.Posicion;
            his.NombreCompleto = ticket.NombreCompleto;
            his.Correo = ticket.Correo;
            his.Area = ticket.Area;
            his.Categoria = ticket.Categoria;
            his.Centro = ticket.Centro;
            his.SubCategoria = ticket.SubCategoria;
            his.DescripcionIncidencia = ticket.DescripcionIncidencia;
            his.PersonasAddNotificar = ticket.PersonasAddNotificar;
            his.GrupoResolutor = ticket.GrupoResolutor;
            his.Prioridad = ticket.Prioridad;
            his.Estatus = _estatus;
            his.EstatusTicket = Type;
            his.FechaRegistro = ticket.FechaRegistro;
            his.Historial = false;
            his.Motivo = Motivo;

            _db.his_Ticket.Add(his);
            int valida = _db.SaveChanges();
            if (valida > 0) { 
              return result = "OK";
            }
            return result = "ERROR";
        }
        
    }
}