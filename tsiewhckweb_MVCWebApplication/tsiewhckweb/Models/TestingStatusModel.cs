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
    /// ADO.NET entity/model processing logic for TestingStatusController class in tsiewhckweb.Models namespace.
    /// </summary>
    public class TestingStatusModel
    {
        private tsiewhckwebEntities db;
        /// <summary>
        /// Public property read only reference to private datamember tsiewhckwebEntities TestingStatusModel.db.
        /// </summary>
        public tsiewhckwebEntities DB
        {
            get { return db; }
        }
        private Dictionary<string,int[]> passFailStats;
        /// <summary>
        /// Public property read/write reference to private datamember Dictionary<string,Stat> passFailStats.
        /// </summary>
        public Dictionary<string,int[]> PassFailStats
        {
            get { return passFailStats; }
            set { passFailStats = value; }
        }

        /// <summary>
        /// TestCaseModel default contructor.
        /// </summary>
        public TestingStatusModel()
        {
            db = new tsiewhckwebEntities();
            passFailStats = new Dictionary<string,int[]>();
        }
        /// <summary>
        /// TestingStatusModel helper function responsible for calculating the Pass/Fail percentage of a
        /// given Project Group Name.
        /// </summary>
        /// <param name="con">The current working Controller (TestingStatus) context for this function.</param>
        /// <returns>A Dictionary structure key+value pairs that represent each Project Group Name and
        /// their associated passing/failing counters for the percentage calculation.</returns>
        public Dictionary<string, int[]> GetProjectPassing( ref Controller con )
        {
            if (null != passFailStats)
                passFailStats.Clear();
            else
                passFailStats = new Dictionary<string, int[]>();

            string projectName = "";
            bool passOrFailStat = false;
            int[] add = new int[] { 0, 0 }; // 0 index is int passed, and 1 index is int failed
            var dataView = GetTestCasesWithStatus();

            foreach( Project_Group project_group in dataView )
                foreach( Component component in project_group.Components )
                    foreach( TestCase testcase in component.TestCases )
                        foreach( Result result in testcase.Results )
                        {
                            if( result.TestCaseID == testcase.TestCaseID &&
                                string.Empty != testcase.Name &&
                                string.Empty != result.Comment &&
                                false == result.Comment.Contains( "Canceled Result" ) &&
                                false == result.Comment.Contains( "InQueue Result" ) &&
                                false == result.Comment.Contains( "Running Result" ) &&
                                false == result.Comment.Contains( "Invalid Result" ) )
                            {
                                projectName = project_group.Name;
                                passOrFailStat = result.Status;

                                if( false == passFailStats.Keys.Contains( projectName ) )
                                    passFailStats.Add( projectName, add ); // Initialize the first result entry if needed.
                                if( true == passOrFailStat )
                                {
                                    add = passFailStats[ projectName ];
                                    add[ 0 ] = add[ 0 ] + 1;
                                    passFailStats[ projectName ] = add;
                                }
                                else if( false == passOrFailStat )
                                {
                                    add = passFailStats[ projectName ];
                                    add[ 1 ] = add[ 1 ] + 1;
                                    passFailStats[ projectName ] = add;
                                }
                            }
                        }

            con.HttpContext.Cache[ "ProjectStats" ] = passFailStats;
            return passFailStats;
        }
        /// <summary>
        /// TestingStatusModel helper function responsible for calculating the Pass/Fail percentage of a
        /// given Driver Name.
        /// </summary>
        /// <param name="con">The current working Controller (TestingStatus) context for this function.</param>
        /// <returns>A Dictionary structure key+value pairs that represent each Driver Name and
        /// their associated passing/failing counters for the percentage calculation.</returns>
        public Dictionary<string, int[]> GetDriverPassing( ref Controller con )
        {
            if( null != passFailStats )
                passFailStats.Clear();
            else
                passFailStats = new Dictionary<string,int[]>();
            passFailStats.Add( "BT Driver", new int[ 2 ] { 0, 0 } );
            passFailStats.Add( "WiFi Driver", new int[ 2 ] { 0, 0 } );
            passFailStats.Add( "NFC Driver", new int[ 2 ] { 0, 0 } );
            passFailStats.Add( "GPS Driver", new int[ 2 ] { 0, 0 } );

            int[] add = new int[2] { 0, 0 }; // 0 index is int passed, and 1 index is int failed
            bool passOrFailStat = false;
            var dataView = GetTestCasesWithStatus();
            foreach( Project_Group project_group in dataView )
                foreach( Package package in project_group.Test_Config.Packages )
                    foreach( Result result in package.Results )
                    {
                        if( result.TestCaseID == result.TestCase.TestCaseID &&
                            string.Empty != result.TestCase.Name &&
                            string.Empty != result.Comment &&
                            false == result.Comment.Contains( "Canceled Result" ) &&
                            false == result.Comment.Contains( "InQueue Result" ) &&
                            false == result.Comment.Contains( "Running Result" ) &&
                            false == result.Comment.Contains( "Invalid Result" ) )
                        {
                            passOrFailStat = result.Status;

                            if( true == passOrFailStat && string.Empty != package.Test_Config.Driver_Config.BT_Driver )
                            {
                                add = passFailStats[ "BT Driver" ];
                                add[ 0 ] = add[ 0 ] + 1;
                                passFailStats[ "BT Driver" ] = add;
                            }
                            else if( false == passOrFailStat && string.Empty != package.Test_Config.Driver_Config.BT_Driver )
                            {
                                add = passFailStats[ "BT Driver" ];
                                add[ 1 ] = add[ 1 ] + 1;
                                passFailStats[ "BT Driver" ] = add;
                            }

                            if( true == passOrFailStat && string.Empty != package.Test_Config.Driver_Config.GPS_Driver )
                            {
                                add = passFailStats[ "GPS Driver" ];
                                add[0] = add[0] + 1;
                                passFailStats[ "GPS Driver" ] = add;
                            }
                            else if( false == passOrFailStat && string.Empty != package.Test_Config.Driver_Config.GPS_Driver )
                            {
                                add = passFailStats[ "GPS Driver" ];
                                add[1] = add[1] + 1;
                                passFailStats[ "GPS Driver" ] = add;
                            }

                            if( true == passOrFailStat && string.Empty != package.Test_Config.Driver_Config.NFC_Driver )
                            {
                                add = passFailStats[ "NFC Driver" ];
                                add[0] = add[0] + 1;
                                passFailStats[ "NFC Driver" ] = add;
                            }
                            else if( false == passOrFailStat && string.Empty != package.Test_Config.Driver_Config.NFC_Driver )
                            {
                                add = passFailStats[ "NFC Driver" ];
                                add[1] = add[1] + 1;
                                passFailStats[ "NFC Driver" ] = add;
                            }

                            if( true == passOrFailStat && string.Empty != package.Test_Config.Driver_Config.WiFi_Driver )
                            {
                                add = passFailStats[ "WiFi Driver" ];
                                add[0] = add[0] + 1;
                                passFailStats[ "WiFi Driver" ] = add;
                            }
                            else if( false == passOrFailStat && string.Empty != package.Test_Config.Driver_Config.WiFi_Driver )
                            {
                                add = passFailStats[ "WiFi Driver" ];
                                add[1] = add[1] + 1;
                                passFailStats[ "WiFi Driver" ] = add;
                            }
                        }
                    }
            con.HttpContext.Cache[ "DriverStats" ] = passFailStats;
            return passFailStats;
        }
        /// <summary>
        /// TestingStatusModel helper function responsible for calculating the Pass/Fail percentage of a
        /// given Test Case Name.
        /// </summary>
        /// <param name="con">The current working Controller (TestingStatus) context for this function.</param>
        /// <returns>A Dictionary structure key+value pairs that represent each TestCase.Name and
        /// their associated passing/failing counters for the percentage calculation.</returns>
        public Dictionary<string,int[]> GetTestCasePassing( ref Controller con )
        {
            if( null != passFailStats )
                passFailStats.Clear();
            else
                passFailStats = new Dictionary<string,int[]>();

            string testName = "";
            bool passOrFailStat = false;
            int[] add = new int[] { 0, 0 }; // 0 index is int passed, and 1 index is int failed
            var dataView = GetTestCasesWithStatus();

            foreach( Project_Group project_group in dataView )
                foreach( Component component in project_group.Components )
                    foreach( TestCase testcase in component.TestCases )
                        foreach( Result result in testcase.Results )
                        {
                            if( result.TestCaseID == testcase.TestCaseID &&
                                string.Empty != testcase.Name &&
                                string.Empty != result.Comment &&
                                false == result.Comment.Contains( "Canceled Result" ) &&
                                false == result.Comment.Contains( "InQueue Result" ) &&
                                false == result.Comment.Contains( "Running Result" ) &&
                                false == result.Comment.Contains( "Invalid Result" ) )
                            {
                                testName = testcase.Name;
                                passOrFailStat = result.Status;

                                if( true == passOrFailStat && false == passFailStats.Keys.Contains( testName ) )
                                {
                                    add = new int[] { 1, 0 };
                                    passFailStats.Add( testName, add );
                                }
                                else if( true == passOrFailStat && true == passFailStats.Keys.Contains( testName ) )
                                {
                                    add = passFailStats[ testName ];
                                    add[ 0 ] = add[ 0 ] + 1;
                                    passFailStats[ testName ] = add;
                                }
                                if( false == passOrFailStat && false == passFailStats.Keys.Contains( testName ) )
                                {
                                    add = new int[] { 0, 1 };
                                    passFailStats.Add( testName, add );
                                }
                                else if( false == passOrFailStat && true == passFailStats.Keys.Contains( testName ) )
                                {
                                    add = passFailStats[ testName ];
                                    add[ 1 ] = add[ 1 ] + 1;
                                    passFailStats[ testName ] = add;
                                }
                            }
                        }
            con.HttpContext.Cache[ "TestCaseStats" ] = passFailStats;
            return passFailStats;
        }
        /// <summary>
        /// Helper function that queries the db object of the tsiewhckwebEntities model,
        /// and returns a DbSet object query to all Project_Group records that have at least
        /// one valid TestCase record this is associated with a valid Result record.
        /// </summary>
        /// <returns>The DbQuery object with the joined result records needing to calculate.</returns>
        private DbQuery<Project_Group> GetTestCasesWithStatus()
        {
            DbQuery<Project_Group> dataView = ( DbQuery<Project_Group> ) db.Project_Group
                .Include( i => i.Components )
                .Include( i => i.Components.Select( j => j.TestCases ) )
                .Include( i => i.Components.Select( j => j.TestCases.Select( k => k.Results ) ) )
                .Include( i => i.Test_Config )
                .Include( i => i.Test_Config.Driver_Config )
                .Include( i => i.Test_Config.Packages.Select( l => l.Results ) )
                .Include( i => i.Test_Config.Packages.Select( l => l.Results.Select( m => m.TestCase ) ) )
                .OrderBy( i => i.ConfigNumID )
                .Distinct();
            return dataView;
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
    }
}