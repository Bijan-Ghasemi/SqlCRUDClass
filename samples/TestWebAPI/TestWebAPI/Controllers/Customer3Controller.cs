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
    public class Customer3Controller : ControllerBase
    {
        IDatabase Database;
       public Customer3Controller(IDatabase database, IConfiguration Config) 
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
        public string Insert([FromBody] Customer3 body)
        {
            //this code generate exception.
            Customer3 o = new Customer3();
            o.Name = body.Name;
            Database.TestWebAPI.Save(o);
            return o.ID.ToString();
        }

        [HttpPost("Update")]
        public string Update(string key, [FromBody] Customer3 body)
        {
            if (key=="0")
            {
                throw new Exception("you must be used key that not equal zero on update. this cause insert new row.");
            }
            //this code generate exception.
            try
            {
                Database.TestWebAPI.BeginTransaction();
                Customer3 o = new Customer3();
                o.ID = Guid.Parse(key);
                o.Name = body.Name;
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
                Database.TestWebAPI.DeleteList<Customer3>("ID = @key", param);
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
        public Customer3 GetFromCustomer3Table(string ID)
        {
            var Customer3 = Database.TestWebAPI.GetByID<Customer3>(ID, false);
            return Customer3;
        }



        [HttpGet("ParametericFind")]
        public List<Customer3> ParametericFind(string Name) 
        {
            var param = new Dictionary<string, string>();
            param.Add("Name", Name);
            List<Customer3> Customer3s = Database.TestWebAPI.Find<Customer3>("Name=@Name", "", false, param).ToList();
            return Customer3s;
               
        }

        [HttpGet("SimpleFind")]
        public List<Customer3> SimpleFind(string Name)
        {
            List<Customer3> Customer3s = Database.TestWebAPI.Find<Customer3>("Name='" + Name + "'", "", false, null).ToList();
            return Customer3s;

        }

        [HttpGet("FindWithCustomFields")]
        public List<Customer3> FindWithCustomFields(string Name)
        {
            //in this function we want to select only ID And LastName
            var param = new Dictionary<string, string>();
            param.Add("Name", Name);
            List<Customer3> Customers = Database.TestWebAPI.Find<Customer3>("Name=@Name", "", false, param, "ID,Name").ToList();
            return Customers;

        }
    }
}
