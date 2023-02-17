using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Mvc;
using DocumentFormat.OpenXml.Spreadsheet;
using SpreadsheetLight;
using System.Collections;

namespace ServiceDesk.Models
{
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public partial class ServiceDeskContext : DbContext
    {

        public DbSet<cat_EstadoTicket> cat_EstadoTicket { get; set; }
        public DbSet<tbl_TicketDetalle> tbl_TicketDetalle { get; set; }
        public DbSet<cat_Categoria> cat_Categoria { get; set; }
        public DbSet<cat_SubCategoria> cat_SubCategoria { get; set; }
        public DbSet<cat_Centro> cat_Centro { get; set; }
        public DbSet<cat_MatrizCategoria> cat_MatrizCategoria { get; set; }
        public DbSet<tbl_User> tbl_User { get; set; }
        public DbSet<tbl_VentanaAtencion> tbl_VentanaAtencion { get; set; }
        public DbSet<tbl_JornadaLaboral> tbl_JornadaLaboral { get; set; }
        public DbSet<his_Ticket> his_Ticket { get; set; }
        public DbSet<VWDetalleTicket> VWDetalleTicket { get; set; }
        public DbSet<vwDetalleSubticket> vwDetalleSubticket { get; set; }
        public DbSet<vwDetalleUsuario> vwDetalleUsuario { get; set; }
        public DbSet<catNivelExperiencia> catNivelExperiencia { get; set; }
        public DbSet<catDiagnosticos> catDiagnosticos { get; set; }
        public DbSet<tbl_Vinculacion> tbl_Vinculacion { get; set; }
        public DbSet<tbl_VinculacionDetalle> tbl_VinculacionDetalle { get; set; }
        public DbSet<tbl_DetalleSubTicket> tbl_DetalleSubTicket { get; set; }
        public DbSet<vwDetalleDiagnostico> vwDetalleDiagnostico { get; set; }
        public DbSet<vwDetalleCategoria> vwDetalleCategoria { get; set; }
        public DbSet<vwDetalleSubcategorias> vwDetalleSubcategorias { get; set; }
        public DbSet<catGrupoResolutor> catGrupoResolutor { get; set; }
        public DbSet<rel_TicketsVinculados> rel_TicketsVinculados { get; set; }
        public DbSet<tblDocumentos> tblDocumentos { get; set; }
        public DbSet<PuestosRoles> PuestosRoles { get; set; }
        public DbSet<Notificaciones> Notificaciones { get; set; }
        public DbSet<TiemposSLA> TiemposSLA { get; set; }
        public DbSet<EncuestaDetalle> EncuestaDetalle { get; set; }
        public DbSet<vw_DetalleReportes> vw_DetalleReportes { get; set; }
        public DbSet<vwEncuestaDetalle> vwEncuestaDetalle { get; set; }
        public DbSet<tbl_rel_SupervisorCentros> tbl_rel_SupervisorCentros { get; set; }
        public DbSet<tbl_User_TI_Exclusion_Filtro_Centro> tbl_User_TI_Exclusion_Filtro_Centro { get; set; }


        //-- TP
        public DbSet<tbl_TareasProgramadas> tblTareasProgramadas { get; set; }
        public DbSet<vw_TareasProgramadas> vw_TareasProgramadas { get; set; }
        public DbSet<his_TareasProgramadas> his_TareasProgramadas { get; set; }
        public DbSet<vw_HistoricoTareas> vw_HistoricoTareas { get; set; }
        //-- CC
        public DbSet<tbl_CC_Caracteristicas> tbl_CC_Caracteristicas { get; set; }
        public DbSet<tbl_CC_Involucrados> tbl_CC_Involucrados { get; set; }
        public DbSet<tbl_CC_Implementer> tbl_CC_Implementer { get; set; }
        public DbSet<vw_CC_Implementer> vw_CC_Implementer { get; set; }
        public DbSet<vw_CC_Dashboard> vw_CC_Dashboard { get; set; }
        public DbSet<tbl_CC_Dashboard> tbl_CC_Dashboard { get; set; }
        public DbSet<tbl_CC_Tareas> tbl_CC_Tareas { get; set; }
        public DbSet<vw_CC_Tareas> vw_CC_Tareas { get; set; }
        public DbSet<his_CC> his_CC { get; set; }
        public DbSet<vw_his_CC> vw_his_CC { get; set; }
        public DbSet<his_CC_Tareas> his_CC_Tareas { get; set; }
        public DbSet<vw_his_CC_Tareas> vw_his_CC_Tareas { get; set; }

        public ServiceDeskContext()
            : base("name=ServiceConn")
        {
            Database.SetInitializer((IDatabaseInitializer<ServiceDeskContext>)null);
        }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    //------------------------------------------------- CC   
    [Table("his_CC_Dashboard")] public class his_CC
    {
        [Key] public int Id { get; set; }        
        public int CCid { get; set; }
        public int UsuarioId { get; set; }
        public string Accion { get; set; }
        public string Comentario { get; set; }
        public int Evidencia { get; set; }
        public DateTime Fecha { get; set; }
    }
    [Table("vw_his_CC_Dashboard")] public class vw_his_CC
    {
        [Key] public int Id { get; set; }
        public int CCid { get; set; }
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Accion { get; set; }
        public string Comentario { get; set; }
        public DateTime Fecha { get; set; }
    }
    [Table("tbl_CC_Tareas")] public class tbl_CC_Tareas
    {
        [Key] public int Id { get; set; }
        public string Nombre { get; set; }
        public string Estatus { get; set; }
        public int Tipo { get; set; }
        public string Descripcion { get; set; }
        public string Comentario { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? FechaFinal { get; set; }
        public DateTime Hora { get; set; }
        public int Tecnico { get; set; }
        public int? Evidencia { get; set; }
        public string GrupoResolutor { get; set; }
        public int CC { get; set; }
        public bool Rechazo { get; set; }
    }
    [Table("his_CC_Tareas")] public class his_CC_Tareas
    {
        [Key] public int Id { get; set; }
        public int TareaId { get; set; }
        public int Tecnico { get; set; }
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; }
        public string Evento { get; set; }
    }
    [Table("vw_his_CC_Tareas")] public class vw_his_CC_Tareas
    {
        [Key] public int Id { get; set; }
        public int TareaId { get; set; }
        public int Tecnico { get; set; }
        public string Nombre{ get; set; }
        public string Correo { get; set; }
        public string Evento { get; set; }
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; }
    }
    [Table("vw_CC_Tareas")] public class vw_CC_Tareas
    {
        [Key] public int Id { get; set; }
        public string Nombre { get; set; }
        public string Estatus { get; set; }
        public int Tipo { get; set; }
        public string Descripcion { get; set; }
        public string Comentario { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? FechaFinal { get; set; }
        public DateTime Hora { get; set; }
        public string Tecnico { get; set; }
        public int? Evidencia { get; set; }
        public string GrupoResolutor { get; set; }
        public string Correo { get; set; }
        public int CC { get; set; }
        public bool Rechazo { get; set; }
    }
    [Table("vw_CC_Dashboard3")] public class vw_CC_Dashboard
    {
        [Key] public int id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string TipoDeCambio { get; set; }
        public string Categoria { get; set; }
        public string Subcategoria { get; set; }
        public string Articulo { get; set; }
        public string FlujoDeTrabajo { get; set; }
        public string Impacto { get; set; }
        public string Urgencia { get; set; }
        public string Prioridad { get; set; }
        public string Riesgo { get; set; }
        public string ServiciosAfectados { get; set; }
        public string MotivosDelCambio { get; set; }
        public string GrupoResolutor { get; set; }

        public string ChangeOwner { get; set; }
        public string ChangeRequester { get; set; }
        public string ChangeManager { get; set; }
        public string ChangeApprover { get; set; }
        public string ChangeApprover2 { get; set; }
        public string ChangeApprover3 { get; set; }
        public string Implementer { get; set; }
        public string LineManager { get; set; }
        public string Reviewer { get; set; }

        public string Estatus { get; set; }
        public string EstatusAp { get; set; }
    }
    [Table("tbl_CC_Dashboard")] public class tbl_CC_Dashboard
    {
        [Key] public int id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public int TipoDeCambio { get; set; }
        public int Categoria { get; set; }
        public int Subcategoria { get; set; }
        public string Articulo { get; set; }
        public int FlujoDeTrabajo { get; set; }
        public int Impacto { get; set; }
        public int Urgencia { get; set; }
        public int Prioridad { get; set; }
        public int Riesgo { get; set; }
        public int ServiciosAfectados { get; set; }
        public int MotivosDelCambio { get; set; }
        public int GrupoResolutor { get; set; }
        public int ChangeOwner { get; set; }
        public int ChangeRequester { get; set; }
        public int ChangeManager { get; set; }
        public int ChangeApprover { get; set; }
        public int? ChangeApprover2 { get; set; }
        public int? ChangeApprover3 { get; set; }
        public int Implementer { get; set; }
        public int LineManager { get; set; }
        public int Reviewer { get; set; }
        public string Estatus { get; set; }
        public string EstatusAp { get; set; }
        public int Fase { get; set; }
        public int? Ticket { get; set; }
        public bool Aproval1 { get; set; }
        public bool Aproval2 { get; set; }
        public bool Aproval3 { get; set; }
    }
    [Table("vw_CC_Implementer")] public class vw_CC_Implementer
    {
        [Key] public int id { get; set; }
        public int EmployeeId { get; set; }
        public string Grupo{ get; set; }
        public string Nombre { get; set; }
        public string CorreoElectronico { get; set; }
        public bool Activado { get; set; }
    }
    [Table("tbl_CC_Implementer")] public class tbl_CC_Implementer
    {
        [Key] public int id { get; set; }
        public int EmployeeId { get; set; }
        public string GrupoResolutor { get; set; }
        public string Nombre { get; set; }
        public string CorreoElectronico { get; set; }
        public bool Activado { get; set; }
    }
    [Table("tbl_CC_Involucrados")] public class tbl_CC_Involucrados
    {
        [Key] public int id { get; set; }
        public int EmployeeId { get; set; }
        public string Perfil { get; set; }
        public int GrupoResolutor { get; set; }
        public string Nombre { get; set; }
        public string CorreoElectronico { get; set; }
        public bool Activado { get; set; }
    }
    [Table("tbl_CC_Caracteristicas")] public class tbl_CC_Caracteristicas
    {
        [Key] public int id { get; set; }
        public string Tipo { get; set; }
        public string Detalle { get; set; }
        public bool Activado { get; set; }
    }

    //---------------------------------------------------- TP
    [Table("vw_HistoricoTareas")] public class vw_HistoricoTareas
    {
        [Key]
        public int Id { get; set; }
        public int IdTarea { get; set; }
        public string Activo { get; set; }
        public string Descripcion { get; set; }
        public string Prioridad { get; set; }
        public string Estatus { get; set; }
        public string ArchivoAdjunto { get; set; }
        public string Periodo { get; set; }
        public bool seLunes { get; set; }
        public bool seMartes { get; set; }
        public bool seMiercoles { get; set; }
        public bool seJueves { get; set; }
        public bool seViernes { get; set; }
        public bool seSabado { get; set; }
        public bool seDomingo { get; set; }
        public DateTime Hora { get; set; }
        public DateTime FechaInicial { get; set; }
        public DateTime FechaFinal { get; set; }
        public int SupervisorID { get; set; }
        public int DiadelMes { get; set; }
        public int DiaCardinal { get; set; }
        public int DiadelaSemana { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Evento { get; set; }
        public string Categoria { get; set; }
        public string SubCategoria { get; set; }
        public string Centro { get; set; }
        public string NombreTecnico { get; set; }
        public string NombreSupervisor { get; set; }
        public string Grupo { get; set; }
        public string Motivo { get; set; }
        public string Observaciones { get; set; }
        public string Diagnostico { get; set; }

    }
    [Table("his_TareasProgramadas")] public class his_TareasProgramadas
    {
        [Key]
        public int Id { get; set; }
        public int IdTarea { get; set; }
        public int CategoriaID { get; set; }
        public int SubCategoriaID { get; set; }
        public int CentroID { get; set; }
        public string Activo { get; set; }
        public string Descripcion { get; set; }
        public int GrupoResolutorID { get; set; }
        public int TecnicoID { get; set; }
        public string Prioridad { get; set; }
        public string Estatus { get; set; }
        public string ArchivoAdjunto { get; set; }
        public string Periodo { get; set; }
        public bool seLunes { get; set; }
        public bool seMartes { get; set; }
        public bool seMiercoles { get; set; }
        public bool seJueves { get; set; }
        public bool seViernes { get; set; }
        public bool seSabado { get; set; }
        public bool seDomingo { get; set; }
        public DateTime Hora { get; set; }
        public DateTime FechaInicial { get; set; }
        public DateTime FechaFinal { get; set; }
        public int SupervisorID { get; set; }
        public int DiadelMes { get; set; }
        public int DiaCardinal { get; set; }
        public int DiadelaSemana { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Evento { get; set; }
        public string Motivo { get; set; }
        public string Observaciones { get; set; }
        public string Diagnostico { get; set; }

    }
    [Table("vw_TareasProgramadas")] public class vw_TareasProgramadas
    {
        [Key]
        public int Id { get; set; }
        public string Categoria { get; set; }
        public string SubCategoria { get; set; }
        public string Centro { get; set; }
        public string Activo { get; set; }
        public string Descripcion { get; set; }
        public string Grupo { get; set; }
        public string NombreTecnico { get; set; }
        public string Prioridad { get; set; }
        public string Color { get; set; }
        public string Estatus { get; set; }
        public string ArchivoAdjunto { get; set; }
        public string Periodo { get; set; }
        public bool seLunes { get; set; }
        public bool seMartes { get; set; }
        public bool seMiercoles { get; set; }
        public bool seJueves { get; set; }
        public bool seViernes { get; set; }
        public bool seSabado { get; set; }
        public bool seDomingo { get; set; }
        public DateTime Hora { get; set; }
        public DateTime FechaInicial { get; set; }
        public DateTime FechaFinal { get; set; }
        public int SupervisorID { get; set; }
        public int? EmpleadoID { get; set; }        
        public int DiadelMes { get; set; }
        public int DiaCardinal { get; set; }
        public int DiadelaSemana { get; set; }
        public bool Activado { get; set; }
        public bool Activado_2 { get; set; }

    }
    [Table("tbl_TareasProgramadas")] public class tbl_TareasProgramadas
    {
        [Key]
        public int Id { get; set; }
        public int CategoriaID { get; set; }
        public int SubCategoriaID { get; set; }
        public int CentroID { get; set; }
        public string Activo { get; set; }
        public string Descripcion { get; set; }
        public int GrupoResolutorID { get; set; }
        public int TecnicoID { get; set; }
        public string Prioridad { get; set; }
        public string Estatus { get; set; }
        public string ArchivoAdjunto { get; set; }
        public string Periodo { get; set; }
        public bool seLunes { get; set; }
        public bool seMartes { get; set; }
        public bool seMiercoles { get; set; }
        public bool seJueves { get; set; }
        public bool seViernes { get; set; }
        public bool seSabado { get; set; }
        public bool seDomingo { get; set; }
        public DateTime Hora { get; set; }
        public DateTime FechaInicial { get; set; }
        public DateTime FechaFinal { get; set; }
        public int SupervisorID { get; set; }
        public int DiadelMes { get; set; }
        public int DiaCardinal { get; set; }
        public int DiadelaSemana { get; set; }
        public bool Activado { get; set; }
        public bool Activado_2 { get; set; }
        public string Observaciones { get; set; }
        public string Diagnostico { get; set; }
    }
    //-----------------------------------------------------

    [Table("tbl_rel_SupervisorCentros")] public class tbl_rel_SupervisorCentros //-------- Varios centros Supervisores
    {
        [Key] public int Id { get; set; }
        public int UserId { get; set; }
        public int CentroId { get; set; }
    }

    [Table("tbl_User_TI_Exclusion_Filtro_Centro")] public class tbl_User_TI_Exclusion_Filtro_Centro //-------- Varios centros Tecnicos
    {
        [Key] public int Id { get; set; }
        public int EmployeeId { get; set; }
    }
    [Table("vw_DetalleReportes")] public class vw_DetalleReportes
    {
        [Key]
        public int Id { get; set; }
        public string Area { get; set; }
        public string Categoria { get; set; }
        public int EstatusTicket { get; set; }
        public string SubCategoria { get; set; }
        public string GrupoResolutor { get; set; }
        public string Prioridad { get; set; }
        public string DescripcionIncidencia { get; set; }
        public int NoReapertura { get; set; }
        public string Color { get; set; }
        public string TecnicoAsignado { get; set; }
        public string TecnicoAsignadoReag2 { get; set; }
        public string TecnicoAsignadoReag { get; set; }
        public int? IdTecnicoAsignado { get; set; }
        public int? IdTecnicoAsignadoReag2 { get; set; }
        public int? IdTecnicoAsignadoReag { get; set; }
        public int? NoReasignaciones { get; set; }
        public string Incidencia { get; set; }
        public string SLAObjetivo { get; set; }
        public int? IdTicketPrincipal { get; set; }
        public DateTime FechaRegistro { get; set; }

    }

    [Table("cat_EstadoTicket")] public class cat_EstadoTicket
    {
        [Key]
        public int Id { get; set; }
        public string Estado { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }

    }

    public class LstDatosReportes
    {
        public List<DatosReportes> DatosReportes { get; set; }

    }

    public class DatosReportes
    {
        public string[] Column { get; set; }
        public int[] Count { get; set; }

    }
    public class ExcelDataReporteria {
        public int EmpleadoID { get; set; }
        public int ticketID { get; set; }
        public SLDocument spreadsheet { get; set; }
        public int row { get; set; }
        public List<vw_DetalleReportes> vwReportes { get; set; }
        public int? subticket { get; set; }
        public string virgulilla { get; set; }
        public int x { get; set; }
        public string incidencia { get; set; }
        public string tenicoQueCerro { get; set; }
        public int? noReasignaciones { get; set; }
        public string SLAobjetivo { get; set; }
        public int minSLAint { get; set; }
        public SLStyle style { get; set; }
        public int? colores { get; set; }
        public string Estatus { get; set; }
        public Hashtable ListadePadres { get; set; }
        public int calif { get; set; }
        public int flag { get; set; }
    }

    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("cat_Categoria")] public class cat_Categoria
    {
        [Key]
        public int Id { get; set; }
        public string Categoria { get; set; }
        public bool Activo { get; set; }
        public int GrupoResolutor { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("cat_SubCategoria")] public class cat_SubCategoria
    {
        [Key]
        public int Id { get; set; }
        public int IDCategoria { get; set; }
        public string SubCategoria { get; set; }
        public string Tipo { get; set; }
        public string Prioridad { get; set; }
        public string SLA { get; set; }
        public int NivelExperiencia { get; set; }
        public string Periodo { get; set; }
        public bool Activo { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("cat_Centro")] public class cat_Centro
    {
        [Key]
        public int Id { get; set; }
        public string Centro { get; set; }
        public bool Activo { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("vw_MatrizCategoria")] public class cat_MatrizCategoria
    {

        [Key]
        public Int64 Id { get; set; }
        public int IDCategoria { get; set; }
        public int IDSubCategoria { get; set; }
        public string Incidencia { get; set; }
        public string GrupoAtencion { get; set; }
        public string NivelExpertiz { get; set; }
        public string Prioridad { get; set; }
        public string SLAObjetivo { get; set; }
        public string Garantia { get; set; }                
        public string Diagnostico { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("tbl_TicketDetalle")] public class tbl_TicketDetalle
    {
        [Key]
        public int Id { get; set; }
        public int EmpleadoID { get; set; }
        public bool TicketTercero { get; set; }
        public string Extencion { get; set; }
        public string NombreTercero { get; set; }
        public string Piso { get; set; }
        public string EmailTercero { get; set; }
        public string ExtensionTercero { get; set; }
        public string Posicion { get; set; }
        public string NombreCompleto { get; set; }
        public string Area { get; set; }
        public int Categoria { get; set; }
        public int Centro { get; set; }
        public int SubCategoria { get; set; }
        public string DescripcionIncidencia { get; set; }
        public string PersonasAddNotificar { get; set; }
        public string GrupoResolutor { get; set; }
        public string Prioridad { get; set; }
        public string Estatus { get; set; }
        public int EstatusTicket { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Correo { get; set; }
        //public string ArchivoAdjunto { get; set; }
        public string Comentarios { get; set; }        
        public int? NoReasignaciones { get; set; }
        public string MotivoReasignacion { get; set; }
        public int? ApruebaReasignacion { get; set; }
        public string MotivoCambioEstatus { get; set; }
        public int? Diagnostico { get; set; }
        //public string ArchivoAdjuntoCambioEstatus { get; set; }
        //Información de subtickets
        //public string ArchivoAdjuntoSubticket { get; set; }
        public int? IdTicketPrincipal { get; set; }
        public int? NoReapertura { get; set; }
        //public string ArchivoAdjuntoRechazoSolucion { get; set; }
        public string ComentariosRechazoSolucion { get; set; }
        public string TecnicoAsignado { get; set; }
        public int? IdTecnicoAsignado { get; set; }
        public string TecnicoAsignadoReag { get; set; }
        public int? IdTecnicoAsignadoReag { get; set; }
        public string TecnicoAsignadoReag2 { get; set; }
        public int? IdTecnicoAsignadoReag2 { get; set; }

        [NotMapped]
        public int? totVinculados { get; set; }
        [NotMapped]
        public bool isPadre { get; set; }
        [NotMapped]
        public int orden { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("his_Ticket")] public class his_Ticket
    {
        [Key]
        public int IdHis { get; set; }
        public int IdTicket { get; set; }
        public int EmpleadoID { get; set; }
        public bool TicketTercero { get; set; }
        public string Extencion { get; set; }
        public string NombreTercero { get; set; }
        public string Piso { get; set; }
        public string EmailTercero { get; set; }
        public string ExtensionTercero { get; set; }
        public string Posicion { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }
        public string Area { get; set; }
        public int Categoria { get; set; }
        public int Centro { get; set; }
        public int SubCategoria { get; set; }
        public string DescripcionIncidencia { get; set; }
        public string PersonasAddNotificar { get; set; }
        public string GrupoResolutor { get; set; }
        public string Prioridad { get; set; }
        public string Estatus { get; set; }
        public int EstatusTicket { get; set; }
        public bool Historial { get; set; }// 1 es creación || 0 es historico (editados)
        public DateTime FechaRegistro { get; set; }
        public string Motivo { get; set; }
        public int? NoReapertura { get; set; }
        public string TecnicoAsignado { get; set; }
        public string TecnicoAsignadoReag { get; set; }
        public string TecnicoAsignadoReag2 { get; set; }
        public int NoAsignaciones { get; set; }//NUEVO

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("tbl_User")] public class tbl_User
    {
        [Key]
        public int Id { get; set; }
        public int EmpleadoID { get; set; }
        public string NombreTecnico { get; set; }
        public string GrupoResolutor { get; set; }
        public int Centro { get; set; }
        public string HoraInicioATC { get; set; }
        public string HoraFinATC { get; set; }
        public string HoraInicioJornada { get; set; }
        public string HoraFinJornada { get; set; }
        public string Correo { get; set; }
        public string NivelExperiencia { get; set; }//Cambiarlo despues por INT 
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public string Username { get; set; }//NUEVO
        public string Rol { get; set; }//NUEVO
        //Ventana Atencion
        [NotMapped] public bool LunesATC { get; set; }
        [NotMapped] public bool MartesATC { get; set; }
        [NotMapped] public bool MiercolesATC { get; set; }
        [NotMapped] public bool JuevesATC { get; set; }
        [NotMapped] public bool ViernesATC { get; set; }
        [NotMapped] public bool SabadoATC { get; set; }
        [NotMapped] public bool DomingoATC { get; set; }
        //Jornada Laboral
        [NotMapped] public bool LunesJornada { get; set; }
        [NotMapped] public bool MartesJornada { get; set; }
        [NotMapped] public bool MiercolesJornada { get; set; }
        [NotMapped] public bool JuevesJornada { get; set; }
        [NotMapped] public bool ViernesJornada { get; set; }
        [NotMapped] public bool SabadoJornada { get; set; }
        [NotMapped] public bool DomingoJornada { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("tbl_VentanaAtencion")] public class tbl_VentanaAtencion
    {
        [Key]
        public int Id { get; set; }
        public int EmpleadoID { get; set; }
        public bool Lunes { get; set; }
        public bool Martes { get; set; }
        public bool Miercoles { get; set; }
        public bool Jueves { get; set; }
        public bool Viernes { get; set; }
        public bool Sabado { get; set; }
        public bool Domingo { get; set; }
        public string HoraInicioATC { get; set; }
        public string HoraFinATC { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("tbl_JornadaLaboral")] public class tbl_JornadaLaboral
    {
        [Key]
        public int Id { get; set; }
        public int EmpleadoID { get; set; }
        public bool Lunes { get; set; }
        public bool Martes { get; set; }
        public bool Miercoles { get; set; }
        public bool Jueves { get; set; }
        public bool Viernes { get; set; }
        public bool Sabado { get; set; }
        public bool Domingo { get; set; }
        public string HoraInicioJornada { get; set; }
        public string HoraFinJornada { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("vw_DetalleTicket")] public class VWDetalleTicket
    {
        [Key]
        public int Id { get; set; }
        public int EmpleadoID { get; set; }
        public bool TicketTercero { get; set; }
        public string Extencion { get; set; }
        public string NombreTercero { get; set; }
        public string Piso { get; set; }
        public string EmailTercero { get; set; }
        public string ExtensionTercero { get; set; }
        public string Posicion { get; set; }
        public string NombreCompleto { get; set; }
        public string Area { get; set; }
        public string Categoria { get; set; }
        public string Centro { get; set; }
        public string SubCategoria { get; set; }
        public string DescripcionIncidencia { get; set; }
        public string PersonasAddNotificar { get; set; }
        public string GrupoResolutor { get; set; }
        public string Prioridad { get; set; }
        public string Estatus { get; set; }
        public int EstatusTicket { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Correo { get; set; }
        public string Color { get; set; }
        public string ArchivoAdjunto { get; set; }
        public string TecnicoAsignado { get; set; }
        public int? IdTecnicoAsignado { get; set; }
        public string TecnicoAsignadoReag { get; set; }
        public int? IdTecnicoAsignadoReag { get; set; }
        public string TecnicoAsignadoReag2 { get; set; }
        public int? IdTecnicoAsignadoReag2 { get; set; }
        public string MotivoReasignacion { get; set; }
        public int NoReapertura { get; set; }
        [NotMapped]
        public string ComentariosCancelacion { get; set; }
        [NotMapped]
        public string ComentariosRecategoriza { get; set; }
        [NotMapped]
        public string ComentariosSolicitaReasignacion { get; set; }
        [NotMapped]
        public string MotivoCambioEstatus { get; set; }

        [NotMapped]
        public int EstadoTicketTecnico { get; set; }
        [NotMapped]
        public int DXTecnico { get; set; }


    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("vw_DetalleSubticket")] public class vwDetalleSubticket
    {
        [Key]
        public int Id { get; set; }
        public int IdTicket { get; set; }
        public string Categoria { get; set; }
        public string Subcategoria { get; set; }
        public string Centro { get; set; }
        public string GrupoResolutor { get; set; }
        public string Prioridad { get; set; }
        public int EstatusId { get; set; }
        public string Estatus { get; set; }
        public string DescIncidencia { get; set; }
        public string ArchivoAdjunto { get; set; }
        public DateTime FechaRegistro { get; set; }


    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("vw_DetalleUsuario")] public class vwDetalleUsuario
    {

        [Key]
        public int Id { get; set; }
        public string GrupoResolutor { get; set; }
        public string Centro { get; set; }
        public string HorarioATC { get; set; }
        public string NombreTecnico { get; set; }
        public string HorarioJornada { get; set; }
        public string Correo { get; set; }
        public bool Activo { get; set; }
        public int EmpleadoID { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("cat_NivelExperiencia")] public class catNivelExperiencia
    {

        [Key]
        public int Id { get; set; }
        public string Nivel { get; set; }
        public bool Activo { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("cat_Diagnosticos")] public class catDiagnosticos
    {
        [Key]
        public int Id { get; set; }
        public int IdCategoria { get; set; }
        public int IdSubcategoria { get; set; }
        public string Diagnostico { get; set; }
        public bool Activo { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class tbl_Vinculacion
    {
        [Key]
        public int IdVinculacion { get; set; }
        public int? IdTicket { get; set; }
        public DateTime? FReg { get; set; }
        public bool? Activo { get; set; }
        [NotMapped]
        public int minutes { get; set; }
        [NotMapped]
        public double hours { get; set; }
        [NotMapped]
        public DateTime Fecha { get; set; }
        [NotMapped]
        public string usuario { get; set; }
        [NotMapped]
        public string tiempoSLA { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class tbl_VinculacionDetalle
    {
        [Key] public int Id { get; set; }
        public int IdTicketChild { get; set; }
        public DateTime? FReg { get; set; }
        public bool? Activo { get; set; }
        public int IdVinculacion { get; set; }
        public int TicketPrincipal { get; set; }

        [NotMapped]
        public int minutes { get; set; }
        [NotMapped]
        public double hours { get; set; }
        [NotMapped]
        public DateTime Fecha { get; set; }
        [NotMapped]
        public string usuario { get; set; }
        [NotMapped]
        public string tiempoSLA { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("tbl_DetalleSubTicket")] public class tbl_DetalleSubTicket
    {
        [Key]
        public int Id { get; set; }
        public int IdTicket { get; set; }
        public int Categoria { get; set; }
        public int Subcategoria { get; set; }
        public int Centro { get; set; }
        public string GrupoResolutor { get; set; }
        public string Prioridad { get; set; }
        public int EstatusId { get; set; }
        public string Estatus { get; set; }
        public string DescIncidencia { get; set; }
        public string ArchivoAdjunto { get; set; }
        public DateTime FechaRegistro { get; set; }


    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("vw_DetalleDiagnostico")] public class vwDetalleDiagnostico
    {
        [Key]
        public int Id { get; set; }
        public int IdCategoria { get; set; }
        public string Categoria { get; set; }
        public int IdSubcategoria { get; set; }
        public string SubCategoria { get; set; }
        public string Diagnostico { get; set; }
        public bool Activo { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("vw_DetalleCategoria")] public class vwDetalleCategoria
    {
        [Key]
        public int Id { get; set; }
        public string Categoria { get; set; }
        public string Grupo { get; set; }
        public bool Activo { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("vw_DetalleSubcategorias")] public class vwDetalleSubcategorias
    {
        [Key]
        public int Id { get; set; }
        public string Categoria { get; set; }
        public string SubCategoria { get; set; }
        public string Tipo { get; set; }
        public string Prioridad { get; set; }
        public string SLA { get; set; }
        public string Nivel { get; set; }
        public string Periodo { get; set; }
        public string Grupo { get; set; }
        public bool Activo { get; set; }
        public int NivelId { get; set; }
        public int CategoriaId { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("cat_GrupoResolutor")] public class catGrupoResolutor
    {
        [Key]
        public int Id { get; set; }
        public string Grupo { get; set; }
        public bool Activo { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("rel_TicketsVinculados")] public class rel_TicketsVinculados
    {
        [Key]
        public int TicketPrincipal { get; set; }
        public int TicketVinculado { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("tbl_Documentos")] public class tblDocumentos
    {
        [Key]
        public int Id { get; set; }
        public int IdTicket { get; set; }
        public string Nombre { get; set; }
        public string Extension { get; set; }
        public int Tipo { get; set; }
        public DateTime FechaRegisto { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("rel_PuestosRoles")] public class PuestosRoles
    {
        [Key]
        public int PuestoId { get; set; }
        public string Rol { get; set; } 
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("tbl_Notificaciones")] public class Notificaciones
    {
        [Key]
        public int Id { get; set; }
        public string Motivo { get; set; }
        public string Mensaje { get; set; }
        public int EmpleadoId { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public bool Vista { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("tbl_TiemposSLA")] public class TiemposSLA
    {
    [Key]
        public int Id { get; set; }
        public int IdTicket { get; set; }
        public string SLA_Objetivo { get; set; }
        public string SLA_Actual { get; set; }
        public string TiempoEspera { get; set; }
        public string TiempoGarantia { get; set; }
        public string SLA_Total { get; set; }
        public DateTime FechaActualicacion { get; set; }    
    
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("tbl_EncuestaDetalle")] public class EncuestaDetalle
    {
        [Key]
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public int IdTicket { get; set; }
        public string CalificaServicio { get; set; }
        public string Comentario { get; set; }
        public DateTime FechaRegistro { get; set; }

    }

    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("vw_EncuestaDetalle")] public class vwEncuestaDetalle
    {
        [Key]
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public int IdTicket { get; set; }
        public string CalificaServicio { get; set; }
        public string Comentario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int? IdTecnicoAsignado { get; set; }        
        public int? IdTecnicoAsignadoReag { get; set; }        
        public int? IdTecnicoAsignadoReag2 { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
}