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
    
    public partial class TestCase
    {
        public TestCase()
        {
            this.Results = new HashSet<Result>();
        }
    
        public int TestCaseID { get; set; }
        public string Name { get; set; }
        public int ComponentID { get; set; }
        public System.DateTime TimeStamp { get; set; }
    
        public virtual Component Component { get; set; }
        public virtual ICollection<Result> Results { get; set; }
    }
}