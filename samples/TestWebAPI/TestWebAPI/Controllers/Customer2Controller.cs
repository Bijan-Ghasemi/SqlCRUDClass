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
    public class Customer2Controller : ControllerBase
    {
        IDatabase Database;
        public Customer2Controller(IDatabase database, IConfiguration Config) 
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
        public string Insert([FromBody] Customer2 body)
        {
            Customer2 o = new Customer2();
            try
            {
                Database.TestWebAPI.BeginTransaction();
                //this code generate exception.
                o.FirstName = body.FirstName;
                o.LastName = body.LastName;
                Database.TestWebAPI.Save(o);
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("FirstName", body.FirstName);
                //you can not use writeonly object in execute procedure
                var result = Database.TestWebAPI.Execute_StoredProcedure<Customer, Customer, Customer2, ReadonlyCustomer>("SP_GetCustomerDetail", parameters);
                Database.TestWebAPI.CommitTransaction();

            }
            catch (Exception ex)
            {

                Database.TestWebAPI.RollbackTransaction();
            }

            return o.ID.ToString();
        }
        [HttpPost("InsertBatch_100000")]
        [Log]
        /// <summary>
        /// This Table is identity 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>

        public string InsertBatch_100000([FromBody] CustomerView body)
        {
            List<Customer2> lst = new List<Customer2>();
            for (int i = 0; i < 10000; i++)
            {
                Customer2 o2 = new Customer2();
                o2.ID = Guid.NewGuid().ToString();
                o2.FirstName = body.FirstName;
                o2.LastName = body.LastName;
                lst.Add(o2);
            }
            Database.TestWebAPI.SaveList(lst, "ID");
            return "1";
        }

        [HttpPost("InsertBatch_100000_In_transaction")]
        [Log]
        /// <summary>
        /// This Table is identity 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>

        public string InsertBatch_100000_In_transaction([FromBody] CustomerView body)
        {
            List<Customer2> lst = new List<Customer2>();
            for (int i = 0; i < 10000; i++)
            {
                Customer2 o2 = new Customer2();
                o2.ID = Guid.NewGuid().ToString();
                o2.FirstName = body.FirstName;
                o2.LastName = body.LastName;
                lst.Add(o2);
            }
            try
            {
                Database.TestWebAPI.BeginTransaction();
                Database.TestWebAPI.SaveList(lst, "ID");
                Database.TestWebAPI.CommitTransaction();
            }
            catch (Exception)
            {

                Database.TestWebAPI.RollbackTransaction();
            }
            return "1";
        }
        [HttpPost("Update")]
        public string Update(string key, [FromBody] Customer2 body)
        {
            if (key=="0")
            {
                throw new Exception("you must be used key that not equal zero on update. this cause insert new row.");
            }
            //this code generate exception.
            try
            {
                Database.TestWebAPI.BeginTransaction();
                Customer2 o = new Customer2();
                o.ID = key;
                o.FirstName = body.FirstName;
                o.LastName = body.LastName;
                Database.TestWebAPI.Save(o);
                Database.TestWebAPI.CommitTransaction();
                return o.ID.ToString();

            }
            catch (Exception ex)
            {
                Database.TestWebAPI.RollbackTransaction();
                throw ex;
            }
        }

        [HttpPost("Delete")]
        public bool Delete(string key)
        {
            //this code generate exception.
            try
            {
                var param = new Dictionary<string, string>();
                param.Add("key", key);
                Database.TestWebAPI.BeginTransaction();
                Database.TestWebAPI.DeleteList<Customer2>("ID = @key", param);
                Database.TestWebAPI.CommitTransaction();
                return true;
            }
            catch (Exception ex)
            {

                Database.TestWebAPI.RollbackTransaction();
                return false;

            }
            
        }

        [HttpGet("GetByID")]
        public Customer2 GetFromCustomer2Table(string ID)
        {
            var Customer2 = Database.TestWebAPI.GetByID<Customer2>(ID, false);
            return Customer2;
        }

        [HttpGet("FindFirst")]
        public Customer2 FindFirst(string FirstName)
        {
            var param = new Dictionary<string, string>();
            param.Add("FirstName", FirstName);
            var Customer2 = Database.TestWebAPI.FindFirst<Customer2>("FirstName=@FirstName", "", false, param);
            return Customer2;
        }

        [HttpGet("ParametericFind")]
        public List<Customer2> ParametericFind(string FirstName) 
        {
            var param = new Dictionary<string, string>();
            param.Add("FirstName", FirstName);
            List<Customer2> Customer2s = Database.TestWebAPI.Find<Customer2>("FirstName=@FirstName", "", false, param).ToList();
            return Customer2s;
               
        }

        [HttpGet("SimpleFind")]
        public List<Customer2> SimpleFind(string FirstName)
        {
            List<Customer2> Customer2s = Database.TestWebAPI.Find<Customer2>("FirstName='" + FirstName + "'", "", false, null).ToList();
            return Customer2s;

        }

        [HttpGet("FindWithCustomFields")]
        public List<Customer2> FindWithCustomFields(string FirstName)
        {
            //in this function we want to select only ID And LastName
            var param = new Dictionary<string, string>();
            param.Add("FirstName", FirstName);
            List<Customer2> Customers = Database.TestWebAPI.Find<Customer2>("FirstName=@FirstName", "", false, param, "ID,LastName").ToList();
            return Customers;

        }
    }
}
