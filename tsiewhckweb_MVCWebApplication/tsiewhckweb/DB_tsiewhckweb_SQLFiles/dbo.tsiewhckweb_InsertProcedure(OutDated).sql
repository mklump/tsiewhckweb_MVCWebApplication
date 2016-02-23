USE tsiewhckweb;
GO

ALTER PROCEDURE sp_InsertTsiewhckweb_DB
(
	@HW_Version AS nvarchar( MAX ),			-- Machine_Config
	@WHCK_Version AS nvarchar( MAX ),		-- Machine_Config
	@Windows_Build_Num AS nvarchar( MAX ),	-- Machine_Config

	@ConfigNum AS int = NULL,				-- Test_Config

	@Team AS nvarchar( MAX ),				-- Driver_Config
	@BKC_Version AS nvarchar( MAX ),			-- Driver_Config
	@SoftAP AS nvarchar( MAX ),				-- Driver_Config
	@BT_Driver AS nvarchar( MAX ),			-- Driver_Config
	@WiFi_Driver AS nvarchar( MAX ),	    -- Driver_Config
	@NFC_Driver AS nvarchar( MAX ),			-- Driver_Config
	@GPS_Driver AS nvarchar( MAX ),			-- Driver_Config

	@IDsid AS nvarchar( 500 ),				-- Login
	@Last_AccessTime AS datetime,			-- Login
	@EAM_role AS nvarchar( 500 ),			-- Login
	@CDIS_email AS nvarchar( 500 ),			-- Login

	@Checksum AS nvarchar( MAX ),			-- Package
	@Date_Uploaded AS datetime,				-- Package
	@FileName AS nvarchar( MAX ),			-- Package
	@Path AS nvarchar( MAX ),				-- Package
	@TestResult_Summary AS nvarchar( MAX )	-- Package
)
AS
BEGIN
	Declare @TransactionCountOnEntry int;
	Declare @ConfigNumID int, @MachineConfigID int, @DriverConfigID int, @UserID int;
	Declare @ErrorCode int, @ErrorMessage nvarchar(100), @RowCount int;
	SET NOCOUNT ON;

	Select @ErrorCode = @@Error
	If @ErrorCode = 0
	Begin
	   Select @TransactionCountOnEntry = @@TranCount
	   BEGIN TRANSACTION
	End
	BEGIN TRY

	--SET IDENTITY_INSERT Test_Config ON;
	--SET IDENTITY_INSERT Machine_Config ON;
	--SET IDENTITY_INSERT Driver_Config ON;

	SELECT @RowCount = COUNT(*) FROM tsiewhckweb_AllTablesView
	IF( 0 = @RowCount )
		SET @ConfigNumID = 1;
	IF( @ConfigNum IS NOT NULL )
		SET @ConfigNumID = @ConfigNum;
	ELSE IF( @ConfigNumID IS NULL )
		SELECT @ConfigNumID = COUNT( t.ConfigNumID ) + 1 FROM Test_Config AS t;

	IF( EXISTS( SELECT t.ConfigNumID FROM Test_Config AS t WHERE t.ConfigNumID = @ConfigNumID ) )
		SELECT @ConfigNumID = MAX( t.ConfigNumID ) + 1 FROM Test_Config AS t;

	-- Insert first all concerned into the [Machine_Config] table
	INSERT [Machine_Config]( HW_Version, WHCK_Version, Windows_Build_Num )
	SELECT @HW_Version, @WHCK_Version, @Windows_Build_Num;

	-- Get the last [Machine_Config] identity value generated within the scope of this stored procedure, and reuse it for the next Insert() calls.
	SET @MachineConfigID = SCOPE_IDENTITY();

	-- Insert second all concerned into the [Driver_Config] table
	INSERT [Driver_Config]( Team, BKC_Version, SoftAP, BT_Driver, WiFi_Driver, NFC_Driver, GPS_Driver )
	SELECT @Team, @BKC_Version, @SoftAP, @BT_Driver, @WiFi_Driver, @NFC_Driver, @GPS_Driver;

	SET @DriverConfigID = SCOPE_IDENTITY();

	--Insert third into the intermediary [Test_Config] table first since both Machine_Config and Driver_Config reference this table.
	INSERT [Test_Config]( ConfigNumID, MachineConfigID, DriverConfigID )
	SELECT @ConfigNumID, @MachineConfigID, @DriverConfigID;

	-- Insert forth all concerned into the [Login] table
	INSERT [Login]( IDsid, Last_AccessTime, EAM_role, CDIS_email )
	SELECT @IDsid, @Last_AccessTime, @EAM_role, @CDIS_email;

	SET @UserID = SCOPE_IDENTITY();

	-- Insert last all concerned into the [Package] table
	INSERT [Package]( ConfigNumID, UserID, [Checksum], Date_Uploaded, [FileName], [Path], TestResult_Summary )
	SELECT @ConfigNumID, @UserID, @Checksum, @Date_Uploaded, @FileName, @Path, @TestResult_Summary;

	END TRY
	BEGIN CATCH
		SELECT 
			ERROR_NUMBER() AS ErrorNumber
			,ERROR_SEVERITY() AS ErrorSeverity
			,ERROR_STATE() AS ErrorState
			,ERROR_PROCEDURE() AS ErrorProcedure
			,ERROR_LINE() AS ErrorLine
			,ERROR_MESSAGE() AS ErrorMessage;
			SET @ErrorMessage = ERROR_MESSAGE();
		-- If the transaction is executing this, then roll it back executing the catch statement.
		If( @@TranCount > @TransactionCountOnEntry )
			ROLLBACK TRANSACTION;
		RETURN 1; --The stored procedure call failed, please see output parameters for the error details.
	END CATCH
	-- If the transaction is executing this code here following try-catch, then INSERT() succeeded - COMMIT TRANSACTION.
	If( @@TranCount > @TransactionCountOnEntry )
		COMMIT TRANSACTION;
	-- Save the @@ERROR and @@ROWCOUNT values in local variables before they are cleared for comparison in the output parameters.
	Set @ErrorCode = @@ERROR;
	Set @RowCount = @@ROWCOUNT;

	RETURN 0; --The stored procedure call succeeded.
END
