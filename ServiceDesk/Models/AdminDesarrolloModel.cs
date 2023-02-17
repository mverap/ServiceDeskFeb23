using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace ServiceDesk.Models
{
    public class AdminDesarrolloContext : DbContext
    {
        public AdminDesarrolloContext()
            : base("name=MemberShipProvider")
        {
            this.Configuration.LazyLoadingEnabled = true;
            Database.SetInitializer((IDatabaseInitializer<AdminDesarrolloContext>)null);
        }

        public class SubMenus
        {
            public int SubMenuId { get; set; }
            public string SubMenu { get; set; }
            public bool Checked { get; set; }
        }
    }
}