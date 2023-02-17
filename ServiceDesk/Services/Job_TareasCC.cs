using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceDesk.Models;
using System.Globalization;
using ServiceDesk.Managers;

namespace QuartzScheduler.Services
{
    public class TareaCC : IJob
    {
        private readonly ServiceDeskContext db = new ServiceDeskContext();
        private readonly ServiceDeskManager _mng = new ServiceDeskManager();
        public void Execute(IJobExecutionContext context)
        {
            // cuando cc activo cc.estatus == Trabajando y cc.fase == 4
            var list_ActiveCC = db.tbl_CC_Dashboard.Where(cc => cc.Estatus == "Trabajando").ToList().Select(cc => cc.id);
            var tareasCC = db.tbl_CC_Tareas.Where(tarea => list_ActiveCC.Contains(tarea.CC) && tarea.Estatus == "Solicitado").ToList(); // trae una lista de tareas que pertenezcan a CCs que estén en la lista de CCs activos
            foreach (var tarea in tareasCC)
            {
                if (tarea.Fecha.Date == DateTime.Now.Date) {
                    if (tarea.Hora.Hour == DateTime.Now.Hour) {
                        _mng.notif("Inicia tu tarea (CC" + tarea.CC +")" , "Es tu turno para realizar la tarea: " + tarea.Nombre, tarea.Tecnico);
                    }
                }
            }
        }
    }
}