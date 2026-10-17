using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Repository.Domain;

namespace Repository
{
     class GeneralFactory<T>
    {
        private IKeyGenerator _KeyGeneratory;
        private string _tableName;
        private string _keycolumnname;
        private bool _keyIsIdentity;
        private enmTableType _tableType;

        private Dictionary<string, string> _StaticValues;
        private Dictionary<string, string> _columnNames;
        private string _SelectStatment;
        private string _InsertStatment;
        private string _DeleteStatment;
        private Repository _Repository;
        internal TableInfoAttribute TableInfo{ get; set; }
        internal GeneralFactory(Repository repository)
        {
            Dictionary<string,string> temp = new Dictionary<string, string>();
            TableInfo = GetTableInfo();
            _tableName = TableInfo.TableName;
            _keycolumnname = TableInfo.keyColumnName;
            ValidateIdentifier(_tableName, "table name", true);
            ValidateIdentifier(_keycolumnname, "key column name", false);
            _keyIsIdentity = TableInfo.KeyIsIdentity;
            _tableType = TableInfo.TableType;
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var item in properties)
            {
                string FieldName = item.Name;
                bool IsIgnore = false;
                foreach (System.Attribute attr in item.GetCustomAttributes(false))
                {
                    if (attr is FieldInfoAttribute)
                    {
                        FieldInfoAttribute FieldInfo = (FieldInfoAttribute)attr;
                        IsIgnore = FieldInfo.Ignore;
                        FieldName = FieldInfo.FieldName;

                    }
                }
                if (IsIgnore == false)
                {
                    if (FieldName == "") { FieldName = item.Name; }
                    ValidateIdentifier(FieldName, "column name", false);
                    temp.Add(item.Name,FieldName);
                }


            }

            _columnNames = temp;
            _StaticValues = null;
            _Repository = repository;
            _KeyGeneratory = new KeyGenerator(_tableName,_Repository);

        }

        private string InsertStatment(bool isguid)
        {
            if (string.IsNullOrEmpty(_InsertStatment))
            {
                _InsertStatment = "INSERT INTO " + _tableName + Environment.NewLine;
                _InsertStatment = _InsertStatment + "(" + Get_ColumnName(CommandType._Insert, "") + ")" + Environment.NewLine;
                if (isguid)
                {
                    _InsertStatment = _InsertStatment + "OUTPUT INSERTED." + _keycolumnname + Environment.NewLine;
                }
                _InsertStatment = _InsertStatment + "VALUES(" + Get_ColumnName(CommandType._Insert, "@") + ")" + Environment.NewLine;
            }
            return _InsertStatment;
        }

        private string UpdateStatment(T o, string filter)
        {
            string _UpdateStatment = null;
            string Columnname;
            string FieldName;
            string Result;
            Result = "";
            foreach (var currentColumnname in _columnNames)
            {
                Columnname = currentColumnname.Value;
                FieldName = currentColumnname.Key;
                if (_keyIsIdentity & ((Columnname ?? "") == (_keycolumnname ?? "")))
                {
                }
                // Do nothing
                else if (_StaticValues is object && _StaticValues.ContainsKey(Columnname) && !string.IsNullOrEmpty(_StaticValues[Columnname]))
                {
                    Result = Result + Columnname + " = " + _StaticValues[Columnname] + ",";
                }
                else
                {
                    var FieldValue = GetPropertyValueByName(o, FieldName);
                    if (FieldValue != null)
                    {
                        Result = Result + Columnname + " = @" + FieldName + ",";
                    }

                }
            }

            if (Result!="")
            {
                Result = Result.Substring(0, (Result.Length - 1));
                _UpdateStatment = "Update " + _tableName + Environment.NewLine;
                _UpdateStatment = _UpdateStatment + "SET " + Result + "" + Environment.NewLine;
                if (filter!=null && filter != "")
                {
                    _UpdateStatment = _UpdateStatment + "Where " + filter;
                }
                else
                {
                    _UpdateStatment = _UpdateStatment + "Where " + _keycolumnname + "=@" + _keycolumnname;
                }
                
            }

            return _UpdateStatment;
        }
        private string UpdateStatmentforbatch( T o, string filter,ref Dictionary<string, string> Parameters)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, string>();
            }

            string _UpdateStatment = null;
            string Columnname;
            string FieldName;
            string Result;
            Result = "";
            foreach (var currentColumnname in _columnNames)
            {
                Columnname = currentColumnname.Value;
                FieldName = currentColumnname.Key;
                if (_keyIsIdentity & ((Columnname ?? "") == (_keycolumnname ?? "")))
                {
                }
                // Do nothing
                else if (_StaticValues is object && _StaticValues.ContainsKey(Columnname) && !string.IsNullOrEmpty(_StaticValues[Columnname]))
                {
                    Result = Result + Columnname + " = " + _StaticValues[Columnname] + ",";
                }
                else
                {
                    var FieldValue = GetPropertyValueByName(o, FieldName);
                    if (FieldValue != null)
                    {
                        var parameter = "o_" + FieldName;
                        Parameters.Add(parameter, Convert.ToString(FieldValue, System.Globalization.CultureInfo.InvariantCulture));
                        Result = Result + Columnname + " = @" + parameter + ",";
                    }

                }
            }

            if (Result != "")
            {
                Result = Result.Substring(0, (Result.Length - 1));
                _UpdateStatment = "Update " + _tableName + Environment.NewLine;
                _UpdateStatment = _UpdateStatment + "SET " + Result + "" + Environment.NewLine;
                _UpdateStatment = _UpdateStatment + "Where " + filter;
            }

            return _UpdateStatment;
        }
        private string DeleteStatment
        {
            get
            {
                if (string.IsNullOrEmpty(_DeleteStatment))
                {
                    _DeleteStatment = "DELETE FROM " + _tableName + " Where " + _keycolumnname + "=@" + _keycolumnname + "";
                }

                return _DeleteStatment;
            }
        }

        private string get_SelectStatment(string NumberofRecords, bool withLock, string FieldNames = "")
        {
            if (NumberofRecords=="")
            {
                _SelectStatment = "Select " + Get_ColumnName(CommandType._Select, "", FieldNames) + Environment.NewLine;
            }
            else
            {
                _SelectStatment = "Select Top " + NumberofRecords + " " + Get_ColumnName(CommandType._Select, "", FieldNames) + Environment.NewLine;
            }

            if (withLock)
            {
                _SelectStatment = _SelectStatment + "From " + _tableName + " WITH(XLOCK) " + Environment.NewLine;
            }
            else
            {
                _SelectStatment = _SelectStatment + "From " + _tableName +  Environment.NewLine;
            }
            return _SelectStatment;
        }
        internal bool Save(ref T o, string filter, Dictionary<string, string> Parameters = null, int commandTimeout = 30)
        {
            if (_tableType==enmTableType.Readonly)
            {
                throw new Exception("Object is Readonly.You can not perform (Save) in this Object.");
            }
            ValidateSqlFragment(filter, "filter");
            bool InTransaction;
            InTransaction = false;
            if (_Repository.Transaction != null)
            {
                InTransaction = true;
            }
            else
            {
                _Repository.OpenConnection();
                InTransaction = false;
            }
            var objectID = GetPropertyValueByName(o, _keycolumnname);
            bool _keyisGuid = false;
            bool _keyIsEmpty = false;
            if (objectID is Guid)
            {
                _keyisGuid = true;
                if ((Guid)objectID == Guid.Empty)
                {
                    _keyIsEmpty = true;
                }
            }
            if (objectID == null)
            {
                _keyIsEmpty = true;
            }
            else if (objectID.ToString() == "")
            {
                _keyIsEmpty = true;
            }
            else if (objectID.ToString() == "0")
            {
                _keyIsEmpty = true;
            }
            if (filter != null && filter != "")
            {
                //batch Update
                var updateStatment = UpdateStatmentforbatch(o, filter,ref Parameters);
                if (updateStatment != "" && updateStatment != null)
                {
                    _Repository.Connection.Execute(updateStatment, Convert_to_anonymouse_object(Parameters), transaction: _Repository.Transaction, commandTimeout: commandTimeout);
                }

            }
            else if (_keyIsEmpty == true)
            {
                //Insert 
                string ID;
                if (_keyisGuid)
                {
                    if (_Repository.__KeyGenerator is null)
                    {
                        ID = Guid.NewGuid().ToString();
                    }
                    else
                    {
                        ID = _Repository.__KeyGenerator(o.GetType().Name);
                    }
                    SetPropertyValueByName(o, _keycolumnname, ID);
                    ID = _Repository.Connection.Query<Guid>(InsertStatment(true) + Environment.NewLine, o, transaction: _Repository.Transaction, commandTimeout: commandTimeout).FirstOrDefault().ToString();
                    SetPropertyValueByName(o, _keycolumnname, ID);

                }
                else if (_keyIsIdentity)
                {
                    ID = _Repository.Connection.Query<string>(InsertStatment(false) + Environment.NewLine + "SELECT SCOPE_IDENTITY() IdentityValue", o, transaction: _Repository.Transaction, commandTimeout: commandTimeout).FirstOrDefault();
                    SetPropertyValueByName(o, _keycolumnname, ID);
                }
                else
                {
                    if (_Repository.__KeyGenerator is null)
                    {
                        ID = _KeyGeneratory.GetNextID();
                    }
                    else
                    {
                        ID = _Repository.__KeyGenerator(o.GetType().Name);
                    }

                    SetPropertyValueByName(o, _keycolumnname, ID);
                    _Repository.Connection.Query<Int64>(InsertStatment(false) + Environment.NewLine, o, transaction: _Repository.Transaction, commandTimeout: commandTimeout).FirstOrDefault();
                }
            }
            else
            {
                //Update
                var updateStatment = UpdateStatment(o, "");
                if (updateStatment != "" && updateStatment != null)
                {
                    _Repository.Connection.Execute(updateStatment, o, transaction: _Repository.Transaction, commandTimeout: commandTimeout);
                }

            }

            if (InTransaction == false)
            {
                _Repository.CloseConnection();
            }
            return true;
        }
        internal T GetByID(string ID, bool withLock, int commandTimeout = 30)
        {
            if (_tableType == enmTableType.Writeonly)
            {
                throw new Exception("Object is Writeonly.You can not perform (GetByID) in this Object.");
            }
            var Parameters = new DynamicParameters();
            Parameters.Add(_keycolumnname, ID);
            bool InTransaction;
            InTransaction = false;
            if (_Repository.Transaction != null)
            {
                InTransaction = true;
            }
            else
            {
                _Repository.OpenConnection();
                InTransaction = false;
            }
            var result= _Repository.Connection.Query<T>(get_SelectStatment("",withLock) + " Where " + _keycolumnname + "= @" + _keycolumnname + "", Parameters, transaction: _Repository.Transaction,commandTimeout: commandTimeout).FirstOrDefault();
            if (InTransaction == false)
            {
                _Repository.CloseConnection();
            }
            return result;

        }

        internal bool Delete(T o, int commandTimeout = 30)
        {
            if (_tableType == enmTableType.Readonly)
            {
                throw new Exception("Object is Readonly.You can not perform (Delete) in this Object.");
            }
            bool InTransaction;
            InTransaction = false;
            if (_Repository.Transaction != null)
            {
                InTransaction = true;
            }
            else
            {
                _Repository.OpenConnection();
                InTransaction = false;
            }
            _Repository.Connection.Execute(DeleteStatment, o, transaction: _Repository.Transaction, commandTimeout: commandTimeout);
            if (InTransaction == false)
            {
                _Repository.CloseConnection();
            }
            return true;
        }

        internal List<T> GetAll(int commandTimeout = 30)
        {
            if (_tableType == enmTableType.Writeonly)
            {
                throw new Exception("Object is Writeonly.You can not perform (GetAll) in this Object.");
            }
            bool InTransaction;
            InTransaction = false;
            if (_Repository.Transaction != null)
            {
                InTransaction = true;
            }
            else
            {
                _Repository.OpenConnection();
                InTransaction = false;
            }
            var result = _Repository.Connection.Query<T>(get_SelectStatment("", false), null, transaction: _Repository.Transaction, commandTimeout: commandTimeout).ToList();
            if (InTransaction == false)
            {
                _Repository.CloseConnection();
            }
            return result;
        }

        internal List<T> Find(string Filter, string orderBy,bool withLock, Dictionary<string, string> Parameters, string FieldNames = "", int commandTimeout = 30)
        {
            if (_tableType == enmTableType.Writeonly)
            {
                throw new Exception("Object is Writeonly.You can not perform (Find) in this Object.");
            }
            ValidateSqlFragment(Filter, "filter");
            ValidateSqlFragment(orderBy, "order by");
            var dbArgs = new DynamicParameters();
            if (Parameters != null)
            {
                foreach (var pair in Parameters) dbArgs.Add(pair.Key, pair.Value);
            }
            bool InTransaction;
            InTransaction = false;
            if (_Repository.Transaction != null)
            {
                InTransaction = true;
            }
            else
            {
                _Repository.OpenConnection();
                InTransaction = false;
            }

            var oResult = new List<T>();
            string sql;
            sql = get_SelectStatment("",withLock, FieldNames);
            if (!string.IsNullOrEmpty(Filter))
            {
                sql = sql + "Where " + Filter + Environment.NewLine;
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                sql = sql + "Order by " + orderBy;
            }
            var result = _Repository.Connection.Query<T>(sql, param: dbArgs, transaction: _Repository.Transaction, commandTimeout: commandTimeout).ToList();
            if (InTransaction == false)
            {
                _Repository.CloseConnection();
            }
            return result;
        }

        internal T FindFirst(string Filter, string orderBy, bool withLock, Dictionary<string, string> Parameters, string FieldNames = "", int commandTimeout = 30)
        {
            if (_tableType == enmTableType.Writeonly)
            {
                throw new Exception("Object is Writeonly.You can not perform (FindFirst) in this Object.");
            }
            ValidateSqlFragment(Filter, "filter");
            ValidateSqlFragment(orderBy, "order by");

            var dbArgs = new DynamicParameters();
            if (Parameters != null)
            {
                foreach (var pair in Parameters) dbArgs.Add(pair.Key, pair.Value);
            }
            bool InTransaction;
            InTransaction = false;
            if (_Repository.Transaction != null)
            {
                InTransaction = true;
            }
            else
            {
                _Repository.OpenConnection();
                InTransaction = false;
            }
            string sql;
            sql = get_SelectStatment("1", withLock, FieldNames);
            if (!string.IsNullOrEmpty(Filter))
            {
                sql = sql + "Where " + Filter + Environment.NewLine;
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                sql = sql + "Order by " + orderBy;
            }

            var result=_Repository.Connection.Query<T>(sql,param: dbArgs, transaction: _Repository.Transaction, commandTimeout: commandTimeout).FirstOrDefault();
            if (InTransaction == false)
            {
                _Repository.CloseConnection();
            }
            return result;
        }

        internal bool SaveList(List<T> list, string basePropertyName, int commandTimeout = 30)
        {
            if (_tableType == enmTableType.Readonly)
            {
                throw new Exception("Object is Readonly.You can not perform (SaveList) in this Object.");
            }
            bool InTransaction;
            InTransaction = false;
            if (_Repository.Transaction != null)
            {
                InTransaction = true;
            }
            else
            {
                _Repository.OpenConnection();
                InTransaction = false;
            }
            var basePropertyNamecollection = basePropertyName.Split(',');
            var baseColumns = new List<string>();
            foreach (var item in basePropertyNamecollection)
            {
                var Columnname="";
                if (_columnNames.TryGetValue(item.Trim(), out Columnname)) 
                {
                    if (Columnname=="")
                    {
                        throw new Exception(string.Format("TryGetValue!: Please enter the valid basePropertyName.PropertyName {0} is invalid.", item));
                    }
                    baseColumns.Add(Columnname);
                }
                else
                {
                    throw new Exception(string.Format("Please enter the valid basePropertyName.PropertyName {0} is invalid.", item));
                }

            }
            if (baseColumns.Count == 0)
            {
                throw new Exception(string.Format("Please enter the valid basePropertyName."));
            }

            var dt = list.ConvertTo<DataTable>();
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)(_Repository.Connection)
                            , SqlBulkCopyOptions.Default
                            , externalTransaction: (SqlTransaction)_Repository.Transaction))
            {

                var updatecolumnName = "";
                var insertcolumnName = "";
                foreach (var currentColumnname in _columnNames)
                {
                    var Columnname = currentColumnname.Value;
                    var FieldName = currentColumnname.Key;
                    bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping( FieldName, Columnname));
                    if (_keyIsIdentity && ((Columnname ?? "") == (_keycolumnname ?? "")))
                    {
                    }
                    else
                    {
                        
                        updatecolumnName = updatecolumnName + "t1."+ Columnname +" = (Case When t2." + Columnname+" is null then t1." + Columnname + " else T2."+ Columnname + " End) ,";
                        insertcolumnName = insertcolumnName + Columnname + " ,";
                    }

                }
                updatecolumnName = updatecolumnName.Remove(updatecolumnName.Length - 1, 1);
                insertcolumnName = insertcolumnName.Remove(insertcolumnName.Length - 1, 1);

                var _tmptableName = "#" + typeof(T).Name;
                // checking whether the table selected from the dataset exists in the database or not
                var checkTableIfExistsCommand = new SqlCommand();
                var exists = _Repository.Connection.ExecuteScalar(
                                        "if (Not OBJECT_ID('tempdb.." + _tmptableName + "') is Null) SELECT 1 ELSE SELECT 0"
                                        ,transaction:_Repository.Transaction)
                                      .ToString().Equals("1");
                // if does not exist
                if (!exists)
                {
                    var createTableBuilder = new StringBuilder("CREATE TABLE [" + _tmptableName + "]");
                    createTableBuilder.AppendLine("(");

                    // selecting each column of the datatable to create a table in the database
                    foreach (var currentColumnname in _columnNames)
                    {
                        var Columnname = currentColumnname.Value;
                        createTableBuilder.AppendLine("  [" + Columnname + "] VARCHAR(MAX),");
                    }

                    createTableBuilder.Remove(createTableBuilder.Length - 1, 1);
                    createTableBuilder.AppendLine(")");

                    _Repository.Connection.Execute(createTableBuilder.ToString(), transaction: _Repository.Transaction);
                }
                bulkCopy.BulkCopyTimeout = commandTimeout;
                bulkCopy.DestinationTableName = _tmptableName;
                bulkCopy.BatchSize = 10000;
                bulkCopy.WriteToServer(dt.CreateDataReader());
                var baseColumnscheck = "";
                foreach (var item in baseColumns)
                {

                    baseColumnscheck = baseColumnscheck + "t1." + item + " = t2." + item;
                    baseColumnscheck = baseColumnscheck + " And ";
                }
                if (baseColumnscheck=="")
                {
                    throw new Exception("Please enter the valid basePropertyName");
                }
                else
                {
                    baseColumnscheck=baseColumnscheck.Remove(baseColumnscheck.Length - 5, 5);
                }
                baseColumnscheck ="(" +  baseColumnscheck  + ")";
                var SqlCmd = "";
                SqlCmd = SqlCmd + "     UPDATE t1" + Environment.NewLine;
                SqlCmd = SqlCmd + "     SET {2}" + Environment.NewLine;
                SqlCmd = SqlCmd + "     FROM {0} t1" + Environment.NewLine;
                SqlCmd = SqlCmd + "         INNER JOIN {1} t2 ON " + baseColumnscheck + Environment.NewLine;
                SqlCmd = SqlCmd + "     " + Environment.NewLine;
                SqlCmd = SqlCmd + "     INSERT INTO {0} ({3})" + Environment.NewLine;
                SqlCmd = SqlCmd + "         SELECT {3}" + Environment.NewLine;
                SqlCmd = SqlCmd + "         FROM {1} t2" + Environment.NewLine;
                SqlCmd = SqlCmd + "         WHERE NOT Exists (SELECT t1.* FROM {0} t1 Where " + baseColumnscheck + ")" + Environment.NewLine;
                SqlCmd = string.Format(SqlCmd, _tableName, _tmptableName, updatecolumnName, insertcolumnName);
                _Repository.Connection.Execute(SqlCmd, transaction: _Repository.Transaction);

                _Repository.Connection.Execute("Drop Table " + _tmptableName, transaction: _Repository.Transaction);
            }
            if (InTransaction == false)
            {
                _Repository.CloseConnection();
            }
            return true;
        }
        private string Get_ColumnName(CommandType oCommandType, string Prefix, string FieldNames = "")
        {
            string Columnname;
            string FieldName;
            string Result;
            Result = "";
            Columnname = "";
            List<string> FilterFieldNames=null;
            if (FieldNames!="")
            {
                FilterFieldNames=FieldNames.Split(',').ToList<string>();
            }
            foreach (var currentColumnname in _columnNames)
            {

                Columnname = currentColumnname.Value;
                FieldName = currentColumnname.Key;
                if (FilterFieldNames != null )
                {
                    if (FilterFieldNames.Contains(FieldName) ==false)
                    {
                        continue;
                    }
                }
                switch (oCommandType)
                {
                    case CommandType._Insert:
                        {
                            if (_keyIsIdentity & (Columnname ?? "") == (_keycolumnname ?? ""))
                            {
                            }
                            // Do nothing
                            else if (!string.IsNullOrEmpty(Prefix) && _StaticValues is object && _StaticValues.ContainsKey(Columnname) && !string.IsNullOrEmpty(_StaticValues[Columnname]))
                            {
                                Result = Result + _StaticValues[Columnname] + ",";
                            }
                            else
                            {
                                if (Prefix=="")
                                {
                                    Result = Result + Prefix + Columnname + ",";
                                }
                                else
                                {
                                    Result = Result + Prefix + FieldName + ",";
                                }
                                
                            }

                            break;
                        }

                    case CommandType._update:
                        {
                            if (_keyIsIdentity & (Columnname ?? "") == (_keycolumnname ?? ""))
                            {
                            }
                            // Do nothing
                            else if (_StaticValues is object && _StaticValues.ContainsKey(Columnname) && !string.IsNullOrEmpty(_StaticValues[Columnname]))
                            {
                                Result = Result + Columnname + " = " + _StaticValues[Columnname] + ",";
                            }
                            else
                            {
                                Result = Result + Columnname + " = " + Prefix + FieldName + ",";
                            }

                            break;
                        }

                    case CommandType._Select:
                        {
                            Result = Result + Columnname + " " + "["+ FieldName + "]" + ",";
                            break;
                        }
                }
            }

            Result = Result.Substring(0, (Result.Length - 1));
            return Result;
        }


        internal bool DeleteList(string filter, Dictionary<string, string> Parameters, int commandTimeout = 30)
        {
            if (_tableType == enmTableType.Readonly)
            {
                throw new Exception("Object is Readonly.You can not perform (Delete) in this Object.");
            }
            if (filter=="")
            {
                throw new Exception("filter is Empty.You can not perform (Delete) in this Object.");
            }
            ValidateSqlFragment(filter, "filter");
            bool InTransaction;
            InTransaction = false;
            if (_Repository.Transaction != null)
            {
                InTransaction = true;
            }
            else
            {
                _Repository.OpenConnection();
                InTransaction = false;
            }
            string sqlcmd = "";
            sqlcmd = "DELETE FROM " + _tableName + " Where " + filter + ";";
            _Repository.Connection.Execute(sqlcmd, Convert_to_anonymouse_object(Parameters), transaction: _Repository.Transaction, commandTimeout: commandTimeout);
            if (InTransaction == false)
            {
                _Repository.CloseConnection();
            }
            return true;
        }

        private object Convert_to_anonymouse_object(Dictionary<string, string> parameters)
        {
            if (parameters==null)
            {
                return null;
            }
            var eo = new ExpandoObject();
            var eoColl = (ICollection<KeyValuePair<string, object>>)eo;

            foreach (var kvp in parameters.ConvertTo<Dictionary<string, object>>())
            {
                eoColl.Add(kvp);
            }
            dynamic eoDynamic = eo;
            return eoDynamic;
        }

        private static void ValidateIdentifier(string value, string name, bool allowQualifiedName)
        {
            var pattern = allowQualifiedName
                ? @"^[\p{L}_][\p{L}\p{N}_]*(\.[\p{L}_][\p{L}\p{N}_]*)*$"
                : @"^[\p{L}_][\p{L}\p{N}_]*$";

            if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, pattern))
            {
                throw new ArgumentException("Invalid " + name + ".", name);
            }
        }

        private static void ValidateSqlFragment(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            // این API فقط یک عبارت شرط یا مرتب‌سازی را می‌پذیرد، نه دسته‌ای از دستورات SQL.
            if (value.IndexOf(";", StringComparison.Ordinal) >= 0 ||
                value.IndexOf("--", StringComparison.Ordinal) >= 0 ||
                value.IndexOf("/*", StringComparison.Ordinal) >= 0 ||
                value.IndexOf("*/", StringComparison.Ordinal) >= 0)
            {
                throw new ArgumentException("The " + name + " cannot contain SQL batch or comment delimiters.", name);
            }
        }

        private enum CommandType
        {
            _Select,
            _Insert,
            _update,
            _Delete
        }

        private static bool SetPropertyValueByName(object obj, string name, string value)
        {
            var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanWrite)
            {
                return false;
            }
            if (prop.PropertyType.Name.ToUpper() == "GUID")
            {
                prop.SetValue(obj, Guid.Parse(value), null);
            }
            else if(prop.PropertyType.Name.ToUpper()=="STRING")
            {
                prop.SetValue(obj, value.ToString(), null);
            }
            else if (prop.PropertyType.Name.ToUpper() == "INT16")
            {
                prop.SetValue(obj, value.ConvertTo<Int16>(), null);
            }
            else if (prop.PropertyType.Name.ToUpper() == "INT32")
            {
                prop.SetValue(obj, value.ConvertTo<Int32>(), null);
            }
            else if (prop.PropertyType.Name.ToUpper() == "INT64")
            {
                prop.SetValue(obj, value.ConvertTo<Int64>(), null);
            }
            else
            {
                prop.SetValue(obj, value, null);
            }
                
            return true;
        }

        private static object GetPropertyValueByName(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanRead)
            {
                return null;
            }

            return prop.GetValue(obj, null);
        }
        private TableInfoAttribute GetTableInfo()
        {
            TableInfoAttribute tableInfo = null;
            Type type = typeof(T);
            System.Attribute[] tableAttribute = System.Attribute.GetCustomAttributes(type);
            foreach (System.Attribute attr in tableAttribute)
            {
                if (attr is TableInfoAttribute)
                {
                    tableInfo = (TableInfoAttribute)attr;
                }
            }
            if (tableInfo == null)
            {
                tableInfo = new TableInfoAttribute("", "", true);
            }
            if (tableInfo.TableName == "")
            {
                tableInfo.TableName = type.Name.ToString();
            }
            if (tableInfo.keyColumnName == "")
            {
                tableInfo.keyColumnName = "ID";
            }
            if (tableInfo.TableName == "" || tableInfo.keyColumnName == "")
            {
                throw new Exception("You must be set ClassAttribue");
            }
            return tableInfo;
        }
    }

    
}
