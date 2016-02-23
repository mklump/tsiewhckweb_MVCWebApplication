use tsiewhckweb;
GO

CREATE VIEW whckweb_AllTablesDataView
AS
SELECT Machine_Config.HW_Version, Machine_Config.WHCK_Version, Machine_Config.Windows_Build_Num, Package.[Checksum], 
       Package.Date_Uploaded, Package.[FileName], Package.[Path], Package.TestResult_Summary, [Login].IDsid, [Login].Last_AccessTime, 
       [Login].EAM_role, [Login].CDIS_email, Result.[Status], Result.Comment, Bug.HSD, Bug.CSP, Bug.WINQUAL, Bug.MANAGEPRO, 
       [Project_Group].Name, Component.Name AS Component_Name, Driver_Config.Team, Driver_Config.BKC_Version, Driver_Config.SoftAP, 
       Driver_Config.BT_Driver, Driver_Config.WiFi_Driver, Driver_Config.NFC_Driver, Driver_Config.GPS_Driver, 
       TestCase.Name AS TestCase_Name, TestCase.[TimeStamp]
FROM Machine_Config INNER JOIN
     Package INNER JOIN
     [Login] ON Package.UserID = [Login].UserID INNER JOIN
     Result ON Package.PackageID = dbo.Result.PackageID INNER JOIN
     Bug ON dbo.Result.BugID = Bug.BugID INNER JOIN
     Test_Config ON dbo.Package.ConfigNumID = Test_Config.ConfigNumID INNER JOIN
     Project_Group INNER JOIN
     Component ON Project_Group.Project_GroupID = Component.Project_GroupID ON 
     Test_Config.ConfigNumID = Project_Group.ConfigNumID INNER JOIN
     Driver_Config ON Test_Config.DriverConfigID = Driver_Config.DriverConfigID ON 
     Machine_Config.MachineConfigID = Test_Config.MachineConfigID INNER JOIN
     TestCase ON dbo.Result.TestCaseID = TestCase.TestCaseID AND
	 Component.ComponentID = TestCase.ComponentID;
GO
