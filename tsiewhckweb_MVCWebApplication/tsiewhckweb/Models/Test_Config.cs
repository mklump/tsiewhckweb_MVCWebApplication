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
    
    public partial class Test_Config
    {
        public Test_Config()
        {
            this.Packages = new HashSet<Package>();
            this.Project_Group = new HashSet<Project_Group>();
        }
    
        public int ConfigNumID { get; set; }
        public int MachineConfigID { get; set; }
        public int DriverConfigID { get; set; }
    
        public virtual Driver_Config Driver_Config { get; set; }
        public virtual Machine_Config Machine_Config { get; set; }
        public virtual ICollection<Package> Packages { get; set; }
        public virtual ICollection<Project_Group> Project_Group { get; set; }
    }
}