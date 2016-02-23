using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using System.Data.Entity.Validation;

namespace tsiewhckweb.Models
{
    using System.Diagnostics;
    using System.Web.Mvc;

    /// <summary>
    /// ADO.NET entity/model processing logic for TestCaseController class in tsiewhckweb.Models namespace.
    /// </summary>
    public class TestCaseModel
    {
        private tsiewhckwebEntities db;
        private static TestCase staticRefTestcase;

        /// <summary>
        /// TestCaseModel default contructor.
        /// </summary>
        public TestCaseModel()
        {
            db = new tsiewhckwebEntities();
            staticRefTestcase = new TestCase();
        }
        /// <summary>
        /// Helper function for GET: /TestCase/ Index() controller function
        /// </summary>
        /// <returns>List collection of type TestCase as query result return variable.</returns>
        public List<TestCase> IndexHelper()
        {
            List<TestCase> rows = new List<TestCase>(
                db.TestCases
                .Include( i => i.Component )
                .Include( i => i.Component.Project_Group )
                .OrderBy( i => i.TestCaseID ) );
            return rows;
        }
        /// <summary>
        /// Helper function for GET: /TestCase/Details/5
        /// </summary>
        /// <param name="ID">TestCaseID record ID to retrieve.</param>
        /// <returns>View/Result Set row for that single TestCaseID.</returns>
        public TestCase DetailsHelper( int ID = 0 )
        {
            TestCase row = db.TestCases
                .Include( i => i.Component )
                .Include( j => j.Component.Project_Group )
                .Single( k => k.TestCaseID == ID );
            return row;
        }
                /// <summary>
        /// Helper function that provides the Create data access functionality for the TestCase
        /// table and all other related tables for the TestCase Create view in the tsiewhckweb database.
        /// </summary>
        /// <param name="project_group">Input TestCase reference to create or edit a record/row.</param>
        /// <param name="context">Current Working Controller context.</param>
        /// <returns>The modified TestCase tables reference back to the Create or Edit view page.
        /// Returns Null if the specified input is not valid.</returns>
        public TestCase CreateHelper( TestCase testcase, ref Controller control )
        {
            bool isCreate = ( null == testcase.Component ) ? true : false;
            try
            {
                DateTime testcaseTimeStamp = DateTime.Now;
                if( string.Empty == control.Request.Form.Get( "txtProjectName" ) )
                    control.ModelState.AddModelError( "txtProjectName", "Project Group is required." );
                if( string.Empty == control.Request.Form.Get( "txtTestCaseName" ) )
                    control.ModelState.AddModelError( "txtTestCaseName", "TestCase Name is required." );
                if( string.Empty == control.Request.Form.Get( "txtTestCaseTimeStamp" ) )
                    testcaseTimeStamp = DateTime.Now;
                else if( false == DateTime.TryParse( control.Request.Form.Get( "txtTestCaseTimeStamp" ), out testcaseTimeStamp ) )
                    control.ModelState.AddModelError( "txtTestCaseTimeStamp", "TestCase TimeStamp is not in an accepted form. Please try again or use \"yyyyMMddHHmmssffff\" form." );
                if( string.Empty == control.Request.Form.Get( "txtComponentName") )
                    control.ModelState.AddModelError("txtComponentName", "Component Name is required.");

                testcase = staticRefTestcase;
                testcase.Component = new Component();
                testcase.Component.Project_Group = new Project_Group();

                testcase.Component.Project_Group = DbPopulateDefaultVaules( testcase.Component.Project_Group );
                testcase.Component.Project_Group.Name = control.Request.Form.Get("txtProjectName");
                testcase.Name = control.Request.Form.Get("txtTestCaseName");
                testcase.TimeStamp = testcaseTimeStamp;
                testcase.Component.Name = control.Request.Form.Get( "txtComponentName" );

                if( control.ModelState.IsValid )
                {
                    if( true == isCreate )
                        db.TestCases.Add( testcase );
                    else
                        db.TestCases.Attach( testcase );
                    db.SaveChanges();
                    return testcase;
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
                        control.Response.Write( "An error occured processing your request. Error details:\n" + validationError.ToString() );
                        Trace.TraceInformation( "Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage );
                    }
                }
                return null;
            }
            catch( DbUpdateException error )
            {
                control.Response.Write( "An error occured processing your request. Error details:\n" + error.ToString() );
                Trace.TraceInformation( "An error occured processing your request. Error details:\n" + error.ToString() );
                return null;
            }
            catch( Exception error )
            {
                control.Response.Write( "An error occured processing your request. Error details:\n" + error.ToString() );
                Trace.TraceInformation( "An error occured processing your request. Error details:\n" + error.ToString() );
                return null;
            }
        }
        /// <summary>
        /// Helper function that take an IN an OUT by reference parameter to the specific
        /// Project_Group row and all relating rows to be added, and initializes every table
        /// in the current schema with default values.
        /// </summary>
        /// <param name="dbRef">Project_Group database reference to completely populate.</param>
        /// <returns>The pre-populated Project_Group database topmost table reference.</returns>
        private Project_Group DbPopulateDefaultVaules( Project_Group dbRef )
        {
            dbRef = new Project_Group();
            dbRef.Name = "";
            Test_Config test_config = new Test_Config();
            List<Test_Config> containTest = db.Test_Config.ToList();
            int lastIndex = 1;
            test_config.ConfigNumID = lastIndex;
            foreach( Test_Config row in containTest )
            {
                if( row.ConfigNumID == test_config.ConfigNumID )
                {
                    test_config = new Test_Config();
                    lastIndex = row.ConfigNumID + 1;
                    test_config.ConfigNumID = lastIndex;
                }
            }
            test_config.Driver_Config = new Driver_Config();
            test_config.Driver_Config.BKC_Version = "";
            test_config.Driver_Config.BT_Driver = "";
            test_config.Driver_Config.GPS_Driver = "";
            test_config.Driver_Config.NFC_Driver = "";
            test_config.Driver_Config.SoftAP = "";
            test_config.Driver_Config.Team = "";
            test_config.Driver_Config.WiFi_Driver = "";
            test_config.Machine_Config = new Machine_Config();
            test_config.Machine_Config.HW_Version = "";
            test_config.Machine_Config.WHCK_Version = "";
            test_config.Machine_Config.Windows_Build_Num = "";
            dbRef.Test_Config = test_config;
            return dbRef;
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
    } // End of: public class TestCaseModel
} // End of: namespace tsiewhckweb.Models