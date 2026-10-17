using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repository.Domain;

namespace TestWebAPI.Domain.Model
{
    [TableInfo("tblCustomer3", "ID", true)]
    public class Customer3
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
    }
}
