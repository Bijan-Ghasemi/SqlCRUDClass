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
    public class WriteonlyCustomerController : ControllerBase
    {
        IDatabase Database;
        public WriteonlyCustomerController(IDatabase database, IConfiguration Config)
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
            WriteonlyCustomer o = new WriteonlyCustomer();
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
                WriteonlyCustomer o = new WriteonlyCustomer();
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
                WriteonlyCustomer o = new WriteonlyCustomer();
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
        public CustomerView GetFromWriteonlyCustomerTable(string ID)
        {
            var WriteonlyCustomer = Database.TestWebAPI.GetByID<WriteonlyCustomer>(ID, false);
            CustomerView result = new CustomerView();
            if (WriteonlyCustomer == null)
            {
                return null;
            }
            result.FirstName = WriteonlyCustomer.FirstName;
            result.LastName = WriteonlyCustomer.LastName;
            return result;
        }



        [HttpGet("ParametericFind")]
        public List<WriteonlyCustomer> ParametericFind(string FirstName) 
        {
            var param = new Dictionary<string, string>();
            param.Add("FirstName", FirstName);
            List<WriteonlyCustomer> WriteonlyCustomers = Database.TestWebAPI.Find<WriteonlyCustomer>("FirstName=@FirstName", "", false, param).ToList();
            return WriteonlyCustomers;
               
        }

        [HttpGet("SimpleFind")]
        public List<WriteonlyCustomer> SimpleFind(string FirstName)
        {
            List<WriteonlyCustomer> WriteonlyCustomers = Database.TestWebAPI.Find<WriteonlyCustomer>("FirstName='" + FirstName + "'", "", false, null).ToList();
            return WriteonlyCustomers;

        }

        [HttpGet("FindWithCustomFields")]
        public List<WriteonlyCustomer> FindWithCustomFields(string FirstName)
        {
            //in this function we want to select only ID And LastName
            var param = new Dictionary<string, string>();
            param.Add("FirstName", FirstName);
            List<WriteonlyCustomer> Customers = Database.TestWebAPI.Find<WriteonlyCustomer>("FirstName=@FirstName", "", false, param, "ID,LastName").ToList();
            return Customers;

        }
    }
}
