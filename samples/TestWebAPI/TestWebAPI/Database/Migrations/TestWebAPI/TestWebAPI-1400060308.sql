--@strMigrationDesc= Create StoredProcedure
GO
IF EXISTS(SELECT * FROM sys.procedures WHERE Name='SP_GetCustomerDetail')
DROP PROCEDURE [dbo].[SP_GetCustomerDetail]
GO
CREATE PROCEDURE [dbo].[SP_GetCustomerDetail]
@FirstName NVARCHAR(MAX)
AS
-- you can return ten result set and handle results in your application.
Select * From dbo.tblCustomer where FirstName = @FirstName
Select * From dbo.tblCustomer where FirstName = @FirstName
Select * From dbo.tblCustomer where FirstName = @FirstName
Select * From dbo.tblCustomer where FirstName = @FirstName
GO