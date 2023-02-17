using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceDesk.ViewModels;
using ServiceDesk.Models;

namespace ServiceDesk.Managers
{
    //==================================================================================================================
    public class TecnicoManager
    {
        private readonly ServiceDeskContext _sd = new ServiceDeskContext();
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<SlaTimesVm> GetSlaTimes(List<his_Ticket> historic) {

            var list = new List<SlaTimesVm>();
            var listactual = new List<SlaTimesVm>();
            var HrsJornada = 8;
            var inicioJrn = new TimeSpan(14,00,00);
            var finJrn = new TimeSpan(22, 00, 00);

            //ORDENAMIENTO POR ID HISTORICO
            historic = historic.OrderBy(a => a.IdHis).ToList();
            


            //SLA TOTAL Y SLA OBJETIVO
            if (historic.Count > 0) {
                var fechainicio = new DateTime();
                var fecharesuelto = new DateTime();


                var abierto = historic.FirstOrDefault(a => a.EstatusTicket == 1);
                var subcategoria = _sd.cat_SubCategoria.Find(abierto.SubCategoria);
                var slaobjetivo = int.Parse(subcategoria.SLA);

                var objetivo = new SlaTimesVm
                {
                    Type = "SLA Objetivo",
                    Order = 1,
                    Time = GetHoursFormat(new TimerVm { Horas = slaobjetivo, Minutos = 0 }),
                    Enable = true, 
                    Tecnico = "-"
                };
                list.Add(objetivo);

                if (abierto!=null) {
                    fechainicio = abierto.FechaRegistro;
                      
                    var resuelto = historic.FirstOrDefault(a => a.EstatusTicket == 4);
                    if (resuelto != null)
                    {
                        fecharesuelto = resuelto.FechaRegistro;
                    }
                    else {
                        fecharesuelto = DateTime.Now;
                    }
                    var days = (fecharesuelto - fechainicio).Days;
                      
                    var totaltime = GetHoursDifference(fechainicio, fecharesuelto, inicioJrn, finJrn,true);
                    var colortotal = GetColor(totaltime.Horas, slaobjetivo);

                    var total = new SlaTimesVm {
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
                    foreach (var his in historic) {

                        if (enespera==true) {
                            //SI EL REGISTRO ANTERIOR FUE EN ESPERA SE OBTIENEN TIEMPOS DE ESPERA
                            var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn,false);
                            horasesp = horasesp + difdias.Horas;
                            minutosesp = minutosesp + difdias.Minutos;

                            tecnico = his.TecnicoAsignado;
                            fecharegistro = his.FechaRegistro;
                            estatusprev = his.EstatusTicket;
                            enespera = false;
                            continue;
                        }
                        if (his.EstatusTicket == 1) {
                            //DO NOTHING
                        }
                        //OBTENER REGISTRO PRIMERA ASIGNACION
                        else if (his.EstatusTicket == 2)
                        {
                            //ESTATUS PRIMERA ASIGNACION O RE ASIGNACION HACIA OTRO TECNICO
                            if (!string.IsNullOrEmpty(tecnico) && his.TecnicoAsignado != tecnico)
                            {
                                //SE GENERA SLA ACTUAL DEL TCNICO ANTERIOR Y SE AGREGAN DATOS TEMPROALES DEL NUEVO
                                var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn,false);
                                horas = horas + difdias.Horas;
                                minutos = minutos + difdias.Minutos;

                                //COMO EL TICKET FUE REASIGNADO SE ENVIAN SLA ACTUAL ANTERIOR TECNICO
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
                            fecharegistro = his.FechaRegistro;
                            estatusprev = his.EstatusTicket;
                            enespera = false;


                        }
                        else if (his.EstatusTicket == 3)
                        {
                            //SE ENCUENTRA EN TRABAJANDO 
                            var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn,false);
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
                            var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn,false);
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
                            var difdias = GetHoursDifference(fecharegistro, his.FechaRegistro, inicioJrn, finJrn,false);
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
                        var difdias = GetHoursDifference(fecharegistro,DateTime.Now, inicioJrn, finJrn, false);
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

                    var ultimotec = listactual.LastOrDefault();
                    list.Add(ultimotec);

                    //VALIDA SI HUBO TIEMPO EN ESPERA DEL TICKET
                    if (horasesp != 0 || minutosesp != 0)
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

                    //TIEMPOS DE GARANTIA 
                    var garantia = historic.FirstOrDefault(a => a.EstatusTicket == 5);
                    if (garantia != null) {
                        var cerrado = historic.FirstOrDefault(a => a.EstatusTicket == 6);
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
                        else {
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
             
            return list.OrderBy(a=>a.Order).ToList();
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public TimerVm GetHoursDifference(DateTime start, DateTime end, TimeSpan inijrn, TimeSpan finjrn,bool apertura) {

            //OBTENCION DE HORAS JORNADA EN BASE A SU INICIO Y FIN DE TURNO


            int hrjs = (finjrn - inijrn).Hours;

            var festivos = new List<DateTime> {
                new DateTime(2022,03,27)
            };

            var horas = 0;
            var minutos = 0;
            for (DateTime counter = start; counter <= end; counter = counter.AddDays(1))
            {
                if (counter.Date == end.Date && start.TimeOfDay > inijrn && apertura == true)
                {
                    //TICKET ASIGNADO DESPUES DEL INICIO DE JORNADA EN APERTURA
                    var time = (finjrn - start.TimeOfDay);
                    horas = time.Hours;
                    minutos = time.Minutes;
                }
                else if (counter.Date == end.Date && start.TimeOfDay < inijrn && apertura == false)
                {
                    //TICKET ASIGNADO DESPUES DEL INICIO DE JORNADA E
                    var time = (end.TimeOfDay - inijrn);
                    horas = time.Hours;
                    minutos = time.Minutes;
                }
                else if (counter.Date == end.Date && start.TimeOfDay > inijrn && apertura == false)
                {
                    //TICKET ASIGNADO DESPUES DEL INICIO DE JORNADA E
                    var time = (end.TimeOfDay - start.TimeOfDay);
                    horas = time.Hours;
                    minutos = time.Minutes;
                }
                else
                {
                    horas = horas + hrjs;
                }
            }

            return new TimerVm { Horas=horas, Minutos = minutos };
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public TimerVm GetHoursDifference24Hrs(DateTime start, DateTime end) {

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
                else {
                    horas = horas + hrjs;
                }
            }

            return new TimerVm { Horas=horas, Minutos = minutos };
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public string GetColor(int currentsla, int slaobjetivo) {

            if (currentsla ==0 && slaobjetivo ==0) {
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
            else {               
                if (porc > 51 && porc < 70)
                {
                    //AMARILLO
                    return "#ffc009";
                }
                else if (porc >= 71) {
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
        public string GetHoursFormat(TimerVm timer) {
            var horastext = timer.Horas.ToString();
            var minutostext = timer.Minutos.ToString();

            if (horastext.Length==1) {
                horastext = "0" + horastext;
            }
            if (minutostext.Length==1) {
                minutostext = "0" + minutostext;
            }
            return horastext + ":" + minutostext;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    }
    //==================================================================================================================
}