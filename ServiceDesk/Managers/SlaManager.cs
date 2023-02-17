using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceDesk.ViewModels;
using ServiceDesk.Models;
using System.Data.Entity.Migrations;
using System.Globalization;

namespace ServiceDesk.Managers
{
    public class SlaManager
    {
        private readonly ServiceDeskContext _sd = new ServiceDeskContext();        
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<SlaTimesVm> GetSlaTimes(List<his_Ticket> historic)
        {

            var list = new List<SlaTimesVm>();
            var listactual = new List<SlaTimesVm>();
            var HrsJornada = 8;
            var inicioJrn = new TimeSpan(9, 00, 00);
            var finJrn = new TimeSpan(17, 00, 00);
            var slaObj = 0; // usado en GetTiempoActual


            //ORDENAMIENTO POR ID HISTORICO
            historic = historic.OrderBy(a => a.IdHis).ToList();


            //SLA TOTAL Y SLA OBJETIVO
            if (historic.Count > 0)
            {
                //Fechas actuales
                var fechainicio = new DateTime();
                var fecharesuelto = new DateTime();



                var abierto = historic.Where(x=> x.EstatusTicket==1 ).OrderBy(x=> x.IdHis).FirstOrDefault();
                //var abierto = historic.Where(x => x.EstatusTicket == 1).OrderByDescending(x => x.IdHis).FirstOrDefault(); // original _ISF
                var subcategoria = _sd.cat_SubCategoria.Find(abierto.SubCategoria);//Para obtener SLA obj y Tmpo Garantía _ AYB
                if (subcategoria == null) //Arreglo Temporal _ISF
                {
                    subcategoria = new cat_SubCategoria();
                    subcategoria.SLA = ""; 
                }
                var temp = subcategoria.SLA;
                if (temp == "") subcategoria.SLA = "99"; //Arreglo Temporal _ISF
                var slaobjetivo = int.Parse(subcategoria.SLA);//Obtiene SLA obj _ AYB
                slaObj = slaobjetivo; // usado en GetTiempoActual

                var objetivo = new SlaTimesVm
                {
                    Type = "SLA Objetivo",
                    Order = 1,
                    Time = GetHoursFormat(new TimerVm { Horas = slaobjetivo, Minutos = 0 }),
                    Enable = true,
                    Tecnico = "-"
                };
                list.Add(objetivo);

               
                if (abierto != null)
                {
                    //Cuando se asigna el ticket, comienza el tiempo actual _ AYBL

                    fechainicio = abierto.FechaRegistro;


                    var resuelto = historic.FirstOrDefault(a => a.EstatusTicket == 4);

                    //Cambia a cerrado


                    if (resuelto != null)
                    {
                        //Aqui iria la validación de Abierto == Historial a false
                        var reapertura = historic.Any(a => a.IdHis > resuelto.IdHis && a.EstatusTicket == 1);

                        if (reapertura == false)
                        {
                            fecharesuelto = resuelto.FechaRegistro;

                        }
                        else 
                        {

                            fecharesuelto = DateTime.Now;
                        }

                    }
                    else
                    {
                        fecharesuelto = DateTime.Now;
                    }
                    var days = (fecharesuelto - fechainicio).Days;

                    var totaltime = GetHoursDifference(fechainicio, fecharesuelto, inicioJrn, finJrn, true);
                    var colortotal = GetColor(totaltime.Horas, slaobjetivo);

                    var total = new SlaTimesVm
                    {
                        Type = "SLA total",
                        Order = 3,
                        Time = GetHoursFormat(totaltime),
                        Enable = true,
                        Color = colortotal,
                        Tecnico = "-"
                    };
                    list.Add(total);




                    //OBTENER USUARIOS MODIFICADORES DEL TICKET
                    //var tecnicos = historic.OrderBy(a=>a.IdHis).Select(a => a.TecnicoAsignado).Distinct();

                    //AGREGADO SLA ACTUAL
                    string tecnico = "";
                    var fecharegistro = new DateTime();
                    var horas = 0;
                    var minutos = 0;
                    var estatusprev = 0;
                    var horasesp = 0;
                    var minutosesp = 0;
                    var enespera = false;
                    var Grupo = "";
                    var DataTecnico = new tbl_User();
                    foreach (var his in historic)
                    {
                        //Sacammos jornada del tecnico
                        if(his.TecnicoAsignado!=null && his.TecnicoAsignadoReag==null && his.TecnicoAsignadoReag2 == null)
                        {
                             DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == his.TecnicoAsignado).FirstOrDefault();
                        }
                        else if (his.TecnicoAsignado != null && his.TecnicoAsignadoReag != null && his.TecnicoAsignadoReag2 == null)
                        {
                             DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == his.TecnicoAsignadoReag).FirstOrDefault();
                        }
                        else if (his.TecnicoAsignado != null && his.TecnicoAsignadoReag != null && his.TecnicoAsignadoReag2 != null)
                        {
                             DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == his.TecnicoAsignadoReag2).FirstOrDefault();
                        }

                        if (DataTecnico != null)
                        {
                            if (DataTecnico.NombreTecnico != null)
                            {
                                var Ini = Convert.ToDateTime(DataTecnico.HoraInicioATC);
                                inicioJrn = new TimeSpan(Ini.Hour, Ini.Minute, 00);
                                var Fin = Convert.ToDateTime(DataTecnico.HoraFinATC);
                                finJrn = new TimeSpan(Fin.Hour, Fin.Minute, 00);
                            }
                            else
                            {
                                inicioJrn = new TimeSpan(9, 00, 00);
                                finJrn = new TimeSpan(17, 00, 00);
                            }
                        }
                        else
                        {
                            inicioJrn = new TimeSpan(9, 00, 00);
                            finJrn = new TimeSpan(17, 00, 00);
                        }


                        //Corregimos Minutos Fuera de Rango
                        if (minutos > 60)
                        {
                            var time = TimeSpan.FromMinutes(minutos);

                            horas = horas + time.Hours;
                            minutos = time.Minutes;
                        }
                        if (enespera == true)
                        {
                            //SI EL REGISTRO ANTERIOR FUE EN ESPERA SE OBTIENEN TIEMPOS DE ESPERA
                            var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn, false);
                            horasesp = horasesp + difdias.Horas;
                            minutosesp = minutosesp + difdias.Minutos;

                            tecnico = his.TecnicoAsignado;
                            
                            fecharegistro = his.FechaRegistro;
                            estatusprev = his.EstatusTicket;
                            enespera = false;
                            continue;
                        }
                        if (his.EstatusTicket == 1)
                        {
                            //DO NOTHING
                            fecharegistro = his.FechaRegistro;

                        }
                        //OBTENER REGISTRO PRIMERA ASIGNACION
                        else if (his.EstatusTicket == 2)
                        {
                            //ESTATUS PRIMERA ASIGNACION O RE ASIGNACION HACIA OTRO TECNICO
                            if (!string.IsNullOrEmpty(tecnico) && his.TecnicoAsignado != tecnico)
                            {
                                //SE GENERA SLA ACTUAL DEL TCNICO ANTERIOR Y SE AGREGAN DATOS TEMPROALES DEL NUEVO
                                var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn, false);
                                horas = horas + difdias.Horas;
                                minutos = minutos + difdias.Minutos;

                                //COMO EL TICKET FUE REASIGNADO SE ENVIAN SLA ACTUAL ANTERIOR TECNICO // AYB Aqui el sla actual debe comenzar en cero
                                //y el tiempo que hubo antes se queda temporal para al final sumarse, de lo contrario afectara al nuevo tecnico
                                var timer = new TimerVm { Horas = horas, Minutos = minutos };
                                var actual = new SlaTimesVm
                                {
                                    Type = "Tiempo Actual",
                                    Order = 2,
                                    Time = GetHoursFormat(timer),
                                    Enable = true,
                                    Color = GetColor(timer.Horas, slaobjetivo),
                                    Tecnico = tecnico
                                };
                                listactual.Add(actual);

                                //SE DEBEN RESETEAR LOS TIEMPOS
                                horas = 0;
                                minutos = 0;
                            }
                            //INICIO DE RANGO PARA TIEMPO EN ESPERA
                            tecnico = his.TecnicoAsignado;
                            if (his.NoAsignaciones != 0 && his.GrupoResolutor != Grupo)
                            {
                                fecharegistro = his.FechaRegistro;
                            }

                            Grupo = his.GrupoResolutor;
                     

                            estatusprev = his.EstatusTicket;
                            enespera = false;



                        }
                        else if (his.EstatusTicket == 3)
                        {
                            //SE ENCUENTRA EN TRABAJANDO 
                            var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn, false);
                            horas = horas + difdias.Horas;
                            minutos = minutos + difdias.Minutos;


                            //ASIGNACION DE FECHAREGISTRO ULTIMA
                            tecnico = his.TecnicoAsignado;

                            fecharegistro = his.FechaRegistro;
                            estatusprev = his.EstatusTicket;
                            enespera = false;
                        }
                        else if (his.EstatusTicket == 4)
                        {
                            //TICKET SE ENCUENTRA RESUELTO
                            var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn, false);
                            horas = horas + difdias.Horas;
                            minutos = minutos + difdias.Minutos;

                            tecnico = his.TecnicoAsignado;
                            
                            fecharegistro = his.FechaRegistro;
                            estatusprev = his.EstatusTicket;
                            enespera = false;

                            //COMO EL TICKET ESTA RESUELTO SE OBTIENE EL SLA ACTUAL DEL TICKET
                            var timer = new TimerVm { Horas = horas, Minutos = minutos };
                            var actual = new SlaTimesVm
                            {
                                Type = "Tiempo Actual",
                                Order = 2,
                                Time = GetHoursFormat(timer),
                                Enable = true,
                                Color = GetColor(timer.Horas, slaobjetivo),
                                Tecnico = "-"
                            };
                            listactual.Add(actual);
                        }
                        else if (his.EstatusTicket == 7)
                        {                            

                            //TICKET SE ENCUENTRA EN ESPERA                          
                            //SUMAR TIEMPO HASTA ESTA FECHA
                            var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn, false);
                            horas = horas + difdias.Horas;
                            minutos = minutos + difdias.Minutos;

                            //INICIO DE RANGO PARA TIEMPO EN ESPERA
                            tecnico = his.TecnicoAsignado;
                            
                            fecharegistro = his.FechaRegistro;
                            estatusprev = his.EstatusTicket;
                            enespera = true;
                        }

                    }
                    //VALIDA SI ES EL ULTIMO REGISTRO Y CONTIENE HORAS
                    if (!string.IsNullOrEmpty(tecnico))
                    {
                        var difdias = new TimerVm();

                        if (resuelto == null)
                        {
                            var Cerrado = historic.OrderByDescending(x => x.IdHis).FirstOrDefault(a => a.EstatusTicket == 6);

                            if (Cerrado != null)
                            {
                                difdias = GetHoursDifference(fecharegistro, Cerrado.FechaRegistro, inicioJrn, finJrn, false);
                            }
                            else
                            {
                                difdias = GetHoursDifference(fecharegistro, DateTime.Now, inicioJrn, finJrn, false);
                            }

                            horas = horas + difdias.Horas;
                            minutos = minutos + difdias.Minutos;

                            //GENERACION DE ULTIMO REGISTRO
                            var timer = new TimerVm { Horas = horas, Minutos = minutos };
                            var actual = new SlaTimesVm
                            {
                                Type = "Tiempo Actual",
                                Order = 2,
                                Time = GetHoursFormat(timer),
                                Enable = true,
                                Color = GetColor(timer.Horas, slaobjetivo),
                                Tecnico = tecnico
                            };
                            listactual.Add(actual);
                        }

                       
                    }

                    if (listactual.Count > 0)
                    {
                        var ultimotec = listactual.LastOrDefault();
                        list.Add(ultimotec);
                    }

                    //VALIDA SI HUBO TIEMPO EN ESPERA DEL TICKET
                    if ((horasesp != 0 || minutosesp != 0) || enespera)
                    {
                        if (enespera)
                        {
                            var difdias = GetHoursDifference(fecharegistro, DateTime.Now, inicioJrn, finJrn, false);
                            horasesp = horasesp + difdias.Horas;
                            minutosesp = minutosesp + difdias.Minutos;


                            //COMO EL TICKET ESTA RESUELTO SE OBTIENE EL SLA ACTUAL DEL TICKET
                            var timer = new TimerVm { Horas = horasesp, Minutos = minutosesp };
                            var timeesp = new SlaTimesVm
                            {
                                Type = "En espera",
                                Order = 3,
                                Time = GetHoursFormat(timer),
                                Enable = true,
                                Color = "#D7F8EE",
                                Tecnico = "-"
                            };
                            list.Add(timeesp);
                        }
                        else
                        {
                            //COMO EL TICKET ESTA RESUELTO SE OBTIENE EL SLA ACTUAL DEL TICKET
                            var timer = new TimerVm { Horas = horasesp, Minutos = minutosesp };
                            var timeesp = new SlaTimesVm
                            {
                                Type = "En espera",
                                Order = 3,
                                Time = GetHoursFormat(timer),
                                Enable = true,
                                Color = "#D7F8EE",
                                Tecnico = "-"
                            };
                            list.Add(timeesp);
                        }

                    }

                    //TIEMPOS DE GARANTIA 
                    var isgarantia = historic.Any(a => a.EstatusTicket == 5);
                    if (isgarantia)
                    {
                        var garantia = historic.Last(a => a.EstatusTicket == 5);
                        var cerrado = historic.FirstOrDefault(a => a.EstatusTicket == 6 && a.IdHis > garantia.IdHis);
                        if (cerrado != null)
                        {
                            var difdias = GetHoursDifference24Hrs(garantia.FechaRegistro, cerrado.FechaRegistro);

                            var timer = new TimerVm { Horas = difdias.Horas, Minutos = difdias.Minutos };
                            var engarantia = new SlaTimesVm
                            {
                                Type = "En Garantia",
                                Order = 4,
                                Time = GetHoursFormat(timer),
                                Enable = true,
                                Color = "#DAF7A6",
                                Tecnico = "-"
                            };
                            list.Add(engarantia);
                        }
                        else
                        {
                            var difdias = GetHoursDifference24Hrs(garantia.FechaRegistro, DateTime.Now);
                                                       

                            var timer = new TimerVm { Horas = difdias.Horas, Minutos = difdias.Minutos };
                            var engarantia = new SlaTimesVm
                            {
                                Type = "En Garantia",
                                Order = 4,
                                Time = GetHoursFormat(timer),
                                Enable = true,
                                Color = "#DAF7A6",
                                Tecnico = "-"
                            };
                            list.Add(engarantia);
                        }
                    }
                }
            }
            else { }
            
            //GetSlaTiempoActual(list, slaObj);

            return list.OrderBy(a => a.Order).ToList();
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<SlaTimesVm> GetSlaTiempoActual(List<SlaTimesVm> slatimes, int slaobjetivo)
        {
            var list = new List<SlaTimesVm>();
            int TiempoEnEsperaEnMinutos = 0;
            int TiempoActual_EnMinutos = 0;

            //Obtener el total del "En espera" del tecnico actual
            var EnEspera = slatimes.Where(t => t.Type == "En espera").FirstOrDefault();
            if (EnEspera != null) { 
                var TiempoEnEspera = EnEspera.Time;
                string[] strTiempoEnEspera = TiempoEnEspera.Split(':');
                int te_Hora = Int32.Parse(strTiempoEnEspera[0]);
                int te_Minu = Int32.Parse(strTiempoEnEspera[1]);
                if (te_Hora != 0)  TiempoEnEsperaEnMinutos = (te_Hora * 60) + te_Minu;
                else TiempoEnEsperaEnMinutos = te_Minu;
            }

            //Restarle al tiempo actual el tiempo 
            var TiempoActual = slatimes.Where(t => t.Type == "Tiempo Actual").FirstOrDefault().Time;
            string[] strTiempoActual = TiempoActual.Split(':');
            int ta_Hora = Int32.Parse(strTiempoActual[0]);
            int ta_Minu = Int32.Parse(strTiempoActual[1]);
            if (ta_Hora != 0) TiempoActual_EnMinutos = (ta_Hora * 60) + ta_Minu;
            else TiempoActual_EnMinutos = ta_Minu;

            TiempoActual_EnMinutos = TiempoActual_EnMinutos - TiempoEnEsperaEnMinutos;

            list = slatimes;
            string hr = (TiempoActual_EnMinutos / 60).ToString("00");
            string min = (TiempoActual_EnMinutos % 60).ToString("00");
            list.Where(t => t.Type == "Tiempo Actual").FirstOrDefault().Time = hr + ":" + min;
            list.Where(t => t.Type == "Tiempo Actual").FirstOrDefault().Color = GetColor(Int32.Parse(hr), slaobjetivo);

            ;
            return list;
        }
        //HORAS TECNICO
        public TimerVm GetHoursDifference(DateTime start, DateTime end, TimeSpan inijrn, TimeSpan finjrn, bool apertura)
        {

            //OBTENCION DE HORAS JORNADA EN BASE A SU INICIO Y FIN DE TURNO


            int hrjs = (finjrn - inijrn).Hours;

            var festivos = new List<DateTime> {
                new DateTime(2022,03,27)
            };

            var horas = 0;
            var minutos = 0;

            //Si es en el mismo dia y dentro de las horas jornadas solamente sacamos la diferencia entre horarios
            if (start.Date==end.Date & start.TimeOfDay>= inijrn && end.TimeOfDay<=finjrn && apertura == false)
            {
                var time = (end.TimeOfDay - start.TimeOfDay);
                horas = horas + time.Hours;
                minutos = time.Minutes;
            }
            else
            {
                for (DateTime counter = start; counter.Date <= end.Date; counter = counter.AddDays(1))
                {

                    //counter.Date = a la fecha que esta iterando 
                    //start.Date = fecha de registro del registro anterior
                    //end.Date = a la fecha con la que estoy comparando
                    //apertura = cuando es la primera apertura del ticket actual | True == apertura (abierto) | False = otro estatus

                    if (counter.Date == end.Date && start.TimeOfDay > inijrn && apertura == true)
                    {
                        //TICKET ASIGNADO DESPUES DEL INICIO DE JORNADA EN APERTURA
                        var time = (end.TimeOfDay - start.TimeOfDay);
                        horas = horas + time.Hours;
                        minutos = time.Minutes;
                    }
                    else if (counter.Date == end.Date && start.TimeOfDay < inijrn && apertura == false)
                    {
                        //TICKET ASIGNADO DESPUES DEL INICIO DE JORNADA E
                        var time = (end.TimeOfDay - inijrn);
                        horas = horas + time.Hours;
                        minutos = time.Minutes;
                    }
                    else if (counter.Date == end.Date && start.TimeOfDay > inijrn && end.TimeOfDay< finjrn && apertura == false)
                    {
                        //TICKET ASIGNADO DESPUES DEL INICIO DE JORNADA E
                        var time = (end.TimeOfDay - inijrn);                        
                        horas = horas + time.Hours;
                        minutos = time.Minutes;
                    }
                    else if (counter.Date == start.Date && start.TimeOfDay > inijrn && apertura == false)
                    {
                        //TICKET ASIGNADO DESPUES DEL INICIO DE JORNADA E
                        var time = (finjrn - start.TimeOfDay);
                        horas = horas + time.Hours;
                        minutos = time.Minutes;
                    }
                    else
                    {
                        horas = horas + hrjs;
                    }
                }
            }

            

            //Sacamos SLA TOTAL
            if (apertura == true)
            {
                TimeSpan span = (end - start);

                String.Format("{0} days, {1} hours, {2} minutes, {3} seconds",
                    span.Days, span.Hours, span.Minutes, span.Seconds);

                //

                horas = (span.Days * 24) + span.Hours;
                minutos = span.Minutes;
            }

            if (horas < 0)
            {
                horas = 0;
            }

            if (minutos < 0)
            {
                minutos = 0;
            }

            return new TimerVm { Horas = horas, Minutos = minutos };
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //Contador de GARANTÍA
        public TimerVm GetHoursDifference24Hrs(DateTime start, DateTime end)
        {

            //OBTENCION DE HORAS JORNADA EN BASE A SU INICIO Y FIN DE TURNO


            int hrjs = 24;

            var festivos = new List<DateTime> {
                new DateTime(2022,03,27)
            };

            var horas = 0;
            var minutos = 0;
            for (DateTime counter = start; counter <= end; counter = counter.AddDays(1))
            {
                if (counter.Date == end.Date)
                {
                    //TICKET ASIGNADO DESPUES DEL INICIO DE JORNADA EN APERTURA
                    var time = (end.TimeOfDay - start.TimeOfDay);
                    horas = horas + time.Hours;
                    minutos = minutos + time.Minutes;
                }
                else
                {
                    horas = horas + hrjs;
                }
            }

            return new TimerVm { Horas = horas, Minutos = minutos };
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public string GetColor(int currentsla, int slaobjetivo)
        {

            if (currentsla == 0 && slaobjetivo == 0)
            {
                //DEFAULT COLOR
                return "#ffffff";
            }
            /*
             TIEMPO GARANTIA
               #DAF7A6
             */
            int porc = (currentsla * 100) / slaobjetivo;
            if (currentsla > slaobjetivo)
            {
                return "#ED3A3D";
            }
            else
            {
                if (porc > 51 && porc < 70)
                {
                    //AMARILLO
                    return "#ffc009";
                }
                else if (porc >= 71)
                {
                    //NARANJA
                    return "#f9a938";
                }
                else
                {
                    //VERDE
                    return "#28a745";
                }
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public string GetHoursFormat(TimerVm timer)
        {
            var horastext = timer.Horas.ToString();
            var minutostext = timer.Minutos.ToString();

            if (horastext.Length == 1)
            {
                horastext = "0" + horastext;
            }
            if (minutostext.Length == 1)
            {
                minutostext = "0" + minutostext;
            }
            return horastext + ":" + minutostext;
        }
       
    }
}