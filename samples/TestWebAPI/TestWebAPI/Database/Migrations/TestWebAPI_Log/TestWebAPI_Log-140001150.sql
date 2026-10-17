--@strMigrationDesc= Last version 14000114
GO
IF NOT EXISTS(SELECT * FROM sys.tables WHERE name='Web_API_Response_Log')
CREATE TABLE Web_API_Response_Log(ID BIGINT IDENTITY(1,1),LogDate DATETIME,ClientAddress NVARCHAR(MAX),UserName NVARCHAR(MAX),API_Controller NVARCHAR(max),API_Action NVARCHAR(max),User_Detail NVARCHAR(max),Input NVARCHAR(max),Output NVARCHAR(max),Duration NVARCHAR(max))
GO
