using System;
using Repository;
using Repository.Domain;
using Xunit;

namespace SqlCRUDRepository.Tests
{
    public class SqlValidationTests
    {
        [Fact]
        public void Find_RejectsAnUnsafeTableName()
        {
            var repository = RepositoryFactory.CreateRepository("Server=(local);Database=unused;Trusted_Connection=True;");

            var exception = Assert.Throws<ArgumentException>(() => repository.Find<UnsafeTableNameModel>("", "", false, null));

            Assert.Equal("table name", exception.ParamName);
        }

        [Fact]
        public void Find_RejectsAnUnsafeColumnName()
        {
            var repository = RepositoryFactory.CreateRepository("Server=(local);Database=unused;Trusted_Connection=True;");

            var exception = Assert.Throws<ArgumentException>(() => repository.Find<UnsafeColumnNameModel>("", "", false, null));

            Assert.Equal("column name", exception.ParamName);
        }

        [Fact]
        public void Find_RejectsSqlBatchDelimitersInFilter()
        {
            var repository = RepositoryFactory.CreateRepository("Server=(local);Database=unused;Trusted_Connection=True;");

            var exception = Assert.Throws<ArgumentException>(() => repository.Find<ValidModel>("ID = @ID; DELETE FROM Customer", "", false, null));

            Assert.Equal("filter", exception.ParamName);
        }

        [Fact]
        public void FindFirst_RejectsSqlCommentsInOrderBy()
        {
            var repository = RepositoryFactory.CreateRepository("Server=(local);Database=unused;Trusted_Connection=True;");

            var exception = Assert.Throws<ArgumentException>(() => repository.FindFirst<ValidModel>("", "ID -- injected", false, null));

            Assert.Equal("order by", exception.ParamName);
        }

        [TableInfo("Customer", "ID", true)]
        private class ValidModel
        {
            public int ID { get; set; }
        }

        [TableInfo("Customer; DROP TABLE Customer", "ID", true)]
        private class UnsafeTableNameModel
        {
            public int ID { get; set; }
        }

        [TableInfo("Customer", "ID", true)]
        private class UnsafeColumnNameModel
        {
            public int ID { get; set; }

            [FieldInfo("Name; DROP TABLE Customer", false)]
            public string Name { get; set; }
        }
    }
}
