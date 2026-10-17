using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TestWebAPI.Domain.Enum;
using TestWebAPI.Domain.Model;
using TestWebAPI.Domain.ViewModel;
using TestWebAPI.Log;

namespace TestWebAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReadonlyCustomerController : ControllerBase
    {
        IDatabase Database;
        public ReadonlyCustomerController(IDatabase database, IConfiguration Config)
        {
            Database = database;
            string ConnectionString;
            string ConnectionStringLog;
            ConnectionString = Config.GetValue<String>("ConnectionString");
            ConnectionStringLog = Config.GetValue<String>("ConnectionStringLog");
            Database.Initialize(ConnectionString, ConnectionStringLog);

        }
        [HttpPost("Insert")]
        [Log]
        /// <summary>
        /// we are create __KeyGenerator Stored Procedure 
        /// Please overwrite your own key generation policy in this procedure in sql server.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public string Insert([FromBody] CustomerView body)
        {
            ReadonlyCustomer o = new ReadonlyCustomer();
            o.FirstName = body.FirstName;
            o.LastName = body.LastName;
            Database.TestWebAPI.Save(o);
            return o.ID.ToString();
        }

        [HttpPost("Update")]
        public string Update(int key, [FromBody] CustomerView body)
        {
            if (key == 0)
            {
                throw new Exception("you must be used key that not equal zero on update. this cause insert new row.");
            }
            try
            {
                Database.TestWebAPI.BeginTransaction();
                ReadonlyCustomer o = new ReadonlyCustomer();
                o.ID = key;
                o.FirstName = body.FirstName;
                o.LastName = body.LastName;
                Database.TestWebAPI.Save(o);
                Database.TestWebAPI.CommitTransaction();
                return o.ID.ToString();

            }
            catch (Exception)
            {
                Database.TestWebAPI.RollbackTransaction();

                throw;
            }
        }

        [HttpPost("Delete")]
        public bool Delete(int key)
        {
            try
            {
                Database.TestWebAPI.BeginTransaction();
                ReadonlyCustomer o = new ReadonlyCustomer();
                o.ID = key;
                Database.TestWebAPI.Delete(o);
                Database.TestWebAPI.CommitTransaction();
                return true;
            }
            catch (Exception)
            {

                Database.TestWebAPI.RollbackTransaction();
                return false;

            }
            
        }

        [HttpGet("GetByID")]
        public CustomerView GetFromReadonlyCustomerTable(string ID)
        {
            var ReadonlyCustomer = Database.TestWebAPI.GetByID<ReadonlyCustomer>(ID, false);
            CustomerView result = new CustomerView();
            if (ReadonlyCustomer == null)
            {
                return null;
            }
            result.FirstName = ReadonlyCustomer.FirstName;
            result.LastName = ReadonlyCustomer.LastName;
            return result;
        }



        [HttpGet("ParametericFind")]
        public List<ReadonlyCustomer> ParametericFind(string FirstName) 
        {
            var param = new Dictionary<string, string>();
            param.Add("FirstName", FirstName);
            List<ReadonlyCustomer> ReadonlyCustomers = Database.TestWebAPI.Find<ReadonlyCustomer>("FirstName=@FirstName", "", false, param).ToList();
            return ReadonlyCustomers;
               
        }

        [HttpGet("SimpleFind")]
        public List<ReadonlyCustomer> SimpleFind(string FirstName)
        {
            List<ReadonlyCustomer> ReadonlyCustomers = Database.TestWebAPI.Find<ReadonlyCustomer>("FirstName='" + FirstName + "'", "", false, null).ToList();
            return ReadonlyCustomers;

        }

        [HttpGet("FindWithCustomFields")]
        public List<ReadonlyCustomer> FindWithCustomFields(string FirstName)
        {
            //in this function we want to select only ID And LastName
            var param = new Dictionary<string, string>();
            param.Add("FirstName", FirstName);
            List<ReadonlyCustomer> Customers = Database.TestWebAPI.Find<ReadonlyCustomer>("FirstName=@FirstName", "", false, param, "ID,LastName").ToList();
            return Customers;

        }
    }
}
