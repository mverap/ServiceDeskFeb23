using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServiceDesk.Models;
using ServiceDesk.Managers;
using ServiceDesk.ViewModels;
using System.Web.Security;
using System.Data;
using System.IO;

namespace ServiceDesk.Controllers
{
    public class ControlCambiosController : Controller
    {
        private readonly ServiceDeskManager _mng = new ServiceDeskManager();
        private readonly AdminContext _adm = new AdminContext();
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private readonly DocumentController _doc = new DocumentController();
        //Vistas
        public ActionResult DashboardCC(int EmployeeId, int? noEditable)
        {
            ViewBag.user = EmployeeId; ViewBag.Rol = RoldeUsuario(EmployeeId);
            ViewBag.noeditable = (noEditable.HasValue) ? (int)noEditable : 0;
            var ControlCambiosVw = new CCDashboard(); 
            //ControlCambiosVw.vw_CC_Dashboards = _db.vw_CC_Dashboard.ToList(); // mostrar todos los CC, linea vieja

            // Obtener lista de CCs que tienen tareas donde EmpleadoId es implementer
            var listTareasEmployeeIsImplementer = _db.tbl_CC_Tareas.Where(c => c.Tecnico == EmployeeId).Select(c => c.CC).ToList();
            // Obtener lista de CC donde EmpleadoId tiene un rol, añadir lista de linea anterior
            var listCCinvolvingEmployee = _db.tbl_CC_Dashboard.Where(c => 
                c.ChangeManager == EmployeeId || c.ChangeApprover == EmployeeId  || c.ChangeRequester == EmployeeId ||
                c.ChangeOwner == EmployeeId   || c.ChangeApprover2 == EmployeeId || c.Reviewer == EmployeeId        || 
                c.LineManager  == EmployeeId  || c.ChangeApprover3 == EmployeeId || c.Implementer == EmployeeId     || listTareasEmployeeIsImplementer.Contains(c.id)
            ).Select(c => c.id).ToList();
            // Guardar en VM los CC que estén contenidos en la lista anterior
            ControlCambiosVw.vw_CC_Dashboards = _db.vw_CC_Dashboard.Where(c => listCCinvolvingEmployee.Contains(c.id)).OrderBy(c => c.id).ToList();

            return View(ControlCambiosVw);
        }
        public ActionResult Gestiones(int EmployeeId, int? job) {
            ViewBag.user = EmployeeId; ViewBag.Rol = RoldeUsuario(EmployeeId);
            if (job != null) ViewBag.job = job;

            var ControlCambiosVw = new ControlCambios();

            List<SelectListItem> Grupo = new List<SelectListItem>();
            Grupo.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            var caracteristicasGR = _db.tbl_CC_Caracteristicas.Where(c => c.Tipo == "Grupo Resolutor");
            var listOfItems = new SelectList(caracteristicasGR, "Detalle", "Detalle");
            //var listOfItems = new SelectList(_db.catGrupoResolutor, "Grupo", "Grupo");
            foreach (var item in listOfItems) { Grupo.Add(item); }

            List<SelectListItem> TecnicoDummy = new List<SelectListItem>();
            TecnicoDummy.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            //listOfItems = new SelectList(_db.tbl_User, "EmpleadoID", "NombreTecnico");
            //foreach (var item in listOfItems) { NombreTecnico.Add(item); }

            List<SelectListItem> NombreTecnico = new List<SelectListItem>();
            NombreTecnico.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            listOfItems = new SelectList(_db.tbl_User, "EmpleadoID", "NombreTecnico");
            foreach (var item in listOfItems) { NombreTecnico.Add(item); }

            List<SelectListItem> Correo = new List<SelectListItem>();
            Correo.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            listOfItems = new SelectList(_db.tbl_User, "EmpleadoID", "Correo");
            foreach (var item in listOfItems) { Correo.Add(item); }

            ViewBag.Grupo = new SelectList(Grupo, "Value", "Text");
            ViewBag.Grupo2 = new SelectList(Grupo, "Value", "Text");
            ViewBag.TecnicoDummy = new SelectList(NombreTecnico, "Value", "Text"); // --------- irugbaknebte tecbuvi dummy
            ViewBag.NombreTecnico = new SelectList(NombreTecnico, "Value", "Text");
            ViewBag.Correo = new SelectList(Correo, "Value", "Text");

            //  Llenar comboboxes
            List<SelectListItem> Perfil = new List<SelectListItem>();
            Perfil.Add(new SelectListItem() { Text = "Change Requester", Value = "Change Requester" });
            Perfil.Add(new SelectListItem() { Text = "Change Manager", Value = "Change Manager" });
            Perfil.Add(new SelectListItem() { Text = "Change Approver", Value = "Change Approver" });
            Perfil.Add(new SelectListItem() { Text = "Line Manager", Value = "Line Manager" });
            Perfil.Add(new SelectListItem() { Text = "Reviewer", Value = "Reviewer" });
            ViewBag.Perfil = new SelectList(Perfil, "Value", "Text");

            List<SelectListItem> Tipo = new List<SelectListItem>();
            Tipo.Add(new SelectListItem() { Text = "Tipo de Cambio", Value = "Tipo de Cambio" });
            Tipo.Add(new SelectListItem() { Text = "Categoría", Value = "Categoria" });
            Tipo.Add(new SelectListItem() { Text = "Subcategoría", Value = "Subcategoria" });
            Tipo.Add(new SelectListItem() { Text = "Flujo de Trabajo", Value = "Flujo de Trabajo" });
            Tipo.Add(new SelectListItem() { Text = "Impacto", Value = "Impacto" });
            Tipo.Add(new SelectListItem() { Text = "Urgencia", Value = "Urgencia" });
            Tipo.Add(new SelectListItem() { Text = "Prioridad", Value = "Prioridad" });
            Tipo.Add(new SelectListItem() { Text = "Riesgo", Value = "Riesgo" });
            Tipo.Add(new SelectListItem() { Text = "Servicios Afectados", Value = "Servicios Afectados" });
            Tipo.Add(new SelectListItem() { Text = "Motivos del Cambio", Value = "Motivos del Cambio" });
            Tipo.Add(new SelectListItem() { Text = "Grupo Resolutor", Value = "Grupo Resolutor" });
            ViewBag.Tipo = new SelectList(Tipo, "Value", "Text");

            ControlCambiosVw.EmployeeIdBO = EmployeeId.ToString();

            //  Llenar tablas
            ControlCambiosVw.list_vw_CC_Implementer = _db.vw_CC_Implementer.Where(c => c.Activado == true).OrderBy(x => x.Grupo).ToList();
            ControlCambiosVw.list_tbl_CC_Involucrados = _db.tbl_CC_Involucrados.Where(c => c.Activado == true).OrderBy(x => x.Perfil).ToList();
            ControlCambiosVw.list_tbl_CC_Caracteristicas = _db.tbl_CC_Caracteristicas.Where(c => c.Activado == true).OrderBy(x => x.Tipo).ToList();

            return View(ControlCambiosVw);
        }
        public ActionResult CreacionControlCambios(int EmployeeId, int? ticket)
        {
            ViewBag.user = EmployeeId; ViewBag.Rol = RoldeUsuario(EmployeeId);
            var ControlCambiosVw = new CCDashboard();

            if (ticket.HasValue) ControlCambiosVw.ticket = ticket.Value;
            else ControlCambiosVw.ticket = 0;
            ViewBag.ticket = ControlCambiosVw.ticket;
            var maxId = 0;
            try { maxId = _db.tbl_CC_Dashboard.Max(m => m.id); }
            catch { }
            var newId = maxId + 1;
            string nuevoID = newId.ToString("0000");

            var caracteristicas = _db.tbl_CC_Caracteristicas.Where(c => c.Activado == true).ToList();
            ViewBag.conteoControl = "CC" + nuevoID;
            ViewBag.TipoDeCambio = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Tipo de Cambio")), "id", "Detalle");
            ViewBag.Categoria = new SelectList(caracteristicas.Where(x => x.Tipo == "Categoria"), "id", "Detalle");
            ViewBag.Subcategoria = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Subcategoria")), "id", "Detalle");
            ViewBag.Flujodetrabajo = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Flujo de Trabajo")), "id", "Detalle");
            ViewBag.Impacto = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Impacto")), "id", "Detalle");
            ViewBag.Urgencia = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Urgencia")), "id", "Detalle");
            ViewBag.Prioridad = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Prioridad")), "id", "Detalle");
            ViewBag.Riesgo = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Riesgo")), "id", "Detalle");
            ViewBag.Serviciosafectados = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Servicios Afectados")), "id", "Detalle");
            ViewBag.Motivosdelcambio = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Motivos del Cambio")), "id", "Detalle");
            ViewBag.Gruporesolutor = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Grupo Resolutor")), "id", "Detalle");

            var involucrados = _db.tbl_CC_Involucrados.Where(c => c.Activado == true).ToList();
            ViewBag.ChangeOwner = EmployeeId;
            ViewBag.requester = new SelectList(involucrados.Where(x => x.Perfil.Contains("Requester")), "EmployeeId", "Nombre");
            ViewBag.Manager = new SelectList(involucrados.Where(x => x.Perfil.Contains("Change Manager")), "EmployeeId", "Nombre");
            ViewBag.Approver = new SelectList(involucrados.Where(x => x.Perfil.Contains("Approver")), "EmployeeId", "Nombre");
            ViewBag.Implementer = new SelectList(_db.tbl_CC_Implementer, "EmployeeId", "Nombre");
            ViewBag.Line = new SelectList(involucrados.Where(x => x.Perfil.Contains("Line")), "EmployeeId", "Nombre");
            ViewBag.Reviewer = new SelectList(involucrados.Where(x => x.Perfil.Contains("Reviewer")), "EmployeeId", "Nombre");

            //  Llenar comboboxes
            //List<SelectListItem> Perfil = new List<SelectListItem>();
            //Perfil.Add(new SelectListItem() { Text = "Change Requester", Value = "Change Requester" });
            //Perfil.Add(new SelectListItem() { Text = "Change Manager", Value = "Change Manager" });
            //Perfil.Add(new SelectListItem() { Text = "Change Approver", Value = "Change Approver" });
            //Perfil.Add(new SelectListItem() { Text = "Line Manager", Value = "Line Manager" });
            //Perfil.Add(new SelectListItem() { Text = "Reviewer", Value = "Reviewer" });
            //ViewBag.Perfil = new SelectList(Perfil, "Value", "Text");
            return View(ControlCambiosVw);
        }
        public ActionResult EditarControlCambios(int EmployeeId, int CCid, int? repgrogramacion)
        {
            ViewBag.user = EmployeeId; ViewBag.Rol = RoldeUsuario(EmployeeId);
            ViewBag.ID = CCid;
            var ControlCambiosVw = new CCDashboard();

            //var rechazos = _db.his_CC.Where(c => c.Accion == "Implementación de control de cambios rechazada." && c.CCid == CCid).Count();
            //ViewBag.Rechazos = rechazos; // Cantidad de rechazos

            string CC = CCid.ToString("0000");
            ViewBag.conteoControl = "CC" + CC;
            var caracteristicas = _db.tbl_CC_Caracteristicas.Where(c => c.Activado == true).ToList();
            ViewBag.TipoDeCambio = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Tipo de Cambio")), "id", "Detalle");
            ViewBag.Categoria = new SelectList(caracteristicas.Where(x => x.Tipo == "Categoria"), "id", "Detalle");
            ViewBag.Subcategoria = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Subcategoria")), "id", "Detalle");
            ViewBag.Flujodetrabajo = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Flujo de Trabajo")), "id", "Detalle");
            ViewBag.Impacto = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Impacto")), "id", "Detalle");
            ViewBag.Urgencia = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Urgencia")), "id", "Detalle");
            ViewBag.Prioridad = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Prioridad")), "id", "Detalle");
            ViewBag.Riesgo = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Riesgo")), "id", "Detalle");
            ViewBag.Serviciosafectados = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Servicios Afectados")), "id", "Detalle");
            ViewBag.Motivosdelcambio = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Motivos del Cambio")), "id", "Detalle");
            ViewBag.Gruporesolutor = new SelectList(caracteristicas.Where(x => x.Tipo.Contains("Grupo Resolutor")), "id", "Detalle");

            var involucrados = _db.tbl_CC_Involucrados.Where(c => c.Activado == true).ToList();
            ViewBag.ChangeOwner = EmployeeId;
            ViewBag.requester = new SelectList(involucrados.Where(x => x.Perfil.Contains("Requester")), "EmployeeId", "Nombre");
            ViewBag.Manager = new SelectList(involucrados.Where(x => x.Perfil.Contains("Change Manager")), "EmployeeId", "Nombre");
            ViewBag.Approver = new SelectList(involucrados.Where(x => x.Perfil.Contains("Approver")), "EmployeeId", "Nombre");
            ViewBag.Implementer = new SelectList(_db.tbl_CC_Implementer, "EmployeeId", "Nombre");
            ViewBag.Line = new SelectList(involucrados.Where(x => x.Perfil.Contains("Line")), "EmployeeId", "Nombre");
            ViewBag.Reviewer = new SelectList(involucrados.Where(x => x.Perfil.Contains("Reviewer")), "EmployeeId", "Nombre");

            var cc = _db.tbl_CC_Dashboard.Where(x => x.id == CCid).FirstOrDefault();
            ControlCambiosVw.tbl_CC_Dashboard = cc;
            ViewBag.ChangeOwner = cc.ChangeOwner;
            ViewBag.ticket = 0;
            if (cc.Ticket.HasValue) { 
                ControlCambiosVw.ticket = (int)cc.Ticket;
                ViewBag.ticket = ControlCambiosVw.ticket;
            } 
            ControlCambiosVw.ticket = (cc.Ticket.HasValue) ? (int)cc.Ticket : 0;
                        
            //condiciones para poder editar: Ser ChangeOwner, ser ChangeRequester o ser ChangeManager cuando es reprogramación
            if (cc.ChangeOwner == EmployeeId || cc.ChangeRequester == EmployeeId) {
                return View(ControlCambiosVw);
            }
            else {
                if (repgrogramacion.HasValue && cc.ChangeManager == EmployeeId)
                {
                    return View(ControlCambiosVw);
                }
                else
                return RedirectToAction("DashboardCC", "ControlCambios", new { EmployeeId = EmployeeId, noEditable = CCid });
            }
        }
        public ActionResult DetalleCC(int EmployeeId, int CCid, int? job, int? aprobed, int? repro, int? tareasP, int? rolcc)
        {
            DetalleCC cc = new DetalleCC();

            ViewBag.user = EmployeeId;
            ViewBag.rolcc = rolcc;
            ViewBag.Rol = RoldeUsuario(EmployeeId); 
            ViewBag.job = job;          // guarda posición del usuario dentro de la página
            ViewBag.apro = aprobed;     // flag para mostrar modal "CC aprobado correctamente"
            ViewBag.Rechazos = CantidadDeRechazosCC(CCid); 
            ViewBag.Repro = repro;      // CC acaba de ser rechazado
            ViewBag.tareasP = tareasP;  // Error: Alguna tarea está programada para una hora previa a Ahora

            // fill cc details
            cc.CCid = CCid;
            cc.EmployeeID = EmployeeId;

            cc.list_vw_CC_Tareas = _db.vw_CC_Tareas.Where(x => x.CC == CCid && x.Rechazo == false).ToList();
            cc.list_vw_CC_Tareas_Rechazadas = _db.vw_CC_Tareas.Where(x => x.CC == CCid && x.Rechazo == true).ToList();
            cc.CCtbl = _db.tbl_CC_Dashboard.Where(x => x.id == CCid).FirstOrDefault();
            cc.CCvw = _db.vw_CC_Dashboard.Where(x => x.id == CCid).FirstOrDefault();

            cc.vw_his_CC = _db.vw_his_CC.Where(x => x.CCid == CCid && x.Accion.Contains("rechaz")).OrderByDescending(x => x.Fecha).FirstOrDefault();
            if (cc.vw_his_CC == null)
                cc.vw_his_CC = _db.vw_his_CC.Where(x => x.CCid == CCid && x.Accion.Contains("Cancel")).OrderByDescending(x => x.Fecha).FirstOrDefault();
            var chmng = cc.CCtbl.ChangeManager;
            //var chmngid = _db.tbl_CC_Involucrados.Where(x => x.id == chmng).FirstOrDefault().EmployeeId;
            //cc.chmgEmail = _db.tbl_User.Where(x => x.EmpleadoID == chmngid).FirstOrDefault().Correo;
            cc.chmgEmail = _db.tbl_User.Where(x => x.EmpleadoID == chmng).FirstOrDefault().Correo;

            // fillin cbox's
            List<SelectListItem> Grupo = new List<SelectListItem>();
            Grupo.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            var listOfItems = new SelectList(_db.catGrupoResolutor, "Id", "Grupo");
            foreach (var item in listOfItems) { Grupo.Add(item); }
            ViewBag.Grupo = new SelectList(Grupo, "Value", "Text");

            List<SelectListItem> Grupo2 = new List<SelectListItem>();
            Grupo2.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            var listOfItems2 = new SelectList(_db.tbl_CC_Caracteristicas.Where(c => c.Activado == true && c.Tipo.Contains("Grupo Resolutor")), "Detalle", "Detalle");
            foreach (var item in listOfItems2) { Grupo2.Add(item); }
            ViewBag.Gruporesolutor = new SelectList(Grupo2, "Value", "Text");

            List<SelectListItem> Tipo = new List<SelectListItem>();
            Tipo.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            Tipo.Add(new SelectListItem() { Text = "Tarea", Value = "1" });
            Tipo.Add(new SelectListItem() { Text = "Tarea Rollback", Value = "2" });
            ViewBag.Tipo = new SelectList(Tipo, "Value", "Text");

            List<SelectListItem> Tec = new List<SelectListItem>();
            Tec.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            ViewBag.Tec = new SelectList(Tec, "Value", "Text");

            List<SelectListItem> Roles = FillRolesCCdeUruario(EmployeeId, cc);
            ViewBag.Roles = new SelectList(Roles, "Value", "Text");

            ViewBag.fase = cc.CCtbl.Fase;
            return View(cc);
        }
        public ActionResult DetalleTareaCC(int EmployeeId, int TareaId, int? refresh, int? his, int? Rev) {
            var vw = new DetalleTareaCC();
            vw.tbl_CC_Tareas = _db.tbl_CC_Tareas.Where(x => x.Id == TareaId).FirstOrDefault();
            vw.vw_CC_Tareas = _db.vw_CC_Tareas.Where(x => x.Id == TareaId).FirstOrDefault();
            vw.vw_his_CC_Tareas = _db.vw_his_CC_Tareas.Where(x => x.TareaId == TareaId).OrderBy(x => x.Fecha).ToList();
            vw.EmployeeID = EmployeeId;
            vw.Docs = _db.tblDocumentos.Where(x => x.IdTicket == TareaId && x.Extension.Contains("CCtarea")).ToList();
            ViewBag.user = EmployeeId;
            ViewBag.refresh = refresh;
            ViewBag.his = his;
            ViewBag.currentEstatus = vw.tbl_CC_Tareas.Estatus;

            // solo tener acceso a edición si se es el implementer de la tarea, si no está en revisión, y si no es tarea de reprogramación previa
            if (Rev.HasValue || vw.tbl_CC_Tareas.Tecnico != EmployeeId || vw.tbl_CC_Tareas.Rechazo == true) { ViewBag.EnRevision = 1; } 

            List<SelectListItem> Estatus = new List<SelectListItem>();
            Estatus.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            if (vw.tbl_CC_Tareas.Tipo == 2)                { Estatus.Add(new SelectListItem() { Text = "No aplica", Value = "No aplica" });     } // 1 = tarea, 2 = tarea rollback
            if (vw.tbl_CC_Tareas.Estatus == "Solicitado")  { Estatus.Add(new SelectListItem() { Text = "Por Iniciar", Value = "Por Iniciar" }); }
            if (vw.tbl_CC_Tareas.Estatus == "Por Iniciar") { Estatus.Add(new SelectListItem() { Text = "En Proceso", Value = "En Proceso" });   }
            if (vw.tbl_CC_Tareas.Estatus == "En Proceso")  { Estatus.Add(new SelectListItem() { Text = "Finalizada", Value = "Finalizada" });   }
            //if (vw.tbl_CC_Tareas.Estatus == "Finalizada") { }
            //if (vw.tbl_CC_Tareas.Estatus == "No aplica") { }

            ViewBag.Estatus = new SelectList(Estatus, "Value", "Text");

            return View(vw);
        }

        //Otros Métodos CC
        public int CantidadDeRechazosCC(int CCid) {
            int rechazos = 0;
            rechazos = _db.his_CC.Where(c => c.Accion == "Implementación de control de cambios rechazada." && c.CCid == CCid).Count();
            return rechazos;
        }
        public bool TicketEnCC(int ticketId) {
            bool ticketTieneCC = false;
            var ccDeTicket = _db.tbl_CC_Dashboard.Where(t => t.Ticket != 0).Count();
            if (ccDeTicket > 0) { ticketTieneCC = true; }
            return ticketTieneCC;
        } 
        public List<SelectListItem> FillRolesCCdeUruario(int EmployeeId, DetalleCC cc)
        {
            List<SelectListItem> Roles = new List<SelectListItem>();
            Roles.Add(new SelectListItem() { Text = "SELECCIONE", Value = "0" });
            tbl_CC_Involucrados[] invs = _db.tbl_CC_Involucrados.ToArray();
            tbl_CC_Implementer[] imps = _db.tbl_CC_Implementer.ToArray();

            //< option value = "1" > Change Owner </ option >
            //< option value = "2" > Change Requester </ option >
            //< option value = "3" > Change Manager </ option >
            //< option value = "4" > Change Approver </ option >
            //< option value = "5" > Implementer </ option >
            //< option value = "6" > Line Manger </ option >
            //< option value = "7" > Change Reviewer </ option >
            //var id2verify = GetIdofRolCC(cc.CCtbl.ChangeOwner, "involucrado", invs, imps);
            var id2verify = 0;
            if (cc.CCtbl.ChangeOwner == EmployeeId) { Roles.Add(new SelectListItem() { Text = "Change Owner", Value = "1" }); }

            id2verify = cc.CCtbl.ChangeRequester;// GetIdofRolCC(cc.CCtbl.ChangeRequester, "involucrado", invs, imps);
            if (id2verify == EmployeeId) { Roles.Add(new SelectListItem() { Text = "Change Requester", Value = "2" }); }

            id2verify = cc.CCtbl.ChangeManager;// GetIdofRolCC(cc.CCtbl.ChangeManager, "involucrado", invs, imps);
            if (id2verify == EmployeeId) { Roles.Add(new SelectListItem() { Text = "Change Manager", Value = "3" }); }

            //Verificar si usuario es alguno de los ChangeApprover
            id2verify = cc.CCtbl.ChangeApprover;// GetIdofRolCC(cc.CCtbl.ChangeApprover, "involucrado", invs, imps);
            var id2verify2 = cc.CCtbl.ChangeApprover2;// GetIdofRolCC(cc.CCtbl.ChangeApprover2, "involucrado", invs, imps);
            var id2verify3 = cc.CCtbl.ChangeApprover3;// GetIdofRolCC(cc.CCtbl.ChangeApprover3, "involucrado", invs, imps);
            if (id2verify == EmployeeId || id2verify2 == EmployeeId || id2verify3 == EmployeeId)
            { Roles.Add(new SelectListItem() { Text = "Change Approver", Value = "4" }); }

            id2verify = cc.CCtbl.Implementer;// GetIdofRolCC(cc.CCtbl.Implementer, "implementer", invs, imps);
            if (id2verify == EmployeeId) { Roles.Add(new SelectListItem() { Text = "implementer", Value = "5" }); }

            id2verify = cc.CCtbl.LineManager;// GetIdofRolCC(cc.CCtbl.LineManager, "involucrado", invs, imps);
            if (id2verify == EmployeeId) { Roles.Add(new SelectListItem() { Text = "Line Manger", Value = "6" }); }

            id2verify = cc.CCtbl.Reviewer;// GetIdofRolCC(cc.CCtbl.Reviewer, "involucrado", invs, imps);
            if (id2verify == EmployeeId) { Roles.Add(new SelectListItem() { Text = "Change Reviewer", Value = "7" }); }

            return Roles;
        }
        public int GetIdofRolCC(int? id, string rol, tbl_CC_Involucrados[] invs, tbl_CC_Implementer[] imps) {
            int id_Of_Rol = 0;
            //if (rol == "implementer")
            //{
            //    id_Of_Rol = imps.Where(z => z.id == id).FirstOrDefault().EmployeeId;
            //}
            //else if (rol == "involucrado") 
            //{
            //    if (id == null) {
            //        return 0;
            //    } 
            //    else id_Of_Rol = invs.Where(x => x.id == id).FirstOrDefault().EmployeeId;
            //}
            return id_Of_Rol;
        }
        [HttpPost] public JsonResult GetTecnicos(string grupo)
        {
            var tecs = _db.tbl_User.Where(a => a.GrupoResolutor == grupo).Select(a => new { a.EmpleadoID, a.NombreTecnico }).ToList();
            return Json(tecs, JsonRequestBehavior.AllowGet);
        }
        [HttpPost] public JsonResult GetImpForEdit(int id)
        {
            var imps = _db.tbl_CC_Implementer.Where(a => a.id == id).Select(a => new { a.Nombre, a.CorreoElectronico, a.GrupoResolutor, a.EmployeeId }).FirstOrDefault();
            return Json(imps, JsonRequestBehavior.AllowGet);
        }
        [HttpPost] public JsonResult GetImps(string grupoReslutor)
        {
            var imps = _db.tbl_CC_Implementer.Where(a => a.GrupoResolutor == grupoReslutor).Select(a => new { a.Nombre, a.CorreoElectronico, a.GrupoResolutor, a.EmployeeId }).ToList();
            return Json(imps, JsonRequestBehavior.AllowGet);
        }
        [HttpPost] public JsonResult GetInvForEdit(int id)
        {
            var invs = _db.tbl_CC_Involucrados.Where(a => a.id == id).Select(a => new { a.Nombre, a.Perfil, a.GrupoResolutor, a.CorreoElectronico, a.EmployeeId }).FirstOrDefault();
            return Json(invs, JsonRequestBehavior.AllowGet);
        }
        [HttpPost] public JsonResult GetCarForEdit(int TareaId)
        {
            var car = _db.tbl_CC_Caracteristicas.Where(a => a.id == TareaId).Select(a => new { a.id, a.Tipo, a.Detalle }).FirstOrDefault();
            return Json(car, JsonRequestBehavior.AllowGet);
        }
        [HttpPost] public JsonResult GetTareaEdit(int TareaId)
        {
            var tarea = _db.tbl_CC_Tareas.Where(tbl => tbl.Id == TareaId).Select(a => new {
                a.Nombre, a.Tipo, a.Descripcion, a.GrupoResolutor, a.Comentario, a.Fecha, a.Hora, a.Tecnico, a.Estatus }).FirstOrDefault();
            return Json(tarea, JsonRequestBehavior.AllowGet);
        }

        // CC
        
        public ActionResult AñadirCC(CCDashboard vm, int EmployeeId, int? ticket)
        {
            vm.tbl_CC_Dashboard.Ticket = (ticket.HasValue) ? ticket : 0;
            vm.tbl_CC_Dashboard.Estatus = "Solicitado";
            vm.tbl_CC_Dashboard.EstatusAp = "-";
            vm.tbl_CC_Dashboard.ChangeOwner = EmployeeId;
            vm.tbl_CC_Dashboard.Fase = 1;
            if (vm.tbl_CC_Dashboard.ChangeApprover2 == 0) { vm.tbl_CC_Dashboard.ChangeApprover2 = null; }
            if (vm.tbl_CC_Dashboard.ChangeApprover3 == 0) { vm.tbl_CC_Dashboard.ChangeApprover3 = null; }

            _mng.AddCC(vm.tbl_CC_Dashboard);
            var maxId = _db.tbl_CC_Dashboard.Max(m => m.id);
            if (ticket.HasValue)
                _mng.SaveHistoricoCC(maxId, (int)ticket, ""); //guardar en historico de ticket que fue creado un cc relacionado
            _mng.CCHis(EmployeeId, maxId, "Creación", "-", 0);

            if (ticket.HasValue)
                NotificacionesCC(vm.tbl_CC_Dashboard, "CreacionDesdeTicket", 0);
            else
                NotificacionesCC(vm.tbl_CC_Dashboard, "Creacion", 0);

            return RedirectToAction("DashboardCC", "ControlCambios", new { EmployeeId = EmployeeId });
        }
        public ActionResult EditarCC(CCDashboard vm, int EmployeeId, int CCid, int? ticket)
        {
            if (ticket.HasValue) 
                vm.tbl_CC_Dashboard.Ticket = (int)ticket;
            vm.tbl_CC_Dashboard.Estatus = "Solicitado";
            //vm.tbl_CC_Dashboard.ChangeOwner = EmployeeId; //-------'------------- checa esto
            vm.tbl_CC_Dashboard.id = CCid;
            vm.tbl_CC_Dashboard.Fase = 1;
            if (vm.tbl_CC_Dashboard.ChangeApprover2 == 0) { vm.tbl_CC_Dashboard.ChangeApprover2 = null; }
            if (vm.tbl_CC_Dashboard.ChangeApprover3 == 0) { vm.tbl_CC_Dashboard.ChangeApprover3 = null; }

            int owner = _db.his_CC.Where(t => t.CCid == CCid).FirstOrDefault().UsuarioId;
            vm.tbl_CC_Dashboard.ChangeOwner = owner;

            var texto = _mng.EditCC(vm.tbl_CC_Dashboard);
            _mng.CCHis(EmployeeId, CCid, "Edición", "-", 0);
            
            return RedirectToAction("DashboardCC", "ControlCambios", new { EmployeeId = EmployeeId });
        }
        public ActionResult DeleteControlCambios(int EmployeeId, int CCid)
        {
            var cc = _db.tbl_CC_Dashboard.Where(x => x.id == CCid).FirstOrDefault();
            if (cc != null) {
                _db.tbl_CC_Dashboard.Remove(cc); _db.SaveChanges();
                _mng.CCHis(EmployeeId, CCid, "Eliminado", "-", 0);
            }
            return RedirectToAction("DashboardCC", "ControlCambios", new { EmployeeId = EmployeeId });
        }
        public void RechazarTareasAlRechazarCC(int CCid) { 
            var tareasCC = _db.tbl_CC_Tareas.Where(x => x.CC == CCid && x.Rechazo == false).ToList();
            bool tareasEditadas = false;
            foreach (var tarea in tareasCC) { 
                tarea.Rechazo = true; 
                ///tarea.Estatus = "Solicitado";
                tarea.Comentario += " (CC reprogramado, tarea antigua)";
                tareasEditadas = true;
            }
            if (tareasEditadas) _db.SaveChanges(); // Convertir todas tareas a rechazadas
        }
        // Flujo de trabajo
            // Change Owner pide cambio                (Detalle)
            // Change Manager decide si lo aprueba     
            // Change Manager crea tareas              (Planeación)
            // Change Manager pide la aprobación
            // Change Approver decide si lo aprueba    (Aprobación)
            // Implementers ven/hacen tareas           (Implementación)
            // Line Manager aprueba la implementación  (Revisión)
            // Reviwer aprueba y cierra el CC
        public ActionResult HistoricoCC(int EmployeeId, int CCid) {
            HisCC his = new HisCC();
            his.EmployeeID = EmployeeId;
            his.CCid = CCid;
            //his.his_cc = _db.his_CC.Where(x => x.CCid == CCid).ToList();
            his.vw_his_CC = _db.vw_his_CC.Where(x => x.CCid == CCid).ToList();
            return View(his);
        }
        public void NotificacionesCC(tbl_CC_Dashboard cc, string Edo, int? tareaid) {
            string Asunto = "";
            string Msj = "";
            int idTicketOwner;
            var invs = _db.tbl_CC_Involucrados.ToList();
            var imps = _db.tbl_CC_Implementer.ToList();
            int owner = cc.ChangeOwner;
            int requester = cc.ChangeRequester; // int requester = invs.Where(c => c.id == cc.ChangeRequester).Select(c => c.EmployeeId).FirstOrDefault();
            int manager = cc.ChangeManager;     // int manager   = invs.Where(c => c.id == cc.ChangeManager).Select(c => c.EmployeeId).FirstOrDefault();
            int approver1 = cc.ChangeApprover;  // int approver1 = invs.Where(c => c.id == cc.ChangeApprover).Select(c => c.EmployeeId).FirstOrDefault();
            int? aprover2 = cc.ChangeApprover2; // int? aprover2 = invs.Where(c => c.id == cc.ChangeApprover2).Select(c => c.EmployeeId).FirstOrDefault();
            int? aprover3 = cc.ChangeApprover3; // int? aprover3 = invs.Where(c => c.id == cc.ChangeApprover3).Select(c => c.EmployeeId).FirstOrDefault();
            int implmntr = cc.Implementer;      // int implmntr  = imps.Where(c => c.id == cc.Implementer).Select(c => c.EmployeeId).FirstOrDefault();
            int lineMng = cc.LineManager;       // int lineMng   = invs.Where(c => c.id == cc.LineManager).Select(c => c.EmployeeId).FirstOrDefault();
            int reviewer = cc.Reviewer;         // int reviewer  = invs.Where(c => c.id == cc.Reviewer).Select(c => c.EmployeeId).FirstOrDefault();           

            // List<int> listEmployeeIDs = new List<int>();
            if (Edo == "CreacionDesdeTicket") { 
                idTicketOwner = _db.tbl_TicketDetalle.Where(c => c.Id == (int)cc.Ticket).Select(c => c.EmpleadoID).FirstOrDefault();

                Asunto = "TU TICKET SE CONVERTIRÁ EN UN CONTROL DE CAMBIOS";
                Msj = "Se creó el control de cambios con ID CC" + cc.id + " asociado al ticket ID" + cc.Ticket;
                _mng.notif(Asunto, Msj, idTicketOwner);

                Asunto = "TU TICKET SE HA CONVERTIDO SATISFACTORIAMENTE"; // sin Asunto establecido
                Msj = "El ticket con ID" + cc.Ticket + " se ha convertido satisfactoriamente en un Control de cambios con id " + cc.id;
                _mng.notif(Asunto, Msj, owner);
                Edo = "Creacion";
            }
            if (Edo == "Rejectedby6") Edo = "Rejectedby7";

            switch (Edo)
            {
                case "Creacion":
                    Asunto = "Nuevo Control de cambios";
                    Msj = "Se te asignó al control de cambios con ID CC" + cc.id + " ";
                    _mng.notif(Asunto, Msj + "como Implementer", implmntr);
                    _mng.notif(Asunto, Msj + "como Approver", approver1);
                    if (aprover2.HasValue) _mng.notif(Asunto, Msj + "como Approver", (int)aprover2);
                    if (aprover3.HasValue) _mng.notif(Asunto, Msj + "como Approver", (int)aprover3);
                    _mng.notif(Asunto, Msj + "como Line Manager", lineMng);
                    _mng.notif(Asunto, Msj + "como Change Reviewer", reviewer);
                    break;

                case "Creacion2": //--------------------------
                    Asunto = "Nuevo Control de cambios";
                    Msj = "Se te asignó al control de cambios con ID CC" + cc.id + " ";
                    _mng.notif(Asunto, Msj + "como Implementer", implmntr);
                    _mng.notif(Asunto, Msj + "como Approver", approver1);
                    if (aprover2.HasValue) _mng.notif(Asunto, Msj + "como Approver", (int)aprover2);
                    if (aprover3.HasValue) _mng.notif(Asunto, Msj + "como Approver", (int)aprover3);
                    _mng.notif(Asunto, Msj + "como Line Manager", lineMng);
                    _mng.notif(Asunto, Msj + "como Change Reviewer", reviewer);
                    break;

                case "Cancel": //cancelación de CC
                    Asunto = "Control de cambios Cancelado";
                    Msj = "El control de cambios con ID CC" + cc.id + " ha sido cancelado.";
                    _mng.notif(Asunto, Msj, implmntr);
                    _mng.notif(Asunto, Msj, approver1);
                    if (aprover2.HasValue) _mng.notif(Asunto, Msj, (int)aprover2);
                    if (aprover3.HasValue) _mng.notif(Asunto, Msj, (int)aprover3);
                    _mng.notif(Asunto, Msj, lineMng);
                    _mng.notif(Asunto, Msj, reviewer);
                    break;

                //case "Aprueba": // aprobación por change manager
                //    Asunto = "Control de cambios Aprobado";
                //    Msj = "El control de cambios con Id " + cc.id + " se aprobó";
                //    break;

                //case "NoAprueba": // rechazo por change manager
                //    //----------------------------------
                //    Asunto = "Control de cambios Rechazado";
                //    Msj = "El control de cambios con Id " + cc.id + " se rechazó";
                //    break;

                //case "TareaAsignada": // Asignación de tarea
                //    //listEmployeeIDs.Add(implmntr);
                //    Asunto = "Control de cambios Rechazado";
                //    Msj = "El control de cambios con Id " + cc.id + " se rechazó";
                //    break;


                case "No aplica":
                    Asunto = "Tarea no necesaria";
                    Msj = "La tarea Tarea " + tareaid + " pasó a estatus no necesario. (CC:"+cc.id+")";
                    _mng.notif(Asunto, Msj, requester);
                    _mng.notif(Asunto, Msj, owner);
                    _mng.notif(Asunto, Msj, manager);
                    break;

                case "Finalizada":
                    Asunto = "Tarea Finalizada";
                    Msj = "La tarea Tarea " + tareaid + " pasó a estatus finalizado. (CC:" + cc.id + ")";
                    _mng.notif(Asunto, Msj, requester);
                    _mng.notif(Asunto, Msj, owner);
                    _mng.notif(Asunto, Msj, manager);
                    break;

                case "En Proceso":
                    Asunto = "Tarea en Proceso";
                    Msj = "La tarea Tarea " + tareaid + " pasó a estatus en proceso. (CC:" + cc.id + ")";
                    _mng.notif(Asunto, Msj, requester);
                    _mng.notif(Asunto, Msj, owner);
                    _mng.notif(Asunto, Msj, manager);
                    break;

                case "PedirAprobacionAPCC": // petición de aprobación por change manager
                    Asunto = "Control de cambios en proceso de aprobación";
                    Msj = "El control de cambios con ID CC" + cc.id + " está en proceso de aprobación.";
                    _mng.notif(Asunto, Msj, requester);
                    _mng.notif(Asunto, Msj, owner);
                    _mng.notif(Asunto, Msj, manager);
                    Asunto = "Control de cambios por aprobar";
                    Msj = "El control de cambios con ID CC" + cc.id + " esta esperando tu aprobación.";
                    _mng.notif(Asunto, Msj, approver1);
                    if (aprover2.HasValue) _mng.notif(Asunto, Msj, (int)aprover2);
                    if (aprover3.HasValue) _mng.notif(Asunto, Msj, (int)aprover3);
                    break;

                case "Aprobado": // aprobado por todos los approvers
                    Asunto = "Control de cambios aprobado";
                    Msj = "El control de cambios con ID CC" + cc.id + " se aprobó.";
                    _mng.notif(Asunto, Msj, requester);
                    _mng.notif(Asunto, Msj, owner);
                    _mng.notif(Asunto, Msj, manager);
                    _mng.notif(Asunto, Msj + " Es necesario tu apoyo para atender las tareas que te fueron asignadas.", implmntr);
                    _mng.notif(Asunto, Msj, lineMng);
                    _mng.notif(Asunto, Msj, reviewer);
                    break;

                case "RechazoAPCC": // rechazo por approvers
                    Asunto = "Control de cambios rechazado";
                    Msj = "El control de cambios con ID CC" + cc.id + " se rechazó.";
                    _mng.notif(Asunto, Msj, requester);
                    _mng.notif(Asunto, Msj, owner);
                    _mng.notif(Asunto, Msj, manager);
                    break;

                case "Impfinished": // implementers terminan de implementar código en ServiceDeskManager TareaHis
                    //Asunto = "Control de cambios en proceso de revisión";
                    //Msj = "Las tareas se finalizaron. El control de cambios con ID CC" + cc.id + " está en proceso de revisión.";
                    //_mng.notif(Asunto, Msj, requester);
                    //_mng.notif(Asunto, Msj, owner);
                    //_mng.notif(Asunto, Msj, manager);
                    //Asunto = "Implementación de control de cambios por revisar";
                    //Msj = "La implementación del contro de cambios con ID CC" + cc.id + " esta esperando tu revisión";
                    //_mng.notif(Asunto, Msj, lineMng);
                    //_mng.notif(Asunto, Msj, reviewer);
                    break;

                //case "Rejectedby6": // rechazo de implementación por linemanager (identidco a Rejectedby7)
                //    break;

                case "Aprovedby6": // áprobación de implementación por linemanager
                    Asunto = "Implementación aprobada";
                    Msj = "La implementación del control de cambios con ID CC" + cc.id + " se aprobó.";
                    _mng.notif(Asunto, Msj, requester);
                    _mng.notif(Asunto, Msj, owner);
                    _mng.notif(Asunto, Msj, manager);
                    _mng.notif(Asunto, Msj, implmntr);
                    _mng.notif(Asunto, Msj, approver1);
                    if (aprover2.HasValue) _mng.notif(Asunto, Msj, (int)aprover2);
                    if (aprover3.HasValue) _mng.notif(Asunto, Msj, (int)aprover3);
                    break;

                case "Rejectedby7": // rechazo de implementación por reviewer
                    Asunto = "Implementación rechazada";
                    int cantidadRechazos = CantidadDeRechazosCC(cc.id);
                    if (cantidadRechazos != 3 || true) { //Reprogramación
                        Msj = "La implementación del control de cambios con ID CC" + cc.id + " se rechazó. Se requiere reprogramar el cambio.";
                    }
                    else {
                        Msj = "La implementación del control de cambios con ID CC" + cc.id + " se rechazó por tercera ocasión y ha superado el límite de rechazos, por tal motivo el Control de Cambios ha sido cerrado.";
                    }
                    _mng.notif(Asunto, Msj, requester);
                    _mng.notif(Asunto, Msj, owner);
                    _mng.notif(Asunto, Msj, manager);
                    _mng.notif(Asunto, Msj, implmntr);
                    _mng.notif(Asunto, Msj, approver1);
                    if (aprover2.HasValue) _mng.notif(Asunto, Msj, (int)aprover2);
                    if (aprover3.HasValue) _mng.notif(Asunto, Msj, (int)aprover3);
                    break;

                case "Aprovedby7": // aproación de implementación por reviewer
                    Asunto = "Control de cambios cerrado";
                    Msj = "El control de cambios con ID CC" + cc.id + " se cerró de manera exitosa.";
                    _mng.notif(Asunto, Msj, requester);
                    _mng.notif(Asunto, Msj, owner);
                    _mng.notif(Asunto, Msj, manager);
                    _mng.notif(Asunto, Msj, implmntr);
                    _mng.notif(Asunto, Msj, approver1);
                    if (aprover2.HasValue) _mng.notif(Asunto, Msj, (int)aprover2);
                    if (aprover3.HasValue) _mng.notif(Asunto, Msj, (int)aprover3);
                    _mng.notif(Asunto, Msj, lineMng);
                    _mng.notif(Asunto, Msj, reviewer);
                    break;

                default:
                    break;
            }

            //foreach (var EmplyID in listEmployeeIDs) { _mng.notif(Asunto, Msj, EmplyID); }
        }
        public ActionResult CambioEdo(DetalleCC vm, int CCid, int EmployeeId, string newEdo, int? rolcc)
        {
            var cc = _db.tbl_CC_Dashboard.Where(x => x.id == CCid).FirstOrDefault();
            var repro = 0; // pedir reprogramaci´n?
            var motivo = vm.motivo;
            if (motivo == null) motivo = "-";
            string evento = "";
            if (!rolcc.HasValue) rolcc = 0;
            bool aprobado = false; // true if aprobado por todos los approvers
            // Fase 1 = Detalle, aprobación por chmanager
            // Fase 2 = Aprobado, ahora en planeación
            // Fase 3 = planeación terminada, petición de aprobación de planeación
            // Fase 4 = Aprobado, ahora en implementación
            // Fase 5 = Implementación terminada, petición de aprobación de implementación
            // Fase 6 = Implementación aprobada por line manager
            // Fase 7 = Implementación aprobada por change reviwer

            //================= Verificar que todas las tareas tengna horarios posteriores a la actual
            if (newEdo == "PedirAprobacionAPCC") {
                bool tareasSonPreviasAHoy = false;
                DateTime hoy = DateTime.Now;
                
                var tareas = _db.tbl_CC_Tareas.Where(t => t.CC == CCid && t.Rechazo == false).ToList();
                foreach (var tarea in tareas)
                {
                    DateTime fechaInicioTarea = tarea.Fecha
                        .AddHours(tarea.Hora.Hour)
                        .AddMinutes(tarea.Hora.Minute);
                    if (fechaInicioTarea <= hoy) { tareasSonPreviasAHoy = true; }
                }

                if (tareasSonPreviasAHoy)
                return RedirectToAction("DetalleCC", "ControlCambios", new { EmployeeId = EmployeeId, CCid = CCid, tareasP = 1, rolcc = 3});
            }
            //======================================================
            bool EvitarDobleHistorico = false;
            switch (newEdo)
            {
                case "Cancel":
                    cc.Estatus = "Cancelado";
                    evento = "Control de Cambios Cancelado";
                    cc.Fase = 0;
                    break;
                case "Aprueba":
                    cc.Estatus = "Aprobado";
                    cc.Fase = 2;
                    evento = "Aprobado por Change Manager";
                    _mng.CCHis(EmployeeId, CCid, evento, motivo, 0); 
                    _db.SaveChanges();
                    return RedirectToAction("DetalleCC", "ControlCambios", new { aprobed = 1, EmployeeId = EmployeeId, CCid = CCid, rolcc = 3 });
                case "NoAprueba":
                    rolcc = 3;
                    cc.Estatus = "No Aprobado";
                    evento = "Rechazado por Change Manager";
                    if (cc.Ticket != 0 && cc.Ticket != null) // guardar en historico de ticket que fue cerrado el cc relacionado
                        _mng.SaveHistoricoCC(CCid, (int)cc.Ticket, motivo);
                    cc.Fase = 0;
                    break;
                case "PedirAprobacionAPCC":
                    rolcc = 3;
                    cc.EstatusAp = "Pendiente";
                    evento = "Petición de aprobación de planeación";
                    cc.Fase = 3;
                    break;
                case "AprobacionAPCC":
                    rolcc = 4;
                    AproverAproves(EmployeeId, CCid);
                    var NoAprovers = getCantidadAproversFromCC(CCid);
                    var NoAproveds = getCantidadAprovedsFromCC(CCid);
                    evento = "Planeación Aprobada:  " + NoAproveds + "/" + NoAprovers;
                    if (NoAproveds == NoAprovers) {  
                        cc.Fase = 4; 
                        cc.EstatusAp = "Aprobado";
                        cc.Estatus = "Trabajando";
                        aprobado = true;
                    }
                    break;
                case "RechazoAPCC":
                    rolcc = 4;
                    cc.EstatusAp = "Rechazado";
                    evento = "Planeación Rechazada";
                    cc.Estatus = "Aprobado"; //redirección a planeación
                    cc.Fase = 2;
                    break;
                case "Impfinished": // Dummy: Se manda a llamar desde ServiceDeskManager.cs / TareaHis()
                    cc.Estatus = "Pendiente"; 
                    cc.Fase = 5;
                    EvitarDobleHistorico = true;
                    break;
                case "Rejectedby6":
                    rolcc = 6;
                    cc.Estatus = "Rechazado";
                    evento = "Implementación de control de cambios rechazada."; //Implementación de control de cambios rechazada.
                    cc.Fase = 5; 
                    repro = 1; // Mandar notificación al manager 
                    RechazarTareasAlRechazarCC(CCid);
                    break;
                case "Aprovedby6":
                    rolcc = 6;
                    evento = "Implementación de control de cambios aprobada. ";
                    cc.Fase = 6;
                    break;
                case "Rejectedby7":
                    rolcc = 7;
                    cc.Estatus = "Rechazado";
                    evento = "Implementación de control de cambios rechazada.";
                    cc.Fase = 5; 
                    repro = 1; // Mandar notificación a Manager 
                    RechazarTareasAlRechazarCC(CCid);
                    break;
                case "Aprovedby7":
                    rolcc = 7;
                    cc.Estatus = "Cerrado";
                    evento = "Control de Cambios cerrado Satisfactoriamente";
                    if (cc.Ticket != 0 && cc.Ticket != null) // guardar en historico de ticket que fue cerrado el cc relacionado
                        _mng.SaveHistoricoCC(CCid, (int)cc.Ticket, motivo); 
                    cc.Fase = 7;
                    break;
                default:
                    break;
            }
            _db.SaveChanges();
            //if (cc.Fase != 5 || newEdo == "Rejectedby6" || newEdo == "Rejectedby7") 
            if (!EvitarDobleHistorico) // Cuando cc.Fase == 5 se hace el guardado en CCHis, esta linea evita un doble guardado
                _mng.CCHis(EmployeeId, CCid, evento, motivo, 0);
            if (aprobado) newEdo = "Aprobado";
            NotificacionesCC(cc, newEdo, 0);
            // if (cc.Fase != 7) { _mng.notif(Asunto, Msj, EmployeeId); }

            // En caso de que sea rechazado por line manager, y tenga ticket asociado, y sea el tercer rechazo (ultimo) pasar ticket a cerrado //Reprogramación
            //if (repro == 1) if (cc.Ticket != 0 && cc.Ticket != null) if (CantidadDeRechazosCC(CCid) > 2) _mng.SaveHistoricoCC(CCid, (int)cc.Ticket, motivo);
            //if (repro == 1 && CantidadDeRechazosCC(CCid) > 2)
            //{
            //    cc.Estatus = "Cerrado";
            //    evento = "Control de Cambios cerrado debido a rechazos excedidos";
            //    _mng.CCHis(EmployeeId, CCid, evento, motivo, 0);
            //    _db.SaveChanges();
            //    //_mng.CCHis(EmployeeId, CCid, evento, motivo, 0);
            //}

            return RedirectToAction("DetalleCC", "ControlCambios", new { EmployeeId = EmployeeId, CCid = CCid, repro = repro , rolcc = rolcc});
        }
        public void AproverAproves(int EmployeeId, int CCid) {
            var cc = _db.tbl_CC_Dashboard.Where(p => p.id == CCid).FirstOrDefault();
            int? empId2 = 0;
            int? empId3 = 0;

            //var inv = _db.tbl_CC_Involucrados.ToList();

            var empId = cc.ChangeApprover;//inv.Where(c => c.id ==  cc.ChangeApprover).FirstOrDefault().EmployeeId;
            if (cc.ChangeApprover2 != null)
                empId2 = cc.ChangeApprover2;
            if (cc.ChangeApprover3 != null)
                empId3 = cc.ChangeApprover3;

            //var empId = inv.Where(c => c.id == cc.ChangeApprover).FirstOrDefault().EmployeeId;
            //if (cc.ChangeApprover2 != null)
            //empId2 = inv.Where(c => c.id == cc.ChangeApprover2).FirstOrDefault().EmployeeId;
            //if (cc.ChangeApprover3 != null)
            //empId3 = inv.Where(c => c.id == cc.ChangeApprover3).FirstOrDefault().EmployeeId;

            if (EmployeeId == empId)  { cc.Aproval1 = true; }
            if (cc.ChangeApprover2 != null)
            if (EmployeeId == empId2) { cc.Aproval2 = true; }
            if (cc.ChangeApprover3 != null)
            if (EmployeeId == empId3) { cc.Aproval3 = true; }
            _db.SaveChanges();
        }
        public int getCantidadAprovedsFromCC(int CCid) {
            int cantidad = 0;
            var cc = _db.tbl_CC_Dashboard.Where(p => p.id == CCid).FirstOrDefault();
            if (cc.Aproval1 == true) cantidad++;
            if (cc.Aproval2 == true) cantidad++;
            if (cc.Aproval3 == true) cantidad++;
            return cantidad;
        }
        public int getCantidadAproversFromCC(int CCid) {
            int cantidad = 1;
            var cc = _db.tbl_CC_Dashboard.Where(p => p.id == CCid).FirstOrDefault();
            if (cc.ChangeApprover2 != null) { cantidad++; }
            if (cc.ChangeApprover3 != null) { cantidad++; }
            return cantidad;
        }

        // Tarea CC
        public ActionResult CrearTareaCC(DetalleCC cc, int EmployeeId, int CCid)
        {
            _mng.AddTareaCC(cc.tbl_CC_Tareas, CCid);
            var maxId = _db.tbl_CC_Tareas.Max(m => m.Id);
            _mng.CCHis(EmployeeId, CCid, "Creación de Tarea: " + maxId, "-", 0);
            _mng.TareaHis(maxId, EmployeeId, "Creación", "-");
            _mng.notif("Tarea asignada", "La tarea '" + cc.tbl_CC_Tareas.Nombre + "' (CC" + CCid + ") se te asignó.", EmployeeId);
            return RedirectToAction("DetalleCC", "ControlCambios", new { EmployeeId = EmployeeId, CCid = CCid, job = 2, rolcc = 3 });
        }
        public ActionResult EditarTareaCC(DetalleCC cc, int EmployeeId, int Tarea)
        {
            cc.tbl_CC_Tareas.Id = Tarea;
            cc.tbl_CC_Tareas.CC = cc.CCid;
            _mng.EditTareaCC(cc.tbl_CC_Tareas);
            _mng.CCHis(EmployeeId, cc.CCid, "Edición de Tarea: " + cc.tbl_CC_Tareas.Id, "-", 0);
            _mng.TareaHis(cc.tbl_CC_Tareas.Id, EmployeeId, "Edición", "-");
            return RedirectToAction("DetalleCC", "ControlCambios", new { EmployeeId = EmployeeId, job = 2, CCid = cc.CCid, rolcc = 3 });
        }
        public ActionResult DeleteTareaCC(int EmployeeId, int TareaId, int CCid)
        {
            _mng.DeleteTareaCC(TareaId);
            _mng.CCHis(EmployeeId, CCid, "Eliminación de Tarea: " + TareaId, "-", 0);
            _mng.TareaHis(TareaId, EmployeeId, "Eliminado", "-");
            return RedirectToAction("DetalleCC", "ControlCambios", new { EmployeeId = EmployeeId, CCid = CCid, job = 2, rolcc = 3 });
        }
        public ActionResult TareaCCEstatus(DetalleTareaCC vm, HttpPostedFileBase[] upload, int EmployeeId, int TareaId) {
            var tarea = _db.tbl_CC_Tareas.Where(x => x.Id == TareaId).FirstOrDefault();
            tarea.Estatus = vm.tbl_CC_Tareas.Estatus;
            //Upload img here
            if (vm.tbl_CC_Tareas.Comentario != null) { tarea.Comentario = vm.tbl_CC_Tareas.Comentario; } else tarea.Comentario = "-";
            _db.SaveChanges();
            var TareaTieneDocumento = false; // tareaCC tiene documento anexado?
            if (ModelState.IsValid)
            {
                foreach (HttpPostedFileBase file in upload)
                {
                    if (file != null)
                    {
                        var extension = file.FileName.ToUpper();
                        if (extension.EndsWith(".PDF") || extension.EndsWith(".DOC") || extension.EndsWith(".DOCX") || extension.EndsWith(".DOT")
                            || extension.EndsWith(".XLS") || extension.EndsWith(".XLSX") || extension.EndsWith(".XLSM") || extension.EndsWith(".XLT")
                            || extension.EndsWith(".PPT") || extension.EndsWith(".PPTX") || extension.EndsWith(".PPS") || extension.EndsWith(".JPG")
                            || extension.EndsWith(".PNG") || extension.EndsWith(".JPEG") || extension.EndsWith(".CSV"))
                        {
                            var InputFileName = Path.GetFileName(file.FileName);
                            var NameCarga = TareaId + "_" + file.FileName;
                            var path = Path.Combine(Server.MapPath("~/ServiceDeskV2docs/"), NameCarga);
                            //Save file to server folder  
                            file.SaveAs(path);

                            //SE GUARDA EN LA RUTA QUE SERA COMPARTIDA PARA AMBAS DIRECCIONES 35 Y 36
                            var fname = @"\\10.200.154.36\uFiles\ServiceDeskV2\\" + NameCarga;
                            System.IO.File.Copy(path, fname, true);
                            System.IO.File.Delete(path);
                            _doc.Upload(NameCarga, path, true);
                            _mng.SetArchivoCCTarea(TareaId, NameCarga);
                            TareaTieneDocumento = true;
                        }
                    }
                }
            }
            //Guardar info en el historico de la tarea
            _mng.TareaHis(TareaId, EmployeeId, tarea.Estatus, tarea.Comentario);
            //------------------------------------ agregar notificaciones
            var cc = _db.tbl_CC_Dashboard.Where(c => c.id == tarea.CC).FirstOrDefault();
            NotificacionesCC(cc, tarea.Estatus, tarea.Id);
            if (TareaTieneDocumento){ }
            else                    { }
            return RedirectToAction("DetalleTareaCC", "ControlCambios", new { EmployeeId = EmployeeId, TareaId = TareaId, refresh = 1 });
        }
           
        // Configuración
        public ActionResult AñadirImplementer(ControlCambios vm, int EmployeeId)
        {
            var id = Int32.Parse(vm.tbl_CC_Implementer.Nombre);
            var user = _db.tbl_User.Where(c => c.EmpleadoID == id).FirstOrDefault();
            //var grupo = _db.catGrupoResolutor.Where(c => c.Grupo == user.GrupoResolutor).FirstOrDefault();
            //_mng.AddImplementer(grupo.Id, user.NombreTecnico, user.Correo, user.EmpleadoID);
            _mng.AddImplementer(vm.tbl_CC_Implementer.GrupoResolutor, user.NombreTecnico, user.Correo, user.EmpleadoID);
            return RedirectToAction("Gestiones", "ControlCambios", new { EmployeeId = EmployeeId, job = 1 });
        }
        public ActionResult EditarImplementer(ControlCambios vm, int EmployeeId, int ImpId)
        {
            var id = Int32.Parse(vm.tbl_CC_Implementer.Nombre);
            var user = _db.tbl_User.Where(c => c.EmpleadoID == id).FirstOrDefault();
            //var grupo = _db.catGrupoResolutor.Where(c => c.Grupo == user.GrupoResolutor).FirstOrDefault();

            var tbl = new tbl_CC_Implementer();
            tbl.id = ImpId;
            tbl.Nombre = user.NombreTecnico;
            tbl.CorreoElectronico = user.Correo;
            tbl.EmployeeId = user.EmpleadoID;
            //tbl.GrupoResolutor = grupo.Id;
            tbl.GrupoResolutor = vm.tbl_CC_Implementer.GrupoResolutor;
            tbl.Activado = true;

            _mng.EditImplementer(tbl);
            return RedirectToAction("Gestiones", "ControlCambios", new { EmployeeId = EmployeeId, job = 1 });
        }
        public ActionResult AñadirInvolucrado(ControlCambios vm, int EmployeeId)
        {
            string Perfil = vm.tbl_CC_Involucrados.Perfil;
            //var grupo = vm.tbl_CC_Involucrados.GrupoResolutor;

            var grupo = 0;
            int id = Int32.Parse(vm.tbl_CC_Involucrados.Nombre);
            string CorreoElectronico = vm.tbl_CC_Involucrados.CorreoElectronico;
            var user = _db.tbl_User.Where(c => c.EmpleadoID == id).FirstOrDefault();

            _mng.AddInvolucrado(Perfil, user.NombreTecnico, user.Correo, user.EmpleadoID, grupo);
            return RedirectToAction("Gestiones", "ControlCambios", new { EmployeeId = EmployeeId, job = 2 });
        }
        public ActionResult EditarInvolucrado(ControlCambios vm, int EmployeeId, int InvId)
        {
            string Perfil = vm.tbl_CC_Involucrados.Perfil;
            //var grupo = vm.tbl_CC_Involucrados.GrupoResolutor;

            int id = Int32.Parse(vm.tbl_CC_Involucrados.Nombre);
            string CorreoElectronico = vm.tbl_CC_Involucrados.CorreoElectronico;
            var user = _db.tbl_User.Where(c => c.EmpleadoID == id).FirstOrDefault();

            var tbl = new tbl_CC_Involucrados(); 
            tbl.id = InvId;
            tbl.Perfil = Perfil;
            tbl.Nombre = user.NombreTecnico;
            tbl.CorreoElectronico = user.Correo;
            tbl.EmployeeId = user.EmpleadoID;
            tbl.Activado = true;
            tbl.GrupoResolutor = vm.tbl_CC_Involucrados.GrupoResolutor;

            _mng.EditInvolucrado(tbl);
            return RedirectToAction("Gestiones", "ControlCambios", new { EmployeeId = EmployeeId, job = 2 });
        }
        public ActionResult AñadirCaracteristica(ControlCambios vm, int EmployeeId)
        {
            string tipo = vm.tbl_CC_Caracteristicas.Tipo;
            string detalle = vm.tbl_CC_Caracteristicas.Detalle;
            _mng.AddCaracteristica(tipo, detalle);
            return RedirectToAction("Gestiones", "ControlCambios", new { EmployeeId = EmployeeId, job = 3 });
        }
        public ActionResult EditarCaracteristica(ControlCambios vm, int EmployeeId, int CarId)
        {
            var tbl = new tbl_CC_Caracteristicas();
            tbl.Detalle = vm.tbl_CC_Caracteristicas.Detalle;
            tbl.Tipo = vm.tbl_CC_Caracteristicas.Tipo;
            tbl.id = CarId;
            tbl.Activado = true;
            ;
            _mng.EditCaracteristica(tbl);
            return RedirectToAction("Gestiones", "ControlCambios", new { EmployeeId = EmployeeId, job = 3 });
        }
        public ActionResult DeleteFromGrid(int type, int EmployeeId, int id) {
            int job = 0;
            if (type == 3) { _mng.DeleteImplementer(id);    job = 1; }
            if (type == 2) { _mng.DeleteInvolucrado(id);    job = 2; }
            if (type == 1) { _mng.DeleteCaracteristica(id); job = 3; }
            return RedirectToAction("Gestiones", "ControlCambios", new { EmployeeId = EmployeeId, job = job });
        }
        public string RoldeUsuario(int EmployeeID) 
        {
            string rol = "";

            var numemp = _adm.tblUser.Where(a => a.EmpleadoId == EmployeeID).FirstOrDefault();
            var rols = Roles.GetRolesForUser(numemp.UserName);

            var ptoSupRol = _mng.GetRolByPuesto(rols);

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
    }
}
