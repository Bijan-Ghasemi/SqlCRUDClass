--@strMigrationDesc= Create Customer Table 
GO
CREATE TABLE [dbo].[tblCustomer3](
	[ID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](max) NULL,
 CONSTRAINT [PK_Customer3] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[tblCustomer3] ADD  CONSTRAINT [DF_tblCustomer3_ID]  DEFAULT (newid()) FOR [ID]
GO
