using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using ServiceDesk.Models;
using ServiceDesk.ViewModels;

namespace ServiceDesk.Managers
{
    //============================================================================================================================================
    public class NotificacionesManager
    {
        private readonly ServiceDeskContext _sd = new ServiceDeskContext();
        private readonly MessengerManager _msg = new MessengerManager();
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<NotificacionVm> getNotificaciones(int employeeId)
        {
            var list = new List<NotificacionVm>();

            var nots = _sd.Notificaciones.Where(a => a.EmpleadoId == employeeId && a.Activo).OrderByDescending(x=> x.FechaRegistro).ToList();
            //var nots = _sd.Notificaciones.Where(a => a.Activo).ToList();
            foreach (var not in nots)
            {
                var dto = new NotificacionVm
                {
                    Id = not.Id,
                    Mensaje = not.Mensaje,
                    Motivo = not.Motivo,
                    Visto = not.Vista ? 1 : 2
                };

                var horas = not.FechaRegistro.ToString("HH:mm");
                var dia = not.FechaRegistro.ToString("dd'/'MM'/'yyyy", new CultureInfo("es-ES"));

                dto.Fecha = dia + " " + horas + "hrs";

                if (not.Vista)
                {
                    dto.Color = "#878F97";
                }
                else
                {
                    dto.Color = "#007CFF";
                }

                list.Add(dto);
            }

            return list;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void UpdNotificacion(int NotiId)
        {

            var notificacion = _sd.Notificaciones.Find(NotiId);
            using (var con = new ServiceDeskContext())
            {
                con.Notificaciones.Attach(notificacion);
                notificacion.Vista = true;
                con.SaveChanges();
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void DelNotificacion(int NotiId)
        {

            var notificacion = _sd.Notificaciones.Find(NotiId);
            using (var con = new ServiceDeskContext())
            {
                con.Notificaciones.Attach(notificacion);
                notificacion.Activo = false;
                con.SaveChanges();
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotificaciones(his_Ticket his)
        {

            if (his.EstatusTicket == 1)
            {

                //TICKET ABIERTO 
                //DEBE NOTIFICARSE A SERVICE DESK

                SetNotiAbierto(his);
            }
            if (his.EstatusTicket == 2)
            {
                //TICKET ASIGNADO
                //DEBE REPORTARSE A USUARIO

                SetNotiAsignado(his);
            }
            var cambioestatus = new List<int> { 3, 4, 5, 6, 7 };
            if (cambioestatus.Contains(his.EstatusTicket))
            {
                //EL TICKET AH CAMBIADO DE ESTATUS
                //NOTIFICAR CAMBIO DE ESTATUS A USUARIO

                SetNotiCambioEstatus(his);
            }
            if (his.EstatusTicket == 8)
            {
                //TICKET CANCELADO
                //REPORTAR A SOLICITANTE

                SetNotiCancelado(his);
            }



        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public int GetCurrentTecnicoByTicketId(int idTicket)
        {

            var ticket = _sd.tbl_TicketDetalle.Find(idTicket);

            var noemp = 0;
            if (ticket.TecnicoAsignadoReag2 != null)
            {
                var agente = _sd.tbl_User.FirstOrDefault(a => a.Id == ticket.IdTecnicoAsignadoReag2);
                noemp = agente.EmpleadoID;
            }
            else if (ticket.TecnicoAsignadoReag != null)
            {
                var agente = _sd.tbl_User.FirstOrDefault(a => a.Id == ticket.IdTecnicoAsignadoReag);
                noemp = agente.EmpleadoID;
            }
            else
            {
                var agente = _sd.tbl_User.FirstOrDefault(a => a.Id == ticket.IdTecnicoAsignado);
                noemp = agente.EmpleadoID;
            }

            return noemp;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public int GetCurrentTecnicoByHis(his_Ticket his)
        {

            var ticket = _sd.tbl_TicketDetalle.Find(his.IdTicket);

            var noemp = 0;
            if (his.TecnicoAsignadoReag2 != null)
            {
                var agente = _sd.tbl_User.FirstOrDefault(a => a.Id == ticket.IdTecnicoAsignadoReag2);
                noemp = agente.EmpleadoID;
            }
            else if (his.TecnicoAsignadoReag != null)
            {
                var agente = _sd.tbl_User.FirstOrDefault(a => a.Id == ticket.IdTecnicoAsignadoReag);
                noemp = agente.EmpleadoID;
            }
            else if (his.TecnicoAsignado != null)//AYB
            {
                var agente = _sd.tbl_User.FirstOrDefault(a => a.Id == ticket.IdTecnicoAsignado);
                noemp = agente.EmpleadoID;
            }
            else
            {
                noemp = 0;//AYB
                
            }

            return noemp;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public int GetPastTecnicoByHis(his_Ticket his)
        {

            var ticket = _sd.tbl_TicketDetalle.Find(his.IdTicket);

            var noemp = 0;
            var tecnicos = new List<int>();

            if (his.TecnicoAsignadoReag2 != null)
            {
                var agente = _sd.tbl_User.FirstOrDefault(a => a.Id == ticket.IdTecnicoAsignadoReag2);
                tecnicos.Add(agente.EmpleadoID);
            }
            else if (his.TecnicoAsignadoReag != null)
            {
                var agente = _sd.tbl_User.FirstOrDefault(a => a.Id == ticket.IdTecnicoAsignadoReag);
                tecnicos.Add(agente.EmpleadoID);
            }
            else
            {
                var agente = _sd.tbl_User.FirstOrDefault(a => a.Id == ticket.IdTecnicoAsignado);
                tecnicos.Add(agente.EmpleadoID);
            }

            var totaltecs = tecnicos.Count();
            if (totaltecs > 1)
            {
                noemp = tecnicos[totaltecs - 2];
            }
            else
            {
                noemp = tecnicos[0];
            }

            return noemp;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public int GetCurrentSupervisorByHis(his_Ticket his)
        {
            int super = 0;

            var users = _sd.tbl_User.FirstOrDefault(a =>  a.GrupoResolutor == his.GrupoResolutor && a.Rol == "Supervisor");
            if (users != null)
            {
                super = users.EmpleadoID;
            }
            return super;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void CrearNotificacion(string motivo, string mensaje, int empleadoid)
        {

            var noti = new Notificaciones
            {
                Motivo = motivo,
                Mensaje = mensaje,
                EmpleadoId = empleadoid,
                FechaRegistro = DateTime.Now,
                Activo = true,
                Vista = false
            };

            _sd.Notificaciones.Add(noti);
            _sd.SaveChanges();

            SendEmailByEmployeeId(empleadoid, mensaje);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiAbierto(his_Ticket his)
        {
            //-------------------------------- EN DESUSO????? verificar, para recategorización parece no usarse
            //TICKET ABIERTO 
            //DEBE NOTIFICARSE A SERVICE DESK

            //VALIDAR SI ES RECATEGORIA 
            var prev = _sd.his_Ticket.Where(a => a.IdTicket == his.IdTicket).ToList();
            if (prev.Count > 0)
            {
                var last = prev.OrderBy(a => a.IdHis).Last();
                if (last.GrupoResolutor != his.GrupoResolutor)
                {
                    //RECATEGORIZACION DEL TICKET

                    string msj = "Se ha generado un nuevo ticket con ID " + his.IdTicket +
                        " se recategorizó y se envió al grupo resolutor adecuado para resolver tu incidencia.";

                    CrearNotificacion("Ticket Recategorizado", msj, his.EmpleadoID);

                    //supervisor del grupo
                    var grup = _sd.tbl_User.Where(a => a.GrupoResolutor==his.GrupoResolutor && a.Rol== "Supervisor").FirstOrDefault();

                    if (grup !=null)
                    {
                        string msj2 = "Se ha generado un nuevo ticket con ID " + his.IdTicket +
                        " se recategorizó y se envió a tu grupo resolutor, Es importante la asignación de este ticket.";

                        CrearNotificacion("Ticket Recategorizado", msj2, grup.EmpleadoID);
                    }

                    //Tecnico
                    var tec = _sd.tbl_User.Where(a => a.NombreTecnico == his.TecnicoAsignado).FirstOrDefault();

                    if (tec != null)
                    {
                        string msj2 = "Se ha generado un nuevo ticket con ID " + his.IdTicket +
                        " se recategorizó y se envió al grupo resolutor adecuado para resolver la incidencia.";

                        CrearNotificacion("Ticket Recategorizado", msj2, tec.EmpleadoID);
                    }


                }
                else
                {
                    //Notificamos a Supervisor de creacion de Ticket
                    var grup = _sd.tbl_User.Where(a => a.GrupoResolutor == his.GrupoResolutor && a.Rol == "Supervisor").FirstOrDefault();

                    if (grup != null)
                    {
                        string msj = "Se ha generado un nuevo ticket con ID " + his.IdTicket + " en estatus abierto. " +
                                   "Es importante la asignación de este ticket.";

                        CrearNotificacion("Ticket Abierto", msj, grup.EmpleadoID);
                    }
                }
            }
            else
            {
                string msj = "Se ha generado un nuevo ticket con ID " + his.IdTicket + " en estatus abierto. " +
                                   "Es importante la asignación de este ticket.";

                CrearNotificacion("Ticket Abierto", msj, 0);
            }



        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiAsignado(his_Ticket his)
        {
            //TICKET ASIGNADO
            //DEBE REPORTARSE A USUARIO

            var prev = _sd.his_Ticket.Where(a => a.IdTicket == his.IdTicket && a.EstatusTicket == 2).ToList();
            if (prev.Count > 0)
            {
                var last = prev.OrderBy(a => a.IdHis).Last();
                if (last.TecnicoAsignado != his.TecnicoAsignado)
                {
                    //TICKET REASIGNADO
                    //NOTIFICACION SOLICITANTE
                    string msj = "El ticket con ID " + his.IdTicket + " se reasigno a otro técnico.";

                    CrearNotificacion("Ticket Reasignado", msj, his.EmpleadoID);

                    //NOTIFICACION TECNICO ANTERIOR
                    msj = "El ticket con ID " + his.IdTicket + " se reasigno a otro técnico.";
                    var anttec = GetPastTecnicoByHis(his);

                    CrearNotificacion("Ticket Reasignado", msj, anttec);

                    //Avisamos a supervisor
                    var grup = _sd.tbl_User.Where(a => a.GrupoResolutor == his.GrupoResolutor && a.Rol == "Supervisor").FirstOrDefault();

                    if (grup != null)
                    {
                        var msj2 = "El ticket con ID " + his.IdTicket + " se reasigno a otro técnico.";

                        CrearNotificacion("Ticket Reasignado", msj2, grup.EmpleadoID);
                    }
                }
                else
                {
                    //Notificamos a Supervisor de creacion de Ticket
                    var grup = _sd.tbl_User.Where(a => a.GrupoResolutor == his.GrupoResolutor && a.Rol == "Supervisor").FirstOrDefault();

                    if (grup != null)
                    {
                      var  msj = "El ticket con ID " + his.IdTicket + " se reasigno a otro técnico.";

                        CrearNotificacion("Ticket Reasignado", msj, grup.EmpleadoID);
                    }
                }
            }
            else
            {
                string msj = "El ticket con ID " + his.IdTicket +
                              " está en estatus asignado. Puedes ver los comentarios del técnico en el detalle del ticket.";

                CrearNotificacion("Ticket Asignado", msj, his.EmpleadoID);
            }

            //NOTIFICACION DE TECNICO 
            string tecmsj = "El ticket con ID " + his.IdTicket +
                             " está en estatus asignado. Es importante tu colaboración para resolver este ticket.";
            var tecnico = GetCurrentTecnicoByHis(his);

            CrearNotificacion("Ticket Asignado", tecmsj, tecnico);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiCambioEstatus(his_Ticket his)
        {

            //EL TICKET AH CAMBIADO DE ESTATUS
            //NOTIFICAR CAMBIO DE ESTATUS A USUARIO
            string msj = "";
            string motivo = "";

            if (his.EstatusTicket == 3)
            {
                //TRABAJANDO
                motivo = "Ticket Trabajando";
                msj = "El ticket con ID " + his.IdTicket + " está en estatus trabajando.";
                CrearNotificacion(motivo, msj, his.EmpleadoID);
            }
            else if (his.EstatusTicket == 4)
            {
                //RESUELTO
                //NOTIFICAR A USUARIO
                
                motivo = "Ticket Resuelto";
                msj = "El ticket con ID " + his.IdTicket + " está en estatus resuelto. Por favor aprueba la solución de tu ticket.";
                CrearNotificacion(motivo, msj, his.EmpleadoID);

            }
            else if (his.EstatusTicket == 5)
            {
                //EN GARANTIA
                motivo = "Ticket en Garantía";
                msj = "El ticket con ID " + his.IdTicket + " está en estatus en garantía. Puedes reabrir el ticket si tu problema persiste.";
                CrearNotificacion(motivo, msj, his.EmpleadoID);

                //NOTIFICAR A TECNICO
                SetNotiAceptacionResuelto(his);
            }
            else if (his.EstatusTicket == 6)
            {
                //CERRADO
                //NOTIFICADO A SOLICITANTE
                motivo = "Ticket Cerrado";
                msj = "El ticket con ID " + his.IdTicket + " está en estatus cerrado. Por favor contesta nuestra encuesta de satisfacción.";
                CrearNotificacion(motivo, msj, his.EmpleadoID);

                //NOTIFICACION A TECNICO
                motivo = "Ticket Cerrado";
                msj = "El ticket con ID " + his.IdTicket + " está en estatus cerrado";
                var tecnico = GetCurrentTecnicoByHis(his);

                CrearNotificacion(motivo, msj, tecnico);
            }
            else if (his.EstatusTicket == 7)
            {
                //EN ESPERA
                motivo = "Ticket en Espera";
                msj = "El ticket con ID " + his.IdTicket + " está en estatus en espera.";
                CrearNotificacion(motivo, msj, his.EmpleadoID);
            }

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiCancelado(his_Ticket his)
        {


            //TICKET CANCELADO 
            //DEBE NOTIFICARSE A USUARIO

            string msj = "Se ha generado un nuevo ticket con ID " + his.IdTicket + " en estatus cancelado. " +
                    "Puedes ver el motivo de cancelación en el detalle del ticket.";

            CrearNotificacion("Ticket Cancelado", msj, his.EmpleadoID);

            //DEBE NOTIFICARSE A TECNICO
            msj = "El ticket con ID " + his.IdTicket + " está en estatus cancelado.";
            var tecnico = GetCurrentTecnicoByHis(his);
            CrearNotificacion("Ticket Cancelado", msj, tecnico);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiEditado(his_Ticket his)
        {

            //TICKET EDITADO
            //SE NOTIFICA A TECNICO
            string msj = "El ticket con ID " + his.IdTicket + " fue editado por el usuario.";
            var teniconum = GetCurrentTecnicoByHis(his);

            CrearNotificacion("Ticket Editado", msj, teniconum);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiRechazoResuelto(his_Ticket his)
        {

            //TICKET RECHAZO DE APROBACION DE RESUELTO
            //NOTIFICAR A TECNICO

            string msj = "La solución del ticket con ID " + his.IdTicket + " fue rechazada. Puedes ver el motivo del rechazo en el detalle del ticket.";
            var teniconum = GetCurrentTecnicoByHis(his);

            CrearNotificacion("Solución de ticket rechazada", msj, teniconum);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiAceptacionResuelto(his_Ticket his)
        {

            //TICKET APROBACION DE RESUELTO
            //NOTIFICAR A TECNICO

            string msj = "El ticket ID " + his.IdTicket + " está en estatus en garantía";
            var teniconum = GetCurrentTecnicoByHis(his);

            CrearNotificacion("Ticket en Garantia", msj, teniconum);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiAprobaReasignacion(his_Ticket his)
        {

            //TICKET APROBACION DE REASIGNACION
            //NOTIFICAR A TECNICO

            string msj = "El ticket ID " + his.IdTicket + " se reasigno a otro técnico.";
            var teniconum = GetPastTecnicoByHis(his);

            CrearNotificacion("Solicitud de reasignación aprobada", msj, teniconum);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiRechazoReasignacion(int idTicket)
        {

            //TICKET RECHAZO DE REASIGNCION DE TICKET
            //NOTIFICAR A TECNICO

            string msj = "La solución del ticket con ID " + idTicket + " fue rechazada. Puedes ver el motivo del rechazo en el detalle del ticket.";
            var teniconum = GetCurrentTecnicoByTicketId(idTicket);

            CrearNotificacion("Solución de ticket rechazada", msj, teniconum);

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiSolicitudReasignacion(his_Ticket his)
        {
            //TICKET SOLICITUD DE REASIGNACION
            //NOTIFICAR A SUPERVISOR

            string msj = "Se solicito la reasignación del ticket " + his.IdTicket + " Es importante tu confirmación.";
            var super = GetCurrentSupervisorByHis(his);

            CrearNotificacion("Solicitud de reasignación de ticket", msj, super);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiSlaPorVencer(his_Ticket his)
        {
            //TICKET SLA POR VENNCER
            //NOTIFICAR A SUPERVISOR

            string msj = "El tiempo de resolución del ticket " + his.IdTicket + " está por vencer.";
            var super = GetCurrentSupervisorByHis(his);

            CrearNotificacion("Ticket con SLA por vencer", msj, super);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiSlaVencido(his_Ticket his)
        {
            //TICKET SLA VENCIDO
            //NOTIFICAR A SUPERVISOR


            string msj = "El tiempo de resolución del ticket " + his.IdTicket + " está vencido.";
            var super = GetCurrentSupervisorByHis(his);

            CrearNotificacion("Ticket con SLA vencido", msj, super);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiMaximoReAsignaciones(his_Ticket his)
        {
            //TICKET NUMERO MAXIMO DE REASIGNACIONES
            //NOTIFICAR A SUPERVISOR
            var tecnico = GetCurrentTecnicoByHis(his);
            var agente = _sd.tbl_User.FirstOrDefault(a => a.EmpleadoID == tecnico);

            string msj = "El técnico " + agente.NombreTecnico + " ha solicitado 5 seasignaciones esta semana.";
            var super = GetCurrentSupervisorByHis(his);

            CrearNotificacion("Exceso de solicitudes de reasignación", msj, super);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiRechazoMaxAprobacionesTec(his_Ticket his)
        {
            //TICKET NUMERO MAXIMO DE REASIGNACIONES
            //NOTIFICAR A SUPERVISOR
            var tecnico = GetCurrentTecnicoByHis(his);
            var agente = _sd.tbl_User.FirstOrDefault(a => a.EmpleadoID == tecnico);

            string msj = "Las soluciones del técnico " + agente.NombreTecnico + " han sido rechazadas 5 veces esta semana.";
            var super = GetCurrentSupervisorByHis(his);

            CrearNotificacion("Exceso de soluciones rechazadas", msj, super);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetNotiRecategorizacion(his_Ticket his) {
            //RECATEGORIZACION DEL TICKET

            //Enviar notif al creador del ticket
            string msj = "Se ha generado un nuevo ticket con ID " + his.IdTicket 
                + " se recategorizó y se envió al grupo resolutor adecuado para resolver tu incidencia.";
            CrearNotificacion("Ticket Recategorizado", msj, his.EmpleadoID);

            //Enviar notif a supervisores del grupo resolutor

            msj = "Se ha generado un nuevo ticket con ID " + his.IdTicket 
                + " se recategorizó y se envió a tu grupo resolutor, Es importante la asignación de este ticket.";
            //Traer todos los supervisores y servicedesk del nuevo grupo resolutor (his ya trae la info actualizada)
            var GrupoResolutor = _sd.tbl_User.Where(t => 
                t.GrupoResolutor == his.GrupoResolutor && 
                (t.Rol == "Supervisor" || t.Rol == "Service Desk" || t.Rol == "ServiceDesk")
                ).Select(t => t.EmpleadoID).ToArray();
            ;
            foreach (var Supervisor in GrupoResolutor) 
            { CrearNotificacion("Ticket Recategorizado", msj, Supervisor); }
        }


        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SendEmailByEmployeeId(int employeeId, string message)
        {

            var usuario = _sd.tbl_User.Where(a=>a.EmpleadoID==employeeId).FirstOrDefault();
            if (usuario != null)
            {
                var email = usuario.Correo;
                _msg.SendEmail("Notificacion", "", message, email, new List<string>());
            }

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    }
    //============================================================================================================================================
}