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
    public class TareasProgramadas : IJob
    {
        private readonly ServiceDeskContext _db = new ServiceDeskContext();
        private readonly ServiceDeskManager _mng = new ServiceDeskManager();
        public void Execute(IJobExecutionContext context)
        {
            System.Diagnostics.Debug.WriteLine("Hello There!! -----------------: Inicio de contador " + DateTime.Now);
            var today = DateTime.Now.Date;

            var listaTareas = _db.tblTareasProgramadas.Where(t => t.Activado == true).ToList();
            foreach (var tarea in listaTareas)
            {
                // si las 3 banderas se vuelven true, la tarea toca a la hora en que se ejecuta esta clase,
                // en ese momento se activa la tarea y solo se desactiva cuando se sale del rango de fechas (tareaRangoFechas = false)
                var tareaTocaHoy = TareaTocaHoy(tarea);                     // según día y periodo (mensual, semanal, etc...)
                var tareaRangoFechas = TareaRangoFechas(tarea, today);      // dentro del rango de fechas (fecha inicial y fecha final)
                var tareaHora = TareaHorario(tarea);                        // según la hora del día (ie. 5:00pm)

                // OLD CODE
                //(DateTime.Now.Hour > tarea.Hora.Hour) ? true : false;  // según la hora del día (ie. 5:00pm)
                //if (DateTime.Now.Hour == tarea.Hora.Hour && DateTime.Now.Minute > tarea.Hora.Minute) { tareaHora = true; } 
                //(today >= tarea.FechaInicial && today <= tarea.FechaFinal) ? true : false;  // dentro del rango de fechas (fecha inicial y fecha final)

                // activarlas en la BD          // OLD CODE: tarea.Activado_2 = tareaTocaHoy && tareaRangoFechas && tareaHora ? true : false;
                bool TareaRecienActivada = false;
                if (tareaTocaHoy && tareaRangoFechas && tareaHora) { tarea.Activado_2 = true; }
                if (!tareaRangoFechas) { tarea.Activado_2 = false; }
                if (tarea.Activado_2 && 
                    DateTime.Now.Hour == tarea.Hora.Hour && 
                    DateTime.Now.Minute == tarea.Hora.Minute) 
                { NotificarTecnico(tarea); TareaRecienActivada = true; }

                if (tarea.Activado_2)
                {
                   // System.Diagnostics.Debug.WriteLine("{0} Tarea programada ha sido programada para la siguiente hora \n     técnico con ID {1} debe ser notificado", tarea.Id, tarea.TecnicoID);
                    System.Diagnostics.Debug.WriteLine("{0} TP esta en estado activo", tarea.Id);

                    if (tarea.Estatus == "Cerrado") {
                        if (TareaRecienActivada)
                        {
                            tarea.Estatus = "Asignado";
                        }
                        //var hist = _db.his_TareasProgramadas.Where(t => t.IdTarea == tarea.Id).OrderByDescending(t => t.FechaRegistro).FirstOrDefault();
                        //if (!Ultimo_Registro_Durante_Ultima_Hora(hist)) {
                        //    tarea.Estatus = "Asignado";
                        //}
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("{0}: No entro debido a: ", tarea.Id);
                    if (!tareaTocaHoy) { System.Diagnostics.Debug.WriteLine("      Día", tarea.Id); }
                    if (!tareaRangoFechas) { System.Diagnostics.Debug.WriteLine("      Rango de fechas", tarea.Id); }
                    if (!tareaHora) { System.Diagnostics.Debug.WriteLine("      fuera de hora", tarea.Id); }
                }
            }
            _db.SaveChanges();
        }
        static bool Ultimo_Registro_Durante_Ultima_Hora(his_TareasProgramadas his) {
            if (his.IdTarea == 13 || his.IdTarea == 12) {
                ;
            }
            // El último registro fue hecho durante la ultima hora?
            bool resultado = false;
            DateTime fechaRegistro = his.FechaRegistro;
            DateTime ahora = DateTime.Now;

            if (fechaRegistro.Hour == ahora.Hour) { resultado = true; }
            else { resultado = false; }

            return resultado;
        }
        static bool TareaHorario(tbl_TareasProgramadas tarea) {
            bool resultado = false;

            // resultado = (DateTime.Now.Hour > tarea.Hora.Hour) ? true : false;
            if (DateTime.Now.Hour == tarea.Hora.Hour && DateTime.Now.Minute > tarea.Hora.Minute) { resultado = true; }

            return resultado;
        }
        public void NotificarTecnico(tbl_TareasProgramadas tarea)
        {
            System.Diagnostics.Debug.WriteLine("Avisando a tecnico con ID {1}, sobre tarea {0}", tarea.Id, tarea.TecnicoID);
            //_mng.notif("Inicia tu tarea (id:" + tarea.Id + ")", "Es tu turno para realizar la tarea con " + tarea.Activo, tarea.TecnicoID);
        }
        static bool TareaRangoFechas(tbl_TareasProgramadas tarea, DateTime date) {
            bool resultado = false;
            resultado = (date >= tarea.FechaInicial && date <= tarea.FechaFinal) ? true : false;
            return resultado;
        }
        static bool TareaTocaHoy(tbl_TareasProgramadas tarea)
        {
            var flag1 = false;
            var today = DateTime.Now.Date;
            var DyWeek = (int)DateTime.Now.DayOfWeek;
            var DyMonth = DateTime.Today.Day;

            var periodo = tarea.Periodo; foreach (char c in periodo) periodo = periodo.Replace(" ", String.Empty);
            // Tarea toca el día de hoy ? ------------------------------------------------
            if (periodo == "Semanal")
            {
                if (DyWeek == 0 && tarea.seDomingo) { flag1 = true; }
                if (DyWeek == 1 && tarea.seLunes) { flag1 = true; }
                if (DyWeek == 2 && tarea.seMartes) { flag1 = true; }
                if (DyWeek == 3 && tarea.seMiercoles) { flag1 = true; }
                if (DyWeek == 4 && tarea.seJueves) { flag1 = true; }
                if (DyWeek == 5 && tarea.seViernes) { flag1 = true; }
                if (DyWeek == 6 && tarea.seSabado) { flag1 = true; }
            }

            if (periodo == "Mensual" && tarea.DiadelMes == DyMonth) { flag1 = true; }
            var dayOfWeek = DayOfWeek.Monday;
            if (tarea.DiadelaSemana == 2) dayOfWeek = DayOfWeek.Tuesday;
            if (tarea.DiadelaSemana == 3) dayOfWeek = DayOfWeek.Wednesday;
            if (tarea.DiadelaSemana == 4) dayOfWeek = DayOfWeek.Thursday;
            if (tarea.DiadelaSemana == 5) dayOfWeek = DayOfWeek.Thursday;
            if (tarea.DiadelaSemana == 6) dayOfWeek = DayOfWeek.Saturday;
            if (tarea.DiadelaSemana == 7) dayOfWeek = DayOfWeek.Sunday;

            //int lastDay = DateTime.DaysInMonth(today.Year, today.Month);
            DateTime primerDiadelMes = new DateTime(today.Year, today.Month, 1);
            DateTime hoy = new DateTime(today.Year, today.Month, today.Day);
            var cantidad = CountDays(dayOfWeek, primerDiadelMes, hoy);
            var cardinal = tarea.DiaCardinal; // cardinal++;
            if (periodo == "Mensual" && tarea.DiadelMes == 0 && cardinal == cantidad) { flag1 = true; }

            return flag1;
        }
        static int CountDays(DayOfWeek day, DateTime start, DateTime end)
        {
            TimeSpan ts = end - start;                       // Total duration
            int count = (int)Math.Floor(ts.TotalDays / 7);   // Number of whole weeks
            int remainder = (int)(ts.TotalDays % 7);         // Number of remaining days
            int sinceLastDay = (int)(end.DayOfWeek - day);   // Number of days since last [day]
            if (sinceLastDay < 0) sinceLastDay += 7;         // Adjust for negative days since last [day]

            // If the days in excess of an even week are greater than or equal to the number days since the last [day], then count this one, too.
            if (remainder >= sinceLastDay) count++;

            return count;
        }
    }
}