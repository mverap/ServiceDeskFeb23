

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceDesk.Models;
using System.Web.Mvc;
using System.Data.Entity.Migrations;
//
using System.Data;
using System.Web.Security;
using System.Data.SqlClient;
using ServiceDesk.ViewModels;

namespace ServiceDesk.Managers
{
    public class ServiceDeskManager
    {
        //==================================================================================================================
        private readonly RHAdminContext _rh = new RHAdminContext();
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private readonly NotificacionesManager _noti = new NotificacionesManager();
        //==================================================================================================================
        //ESTATUS DE TICKET
        //1	Abierto
        //2	Asignado
        //3	Trabajando
        //4	Resuelto
        //5	En Garantía
        //6	Cerrado
        //7	En Espera
        //8	Cancelado
        //==================================================================================================================
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //Guarda Ticket        
        public int SetTicket(tbl_TicketDetalle vm)
        {

            try
            {

                vm.FechaRegistro = DateTime.Now;
                vm.Estatus = "Abierto";
                vm.EstatusTicket = 1;
                vm.NoReapertura = 0;
                vm.NoReasignaciones = 0;

                _db.tbl_TicketDetalle.Add(vm);
                var res = _db.SaveChanges();

                if (res != 0)
                {

                    //Guarda en historico
                    SaveHistorico(vm);
                    return vm.Id;

                }
                else
                {

                    return 0;
                }

            }
            catch (Exception e)
            {

                return 0;
            }

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SaveHistorico(tbl_TicketDetalle vm)
        {
            //HISTORICO QUE GUARDA EN CREACION DE TICKET

            var dto = new his_Ticket();
            dto.IdTicket = vm.Id;
            dto.EmpleadoID = vm.EmpleadoID;
            dto.TicketTercero = vm.TicketTercero;
            dto.Extencion = vm.Extencion;
            dto.NombreTercero = vm.NombreTercero;
            dto.Piso = vm.Piso;
            dto.EmailTercero = vm.EmailTercero;
            dto.ExtensionTercero = vm.ExtensionTercero;
            dto.Posicion = vm.Posicion;
            dto.NombreCompleto = vm.NombreCompleto;
            dto.Correo = vm.Correo;
            dto.Area = vm.Area;
            dto.Categoria = vm.Categoria;
            dto.Centro = vm.Centro;
            dto.SubCategoria = vm.SubCategoria;
            dto.DescripcionIncidencia = vm.DescripcionIncidencia;
            dto.PersonasAddNotificar = vm.PersonasAddNotificar;
            dto.GrupoResolutor = vm.GrupoResolutor;
            dto.Prioridad = vm.Prioridad;
            dto.Estatus = vm.Estatus;
            dto.EstatusTicket = vm.EstatusTicket;
            dto.FechaRegistro = vm.FechaRegistro;
            dto.Historial = true;
            dto.NoReapertura = vm.NoReapertura;
            dto.Motivo = vm.DescripcionIncidencia;
            //tecnicos
            dto.TecnicoAsignado = vm.TecnicoAsignado;
            dto.TecnicoAsignadoReag = vm.TecnicoAsignadoReag;
            dto.TecnicoAsignadoReag2 = vm.TecnicoAsignadoReag2;
            dto.NoAsignaciones= Convert.ToInt32(vm.NoReasignaciones);

            _db.his_Ticket.Add(dto);
            _db.SaveChanges();

            //ENVIO DE GUARDADO DE TICKET
            if (vm.IdTicketPrincipal != null)  _noti.SetNotificaciones(dto, 1);  // ,1 sets the notification for SUB ticket
            else _noti.SetNotificaciones(dto);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SaveHistoricoSubticket(DetalleSelectedTicketVm vm, int Id)
        {
            //GUARDA HISTORICO DEL SUBTICKET CREACION
            //Buscar la información del ticket principal
            var padre = _db.tbl_TicketDetalle.Where(a => a.Id == vm.subticket.IdTicket).FirstOrDefault();
            var TenicoCreadorDeSubticket = Int32.Parse(vm.EmployeeIdBO);
            ;
            if (padre != null)
            {

                var dto = new his_Ticket();
                dto.IdTicket = Id;
                dto.EmpleadoID = padre.EmpleadoID;
                dto.EmpleadoID = TenicoCreadorDeSubticket; // NEW INFO
                dto.TicketTercero = padre.TicketTercero;
                dto.Extencion = padre.Extencion;
                dto.NombreTercero = padre.NombreTercero;
                dto.Piso = padre.Piso;
                dto.EmailTercero = padre.EmailTercero;
                dto.ExtensionTercero = padre.ExtensionTercero;
                dto.Posicion = padre.Posicion;
                dto.NombreCompleto = padre.NombreCompleto;
                dto.NombreCompleto = padre.TecnicoAsignado.ToUpper(); // NEW INFO
                dto.Correo = padre.Correo;
                dto.Area = padre.Area;
                dto.Categoria = Convert.ToInt32(vm.subticket.Categoria);        // NEW INFO
                dto.Centro = Convert.ToInt32(vm.subticket.Centro);              // NEW INFO
                dto.SubCategoria = Convert.ToInt32(vm.subticket.Subcategoria);  // NEW INFO
                dto.DescripcionIncidencia = vm.subticket.DescIncidencia;        // NEW INFO
                dto.PersonasAddNotificar = padre.PersonasAddNotificar;
                dto.GrupoResolutor = vm.subticket.GrupoResolutor;               // NEW INFO
                dto.Prioridad = vm.subticket.Prioridad;                         // NEW INFO
                dto.Estatus = vm.subticket.Estatus;                             // NEW INFO
                dto.EstatusTicket = 1;                                          // NEW INFO
                dto.FechaRegistro = DateTime.Now;                               // NEW INFO
                dto.Historial = true;                                           //
                dto.Motivo = vm.subticket.DescIncidencia;                       // NEW INFO
                //tecnicos
                //dto.TecnicoAsignado = padre.TecnicoAsignado;
                dto.TecnicoAsignado = dto.TecnicoAsignado;
                dto.TecnicoAsignadoReag = dto.TecnicoAsignadoReag;      
                dto.TecnicoAsignadoReag2 = dto.TecnicoAsignadoReag2;     
                dto.NoAsignaciones= Convert.ToInt32(padre.NoReasignaciones);

                _db.his_Ticket.Add(dto);
                _db.SaveChanges();

                _noti.SetNotificaciones(dto, 1);
            }


        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public int SetEditTicket(tbl_TicketDetalle vm)
        {
            var dto = _db.tbl_TicketDetalle.Where(a => a.Id == vm.Id).FirstOrDefault();


            if (dto != null)
            {
                var est = _db.cat_EstadoTicket.Where(a => a.Estado == dto.Estatus).FirstOrDefault();


                dto.Id = vm.Id;
                dto.EmpleadoID = vm.EmpleadoID;
                dto.TicketTercero = vm.TicketTercero;
                dto.Extencion = vm.Extencion;
                dto.NombreTercero = vm.NombreTercero;
                dto.Piso = vm.Piso;
                dto.EmailTercero = vm.EmailTercero;
                dto.ExtensionTercero = vm.ExtensionTercero;
                dto.Posicion = vm.Posicion;
                dto.NombreCompleto = vm.NombreCompleto;
                dto.Area = vm.Area;
                dto.Categoria = vm.Categoria;
                dto.Centro = vm.Centro;
                dto.SubCategoria = vm.SubCategoria;
                dto.DescripcionIncidencia = vm.DescripcionIncidencia;
                dto.PersonasAddNotificar = vm.PersonasAddNotificar;
                dto.GrupoResolutor = vm.GrupoResolutor;
                dto.Prioridad = vm.Prioridad;
                dto.FechaRegistro = DateTime.Now;
                dto.Correo = vm.Correo;
                dto.Estatus = est.Estado;
                dto.EstatusTicket = est.Id;
                dto.NoReapertura = dto.NoReapertura;
                dto.NoReasignaciones = dto.NoReasignaciones;

                _db.tbl_TicketDetalle.AddOrUpdate(dto);
                _db.SaveChanges();

                SaveHistoricoEdit(vm, dto.Estatus, dto.EstatusTicket);

                return dto.Id;


            }
            else
            {

                return 0;
            }

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public int SetReapertura(tbl_TicketDetalle vm)
        {
            var NoReabrir = 1;

            var dto = _db.tbl_TicketDetalle.Where(a => a.Id == vm.Id).FirstOrDefault();

            if (dto != null)
            {
                var est = _db.cat_EstadoTicket.Where(a => a.Estado == dto.Estatus).FirstOrDefault();

                if (dto.NoReapertura == 0)
                {

                    dto.Id = vm.Id;
                    dto.EmpleadoID = vm.EmpleadoID;
                    dto.TicketTercero = vm.TicketTercero;
                    dto.Extencion = vm.Extencion;
                    dto.NombreTercero = vm.NombreTercero;
                    dto.Piso = vm.Piso;
                    dto.EmailTercero = vm.EmailTercero;
                    dto.ExtensionTercero = vm.ExtensionTercero;
                    dto.Posicion = vm.Posicion;
                    dto.NombreCompleto = vm.NombreCompleto;
                    dto.Area = vm.Area;
                    dto.Categoria = vm.Categoria;
                    dto.Centro = vm.Centro;
                    dto.SubCategoria = vm.SubCategoria;
                    dto.DescripcionIncidencia = vm.DescripcionIncidencia;
                    dto.PersonasAddNotificar = vm.PersonasAddNotificar;
                    dto.GrupoResolutor = vm.GrupoResolutor;
                    dto.Prioridad = vm.Prioridad;
                    dto.FechaRegistro = DateTime.Now;
                    dto.Correo = vm.Correo;
                    dto.NoReapertura = dto.NoReapertura + NoReabrir;
                    //En la reapertura, el ticket pasa a En Espera con el mismo técnico quien dio el seguimiento
                    dto.Estatus = "En Espera";
                    dto.EstatusTicket = 7;
                    dto.TecnicoAsignadoReag = dto.TecnicoAsignado;
                    dto.IdTecnicoAsignadoReag = dto.IdTecnicoAsignado;

                    _db.tbl_TicketDetalle.AddOrUpdate(dto);
                    _db.SaveChanges();

                    SaveHistoricoEdit(vm, dto.Estatus, dto.EstatusTicket);

                }
                else if (dto.NoReapertura == 1)
                {

                    dto.Id = vm.Id;
                    dto.EmpleadoID = vm.EmpleadoID;
                    dto.TicketTercero = vm.TicketTercero;
                    dto.Extencion = vm.Extencion;
                    dto.NombreTercero = vm.NombreTercero;
                    dto.Piso = vm.Piso;
                    dto.EmailTercero = vm.EmailTercero;
                    dto.ExtensionTercero = vm.ExtensionTercero;
                    dto.Posicion = vm.Posicion;
                    dto.NombreCompleto = vm.NombreCompleto;
                    dto.Area = vm.Area;
                    dto.Categoria = vm.Categoria;
                    dto.Centro = vm.Centro;
                    dto.SubCategoria = vm.SubCategoria;
                    dto.DescripcionIncidencia = vm.DescripcionIncidencia;
                    dto.PersonasAddNotificar = vm.PersonasAddNotificar;
                    dto.GrupoResolutor = vm.GrupoResolutor;
                    dto.Prioridad = vm.Prioridad;
                    dto.FechaRegistro = DateTime.Now;
                    dto.Correo = vm.Correo;
                    dto.NoReapertura = dto.NoReapertura + NoReabrir;
                    //En la reapertura, el ticket pasa En Espera con el mismo técnico quien dio el seguimiento
                    dto.Estatus = "En Espera";
                    dto.EstatusTicket = 7;
                    dto.TecnicoAsignadoReag2 = dto.TecnicoAsignadoReag;
                    dto.IdTecnicoAsignadoReag2 = dto.IdTecnicoAsignadoReag;

                    _db.tbl_TicketDetalle.AddOrUpdate(dto);
                    _db.SaveChanges();

                    SaveHistoricoEdit(vm, dto.Estatus, dto.EstatusTicket);

                }

                return dto.Id;


            }
            else
            {
                return 0;
            }

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SaveHistoricoEdit(tbl_TicketDetalle vm, string Estatus, int EstatusTicket)
        {
            //GUARDA HISTORICO CUANDO SE EDITA EL TICKET POR EL USUARIO
            var dto = new his_Ticket();
            dto.IdTicket = vm.Id;
            dto.EmpleadoID = vm.EmpleadoID;
            dto.TicketTercero = vm.TicketTercero;
            dto.Extencion = vm.Extencion;
            dto.NombreTercero = vm.NombreTercero;
            dto.Piso = vm.Piso;
            dto.EmailTercero = vm.EmailTercero;
            dto.ExtensionTercero = vm.ExtensionTercero;
            dto.Posicion = vm.Posicion;
            dto.NombreCompleto = vm.NombreCompleto;
            dto.Correo = vm.Correo;
            dto.Area = vm.Area;
            dto.Categoria = vm.Categoria;
            dto.Centro = vm.Centro;
            dto.SubCategoria = vm.SubCategoria;
            dto.DescripcionIncidencia = vm.DescripcionIncidencia;
            dto.PersonasAddNotificar = vm.PersonasAddNotificar;
            dto.GrupoResolutor = vm.GrupoResolutor;
            dto.Prioridad = vm.Prioridad;
            dto.Estatus = Estatus;
            dto.EstatusTicket = EstatusTicket;
            dto.NoReapertura = vm.NoReapertura;
            dto.NoAsignaciones = Convert.ToInt32(vm.NoReasignaciones);
            dto.FechaRegistro = DateTime.Now;
            dto.Historial = false;
            dto.Motivo = vm.DescripcionIncidencia;
            //tecnicos
            dto.TecnicoAsignado = vm.TecnicoAsignado;
            dto.TecnicoAsignadoReag = vm.TecnicoAsignadoReag;
            dto.TecnicoAsignadoReag2 = vm.TecnicoAsignadoReag2;

            _db.his_Ticket.Add(dto);
            _db.SaveChanges();

            //ENVIO DE EDITADO DE TICKET
            _noti.SetNotiEditado(dto);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //Historico para asignacion de supervisor
        //Historico de aprueba reasignacion
        //Usuario Aprueba estatus "Resuelto"
        public void SaveHistoricoUser(tbl_TicketDetalle vm)
        {            

            //GUARDA HISTORICO EN REASIGNACION DE TECNICO
            var dto = new his_Ticket();
            dto.IdTicket = vm.Id;
            dto.EmpleadoID = vm.EmpleadoID;
            dto.TicketTercero = vm.TicketTercero;
            dto.Extencion = vm.Extencion;
            dto.NombreTercero = vm.NombreTercero;
            dto.Piso = vm.Piso;
            dto.EmailTercero = vm.EmailTercero;
            dto.ExtensionTercero = vm.ExtensionTercero;
            dto.Posicion = vm.Posicion;
            dto.NombreCompleto = vm.NombreCompleto;
            dto.Correo = vm.Correo;
            dto.Area = vm.Area;
            dto.Categoria = vm.Categoria;
            dto.Centro = vm.Centro;
            dto.SubCategoria = vm.SubCategoria;
            dto.DescripcionIncidencia = vm.DescripcionIncidencia;
            dto.PersonasAddNotificar = vm.PersonasAddNotificar;
            dto.GrupoResolutor = vm.GrupoResolutor;
            dto.Prioridad = vm.Prioridad;
            dto.Estatus = vm.Estatus;
            dto.EstatusTicket = vm.EstatusTicket;
            dto.NoReapertura = vm.NoReapertura;
            dto.FechaRegistro = DateTime.Now;
            dto.Historial = false;
            dto.Motivo = vm.MotivoCambioEstatus;
            //tecnicos
            dto.TecnicoAsignado = vm.TecnicoAsignado;
            dto.TecnicoAsignadoReag = vm.TecnicoAsignadoReag;
            dto.TecnicoAsignadoReag2 = vm.TecnicoAsignadoReag2;
            //
            dto.NoAsignaciones= Convert.ToInt32(vm.NoReasignaciones);


            _db.his_Ticket.Add(dto);
            _db.SaveChanges();


            if (vm.IdTicketPrincipal != null)
                _noti.SetNotificaciones(dto, 1);  // ,1 sets the notification for SUB ticket
            else 
            _noti.SetNotificaciones(dto);


        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<SelectListItem> LstHoraInicio()
        {
            var lista = new List<SelectListItem> {

                new SelectListItem{ Value = "06:00", Text = "06:00" },
                new SelectListItem{ Value = "07:00", Text = "07:00" },
                new SelectListItem{ Value = "08:00", Text = "08:00" },
                new SelectListItem{ Value = "09:00", Text = "09:00" },
                new SelectListItem{ Value = "10:00", Text = "10:00" },
                new SelectListItem{ Value = "11:00", Text = "11:00" },
                new SelectListItem{ Value = "12:00", Text = "12:00" },
                new SelectListItem{ Value = "13:00", Text = "13:00" },
                new SelectListItem{ Value = "14:00", Text = "14:00" },
                new SelectListItem{ Value = "15:00", Text = "15:00" },
                new SelectListItem{ Value = "16:00", Text = "16:00" },
                new SelectListItem{ Value = "17:00", Text = "17:00" },
                new SelectListItem{ Value = "18:00", Text = "18:00" },
                new SelectListItem{ Value = "19:00", Text = "19:00" },
                new SelectListItem{ Value = "20:00", Text = "20:00" },
                new SelectListItem{ Value = "21:00", Text = "21:00" }

            };

            return lista;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<SelectListItem> LstHoraFin()
        {
            var lista = new List<SelectListItem> {

                new SelectListItem{ Value = "06:00", Text = "06:00" },
                new SelectListItem{ Value = "07:00", Text = "07:00" },
                new SelectListItem{ Value = "08:00", Text = "08:00" },
                new SelectListItem{ Value = "09:00", Text = "09:00" },
                new SelectListItem{ Value = "10:00", Text = "10:00" },
                new SelectListItem{ Value = "11:00", Text = "11:00" },
                new SelectListItem{ Value = "12:00", Text = "12:00" },
                new SelectListItem{ Value = "13:00", Text = "13:00" },
                new SelectListItem{ Value = "14:00", Text = "14:00" },
                new SelectListItem{ Value = "15:00", Text = "15:00" },
                new SelectListItem{ Value = "16:00", Text = "16:00" },
                new SelectListItem{ Value = "17:00", Text = "17:00" },
                new SelectListItem{ Value = "18:00", Text = "18:00" },
                new SelectListItem{ Value = "19:00", Text = "19:00" },
                new SelectListItem{ Value = "20:00", Text = "20:00" },
                new SelectListItem{ Value = "21:00", Text = "21:00" },
                new SelectListItem{ Value = "22:00", Text = "22:00" }
            };

            return lista;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<SelectListItem> LstPrioridad()
        {
            var lista = new List<SelectListItem> {

                new SelectListItem{ Value = "Alto", Text = "Alto" },
                new SelectListItem{ Value = "Medio", Text = "Medio" },
                new SelectListItem{ Value = "Bajo", Text = "Bajo" }

            };

            return lista;


        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<SelectListItem> LstSolicitud()
        {
            var lista = new List<SelectListItem> {

                new SelectListItem{ Value = "Solicitud", Text = "Solicitud" },
                new SelectListItem{ Value = "Incidencia", Text = "Incidencia" }

            };

            return lista;


        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetHistoricoCambioEstatus(int Id)
        {
            //GUARDADO DE HISTORICO CUANDO TECNICO CAMBIA DE ESTATUS
            var data = _db.tbl_TicketDetalle.Where(a => a.Id == Id).FirstOrDefault();

            if (data != null)
            {

                var dto = new his_Ticket();
                dto.IdTicket = Id;
                dto.EmpleadoID = data.EmpleadoID;
                dto.TicketTercero = data.TicketTercero;
                dto.Extencion = data.Extencion;
                dto.NombreTercero = data.NombreTercero;
                dto.Piso = data.Piso;
                dto.EmailTercero = data.EmailTercero;
                dto.ExtensionTercero = data.ExtensionTercero;
                dto.Posicion = data.Posicion;
                dto.NombreCompleto = data.NombreCompleto;
                dto.Correo = data.Correo;
                dto.Area = data.Area;
                dto.Categoria = data.Categoria;
                dto.Centro = data.Centro;
                dto.SubCategoria = data.SubCategoria;
                dto.DescripcionIncidencia = data.DescripcionIncidencia;
                dto.PersonasAddNotificar = data.PersonasAddNotificar;
                dto.GrupoResolutor = data.GrupoResolutor;
                dto.Prioridad = data.Prioridad;
                dto.Estatus = data.Estatus;
                dto.EstatusTicket = data.EstatusTicket;
                dto.NoReapertura = data.NoReapertura;
                dto.NoAsignaciones = Convert.ToInt32(data.NoReasignaciones);

                if (data.EstatusTicket == 6)
                {
                    dto.FechaRegistro = data.FechaRegistro;
                }
                else
                {
                    dto.FechaRegistro = DateTime.Now;
                }

                
                dto.Historial = false;
                dto.Motivo = data.MotivoCambioEstatus;
                //tecnicos
                dto.TecnicoAsignado = data.TecnicoAsignado;
                dto.TecnicoAsignadoReag = data.TecnicoAsignadoReag;
                dto.TecnicoAsignadoReag2 = data.TecnicoAsignadoReag2;

                _db.his_Ticket.Add(dto);
                _db.SaveChanges();

                //NOTIFICACIONES DE CAMBIO DE ESTATUS DE TICKET

                if (data.IdTicketPrincipal != null)
                    _noti.SetNotificaciones(dto, 1);  // ,1 sets the notification for SUB ticket
                else
                    _noti.SetNotificaciones(dto);

            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetHistoricoVinculados(int Id)
        {
            //GUARDA HISTORICO DE TICKETS VINCULADOS
            var data = _db.tbl_TicketDetalle.Where(a => a.Id == Id).FirstOrDefault();

            if (data != null)
            {

                var dto = new his_Ticket();
                dto.IdTicket = Id;
                dto.EmpleadoID = data.EmpleadoID;
                dto.TicketTercero = data.TicketTercero;
                dto.Extencion = data.Extencion;
                dto.NombreTercero = data.NombreTercero;
                dto.Piso = data.Piso;
                dto.EmailTercero = data.EmailTercero;
                dto.ExtensionTercero = data.ExtensionTercero;
                dto.Posicion = data.Posicion;
                dto.NombreCompleto = data.NombreCompleto;
                dto.Correo = data.Correo;
                dto.Area = data.Area;
                dto.Categoria = data.Categoria;
                dto.Centro = data.Centro;
                dto.SubCategoria = data.SubCategoria;
                dto.DescripcionIncidencia = data.DescripcionIncidencia;
                dto.PersonasAddNotificar = data.PersonasAddNotificar;
                dto.GrupoResolutor = data.GrupoResolutor;
                dto.Prioridad = data.Prioridad;
                dto.Estatus = data.Estatus;
                dto.EstatusTicket = data.EstatusTicket;
                dto.NoReapertura = data.NoReapertura;
                dto.NoAsignaciones = Convert.ToInt32(data.NoReasignaciones);
                dto.FechaRegistro = DateTime.Now;
                dto.Historial = false;
                dto.Motivo = data.MotivoCambioEstatus;
                //tecnicos
                dto.TecnicoAsignado = data.TecnicoAsignado;
                dto.TecnicoAsignadoReag = data.TecnicoAsignadoReag;
                dto.TecnicoAsignadoReag2 = data.TecnicoAsignadoReag2;

                _db.his_Ticket.Add(dto);
                _db.SaveChanges();
            }

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SaveHistoricoRechazaSolicitud(int TicketId)
        {
            //GUARDA HISTORICO CUANDO USUARIO EN GARANTIA LO RECHAZA Y REGRESA A TRABAJANDO
            var data = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            if (data != null)
            {

                var dto = new his_Ticket();
                dto.IdTicket = TicketId;
                dto.EmpleadoID = data.EmpleadoID;
                dto.TicketTercero = data.TicketTercero;
                dto.Extencion = data.Extencion;
                dto.NombreTercero = data.NombreTercero;
                dto.Piso = data.Piso;
                dto.EmailTercero = data.EmailTercero;
                dto.ExtensionTercero = data.ExtensionTercero;
                dto.Posicion = data.Posicion;
                dto.NombreCompleto = data.NombreCompleto;
                dto.Correo = data.Correo;
                dto.Area = data.Area;
                dto.Categoria = data.Categoria;
                dto.Centro = data.Centro;
                dto.SubCategoria = data.SubCategoria;
                dto.DescripcionIncidencia = data.DescripcionIncidencia;
                dto.PersonasAddNotificar = data.PersonasAddNotificar;
                dto.GrupoResolutor = data.GrupoResolutor;
                dto.Prioridad = data.Prioridad;
                dto.Estatus = data.Estatus;
                dto.EstatusTicket = data.EstatusTicket;
                dto.NoReapertura = data.NoReapertura;
                dto.NoAsignaciones = Convert.ToInt32(data.NoReasignaciones);
                dto.FechaRegistro = DateTime.Now;
                dto.Historial = false;
                dto.Motivo = "Solicitud Rechazada - " + data.ComentariosRechazoSolucion;
                //tecnicos
                dto.TecnicoAsignado = data.TecnicoAsignado;
                dto.TecnicoAsignadoReag = data.TecnicoAsignadoReag;
                dto.TecnicoAsignadoReag2 = data.TecnicoAsignadoReag2;

                _db.his_Ticket.Add(dto);
                _db.SaveChanges();

                _noti.SetNotiRechazoResuelto(dto);


            }


        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //Metodo Migue
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //=======================================================GUARDA ARCHIVOS========================================
        public void SetArchivoCreateTicket(int id, string uploadName)
        {

            var vm = new tblDocumentos();

            vm.IdTicket = id;
            vm.Nombre = uploadName;
            vm.Tipo = 1;
            vm.Extension = "Creación";
            vm.FechaRegisto = DateTime.Now;
            _db.tblDocumentos.Add(vm);
            _db.SaveChanges();

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetArchivoEditTicket(int id, string uploadName)
        {

            var datos = _db.tblDocumentos.Where(a => a.IdTicket == id && a.Tipo == 1).FirstOrDefault();

            if (datos != null)
            {

                _db.tblDocumentos.Attach(datos);
                datos.Nombre = uploadName;
                datos.Extension = "Editado";
                datos.FechaRegisto = DateTime.Now;
                _db.SaveChanges();

            }

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetArchivoReapertura(int id, string uploadName)
        {
            var vm = new tblDocumentos();

            vm.IdTicket = id;
            vm.Nombre = uploadName;
            vm.Tipo = 4;
            vm.Extension = "Archivo Reapertura";
            vm.FechaRegisto = DateTime.Now;
            _db.tblDocumentos.Add(vm);
            _db.SaveChanges();

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetArchivoSubticket(int id, string uploadName)
        {
            var vm = new tblDocumentos();

            vm.IdTicket = id;
            vm.Nombre = uploadName;
            vm.Tipo = 4;
            vm.Extension = "Archivo Subticket";
            vm.FechaRegisto = DateTime.Now;
            _db.tblDocumentos.Add(vm);
            _db.SaveChanges();

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void SetArchivoRechazaSolicitud(int id, string uploadName)
        {

            var vm = new tblDocumentos();

            vm.IdTicket = id;
            vm.Nombre = uploadName;
            vm.Tipo = 3;
            vm.Extension = "Archivo Rechazo Solicitud";
            vm.FechaRegisto = DateTime.Now;
            _db.tblDocumentos.Add(vm);
            _db.SaveChanges();


        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //Cambio de estatus
        public void SetArchivoTecnico(int id, string uploadName)
        {

            var vm = new tblDocumentos();

            var info = _db.tbl_TicketDetalle.Where(a => a.Id == id).FirstOrDefault();

            vm.IdTicket = id;
            vm.Nombre = uploadName;
            vm.Tipo = 2;
            vm.Extension = "Archivo " + info.Estatus;
            vm.FechaRegisto = DateTime.Now;
            _db.tblDocumentos.Add(vm);
            _db.SaveChanges();


        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<string> GetRolByPuesto(string[] puestos)
        {
            var list = new List<string>();
            for (var i = 0; i < puestos.Length; i++)
            {
                var puestoid = int.Parse(puestos[i]);
                var data = _db.PuestosRoles.Where(a => a.PuestoId == puestoid);
                if (data != null)
                {
                    foreach (var dat in data)
                    {
                        list.Add(dat.Rol);
                    }
                }
            }
            return list;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //=======================================================GUARDA ARCHIVOS========================================
        //// GUARDADO HISTORICO DE CANCELACION DE TICKET
        public void SaveHistoricoCancelacion(int TicketId) 
        {

            //GUARDA HISTORICO CUANDO USUARIO EN GARANTIA LO RECHAZA Y REGRESA A TRABAJANDO
            var data = _db.tbl_TicketDetalle.Where(a => a.Id == TicketId).FirstOrDefault();

            if (data != null)
            {

                var dto = new his_Ticket();
                dto.IdTicket = TicketId;
                dto.EmpleadoID = data.EmpleadoID;
                dto.TicketTercero = data.TicketTercero;
                dto.Extencion = data.Extencion;
                dto.NombreTercero = data.NombreTercero;
                dto.Piso = data.Piso;
                dto.EmailTercero = data.EmailTercero;
                dto.ExtensionTercero = data.ExtensionTercero;
                dto.Posicion = data.Posicion;
                dto.NombreCompleto = data.NombreCompleto;
                dto.Correo = data.Correo;
                dto.Area = data.Area;
                dto.Categoria = data.Categoria;
                dto.Centro = data.Centro;
                dto.SubCategoria = data.SubCategoria;
                dto.DescripcionIncidencia = data.DescripcionIncidencia;
                dto.PersonasAddNotificar = data.PersonasAddNotificar;
                dto.GrupoResolutor = data.GrupoResolutor;
                dto.Prioridad = data.Prioridad;
                dto.Estatus = data.Estatus;
                dto.EstatusTicket = data.EstatusTicket;
                dto.NoReapertura = data.NoReapertura;
                dto.NoAsignaciones = Convert.ToInt32(data.NoReasignaciones);
                dto.FechaRegistro = DateTime.Now;
                dto.Historial = false;
                dto.Motivo = data.Comentarios;
                //tecnicos
                dto.TecnicoAsignado = data.TecnicoAsignado;
                dto.TecnicoAsignadoReag = data.TecnicoAsignadoReag;
                dto.TecnicoAsignadoReag2 = data.TecnicoAsignadoReag2;

                _db.his_Ticket.Add(dto);
                _db.SaveChanges();


                if (data.IdTicketPrincipal != null)
                    _noti.SetNotificaciones(dto, 1);  // ,1 sets the notification for SUB ticket
                else
                    _noti.SetNotificaciones(dto);


            }
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
           

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //Guarda Encuesta Satisfacción
        public void SetEncuesta(EncuestaDetalle vm)
        {
            vm.FechaRegistro = DateTime.Now;
            _db.EncuestaDetalle.Add(vm);
            _db.SaveChanges();

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - Tareas programadas
        public void SetArchivoTareaRechazo(int id, string uploadName)
        {
            var vm = new tblDocumentos();

            vm.IdTicket = id;
            vm.Nombre = uploadName;
            vm.Tipo = 6;
            vm.Extension = "Tarea Rechazo Evidencia";
            vm.FechaRegisto = DateTime.Now;
            _db.tblDocumentos.Add(vm);
            _db.SaveChanges();

        }
        public void SetArchivoTarea(int id, string uploadName)
        {
            var vm = new tblDocumentos();

            vm.IdTicket = id;
            vm.Nombre = uploadName;
            vm.Tipo = 5;
            vm.Extension = "Tarea Creacion";
            vm.FechaRegisto = DateTime.Now;
            _db.tblDocumentos.Add(vm);
            _db.SaveChanges();
        }
        public void SetArchivoCCTarea(int id, string uploadName)
        {
            var vm = new tblDocumentos();

            vm.IdTicket = id;
            vm.Nombre = uploadName;
            vm.Tipo = 7;
            vm.Extension = "CCtarea Creacion";
            vm.FechaRegisto = DateTime.Now;
            _db.tblDocumentos.Add(vm);

            var tarea = _db.tbl_CC_Tareas.Where(x => x.Id == id).FirstOrDefault();
            tarea.Evidencia = 1; //bool like for showing evidence in DetalleCC
            _db.SaveChanges();
        }
        public int SetTareaProgramada(tbl_TareasProgramadas vm)
        {
            if (vm.Estatus.Contains("Asignado"))
            { notif("Ticket asignado", "El ticket con ID " + vm.Id + " está en estatus asignado. Es importante tu colaboración para resolver este ticket.", vm.TecnicoID); }

            _db.tblTareasProgramadas.Add(vm);
            ;
            var res = _db.SaveChanges();
            SaveHistorico(vm, "Creación");

            return vm.Id;

        }
        public int EditTareaProgramada(tbl_TareasProgramadas vm)
        {
            var dtpo = _db.tblTareasProgramadas.Where(a => a.Id == vm.Id).FirstOrDefault(); //Detalle Tarea Programada Original
            var flag = false;
            if (dtpo.TecnicoID != vm.TecnicoID) { flag = true; }
            if (dtpo != null)
            {
                dtpo.Id = vm.Id;
                dtpo.CategoriaID = vm.CategoriaID;
                dtpo.SubCategoriaID = vm.SubCategoriaID;
                dtpo.CentroID = vm.CentroID;
                dtpo.Activo = vm.Activo;
                dtpo.Descripcion = vm.Descripcion;
                dtpo.GrupoResolutorID = vm.GrupoResolutorID;
                dtpo.TecnicoID = vm.TecnicoID;
                dtpo.Prioridad = vm.Prioridad;
                dtpo.Estatus = vm.Estatus;
                dtpo.ArchivoAdjunto = vm.ArchivoAdjunto;
                dtpo.Periodo = vm.Periodo;
                dtpo.seLunes = vm.seLunes;
                dtpo.seMartes = vm.seMartes;
                dtpo.seMiercoles = vm.seMiercoles;
                dtpo.seJueves = vm.seJueves;
                dtpo.seViernes = vm.seViernes;
                dtpo.seSabado = vm.seSabado;
                dtpo.seDomingo = vm.seDomingo;
                dtpo.Hora = vm.Hora;
                dtpo.FechaInicial = vm.FechaInicial;
                dtpo.FechaFinal = vm.FechaFinal;
                dtpo.SupervisorID = vm.SupervisorID;
                dtpo.DiadelMes = vm.DiadelMes;
                dtpo.DiaCardinal = vm.DiaCardinal;
                dtpo.DiadelaSemana = vm.DiadelaSemana;
                dtpo.Activado = vm.Activado;
                dtpo.Observaciones = vm.Observaciones;
                dtpo.Diagnostico = vm.Diagnostico;


                _db.tblTareasProgramadas.AddOrUpdate(dtpo);
                _db.SaveChanges();

                if (flag)
                {
                    SaveHistorico(dtpo, "Reasignación");
                    notif("Ticket asignado", "El ticket con ID " + vm.Id + " está en estatus asignado. Es importante tu colaboración para resolver este ticket.", vm.TecnicoID);
                }
                else
                {
                    if (dtpo.Estatus.Contains("Asignado") || dtpo.Estatus.Contains("En Espera") || dtpo.Estatus.Contains("Trabajando"))
                        notif("Ticket editado: " + vm.Id, "El ticket con ID " + vm.Id + "  ha sido editado. Por favor revisa el detalle del ticket.", vm.TecnicoID);
                }
                //SaveHistorico(dtpo, "Edición");

                return dtpo.Id;
            }
            else
            {

                return 0;
            }

        }
        public int Activar_Desactivar_TareaProgramada(tbl_TareasProgramadas vm)
        {
            var dtpo = _db.tblTareasProgramadas.Where(a => a.Id == vm.Id).FirstOrDefault(); //Detalle Tarea Programada Original
            if (dtpo != null)
            {
                if (dtpo.Activado)
                {
                    dtpo.Activado = false;
                    if (dtpo.Estatus.Contains("Asignado") || dtpo.Estatus.Contains("En Espera") || dtpo.Estatus.Contains("Trabajando"))
                        notif("Ticket Desactivado: " + vm.Id, "El ticket con ID " + vm.Id + " ha sido desctivado. Consulta detalles con tu supervisor.", vm.TecnicoID);
                }
                else
                {
                    dtpo.Activado = true;
                    if (dtpo.Estatus.Contains("Asignado") || dtpo.Estatus.Contains("En Espera") || dtpo.Estatus.Contains("Trabajando"))
                        notif("Ticket Activado: " + vm.Id, "El ticket con ID " + vm.Id + " ha sido activado.Es importante tu colaboración para resolver este ticket.", vm.TecnicoID);
                }


                _db.tblTareasProgramadas.AddOrUpdate(dtpo);
                _db.SaveChanges();

                //SaveHistorico(dtpo, "Edición");

                return dtpo.Id;
            }
            else
            {

                return 0;
            }

        }
        public int CambioEstatusTareaProgramada(tbl_TareasProgramadas vm)
        {
            var dtpo = _db.tblTareasProgramadas.Where(a => a.Id == vm.Id).FirstOrDefault(); //Detalle Tarea Programada Original
            if (dtpo != null)
            {
                dtpo.Observaciones = vm.Observaciones;
                dtpo.Diagnostico = vm.Diagnostico;
                dtpo.Estatus = vm.Estatus;
                if (dtpo.Estatus.Contains("Resuelto")) { notif("Ticket Resuelto", "El ticket ID " + dtpo.Id + " requiere la aprobación de la solución", dtpo.SupervisorID); }
                _db.tblTareasProgramadas.AddOrUpdate(dtpo);
                _db.SaveChanges();

                SaveHistorico(dtpo, vm.Estatus);

                return dtpo.Id;
            }
            else
            {

                return 0;
            }

        }
        public void notif(string Astunto, string Msj, int EmpleadoID)
        {
            _noti.CrearNotificacion(Astunto, Msj, EmpleadoID);
        }
        public void SaveHistorico(tbl_TareasProgramadas vm, string evento, string motivo)
        {
            //HISTORICO QUE GUARDA cuando se CREA UNA TAREA

            var dtt = new his_TareasProgramadas();

            dtt.FechaRegistro = DateTime.Now;
            dtt.Evento = evento;
            dtt.Motivo = motivo;
            dtt.Diagnostico = vm.Diagnostico;
            dtt.Observaciones = vm.Observaciones;

            dtt.IdTarea = vm.Id;
            dtt.CategoriaID = vm.CategoriaID;
            dtt.SubCategoriaID = vm.SubCategoriaID;
            dtt.CentroID = vm.CentroID;
            dtt.Activo = vm.Activo;
            dtt.Descripcion = vm.Descripcion;
            dtt.GrupoResolutorID = vm.GrupoResolutorID;
            dtt.TecnicoID = vm.TecnicoID;
            dtt.Prioridad = vm.Prioridad;
            dtt.Estatus = vm.Estatus;
            dtt.ArchivoAdjunto = vm.ArchivoAdjunto;
            dtt.Periodo = vm.Periodo;
            dtt.seLunes = vm.seLunes;
            dtt.seMartes = vm.seMartes;
            dtt.seMiercoles = vm.seMiercoles;
            dtt.seJueves = vm.seJueves;
            dtt.seViernes = vm.seViernes;
            dtt.seSabado = vm.seSabado;
            dtt.seDomingo = vm.seDomingo;
            dtt.Hora = vm.Hora;
            dtt.FechaInicial = vm.FechaInicial;
            dtt.FechaFinal = vm.FechaFinal;
            dtt.SupervisorID = vm.SupervisorID;
            dtt.DiadelMes = vm.DiadelMes;
            dtt.DiaCardinal = vm.DiaCardinal;
            dtt.DiadelaSemana = vm.DiadelaSemana;


            _db.his_TareasProgramadas.Add(dtt);
            _db.SaveChanges();
        }
        public void SaveHistorico(tbl_TareasProgramadas vm, string evento)
        {
            //HISTORICO QUE GUARDA cuando se CREA UNA TAREA

            var dtt = new his_TareasProgramadas();

            dtt.FechaRegistro = DateTime.Now;
            dtt.Evento = evento;
            dtt.Observaciones = vm.Observaciones;
            dtt.Diagnostico = vm.Diagnostico;

            dtt.IdTarea = vm.Id;
            dtt.CategoriaID = vm.CategoriaID;
            dtt.SubCategoriaID = vm.SubCategoriaID;
            dtt.CentroID = vm.CentroID;
            dtt.Activo = vm.Activo;
            dtt.Descripcion = vm.Descripcion;
            dtt.GrupoResolutorID = vm.GrupoResolutorID;
            dtt.TecnicoID = vm.TecnicoID;
            dtt.Prioridad = vm.Prioridad;
            dtt.Estatus = vm.Estatus;
            dtt.ArchivoAdjunto = vm.ArchivoAdjunto;
            dtt.Periodo = vm.Periodo;
            dtt.seLunes = vm.seLunes;
            dtt.seMartes = vm.seMartes;
            dtt.seMiercoles = vm.seMiercoles;
            dtt.seJueves = vm.seJueves;
            dtt.seViernes = vm.seViernes;
            dtt.seSabado = vm.seSabado;
            dtt.seDomingo = vm.seDomingo;
            dtt.Hora = vm.Hora;
            dtt.FechaInicial = vm.FechaInicial;
            dtt.FechaFinal = vm.FechaFinal;
            dtt.SupervisorID = vm.SupervisorID;
            dtt.DiadelMes = vm.DiadelMes;
            dtt.DiaCardinal = vm.DiaCardinal;
            dtt.DiadelaSemana = vm.DiadelaSemana;


            _db.his_TareasProgramadas.Add(dtt);
            _db.SaveChanges();
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - Control de camnbios
        public void SaveHistoricoCC(int CCid, int TicketId, string comentario)
        {
            bool cerrado = false;
            if (comentario != "") cerrado = true;
            tbl_TicketDetalle vm = _db.tbl_TicketDetalle.Where(z => z.Id == TicketId).FirstOrDefault();
            var idSup = _db.tbl_CC_Dashboard.Where(c => c.id == CCid).FirstOrDefault().ChangeOwner;
            var nombreSupervisor = _db.tbl_User.Where(c => c.EmpleadoID == idSup).FirstOrDefault().NombreTecnico.ToUpper();
            var Descripcion = _db.tbl_CC_Dashboard.Where(c => c.id == CCid).FirstOrDefault().Descripcion;

            // Notificar a usuario que levanto el ticket que "Su ticket (id) se convertirá en un Control de Cambios"
            if (!cerrado)
            {
                // notificaciones ahora en NotificacionesCC () en ControlCambiosController
                //notif("TU TICKET SE CONVERTIRÁ EN UN CONTROL DE CAMBIOS", "Se creó el control de cambio con ID " + CCid + " asociado al ticket ID " + vm.Id, vm.EmpleadoID);
                //notif("TICKET SE HA CONVERTIDO EN UN CONTROL DE CAMBIOS", "El Ticket con ID " + vm.Id + " se ha convertido satisfactoriamente en un control de cambios con ID CCXXXX" + CCid, idSup);
                vm.Estatus = "Trabajando";
                vm.EstatusTicket = 3;
                //Cerrar ticket en cuanto se crea el CC
                vm.Estatus = "Cerrado";
                vm.EstatusTicket = 6;
            }
            else {
                // informar a usuario que el control de cambios ha sido cerrado
                vm.Estatus = "Cerrado";
                vm.EstatusTicket = 6;
                notif("CONTROL DE CAMBIOS Cerrado", "Se ha cerrado el control de cambio con ID " + CCid + " asociado al ticket ID " + vm.Id, vm.EmpleadoID);
            }

            // GUARDA HISTORICO CUAND TICKET SE CONVIERTE EN CC
            var dto = new his_Ticket();
            dto.IdTicket = vm.Id;
            dto.EmpleadoID = vm.EmpleadoID;
            dto.TicketTercero = vm.TicketTercero;
            dto.Extencion = vm.Extencion;
            dto.NombreTercero = vm.NombreTercero;
            dto.Piso = vm.Piso;
            dto.EmailTercero = vm.EmailTercero;
            dto.ExtensionTercero = vm.ExtensionTercero;
            dto.Posicion = vm.Posicion;
            dto.Correo = vm.Correo;
            dto.Area = vm.Area;
            dto.Categoria = vm.Categoria;
            dto.Centro = vm.Centro;
            dto.SubCategoria = vm.SubCategoria;
            dto.DescripcionIncidencia = vm.DescripcionIncidencia;
            dto.PersonasAddNotificar = vm.PersonasAddNotificar;
            dto.GrupoResolutor = vm.GrupoResolutor;
            dto.Prioridad = vm.Prioridad;
            dto.NoReapertura = vm.NoReapertura;
            dto.NoAsignaciones = Convert.ToInt32(vm.NoReasignaciones);
            dto.Historial = false;
            //tecnicos
            dto.TecnicoAsignado = vm.TecnicoAsignado;
            dto.TecnicoAsignadoReag = vm.TecnicoAsignadoReag;
            dto.TecnicoAsignadoReag2 = vm.TecnicoAsignadoReag2;
            dto.FechaRegistro = DateTime.Now;                               //----- Fecha y hora del evento
            dto.NombreCompleto = nombreSupervisor;                          //----- Nombre del supervisor que creo el CC

            dto.Motivo = "Ticket pasó a Control de Cambios(" + CCid + "): " + Descripcion;  //----- Descripción de evento
            dto.Estatus = "Trabajando";                                                     //----- Cambiar Estatus a
            dto.EstatusTicket = 3;                                                          //----- Trabajando

            //Cerrar ticket en cuanto se crea el CC
            dto.Motivo = "Ticket pasó a Control de Cambios (CC" + CCid + "): " + Descripcion;   //----- Descripción de evento
            dto.Estatus = "Cerrado";                                                            //----- Cambiar Estatus a
            dto.EstatusTicket = 6;                                                              //----- Trabajando

            if (cerrado) {
                dto.Motivo = "Control de Cambios Cerrado(" + CCid + "): " + comentario;  //----- Descripción de evento
                dto.Estatus = "Cerrado";                                                 //----- Cambiar Estatus a
                dto.EstatusTicket = 6;                                                   //----- Cerrado
            }


            _db.tbl_TicketDetalle.AddOrUpdate(vm);  // Cambiar estado de ticket
            _db.his_Ticket.Add(dto);                // Agregar a historico del ticket
            _db.SaveChanges();

            //ENVIO DE EDITADO DE TICKET
            //_noti.SetNotiEditado(dto);
        } // set historico de ticket cuando CC es vinculado
        public string AddCC(tbl_CC_Dashboard vm) {
            string result = "Error";
            try {
                _db.tbl_CC_Dashboard.Add(vm);
                _db.SaveChanges();
                result = "Correcto";
            } catch {
                result = "Error";
            }
            return result;
        }
        public string EditCC(tbl_CC_Dashboard vm)
        {
            string result = "Error";

            var cc = _db.tbl_CC_Dashboard.Where(x => x.id == vm.id).FirstOrDefault();
            try
            {
                cc.TipoDeCambio = vm.TipoDeCambio;
                cc.Categoria = vm.Categoria;
                cc.Subcategoria = vm.Subcategoria;
                cc.Articulo = vm.Articulo;
                cc.FlujoDeTrabajo = vm.FlujoDeTrabajo;
                cc.Impacto = vm.Impacto;
                cc.Urgencia = vm.Urgencia;
                cc.Prioridad = vm.Prioridad;
                cc.Riesgo = vm.Riesgo;
                cc.ServiciosAfectados = vm.ServiciosAfectados;
                cc.MotivosDelCambio = vm.MotivosDelCambio;
                cc.GrupoResolutor = vm.GrupoResolutor;
                cc.Titulo = vm.Titulo;
                cc.Descripcion = vm.Descripcion;
                cc.ChangeOwner = vm.ChangeOwner;
                cc.ChangeRequester = vm.ChangeRequester;
                cc.ChangeManager = vm.ChangeManager;
                cc.ChangeApprover = vm.ChangeApprover;
                if (vm.ChangeApprover2 != null ) cc.ChangeApprover2 = vm.ChangeApprover2;
                if (vm.ChangeApprover3 != null)  cc.ChangeApprover3 = vm.ChangeApprover3;
                cc.Ticket = vm.Ticket;
                cc.Implementer = vm.Implementer;
                cc.LineManager = vm.LineManager;
                cc.Reviewer = vm.Reviewer;
                cc.Estatus = vm.Estatus;
                cc.Fase = vm.Fase;
                cc.Ticket = vm.Ticket;

                    _db.tbl_CC_Dashboard.AddOrUpdate(cc);
                    _db.SaveChanges();
                    result = "Correcto";
            }
            catch
            {
                result = "Error";
            }
            return result;
        }
        public void CCHis(int EmployeeId, int CCid, string Action, string Comentario, int Evid) { //Evid = Evidencia, si hay un archivo adjutno
            his_CC cc = new his_CC();
            cc.UsuarioId = EmployeeId;
            cc.CCid = CCid;
            cc.Evidencia = Evid;
            cc.Accion = Action;
            cc.Comentario = Comentario;
            cc.Fecha = DateTime.Now;
            _db.his_CC.Add(cc);
            _db.SaveChanges();
        }

        public void AddTareaCC(tbl_CC_Tareas cct, int ccid) 
        {
            cct.CC = ccid;
            cct.Estatus = "Solicitado";
            cct.Comentario = "-";
            _db.tbl_CC_Tareas.Add(cct);
            _db.SaveChanges();
        }
        public void EditTareaCC(tbl_CC_Tareas cct)
        {
            var tarea = _db.tbl_CC_Tareas.Where(x => x.Id == cct.Id).FirstOrDefault();
            if (tarea != null) {
                //var newtarea = new tbl_CC_Tareas();
                tarea.Nombre = cct.Nombre;
                tarea.Estatus = cct.Estatus;
                tarea.Tipo = cct.Tipo;
                tarea.Descripcion = cct.Descripcion;
                tarea.Comentario = cct.Comentario;
                tarea.Fecha = cct.Fecha;
                tarea.Hora = cct.Hora;
                tarea.Tecnico = cct.Tecnico;
                tarea.GrupoResolutor = cct.GrupoResolutor;
                tarea.CC = cct.CC;

                _db.tbl_CC_Tareas.AddOrUpdate(tarea);
                _db.SaveChanges();
            }
        }
        public void DeleteTareaCC(int id) {
            var tarea = _db.tbl_CC_Tareas.Where(x => x.Id == id).FirstOrDefault();
            if (tarea != null) {
                _db.tbl_CC_Tareas.Remove(tarea);
                _db.SaveChanges();
            }
        }
        public void TareaHis(int TareaId, int EmployeeId, string Evento, string Comentario) {
            his_CC_Tareas his = new his_CC_Tareas();
            his.TareaId = TareaId;
            his.Tecnico = EmployeeId;
            his.Fecha = DateTime.Now;
            his.Comentario = Comentario;
            his.Evento = Evento;

            string Asunto = "";
            string Msj = "";
            _db.his_CC_Tareas.Add(his);
            if (Evento == "Finalizada" || Evento == "No aplica") {  
                var tarea = _db.tbl_CC_Tareas.Where(x => x.Id == TareaId).FirstOrDefault();
                tarea.FechaFinal = DateTime.Now;
                var tareasDeCC = _db.tbl_CC_Tareas.Count(x => x.CC == tarea.CC && tarea.Rechazo == false);
                var tareasFinalizadas = _db.tbl_CC_Tareas.Count(x => x.CC == tarea.CC && x.Estatus == "Finalizada" && tarea.Rechazo == false);
                var tareasNoAplica = _db.tbl_CC_Tareas.Count(x => x.CC == tarea.CC && x.Estatus == "No aplica" && tarea.Rechazo == false);
                if (tareasDeCC == (tareasFinalizadas + tareasNoAplica))
                {
                    var cc = _db.tbl_CC_Dashboard.Where(x => x.id == tarea.CC).FirstOrDefault();
                    cc.Estatus = "Trabajando";
                    cc.Fase = 5;
                    _db.SaveChanges();
                    CCHis(EmployeeId, tarea.CC, "Implementación Finalizada", "-", 0);
                    //var invs = _db.tbl_CC_Involucrados.ToList();

                    Asunto = "Control de cambios en proceso de revisión";
                    Msj = "Las tareas se finalizaron. El control de cambios con ID CC" + cc.id + " está en proceso de revisión.";
                    notif(Asunto, Msj, cc.ChangeOwner);
                    notif(Asunto, Msj, cc.ChangeRequester);
                    notif(Asunto, Msj, cc.ChangeManager);
                    //notif(Asunto, Msj, invs.Where(c => c.id == cc.ChangeRequester).Select(c => c.EmployeeId).FirstOrDefault());
                    //notif(Asunto, Msj, invs.Where(c => c.id == cc.ChangeManager).Select(c => c.EmployeeId).FirstOrDefault());

                    Asunto = "Implementación de control de cambios por revisar";
                    Msj = "La implementación del control de cambios con ID CC" + cc.id + " esta esperando tu revisión";
                    notif(Asunto, Msj, cc.LineManager);
                    notif(Asunto, Msj, cc.Reviewer);
                    //notif(Asunto, Msj, invs.Where(c => c.id == cc.LineManager).Select(c => c.EmployeeId).FirstOrDefault());
                    //notif(Asunto, Msj, invs.Where(c => c.id == cc.Reviewer).Select(c => c.EmployeeId).FirstOrDefault());
                }
            }
            _db.SaveChanges();
        }

        public string AddImplementer(string grupo, string nombre, string correo, int employeeid)
        {
            string result = "Error";
            var imp = new tbl_CC_Implementer();
            imp.EmployeeId = employeeid;
            imp.Nombre = nombre;
            imp.GrupoResolutor = grupo;
            imp.CorreoElectronico = correo;
            imp.Activado = true;
            try
            {
                var tbl = _db.tbl_CC_Implementer.Add(imp);
                _db.SaveChanges();
                result = "Correcto";

            }
            catch {
                result = "Error";                
            }
            return result;
        }
        public string EditImplementer(tbl_CC_Implementer imp)
        {
            string result = "Error";
            var implenter = _db.tbl_CC_Implementer.Where(c => c.id == imp.id).FirstOrDefault();
            implenter.Nombre = imp.Nombre;
            implenter.EmployeeId = imp.EmployeeId;
            implenter.CorreoElectronico = imp.CorreoElectronico;
            implenter.GrupoResolutor = imp.GrupoResolutor;
            implenter.Activado = imp.Activado;
            try { 
                _db.tbl_CC_Implementer.AddOrUpdate(implenter);
                _db.SaveChanges();
                result = "Correcto";
            } catch { }
            return result;
        }
        public void DeleteImplementer(int id) {
            var implementer = _db.tbl_CC_Implementer.Where(x => x.id == id).FirstOrDefault();
            if (implementer != null)
            {
                //_db.tbl_CC_Implementer.Remove(implementer);
                implementer.Activado = false;
                _db.SaveChanges();
            }
        }

        public string AddCaracteristica(string tipo, string detalle)
        {
            string result = "Error";
            var car = new tbl_CC_Caracteristicas();
            car.Tipo = tipo;
            car.Detalle = detalle;
            car.Activado = true;
                var tbl = _db.tbl_CC_Caracteristicas.Add(car);
                _db.SaveChanges();
            try
            {
                result = "Correcto";
            }
            catch
            {
                result = "Error";
            }
            return result;
        }
        public string EditCaracteristica(tbl_CC_Caracteristicas car)
        {
            string result = "Error";
            var caracteristica = _db.tbl_CC_Caracteristicas.Where(c => c.id == car.id).FirstOrDefault();
            caracteristica.Tipo = car.Tipo;
            caracteristica.Detalle = car.Detalle;
            caracteristica.Activado = car.Activado;
            try
            {
                _db.tbl_CC_Caracteristicas.AddOrUpdate(caracteristica);
                _db.SaveChanges();
                result = "Correcto";
            }
            catch { }
            return result;
        }
        public void DeleteCaracteristica(int id)
        {
            var Caracteristica = _db.tbl_CC_Caracteristicas.Where(x => x.id == id).FirstOrDefault();
            if (Caracteristica != null)
            {
                //_db.tbl_CC_Caracteristicas.Remove(Caracteristica);
                Caracteristica.Activado = false;
                _db.SaveChanges();
            }
        }

        public string AddInvolucrado(string perfil, string nombre, string correo, int employeeid, int grupo)
        {
            string result = "Error";
            var inv = new tbl_CC_Involucrados();
            inv.EmployeeId = employeeid;
            inv.Nombre = nombre;
            inv.Perfil = perfil;
            inv.CorreoElectronico = correo;
            inv.GrupoResolutor = grupo;
            inv.Activado = true;
            try
            {
                var tbl = _db.tbl_CC_Involucrados.Add(inv);
                _db.SaveChanges();
                result = "Correcto";
            }
            catch
            {
                result = "Error";
            }
            return result;
        }
        public string EditInvolucrado(tbl_CC_Involucrados inv)
        {
            string result = "Error";
            var involucrado = _db.tbl_CC_Involucrados.Where(c => c.id == inv.id).FirstOrDefault();
            involucrado.Nombre = inv.Nombre;
            involucrado.EmployeeId = inv.EmployeeId;
            involucrado.Perfil= inv.Perfil;
            involucrado.CorreoElectronico = inv.CorreoElectronico;
            involucrado.GrupoResolutor = inv.GrupoResolutor;
            involucrado.Activado = inv.Activado;
            try
            {
                _db.tbl_CC_Involucrados.AddOrUpdate(involucrado);
                _db.SaveChanges();
                result = "Correcto";
            }
            catch { }
            return result;
        }
        public void DeleteInvolucrado(int id)
        {
            var Involucrado = _db.tbl_CC_Involucrados.Where(x => x.id == id).FirstOrDefault();
            if (Involucrado != null)
            {
                //_db.tbl_CC_Involucrados.Remove(Involucrado);
                Involucrado.Activado = false;
                _db.SaveChanges();
            }
        }
    }
}

