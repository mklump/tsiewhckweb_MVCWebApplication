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
    
    internal partial class Project_Group_Mapping : EntityTypeConfiguration<Project_Group>
    {
        public Project_Group_Mapping()
        {                        
              this.HasKey(t => t.Project_GroupID);        
              this.ToTable("Project_Group");
              this.Property(t => t.Project_GroupID).HasColumnName("Project_GroupID");
              this.Property(t => t.Name).HasColumnName("Name").IsRequired().HasMaxLength(500);
              this.Property(t => t.ConfigNumID).HasColumnName("ConfigNumID");
              this.HasRequired(t => t.Test_Config).WithMany(t => t.Project_Group).HasForeignKey(d => d.ConfigNumID);
         }
    }
}
