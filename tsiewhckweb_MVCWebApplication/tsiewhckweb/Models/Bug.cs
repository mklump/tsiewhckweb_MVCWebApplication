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
    
    public partial class Bug
    {
        public Bug()
        {
            this.Results = new HashSet<Result>();
        }
    
        public int BugID { get; set; }
        public string HSD { get; set; }
        public string CSP { get; set; }
        public string WINQUAL { get; set; }
        public string MANAGEPRO { get; set; }
    
        public virtual ICollection<Result> Results { get; set; }
    }
}
