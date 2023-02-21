using Quartz;
using System.Linq;
using ServiceDesk.Models;
using ServiceDesk.Managers;
using System.Data.Entity.Migrations;

namespace QuartzScheduler.Services
{
    public class SendNotificaciones : IJob
    {
        private readonly ServiceDeskContext db = new ServiceDeskContext();
        private readonly NotificacionesManager nt = new NotificacionesManager();
        public void Execute(IJobExecutionContext context)
        {
            // Servicio que manda notificaiones en el fondo
            var Notificaciones_Sin_Mandar = db.Notificaciones.Where(t => t.Enviada == false).ToList();
            foreach (var notificacion in Notificaciones_Sin_Mandar)
            {
                nt.SendEmailByEmployeeId(notificacion.EmpleadoId, notificacion.Mensaje);
                notificacion.Enviada = true;
                db.Notificaciones.AddOrUpdate(notificacion);
                db.SaveChanges();
            }

        }
    }
}