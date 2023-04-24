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
        public List<SlaTimesVm> GetSlaTimes(List<his_Ticket> historic, int Quitar_Este_Int_Para_Que_Funcione)
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

                //var abierto = historic.Where(x=> x.EstatusTicket==1 ).OrderBy(x=> x.IdHis).FirstOrDefault();
                var abierto = historic.Where(x => x.EstatusTicket == 1).OrderByDescending(x => x.IdHis).FirstOrDefault();
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

                // SLA: SLA OBJETIVO
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
                        if (reapertura == false) { fecharesuelto = resuelto.FechaRegistro; }
                        else { fecharesuelto = DateTime.Now; }
                    }
                    else
                    {
                        fecharesuelto = DateTime.Now;
                    }
                    var days = (fecharesuelto - fechainicio).Days;

                    var totaltime = GetHoursDifference(fechainicio, fecharesuelto, inicioJrn, finJrn, true);
                    var colortotal = GetColor(totaltime.Horas, slaobjetivo);

                    // SLA: SLA TOTAL
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
                        if (his.TecnicoAsignado != null && his.TecnicoAsignadoReag == null && his.TecnicoAsignadoReag2 == null)
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
                                // If (ticket = Cerrado) :
                                // difdias = horas laborales del tenico contando desde la apertura del ticket hasta su cierre
                                difdias = GetHoursDifference(fecharegistro, Cerrado.FechaRegistro, inicioJrn, finJrn, false);
                            }
                            else
                            {
                                // If (ticket != Cerrado) :
                                // difdias = horas laborales del tenico contando desde la apertura del ticket hasta NOW
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

                    // SLA: EN ESPERA
                    if ((horasesp != 0 || minutosesp != 0) || enespera)
                    {
                        if (enespera)
                        {
                            //EL TICKET ESTA EN ESPERA;
                            //  Obtener tiempo desde que fue puesto en espera hasta NOW,
                            //  y sumarlo al tiempo que había estado en espera anteriormente
                            var difdias = GetHoursDifference(fecharegistro, DateTime.Now, inicioJrn, finJrn, false);
                            horasesp = horasesp + difdias.Horas;
                            minutosesp = minutosesp + difdias.Minutos;

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
                            //EL TICKET ESTA RESUELTO:
                            //  Obtener el tiempo total que estuvo en espera
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

                    // SLA: EN GARANTÍA
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
        public List<SlaTimesVm> GetSlaTimes(List<his_Ticket> historico, int Quitar_Este_Int_Para_Que_Funcione, int Tambien_Este_otro)
        {
            historico = historico.OrderBy(t => t.IdHis).ToList();
            List<SlaTimesVm> slaTimesVms = new List<SlaTimesVm>();
            var Inicio_Jrnd = new TimeSpan(9, 00, 00);
            var Final_Jrnd  = new TimeSpan(17, 00, 00);
            var DataTecnico = new tbl_User();
            string[] DiasLaborales = new string[0];
            var LastEntry   = historico.OrderByDescending(t => t.IdHis).FirstOrDefault();
            var FirstEntry  = historico.Where(t => t.Historial = true).FirstOrDefault();
            var Asignado    = historico.OrderByDescending(t => t.FechaRegistro).Where(t => t.EstatusTicket == 2).FirstOrDefault(); // Fecha de última asignación
            var Cerrado     = historico.Where(t => t.EstatusTicket == 6).FirstOrDefault(); // fecha de cierre
            var Cancelado   = historico.Where(t => t.EstatusTicket == 8).FirstOrDefault(); // fecha de cancelación
            var Garantia    = historico.Where(t => t.EstatusTicket == 5).FirstOrDefault(); // fecha que empezó a estar en garantía            
            // vars usados para parsear datos
            int SLA_Objetivo_int = 0, hrs = 0, min = 0;
            string[] substrings;
            var timer_En_Espera_Tecnico_Actual = new TimerVm();

            // Obtener Eventos de creación y Ahora, si está cerrado o cancelado remplazar ahora con la fecha de ese evento
            DateTime FechaCreacion = FirstEntry.FechaRegistro;   // Fecha de cración
            DateTime Cierre_Cancelacion_Now = DateTime.Now;               // Fecha de cierre, cancelación u hoy
            if (Cerrado != null) Cierre_Cancelacion_Now = Cerrado.FechaRegistro;
            if (Cancelado != null) Cierre_Cancelacion_Now = Cancelado.FechaRegistro;

            // Obterner datos del último tecnico asignado que tuvo el ticket DataTenico, Inicio_Jrnd, Final_Jrnd, DíasLaborales[]
            if (true)
            {
                if (LastEntry.TecnicoAsignado != null && LastEntry.TecnicoAsignadoReag == null && LastEntry.TecnicoAsignadoReag2 == null)
                {
                    DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignado).FirstOrDefault();
                }
                else if (LastEntry.TecnicoAsignado != null && LastEntry.TecnicoAsignadoReag != null && LastEntry.TecnicoAsignadoReag2 == null)
                {
                    DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignadoReag).FirstOrDefault();
                }
                else if (LastEntry.TecnicoAsignado != null && LastEntry.TecnicoAsignadoReag != null && LastEntry.TecnicoAsignadoReag2 != null)
                {
                    DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignadoReag2).FirstOrDefault();
                }

                if (DataTecnico != null)
                {
                    if (DataTecnico.NombreTecnico != null)
                    {
                        var Ini = Convert.ToDateTime(DataTecnico.HoraInicioATC);
                        Inicio_Jrnd = new TimeSpan(Ini.Hour, Ini.Minute, 00);
                        var Fin = Convert.ToDateTime(DataTecnico.HoraFinATC);
                        Final_Jrnd = new TimeSpan(Fin.Hour, Fin.Minute, 00);

                        var data_Dias = _sd.tbl_VentanaAtencion.Where(t => t.EmpleadoID == DataTecnico.EmpleadoID).FirstOrDefault();
                        
                        //if (data_Dias.Lunes)    { DiasLaborales = DiasLaborales.Concat(new string[] { "lunes" }).ToArray(); }
                        //if (data_Dias.Martes)   { DiasLaborales = DiasLaborales.Concat(new string[] { "martes" }).ToArray(); }
                        //if (data_Dias.Miercoles){ DiasLaborales = DiasLaborales.Concat(new string[] { "miércoles" }).ToArray(); }
                        //if (data_Dias.Jueves)   { DiasLaborales = DiasLaborales.Concat(new string[] { "jueves" }).ToArray(); }
                        //if (data_Dias.Viernes)  { DiasLaborales = DiasLaborales.Concat(new string[] { "viernes" }).ToArray(); }
                        //if (data_Dias.Sabado)   { DiasLaborales = DiasLaborales.Concat(new string[] { "sábado" }).ToArray(); }
                        //if (data_Dias.Domingo)  { DiasLaborales = DiasLaborales.Concat(new string[] { "domingo" }).ToArray(); }

                        if (data_Dias != null) { 
                            if (data_Dias.Lunes)    { DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray(); }
                            if (data_Dias.Martes)   { DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray(); }
                            if (data_Dias.Miercoles){ DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray(); }
                            if (data_Dias.Jueves)   { DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray(); }
                            if (data_Dias.Viernes)  { DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray(); }
                            if (data_Dias.Sabado)   { DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray(); }
                            if (data_Dias.Domingo)  { DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray(); }
                        }
                        else
                        {
                            DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray();
                            DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray();
                            DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray();
                            DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray();
                            DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray();
                            DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray();
                            DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray();
                        }
                    }
                    else
                    {
                        Inicio_Jrnd = new TimeSpan(9, 00, 00);
                        Final_Jrnd = new TimeSpan(17, 00, 00);
                        DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray();
                        DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray();
                        DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray();
                        DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray();
                        DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray();
                        DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray();
                        DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray();
                    }
                }
                else
                {
                    Inicio_Jrnd = new TimeSpan(9, 00, 00);
                    Final_Jrnd = new TimeSpan(17, 00, 00);
                    DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray();
                    DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray();
                    DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray();
                    DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray();
                    DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray();
                    DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray();
                    DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray();
                }
            }

            // Obtener SLA Objetivo
            if (true)
            {
                // Tiempo que le debería tomar al Grupo Resolutor resolver la incidencia
                var Subcategoria = _sd.cat_SubCategoria.Where(t => t.Id == LastEntry.SubCategoria).FirstOrDefault();
                if (Subcategoria != null) SLA_Objetivo_int = int.Parse(Subcategoria.SLA);
                else SLA_Objetivo_int = 99;
                var SLA_Objetivo = new SlaTimesVm
                {
                    Type = "SLA Objetivo",
                    Order = 1,
                    Enable = true,
                    Tecnico = "~",
                    Time = GetHoursFormat(new TimerVm { Horas = SLA_Objetivo_int, Minutos = 0 })
                };
                slaTimesVms.Add(SLA_Objetivo);
            }

            // Obtener SLA Total
            if (true)
            {
                // Tiempo total desde que fue creado el ticket hasta Cierre/Cancelación/Now (Contador 24hrs)
                var string_SLAtotal = ContarTiempoLaboralEntreEventos(FechaCreacion, Cierre_Cancelacion_Now, Inicio_Jrnd, Final_Jrnd, DiasLaborales, 1); // Contador Horario Laboral
                //var string_SLAtotal = GetHoursDifference24Hrs2(FechaCreacion, Cierre_Cancelacion_Now); // Conteo 24 hrs
                substrings = string_SLAtotal.Split(':');
                hrs = Int32.Parse(substrings[0]);
                min = Int32.Parse(substrings[1]);

                var SLA_Total = new SlaTimesVm
                {
                    Type = "SLA total",
                    Order = 3,
                    Time = GetHoursFormat(new TimerVm { Horas = hrs, Minutos = min }),
                    Enable = true,
                    Color = GetColor(hrs, SLA_Objetivo_int),
                    Tecnico = "-"
                };
                slaTimesVms.Add(SLA_Total);
            }

            // Obtener SLA En Espera (Desde creación)
            if (Asignado != null)
            {
                // Tiempo total que el ticket ha pasado "En Espera" desde su creación (contador 24hrs)
                DateTime InicioEspera = DateTime.Now;
                DateTime FinalEspera = DateTime.Now;
                bool EnEspera = false;
                bool Inicio_y_Final_Espera_Obtenidos = false;
                string tiempo_en_espera;    // usado para parsear datos
                hrs = 0;                    // usado para parsear datos
                min = 0;                    // contador de minutos en espera

                foreach (var evento in historico)
                {
                    // Obtener inicio de En Espera
                    if (evento.EstatusTicket == 7 && !EnEspera)
                    {
                        EnEspera = true;
                        InicioEspera = evento.FechaRegistro;
                    }

                    // Obtener final de En Espera
                    if ((evento.EstatusTicket == 3 || evento.EstatusTicket == 8) && EnEspera)
                    {
                        EnEspera = false;
                        FinalEspera = evento.FechaRegistro;
                        Inicio_y_Final_Espera_Obtenidos = true;
                    }

                    // Calcular y sumar a contador (min)
                    if (Inicio_y_Final_Espera_Obtenidos)
                    {
                        Inicio_y_Final_Espera_Obtenidos = false;
                        //tiempo_en_espera = GetHoursDifference24Hrs2(InicioEspera, FinalEspera);
                        tiempo_en_espera = ContarTiempoLaboralEntreEventos(InicioEspera, FinalEspera, Inicio_Jrnd, Final_Jrnd, DiasLaborales, 21);
                        substrings = tiempo_en_espera.Split(':');
                        hrs = Int32.Parse(substrings[0]);
                        min += Int32.Parse(substrings[1]) + (hrs * 60);
                    }
                    // fin de foreach
                }
                if (EnEspera) // Si EnEspera es true significa que obtuvo un inicio en espera pero no su final, asi que el ticket sigue en espera        //(EnEspera && min == 0)
                { // Si todavía está en espera
                    tiempo_en_espera = ContarTiempoLaboralEntreEventos(InicioEspera, DateTime.Now, Inicio_Jrnd, Final_Jrnd, DiasLaborales, 22); // Contador Horario Laboral
                    //tiempo_en_espera = GetHoursDifference24Hrs2(InicioEspera, DateTime.Now); Contador 24 Hrs
                    substrings = tiempo_en_espera.Split(':');
                    hrs = Int32.Parse(substrings[0]);
                    min += Int32.Parse(substrings[1]) + (hrs * 60);
                }

                hrs = min / 60;
                min = min % 60;

                if (hrs != 0 || min != 0) { 
                    var timer = new TimerVm { Horas = hrs, Minutos = min };
                    var timeesp = new SlaTimesVm
                    {
                        Type = "En espera",
                        Order = 3,
                        Time = GetHoursFormat(timer),
                        Enable = true,
                        Color = "#D7F8EE",
                        Tecnico = "-"
                    };
                    slaTimesVms.Add(timeesp);
                }
            }

            // Obtener Tiempo En Espera (Por Tecnico Actual)
            if (Asignado != null)
            {  
                DateTime InicioEspera = DateTime.Now;
                DateTime FinalEspera = DateTime.Now;
                bool EnEspera = false;
                bool Inicio_y_Final_Espera_Obtenidos = false;
                string tiempo_en_espera; // usado para parsear datos
                hrs = 0; // usado para parsear datos
                min = 0; // contador de minutos en espera

                var historico_Tecnico_Actual = new List<his_Ticket>();
              
                historico_Tecnico_Actual = historico.Where(t => t.FechaRegistro > Asignado.FechaRegistro).ToList();

                foreach (var evento in historico_Tecnico_Actual)
                {
                    // Obtener inicio de En Espera
                    if (evento.EstatusTicket == 7 && !EnEspera)
                    {
                        EnEspera = true;
                        InicioEspera = evento.FechaRegistro;
                    }
                    // Obtener final de En Espera
                    if (evento.EstatusTicket != 7 && EnEspera)
                    {
                        // Este if busca el sig registro historico que no sea "En Espera"
                        //   debido a que es posible pasar de "En Espera" a "En Espera"
                        //   desconozco por que esto sucede 
                        //   ver historico del ticket 4967 para más detalles
                        EnEspera = false;
                        FinalEspera = evento.FechaRegistro;
                        Inicio_y_Final_Espera_Obtenidos = true;
                    }
                    if (Inicio_y_Final_Espera_Obtenidos)
                    {
                        Inicio_y_Final_Espera_Obtenidos = false;
                        tiempo_en_espera = ContarTiempoLaboralEntreEventos(InicioEspera, FinalEspera, Inicio_Jrnd, Final_Jrnd, DiasLaborales, 31);
                        //tiempo_en_espera = GetHoursDifference24Hrs2(InicioEspera, FinalEspera);
                        substrings = tiempo_en_espera.Split(':');
                        hrs = Int32.Parse(substrings[0]);
                        min += Int32.Parse(substrings[1]) + (hrs * 60);
                    }
                    // fin de foreach
                }

                // RECENTLY ADDED CHECK:---------------------- START
                if (EnEspera) // Si EnEspera es true significa que obtuvo un inicio en espera pero no su final, asi que el ticket sigue en espera        //(EnEspera && min == 0)
                { // Si todavía está en espera
                    tiempo_en_espera = ContarTiempoLaboralEntreEventos(InicioEspera, DateTime.Now, Inicio_Jrnd, Final_Jrnd, DiasLaborales, 32); // Contador Horario Laboral
                                                                                                                                                //tiempo_en_espera = GetHoursDifference24Hrs2(InicioEspera, DateTime.Now); Contador 24 Hrs
                    substrings = tiempo_en_espera.Split(':');
                    hrs = Int32.Parse(substrings[0]);
                    min += Int32.Parse(substrings[1]) + (hrs * 60);
                }
                // RECENTLY ADDED CHECK:---------------------- END

                hrs = min / 60;
                min = min % 60;

                timer_En_Espera_Tecnico_Actual = new TimerVm { Horas = hrs, Minutos = min };          
            }

            // Obtener SLA Tiempo Actual
            if (Asignado != null)
            {
                // Tiempo Actual = Tiempo laboral desde que fue asignado al último tecnico hasta Cierre/Cancelación/Now
                string string_SLA_Actual = ContarTiempoLaboralEntreEventos(Asignado.FechaRegistro, Cierre_Cancelacion_Now, Inicio_Jrnd, Final_Jrnd, DiasLaborales, 4);
                substrings = string_SLA_Actual.Split(':');
                hrs = Int32.Parse(substrings[0]);
                min = Int32.Parse(substrings[1]);

                // Antes de imprimir, restar el tiempo que el tecnico actual ha tenido en espera el ticket ----- START
                // Tiempo Actual en minutos
                min += (hrs * 60);
                // Tiempo en Espera en minutos
                var minutos_En_Espera = (timer_En_Espera_Tecnico_Actual.Minutos) + (timer_En_Espera_Tecnico_Actual.Horas * 60);
                min = min - minutos_En_Espera;
                hrs = min / 60;
                min = min % 60;
                // Antes de imprimir, restar el tiempo que el tecnico actual ha tenido en espera el ticket ----- END

                var timer = new TimerVm { Horas = hrs, Minutos = min };
                var actual = new SlaTimesVm
                {
                    Type = "Tiempo Actual",
                    Order = 2,
                    Time = GetHoursFormat(timer),
                    Enable = true,
                    Color = GetColor(timer.Horas, SLA_Objetivo_int),
                    Tecnico = LastEntry.TecnicoAsignado
                };
                slaTimesVms.Add(actual);
            }

            // Obtener En Garantía
            if (Garantia != null)
            {
                DateTime end = DateTime.Now;
                if (Cerrado != null) end = Cerrado.FechaRegistro; 
                var difdias = GetHoursDifference24Hrs(Garantia.FechaRegistro, end); 
                var timer = new TimerVm { Horas = difdias.Horas, Minutos = difdias.Minutos };
                var GarantiaSLA = new SlaTimesVm
                {
                    Type = "En Garantía",
                    Order = 4,
                    Time = GetHoursFormat(timer),
                    Enable = true,
                    Color = "#DAF7A6",
                    Tecnico = "~"
                };
                slaTimesVms.Add(GarantiaSLA);
            }

            return slaTimesVms.OrderBy(t => t.Order).ToList();
        } // Method 1 Iv
        public List<SlaTimesVm> GetSlaTimes2(List<his_Ticket> historico) // Method 2 Iv 
        {
            historico = historico.OrderBy(t => t.IdHis).ToList();
            UserTimeData JornadaDelTecnico = new UserTimeData();
            List<SlaTimesVm> slaTimesVms = new List<SlaTimesVm>();
            string[] DiasLaborales = new string[0];
            TimeSpan Inicio_Jrnd = new TimeSpan(9, 00, 00);
            TimeSpan Final_Jrnd  = new TimeSpan(17, 00, 00);
            tbl_User DataTecnico = new tbl_User();
            his_Ticket LastEntry   = historico.OrderByDescending(t => t.IdHis).FirstOrDefault();
            his_Ticket FirstEntry  = historico.Where(t => t.Historial = true).FirstOrDefault();
            his_Ticket Asignado    = historico.OrderByDescending(t => t.FechaRegistro).Where(t => t.EstatusTicket == 2).FirstOrDefault(); // Fecha de última asignación
            his_Ticket Cerrado     = historico.Where(t => t.EstatusTicket == 6).FirstOrDefault(); // fecha de cierre
            his_Ticket Cancelado   = historico.Where(t => t.EstatusTicket == 8).FirstOrDefault(); // fecha de cancelación
            his_Ticket Garantia    = historico.Where(t => t.EstatusTicket == 5).FirstOrDefault(); // fecha que empezó a estar en garantía// 
            var MatrizCat          = _sd.cat_MatrizCategoria.FirstOrDefault(t => t.IDSubCategoria == LastEntry.SubCategoria);
            int SLA_Objetivo_int = 0, hrs = 0, min = 0, min_EnEspera_Tecnico_Actual = 0; // vars usados para parsear datos

            // Obtener Eventos de creación y Ahora, si está cerrado o cancelado remplazar ahora con la fecha de ese evento
            DateTime                Fecha_de_Creacion_de_ticket = FirstEntry.FechaRegistro; // Fecha de cración
            DateTime                Fecha_de_CierreCancelacionHoy = DateTime.Now;    // Fecha de cierre, cancelación u hoy
            if (Cerrado != null)    Fecha_de_CierreCancelacionHoy = Cerrado.FechaRegistro; else
            if (Cancelado != null)  Fecha_de_CierreCancelacionHoy = Cancelado.FechaRegistro;

            // Obterner datos del último tecnico asignado que tuvo el ticket DataTenico, Inicio_Jrnd, Final_Jrnd, DíasLaborales[]
            if (true)
            {
                // IF no data, then use dummy info, normal weekday, 9am to 5pm
                DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray();
                Inicio_Jrnd = new TimeSpan(9, 00, 00);
                Final_Jrnd = new TimeSpan(17, 00, 00);

                if (LastEntry.TecnicoAsignado       != null && 
                    LastEntry.TecnicoAsignadoReag   == null && 
                    LastEntry.TecnicoAsignadoReag2  == null) {
                    DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignado).FirstOrDefault();
                }
                else if (LastEntry.TecnicoAsignado      != null && 
                        LastEntry.TecnicoAsignadoReag   != null && 
                        LastEntry.TecnicoAsignadoReag2  == null) {
                    DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignadoReag).FirstOrDefault();
                }
                else if (LastEntry.TecnicoAsignado      != null && 
                        LastEntry.TecnicoAsignadoReag   != null && 
                        LastEntry.TecnicoAsignadoReag2  != null) {
                    DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignadoReag2).FirstOrDefault();
                }

                if (DataTecnico != null && DataTecnico.NombreTecnico != null) 
                {
                    //empty the info dummy and replace with actual correct data
                    var Ini = Convert.ToDateTime(DataTecnico.HoraInicioATC);
                    var Fin = Convert.ToDateTime(DataTecnico.HoraFinATC);

                    Inicio_Jrnd = new TimeSpan(Ini.Hour, Ini.Minute, 00);
                    Final_Jrnd = new TimeSpan(Fin.Hour, Fin.Minute, 00);

                    var data_Dias = _sd.tbl_VentanaAtencion.Where(t => t.EmpleadoID == DataTecnico.EmpleadoID).FirstOrDefault();

                    if (data_Dias != null)
                    {
                        DiasLaborales = new string[0];
                        if (data_Dias.Lunes) { DiasLaborales        = DiasLaborales.Concat(new string[] { "Monday" }).ToArray(); }
                        if (data_Dias.Martes) { DiasLaborales       = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray(); }
                        if (data_Dias.Miercoles) { DiasLaborales    = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray(); }
                        if (data_Dias.Jueves) { DiasLaborales       = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray(); }
                        if (data_Dias.Viernes) { DiasLaborales      = DiasLaborales.Concat(new string[] { "Friday" }).ToArray(); }
                        if (data_Dias.Sabado) { DiasLaborales       = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray(); }
                        if (data_Dias.Domingo) { DiasLaborales      = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray(); }
                    }
                }

                JornadaDelTecnico.Inicio_Jrnd = Inicio_Jrnd;
                JornadaDelTecnico.Final_Jrnd = Final_Jrnd;
                JornadaDelTecnico.DiasLaborales = DiasLaborales;
                JornadaDelTecnico.debug = 0;
            }

            // Obtener SLA Objetivo
            if (true)
            {
                // Tiempo que le debería tomar al Grupo Resolutor resolver la incidencia
                //var Subcategoria = _sd.cat_SubCategoria.Where(t => t.Id == LastEntry.SubCategoria).FirstOrDefault();
                //if (Subcategoria != null) SLA_Objetivo_int = int.Parse(Subcategoria.SLA);
                //else SLA_Objetivo_int = 99;
                SLA_Objetivo_int = (MatrizCat != null) ? Int32.Parse(MatrizCat.SLAObjetivo) : 99;
                var SLA_Objetivo = new SlaTimesVm
                {
                    Type = "SLA Objetivo",
                    Order = 1,
                    Enable = true,
                    Tecnico = "~",
                    Time = SLA_Objetivo_int + ":00"
                };
                slaTimesVms.Add(SLA_Objetivo);
            }

            // Obtener SLA Total
            if (true)
            {
                // Tiempo total desde que fue creado el ticket hasta Cierre/Cancelación/Now (Contador 24hrs)
                min = Min_Laborales_Entre_Eventos(Fecha_de_Creacion_de_ticket, Fecha_de_CierreCancelacionHoy, JornadaDelTecnico); // Contador Horario Laboral //1
                hrs = (min != 0) ? min / 60 : 0;
                var SLA_Total = new SlaTimesVm
                {
                    Type = "SLA total",
                    Order = 3,
                    Time = String_Reloj_FromMinutos(min),
                    Enable = true,
                    Color = GetColor(hrs, SLA_Objetivo_int),
                    Tecnico = "-"
                };
                slaTimesVms.Add(SLA_Total);
            }

            // Obtener SLA En Espera (Desde creación)
            if (Asignado != null)
            {
                // Tiempo total que el ticket ha pasado "En Espera" desde su creación (contador 24hrs)
                DateTime InicioEspera = DateTime.Now, FinalEspera = DateTime.Now;
                bool EnEspera = false, Inicio_y_Final_Espera_Obtenidos = false;
                min = 0; // contador de minutos en espera

                foreach (var evento in historico)
                {
                    // Obtener inicio de En Espera
                    if (evento.EstatusTicket == 7 && !EnEspera)
                    {
                        EnEspera = true;
                        InicioEspera = evento.FechaRegistro;
                    }

                    // Obtener final de En Espera
                    if ((evento.EstatusTicket == 3 || evento.EstatusTicket == 8) && EnEspera)
                    {
                        EnEspera = false;
                        FinalEspera = evento.FechaRegistro;
                        Inicio_y_Final_Espera_Obtenidos = true;
                    }

                    // Calcular y sumar a contador (min)
                    if (Inicio_y_Final_Espera_Obtenidos)
                    {
                        Inicio_y_Final_Espera_Obtenidos = false;
                        min += Min_Laborales_Entre_Eventos(InicioEspera, FinalEspera, JornadaDelTecnico); //21
                    }
                    // fin de foreach
                }
                // Sino se cerró la última espera
                if (EnEspera) {  min += Min_Laborales_Entre_Eventos(InicioEspera, DateTime.Now, JornadaDelTecnico); } //22

                if (min != 0)
                {
                    var timeesp = new SlaTimesVm
                    {
                        Type = "En espera",
                        Order = 3,
                        Time = String_Reloj_FromMinutos(min),
                        Enable = true,
                        Color = "#D7F8EE",
                        Tecnico = "-"
                    };
                    slaTimesVms.Add(timeesp);
                }
            }

            // Obtener Tiempo En Espera (Por Tecnico Actual)
            if (Asignado != null)
            {
                DateTime InicioEspera = DateTime.Now, FinalEspera = DateTime.Now;
                bool EnEspera = false, Inicio_y_Final_Espera_Obtenidos = false;
                min = 0; // contador de minutos en espera

                var historico_Tecnico_Actual = new List<his_Ticket>();

                historico_Tecnico_Actual = historico.Where(t => t.FechaRegistro > Asignado.FechaRegistro).ToList();

                foreach (var evento in historico_Tecnico_Actual)
                {
                    // Obtener inicio de En Espera
                    if (evento.EstatusTicket == 7 && !EnEspera)
                    {
                        EnEspera = true;
                        InicioEspera = evento.FechaRegistro;
                    }
                    // Obtener final de En Espera
                    if (evento.EstatusTicket != 7 && EnEspera)
                    {
                        EnEspera = false;
                        FinalEspera = evento.FechaRegistro;
                        Inicio_y_Final_Espera_Obtenidos = true;
                    }
                    if (Inicio_y_Final_Espera_Obtenidos)
                    {
                        Inicio_y_Final_Espera_Obtenidos = false;
                        min += Min_Laborales_Entre_Eventos(InicioEspera, FinalEspera, JornadaDelTecnico); //31
                    }
                }

                // Si todavía está en espera
                if (EnEspera)  {  min += Min_Laborales_Entre_Eventos(InicioEspera, DateTime.Now, JornadaDelTecnico); } //32

                min_EnEspera_Tecnico_Actual = min;
            }

            // Obtener SLA Tiempo Actual
            if (Asignado != null)
            {
                // Tiempo Actual = Tiempo laboral desde que fue asignado al último tecnico hasta Cierre/Cancelación/Now
                min = Min_Laborales_Entre_Eventos(Asignado.FechaRegistro, Fecha_de_CierreCancelacionHoy, JornadaDelTecnico); //4
                min = min - min_EnEspera_Tecnico_Actual;
                hrs = (min != 0) ? min / 60 : 0;
                var actual = new SlaTimesVm
                {
                    Type = "Tiempo Actual",
                    Order = 2,
                    Time = String_Reloj_FromMinutos(min),
                    Enable = true,
                    Color = GetColor(hrs, SLA_Objetivo_int),
                    Tecnico = LastEntry.TecnicoAsignado
                };
                slaTimesVms.Add(actual);
            }

            // Obtener En Garantía
            if (Garantia != null)
            {
                //Obtener el tiempo que el ticket ha estado en garantía
                DateTime end = DateTime.Now;
                if (Cerrado != null) end = Cerrado.FechaRegistro;
                var difdias = GetHoursDifference24Hrs(Garantia.FechaRegistro, end);
                var timer = new TimerVm { Horas = difdias.Horas, Minutos = difdias.Minutos };

                //Obtener tiempo que debe estar en garantía el ticket
                var Garantia_Definida_Para_Subcat = (MatrizCat != null) ? Int32.Parse(MatrizCat.Garantia) : 8;
                TimeSpan Garantia_Definida = new TimeSpan(Garantia_Definida_Para_Subcat, 0, 0);
                TimeSpan Tiempo_En_Garantia = new TimeSpan(difdias.Horas, difdias.Minutos, 0);
                TimeSpan Time_to_show = new TimeSpan(0,0,0);
                if (Tiempo_En_Garantia > Garantia_Definida)
                {
                    timer.Horas = 0; 
                    timer.Minutos = 0;
                }
                else {
                    Time_to_show = Garantia_Definida - Tiempo_En_Garantia;
                    timer.Horas     = (int)Time_to_show.TotalHours;
                    timer.Minutos   = (int)Time_to_show.Minutes;
                }

                var GarantiaSLA = new SlaTimesVm
                {
                    Type = "En Garantía",
                    Order = 4,
                    Time = GetHoursFormat(timer),
                    Enable = true,
                    Color = "#DAF7A6",
                    Tecnico = "~"
                };
                slaTimesVms.Add(GarantiaSLA);
            }

            return slaTimesVms.OrderBy(t => t.Order).ToList();
        } 
        public List<SlaTimesVm> GetSlaTimes(List<his_Ticket> historico) // Method 3 Iv ----- IN USE en caso de modificar este también modificar inTime que funciona de manera casi identica 
        {
            historico = historico.OrderBy(t => t.IdHis).ToList();
            UserTimeData JornadaDelTecnico = new UserTimeData();
            List<SlaTimesVm> slaTimesVms = new List<SlaTimesVm>();
            string[] DiasLaborales = new string[0];
            TimeSpan Inicio_Jrnd = new TimeSpan(9, 00, 00);
            TimeSpan Final_Jrnd = new TimeSpan(17, 00, 00);
            tbl_User DataTecnico = new tbl_User();
            his_Ticket LastEntry = historico.OrderByDescending(t => t.IdHis).FirstOrDefault();
            his_Ticket FirstEntry = historico.Where(t => t.Historial = true).FirstOrDefault();
            his_Ticket Asignado = historico.OrderByDescending(t => t.FechaRegistro).Where(t => t.EstatusTicket == 2).FirstOrDefault(); // Fecha de última asignación
            his_Ticket Cerrado = historico.Where(t => t.EstatusTicket == 6).FirstOrDefault(); // fecha de cierre
            his_Ticket Cancelado = historico.Where(t => t.EstatusTicket == 8).FirstOrDefault(); // fecha de cancelación
            his_Ticket Garantia = historico.Where(t => t.EstatusTicket == 5).FirstOrDefault(); // fecha que empezó a estar en garantía// 
            var MatrizCat = _sd.cat_MatrizCategoria.FirstOrDefault(t => t.IDSubCategoria == LastEntry.SubCategoria);
            int SLA_Objetivo_int = 0, hrs = 0, min = 0, min_EnEspera_Tecnico_Actual = 0; // vars usados para parsear datos
            System.Diagnostics.Debug.WriteLine("SLA: " + FirstEntry.IdTicket);

            // Obtener Eventos de creación y Ahora, si está cerrado o cancelado remplazar ahora con la fecha de ese evento
            DateTime Fecha_de_Creacion_de_ticket = FirstEntry.FechaRegistro; // Fecha de cración
            DateTime Fecha_de_CierreCancelacionHoy = DateTime.Now;    // Fecha de cierre, cancelación u hoy
            if (Cerrado != null) Fecha_de_CierreCancelacionHoy = Cerrado.FechaRegistro;
            else
            if (Cancelado != null) Fecha_de_CierreCancelacionHoy = Cancelado.FechaRegistro;

            // Obterner datos del último tecnico asignado que tuvo el ticket DataTenico, Inicio_Jrnd, Final_Jrnd, DíasLaborales[]
            if (true)
            {
                // IF no data, then use dummy info, normal weekday, 9am to 5pm
                DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray();
                Inicio_Jrnd = new TimeSpan(9, 00, 00);
                Final_Jrnd = new TimeSpan(17, 00, 00);

                if (LastEntry.TecnicoAsignado != null &&
                    LastEntry.TecnicoAsignadoReag == null &&
                    LastEntry.TecnicoAsignadoReag2 == null)
                {
                    DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignado).FirstOrDefault();
                }
                else if (LastEntry.TecnicoAsignado != null &&
                        LastEntry.TecnicoAsignadoReag != null &&
                        LastEntry.TecnicoAsignadoReag2 == null)
                {
                    DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignadoReag).FirstOrDefault();
                }
                else if (LastEntry.TecnicoAsignado != null &&
                        LastEntry.TecnicoAsignadoReag != null &&
                        LastEntry.TecnicoAsignadoReag2 != null)
                {
                    DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignadoReag2).FirstOrDefault();
                }

                if (DataTecnico != null && DataTecnico.NombreTecnico != null)
                {
                    //empty the info dummy and replace with actual correct data
                    var Ini = Convert.ToDateTime(DataTecnico.HoraInicioATC);
                    var Fin = Convert.ToDateTime(DataTecnico.HoraFinATC);

                    Inicio_Jrnd = new TimeSpan(Ini.Hour, Ini.Minute, 00);
                    Final_Jrnd = new TimeSpan(Fin.Hour, Fin.Minute, 00);

                    var data_Dias = _sd.tbl_VentanaAtencion.Where(t => t.EmpleadoID == DataTecnico.EmpleadoID).FirstOrDefault();

                    if (data_Dias != null)
                    {
                        DiasLaborales = new string[0];
                        if (data_Dias.Lunes) { DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray(); }
                        if (data_Dias.Martes) { DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray(); }
                        if (data_Dias.Miercoles) { DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray(); }
                        if (data_Dias.Jueves) { DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray(); }
                        if (data_Dias.Viernes) { DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray(); }
                        if (data_Dias.Sabado) { DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray(); }
                        if (data_Dias.Domingo) { DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray(); }
                    }
                }

                JornadaDelTecnico.Inicio_Jrnd = Inicio_Jrnd;
                JornadaDelTecnico.Final_Jrnd = Final_Jrnd;
                JornadaDelTecnico.DiasLaborales = DiasLaborales;
                JornadaDelTecnico.debug = 0;
            }

            // Obtener SLA Objetivo      OK
            if (true)
            {
                // Tiempo que le debería tomar al Grupo Resolutor resolver la incidencia
                //var Subcategoria = _sd.cat_SubCategoria.Where(t => t.Id == LastEntry.SubCategoria).FirstOrDefault();
                //if (Subcategoria != null) SLA_Objetivo_int = int.Parse(Subcategoria.SLA);
                //else SLA_Objetivo_int = 99;
                SLA_Objetivo_int = (MatrizCat != null) ? Int32.Parse(MatrizCat.SLAObjetivo) : 99;
                var SLA_Objetivo = new SlaTimesVm
                {
                    Type = "SLA Objetivo",
                    Order = 1,
                    Enable = true,
                    Tecnico = "~",
                    Time = SLA_Objetivo_int + ":00"
                };
                slaTimesVms.Add(SLA_Objetivo);
            }

            // Obtener SLA Total Y SLA En Espera y Actual
            if (true)
            {
                string       prev_evento = "";
                UserTimeData prev_tecnico = new UserTimeData();
                DateTime     prev_fecha = DateTime.Now;
                int  min_asignado = 0;         // tiempo laboral que el ticket ha estado asignado a alguien
                int  min_asignado_actual = 0;  // tiempo laboral que el ticket ha estado asignado al tecnico actual
                int  min_espera = 0;           // tiempo laboral que el ticket ha estado En Espera
                int  min_prev_asign = 0;       // tiempo 27/7 que el ticket no estuvo asignado a nadie
                int  min_espera_prev = 0;      // tiempo que otros grupos o tecnicos lo tuvieron en espera antes que el tecnico actual
                bool asignado_a_tecnico_actual = false; // ticket ya está en su última asignación
                min = 0;
                DateTime fecha_Ultima_asignación = new DateTime();
                var historico2 = historico.OrderBy(t => t.FechaRegistro).ToList();
                if (Asignado != null) fecha_Ultima_asignación = Asignado.FechaRegistro;
                foreach (var evento in historico2) {
                    if (prev_evento.Contains("Abierto"))
                    {
                        prev_tecnico = GetUserTimeData(-1); // obtener usertimedata dummy
                        min = Min_Laborales_Entre_Eventos(prev_fecha, evento.FechaRegistro, prev_tecnico);
                        min_prev_asign += min;
                        min_espera_prev += min_espera;
                        min_espera = 0;
                        asignado_a_tecnico_actual = false;
                    }
                    if (prev_evento.Contains("Asignado") || prev_evento.Contains("Trabajando"))
                    {
                        // agregar if para contar si hay otro Abierto después de asignado_a_tecnicoactual, de ser así reiniciar contaodr de asignado-actual
                        // tambier resetear min_espera
                        min = Min_Laborales_Entre_Eventos(prev_fecha, evento.FechaRegistro, prev_tecnico);
                        min_asignado += min;
                        if (asignado_a_tecnico_actual) min_asignado_actual += min;
                    }
                    if (prev_evento.Contains("Espera"))
                    {
                        min = Min_Laborales_Entre_Eventos(prev_fecha, evento.FechaRegistro, prev_tecnico);
                        min_espera += min;
                    }
                    //--- preparacion
                    ;
                    prev_evento     = evento.Estatus;
                    prev_fecha      = evento.FechaRegistro;
                    prev_tecnico    = GetUserTimeData(     GetTecnicoAsignadoByHistoric(evento)    );
                    // if evento previo fue la última asignación del ticket
                    if (prev_fecha == fecha_Ultima_asignación)  { 
                        asignado_a_tecnico_actual = true;
                    }
                }
                if (true)
                {
                    if (prev_evento.Contains("Abierto"))
                    {
                        prev_tecnico = GetUserTimeData(-1); // obtener usertimedata dummy
                        min = Min_Laborales_Entre_Eventos(prev_fecha, DateTime.Now, prev_tecnico);
                        min_prev_asign += min; 
                        min_asignado_actual = 0;
                        min_espera_prev = min_espera;
                        min_espera = 0;
                    }
                    if (prev_fecha == fecha_Ultima_asignación) { asignado_a_tecnico_actual = true; }
                    if (prev_evento.Contains("Asignado") || prev_evento.Contains("Trabajando"))
                    {
                        min = Min_Laborales_Entre_Eventos(prev_fecha, DateTime.Now, prev_tecnico);
                        min_asignado += min;
                        if (asignado_a_tecnico_actual) min_asignado_actual += min;
                    }
                    if (prev_evento.Contains("Espera"))
                    {
                        min = Min_Laborales_Entre_Eventos(prev_fecha, DateTime.Now, prev_tecnico);
                        min_espera += min;
                    }
                } // obtener tiempo del último elemento del historico

                min = min_asignado + min_prev_asign + min_espera + min_espera_prev; // minutos que estuvo asignado - minutos previos a primera asignacion //--------------------------------- agregar aunque esté en espera
                hrs = (min != 0) ? min / 60 : 0;
                var SLA_Total = new SlaTimesVm
                {
                    Type = "SLA total",
                    Order = 3,
                    Time = String_Reloj_FromMinutos(min),
                    Enable = true,
                    Color = GetColor(hrs, SLA_Objetivo_int),
                    Tecnico = "-"
                };
                slaTimesVms.Add(SLA_Total);

                min = min_espera;
                if (LastEntry.EstatusTicket == 7 || min != 0)
                {
                    var timeesp = new SlaTimesVm
                    {
                        Type = "En espera",
                        Order = 3,
                        Time = String_Reloj_FromMinutos(min),
                        Enable = true,
                        Color = "#D7F8EE",
                        Tecnico = "-"
                    };
                    slaTimesVms.Add(timeesp);
                }

                min = min_asignado_actual;
                if (asignado_a_tecnico_actual && LastEntry.EstatusTicket != 1) { 
                    hrs = (min != 0) ? min / 60 : 0;
                    var actual = new SlaTimesVm
                    {
                        Type = "Tiempo Actual",
                        Order = 2,
                        Time = String_Reloj_FromMinutos(min),
                        Enable = true,
                        Color = GetColor(hrs, SLA_Objetivo_int),
                        Tecnico = LastEntry.TecnicoAsignado
                    };
                    slaTimesVms.Add(actual);
                }

                if (false) {

                    //bool ticket_esta_asignado = false;
                    //DateTime Asignacion_Anterior = new DateTime();
                    //var historico_asignaciones = historico.Where(t => t.EstatusTicket == 2);
                    //int Tecnico_Previamente_Asignado = 0;
                    //foreach (var evento in historico_asignaciones)
                    //{
                    //    if (ticket_esta_asignado) // No ejecutar durante primera asignación
                    //    { 
                    //        UserTimeData Jornada_Tecnico_Previamente_Asignado = GetUserTimeData(Tecnico_Previamente_Asignado);
                    //         // obtener minutos trabajados por el tecnico anterior desde su asignación hasta ahora que fue asignado a otrox
                    //        min += Min_Laborales_Entre_Eventos(Asignacion_Anterior, evento.FechaRegistro, Jornada_Tecnico_Previamente_Asignado);

                    //        // tecnico de este evento se convierte en el tecnico anterior, busca siguiente asignación
                    //        Tecnico_Previamente_Asignado = GetTecnicoAsignadoByHistoric(evento);
                    //    }                   

                    //    if (!ticket_esta_asignado) ticket_esta_asignado = true;                    

                    //    Asignacion_Anterior = evento.FechaRegistro;
                    //}

                    // A este momento
                    // 1: si hay varias asignaciones min tiene una sumatoria de los minutos que otros tecnicos lo tuvieron asignado
                    // 2: min está vacio por que el tecnico actual es el único y Asignación_Anterior tiene info de la primera asignación
                    //min += Min_Laborales_Entre_Eventos(Asignacion_Anterior, Fecha_de_CierreCancelacionHoy, JornadaDelTecnico); // Contador Horario Laboral //1
                }
            }

            // Obtener SLA En Espera (Desde creación)       ------- ignorar
            if (Asignado != null)
            {
                //// Tiempo total que el ticket ha pasado "En Espera" desde su creación (contador 24hrs)
                //DateTime InicioEspera = DateTime.Now, FinalEspera = DateTime.Now;
                //bool EnEspera = false, Inicio_y_Final_Espera_Obtenidos = false;
                //min = 0; // contador de minutos en espera

                //foreach (var evento in historico)
                //{
                //    // Obtener inicio de En Espera
                //    if (evento.EstatusTicket == 7 && !EnEspera)
                //    {
                //        EnEspera = true;
                //        InicioEspera = evento.FechaRegistro;
                //    }

                //    // Obtener final de En Espera
                //    if ((evento.EstatusTicket == 3 || evento.EstatusTicket == 8) && EnEspera)
                //    {
                //        EnEspera = false;
                //        FinalEspera = evento.FechaRegistro;
                //        Inicio_y_Final_Espera_Obtenidos = true;
                //    }

                //    // Calcular y sumar a contador (min)
                //    if (Inicio_y_Final_Espera_Obtenidos)
                //    {
                //        Inicio_y_Final_Espera_Obtenidos = false;
                //        int TecnicoQuePusoEnEspera = GetTecnicoAsignadoByHistoric(evento);
                //        UserTimeData Jornada_TecnicoQuePusoEnEspera = GetUserTimeData(TecnicoQuePusoEnEspera);
                //        min += Min_Laborales_Entre_Eventos(InicioEspera, FinalEspera, Jornada_TecnicoQuePusoEnEspera); //21
                //    }
                //    // fin de foreach
                //}
                //// Sino se cerró la última espera
                //if (EnEspera) { min += Min_Laborales_Entre_Eventos(InicioEspera, DateTime.Now, JornadaDelTecnico); } //22

                //if (min != 0)
                //{
                //    var timeesp = new SlaTimesVm
                //    {
                //        Type = "En espera",
                //        Order = 3,
                //        Time = String_Reloj_FromMinutos(min),
                //        Enable = true,
                //        Color = "#D7F8EE",
                //        Tecnico = "-"
                //    };
                //    slaTimesVms.Add(timeesp);
                //}
            }

            // Obtener Tiempo En Espera (Por Tecnico Actual)      -------------Ignorar
            if (Asignado != null)
            {
                //DateTime InicioEspera = DateTime.Now, FinalEspera = DateTime.Now;
                //bool EnEspera = false, Inicio_y_Final_Espera_Obtenidos = false;
                //min = 0; // contador de minutos en espera

                //var historico_Tecnico_Actual = new List<his_Ticket>();

                //historico_Tecnico_Actual = historico.Where(t => t.FechaRegistro > Asignado.FechaRegistro).ToList();

                //foreach (var evento in historico_Tecnico_Actual)
                //{
                //    // Obtener inicio de En Espera
                //    if (evento.EstatusTicket == 7 && !EnEspera)
                //    {
                //        EnEspera = true;
                //        InicioEspera = evento.FechaRegistro;
                //    }
                //    // Obtener final de En Espera
                //    if (evento.EstatusTicket != 7 && EnEspera)
                //    {
                //        EnEspera = false;
                //        FinalEspera = evento.FechaRegistro;
                //        Inicio_y_Final_Espera_Obtenidos = true;
                //    }
                //    if (Inicio_y_Final_Espera_Obtenidos)
                //    {
                //        Inicio_y_Final_Espera_Obtenidos = false;
                //        min += Min_Laborales_Entre_Eventos(InicioEspera, FinalEspera, JornadaDelTecnico); //31
                //    }
                //}

                //// Si todavía está en espera
                //if (EnEspera) { min += Min_Laborales_Entre_Eventos(InicioEspera, DateTime.Now, JornadaDelTecnico); } //32

                //min_EnEspera_Tecnico_Actual = min;
            } 

            // Obtener SLA Tiempo Actual    ----- Ignorar
            if (Asignado != null)
            {
                //min = 0;
                //// Tiempo Actual = Tiempo laboral desde que fue asignado al último tecnico hasta Cierre/Cancelación/Now
                //min = Min_Laborales_Entre_Eventos(Asignado.FechaRegistro, Fecha_de_CierreCancelacionHoy, JornadaDelTecnico); //4
                //min = min - min_EnEspera_Tecnico_Actual;
                //hrs = (min != 0) ? min / 60 : 0;
                //var actual = new SlaTimesVm
                //{
                //    Type = "Tiempo Actual",
                //    Order = 2,
                //    Time = String_Reloj_FromMinutos(min),
                //    Enable = true,
                //    Color = GetColor(hrs, SLA_Objetivo_int),
                //    Tecnico = LastEntry.TecnicoAsignado
                //};
                //slaTimesVms.Add(actual);
            }

            // Obtener En Garantía       OK
            if (Garantia != null)
            {
                //Obtener el tiempo que el ticket ha estado en garantía
                DateTime end = DateTime.Now;
                if (Cerrado != null) end = Cerrado.FechaRegistro;
                var difdias = GetHoursDifference24Hrs(Garantia.FechaRegistro, end);
                var timer = new TimerVm { Horas = difdias.Horas, Minutos = difdias.Minutos };

                //Obtener tiempo que debe estar en garantía el ticket
                var Garantia_Definida_Para_Subcat = (MatrizCat != null) ? Int32.Parse(MatrizCat.Garantia) : 8;
                TimeSpan Garantia_Definida = new TimeSpan(Garantia_Definida_Para_Subcat, 0, 0);
                TimeSpan Tiempo_En_Garantia = new TimeSpan(difdias.Horas, difdias.Minutos, 0);
                TimeSpan Time_to_show = new TimeSpan(0, 0, 0);
                if (Tiempo_En_Garantia > Garantia_Definida)
                {
                    timer.Horas = 0;
                    timer.Minutos = 0;
                }
                else
                {
                    Time_to_show = Garantia_Definida - Tiempo_En_Garantia;
                    timer.Horas = (int)Time_to_show.TotalHours;
                    timer.Minutos = (int)Time_to_show.Minutes;
                }

                var GarantiaSLA = new SlaTimesVm
                {
                    Type = "En Garantía",
                    Order = 4,
                    Time = GetHoursFormat(timer),
                    Enable = true,
                    Color = "#DAF7A6",
                    Tecnico = "~"
                };
                slaTimesVms.Add(GarantiaSLA);
            }

            return slaTimesVms.OrderBy(t => t.Order).ToList();
        }

        public bool inTime(List<his_Ticket> historico, List<cat_MatrizCategoria> catMatrizCat, List<tbl_User> tbl_User, List<tbl_VentanaAtencion> tbl_VentanaAtencion) 
            // Method 4, para reporteria, regresa true si sla total < sla objetivo
        {
            // copiado pegado y modificado de GetSlaTimes Method 3
            // nótese como se eliminaron todas las llamadas a BD, esto para optimizar el conteo
            historico = historico.OrderBy(t => t.IdHis).ToList();
            UserTimeData JornadaDelTecnico = new UserTimeData();
            string[] DiasLaborales = new string[0];
            TimeSpan Inicio_Jrnd = new TimeSpan(9, 00, 00);
            TimeSpan Final_Jrnd = new TimeSpan(17, 00, 00);
            tbl_User DataTecnico = new tbl_User();
            his_Ticket LastEntry = historico.OrderByDescending(t => t.IdHis).FirstOrDefault();
            his_Ticket FirstEntry = historico.Where(t => t.Historial = true).FirstOrDefault();
            his_Ticket Asignado = historico.OrderByDescending(t => t.FechaRegistro).Where(t => t.EstatusTicket == 2).FirstOrDefault(); // Fecha de última asignación
            his_Ticket Cerrado = historico.Where(t => t.EstatusTicket == 6).FirstOrDefault(); // fecha de cierre
            his_Ticket Cancelado = historico.Where(t => t.EstatusTicket == 8).FirstOrDefault(); // fecha de cancelación
            his_Ticket Garantia = historico.Where(t => t.EstatusTicket == 5).FirstOrDefault(); // fecha que empezó a estar en garantía// 
            var MatrizCat = catMatrizCat.FirstOrDefault(t => t.IDSubCategoria == LastEntry.SubCategoria);
            int SLA_Objetivo_int = 0, hrs = 0, min = 0; // vars usados para parsear datos
            System.Diagnostics.Debug.WriteLine("SLA: " + FirstEntry.IdTicket);

            // Obtener Eventos de creación y Ahora, si está cerrado o cancelado remplazar ahora con la fecha de ese evento
            DateTime Fecha_de_Creacion_de_ticket = FirstEntry.FechaRegistro; // Fecha de cración
            DateTime Fecha_de_CierreCancelacionHoy = DateTime.Now;    // Fecha de cierre, cancelación u hoy
            if (Cerrado != null)    Fecha_de_CierreCancelacionHoy = Cerrado.FechaRegistro; else
            if (Cancelado != null)  Fecha_de_CierreCancelacionHoy = Cancelado.FechaRegistro;

            // Obterner datos del último tecnico asignado que tuvo el ticket DataTenico, Inicio_Jrnd, Final_Jrnd, DíasLaborales[]
            if (true)
            {
                // IF no data, then use dummy info, normal weekday, 9am to 5pm
                DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray();
                Inicio_Jrnd = new TimeSpan(9, 00, 00);
                Final_Jrnd = new TimeSpan(17, 00, 00);

                if (LastEntry.TecnicoAsignado != null &&
                    LastEntry.TecnicoAsignadoReag == null &&
                    LastEntry.TecnicoAsignadoReag2 == null)
                {
                    DataTecnico = tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignado).FirstOrDefault();
                }
                else if (LastEntry.TecnicoAsignado != null &&
                        LastEntry.TecnicoAsignadoReag != null &&
                        LastEntry.TecnicoAsignadoReag2 == null)
                {
                    DataTecnico = tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignadoReag).FirstOrDefault();
                }
                else if (LastEntry.TecnicoAsignado != null &&
                        LastEntry.TecnicoAsignadoReag != null &&
                        LastEntry.TecnicoAsignadoReag2 != null)
                {
                    DataTecnico = tbl_User.Where(x => x.NombreTecnico == LastEntry.TecnicoAsignadoReag2).FirstOrDefault();
                }

                if (DataTecnico != null && DataTecnico.NombreTecnico != null)
                {
                    //empty the info dummy and replace with actual correct data
                    var Ini = Convert.ToDateTime(DataTecnico.HoraInicioATC);
                    var Fin = Convert.ToDateTime(DataTecnico.HoraFinATC);

                    Inicio_Jrnd = new TimeSpan(Ini.Hour, Ini.Minute, 00);
                    Final_Jrnd = new TimeSpan(Fin.Hour, Fin.Minute, 00);

                    var data_Dias = tbl_VentanaAtencion.Where(t => t.EmpleadoID == DataTecnico.EmpleadoID).FirstOrDefault();

                    if (data_Dias != null)
                    {
                        DiasLaborales = new string[0];
                        if (data_Dias.Lunes) { DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray(); }
                        if (data_Dias.Martes) { DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray(); }
                        if (data_Dias.Miercoles) { DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray(); }
                        if (data_Dias.Jueves) { DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray(); }
                        if (data_Dias.Viernes) { DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray(); }
                        if (data_Dias.Sabado) { DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray(); }
                        if (data_Dias.Domingo) { DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray(); }
                    }
                }

                JornadaDelTecnico.Inicio_Jrnd = Inicio_Jrnd;
                JornadaDelTecnico.Final_Jrnd = Final_Jrnd;
                JornadaDelTecnico.DiasLaborales = DiasLaborales;
                JornadaDelTecnico.debug = 0;
            }

            // Obtener SLA Objetivo      OK
            if (true)
            {
                SLA_Objetivo_int = (MatrizCat != null) ? Int32.Parse(MatrizCat.SLAObjetivo) : 99;
                //var SLA_Objetivo = new SlaTimesVm
                //{
                //    Type = "SLA Objetivo",
                //    Order = 1,
                //    Enable = true,
                //    Tecnico = "~",
                //    Time = SLA_Objetivo_int + ":00"
                //};
                //slaTimesVms.Add(SLA_Objetivo);
            }

            // Obtener SLA Total Y SLA En Espera y Actual
            if (true)
            {
                string prev_evento = "";
                UserTimeData prev_tecnico = new UserTimeData();
                DateTime prev_fecha = DateTime.Now;
                int min_asignado = 0;         // tiempo laboral que el ticket ha estado asignado a alguien
                int min_asignado_actual = 0;  // tiempo laboral que el ticket ha estado asignado al tecnico actual
                int min_espera = 0;           // tiempo laboral que el ticket ha estado En Espera
                int min_prev_asign = 0;       // tiempo 27/7 que el ticket no estuvo asignado a nadie
                int min_espera_prev = 0;      // tiempo que otros grupos o tecnicos lo tuvieron en espera antes que el tecnico actual
                bool asignado_a_tecnico_actual = false; // ticket ya está en su última asignación
                min = 0;
                DateTime fecha_Ultima_asignación = new DateTime();
                var historico2 = historico.OrderBy(t => t.FechaRegistro).ToList();
                if (Asignado != null) fecha_Ultima_asignación = Asignado.FechaRegistro;
                foreach (var evento in historico2)
                {
                    if (prev_evento.Contains("Abierto"))
                    {
                        prev_tecnico = GetUserTimeData(-1); // obtener usertimedata dummy
                        min = Min_Laborales_Entre_Eventos(prev_fecha, evento.FechaRegistro, prev_tecnico);
                        min_prev_asign += min;
                        min_espera_prev += min_espera;
                        min_espera = 0;
                        asignado_a_tecnico_actual = false;
                    }
                    if (prev_evento.Contains("Asignado") || prev_evento.Contains("Trabajando"))
                    {
                        // agregar if para contar si hay otro Abierto después de asignado_a_tecnicoactual, de ser así reiniciar contaodr de asignado-actual
                        // tambier resetear min_espera
                        min = Min_Laborales_Entre_Eventos(prev_fecha, evento.FechaRegistro, prev_tecnico);
                        min_asignado += min;
                        if (asignado_a_tecnico_actual) min_asignado_actual += min;
                    }
                    if (prev_evento.Contains("Espera"))
                    {
                        min = Min_Laborales_Entre_Eventos(prev_fecha, evento.FechaRegistro, prev_tecnico);
                        min_espera += min;
                    }
                    //--- preparacion
                    ;
                    prev_evento = evento.Estatus;
                    prev_fecha = evento.FechaRegistro;
                    prev_tecnico = GetUserTimeData(GetTecnicoAsignadoByHistoric(evento, tbl_User), tbl_User, tbl_VentanaAtencion);
                    // if evento previo fue la última asignación del ticket
                    if (prev_fecha == fecha_Ultima_asignación)
                    {
                        asignado_a_tecnico_actual = true;
                    }
                }
                if (true)
                {
                    if (prev_evento.Contains("Abierto"))
                    {
                        prev_tecnico = GetUserTimeData(-1); // obtener usertimedata dummy
                        min = Min_Laborales_Entre_Eventos(prev_fecha, DateTime.Now, prev_tecnico);
                        min_prev_asign += min;
                        min_asignado_actual = 0;
                        min_espera_prev = min_espera;
                        min_espera = 0;
                    }
                    if (prev_fecha == fecha_Ultima_asignación) { asignado_a_tecnico_actual = true; }
                    if (prev_evento.Contains("Asignado") || prev_evento.Contains("Trabajando"))
                    {
                        min = Min_Laborales_Entre_Eventos(prev_fecha, DateTime.Now, prev_tecnico);
                        min_asignado += min;
                        if (asignado_a_tecnico_actual) min_asignado_actual += min;
                    }
                    if (prev_evento.Contains("Espera"))
                    {
                        min = Min_Laborales_Entre_Eventos(prev_fecha, DateTime.Now, prev_tecnico);
                        min_espera += min;
                    }
                } // obtener tiempo del último elemento del historico

                min = min_asignado + min_prev_asign + min_espera + min_espera_prev; // minutos que estuvo asignado - minutos previos a primera asignacion //--------------------------------- agregar aunque esté en espera
                hrs = (min != 0) ? min / 60 : 0;
                //var SLA_Total = new SlaTimesVm
                //{
                //    Type = "SLA total",
                //    Order = 3,
                //    Time = String_Reloj_FromMinutos(min),
                //    Enable = true,
                //    Color = GetColor(hrs, SLA_Objetivo_int),
                //    Tecnico = "-"
                //};
                //slaTimesVms.Add(SLA_Total);     
            }

            TimeSpan objetivo = new TimeSpan(SLA_Objetivo_int,0,0);
            TimeSpan slatotal = new TimeSpan(0, min, 0);

            if (objetivo > slatotal) return false; 
            else return true;
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public List<SlaTimesVm> GetSlaTiempoActual(List<SlaTimesVm> slatimes, int slaobjetivo)
        {
            // Esto "pausa" el tiempo actual cuando el ticket está en espera
            // funciona restando al tiempo actual el tiempo que el ticket estuvo en espera
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
                if (te_Hora != 0) TiempoEnEsperaEnMinutos = (te_Hora * 60) + te_Minu;
                else TiempoEnEsperaEnMinutos = te_Minu;
            }

            //Restarle al tiempo actual el tiempo EN ESPERA
            var TiempoActual = slatimes.Where(t => t.Type == "Tiempo Actual").FirstOrDefault().Time;
            string[] strTiempoActual = TiempoActual.Split(':');
            int ta_Hora = Int32.Parse(strTiempoActual[0]);
            int ta_Minu = Int32.Parse(strTiempoActual[1]);
            if (ta_Hora != 0) TiempoActual_EnMinutos = (ta_Hora * 60) + ta_Minu;
            else TiempoActual_EnMinutos = ta_Minu;

            TiempoActual_EnMinutos = TiempoActual_EnMinutos - TiempoEnEsperaEnMinutos;

            //Cambia el Tiempo Actual de la lista por el obtenido
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
            if (start.Date == end.Date & start.TimeOfDay >= inijrn && end.TimeOfDay <= finjrn && apertura == false)
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
                    else if (counter.Date == end.Date && start.TimeOfDay > inijrn && end.TimeOfDay < finjrn && apertura == false)
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

        public string GetHoursDifference24Hrs2(DateTime start, DateTime end)
        {
            TimeSpan diff = end - start;
            double horas = (int)diff.TotalHours;
            int minutos = diff.Minutes;
            return horas + ":" + minutos;
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

        public string String_Reloj_FromMinutos(int min) { 
            return (min / 60).ToString() + ":" + (min % 60).ToString("D2");
        }

        // método multiplicador
        public string ContarTiempoLaboralEntreEventos1(DateTime A_Date, DateTime B_Date, TimeSpan Inicio_Jorn, TimeSpan Final_Jorn, string[] DiasLaborales, int debug = 0)
        {
            /*
             * debug 
             * 1  = SLA Total
             * 21 = En Espera desde creación
             * 22 = En Espera desde creación y sigue en espera
             * 31 = En Espera desde asignación a último técnico
             * 32 = En Espera desde asignación a último técnico y sigue en espera
             * 4  = SLA Actual
             */

            string string_day = "";
            TimeSpan total_work_time = new TimeSpan (0,0,0);

            TimeSpan A_Time = new TimeSpan(A_Date.Hour, A_Date.Minute, 0);
            TimeSpan B_Time = new TimeSpan(B_Date.Hour, B_Date.Minute, 0);
            A_Date = A_Date.Date;
            B_Date = B_Date.Date;

            TimeSpan FullDay = Final_Jorn - Inicio_Jorn;
            int FullDay_in_Minutes = Int32.Parse(FullDay.TotalMinutes.ToString());   

            if (A_Date == B_Date) // ambos evento pasan el mismo día
            {
                string_day = A_Date.DayOfWeek.ToString();
                if (DiasLaborales.Any(d => d == string_day)) { // eventos pasaron en un día labroal
                    TimeSpan first = (A_Time < Inicio_Jorn) ? Inicio_Jorn : A_Time; // El contador no debe empezar antes del inicio de la jornada
                    TimeSpan lastt = (B_Time < Final_Jorn) ? B_Time : Final_Jorn;   // El contador no debe terminar después del final de la jornada
                    total_work_time = lastt - first;
                }
                else {  
                    // eventos no pasaron en un día labroal, regresar 00:00
                    ;
                }
            }
            else {
                /*
                    Contar días laborales entre fechas
                    Multiplicar cantidad obtenida por tiempo trabajado en un día completo
                    Restar el tiempo no trabajado 
                        Tiempo antes de la hora inicial el primer día
                        Tiempo depsués de la hora final el últmimo día
                */

                // Get days worked
                DateTime date_current = A_Date;
                int amount_of_days = 0;
                while (date_current <= B_Date) {
                    string_day = date_current.DayOfWeek.ToString();
                    if (DiasLaborales.Any(d => d == string_day)) {
                        amount_of_days = amount_of_days + 1; 
                    }
                    add_1_day(date_current);
                }

                System.Diagnostics.Debug.WriteLine("Entre fechas: " + A_Date + " and " + B_Time);
                System.Diagnostics.Debug.WriteLine("Hay " + amount_of_days + " días laborales");
                // Get time worked
                total_work_time = TimeSpan.FromMinutes(FullDay_in_Minutes * amount_of_days); 


                // Get time not worked the first day
                TimeSpan not_worked = new TimeSpan(0, 0, 0);
                string_day = A_Date.DayOfWeek.ToString();
                if (DiasLaborales.Any(d => d == string_day)) // primer día fue un día laboral?
                {
                    if (A_Time < Final_Jorn) // Evento empezó antes del final de jornada
                    {
                        // tiempo desde el inicio de la jornada hasta que empezó el evento A no debe ser contado
                        not_worked = A_Time - Inicio_Jorn;
                        total_work_time = total_work_time - not_worked;
                    }
                }

                // Restar tiempo entre el final de la jornada y evento b
                // Get time out of the last day
                string_day = B_Date.DayOfWeek.ToString();
                if (DiasLaborales.Any(d => d == string_day))
                {
                    if (B_Time > Inicio_Jorn) // Evento terminó después de iniciar la jornada
                    {
                        // tiempo desde evento b hasta el final de la jornada no debe ser contado
                        not_worked = Final_Jorn - B_Time;
                        total_work_time = total_work_time - not_worked;
                    }
                }
            }

            // Output total work time
            System.Diagnostics.Debug.WriteLine("Total work time: " + total_work_time.ToString(@"hh\:mm"));
            var hors = (int)total_work_time.TotalHours;
            var mins = total_work_time.Minutes;
            return hors + ":" + mins;
        }
        
        // método sumador
        public string ContarTiempoLaboralEntreEventos(DateTime Evento_A, DateTime Evento_B, TimeSpan Inicio_Jornada, TimeSpan Final_Jornada, string[] DiasLaborales, int debug = 0)
        {
            DateTime start_datetime, end_datetime, current_datetime;
            TimeSpan work_start_time, work_end_time;
            string[] working_days;

            // Input start and end datetime
            start_datetime = Evento_A;
            end_datetime = Evento_B;

            // Input employee's working schedule
            working_days = DiasLaborales;
            work_start_time = Inicio_Jornada;
            work_end_time = Final_Jornada;
            int total_minutes = 0;

            current_datetime = start_datetime;

            string day = current_datetime.DayOfWeek.ToString();
            while (current_datetime < end_datetime) 
            {
                if (day != current_datetime.DayOfWeek.ToString())
                    System.Diagnostics.Debug.WriteLine("Day añadido: " + day + " " + current_datetime);

                day = current_datetime.DayOfWeek.ToString();
                if (working_days.Any(d => d == day)) { // El día en cuestión ¿el tecnico trabaja?
                    TimeSpan Clock_current = new TimeSpan(0,current_datetime.Hour, current_datetime.Minute, 0); // Hora:Minuto siendo analizado
                    ;
                    if (Clock_current > Inicio_Jornada && Clock_current < Final_Jornada) // Hora:Minuto ¿Está dentro del horario laboral del tecnico?
                    {
                        total_minutes = total_minutes + 1; // sumar 1 al contador de minutos laborales
                    }
                }
                current_datetime = current_datetime.AddMinutes(1);  

                //Time Jump on days
                if (current_datetime.TimeOfDay >= new TimeSpan(23, 59, 0))
                {
                    // add a day
                    current_datetime = current_datetime.Date.AddDays(1).Add(new TimeSpan(0, 0, 0));

                    // check if adding a month is needed (already takes into consideration February and leapyears)
                    int DaysInMonth = DateTime.DaysInMonth(current_datetime.Year, current_datetime.Month);
                    if (current_datetime.Day > DaysInMonth) {
                        // check if adding a year is needed
                        if (current_datetime.Month == 12){
                            current_datetime = current_datetime.AddYears(1);
                            current_datetime = current_datetime.AddDays(-(current_datetime.Day - 1));
                            current_datetime = current_datetime.AddMonths(-(current_datetime.Month - 1));

                        }
                        else {
                            //add a month and reset day
                            current_datetime = current_datetime.AddMonths(1);
                            current_datetime = current_datetime.AddDays(-(current_datetime.Day - 1));
                        }                        ;
                    }
                }
            }

            // Convert total minutes to hours and minutes
            int total_hours = total_minutes / 60;
            int remaining_minutes = total_minutes % 60;

            return total_hours + ":" + remaining_minutes;
        }
        
        // método multiplicador (output en minutos)
        public int Min_Laborales_Entre_Eventos(DateTime Start_Event, DateTime Finish_Event, UserTimeData User) {
            return Min_Laborales_Entre_Eventos(Start_Event, Finish_Event, User.Inicio_Jrnd, User.Final_Jrnd, User.DiasLaborales, User.debug);
        }
        public int Min_Laborales_Entre_Eventos(DateTime A_Date, DateTime B_Date, TimeSpan Inicio_Jorn, TimeSpan Final_Jorn, string[] DiasLaborales, int debug = 0)
        {
            /*
             * debug 
             * 1  = SLA Total
             * 21 = En Espera desde creación
             * 22 = En Espera desde creación y sigue en espera
             * 31 = En Espera desde asignación a último técnico
             * 32 = En Espera desde asignación a último técnico y sigue en espera
             * 4  = SLA Actual
             */

            string string_day = "";
            TimeSpan total_work_time = new TimeSpan (0,0,0);

            TimeSpan A_Time = new TimeSpan(A_Date.Hour, A_Date.Minute, 0);
            TimeSpan B_Time = new TimeSpan(B_Date.Hour, B_Date.Minute, 0);
            A_Date = A_Date.Date;
            B_Date = B_Date.Date;

            TimeSpan FullDay = Final_Jorn - Inicio_Jorn;
            int FullDay_in_Minutes = Int32.Parse(FullDay.TotalMinutes.ToString());   

            if (A_Date == B_Date) // ambos evento pasan el mismo día
            {
                string_day = A_Date.DayOfWeek.ToString();
                if (DiasLaborales.Any(d => d == string_day)) { // eventos pasaron en un día labroal
                    TimeSpan first = (A_Time < Inicio_Jorn) ? Inicio_Jorn : A_Time; // El contador no debe empezar antes del inicio de la jornada
                    TimeSpan lastt = (B_Time < Final_Jorn) ? B_Time : Final_Jorn;   // El contador no debe terminar después del final de la jornada
                    if (lastt > first) total_work_time = lastt - first; // If last > first: los eventos empezaron y terminaron después de la jornada laboral, devolver 0 min
                }
                else {  
                    // eventos no pasaron en un día labroal, regresar 00:00
                    ;
                }
            }
            else {
                /*
                    Contar días laborales entre fechas
                    Multiplicar cantidad obtenida por tiempo trabajado en un día completo
                    Restar el tiempo no trabajado 
                        Tiempo antes de la hora inicial el primer día
                        Tiempo depsués de la hora final el últmimo día
                */

                // Get days worked
                DateTime date_current = A_Date;
                int amount_of_days = 0;
                while (date_current <= B_Date) {
                    string_day = date_current.DayOfWeek.ToString();
                    if (DiasLaborales.Any(d => d == string_day)) {
                        amount_of_days = amount_of_days + 1; 
                    }
                    date_current = add_1_day(date_current);
                }

                //System.Diagnostics.Debug.WriteLine("Entre fechas: " + A_Date + " and " + B_Time);
                //System.Diagnostics.Debug.WriteLine("Hay " + amount_of_days + " días laborales");
                // Get time worked
                total_work_time = TimeSpan.FromMinutes(FullDay_in_Minutes * amount_of_days); 


                // Get time not worked the first day
                TimeSpan not_worked = new TimeSpan(0, 0, 0);
                string_day = A_Date.DayOfWeek.ToString();
                if (DiasLaborales.Any(d => d == string_day)) // primer día fue un día laboral?
                {
                    if (A_Time < Final_Jorn && A_Time > Inicio_Jorn) // Evento empezó en medio de la jornada
                    {
                        // tiempo desde el inicio de la jornada hasta que empezó el evento A no debe ser contado
                        not_worked = A_Time - Inicio_Jorn;
                        total_work_time = total_work_time - not_worked;
                    }
                    if (A_Time > Final_Jorn)
                    {
                        // Usuario no trabajó el primer día 
                        total_work_time = total_work_time - FullDay;
                    }
                }

                // Restar tiempo entre el final de la jornada y evento b
                // Get time out of the last day
                string_day = B_Date.DayOfWeek.ToString();
                if (DiasLaborales.Any(d => d == string_day))
                {
                    if (B_Time > Inicio_Jorn && B_Time < Final_Jorn) // Evento terminó en medio de la jornada
                    {
                        // tiempo desde evento b hasta el final de la jornada no debe ser contado
                        not_worked = Final_Jorn - B_Time;
                        total_work_time = total_work_time - not_worked;
                    }
                    if (B_Time < Inicio_Jorn)
                    {
                        // Usuario no trabajó el último día 
                        total_work_time = total_work_time - FullDay;
                    }

                }
            }

            // Output total work time
            //System.Diagnostics.Debug.WriteLine("Total work time: " + total_work_time.ToString(@"hh\:mm"));
            return Int32.Parse(total_work_time.TotalMinutes.ToString());
        }


        public DateTime add_1_day(DateTime current_datetime) {
            // add a day
            current_datetime = current_datetime.Date.AddDays(1).Add(new TimeSpan(0, 0, 0));

            // check if adding a month is needed (already takes into consideration February and leapyears)
            int DaysInMonth = DateTime.DaysInMonth(current_datetime.Year, current_datetime.Month);
            if (current_datetime.Day > DaysInMonth)
            {
                // check if adding a year is needed
                if (current_datetime.Month == 12)
                {
                    current_datetime = current_datetime.AddYears(1);
                    current_datetime = current_datetime.AddDays(-(current_datetime.Day - 1));
                    current_datetime = current_datetime.AddMonths(-(current_datetime.Month - 1));

                }
                else
                {
                    //add a month and reset day
                    current_datetime = current_datetime.AddMonths(1);
                    current_datetime = current_datetime.AddDays(-(current_datetime.Day - 1));
                };
            }
            return current_datetime;
        }
        public UserTimeData GetUserTimeData(int tbl_user_ID) { // this is not EmployeeId but tbl_user id
            UserTimeData utd = new UserTimeData();
            var DiasLaborales = new string[0];
            // info dummy
            DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray();
            DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray();
            DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray();
            DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray();
            DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray();
            utd.DiasLaborales = DiasLaborales;
            utd.Inicio_Jrnd = new TimeSpan(9, 00, 00);
            utd.Final_Jrnd = new TimeSpan(17, 00, 00);

            // if user 0  then return dummy, 9-5pm, 5 day weekk
            // if user -1 then return full time, 24/7
            if (tbl_user_ID == 0)
            {
                return utd;
            }
            else if (tbl_user_ID == -1) {
                DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray(); 
                DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray();
                utd.DiasLaborales = DiasLaborales;
                utd.Inicio_Jrnd = new TimeSpan(0, 00, 00);
                utd.Final_Jrnd = new TimeSpan(23, 59, 00);

                return utd;
            }

            // ----------------------------------- Actually get the info
            var user = _sd.tbl_User.FirstOrDefault(t => t.Id == tbl_user_ID);
            if (user == null) 
            {
                return utd;
            }
            else {

                //empty the info dummy and replace with actual correct data
                var Ini = Convert.ToDateTime(user.HoraInicioATC);
                var Fin = Convert.ToDateTime(user.HoraFinATC);

                utd.Inicio_Jrnd = new TimeSpan(Ini.Hour, Ini.Minute, 00);
                utd.Final_Jrnd = new TimeSpan(Fin.Hour, Fin.Minute, 00);

                var data_Dias = _sd.tbl_VentanaAtencion.Where(t => t.EmpleadoID == user.EmpleadoID).FirstOrDefault();

                if (data_Dias != null)
                {
                    DiasLaborales = new string[0]; // Vaciar info dummy
                    if (data_Dias.Lunes) {      DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray(); }
                    if (data_Dias.Martes) {     DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray(); }
                    if (data_Dias.Miercoles) {  DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray(); }
                    if (data_Dias.Jueves) {     DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray(); }
                    if (data_Dias.Viernes) {    DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray(); }
                    if (data_Dias.Sabado) {     DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray(); }
                    if (data_Dias.Domingo) {    DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray(); }
                }
                utd.DiasLaborales = DiasLaborales;

            }

            return utd;
        }
        public int GetTecnicoAsignadoByHistoric(his_Ticket his) {
            // DOES NOT RETURN EMPLOYEE ID, only tbl_user id
            int id = 0;
            var DataTecnico = new tbl_User();
            if (    his.TecnicoAsignado         != null &&
                    his.TecnicoAsignadoReag     == null &&
                    his.TecnicoAsignadoReag2    == null)
            {
                DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == his.TecnicoAsignado).FirstOrDefault();
            }
            else if (his.TecnicoAsignado        != null &&
                     his.TecnicoAsignadoReag    != null &&
                     his.TecnicoAsignadoReag2   == null)
            {
                DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == his.TecnicoAsignadoReag).FirstOrDefault();
            }
            else if (his.TecnicoAsignado        != null &&
                     his.TecnicoAsignadoReag    != null &&
                     his.TecnicoAsignadoReag2   != null)
            {
                DataTecnico = _sd.tbl_User.Where(x => x.NombreTecnico == his.TecnicoAsignadoReag2).FirstOrDefault();
            }

            if(DataTecnico != null) id = DataTecnico.Id;
            if (id == 0) 
                ;
            return id;
        }
        public UserTimeData GetUserTimeData(int tbl_user_ID, List<tbl_User> tbl_User, List<tbl_VentanaAtencion> tbl_VentanaAtencion)
        { // this is not EmployeeId but tbl_user id
            UserTimeData utd = new UserTimeData();
            var DiasLaborales = new string[0];
            // info dummy
            DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray();
            DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray();
            DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray();
            DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray();
            DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray();
            utd.DiasLaborales = DiasLaborales;
            utd.Inicio_Jrnd = new TimeSpan(9, 00, 00);
            utd.Final_Jrnd = new TimeSpan(17, 00, 00);

            // if user 0  then return dummy, 9-5pm, 5 day weekk
            // if user -1 then return full time, 24/7
            if (tbl_user_ID == 0)
            {
                return utd;
            }
            else if (tbl_user_ID == -1)
            {
                DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray();
                DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray();
                utd.DiasLaborales = DiasLaborales;
                utd.Inicio_Jrnd = new TimeSpan(0, 00, 00);
                utd.Final_Jrnd = new TimeSpan(23, 59, 00);

                return utd;
            }

            // ----------------------------------- Actually get the info
            var user = tbl_User.FirstOrDefault(t => t.Id == tbl_user_ID);
            if (user == null)
            {
                return utd;
            }
            else
            {

                //empty the info dummy and replace with actual correct data
                var Ini = Convert.ToDateTime(user.HoraInicioATC);
                var Fin = Convert.ToDateTime(user.HoraFinATC);

                utd.Inicio_Jrnd = new TimeSpan(Ini.Hour, Ini.Minute, 00);
                utd.Final_Jrnd = new TimeSpan(Fin.Hour, Fin.Minute, 00);

                var data_Dias = tbl_VentanaAtencion.Where(t => t.EmpleadoID == user.EmpleadoID).FirstOrDefault();

                if (data_Dias != null)
                {
                    DiasLaborales = new string[0]; // Vaciar info dummy
                    if (data_Dias.Lunes) { DiasLaborales = DiasLaborales.Concat(new string[] { "Monday" }).ToArray(); }
                    if (data_Dias.Martes) { DiasLaborales = DiasLaborales.Concat(new string[] { "Tuesday" }).ToArray(); }
                    if (data_Dias.Miercoles) { DiasLaborales = DiasLaborales.Concat(new string[] { "Wednesday" }).ToArray(); }
                    if (data_Dias.Jueves) { DiasLaborales = DiasLaborales.Concat(new string[] { "Thursday" }).ToArray(); }
                    if (data_Dias.Viernes) { DiasLaborales = DiasLaborales.Concat(new string[] { "Friday" }).ToArray(); }
                    if (data_Dias.Sabado) { DiasLaborales = DiasLaborales.Concat(new string[] { "Saturday" }).ToArray(); }
                    if (data_Dias.Domingo) { DiasLaborales = DiasLaborales.Concat(new string[] { "Sunday" }).ToArray(); }
                }
                utd.DiasLaborales = DiasLaborales;

            }

            return utd;
        }
        public int GetTecnicoAsignadoByHistoric(his_Ticket his, List<tbl_User> tbl_User)
        {
            // DOES NOT RETURN EMPLOYEE ID, only tbl_user id
            int id = 0;
            var DataTecnico = new tbl_User();
            if (    his.TecnicoAsignado      != null &&
                    his.TecnicoAsignadoReag  == null &&
                    his.TecnicoAsignadoReag2 == null)
            {
                DataTecnico = tbl_User.Where(x => x.NombreTecnico == his.TecnicoAsignado).FirstOrDefault();
            }
            else if (his.TecnicoAsignado        != null &&
                     his.TecnicoAsignadoReag    != null &&
                     his.TecnicoAsignadoReag2   == null)
            {
                DataTecnico = tbl_User.Where(x => x.NombreTecnico == his.TecnicoAsignadoReag).FirstOrDefault();
            }
            else if (his.TecnicoAsignado      != null &&
                     his.TecnicoAsignadoReag  != null &&
                     his.TecnicoAsignadoReag2 != null)
            {
                DataTecnico = tbl_User.Where(x => x.NombreTecnico == his.TecnicoAsignadoReag2).FirstOrDefault();
            }

            if (DataTecnico != null) id = DataTecnico.Id;
            return id;
        }
    }
}