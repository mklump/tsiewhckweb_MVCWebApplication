using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;

namespace tsiewhckweb.Models
{
    using Microsoft.Windows.Kits.Hardware.ObjectModel;

    using System.Diagnostics;
    using System.Web;
    using System.Web.Mvc;
    using System.Linq;

    /// <summary>
    /// ADO.NET entity/model processing logic for ConfigurationController class in tsiewhckweb.Models namespace.
    /// </summary>
    public class ConfigurationModel
    {
        private tsiewhckwebEntities db;
        private PackageModel package;

        private static int logLine;
        private static Controller controllerContext;
        private static Project_Group project_groupDB;
        /// <summary>
        /// Public exposure property to the private datamember ConfigurationModel.project_groupDB.
        /// </summary>
        public Project_Group Project_GroupDB
        {
            set { project_groupDB = value; }
            get { return project_groupDB; }
        }

        /// <summary>
        /// ConfigurationModel default contructor.
        /// </summary>
        public ConfigurationModel()
        {
            db = new tsiewhckwebEntities();
            if( null == project_groupDB )
                project_groupDB = new Project_Group();
            if( null != project_groupDB && null == project_groupDB.Test_Config )
                project_groupDB.Test_Config = new Test_Config();
            package = new PackageModel();
            logLine = 1;
        }
        /// <summary>
        /// Get Configuration page view helper function.
        /// </summary>
        /// <param name="context">Current working controller context.</param>
        /// <returns>View result set.</returns>
        public List<Project_Group> IndexHelper( ref Controller context )
        {
            controllerContext = context;
            List<Project_Group> configView = new List<Project_Group>
               (
                    db.Project_Group
                    .Include( i => i.Test_Config )
                    .Include( i => i.Test_Config.Driver_Config )
                    .Include( i => i.Test_Config.Machine_Config )
                    .Include( i => i.Test_Config.Packages )
                    .Include( j => j.Test_Config.Packages.Select(k => k.Login) )
                    .Where( i => i.Name != string.Empty &&
                        i.ConfigNumID != 0 &&
                        i.Test_Config.Machine_Config.HW_Version != string.Empty &&
                        i.Test_Config.Machine_Config.WHCK_Version != string.Empty &&
                        i.Test_Config.Machine_Config.Windows_Build_Num != string.Empty &&
                        i.Test_Config.Driver_Config.BKC_Version != string.Empty )
                    .OrderBy( i => i.Test_Config.ConfigNumID )
                );
            return configView;
        }
        /// <summary>
        /// Helper functions for GET: /Configuration/Details/5
        /// </summary>
        /// <param name="ID">Config # (Num) identification for the complete row to retrieve.</param>
        /// <returns>The complete TestResults result set that was found as a List of Project_Group.</returns>
        public Project_Group DetailsHelper( int ID = 0 )
        {
            Project_Group project_group =
                db.Project_Group.Include( i => i.Test_Config )
                    .Include( j => j.Test_Config.Packages )
                    .Include( k => k.Test_Config.Packages.Select( l => l.Results ) )
                    .Include( m => m.Components )
                    .Include( n => n.Components.Select( o => o.TestCases ) )
                    .Single( r => r.Test_Config.ConfigNumID == ID );
            return project_group;
        }
        /// <summary>
        /// Helper function to Overloaded POST: /Configuration/Create
        /// </summary>
        /// <param name="file">Posted file to the web application server location.</param>
        /// <param name="context">Current working controller context.</param>
        public void GetResultsHelper( HttpPostedFileBase file, ref Controller context )
        {
            controllerContext = context;
            context.ViewBag.whckVersion = package.WhckVersion(file);
            context.ViewBag.TestResult = context.ViewBag.whckVersion;

            Package packageDB = new Package();
            packageDB.Checksum = package.checksum;
            packageDB.Date_Uploaded = package.datetimeRef;
            packageDB.FileName = package.fileName;
            packageDB.Path = package.path;
            packageDB.TestResult_Summary = context.ViewBag.TestResult;
            packageDB.Login = new Login();
            packageDB.Login.IDsid = "Anonymous - authorization not yet completed";
            packageDB.Login.Last_AccessTime = DateTime.Now;
            packageDB.Login.EAM_role = "n/a";
            packageDB.Login.CDIS_email = "n/a";
            project_groupDB.Test_Config.Packages.Add( packageDB );
        }
        /// <summary>
        /// Helper function for GET: /Configuration/Edit/5
        /// </summary>
        /// <param name="ID">The complete Project_Group row that was found.</param>
        public Project_Group EditHelper( int ID = 0 )
        {
            Project_Group project_group = db.Project_Group
                .Include( i => i.Test_Config )
                .Include( i => i.Test_Config.Driver_Config )
                .Include( j => j.Test_Config.Machine_Config )
                .Include( k => k.Test_Config.Packages )
                .Include( l => l.Test_Config.Packages.Select( m => m.Login ) )
                .Single( n => n.ConfigNumID == ID );
            return project_group;
        }
        /// <summary>
        /// Helper fucntion for POST: /Configuration/Edit/5
        /// </summary>
        /// <param name="project_group">Data record/row of the Test_Config table plus relating tables to edit.</param>
        /// <returns>The modified Project_Group table plus all relating tables to the Edit View page.</returns>
        public Project_Group EditAndSaveHelper( Project_Group project_group )
        {
            db.Project_Group.Remove( project_group );
            db.Project_Group.Add( project_group );
            db.Project_Group.Attach( project_group ); // Function is untested.
            db.SaveChanges();
            return project_group;
        }
        /// <summary>
        /// Helper function for GET: /Configuration/Delete/5
        /// </summary>
        /// <param name="ID">Record ID of row to delete.</param>
        /// <param name="context">Current working controller context.</param>
        /// <returns>The data record/row found to be deleted.</returns>
        public Project_Group DeleteHelper( ref Controller context, int ID = 0 )
        {
            controllerContext = context;
            Project_Group project_group = null;
            try
            {
                project_group = db.Project_Group
                    .Include( g => g.Test_Config )
                    .Include( i => i.Test_Config.Driver_Config )
                    .Include( j => j.Test_Config.Machine_Config )
                    .Include( k => k.Test_Config.Packages )
                    .Include( l => l.Test_Config.Packages.Select( m => m.Login ) )
                    .Single( n => n.Test_Config.ConfigNumID == ID );
            }
            catch( Exception error )
            {
                context.Response.Write( "An error occured processing your request. Error details:\n" + error.ToString() );
                return null;
            }
            return project_group;
        }
        /// <summary>
        /// Helper function POST: /Configuration/Delete/5
        /// </summary>
        /// <param name="ID">Record ID to delete from the tsiewhckweb database.</param>
        /// <param name="context">Current working controller context.</param>
        /// <returns>Project_Group record that was or is being deleted.</returns>
        public Project_Group DeleteConfirmedHelper( int ID, ref Controller context )
        {
            controllerContext = context;
            try
            {
                Project_Group project_group = db.Project_Group
                    .Include( g => g.Test_Config )
                    .Include( i => i.Test_Config.Driver_Config )
                    .Include( j => j.Test_Config.Machine_Config )
                    .Include( k => k.Test_Config.Packages )
                    .Include( l => l.Test_Config.Packages.Select( m => m.Login ) )
                    .Single( n => n.Test_Config.ConfigNumID == ID );
                db.Project_Group.Remove( project_group );
                db.SaveChanges();
                return project_group;
            }
            catch( Exception error )
            {
                context.Response.Write( "An error occured processing your request. Error details:\n" + error.ToString() );
                return null;
            }
        }
        /// <summary>
        /// Helper function that provides the Create data access functionality for the Project_Group
        /// table and all other related tables in the tsiewhckweb database.
        /// </summary>
        /// <param name="project_group">Input Test_Config/BKC reference to create or edit a record/row.</param>
        /// <param name="context">Current Working ControllerContext context.</param>
        /// <returns>The Project_Group+BCK configuration row that was submitted.</returns>
        public Project_Group CreateHelper( Project_Group project_group, ref Controller context )
        {
            controllerContext = context;
            bool isCreate = (null == project_group.Test_Config) ? true : false;
            try
            {
                int configNumID = 0;
                if( string.Empty == context.Request.Form.GetValues( "txtProjectGroup" )[0] )
                    context.ModelState.AddModelError("txtProjectGroup", "Project Group is required.");
                if( string.Empty == context.Request.Form.GetValues("txtConfigNumID")[0] )
                    context.ModelState.AddModelError("txtConfigNumID", "Config # is required.");
                else if( false == int.TryParse( context.Request.Form.GetValues( "txtConfigNumID" )[0], out configNumID ) )
                    context.ModelState.AddModelError("txtConfigNumID", "Config # MUST be a number (positive integer).");
                if( string.Empty == context.Request.Form.GetValues( "txtHW_Version" )[0] )
                    context.ModelState.AddModelError("txtHW_Version", "HW Version is required.");
                if( string.Empty == context.Request.Form.GetValues( "txtWHCK_Version" )[0] )
                    context.ModelState.AddModelError("txtWHCK_Version", "WHCK Version is required.");
                if( string.Empty == context.Request.Form.GetValues( "txtWindows_Build_Num" )[0] )
                    context.ModelState.AddModelError("txtWindows_Build_Num", "Windows Build # is required.");
                if( string.Empty == context.Request.Form.GetValues( "txtBKC_Version" )[0] )
                    context.ModelState.AddModelError("txtBKC_Version", "BKC Version is required.");
                //if( string.Empty == Request.Form.GetValues( "txtTeam" )[0] )
                //    ModelState.AddModelError( "txtTeam", "Team is required." );
                //if( string.Empty == Request.Form.GetValues( "txtSoftAP" )[0] )
                //    ModelState.AddModelError( "txtSoftAP", "SoftAP is required." );
                //if( string.Empty == Request.Form.GetValues( "txtBT_Driver" )[0] )
                //    ModelState.AddModelError( "txtBT_Driver", "BT Driver is required." );
                //if( string.Empty == Request.Form.GetValues( "txtWiFi_Driver" )[0] )
                //    ModelState.AddModelError( "txtWiFi_Driver", "WiFi Driver is required." );
                //if( string.Empty == Request.Form.GetValues( "txtNFC_Driver" )[0] )
                //    ModelState.AddModelError( "txtNFC_Driver", "NFC Driver is required." );
                //if( string.Empty == Request.Form.GetValues( "txtGPS_Driver" )[0] )
                //    ModelState.AddModelError( "txtGPS_Driver", "GPS Driver is required." );

                project_group = project_groupDB;
                List<Test_Config> containTest = db.Test_Config.ToList();
                project_group.Test_Config.ConfigNumID = configNumID;
                int lastIndex = 0;
                foreach( Test_Config row in containTest )
                {
                    if( row.ConfigNumID == project_group.Test_Config.ConfigNumID )
                    {
                        lastIndex = row.ConfigNumID + 1;
                        project_group.Test_Config.ConfigNumID = lastIndex;
                    }
                }
                project_group.Test_Config.Driver_Config = new Driver_Config();
                project_group.Test_Config.Machine_Config = new Machine_Config();
                project_group.Name = context.Request.Form.GetValues("txtProjectGroup")[0];
                project_group.Test_Config.ConfigNumID = project_group.Test_Config.ConfigNumID;
                project_group.Test_Config.Driver_Config.Team = ( "" == context.Request.Form.GetValues("txtTeam")[0] ) ? "" : context.Request.Form.GetValues("txtTeam")[0];
                project_group.Test_Config.Machine_Config.HW_Version = context.Request.Form.GetValues("txtHW_Version")[0];
                project_group.Test_Config.Machine_Config.WHCK_Version = context.Request.Form.GetValues("txtWHCK_Version")[0];
                project_group.Test_Config.Machine_Config.Windows_Build_Num = context.Request.Form.GetValues("txtWindows_Build_Num")[0];
                project_group.Test_Config.Driver_Config.BKC_Version = context.Request.Form.GetValues("txtBKC_Version")[0];
                project_group.Test_Config.Driver_Config.SoftAP = ( "" == context.Request.Form.GetValues("txtSoftAP")[0] ) ? "" : context.Request.Form.GetValues("txtSoftAP")[0];
                project_group.Test_Config.Driver_Config.BT_Driver = ( "" == context.Request.Form.GetValues("txtBT_Driver")[0] ) ? "" : context.Request.Form.GetValues("txtBT_Driver")[0];
                project_group.Test_Config.Driver_Config.WiFi_Driver = ( "" == context.Request.Form.GetValues("txtWiFi_Driver")[0] ) ? "" : context.Request.Form.GetValues("txtWiFi_Driver")[0];
                project_group.Test_Config.Driver_Config.NFC_Driver = ( "" == context.Request.Form.GetValues("txtNFC_Driver")[0] ) ? "" : context.Request.Form.GetValues("txtNFC_Driver")[0];
                project_group.Test_Config.Driver_Config.GPS_Driver = ( "" == context.Request.Form.GetValues("txtGPS_Driver")[0] ) ? "" : context.Request.Form.GetValues("txtGPS_Driver")[0];
                project_groupDB = project_group;
                if( context.ModelState.IsValid )
                {
                    if( true == isCreate )
                        db.Project_Group.Add( project_group );
                    else
                        db.Project_Group.Attach( project_group );
                    db.SaveChanges();
                    return project_group;
                }
                else
                    return null;
            }
            catch( DbEntityValidationException dbEx )
            {
                foreach( DbEntityValidationResult validationErrors in dbEx.EntityValidationErrors )
                {
                    foreach( DbValidationError validationError in validationErrors.ValidationErrors )
                    {
                        context.Response.Write( "An error occured processing your request. Error details:\n" + validationError.ToString() );
                        Trace.TraceInformation( "Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage );
                    }
                }
                return null;
            }
            catch( DbUpdateException error )
            {
                context.Response.Write( "An error occured processing your request. Error details:\n" + error.ToString() );
                Trace.TraceInformation( "An error occured processing your request. Error details:\n" + error.ToString() );
                return null;
            }
            catch( Exception error )
            {
                context.Response.Write( "An error occured processing your request. Error details:\n" + error.ToString() );
                Trace.TraceInformation( "An error occured processing your request. Error details:\n" + error.ToString() );
                return null;
            }
        } // End of: public Project_Group CreateHelper( Project_Group project_group )

        /// <summary>
        /// Helper function that retrieves/parses all saved sub-test results in the WHCK
        /// package being submitted, and saves them to the database.
        /// </summary>
        /// <param name="ID">BCK(Test_Config) Config #(ConfigNumID) identifier at which to insert ALL gathered
        /// package test results.</param>
        /// <param name="context">Current working controller context</param>
        /// <returns>True if the test result additions succeeded, otherwise false.</returns>
        public bool SavePackageSubTestresults( int ID, ref Controller context )
        {
            controllerContext = context;
            bool isCreate = (null == project_groupDB.Test_Config) ? true : false;
            List<string> errorObj = null;
            try
            {
                if( false == isCreate && ID == project_groupDB.Test_Config.ConfigNumID )
                {
                    foreach( Package packageItem in project_groupDB.Test_Config.Packages )
                    {
                        foreach( List<string> testResult in PackageModel.testCollection )
                        {
                            // if( true == TestcaseIsExisting( project_groupDB, testResult[1] ) ) // Test if the current package result matches to a testcase.
                            errorObj = testResult;
                            try
                            {
                                Result result = new Result();
                                result.TestCase = new TestCase();

                                if( testResult.Count < 4 ) // SKIP over any test result entry who's total amount of required data is less than 4 items.
                                    break;
                                if( string.Empty == testResult[ 1 ] )
                                    throw new ApplicationException( "The test case name cannot be empty - ignoring result." );
                                if( string.Empty == testResult[ 2 ] )
                                    throw new ApplicationException( "The test case status cannot be empty - ignoring result." );
                                if( string.Empty == testResult[ 3 ] )
                                    throw new ApplicationException( "The package name cannot be empty - ignoring result." );

                                result.Comment = string.Format("Target Driver Name : \"{0}\", from Package : \"{1}\"", // Set the Target Driver Name as the comment for the test result.
                                    testResult[ 0 ], testResult[ 3 ] );

                                result.TestCase.Name = testResult[ 1 ]; // Set the TestCase Name.

                                result.TestCase.TimeStamp = packageItem.Date_Uploaded; // Set the TestCase TimeStamp.
                                result.Status = new bool();
                                if( "Passed" == testResult[ 2 ] ) // Set the TestCase test status.
                                    result.Status = true;
                                else if( "Failed" == testResult[ 2 ] )
                                    result.Status = false;
                                else if( "Canceled" == testResult[ 2 ] )
                                    result.Comment = string.Concat( "Canceled Result - Not included in status reporting. ", result.Comment );
                                else if( "InQueue" == testResult[ 2 ] )
                                    result.Comment = string.Concat( "InQueue Result - Not included in status reporting. ", result.Comment );
                                else if( "Running" == testResult[ 2 ] )
                                    result.Comment = string.Concat( "Running Result - Not included in status reporting. ", result.Comment );
                                else if( string.Empty == testResult[ 2 ] )
                                    result.Comment = string.Concat( "Invalid Result - Test Status was not given. ", result.Comment );
                                // TODO: Do NOT add the result record UNLESS there is a matching TestCase row!
                                packageItem.Results.Add( result );
                            }
                            catch( ApplicationException error )
                            {
                                Trace.TraceInformation( "Result : \"{0}\" in package : \"{1}\" was ignored because a matching test case was not found.",
                                    (testResult.Count < 4) ? string.Empty : testResult[1], (testResult.Count < 4) ? string.Empty : testResult[3]);
                                FileStream stream = File.Open( context.Server.MapPath( string.Format( "~/App_Data/uploads/{0}.ResultsIgnored.Log", testResult[ 3 ] ) ),
                                    FileMode.Append, FileAccess.Write, FileShare.None );
                                StreamWriter writeLog = new StreamWriter( stream );
                                writeLog.WriteLine( "Result : \"{0}\" in package : \"{1}\" has error. Error Details: \n{2}",
                                    (testResult.Count < 4) ? string.Empty : testResult[1], (testResult.Count < 4) ? string.Empty : testResult[3], error.ToString());
                                writeLog.Close();
                            }
                        }
                        break; // Break this foreach loop: ONLY 1 Package record should exist AFTER that last insert is done.
                    } // End of: foreach( Package packageItem in project_groupDB.Test_Config.Packages )
                } // End of: if( ID == project_groupDB.Test_Config.ConfigNumID )
                if( context.ModelState.IsValid )
                {
                    db.Project_Group.Attach( project_groupDB );
                    db.SaveChanges();
                    return true;
                }
                else
                    return false;
            }
            catch( DbEntityValidationException dbEx )
            {
                foreach( DbEntityValidationResult validationErrors in dbEx.EntityValidationErrors )
                {
                    foreach( DbValidationError validationError in validationErrors.ValidationErrors )
                    {
                        context.Response.Write( "An error occured processing your request. Error details:\n" + validationError.ToString() );
                        Trace.TraceInformation( "Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage );
                    }
                }
                return false;
            }
            catch( DbUpdateException error )
            {
                context.Response.Write( "An error occured processing your request. Error details:\n" + error.ToString() );
                Trace.TraceInformation( "An error occured processing your request. Error details:\n" + error.ToString() );
                return false;
            }
            catch( Exception error )
            {
                context.Response.Write( "An error occured processing your request. Error details:\n" + error.ToString() );
                Trace.TraceInformation( "An error occured processing your request. Error details:\n" + error.ToString() );
                FileStream stream = File.Open( context.Server.MapPath( string.Format( "~/App_Data/uploads/{0}.ResultsIgnored.Log", errorObj[ 3 ] ) ),
                                    FileMode.Append, FileAccess.Write, FileShare.None );
                StreamWriter writeLog = new StreamWriter( stream );
                writeLog.WriteLine( "{} Result : \"{0}\" in package : \"{1}\" was ignored because a required test name, test status, or a package name was not found. Error Details: \n{2}",
                    errorObj[ 1 ], errorObj[ 3 ], error.ToString() );
                writeLog.Close();
                return false;
            }
        } // End of: public void SavePackageSubTestresults( int ID, ref Controller context )
        /// <summary>
        /// Helper function that writes the Test object data to a log file from the
        /// Microsoft.Windows.Kits.Hardware.ObjectModel name space.
        /// </summary>
        /// <param name="test">Windows.Kits.Hardware.ObjectModel.Test object with data to exam.</param>
        /// <param name="packageName">Name of the current processing package.</param>
        public static void TraceWrite( Test test, string packageName )
        {
            FileStream stream = File.Open( controllerContext.Server.MapPath( string.Format( "~/App_Data/uploads/{0}_Exception.Log", packageName ) ),
                FileMode.Append, FileAccess.Write, FileShare.None );
            StreamWriter writeLog = new StreamWriter( stream );
            writeLog.WriteLine( "Line : {0} Result : \"{1}\" with Status : \"{2}\" in Package : \"{3}\" was Logged.",
                logLine, test.Name, test.Status.ToString(), packageName );
            writeLog.Close();
            logLine = logLine + 1;
        }
        /// <summary>
        /// Helper function used by other Helper function SavePackageSubTestresults() that
        /// searches the database for a matching testcase from the specified testcase within
        /// the specified Project_Group database reference under the Packages->Results table,
        /// and returns either true or false.
        /// </summary>
        /// <param name="project_group">Project_Group database reference for which to search for the testcase.</param>
        /// <param name="testcaseNameToFind">The specified testcase Name for which to search for.</param>
        /// <returns></returns>
        public bool TestcaseIsExisting( Project_Group project_group, string testcaseNameToFind )
        {
            bool matchFound = false;
            foreach( Component componentItem in project_group.Components )
                foreach( TestCase testcaseItem in componentItem.TestCases )
                {
                    if( testcaseNameToFind == testcaseItem.Name )
                    {
                        matchFound = true;
                        return matchFound;
                    }
                    else
                    {
                        matchFound = false;
                        continue;
                    }
                }
            return matchFound;
        }
        /// <summary>
        /// Helper function for overriden derived class Dispose() function from
        /// System.Web.Mvc.Controller Abstract Class.
        /// </summary>
        /// <param name="disposing">Dispose the DB connection either true or false.</param>
        public void DisposeHelper( bool disposing )
        {
            db.Dispose();
        }
    } // End of: public class ConfigurationModel : Controller
} // End of: namespace tsiewhckweb.Models