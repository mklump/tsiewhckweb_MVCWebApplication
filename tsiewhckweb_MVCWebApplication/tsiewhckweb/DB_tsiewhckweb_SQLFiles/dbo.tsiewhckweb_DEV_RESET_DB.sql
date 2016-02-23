USE tsiewhckweb;
GO

IF EXISTS( SELECT 1 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_TYPE='BASE TABLE' 
    AND TABLE_NAME='Component' )
BEGIN
	ALTER TABLE [Component] DROP CONSTRAINT FK_Component_Project_Group;
END
GO

IF EXISTS( SELECT 1 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_TYPE='BASE TABLE' 
    AND TABLE_NAME='Project_Group' )
BEGIN
	ALTER TABLE [Project_Group] DROP CONSTRAINT FK_Project_Group_Test_Config;
	DROP TABLE [Project_Group];
END
GO

IF EXISTS( SELECT 1
	FROM INFORMATION_SCHEMA.TABLES
	WHERE TABLE_TYPE='BASE TABLE'
	AND TABLE_NAME='Result' )
BEGIN
	ALTER TABLE Result DROP CONSTRAINT FK_Result_Package;
	ALTER TABLE Result DROP CONSTRAINT FK_Result_Bug;
	ALTER TABLE Result DROP CONSTRAINT FK_Result_TestCase;
	DROP TABLE Result;
END
GO

IF EXISTS( SELECT 1
	FROM INFORMATION_SCHEMA.TABLES
	WHERE TABLE_TYPE='BASE TABLE'
	AND TABLE_NAME='TestCase' )
BEGIN
	ALTER TABLE TestCase DROP CONSTRAINT FK_TestCase_Component;
	DROP TABLE TestCase;
END
GO

IF EXISTS( SELECT 1 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_TYPE='BASE TABLE' 
    AND TABLE_NAME='Component' )
BEGIN
	DROP TABLE [Component];
END
GO

IF EXISTS( SELECT 1 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_TYPE='BASE TABLE' 
    AND TABLE_NAME='Bug' )
BEGIN
	DROP TABLE [Bug];
END
GO

IF EXISTS( SELECT 1
	FROM INFORMATION_SCHEMA.TABLES
	WHERE TABLE_TYPE='BASE TABLE'
	AND TABLE_NAME='Package' )
BEGIN
	ALTER TABLE [Package] DROP CONSTRAINT FK_Package_Test_Config;
	ALTER TABLE [Package] DROP CONSTRAINT FK_Package_Login;
	DROP TABLE [Package];
END

IF EXISTS( SELECT 1
	FROM INFORMATION_SCHEMA.TABLES
	WHERE TABLE_TYPE='BASE TABLE'
	AND TABLE_NAME='Test_Config' )
BEGIN
	ALTER TABLE [Test_Config] DROP CONSTRAINT FK_Test_Config_Machine_Config;
	ALTER TABLE [Test_Config] DROP CONSTRAINT FK_Test_Config_Driver_Config;
	DROP TABLE [Test_Config];
END

IF EXISTS( SELECT 1 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_TYPE='BASE TABLE' 
    AND TABLE_NAME='Driver_Config' )
BEGIN
	DROP TABLE [Driver_Config];
END
GO

IF EXISTS( SELECT 1 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_TYPE='BASE TABLE' 
    AND TABLE_NAME='Machine_Config' )
BEGIN
	DROP TABLE [Machine_Config];
END
GO

IF EXISTS( SELECT 1 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_TYPE='BASE TABLE' 
    AND TABLE_NAME='Login' )
BEGIN
	DROP TABLE [Login];
END
GO

CREATE TABLE [dbo].[Machine_Config]
(
	[MachineConfigID] INT IDENTITY (1, 1) NOT NULL,
	[HW_Version] nvarchar( MAX ) NOT NULL,
	[WHCK_Version] nvarchar ( MAX ) NOT NULL,
	[Windows_Build_Num] nvarchar( MAX ) NOT NULL,
	CONSTRAINT PK_Machine_Config_MachineConfigID PRIMARY KEY CLUSTERED ([MachineConfigID] ASC)
);
GO

CREATE TABLE [dbo].[Driver_Config]
(
	[DriverConfigID] INT IDENTITY (1, 1) NOT NULL,
	[Team] nvarchar( MAX ) NOT NULL,
	[BKC_Version] nvarchar( MAX ) NOT NULL,
	[SoftAP] nvarchar( MAX ) NOT NULL,
	[BT_Driver] nvarchar( MAX ) NOT NULL,
	[WiFi_Driver] nvarchar( MAX ) NOT NULL,
	[NFC_Driver] nvarchar( MAX ) NOT NULL,
	[GPS_Driver] nvarchar( MAX ) NOT NULL,
	CONSTRAINT PK_Driver_Config_DriverConfigID PRIMARY KEY CLUSTERED ([DriverConfigID] ASC)
);
GO

CREATE TABLE [dbo].[Test_Config]
(
	[ConfigNumID] INT NOT NULL,
	[MachineConfigID] INT NOT NULL,
	[DriverConfigID] INT NOT NULL,
	CONSTRAINT PK_Test_Config_ConfigNumID PRIMARY KEY CLUSTERED ([ConfigNumID] ASC),
	CONSTRAINT FK_Test_Config_Machine_Config FOREIGN KEY ([MachineConfigID])
	REFERENCES [Machine_Config] ([MachineConfigID]) ON DELETE CASCADE,
	CONSTRAINT FK_Test_Config_Driver_Config FOREIGN KEY ([DriverConfigID])
	REFERENCES [Driver_Config] ([DriverConfigID]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[Login]
(
	[UserID] INT IDENTITY (1, 1) NOT NULL,
	[IDsid] nvarchar( 500 ) NOT NULL,
	[Last_AccessTime] datetime NOT NULL,
	[EAM_role] nvarchar( 500 ) NOT NULL,
	[CDIS_email] nvarchar( 500 ) NOT NULL,
	CONSTRAINT PK_User_UserID PRIMARY KEY CLUSTERED ([UserID] ASC)
);
GO

CREATE TABLE [dbo].[Package]
(
	[PackageID] INT IDENTITY (1, 1) NOT NULL,
	[ConfigNumID] INT NOT NULL,
	[UserID] INT NOT NULL,
	[Checksum] nvarchar( MAX ) NOT NULL,
	[Date_Uploaded] datetime NOT NULL,
	[FileName] nvarchar( MAX ) NOT NULL,
	[Path] nvarchar( MAX ) NOT NULL,
	[TestResult_Summary] nvarchar( MAX ) NOT NULL,
	CONSTRAINT PK_Package_PackageID PRIMARY KEY CLUSTERED ([PackageID] ASC),
	CONSTRAINT FK_Package_Test_Config FOREIGN KEY ([ConfigNumID])
	REFERENCES [Test_Config] ([ConfigNumID]) ON DELETE CASCADE,
	CONSTRAINT FK_Package_Login FOREIGN KEY ([UserID])
	REFERENCES [Login] ([UserID]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[Component]
(
	ComponentID INT IDENTITY (1, 1) NOT NULL,
	Name nvarchar( 500 ) NOT NULL,
	Project_GroupID INT NOT NULL,
	CONSTRAINT PK_Component_ComponentID PRIMARY KEY CLUSTERED ( ComponentID ASC ),
);
GO

CREATE TABLE [dbo].[Bug]
(
	BugID INT IDENTITY (1, 1) NOT NULL,
	HSD nvarchar( MAX ) NOT NULL,
	CSP nvarchar( MAX ) NOT NULL,
	WINQUAL nvarchar( MAX ) NOT NULL,
	MANAGEPRO nvarchar( MAX ) NOT NULL,
	CONSTRAINT PK_Bug_BugID PRIMARY KEY CLUSTERED ( BugId ASC )
);
GO

CREATE TABLE [dbo].[TestCase]
(
	TestCaseID INT IDENTITY (1, 1) NOT NULL,
	Name nvarchar( 500 ) NOT NULL,
	ComponentID INT NOT NULL,
	[TimeStamp] DATETIME NOT NULL,
	CONSTRAINT PK_TestCase_TestCaseID PRIMARY KEY CLUSTERED ( TestCaseID ASC ),
	CONSTRAINT FK_TestCase_Component FOREIGN KEY ( ComponentID )
	REFERENCES Component ( ComponentID ) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[Result]
(
	ResultID INT IDENTITY (1, 1) NOT NULL,
	[Status] BIT NOT NULL, --TRUE or 1 for passing, and FALSE or 0 for failing.
	Comment nvarchar( 1000 ) NOT NULL,
	PackageID INT NOT NULL,
	BugID INT NOT NULL,
	TestCaseID INT NOT NULL,
	CONSTRAINT PK_Result_ResultID PRIMARY KEY CLUSTERED ( ResultID ASC ),
	CONSTRAINT FK_Result_Package FOREIGN KEY ( PackageID )
	REFERENCES Package ( PackageID ) ON DELETE CASCADE,
	CONSTRAINT FK_Result_Bug FOREIGN KEY ( BugID )
	REFERENCES Bug ( BugID ) ON DELETE CASCADE,
	CONSTRAINT FK_Result_TestCase FOREIGN KEY ( TestCaseID )
	REFERENCES TestCase ( TestCaseID ) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[Project_Group]
(
	Project_GroupID INT IDENTITY (1, 1) NOT NULL,
	Name nvarchar( 500 ) NOT NULL,
	ConfigNumID INT NOT NULL,
	CONSTRAINT PK_Project_Group_Project_GroupID PRIMARY KEY CLUSTERED ( Project_GroupID ASC ),
	CONSTRAINT FK_Project_Group_Test_Config FOREIGN KEY ( ConfigNumID )
	REFERENCES Test_Config ( ConfigNumID ) ON DELETE CASCADE
);
GO

ALTER TABLE [Component]
WITH NOCHECK ADD CONSTRAINT FK_Component_Project_Group FOREIGN KEY ( Project_GroupID )
REFERENCES Project_Group ( Project_GroupID ) ON DELETE NO ACTION;
GO

--ALTER TABLE [Driver_Config]
--WITH NOCHECK ADD CONSTRAINT tsiewhckweb_Driver_Config_FKConstraint
--FOREIGN KEY ( [ConfigNum] ) REFERENCES [Machine_Config]( [ConfigNumID] );
--GO
