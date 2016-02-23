USE tsiewhckweb;
GO

ALTER VIEW Tsiewhckweb_ConfigMachineTestTableView
AS
SELECT TOP( 100 ) m.MachineConfigID AS MConfigNumID, d.DriverConfigID AS DConfigNumID,
tc.ConfigNumID AS ConfigNum, m.HW_Version, m.WHCK_Version, m.Windows_Build_Num, d.BKC_Version,
d.SoftAP, d.BT_Driver, d.WiFi_Driver, d.NFC_Driver, d.GPS_Driver
FROM [Machine_Config] AS m LEFT JOIN [Test_Config] AS tc
ON m.MachineConfigID = tc.MachineConfigID LEFT JOIN [Driver_Config] AS d
ON tc.DriverConfigID = d.DriverConfigID;
GO
