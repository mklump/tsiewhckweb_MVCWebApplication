//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace tsiewhckweb.Models
{
    #pragma warning disable 1573
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration;
    using System.Data.Entity.Infrastructure;
    
    internal partial class Machine_Config_Mapping : EntityTypeConfiguration<Machine_Config>
    {
        public Machine_Config_Mapping()
        {                        
              this.HasKey(t => t.MachineConfigID);        
              this.ToTable("Machine_Config");
              this.Property(t => t.MachineConfigID).HasColumnName("MachineConfigID");
              this.Property(t => t.HW_Version).HasColumnName("HW_Version").IsRequired();
              this.Property(t => t.WHCK_Version).HasColumnName("WHCK_Version").IsRequired();
              this.Property(t => t.Windows_Build_Num).HasColumnName("Windows_Build_Num").IsRequired();
         }
    }
}