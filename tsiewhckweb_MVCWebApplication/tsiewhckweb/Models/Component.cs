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
    
    public partial class Component
    {
        public Component()
        {
            this.TestCases = new HashSet<TestCase>();
        }
    
        public int ComponentID { get; set; }
        public string Name { get; set; }
        public int Project_GroupID { get; set; }
    
        public virtual Project_Group Project_Group { get; set; }
        public virtual ICollection<TestCase> TestCases { get; set; }
    }
}
