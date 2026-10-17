using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Repository.Domain;

namespace Repository
{
    internal class Repository : IRepository
    {
        Dictionary<string, object> TableFactories = new Dictionary<string, object>();
        internal IDbConnection Connection;
        internal IDbTransaction Transaction;
        private string _ConnectionString;
        private int _TransactionCount;
        private readonly bool _ownsConnection;
        private Thread _transactionOwner;


        public int TransactionCount 
        {
            get { return _TransactionCount; }
        }

        public Func<string, string> __KeyGenerator { get ; set ; }

        public Repository(string connectionString)
        {
            if (connectionString == "" || connectionString == null)
            {
                throw new Exception("Connection string is empty.");
            }
            _ConnectionString = connectionString;
            _ownsConnection = true;
        }

        public Repository(IDbConnection dbConnection)
        {
            if (dbConnection == null)
            {
                throw new Exception("Connection is null.");
            }
            this.Connection = dbConnection;
            _ownsConnection = false;
        }

        internal void OpenConnection()
        {
                if (Connection == null)
                {
                    Connection = new SqlConnection(_ConnectionString);
                }

                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }

            
        }
        internal void CloseConnection()
        {
                if (_ownsConnection && Connection != null && Connection.State == ConnectionState.Open)
                {
                    Connection.Close();
                }


        }
        public bool BeginTransaction()
        {
            Monitor.Enter(this);
            try
            {
                if (Transaction is null)
                {
                    OpenConnection();
                    Transaction = Connection.BeginTransaction();
                    _transactionOwner = Thread.CurrentThread;
                }
                _TransactionCount++;
                return true;
            }
            catch
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }
                _transactionOwner = null;
                Monitor.Exit(this);
                throw;
            }
        }

        public bool CommitTransaction()
        {
            if (_transactionOwner != Thread.CurrentThread)
            {
                throw new InvalidOperationException("The current thread does not own the transaction.");
            }
            try
            {
                if (_TransactionCount == 0 || Transaction is null)
                {
                    throw new InvalidOperationException("You must call BeginTransaction first.");
                }
                _TransactionCount--;
                if (_TransactionCount == 0)
                {
                    try
                    {
                        Transaction.Commit();
                    }
                    finally
                    {
                        Transaction.Dispose();
                        Transaction = null;
                        _transactionOwner = null;
                        CloseConnection();
                    }
                }
                return true;
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        public bool RollbackTransaction()
        {
            if (_transactionOwner != Thread.CurrentThread)
            {
                throw new InvalidOperationException("The current thread does not own the transaction.");
            }
            if (_TransactionCount == 0 || Transaction is null)
            {
                throw new InvalidOperationException("You must call BeginTransaction first.");
            }
            var lockCount = _TransactionCount;
            try
            {
                Transaction.Rollback();
            }
            finally
            {
                try
                {
                    Transaction.Dispose();
                }
                finally
                {
                    Transaction = null;
                    _transactionOwner = null;
                    _TransactionCount = 0;
                    CloseConnection();
                    for (var i = 0; i < lockCount; i++)
                    {
                        Monitor.Exit(this);
                    }
                }
            }
            return true;
        }

        private GeneralFactory<T> GetGeneralFactory<T>()
        {
            object oGeneralFactory;
            TableFactories.TryGetValue(typeof(T).Name, out oGeneralFactory);
            if (oGeneralFactory is null)
            {
                oGeneralFactory = new GeneralFactory<T>(this);
                TableFactories.Add(typeof(T).Name, oGeneralFactory);
            }
            return (GeneralFactory<T>)oGeneralFactory;
        }

        private TResult ExecuteWithConnectionCleanup<TResult>(Func<TResult> operation)
        {
            try
            {
                return operation();
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }
            }
        }

        public bool Save<T>(T o, int commandTimeout = 30)
        {
            lock (this)
            {
                var oGeneralFactory = GetGeneralFactory<T>();
                try
                {
                    return oGeneralFactory.Save(ref o,"",null, commandTimeout );
                }
                finally
                {
                    if (Transaction is null)
                    {
                        CloseConnection();
                    }
                }
            }


        }
        public bool Delete<T>(T o, int commandTimeout = 30)
        {
            lock (this)
            {
                var oGeneralFactory = GetGeneralFactory<T>();
                return ExecuteWithConnectionCleanup(() => oGeneralFactory.Delete(o, commandTimeout));

            }
        }

        public T GetByID<T>(string ID, bool withLock, int commandTimeout = 30)
        {
            lock (this)
            {
                var oGeneralFactory = GetGeneralFactory<T>();
                return ExecuteWithConnectionCleanup(() => oGeneralFactory.GetByID(ID, withLock, commandTimeout));

            }
        }

        public List<T> GetAll<T>( int commandTimeout = 30)
        {
            lock (this)
            {
                var oGeneralFactory = GetGeneralFactory<T>();
                return ExecuteWithConnectionCleanup(() => oGeneralFactory.GetAll(commandTimeout));

            }
        }

        public List<T> Execute_StoredProcedure<T>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            return (List<T>)Execute_StoredProcedure<T, object, object, object, object, object, object>(ProcedureName, Parameters, commandTimeout).FirstOrDefault();
        }

        public List<object> Execute_StoredProcedure<TFirst, TSecond>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            return Execute_StoredProcedure<TFirst, TSecond, object, object, object, object, object>(ProcedureName, Parameters, commandTimeout).Take(2).ToList();
        }

        public List<object> Execute_StoredProcedure<TFirst, TSecond, TThird>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            return Execute_StoredProcedure<TFirst, TSecond, TThird, object, object, object, object>(ProcedureName, Parameters, commandTimeout).Take(3).ToList();
        }

        public List<object> Execute_StoredProcedure<TFirst, TSecond, TThird, TFour>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            return Execute_StoredProcedure<TFirst, TSecond, TThird, TFour, object, object, object>(ProcedureName, Parameters, commandTimeout).Take(4).ToList();
        }

        public List<object> Execute_StoredProcedure<TFirst, TSecond, TThird, TFour, TFive>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            return Execute_StoredProcedure<TFirst, TSecond, TThird, TFour, TFive, object, object>(ProcedureName, Parameters, commandTimeout).Take(5).ToList();
        }

        public List<object> Execute_StoredProcedure<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            return Execute_StoredProcedure<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, object>(ProcedureName, Parameters, commandTimeout).Take(6).ToList();
        }

        public List<object> Execute_StoredProcedure<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            return Execute_StoredProcedure<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, object>(ProcedureName, Parameters, commandTimeout).Take(7).ToList();
        }

        public List<object> Execute_StoredProcedure<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            return Execute_StoredProcedure<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, object>(ProcedureName, Parameters, commandTimeout).Take(8).ToList();
        }

        public List<object> Execute_StoredProcedure<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            return Execute_StoredProcedure<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, object>(ProcedureName, Parameters, commandTimeout).Take(9).ToList();
        }

        public List<object> Execute_StoredProcedure<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth>(string ProcedureName, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            lock (this)
            {
                TableInfoAttribute TFirst_TableInfo = GetGeneralFactory<TFirst>().TableInfo;
                if (TFirst_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TFirst).Name);
                }

                TableInfoAttribute TSecond_TableInfo = GetGeneralFactory<TSecond>().TableInfo;
                if (TSecond_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TSecond).Name);
                }

                TableInfoAttribute TThird_TableInfo = GetGeneralFactory<TThird>().TableInfo;
                if (TThird_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TThird).Name);
                }

                TableInfoAttribute TFourth_TableInfo = GetGeneralFactory<TFourth>().TableInfo;
                if (TFourth_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TFourth).Name);
                }
                TableInfoAttribute TFifth_TableInfo = GetGeneralFactory<TFifth>().TableInfo;
                if (TFifth_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TFifth).Name);
                }
                TableInfoAttribute TSixth_TableInfo = GetGeneralFactory<TSixth>().TableInfo;
                if (TSixth_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TSixth).Name);
                }
                TableInfoAttribute TSeventh_TableInfo = GetGeneralFactory<TSeventh>().TableInfo;
                if (TSeventh_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TSeventh).Name);
                }
                TableInfoAttribute TEighth_TableInfo = GetGeneralFactory<TEighth>().TableInfo;
                if (TEighth_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TEighth).Name);
                }
                TableInfoAttribute TNinth_TableInfo = GetGeneralFactory<TNinth>().TableInfo;
                if (TNinth_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TNinth).Name);
                }
                TableInfoAttribute TTenth_TableInfo = GetGeneralFactory<TTenth>().TableInfo;
                if (TTenth_TableInfo.TableType == enmTableType.Writeonly)
                {
                    throw new Exception("Class is Writeonly.You can not perform (Execute_StoredProcedure) in this Class. ClassName:" + typeof(TTenth).Name);
                }

                var dbArgs = new DynamicParameters();
                if (Parameters != null)
                {
                    foreach (var pair in Parameters) dbArgs.Add(pair.Key, pair.Value);
                }

                List<object> Result = new List<object>();
                SqlMapper.GridReader grid;
                    bool InTransaction;
                    InTransaction = false;
                    if (Transaction != null)
                    {
                        InTransaction = true;
                    }
                    else
                    {
                        OpenConnection();
                        InTransaction = false;
                    }
                    grid = this.Connection.QueryMultiple(ProcedureName, dbArgs, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout, transaction: this.Transaction);
                    Result.Add(grid.Read<TFirst>().ToList<TFirst>());
                    if (grid.IsConsumed == false) { Result.Add(grid.Read<TSecond>().ToList()); } else { Result.Add(new List<TSecond>()); }
                    if (grid.IsConsumed == false) { Result.Add(grid.Read<TThird>().ToList()); } else { Result.Add(new List<TThird>()); }
                    if (grid.IsConsumed == false) { Result.Add(grid.Read<TFourth>().ToList()); } else { Result.Add(new List<TFourth>()); }
                    if (grid.IsConsumed == false) { Result.Add(grid.Read<TFifth>().ToList()); } else { Result.Add(new List<TFifth>()); }
                    if (grid.IsConsumed == false) { Result.Add(grid.Read<TSixth>().ToList()); } else { Result.Add(new List<TSixth>()); }
                    if (grid.IsConsumed == false) { Result.Add(grid.Read<TSeventh>().ToList()); } else { Result.Add(new List<TSeventh>()); }
                    if (grid.IsConsumed == false) { Result.Add(grid.Read<TEighth>().ToList()); } else { Result.Add(new List<TEighth>()); }
                    if (grid.IsConsumed == false) { Result.Add(grid.Read<TNinth>().ToList()); } else { Result.Add(new List<TNinth>()); }
                    if (grid.IsConsumed == false) { Result.Add(grid.Read<TTenth>().ToList()); } else { Result.Add(new List<TTenth>()); }
                if (InTransaction == false)
                {
                    CloseConnection();
                }
                return Result;

            }

        }

        public List<T> Find<T>(string Filter, string orderBy, bool withLock, Dictionary<string, string> Parameters, string FieldNames = "", int commandTimeout = 30)
        {
            lock (this)
            {
                var oGeneralFactory = GetGeneralFactory<T>();
                return ExecuteWithConnectionCleanup(() => oGeneralFactory.Find(Filter, orderBy, withLock, Parameters, FieldNames, commandTimeout));

            }
        }

        public T FindFirst<T>(string Filter, string orderBy, bool withLock, Dictionary<string, string> Parameters, string FieldNames = "", int commandTimeout = 30)
        {
            lock (this)
            {
                var oGeneralFactory = GetGeneralFactory<T>();
                return ExecuteWithConnectionCleanup(() => oGeneralFactory.FindFirst(Filter, orderBy, withLock, Parameters, FieldNames, commandTimeout));

            }
        }

        public bool DeleteList<T>(string Filter, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            lock (this)
            {
                var oGeneralFactory = GetGeneralFactory<T>();
                return ExecuteWithConnectionCleanup(() => oGeneralFactory.DeleteList(Filter, Parameters, commandTimeout));

            }

        }

        public bool Save<T>(T o, string Filter, Dictionary<string, string> Parameters=null, int commandTimeout = 30)
        {
            lock (this)
            {
                var oGeneralFactory = GetGeneralFactory<T>();
                try
                {
                    return oGeneralFactory.Save(ref o, Filter, Parameters, commandTimeout);
                }
                finally
                {
                    if (Transaction is null)
                    {
                        CloseConnection();
                    }
                }
            }

        }

        public bool SaveList<T>(List<T> list, string basePropertyName, int commandTimeout = 30)
        {
            lock (this)
            {
                var oGeneralFactory = GetGeneralFactory<T>();
                return ExecuteWithConnectionCleanup(() => oGeneralFactory.SaveList(list, basePropertyName, commandTimeout));

            }
        }
    }

}
