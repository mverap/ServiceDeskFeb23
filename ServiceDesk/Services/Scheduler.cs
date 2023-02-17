using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuartzScheduler.Services
{
    public class Scheduler
    {
        public static void Start()
        {
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();

            IJobDetail TareasProgramadas = JobBuilder.Create<TareasProgramadas>().Build();
            IJobDetail TareasCC = JobBuilder.Create<TareaCC>().Build();

            ITrigger EveryHour = TriggerBuilder.Create()
                .WithDailyTimeIntervalSchedule
                  (s =>
                    s.WithIntervalInHours(1)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(0, 0))
                  )
                .Build();

            ITrigger Every60secs = TriggerBuilder.Create() // Este trigger es usado solo para debugging
                .WithDailyTimeIntervalSchedule
                  (s =>
                    s.WithIntervalInSeconds(60)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(0, 0))
                  )
                .Build();

            scheduler.ScheduleJob(TareasProgramadas, Every60secs); // cambiar trigger, el que está es solo para debugging
            scheduler.ScheduleJob(TareasCC, EveryHour);
        }
    }
}