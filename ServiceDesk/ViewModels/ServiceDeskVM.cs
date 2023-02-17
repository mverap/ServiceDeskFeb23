using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceDesk.Models;

namespace ServiceDesk.ViewModels
{
    public class HistoricoInfoVM
    {
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
        public bool Historial { get; set; }
        public DateTime FechaRegistro { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class DetalleSelectedTicketVm
    {
        public List<his_Ticket> historico { get; set; }
        public VWDetalleTicket detalle { get; set; }
        public vwDetalleSubticket subticket { get; set; }
        public List<vwDetalleSubticket> ListSubticket { get; set; }
        public List<tbl_VinculacionDetalle> lstVinculacion { get; set; }
        //JOSUE
        public string horas_sla { get; set; }
        public string minutes { get; set; }
        public string hours { get; set; }
        //
        public List<hisDetailV2> his = new List<hisDetailV2>();
        //AYB
        public List<tblDocumentos> Docs { get; set; }

        //MvP
        public List<VWDetalleTicket> ViewDetalleTickets { get; set; }

        //CODE BY THE GLORY EMPEROR WILL
        public List<SlaTimesVm> Slas { get; set; }

        //AYB
        public string EmployeeIdBO { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }
        public string Area { get; set; }


    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    //JOSUE
    public class hisDetailV2
    {
        public string nombre { get; set; }
        public DateTime fechaReg { get; set; }
        public string correo { get; set; }
        public string estatus { get; set; }
        public bool? file { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class DetalleGestiones
    {
        public List<vwDetalleDiagnostico> diagLst { get; set; }
        public List<vwDetalleCategoria> detCategoria { get; set; }
        public List<vwDetalleSubcategorias> detSubcat { get; set; }
        //Usuarios
        public tbl_User user { get; set; }
        public List<vwDetalleUsuario> userLst { get; set; }
        public tbl_VentanaAtencion atc { get; set; }
        public tbl_JornadaLaboral lab { get; set; }
        public List<catNivelExperiencia> NivExpe { get; set; }

        public string EmployeeIdBO { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class IdResultSubticket
    {
        public int Id { get; set; }
        public int folio { get; set; }

    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class InfoUsuario
    {

        public int Id { get; set; }
        public int EmpleadoID { get; set; }
        public string NombreTecnico { get; set; }
        public string GrupoResolutor { get; set; }
        public int IdGrupoResolutor { get; set; }
        public string Centro { get; set; }
        public int IdCentro { get; set; }
        //Ventana Atencion
        public bool Lunesatc { get; set; }
        public bool Martesatc { get; set; }
        public bool Miercolesatc { get; set; }
        public bool Juevesatc { get; set; }
        public bool Viernesatc { get; set; }
        public bool Sabadoatc { get; set; }
        public bool Domingoatc { get; set; }
        public string HoraInicioATC { get; set; }
        public string HoraFinATC { get; set; }
        //Jornada Laboral
        public bool Luneslab { get; set; }
        public bool Marteslab { get; set; }
        public bool Miercoleslab { get; set; }
        public bool Jueveslab { get; set; }
        public bool Vierneslab { get; set; }
        public bool Sabadolab { get; set; }
        public bool Domingolab { get; set; }
        public string HoraInicioJornada { get; set; }
        public string HoraFinJornada { get; set; }
        public string Correo { get; set; }
        public string NivelExperiencia { get; set; }
        public bool Activo { get; set; }


    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class LoginVm
    {
        public string Usuario { get; set; }
        public string Password { get; set; }
        public string Error { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class SlaTimesVm
    {
        public string Time { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public bool Enable { get; set; }
        public string Color { get; set; }
        public string Tecnico { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class TimerVm
    {
        public int Horas { get; set; }
        public int Minutos { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -


    //--------- TP
    public class TareasProgramadasVM
    {
        public List<vw_TareasProgramadas> tareas { get; set; }
    }
    public class TareasProgramadasHisVM
    {
        public List<vw_HistoricoTareas> tareasHis { get; set; }
        //public List<vw_TareasProgramadas> tareasVista { get; set; }
        public vw_TareasProgramadas tarea { get; set; }
        public tbl_TareasProgramadas tblTarea { get; set; }
        public List<tblDocumentos> Docs { get; set; }
        public TareasProgramadasEditVM task { get; set; }
        public int id { get; set; }
        public int taskid { get; set; }
        public string nombreCompleto { get; set; }
        public string area { get; set; }
        public string puesto { get; set; }
        public string correo { get; set; }
        public string tiempoTotal { get; set; }
        public string tiempoEspera { get; set; }
    }
    public class TareasProgramadasEditVM
    {
        public tbl_TareasProgramadas tareas { get; set; }
        public List<tblDocumentos> Docs { get; set; }
        public HttpPostedFileBase[] files { get; set; }
        public int id { get; set; }
        public string nombreCompleto { get; set; }
        public string area { get; set; }
        public string puesto { get; set; }
        public string correo { get; set; }
    }
    //--------- CC
    public class ControlCambios
    {
        //public List<tbl_CC_Implementer> list_tbl_CC_Implementer { get; set; }
        public List<tbl_CC_Involucrados> list_tbl_CC_Involucrados { get; set; }
        public List<tbl_CC_Caracteristicas> list_tbl_CC_Caracteristicas { get; set; }
        public List<vw_CC_Implementer> list_vw_CC_Implementer { get; set; }
        public tbl_CC_Caracteristicas tbl_CC_Caracteristicas { get; set; }
        public tbl_CC_Implementer tbl_CC_Implementer { get; set; }
        public tbl_CC_Involucrados tbl_CC_Involucrados { get; set; }
        public string EmployeeIdBO { get; set; }
    }
    public class HisCC
    {
        public List<his_CC> his_cc { get; set; } //-
        public List<vw_his_CC> vw_his_CC { get; set; }
        public int EmployeeID { get; set; }
        public int CCid { get; set; }
    }

    public class DetalleTareaCC
    {
        public tbl_CC_Tareas tbl_CC_Tareas { get; set; }
        public vw_CC_Tareas vw_CC_Tareas { get; set; }
        public List<vw_his_CC_Tareas> vw_his_CC_Tareas { get; set; }
        public int EmployeeID { get; set; }
        public int CCid { get; set; }
        public List<tblDocumentos> Docs { get; set; }
    }
    public class DetalleCC
    {
        public List<tbl_CC_Involucrados> list_tbl_CC_Involucrados { get; set; }
        public List<tbl_CC_Caracteristicas> list_tbl_CC_Caracteristicas { get; set; }
        public List<vw_CC_Implementer> list_vw_CC_Implementer { get; set; }
        public tbl_CC_Tareas tbl_CC_Tareas { get; set; }
        public List<tbl_CC_Tareas> list_tbl_CC_Tareas { get; set; }
        public List<vw_CC_Tareas> list_vw_CC_Tareas { get; set; }
        //public List<vw_CC_Tareas> list_vw_CC_Tareas2 { get; set; }
        public List<vw_CC_Tareas> list_vw_CC_Tareas_Rechazadas { get; set; }
        public tbl_CC_Dashboard CCtbl { get; set; }
        public vw_CC_Dashboard CCvw { get; set; }
        public vw_his_CC vw_his_CC { get; set; }
        public int EmployeeID { get; set; }
        public int CCid { get; set; }
        public int ticketid { get; set; }
        public string chmgEmail { get; set; }

        public string motivo { get; set; }
    }
    public class debugClass
    {
        public int ticketid { get; set; }
        public string slaObjetive { get; set; }
        public string slaActual { get; set; }
        public string inOrOut { get; set; }
    }
    public class CCDashboard
    {
        public List<vw_CC_Dashboard> vw_CC_Dashboards { get; set; }
        public tbl_CC_Dashboard tbl_CC_Dashboard { get; set; }
        public int EmployeeId { get; set; }
        public int ticket { get; set; }
    }
}