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
    
    public partial class Machine_Config
    {
        public Machine_Config()
        {
            this.Test_Config = new HashSet<Test_Config>();
        }
    
        public int MachineConfigID { get; set; }
        public string HW_Version { get; set; }
        public string WHCK_Version { get; set; }
        public string Windows_Build_Num { get; set; }
    
        public virtual ICollection<Test_Config> Test_Config { get; set; }
    }
}