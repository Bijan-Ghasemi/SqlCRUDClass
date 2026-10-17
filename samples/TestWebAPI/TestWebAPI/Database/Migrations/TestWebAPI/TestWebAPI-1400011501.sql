--@strMigrationDesc= Create Customer Table 
GO
Create Table tblCustomer(ID int Identity(1,1), FirstName nvarchar(max), LastName nvarchar(max))
GO
/*
we are create __KeyGenerator Stored Procedure 
Please overwrite your own key generation policy in this procedure in sql server.
*/
Create Table tblCustomer2(ID nvarchar(500), FirstName nvarchar(max), CustomerLastName nvarchar(max),Description nvarchar(max))