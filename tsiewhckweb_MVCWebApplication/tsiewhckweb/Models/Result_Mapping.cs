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
    
    internal partial class Result_Mapping : EntityTypeConfiguration<Result>
    {
        public Result_Mapping()
        {                        
              this.HasKey(t => t.ResultID);        
              this.ToTable("Result");
              this.Property(t => t.ResultID).HasColumnName("ResultID");
              this.Property(t => t.Status).HasColumnName("Status");
              this.Property(t => t.Comment).HasColumnName("Comment").IsRequired().HasMaxLength(1000);
              this.Property(t => t.PackageID).HasColumnName("PackageID");
              this.Property(t => t.BugID).HasColumnName("BugID");
              this.Property(t => t.TestCaseID).HasColumnName("TestCaseID");
              this.HasRequired(t => t.Bug).WithMany(t => t.Results).HasForeignKey(d => d.BugID);
              this.HasRequired(t => t.Package).WithMany(t => t.Results).HasForeignKey(d => d.PackageID);
              this.HasRequired(t => t.TestCase).WithMany(t => t.Results).HasForeignKey(d => d.TestCaseID);
         }
    }
}