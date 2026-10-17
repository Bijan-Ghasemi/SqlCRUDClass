using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repository.Domain;

namespace TestWebAPI.Domain.Model
{
    [TableInfo("tblCustomer2", "ID", false)]
    public class Customer2
    {
        public string ID { get; set; }
        public string FirstName { get; set; }
        [FieldInfo(FieldName = "CustomerLastName")]
        public string LastName { get; set; }

        public string Description { get; set; }


        [FieldInfo(Ignore = true)]
        public List<Orders> Orders { get; set; }
    }
}
