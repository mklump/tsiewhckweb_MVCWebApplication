using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tsiewhckweb.Models
{
    using Microsoft.Windows.Kits.Hardware.ObjectModel;
    using Microsoft.Windows.Kits.Hardware.ObjectModel.Submission;
    using Microsoft.Windows.Kits.Hardware.Logging;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public class PackageModel
    {
        public string idConfiguration;
        public string checksum;
        public string dateUploaded;
        public string fileName;
        public string path;
        public string idUser;
        public string whckVersion;

        /// <summary>
        /// Matt added to avoid dateUploaded DB type conversion AND model conversion.
        /// </summary>
        public DateTime datetimeRef;
        /// <summary>
        /// Matt added [Element 1] Test->"Target Driver Name",
        ///            [Element 2] Test->"Test Case Name",
        ///            [Element 3] Test->"Test Case Status",
        ///            [Element 4] Test->"Package Name",
        ///            as a jagged two dimentional generic List of strings.
        /// </summary>
        public static List<List<string>> testCollection;

        /// <summary>
        /// PackageModel class default constructor.
        /// </summary>
        public PackageModel()
        {
            datetimeRef = new DateTime();
            if( null == testCollection )
                testCollection = new List<List<string>>();
        }

        /// <summary>
        /// Retrieves all top level meta-data about the specified WHCK test results package.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public String WhckVersion( HttpPostedFileBase file )
        {
            if (file.ContentLength > 0)
            {
                var fileName = System.IO.Path.GetFileName(file.FileName);
                var path = System.IO.Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/App_Data/uploads"), fileName);

                // ensure directory exists
                var dirPath = System.IO.Directory.GetParent(path).ToString();
                if (!System.IO.Directory.Exists(dirPath))
                {
                    System.IO.Directory.CreateDirectory(dirPath);
                }

                // ensure file doesn't already exist, by name
                while (System.IO.File.Exists(path))
                {
                    path = dirPath + @"\" + System.IO.Path.GetFileNameWithoutExtension(path) + "0" + System.IO.Path.GetExtension(path);
                }

                // save attachment
                file.SaveAs(path);

                // ensure file doesn't have the same checksum.
                /// note: database required for full implementation
                this.checksum = GetMD5HashFromFile(path);

                // populate other member variables
                this.path = path;
                this.fileName = System.IO.Path.GetFileName(path);

                // Matt added to avoid dateUploaded DB type conversion AND model conversion.
                datetimeRef = DateTime.Now;
                this.dateUploaded = datetimeRef.ToString( "yyyyMMddHHmmssffff" );

                // get package results
                return String.Format("\n{0}\nChecksum: {1}\nWHCK version: {2}\nTimestamp: {3}\n", this.fileName, this.checksum, this.whckVersion, this.dateUploaded) + getPackageInfo(path);
            }

            return null;
        }

        /// <summary>
        /// Retrieves all required test data from a specified WHCK result package.
        /// </summary>
        /// <param name="packagePath">Path to WHCK test package</param>
        /// <returns>All required test data about the specified test package.</returns>
        protected string getPackageInfo( string packagePath )
        {
            StringBuilder returnString = new System.Text.StringBuilder();

            // load package
            PackageManager manager = new PackageManager(packagePath);

            // print package version
            this.whckVersion = manager.VersionString;

            // get only one project name, assuming that packages only contain one project
            String projectName = manager.GetProjectNames()[0];
            returnString.AppendFormat("\nProject name: {0}", projectName);

            // get total passes and total failures
            ProjectInfo projectInfo = manager.GetProjectInfo(projectName);
            int projectPass = projectInfo.PassedCount;
            int projectFail = projectInfo.FailedCount;
            int projectRunning = projectInfo.RunningCount;
            returnString.AppendFormat("\nPass: {0}\nFail: {1}\nRunning: {2}", projectPass, projectFail, projectRunning);

            // list all the tests for each project
            returnString.AppendFormat("\nGetting all projects and their tests");
            foreach (string name in manager.GetProjectNames())
            {
                Project project = manager.GetProject(name);

                returnString.AppendFormat("\nProject name : {0}, status: {1}", project.Name, project.Info.Status);

                foreach (ProductInstance pi in project.GetProductInstances())
                {
                    if (pi.Name != null && pi.OSPlatform.Description != null)
                    {
                        returnString.AppendFormat("\nProduct Instance : {0}, Platform type : {1}", pi.Name, pi.OSPlatform.Description);
                    }

                    // exclude tests that have not been run
                    foreach( Target target in pi.GetTargets() )
                    {
                        returnString.AppendFormat("\nTarget Name : {0}", target.Name);
                        int index = 0; // Matt Added index to testCollection structure variable.

                        foreach( Test test in target.GetTests() )
                        {
                            if( test.Status != TestResultStatus.NotRun )
                            {
                                returnString.AppendFormat( "\n\tTest : {0}, status : {1}", test.Name, test.Status );

                                // Target driver can be the empty string or it might be null.
                                string targetName = ( string.Empty != target.Name && null != target.Name ) ? target.Name : string.Empty;
                                testCollection.Add( new List<string>(new string[] { targetName } ) ); // Matt added for Driver Target Name retrival.

                                // Test Name might be the empty string or null, but should never be either of them.
                                string testName = ( string.Empty != test.Name && null != test.Name ) ? test.Name : string.Empty;
                                testCollection[ index ].Add( testName ); // Matt added to get the TestCase Name

                                switch( test.Status ) // Matt added switch statement to get the pass or fail status.
                                {
                                    case TestResultStatus.Passed:
                                        testCollection[ index ].Add( "Passed" );
                                        break;
                                    case TestResultStatus.Failed:
                                        testCollection[ index ].Add( "Failed" );
                                        break;
                                    case TestResultStatus.Canceled:
                                        testCollection[ index ].Add( "Canceled" );
                                        break;
                                    case TestResultStatus.InQueue:
                                        testCollection[ index ].Add( "InQueue" );
                                        break;
                                    case TestResultStatus.Running:
                                        testCollection[ index ].Add( "Running" );
                                        break;
                                    case TestResultStatus.NotRun: // Fall through to default case.
                                    default:
                                        testCollection[ index ].Add( string.Empty );
                                        break;
                                }
                                string packageName = fileName.TrimEnd( ".hckx".ToCharArray() );
                                packageName = packageName.TrimEnd( new char[] { '0' } );

                                // The package name might be the empty string or null, but again it should never be.
                                packageName = ( string.Empty != packageName && null != packageName ) ? packageName : string.Empty;
                                testCollection[ index ].Add( packageName ); // Matt added for Package Name same as the FileName retrival.

                                index = index + 1; // Increment index counter to the next test result entry.

                                ConfigurationModel.TraceWrite( test, packageName );
                            }
                        } // End of: foreach( Test test in target.GetTests() )
                    }
                }
            }

            return returnString.ToString();
        }
        /// <summary>
        /// Calculate checksum (reference: http://sharpertutorials.com/calculate-md5-checksum-file/)
        /// </summary>
        /// <param name="fileName">Input file name of which to calculate the required MD5 Hash for.</param>
        /// <returns>Required MD5 Hash values.</returns>
        protected string GetMD5HashFromFile( string fileName )
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}