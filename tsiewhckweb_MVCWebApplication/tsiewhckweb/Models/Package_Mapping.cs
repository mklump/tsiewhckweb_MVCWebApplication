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
    
    internal partial class Package_Mapping : EntityTypeConfiguration<Package>
    {
        public Package_Mapping()
        {                        
              this.HasKey(t => t.PackageID);        
              this.ToTable("Package");
              this.Property(t => t.PackageID).HasColumnName("PackageID");
              this.Property(t => t.ConfigNumID).HasColumnName("ConfigNumID");
              this.Property(t => t.UserID).HasColumnName("UserID");
              this.Property(t => t.Checksum).HasColumnName("Checksum").IsRequired();
              this.Property(t => t.Date_Uploaded).HasColumnName("Date_Uploaded");
              this.Property(t => t.FileName).HasColumnName("FileName").IsRequired();
              this.Property(t => t.Path).HasColumnName("Path").IsRequired();
              this.Property(t => t.TestResult_Summary).HasColumnName("TestResult_Summary").IsRequired();
              this.HasRequired(t => t.Login).WithMany(t => t.Packages).HasForeignKey(d => d.UserID);
              this.HasRequired(t => t.Test_Config).WithMany(t => t.Packages).HasForeignKey(d => d.ConfigNumID);
         }
    }
}