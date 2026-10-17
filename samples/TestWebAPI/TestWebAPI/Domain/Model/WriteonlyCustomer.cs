using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repository.Domain;

namespace TestWebAPI.Domain.Model
{
    [TableInfo("[dbo].[tblCustomer]", "ID", true,enmTableType.Writeonly)]
    public class WriteonlyCustomer
    {
        public Int32 ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [FieldInfo(Ignore = true)]
        public List<Orders> Orders { get; set; }
    }
}
