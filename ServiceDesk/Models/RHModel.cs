using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Mvc;


namespace ServiceDesk.Models
{
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    public partial class RHAdminContext : DbContext
    {
        //public DbSet<vw_DetalleEmpleado> vw_DetalleEmpleado { get; set; }
        public DbSet<cat_Supervisor> cat_Supervisor { get; set; }
        public DbSet<RutasCargaArchivosRh> RutasCargaArchivosRh { get; set; }


        public RHAdminContext()
            : base("name=RhAdmin")
        {
            Database.SetInitializer((IDatabaseInitializer<RHAdminContext>)null);
        }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    public class vw_DetalleEmpleado
    {
        [Key]
        public int NumeroPenta { get; set; }

        public int NumeroEmpleado { get; set; }

        public DateTime? FechaIngresoOperacion { get; set; }

        public DateTime? FechaIngresoNomina { get; set; }

        public int? Permanencia { get; set; }

        public Decimal? SueldoNeto { get; set; }

        public DateTime? FechaBajaNomina { get; set; }

        public DateTime? FechaBajaOperacion { get; set; }

        public string Motivo { get; set; }

        public string NombreCompleto { get; set; }

        public string Nombre { get; set; }

        public string ApellidoPaterno { get; set; }

        public string ApellidoMaterno { get; set; }

        public string Area { get; set; }

        public string Puesto { get; set; }
        public int? PuestoID { get; set; }

        public string Turno { get; set; }

        public decimal HrsJornada { get; set; }

        public string Supervisor { get; set; }

        public string Coordinador { get; set; }

        public string TelefonoLoc { get; set; }

        public string TelefonoCel { get; set; }

        public string TelefonoEmerg { get; set; }

        public string Email { get; set; }

        public string CURP { get; set; }

        public string RFC { get; set; }

        public string NSS { get; set; }

        public string Sexo { get; set; }

        public DateTime FechaNacimiento { get; set; }

        public int? Edad { get; set; }

        public string Calle { get; set; }

        public string NumExterior { get; set; }

        public string NumInterior { get; set; }

        public string Colonia { get; set; }

        public string Estado { get; set; }

        public string Municipio { get; set; }
        public int EstatusID { get; set; }
        public string Estatus { get; set; }

        public int AreaID { get; set; }

        public int SupervisorID { get; set; }

        public DateTime? FechaAntiguedad { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class cat_Supervisor
    {
        [Key]
        public int SupervisorID { get; set; }
        public string Supervisor { get; set; }
        public int EmpleadoID { get; set; }
        public bool Estatus { get; set; }
        public int AreaID { get; set; }
        public bool Ojt { get; set; }
        public int Nivel { get; set; }
        public string Email { get; set; }
        public int Etapa { get; set; }


    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Table("cat_RutasCargaArchivosRh")]
    public class RutasCargaArchivosRh
    {
        [Key]
        public int Id { get; set; }
        public string Descripcion { get; set; }
        public string Ruta { get; set; }
        public bool Activo { get; set; }
    }
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
}