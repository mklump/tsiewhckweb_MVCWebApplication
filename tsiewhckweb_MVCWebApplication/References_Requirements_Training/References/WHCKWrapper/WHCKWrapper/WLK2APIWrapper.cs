using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Windows.Kits.Hardware.ObjectModel;
using Microsoft.Windows.Kits.Hardware.ObjectModel.DBConnection;
using Microsoft.Windows.Kits.Hardware.ObjectModel.Submission;
using System.Collections.ObjectModel;
using System.Xml;
using System.Threading;
using System.IO;
using System.Management;
using System.Collections.Specialized;
using System.Collections;
using System.Threading.Tasks;
using System.Text.RegularExpressions;



namespace Intel.WHQLCert
{


    public struct wlkMachinePool
    {
        public string Name;
        public List<wlkMachine> machines;

    }
    public struct wlkMachine
    {
        public string Name;
        public string OSName;
        public string Status;
        public string LastHeartbeat;
    }
    public struct wlkTestResult
    {
        public int ResultID;
        public string GUID;
        public wlkTest Test;
        public List<wlkTask> Tasks;
        public List<wlkTestParameter> TestParameters;
        public DateTime StartTime;
        public string MachineName;
        public string Status;
        public DateTime CompletionTime;
        public List<wlkFilter> Filters;
    }
    public struct wlkTask
    {
        public string Stage;
        public string TaskName;
        public string TaskType;
        public string TaskCommandLine;
        public string Status;
        public List<wlkTask> Tasks;
        public List<string> Logs;
        public List<wlkFilter> Filters;
    }
    public struct wlkTestParameter
    {
        public string Name;
        public string DefaultValue;
        public string ActualValue;
    }
    public struct wlkTest
    {
        public int TestID;
        public string TestGUID;
        public string TestInstanceID;
        public string Name;
        public string ScheduleOptions;
        public TimeSpan EstimatedRuntime;
        public String Description;
        public String Status;
        public String ExecutionState;
        public string Type;
        public List<wlkTestResult> Results;
        public List<wlkRequirment> Requirments;
        public bool Selected;
    }
    public struct wlkTarget
    {
        public string Type;
        public string Key;
        public int TestsCount;
        public wlkMachine Machine;
        public List<wlkTest> Tests;
    }
    public struct wlkTargetFamily
    {
        public string Name;
        public List<wlkTarget> Targets;
    }
    public struct wlkProductInstance
    {
        public string Name;
        public List<wlkTarget> Targets;
        public List<wlkTargetFamily> TargetFamilies;
    }
    public struct wlkProject
    {
        public string Name;
        public string Status;
        public int TotalCount;
        public int NotRunCount;
        public int PassedCount;
        public int FailedCount;
        public int RunningCount;
        public int TotalRunCount; 
        public List<wlkProductInstance> ProductInstances;
        public bool ContainsInstance(string name)
        {
            foreach (wlkProductInstance ins in ProductInstances)
            {
                if (ins.Name == name)
                    return true;
            }
            return false;
        }
        public DateTime lastUpdate;
    }
    public struct wlkRequirment
    {
        public string Name;
        public string FullName;
        public string Feature;

    }
    public struct wlkFeature
    {
        public string Name;
        public string FullName;
        public string Feature;

    }
    public struct wlkProductType
    {
        public string Name;
        public string Description;
        public List<wlkFeature> Features;
    }
    public struct wlkFilter
    {
        public List<string> Constraints; //This property represents the filter constraints for the filter.
        public DateTime ExpirationDate; //This property represents the expiration date for the filter.
        public int FilterNumber;        //This property represents the filter ID number. The Filter ID number uniquely identifies a filter within the HCK.
        public bool IsLogRequired;        //This property determines whether the logs are required for this filter to be applicable.
        public bool IsResultRequired;        //This property represents determines whether the results are required for this filter to be applicable.
        public string IssueDescription;        //This property represents the issue description for the filter.
        public string IssueResolution;        //This property represents the issue resolution for the filter.
        public DateTime LastModifiedDate;        //This property represents the last modified date for the filter.
        //public List<string> LogNodes;        //This property represents the filter log nodes for the filter.
        public bool ShouldFilterAllZeros;        //This property determines whether the filter should be applied if the pass and fail count are both zero.
        public bool ShouldFilterNotRuns;        //This property determines whether the filter should be applied for not run task results.
        public string Status;        //This property represents the status for the filter.
        public string TestCommandLine;        //This property represents the command line for the test being filtered.
        public string Title;        //This property represents the filter title value (string).
        public string Type;        //This property represents the filter type for the given filter.
        public int Version;        //This property represents the version for the filter.
    }

    public class OMControllerObject
    {
        public OMControllerObject(String Name, WHCKLogger logger)
        {
            ControllerName = Name;
            Logger = logger;
            _coProjectManager = null;
            TargetLoock = new object();
            EnvironmentLock = new object();
            TargetLoockStartTime = DateTime.MaxValue;
            openWRConnections = 0;
            isTargeting = 0;
            Methods = new Dictionary<string, int>();
            Targets = new Dictionary<string, wlkTarget>();
            prTests = new Dictionary<string, List<wlkTest>>();
            TestsCountByHWID = new Dictionary<string, Dictionary<string, KeyValuePair<string, int>>>();
            Pools = new List<wlkMachinePool>();
            _pool = new Semaphore(50, 50);
        }
        public ProjectManager coProjectManager
        {
            get {
                try
                {
                    ConnectionType ct;
                    if(_coProjectManager!=null)
                        ct = _coProjectManager.ConnectionType;
                }
                catch (ObjectDisposedException ex)
                {
                    _coProjectManager = null;
                    if (Logger != null)
                        Logger.OutputLine(String.Format("ERROR in coProjectManager: {0} \n {1}", ex.Message, ex.StackTrace), 1);
                }
                return _coProjectManager; 
            }
            set { _coProjectManager = value; }
        }
        public String ControllerName;
        public List<wlkProject> Projects;
        public List<wlkMachinePool> Pools;
        public Dictionary<string, List<wlkTest>> prTests;
        public Dictionary<string, wlkTarget> Targets;
        public Dictionary<string, int> Methods;
        public Dictionary<string, Dictionary<string, List<string>>> TestsToRun; //Dic<PI, Dic<Machine, List<test>>>
        public Dictionary<string, Dictionary<string, KeyValuePair<string, int>>> TestsCountByHWID; //Dic<PI, Dic<HWID, Pair<targetName, countOfTests>>>
        public object TargetLoock;
        public DateTime TargetLoockStartTime;
        public string currentTarget;
        public object EnvironmentLock;
        public int openWRConnections;
        public int isTargeting;

        public WHCKLogger Logger = null;

        private Semaphore _pool;
        private ProjectManager _coProjectManager;
        private int availableROConnections;
        public ProjectManager getRO_ProjectManager()
        {
            _pool.WaitOne();
            ProjectManager _pm = new DatabaseProjectManager(ControllerName);
            return _pm;
        }

        public int releaseRO_ProjectManager(ref ProjectManager _pm)
        {
            if (_pm != null)
                _pm.Dispose();
            _pm=null;
            if(availableROConnections<50)
                availableROConnections=_pool.Release();
            return availableROConnections;
        }

        public void cleanCacheDTO(string project)
        {
            try
            {
                Projects = null;
                Pools = null;
                if (prTests!=null && prTests.ContainsKey(project))
                    prTests.Remove(project);
                if (Targets != null)
                    Targets.Clear();
                if (TestsToRun!=null && TestsToRun.ContainsKey(project))
                    TestsToRun.Remove(project);
                if (TestsCountByHWID!=null && TestsCountByHWID.ContainsKey(project))
                    TestsCountByHWID.Remove(project);
            }
            catch (Exception ex)
            {
                if (Logger != null)
                    Logger.OutputLine(String.Format("ERROR in cleanCacheDTO: {0} \n {1}", ex.Message, ex.StackTrace), 1);
            }
        }
    }

    public enum OMAccessType
    {
        Fresh,
        ReadOnly,
        Write
    }
    public class WLK2OMWrapper
    {
        private static Dictionary<string, OMControllerObject> myControllersObjects = new Dictionary<string, OMControllerObject>(); 
        private static Dictionary<string, object> myEnvironmentLock = new Dictionary<string, object>();
        private static object mySingleObjectLock = new object();
        private static String _TestLogsSharePath;
        private ProjectManager _projectManager;
        private Project logoProject;
        private PackageWriter packageWriter=null;
        private const int TARGET_TIMEOUT_MIN=10;
        private const int RETRIES = 5;
        private const string SYSTEM_DEVICE_FAMILY_NAME = "[SYSTEM]";
        private OMAccessType _myConnectionType;

        public string ControllerName;
        public static Dictionary<string, string> packageStatus=new Dictionary<string,string>();
        public String TestLogsSharePath
        {
            get { return _TestLogsSharePath; }
            set {
                _TestLogsSharePath = value.Trim();
                if (!String.IsNullOrEmpty(_TestLogsSharePath))
                    _TestLogsSharePath = _TestLogsSharePath.EndsWith(@"\") ? _TestLogsSharePath : _TestLogsSharePath + @"\";
            }
        }
        
        private WHCKLogger logger = new  WHCKLogger();
        private string logFile = "c://UI_LOGS//WHCK2//{0}.txt";

        private enum objectType { prManager=0 , lProject=2, pckWriter=4}

        private ProjectManager projectManager
        {
            get
            {   
                if (_projectManager == null)
                    ConnectToController(ControllerName);
                return _projectManager;
            }
        }

        public WLK2OMWrapper()
        {
            _projectManager = null;
            logoProject = null;
            logger._LogFile = String.Format(logFile,"__WHCK");
            logger._LogLevel = 3;
        }

        #region -- GeneraL ---
        public void ConnectToController(string controllerName, OMAccessType connectionType=OMAccessType.Write)
        {
            _myConnectionType = connectionType;
            bool fresh = connectionType == OMAccessType.Fresh;
            if(String.IsNullOrEmpty(controllerName))
            {
                logger.OutputLine("Conroller Name is Empty", 1);
                throw new ArgumentException("Conroller Name is Empty");
            }

            ControllerName = controllerName;

            //CREATE Controller object
            if (!myControllersObjects.ContainsKey(controllerName))
            {
                lock (mySingleObjectLock)
                {
                    if (!myControllersObjects.ContainsKey(controllerName))
                        myControllersObjects.Add(controllerName, new OMControllerObject(controllerName, logger));
                }
            }

            int retries=5;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    logger._LogFile = String.Format(logFile, controllerName);
                    logger._LogName = controllerName;

                    switch (connectionType)
                    {
                        case OMAccessType.ReadOnly:
                           _projectManager = myControllersObjects[ControllerName].getRO_ProjectManager();
                            break;
                        default:
                            getWRConnection(controllerName, fresh);
                            break;

                    }
                    if (_projectManager != null)
                        break;
                    else
                        throw new ArgumentNullException(" Trying to access NULL ProjectManager"); 
                }
                catch (Exception ex)
                {
                    logger.OutputLine(String.Format("Error : {0} \r\n {1}",ex.Message,ex.StackTrace),1);
                    if(i==0)
                        checkEnterpriseXML(controllerName);
                    if (i == retries - 1)
                        throw;
                    else
                        Thread.Sleep(5*1000);
                }
            }
        }

        private void getWRConnection(string controllerName, bool fresh)
        {
            bool useExists = true;

            if (fresh || myControllersObjects[controllerName].coProjectManager == null || myControllersObjects[controllerName].openWRConnections == 0)
            {
                lock (myControllersObjects[controllerName].EnvironmentLock)
                {
                    if (fresh || myControllersObjects[controllerName].coProjectManager == null || myControllersObjects[controllerName].openWRConnections == 0)
                    {
                        logger.OutputLine(String.Format("Open new Conection currConn: {0} {1} {2}", fresh ? "fresh" : "", myControllersObjects[controllerName].coProjectManager == null ? "NULL" : "", myControllersObjects[ControllerName].openWRConnections), 3);
                        _projectManager = new DatabaseProjectManager(controllerName);
                        myControllersObjects[controllerName].coProjectManager = _projectManager;
                        myControllersObjects[controllerName].openWRConnections = 1;
                        useExists = false;
                    }
                }
            }
            if (useExists)
            {
                _projectManager = myControllersObjects[controllerName].coProjectManager;
                Interlocked.Increment(ref myControllersObjects[ControllerName].openWRConnections);
            }
        }

        public void checkEnterpriseXML(string controllerName)
        {
            String path=String.Format(@"C:\Windows\SysWOW64\config\systemprofile\AppData\Roaming\WindowsLogoKit\Enterprise\{0}\",controllerName);
            String tmplPath = @"c:\UI_TOOLS\";
            String file="Enterprise.xml";
            if (File.Exists(path + file))
                File.Delete(path + file);
                //return;
            Directory.CreateDirectory(path);
            StreamReader sr = new StreamReader(tmplPath + file);
            StreamWriter sw = new StreamWriter(path + file);
            sw.Write(sr.ReadToEnd().Replace("AUTO-WLK-2", controllerName));
            sw.Close();
            sr.Close();

        }

        public void DisconnectFromController()
        {
            if (_projectManager != null)
            {
                switch (_myConnectionType)
                {
                    case OMAccessType.ReadOnly:
                        int ac = myControllersObjects[ControllerName].releaseRO_ProjectManager(ref _projectManager);
                        logger.OutputLine(String.Format("Available Read Connect: {0}", ac), 3);
                        break;
                    default:
                        if (myControllersObjects[ControllerName].openWRConnections > 1)
                            logger.OutputLine(String.Format("Concurent connections : {0}", myControllersObjects[ControllerName].openWRConnections), 3);
                        Interlocked.Decrement(ref myControllersObjects[ControllerName].openWRConnections);
                        break;
                }
            }
        }

        public void resetCachedConnection(string controllerName)
        {
            if (myControllersObjects[controllerName].coProjectManager != null /*|| myControllersObjects[controllerName].connections == 0*/)
            {
                logger.OutputLine(String.Format("Trying to reset connection to : {0}", controllerName), 3);
                lock (myControllersObjects[controllerName].EnvironmentLock)
                {
                    ProjectManager pm = myControllersObjects[controllerName].coProjectManager;
                    pm.Dispose();
                    myControllersObjects[controllerName].coProjectManager = null;
                    _projectManager = null;
                }
            }
        }

        public string getControllerVersion()
        {
            checkForNotEmpty(objectType.prManager);
            //return string.Format("{0}.{1}.{2}", projectManager.Version.Major, projectManager.Version.Minor, projectManager.Version.Build); 
            return projectManager.VersionString;
        }

        private void checkForNotEmpty(objectType type)
        {
            if (projectManager == null)
                throw new ArgumentNullException("Logo Manager is NULL");

            if (type == objectType.lProject && logoProject == null)
                throw new ArgumentNullException("Project is NULL");

        }

        private int createMethodKey(string methodName)
        {
            int result = 0;
            if (!myControllersObjects[ControllerName].Methods.ContainsKey(methodName))
            {
                lock (mySingleObjectLock)
                {
                    if (!myControllersObjects[ControllerName].Methods.ContainsKey(methodName))
                        myControllersObjects[ControllerName].Methods.Add(methodName, 0);
                }
            }
            else result = myControllersObjects[ControllerName].Methods[methodName];

            return result;
        }

        private string setTargetingInfo(bool increment, String MachineName)
        {
            string target = string.Empty;
            lock (myControllersObjects[ControllerName].TargetLoock)
            {
                target = myControllersObjects[ControllerName].currentTarget;
                Interlocked.Exchange(ref myControllersObjects[ControllerName].isTargeting, increment ? 1 : 0);
                myControllersObjects[ControllerName].TargetLoockStartTime = increment ? DateTime.Now : DateTime.MaxValue;
                myControllersObjects[ControllerName].currentTarget = increment ? MachineName : String.IsNullOrEmpty(target) ? String.Empty : MachineName;
            }
            return target;
        }

        #endregion

        #region --- DeviceFamily ---

        public DeviceFamily GetDeviceFamily(string name)
        {
            DeviceFamily dfRet = null;
            ReadOnlyCollection<DeviceFamily> families = projectManager.GetDeviceFamilies();
            foreach (DeviceFamily df in families)
            {
                if (df.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                {
                    dfRet = df;
                }
            }
            return dfRet;
        }

        public void createDeviceFamily(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException(filename + ": XML not found");
            XmlReader w = XmlReader.Create(filename);
            List<string> hardwareIds = new List<string>();

            w.ReadStartElement("family");
            w.ReadStartElement("DeviceFamily");
            string name = w.ReadContentAsString();
            w.ReadEndElement();

            while (w.Read())
            {
                switch (w.NodeType)
                {
                    default:
                        {
                            w.ReadStartElement("HWID");
                            string id = w.ReadContentAsString();
                            hardwareIds.Add(id);
                            w.ReadEndElement();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        w.ReadEndElement();
                        break;
                }
            }

            foreach (DeviceFamily df in projectManager.GetDeviceFamilies())
            {
                if (df.Name.Equals(name))
                {
                    Console.WriteLine("Deleting existing " + name + " Device Family.");
                    projectManager.DeleteDeviceFamily(name);
                }
                else
                {
                    foreach (string hdid in hardwareIds)
                    {
                        if (df.Name.Equals(hdid))
                        {
                            Console.WriteLine("Deleting existing " + name + " Device Family.");
                            projectManager.DeleteDeviceFamily(df.Name);
                        }
                    }
                }
            }

            projectManager.CreateDeviceFamily(name, hardwareIds);
            this.listDeviceFamilies();
        }

        public void createDeviceFamily(string name, string[] Hwids)
        {
            checkForNotEmpty(objectType.prManager);

            ReadOnlyCollection<DeviceFamily> families = projectManager.GetDeviceFamilies();
            foreach (DeviceFamily df in families)
            {
                if (df.Name.Equals(name))
                {
                    Console.WriteLine("Deleting existing " + name + " Device Family.");
                    projectManager.DeleteDeviceFamily(name);
                }
            }
            projectManager.CreateDeviceFamily(name, Hwids);
        }

        public Dictionary<string, Dictionary<string, List<string>>> listDeviceFamilies()
        {
            Dictionary<string, Dictionary<string, List<string>>> dFamilies = new Dictionary<string, Dictionary<string, List<string>>>();

            checkForNotEmpty(objectType.prManager);

            ReadOnlyCollection<DeviceFamily> families = projectManager.GetDeviceFamilies();

            foreach (DeviceFamily df in families)
            {
                dFamilies.Add(df.Name, new Dictionary<string, List<string>>());
                dFamilies[df.Name].Add(df.Name, new List<string>());
                Console.WriteLine("Device Family: " + df.Name);
                foreach (string hwid in df.HardwareIds)
                {

                    dFamilies[df.Name][df.Name].Add(hwid);
                    Console.WriteLine("\tHardware ID: " + hwid);
                }

            }
            return dFamilies;
        }

        public void ExportDeviceFamilies()
        {
            if (projectManager.GetDeviceFamilies().Count == 0)
            {
                Console.WriteLine("No Device families in this project");
                return;

            }
            int i = 0;
            foreach (DeviceFamily df in projectManager.GetDeviceFamilies())
            {
                i++;

                XmlTextWriter w = new XmlTextWriter("Family" + i.ToString() + ".xml", null);

                w.Formatting = Formatting.Indented;

                //Write the XML delcaration. 
                w.WriteStartDocument();
                Console.WriteLine("Writing Family device data to " + "Family" + i.ToString() + ".xml");

                w.WriteStartElement(df.Name.ToString());
                w.WriteStartAttribute("Name");
                w.WriteString(df.Name);
                w.WriteEndAttribute();
                foreach (string id in df.HardwareIds)
                {
                    string name = "HWID";
                    w.WriteStartElement(name);
                    Console.WriteLine("\tHWID: " + id);
                    w.WriteString(id);
                    w.WriteEndElement();
                }

                w.WriteEndDocument();
                w.Flush();
                w.Close();
            }
        }

        public void deleteDeviceFamily(string dfName)
        {
            checkForNotEmpty(objectType.prManager);

            logger.OutputLine("Deleting existing " + dfName + " Device Family.", 3);
            projectManager.DeleteDeviceFamily(dfName);

        }

        public bool addHardwareIDtoDeviceFamily(string machineName, string DeviceFamily)
        {
            bool ret = false;
            DeviceFamily devF = null;
            String[] hdwID = getHID(machineName);
            if (hdwID.Length <= 0)
                return false;

            checkForNotEmpty(objectType.prManager);

            ReadOnlyCollection<DeviceFamily> families = projectManager.GetDeviceFamilies();
            foreach (DeviceFamily df in families)
            {
                if (df.Name.Equals(DeviceFamily, StringComparison.CurrentCultureIgnoreCase))
                {
                    devF = df;
                    break;
                }
            }
            if (devF == null)
            {
                devF = projectManager.CreateDeviceFamily(DeviceFamily, hdwID);
                ret = devF != null;
            }
            else
            {
                foreach (string hid in hdwID)
                {
                    if (!devF.HardwareIds.Contains(hid))
                        devF.AddHardwareId(hid);
                }
                ret = true;
            }
            return ret;
        }

        #endregion

        #region --- Project ---

        public void createProject(string name)
        {
            checkForNotEmpty(objectType.prManager);

            if(projectExist(name))
            {
                logger.OutputLine(String.Format("ERROR: Project already exist - {0}", name), 1);
                throw new ArgumentException(String.Format("ERROR: Project already exist - {0}", name));
            }
            try
            {
                lock (mySingleObjectLock)
                {
                    if (!projectExist(name))
                    {
                        projectManager.CreateProject(name);
                        myControllersObjects[ControllerName].Projects = null;
                    }
                }
                
            }
            catch ( Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: 'Can not Create this Project' {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
        }

        public void deleteProject(string name)
        {
            checkForNotEmpty(objectType.prManager);
            try
            {
                if (projectExist(name))
                {
                    loadProject(name);
                    foreach (Test test in logoProject.GetTests())
                    {
                        deleteTest(test, true);
                    }
                    projectManager.DeleteProject(name);
                    logger.OutputLine("Succesfully deleted project: " + name, 3);
                    myControllersObjects[ControllerName].cleanCacheDTO(name);
                }
                else
                    logger.OutputLine(String.Format("deleteProject: Can not find Project: {0}", name), 3);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: Can not Delete Project {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
        }

        public void loadProject(string ProjectName)
        {
            checkForNotEmpty(objectType.prManager);
            releaseTargetLock();
            try
            {
                logoProject = projectManager.GetProject(ProjectName);
                logger._LogFile = String.Format(logFile, ProjectName);
                logger._LogName = ProjectName;
                logger.OutputLine("Loaded project " + logoProject.Name, 3);
            }
            catch (ProjectException ex)
            {
                logger.OutputLine(String.Format("ERROR: 'Can not Load Project' {0} {1}", ex.Message, ex.StackTrace), 1);
            }
        }

        public bool projectExist(string pName)
        {
            return !String.IsNullOrEmpty(projectManager.GetProjectNames().FirstOrDefault(x => x == pName));
        }

        public List<wlkProject> listProjectsAsync(bool all = false, AsyncCallback callBack = null)
        {
            List<wlkProject> ret = new List<wlkProject>();
            try
            {
                if (myControllersObjects.ContainsKey(ControllerName) && myControllersObjects[ControllerName].Projects != null)
                {
                    ret = myControllersObjects[ControllerName].Projects;
                    Interlocked.Increment(ref myControllersObjects[ControllerName].openWRConnections);
                    ThreadPool.QueueUserWorkItem((p) =>
                    {
                        try
                        {
                            bool _all = (bool)p;
                            if (0 == createMethodKey("listProjects"))
                            {
                                //TODO: add syncronization
                                myControllersObjects[ControllerName].Methods["listProjects"] = 1;
                                logger.OutputLine(String.Format("List Projects: {0}", myControllersObjects[ControllerName].Methods["listProjects"]), 3);
                                List<wlkProject> lp = listProjects(_all);
                                myControllersObjects[ControllerName].Projects = lp;
                            }
                        }
                        catch (Exception ex)
                        {
                            //resetCachedConnection(ControllerName);
                            logger.OutputLine(String.Format("ERROR List Projects: {0} {1}", ex.Message, ex.StackTrace), 1);
                        }
                        finally
                        {
                            myControllersObjects[ControllerName].Methods["listProjects"] = 0;
                            Interlocked.Decrement(ref myControllersObjects[ControllerName].openWRConnections);
                            logger.OutputLine(String.Format("END List Projects: {0}", myControllersObjects[ControllerName].Methods["listProjects"]), 3);
                            if (callBack != null)
                                callBack(null);
                        }
                    }, all);
                }
                else
                {
                    ret = listProjects(all);
                    myControllersObjects[ControllerName].Projects = ret;
                }
            }
            catch (Exception ex)
            {
                //resetCachedConnection(ControllerName);
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
            return ret;

        }

        public List<wlkProject> listProjects(bool all = false)
        {
            List<wlkProject> ret = new List<wlkProject>();
            _projectManager=myControllersObjects[ControllerName].getRO_ProjectManager();
            checkForNotEmpty(objectType.prManager);
            try
            {
                IList<string> projects = projectManager.GetProjectNames();

                foreach (string name in projects)
                {
                    Project p = projectManager.GetProject(name);
                    wlkProject wlkpr = new wlkProject();
                    ProjectInfo pInfo = p.Info;
                    wlkpr.Name = name;
                    wlkpr.Status = pInfo.Status.ToString();
                    wlkpr.NotRunCount = pInfo.NotRunCount;
                    wlkpr.PassedCount = pInfo.PassedCount;
                    wlkpr.FailedCount = pInfo.FailedCount;
                    wlkpr.RunningCount = pInfo.RunningCount;
                    wlkpr.TotalCount = pInfo.TotalCount;
                    wlkpr.TotalRunCount = wlkpr.PassedCount + wlkpr.FailedCount + wlkpr.RunningCount;
                    wlkpr.ProductInstances = listProductInstances(all, p);
                    wlkpr.lastUpdate = DateTime.Now;
                    ret.Add(wlkpr);
                } // end foreach projects
            }
            finally
            {
               int ac= myControllersObjects[ControllerName].releaseRO_ProjectManager(ref _projectManager);
               logger.OutputLine(String.Format("Available Read Connect: {0}", ac), 3);
            }
            return ret;
        }

        public void listProjectRequirements()
        {
            checkForNotEmpty(objectType.prManager);

            int i = 1;
            foreach (Requirement r in projectManager.GetRequirements())
            {
                Console.WriteLine("Requirement: {0} - {1}", i, r.FullName);
                i++;
            }

        }

        public void listFeatures()
        {
            checkForNotEmpty(objectType.prManager);

            int i = 1;

            foreach (Feature f in projectManager.GetFeatures())
            {
                Console.WriteLine("\nFeature: " + i.ToString());
                i++;

                Console.WriteLine("Feature: " + f.FullName.ToString());
                Console.WriteLine("Feature Query: " + f.Query.ToString());
            }
        }

        public void listFeaturesEx()
        {
            checkForNotEmpty(objectType.prManager);

            int i = 1;
            int j = 1;

            foreach (Feature f in projectManager.GetFeatures())
            {

                j = 1;

                Console.WriteLine("\n");

                Console.WriteLine("\n----------------------------------------------------------------------------------------------------------------------");

                Console.WriteLine("\nFeature: " + i.ToString());
                i++;

                Console.WriteLine("\n");

                Console.WriteLine("Feature Full name: " + f.FullName.ToString());
                Console.WriteLine("Feature Description: " + f.Description);
                Console.WriteLine("Feature Query: " + f.Query.ToString());

                foreach (Requirement r in f.GetRequirements())
                {
                    Console.WriteLine("\n");
                    Console.WriteLine("\nRequirement: " + j.ToString());
                    j++;

                    Console.WriteLine("\tRequirement: " + r.FullName.ToString());
                }
            }
        }

        #endregion

        #region ProductInstance

        /// <summary>
        /// find ProductInstances
        /// </summary>
        /// <param name="piName"></param>
        /// <returns>ProductInstance or NULL</returns>
        /// 
        private ProductInstance findProductInstanceByName(string piName)
        {
            ProductInstance pi = null;
            checkForNotEmpty(objectType.prManager);
            try
            {
                var _pi = logoProject.GetProductInstances().FirstOrDefault(p => p.Name.Equals(piName, StringComparison.CurrentCultureIgnoreCase));
                if (_pi != null && !String.IsNullOrEmpty(_pi.Name))
                    pi = _pi;
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
            return pi;
        }

        public void createPI(string piName, string MachinePool, string platform = null)
        {
            checkForNotEmpty(objectType.prManager);
            string lockKey = piName + "_PI";
            OSPlatform osp = null;

            try
            {
                //check if PI exist
                if (findProductInstanceByName(piName) != null)
                {
                    logger.OutputLine(String.Format("Product instance: {0} already exist. Delete it first!", piName), 1);
                    return;
                }

                //get MAchine Pool
                MachinePool mp = this.findMachinePool(MachinePool);
                if (mp == null || mp.GetMachines().Count() == 0)
                {
                    logger.OutputLine("ERROR: machine pool \"" + MachinePool + "\" not found or empty", 1);
                    throw new ArgumentNullException("Machine Pool is NULL OR Empty");
                }

                //get OSPlatform
                if (String.IsNullOrEmpty(platform))
                    osp = mp.GetMachines()[0].OSPlatform;
                else
                    osp = this.FindPlatform(mp, platform);
                if (osp == null)
                {
                    logger.OutputLine("ERROR: OS Platform \"" + platform + "\" not found in machine pool " + mp.Name, 1);
                    throw new ArgumentNullException("OSPlatforms is NULL");
                }

                //Create PI inside lock with doublechecking.
                if (findProductInstanceByName(piName) != null)
                {
                    logger.OutputLine(String.Format("Product instance: {0} already exist (before lock).  Delete it first!", piName), 1);
                    return;
                }
                if (!myEnvironmentLock.ContainsKey(lockKey))
                    myEnvironmentLock.Add(lockKey, new object());
                lock (myEnvironmentLock[lockKey])
                {
                    if (findProductInstanceByName(piName) != null)
                        logger.OutputLine(String.Format("Product instance: {0} already exist (inside lock).  Delete it first!", piName), 1);
                    else
                        logoProject.CreateProductInstance(piName, mp, osp);
                }
                myEnvironmentLock.Remove(lockKey);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
        }

        public void deletePI(string piName)
        {
            List<string> pi_names = new List<string>();
            foreach (ProductInstance pIn in logoProject.GetProductInstances())
            {
                if (pIn.Name.Equals(piName, StringComparison.CurrentCultureIgnoreCase))
                {
                    List<TargetFamily> ltf = new List<TargetFamily>();
                    foreach (TargetFamily _tf in pIn.GetTargetFamilies())
                    {
                        ltf.Add(_tf);
                        foreach (Test test in _tf.GetTests())
                            deleteTestInQueue(test);
                    }
                    foreach (TargetFamily _tf in ltf)
                    {
                        pIn.DeleteTargetFamily(_tf);
                    }
                    pi_names.Add(pIn.Name);
                }
            }
            foreach (string name in pi_names)
                logoProject.DeleteProductInstance(name);
        }

        public List<wlkProductInstance> listProductInstances(bool all=false)
        {
            checkForNotEmpty(objectType.lProject);
            return listProductInstances(all, logoProject);
        }

        private List<wlkProductInstance> listProductInstances(bool all, Project p)
        {
            List<wlkProductInstance> lpi = new List<wlkProductInstance>();

            foreach (ProductInstance pi in p.GetProductInstances())
            {
                wlkProductInstance wlkpi = listProductInstance(pi, all);
                lpi.Add(wlkpi);
            } 
            return lpi;
        }

        public wlkProductInstance listProductInstance(string ProductInstance, bool all = false)
        {
            return listProductInstance(findProductInstanceByName(ProductInstance), all);
        }

        private wlkProductInstance listProductInstance(ProductInstance pi, bool all = false)
        {
            wlkProductInstance wlkpi = new wlkProductInstance();
            wlkpi.Name = pi.Name;
            wlkpi.Targets = new List<wlkTarget>();
            wlkpi.TargetFamilies = new List<wlkTargetFamily>();

            //Console.WriteLine("Product Instance Name: " + pi.Name);
            foreach (TargetFamily tf in pi.GetTargetFamilies())
            {
                wlkTargetFamily wlktf = new wlkTargetFamily();
                wlktf.Name = tf.Family.Name;
                wlktf.Targets = new List<wlkTarget>();
                //  Console.WriteLine("\tTargetFamily: " + tf.Family.Name);
                wlktf.Targets = listTargets(tf.GetTargets());
                wlkpi.TargetFamilies.Add(wlktf);
            }
            if (all)
            {
                wlkpi.Targets = listTargets(pi.GetTargets());
            }
            return wlkpi;
        }

        #endregion

        #region --- Target --

        private List<wlkTarget> listTargets(ReadOnlyCollection<Target> Targets, bool withTests=false)
        {
            List<wlkTarget> result = new List<wlkTarget>();
            foreach (Target t in Targets)
            {
                wlkTarget wlkt = crackTarget(t, withTests);
                result.Add(wlkt);
            }
            return result;
        }

        private wlkTarget crackTarget(Target target, bool withTests)
        {
            int i=0;
            // we need to wait to not deadlock with createTarget 
            if (myControllersObjects[ControllerName].currentTarget == target.Name)
                lock (myControllersObjects[ControllerName].TargetLoock)
                    i++;

            wlkTarget wlkt = new wlkTarget();
            wlkt.Type = target.TargetType.ToString();
            wlkt.Key = target.Key;
            wlkt.Machine = crackMachine(target.Machine);
            wlkt.TestsCount = target.GetTests().Count;
            if (withTests)
                wlkt.Tests = listTests(target);
            return wlkt;
        }

        public wlkTarget getTarget(string machineName, string ProdInst)
        {
            wlkTarget wlkt = new wlkTarget();

            checkForNotEmpty(objectType.lProject);

            foreach (ProductInstance pi in logoProject.GetProductInstances())
            {
                if (pi.Name != ProdInst)
                    continue;
                foreach (TargetFamily tf in pi.GetTargetFamilies())
                {
                    foreach (Target t in tf.GetTargets())
                    {
                        if (t.Machine.Name == machineName)
                        {
                            //TODO refactor into listTargets
                            wlkt.Type = t.TargetType.ToString();
                            wlkt.Key = t.Key;
                            wlkt.Machine = crackMachine(t.Machine);
                            wlkt.Tests = listTests(t);
                            return wlkt;
                        }
                    }
                }
                //foreach (Target t in pi.GetTargets())
                //{
                //    if (t.Machine.Name == machineName)
                //    {
                //        //TODO refactor into listTarget
                //        wlkt.Type = t.TargetType.ToString();
                //        wlkt.Key = t.Key;
                //        wlkt.Machine.Name = t.Machine.Name.ToString();
                //        wlkt.Machine.OSName = t.Machine.OSPlatform.Name.ToString();
                //        wlkt.Tests = listTests(t);
                //        return wlkt;
                //    }
                //}
            }
            return wlkt;
        }

        public IEnumerable<wlkFeature> listFeaturesByTarget(String targetName, String piName)
        {
            List<wlkFeature> features = new List<wlkFeature>();
            ProductInstance pi = findProductInstanceByName(piName);
            Target trg = pi.GetTargets().FirstOrDefault(x => x.Machine.Name == targetName);
            if (trg != null)
                return listFeaturesByTarget(trg);
            else
                return features;
        }

        public IEnumerable<wlkFeature> listFeaturesByTarget(Target target)
        {
            List<wlkFeature> features = new List<wlkFeature>();
            foreach (var ms_f in target.GetFeatures())
            {
                wlkFeature f = new wlkFeature();
                f.Name = ms_f.Name;
                f.FullName = ms_f.FullName;
                f.Feature = ms_f.Description;
                features.Add(f);
            }

            return features;
        }

        public OSPlatform FindPlatform(MachinePool mp, string platform)
        {
            OSPlatform ospRet = null;
            foreach (Machine m in mp.GetMachines())
            {
                if (m.OSPlatform.Name.Equals(platform, StringComparison.OrdinalIgnoreCase) == true)
                {
                    ospRet = m.OSPlatform;
                    break;
                }
            }

            return ospRet;
        }

        public void createTargetFromHWID(string MachinePool, string hwid, string platform)
        {

            MachinePool mp = this.findMachinePool(MachinePool);
            OSPlatform osp = this.FindPlatform(mp, platform);

            try
            {
                ProductInstance pi = logoProject.CreateProductInstance(mp.Name, mp, osp);


                foreach (TargetData td in pi.FindTargetFromId(hwid))
                {
                    Target target = createTarget(null, td, pi);
                    if (target == null)
                        logger.OutputLine(String.Format("Cannot find TargetData in: {0} PI-{1}", td.Machine.Name, pi.Name), 1);
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
        }

        public void createSystemTargetFromPool(string MachinePool, string platform)
        {

            MachinePool mp = this.findMachinePool(MachinePool);
            if(mp==null)
                throw new ArgumentNullException("Machine Pool is NULL");

            OSPlatform osp = this.FindPlatform(mp, platform);
            if (osp == null)
                throw new ArgumentNullException("OSPlatform is NULL");

            try
            {
                ProductInstance pi = logoProject.CreateProductInstance(mp.Name, mp, osp);

                foreach (TargetData td in pi.FindTargetFromSystem())
                {
                    Target target = createTarget(null, td, pi);
                    if (target == null)
                        logger.OutputLine(String.Format("Cannot find TargetData in: {0} PI-{1}", td.Machine.Name, pi.Name), 1);
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
        }

        public void createTargetFromDF(string piName, string MachinePool, string dfName, string platform)
        {
            if (dfName == SYSTEM_DEVICE_FAMILY_NAME)
            {
                createSystemTargetFromPool(MachinePool, platform);
                return;
            }

            checkForNotEmpty(objectType.prManager);

            DeviceFamily df = GetDeviceFamily(dfName);
            MachinePool mp = this.findMachinePool(MachinePool);
            OSPlatform osp = this.FindPlatform(mp, platform);

            if (mp == null)
            {
                logger.OutputLine("Exception: machine pool \"" + MachinePool + "\" not found", 1);
                throw new ArgumentNullException("Machine Pool is NULL");
            }
            if (osp == null)
            {
                logger.OutputLine("Exception: OS Platform \"" + platform + "\" not found in machine pool " + mp.Name, 1);
                throw new ArgumentNullException("OSPlatforms is NULL");
            }

            try
            {
                deletePI(piName);

                ProductInstance pi = logoProject.CreateProductInstance(piName, mp, osp);
                TargetFamily tf = createTargetFamilyFromDF(pi,df);

                int i=0;
                foreach (TargetData td in pi.FindTargetFromDeviceFamily(df))
                {

                    Target target=createTarget(tf, td);

                    if (target == null)
                        logger.OutputLine(String.Format("Cannot create Target: {0} PI-{1} DF-{2} TD-{3}", td.Machine.Name, piName, df.Name, td.Description), 1);
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;

            }
        }

        public bool createTargetFromSystemClientAsync(string piName, string clientName, Dictionary<string, int> TestsToRun = null, int AverageTestsCount = -1, AsyncCallback callBack = null)
        {
            bool result = false;
            
            checkForNotEmpty(objectType.lProject);

            if (checkIsTargeting())
                return result;

            Tuple<string, string, Dictionary<string, int>, int> parameters = new Tuple<string, string, Dictionary<string, int>, int>(piName, clientName, TestsToRun, AverageTestsCount);
            Interlocked.Increment(ref myControllersObjects[ControllerName].openWRConnections);
            ThreadPool.QueueUserWorkItem((x) =>
            {
                string l_piName = parameters.Item1;
                string l_clientName = parameters.Item2;
                Dictionary<string, int> l_TestsToRun = parameters.Item3;
                int l_AverageTestsCount = parameters.Item4;

                try
                {
                    createTargetFromSystemClient(l_piName, l_clientName, l_TestsToRun, l_AverageTestsCount);
                }
                catch (Exception ex)
                {
                    logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                }
                finally
                {
                    Interlocked.Decrement(ref myControllersObjects[ControllerName].openWRConnections);
                    if (callBack != null)
                        callBack(null);
                }
            }, parameters);
            return true;
        }

        public bool createTargetFromSystemClient(string piName, string clientName, Dictionary<string, int> TestsToRun = null, int AverageTestsCount = -1)
        {
            bool result = false;

            checkForNotEmpty(objectType.lProject);

            if (checkIsTargeting())
                return result;

            try
            {
                ProductInstance pi = findProductInstanceByName(piName);
                Target target = null;
                foreach (TargetData td in pi.FindTargetFromSystem())
                {
                    if (!td.Machine.Name.Equals(clientName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    target = createTarget(null, td, pi);
                }
                if (target == null)
                    logger.OutputLine(String.Format("Cannot create Target in: PI-{0} M-{1}", piName, clientName), 1);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
            }

            return result;
        }

        public bool createTargetFromDeviceClientAsync(string piName, string deviceFamily, string clientName, Dictionary<string, int> TestsToRun = null, int AverageTestsCount = -1, AsyncCallback callBack=null)
        {
            checkForNotEmpty(objectType.lProject);
            bool result = false;

            if (checkIsTargeting())
                return result;

            Tuple<string, string, string, Dictionary<string, int>, int> parameters = new Tuple<string, string, string, Dictionary<string, int>, int>(piName, deviceFamily, clientName, TestsToRun, AverageTestsCount);
            Interlocked.Increment(ref myControllersObjects[ControllerName].openWRConnections);
            ThreadPool.QueueUserWorkItem((x) =>
            {
                try
                {
                    string l_piName = parameters.Item1;
                    string l_deviceFamily = parameters.Item2;
                    string l_clientName = parameters.Item3;
                    Dictionary<string, int> l_TestsToRun = parameters.Item4;
                    int l_AverageTestsCount = parameters.Item5;

                    createTeargetFromDeviceClient(l_piName, l_deviceFamily, l_clientName, l_TestsToRun, l_AverageTestsCount);
                }
                catch (Exception ex)
                {
                    logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                }
                finally
                {
                    Interlocked.Decrement(ref myControllersObjects[ControllerName].openWRConnections);
                    if (callBack != null)
                        callBack(null);
                }

            }, parameters);
            return true;
        }

        public bool createTeargetFromDeviceClient(string piName, string deviceFamily, string clientName, Dictionary<string, int> TestsToRun = null, int AverageTestsCount = -1)
        {
            bool result = false;

            checkForNotEmpty(objectType.lProject);

            if (checkIsTargeting())
                return result;

            ProductInstance pi = findProductInstanceByName(piName);
            DeviceFamily df = GetDeviceFamily(deviceFamily);


            Target target=null;
            foreach (var t in pi.GetTargets())
            {
                if (t.Machine.Name.Equals(clientName, StringComparison.CurrentCultureIgnoreCase) /*&& t.Name.Equals(deviceFamily, StringComparison.CurrentCultureIgnoreCase)*/)
                    target = t;
            }
            if(target==null)
                target = createTeargetFromDeviceClient(pi, df, clientName);
            if (target!=null && TestsToRun != null && TestsToRun.Count > 0)
            {
                setTargetingInfo(true, target.Machine.Name);
                result = validateTargetandScheduleTests(pi, df, target, TestsToRun, AverageTestsCount);
                setTargetingInfo(false, String.Empty);
            }
            else
                result = target != null;


            return result;
        }

        private bool validateTargetandScheduleTests( ProductInstance pi, DeviceFamily df, Target target, Dictionary<string, int> TestsToRun, int AverageTestsCount)
        {
            bool result = false;
            if (target == null)
                return result;

            string piName = pi.Name;
            string clientName=target.Machine.Name;
            int testsCount = target.GetTests().Count;

            if (AverageTestsCount > 0 && testsCount < AverageTestsCount)
            {
                deleteTarget(target, true, true);
            }
            else
            {
                //save number of tests for next target
                if (!myControllersObjects[ControllerName].TestsCountByHWID.ContainsKey(piName))
                    myControllersObjects[ControllerName].TestsCountByHWID.Add(piName, new Dictionary<string, KeyValuePair<string, int>>());
                if (!myControllersObjects[ControllerName].TestsCountByHWID[piName].ContainsKey(target.Key))
                    myControllersObjects[ControllerName].TestsCountByHWID[piName].Add(target.Key, new KeyValuePair<string, int>(target.Machine.Name, testsCount));
                KeyValuePair<string, int> maxTestsCount = myControllersObjects[ControllerName].TestsCountByHWID[piName][target.Key];
                if (maxTestsCount.Value < testsCount)
                {
                    //delete prev target - delete tests too
                    //delete tests 
                    Target t = pi.GetTargets().FirstOrDefault(x => x.Machine.Name == maxTestsCount.Key);
                    if (t != null)
                    {
                        foreach (Test test in t.GetTests())
                        {
                            deleteTest(test, true);
                        }
                    }
                    deleteTarget(t, true, true);
                    for (int i = 0; i < RETRIES; i++)
                    {
                        Target new_target = createTeargetFromDeviceClient(pi, df, maxTestsCount.Key);
                        if (new_target != null && maxTestsCount.Value >= new_target.GetTests().Count)
                            break;
                    }
                }
                else
                {
                    myControllersObjects[ControllerName].TestsCountByHWID[piName][target.Key] = new KeyValuePair<string, int>(target.Name, testsCount);
                    //schedule tests from testList
                    if (TestsToRun != null && TestsToRun.Count > 0)
                        scheduleTestsForClient(TestsToRun, clientName, target.GetTests(), new[] { target.Machine });
                    result = true;
                }
            }
            return result;
        }

        private Target createTeargetFromDeviceClient(ProductInstance pi, DeviceFamily df, string clientName)
        {
            Target target = null;

            try
            {
                TargetFamily tf = createTargetFamilyFromDF(pi, df);

  
                foreach (TargetData td in pi.FindTargetFromDeviceFamily(df))
                {
                    if (!td.Machine.Name.Equals(clientName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    target = createTarget(tf, td);
                }
                if (target == null)
                    logger.OutputLine(String.Format("Cannot find TargetData in: {0} PI-{1} DF-{2} M-{3}", df.Name, pi.Name, df.Name, clientName), 1);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
            }

            return target;
        }

        private bool checkIsTargeting()
        {
            // This compare exchange ensures the latest value of isTargeting is gotten, since it is not declared volatile.
            if (Interlocked.CompareExchange(ref myControllersObjects[ControllerName].isTargeting, myControllersObjects[ControllerName].isTargeting, 1) == 1)
            {
                logger.OutputLine(String.Format("Currently targeting {0} ", myControllersObjects[ControllerName].currentTarget), 3);
                releaseTargetLock();
                return true;
            }
            return false;
        }

        public bool createTargetFamilyFromDF(string piName, string deviceFamily, string type)
        {
            bool result = false;
            checkForNotEmpty(objectType.lProject);
            ProductInstance pi = findProductInstanceByName(piName);
            DeviceFamily df = GetDeviceFamily(deviceFamily);
            try
            {
                TargetFamily tf = createTargetFamilyFromDFnew(pi, df, type);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
            }

            return result;
        }

        private static TargetFamily createTargetFamilyFromDFnew(ProductInstance pi, DeviceFamily df, string type = null)
        {
            string lockKey = pi.Name + "_TF";
            lock (myEnvironmentLock)
            {
                if (!myEnvironmentLock.ContainsKey(lockKey))
                    myEnvironmentLock.Add(lockKey, new object());
            }
            lock (myEnvironmentLock[lockKey])
            {
                foreach (TargetFamily _tf in pi.GetTargetFamilies())
                {
                    if (_tf.Family.Name == df.Name)
                        return _tf;
                }
            }
            return pi.CreateTargetFamily(df);
        }

        private static TargetFamily createTargetFamilyFromDF(ProductInstance pi, DeviceFamily df)
        {
            string lockKey = pi.Name + "_TF";
            TargetFamily targetFamily = null;
            lock (myEnvironmentLock)
            {
                if (!myEnvironmentLock.ContainsKey(lockKey))
                    myEnvironmentLock.Add(lockKey, new object());
            }
            lock (myEnvironmentLock[lockKey])
            {
                foreach (TargetFamily _tf in pi.GetTargetFamilies())
                {
                    if (_tf.Family.Name == df.Name)
                        return _tf;
                }
                targetFamily=pi.CreateTargetFamily(df);
            }
            return targetFamily;
        }

        private Target createTarget(TargetFamily tf, TargetData td, ProductInstance pi=null)
        {
            if (td.Description.Contains("H.264"))
                return null;
            Target target = null;
            StringBuilder sb = new StringBuilder();
            String machineName=String.Empty;
           
            try
            {
                machineName = td.Machine.Name;
                sb.Append("Target:" + machineName + " ");

                if (checkIsTargeting())
                    return target;


                //System
                if (pi != null)
                {
                    Target tg = getTargetFromPIByMachineName(pi, td.Machine.Name);
                    if (tg==null || String.IsNullOrEmpty(tg.Key))
                    {
                        lock (myControllersObjects[ControllerName].TargetLoock)
                        {
                            myControllersObjects[ControllerName].TargetLoockStartTime = DateTime.Now;
                            tg = getTargetFromPIByMachineName(pi, td.Machine.Name);
                            if (tg==null || String.IsNullOrEmpty(tg.Key))
                            {
                                target = _createTargetSync(null, td, pi);
                            }
                            myControllersObjects[ControllerName].TargetLoockStartTime = DateTime.MaxValue;
                        }
                    }

                }
                // Device
                else if (tf!=null && tf.IsValidTarget(td, sb))
                {
                    lock (myControllersObjects[ControllerName].TargetLoock)
                    {
                        myControllersObjects[ControllerName].TargetLoockStartTime = DateTime.Now;
                        target = _createTargetSync(tf, td, null);
                        myControllersObjects[ControllerName].TargetLoockStartTime = DateTime.MaxValue;
                    }
                }


                if (sb.Length > 0)
                    logger.OutputLine(String.Format("Output from tf.CreateTarget: IsValidTarget {0} ", sb.ToString()), 3);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR Exception to CreateTarget {0} : {1} {2}", machineName, ex.Message, ex.StackTrace), 1);
            }
            return target;
        }

        private void releaseTargetLock()
        {
            try
            {
                if (myControllersObjects[ControllerName].TargetLoockStartTime!=DateTime.MaxValue && myControllersObjects[ControllerName].TargetLoockStartTime.Add(new TimeSpan(0, TARGET_TIMEOUT_MIN, 0)) < DateTime.Now)
                {
                    resetCachedConnection(ControllerName);
                    myControllersObjects[ControllerName] = new OMControllerObject(ControllerName, logger);
                    logger.OutputLine(String.Format("ERROR Detected DeadLock on tergeting: {0} from {1}"
                        , myControllersObjects[ControllerName].currentTarget
                        , myControllersObjects[ControllerName].TargetLoockStartTime), 1);
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR releaseTargetLock: {0} {1}", ex.Message, ex.StackTrace), 1);
            }
        }

        private Target _createTargetSync(TargetFamily tf, TargetData td, ProductInstance pi)
        {
            Target target = null;
            String prevTarget = String.Empty;
            logger.OutputLine(String.Format("Before {0}.CreateTarget : {1} {2}", tf==null?"pi":"tf", td.Machine.Name, DateTime.Now), 3);
            try
            {
                prevTarget=setTargetingInfo(true, td.Machine.Name);
                if (pi != null)
                    target = pi.CreateTarget(td);
                if (tf != null)
                    target = tf.CreateTarget(td);
            }
            catch(Exception ex)
            {
                logger.OutputLine(String.Format("ERROR Exception inside {2}.CreateTarget : {0} {1}", ex.Message, ex.StackTrace, tf == null ? "pi" : "tf"), 1);
            }
            finally
            {
                setTargetingInfo(false, prevTarget);
            }
            logger.OutputLine(String.Format("After {0}.CreateTarget : {1} {2}",tf==null?"pi":"tf", target == null ? "TARGET IS NULL" : target.Machine.Name, DateTime.Now), 3);
            return target;
        }

        private void reCreateTargets(ProductInstance pi, bool isDeleteTarget = false)
        {
                try
                {
                    foreach (Machine machine in pi.MachinePool.GetMachines())
                    {
                        // filter out any machine where the OSPlatform is not already part of the project
                        if (isDeleteTarget || machine.Status != MachineStatus.Running)
                        {
                            if (!setMachineState(machine, MachineStatus.Ready))
                            {
                                logger.OutputLine(String.Format("ERROR: Cannot set {0} to READY", machine.Name), 1);
                                break;
                            }

                            //TODO: sync to not create target if exist
                            deleteTarget(machine.Name, pi);
                        }
                    }

                    

                    foreach (TargetFamily tf in pi.GetTargetFamilies())
                    {
                        var TargetsNames = tf.GetTargets().Select(x=>x.Name).ToList();
                        Target target = null;

                        foreach (TargetData td in pi.FindTargetFromDeviceFamily(tf.Family))
                        {
                            if(!TargetsNames.Contains(td.Machine.Name))
                                target = createTarget(tf, td);
                        }
                        if (target == null && tf.Family.Name == SYSTEM_DEVICE_FAMILY_NAME)
                        {
                            foreach (TargetData td in pi.FindTargetFromSystem())
                            {
                                if (!TargetsNames.Contains(td.Machine.Name))
                                    target = createTarget(null, td, pi);
                            }
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                }
            
        }

        public bool deleteTarget(String machineName, String PIname, bool anyway)
        {
            checkForNotEmpty(objectType.lProject);
            ProductInstance pi = findProductInstanceByName(PIname);
            return deleteTarget(machineName, pi, anyway);
        }

        private bool deleteTarget(String machineName, ProductInstance pi, bool anyway=false)
        {
            bool result = false;

            if (checkIsTargeting())
                return result;

            try
            {
                foreach (TargetFamily tf in pi.GetTargetFamilies())
                {
                    foreach (Target target in tf.GetTargets().Where(x => x.Machine.Name == machineName))
                    {
                        result = deleteTarget(target, anyway, true);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
            }
            return result;
        }

        private bool deleteTarget(Target target, bool anyway, bool recursive = false)
        {
            bool result = false;
            if (!recursive && checkIsTargeting())
                return result;
            try
            {
                int resultsCount = 0;
                if (!anyway)
                    resultsCount = getResultsCountForTarget(target);
                if (0 >= resultsCount)
                {
                    lock (myControllersObjects[ControllerName].TargetLoock)
                    {
                        string prevTarget=setTargetingInfo(true, target.Machine.Name);
                        try
                        {
                            target.TargetFamily.DeleteTarget(target);
                            result = true;
                        }
                        finally
                        {
                            setTargetingInfo(false, prevTarget);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
            }
            return result;
        }

        #endregion

        private static int getResultsCountForTarget(Target target)
        {
            int testsCount = 0;
            foreach (var test in target.GetTests())
            {
                foreach (var result in test.GetTestResults())
                {
                    if (result.Target!=null && result.Target.Equals(target))
                        testsCount++;
                }
            }

            //int testsCount = target.GetTests().Where(x => (x.GetTestResults().Where(y => y.Machine.Name == machineName).Count<TestResult>() > 0)).Count<Test>();
            return testsCount;
        }

        private Target getTargetFromPIByMachineName(ProductInstance pi, String machineName)
        {
            return pi.GetTargets().FirstOrDefault(x => x.Machine.Name == machineName);
        }

        public String getMachineInTargeting()
        {
            return myControllersObjects.ContainsKey(ControllerName) ? myControllersObjects[ControllerName].currentTarget : String.Empty;
        }





        #region Machines

        public void ListMachine(string machine)
        {
            Machine m = FindMachine(machine);

            if (m == null)
            {
                Console.WriteLine("Machine \"" + machine + "\" not found");
                return;
            }

            Console.WriteLine("Name: " + m.Name);
            Console.WriteLine("Resource ID: " + m.Id);
            Console.WriteLine("Heartbeat: " + m.LastHeartbeat.ToString());
            Console.WriteLine("Platform: " + m.OSPlatform.Name);
            Console.WriteLine("In Pool: " + m.Pool.Name);
            Console.WriteLine("Status: " + m.Status.ToString());



            Console.WriteLine("\t" + m.GetTestTargets().Count() + " Target devices on system");
            foreach (TargetData td in m.GetTestTargets())
            {
                Console.WriteLine("\tTarget Description: " + td.Description);
                Console.WriteLine("\tName: " + td.Name);
                Console.WriteLine("\tType: " + td.TargetType.ToString());
                Console.WriteLine("\tKey: " + td.Key);
                Console.WriteLine("\n");
            }

            foreach (MachineProperty prop in m.GetMachineProperties())
            {
                Console.WriteLine("\tProperty: " + prop.Name + " Type: " + prop.PropertyType.ToString() + " Value: " + prop.Value.ToString());
            }

        }

        private static wlkMachine crackMachine(Machine machine, bool withLastHeartbeat = false)
        {
            wlkMachine Machine = new wlkMachine();
            Machine.Name = machine.Name.ToString();
            Machine.OSName = machine.OSPlatform.Name.ToString();
            Machine.Status = machine.Status.ToString();
            Machine.LastHeartbeat = withLastHeartbeat ? machine.LastHeartbeat.ToShortTimeString() : "N/A";
            //machine.EstimatedRuntime
            return Machine;
        }


        public List<wlkMachinePool> listMachinesAsync(string pool = null, bool withLastHeartbeat = false)
        {
            List<wlkMachinePool> ret = new List<wlkMachinePool>();
            try
            {
                if (myControllersObjects.ContainsKey(ControllerName) && myControllersObjects[ControllerName].Pools != null)
                {
                    ret = myControllersObjects[ControllerName].Pools;
                    Interlocked.Increment(ref myControllersObjects[ControllerName].openWRConnections);
                    ThreadPool.QueueUserWorkItem((p) =>
                    {
                        try
                        {
                            if (0 == createMethodKey("listMachines"))
                            {
                                //TODO: add syncronization
                                myControllersObjects[ControllerName].Methods["listMachines"] = 1;
                                logger.OutputLine(String.Format("listMachines: {0}", myControllersObjects[ControllerName].Methods["listMachines"]), 3);
                                List<wlkMachinePool> lp = listMachines(null, true);
                                myControllersObjects[ControllerName].Pools = lp;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.OutputLine(String.Format("ERROR listMachines: {0} {1}", ex.Message, ex.StackTrace), 1);
                        }
                        finally
                        {
                            myControllersObjects[ControllerName].Methods["listMachines"] = 0;
                            Interlocked.Decrement(ref myControllersObjects[ControllerName].openWRConnections);
                            logger.OutputLine(String.Format("END listMachines: {0}", myControllersObjects[ControllerName].Methods["listMachines"]), 3);
                        }
                    });
                }
                else
                {
                    ret = listMachines(pool, withLastHeartbeat);
                    if (String.IsNullOrEmpty(pool) && withLastHeartbeat)
                        myControllersObjects[ControllerName].Pools = ret;
                    else
                        listMachinesAsync(null, true);

                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
            return ret;
        }

        /// <summary>
        /// listMachines
        /// </summary>
        /// <param name="pool">if provided - look for specific pool</param>
        /// <param name="withLastHeartbea">if true - add LastHeartbeat to the wlkMachine object (slow)</param>
        /// <returns>List of wlkMachinePool objects</returns>
        public List<wlkMachinePool> listMachines(string pool = null, bool withLastHeartbeat = false)
        {
            List<wlkMachinePool> pMachines = new List<wlkMachinePool>();
            _projectManager = myControllersObjects[ControllerName].getRO_ProjectManager();
            checkForNotEmpty(objectType.prManager);

            try
            {
                // iterate over all pools
                foreach (MachinePool p in projectManager.GetRootMachinePool().GetChildPools())
                {
                    //list machines for specific pool
                    if (!String.IsNullOrEmpty(pool) && p.Name != pool)
                        continue;
                    wlkMachinePool mp = new wlkMachinePool();
                    mp.Name = p.Name;
                    mp.machines = new List<wlkMachine>();
                    pMachines.Add(mp);
                    //for each pool, list all the machines in that pool
                    foreach (Machine m in p.GetMachines())
                    {
                        mp.machines.Add(crackMachine(m, withLastHeartbeat));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
            finally
            {
                int ac = myControllersObjects[ControllerName].releaseRO_ProjectManager(ref _projectManager);
                logger.OutputLine(String.Format("Available Read Connect: {0}", ac), 3);
            }
            return pMachines;
        }

        public string[] listMachinePools()
        {
            List<string> list = new List<string>();
            _projectManager = myControllersObjects[ControllerName].getRO_ProjectManager();
            checkForNotEmpty(objectType.prManager);
            try
            {
                foreach (MachinePool mp in projectManager.GetRootMachinePool().GetChildPools())
                {
                    list.Add(mp.Name.ToString());
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
            finally
            {
                int ac = myControllersObjects[ControllerName].releaseRO_ProjectManager(ref _projectManager);
                logger.OutputLine(String.Format("Available Read Connect: {0}", ac), 3);
            }
            return list.ToArray();
        }

        public void createMachinePool(string name)
        {
            checkForNotEmpty(objectType.prManager);
            if (findMachinePool(name) != null)
            {
                logger.OutputLine("Machine pool already exist " + name, 3);
                return;
            }
            try
            {
                lock (mySingleObjectLock)
                {
                    if (findMachinePool(name) != null)
                        return;
                    MachinePool mproot = projectManager.GetRootMachinePool();
                    logger.OutputLine("Creating new machine pool " + name, 3);
                    mproot.CreateChildPool(name);
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }

            
            //this.listMachines();
        }

        public void deleteMachinePool(string name)
        {
            checkForNotEmpty(objectType.prManager);

            MachinePool mproot = projectManager.GetRootMachinePool();
            foreach (MachinePool p in mproot.GetChildPools())
            {
                if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                {
                    foreach (Machine mch in p.GetMachines())
                    {
                        mch.SetMachineStatus(MachineStatus.NotReady, 100);
                        mch.Pool.MoveMachineTo(mch, mproot.DefaultPool);
                    }
                    logger.OutputLine(String.Format("Deleting machine pool {0}. All machines in pool are moved to default pool", name),3);
                    mproot.DeleteChildPool(p);
                    //this.listMachines();
                }
            }

        }


        private Machine FindMachine(string name)
        {

            Machine mRet = null;
            checkForNotEmpty(objectType.prManager);
            foreach (MachinePool p in projectManager.GetRootMachinePool().GetChildPools())
            {
                foreach (Machine m in p.GetMachines())
                {
                    if (m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        mRet = m;
                    }

                }
            }
            return mRet;
        }


        public MachinePool findMachinePool(string name)
        {
            checkForNotEmpty(objectType.prManager);
            MachinePool mpRet = null;
            foreach (MachinePool mp in projectManager.GetRootMachinePool().GetChildPools())
            {
                if (mp.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                {
                    mpRet = mp;
                }
            }
            return mpRet;
        }

        public void moveMachine(string name, string topool="Default Pool")
        {
            checkForNotEmpty(objectType.prManager);
            try
            {
                MachinePool destPool = this.findMachinePool(topool);
                Machine m = this.FindMachine(name);

                if (m == null)
                {
                    logger.OutputLine("Exception: machine \"" + name + "\" not found", 1);
                    throw new ArgumentNullException("Cannot find Machine!");
                }
                if (destPool == null)
                {
                    logger.OutputLine("Exception: destination pool \"" + topool + "\" not found", 1);
                    throw new ArgumentNullException("Cannot find Pool!");
                }

                if (m.Pool.Name == destPool.Name)
                    return;


                logger.OutputLine("Moving machine " + m.Name + " from " + m.Pool.Name.ToString() + " to " + destPool.Name.ToString(), 3);
                m.Pool.MoveMachineTo(m, destPool);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
            //this.listMachines();
        }

        public void reSetMachineState(string name)
        {
            checkForNotEmpty(objectType.prManager);
            Machine m = this.FindMachine(name);

            if (m == null)
            {
                logger.OutputLine("Exception: machine \"" + name + "\" not found", 1);
                throw new ArgumentNullException("Cannot find Machine!");
            }

            logger.OutputLine("Resetting machine " + m.Name.ToString() + " from " + m.Status.ToString() + " to " + MachineStatus.Ready.ToString(), 3);

            try
            {
                switch (m.Status)
                {
                    case MachineStatus.Ready:
                        m.SetMachineStatus(MachineStatus.NotReady, 6000);
                        break;
                    default:
                        m.SetMachineStatus(MachineStatus.Ready, 6000);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
        }

        public bool setMachineState(string name, string status)
        {
            checkForNotEmpty(objectType.prManager);
            bool result = false;
            try
            {
                Machine m = this.FindMachine(name);
                if (m == null)
                {
                    logger.OutputLine(String.Format("ERROR: machine {0} not found",name),1);
                    return false;
                }
                MachineStatus ms;
                switch (status.ToUpper())
                {
                    case "READY":
                        ms = MachineStatus.Ready;
                        break;
                    case "NOTREADY":
                        ms = MachineStatus.NotReady;
                        break;
                    default:
                        return false;
                }

                result = m.SetMachineStatus(ms, 100);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
            }
            return result;
        }

        private bool setMachineState(Machine m, MachineStatus status)
        {
            bool result = false;
            try
            {
                result = m.SetMachineStatus(status, 100);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
            }
            return result;
        }

        public void deleteMachine(String machineName)
        {
            checkForNotEmpty(objectType.prManager);
            Machine machine = FindMachine(machineName);
            MachinePool defaultPool = projectManager.GetRootMachinePool().DefaultPool;
            if(machine==null)
                throw new ArgumentException(String.Format("Couldn't find machine:{0} on Controller", machineName));
            machine.Pool.DeleteMachine(machineName);
            //projectManager.GetRootMachinePool().DefaultPool.DeleteMachine(machine);
        }

        public string[] getHID(String machine)
        {
            List<string> hids = new List<string>();
            try
            {
                ConnectionOptions connOptions = new ConnectionOptions();
                connOptions.Impersonation = ImpersonationLevel.Impersonate;
                connOptions.EnablePrivileges = true;
                ManagementScope manScope = new ManagementScope(String.Format(@"\\{0}\ROOT\CIMV2", machine), connOptions);
                ObjectQuery query = new ObjectQuery("select PNPDeviceID from Win32_VideoController");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(manScope, query);
                foreach (ManagementObject mo in searcher.Get())
                {
                    if (mo.Properties["PNPDeviceID"].Value != null)
                    {
                        string id = mo.Properties["PNPDeviceID"].Value.ToString();
                        id = id.Substring(0, id.LastIndexOf("\\"));
                        //PCI\VEN_8086&DEV_0126&SUBSYS_22108086&REV_09\3&11583659&0&10
                        hids.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
            return hids.ToArray();

        }

        #endregion

        #region Tests


        public List<wlkTest> listTestsForTargetAsync(string piName, string name, AsyncCallback callBack)
        {
            List<wlkTest> Tests = null;
            if (myControllersObjects[ControllerName].Targets.ContainsKey(name))
            {
                Tests = myControllersObjects[ControllerName].Targets[name].Tests;
                string[] pars = { piName, name };
                Interlocked.Increment(ref myControllersObjects[ControllerName].openWRConnections);
                ThreadPool.QueueUserWorkItem((_pars) =>
                {
                    try
                    {
                        string _piName = ((String[])_pars)[0];
                        string _name = ((String[])_pars)[1];
                        string key_name = String.Format("listTestsForTarget_{0}", _name);

                        try
                        {
                            if (0 == createMethodKey(key_name))
                            {
                                myControllersObjects[ControllerName].Methods[key_name] = 1;
                                listTestsForTarget(_piName, _name);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                        }
                        finally
                        {
                            myControllersObjects[ControllerName].Methods[key_name] = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref myControllersObjects[ControllerName].openWRConnections);
                        if(callBack!=null)
                            callBack(null);
                    }

                }, pars);
            }
            else
            {
                Tests = listTestsForTarget(piName, name);
                if (callBack != null)
                    callBack(null);
            }

            return Tests;
        }

        /// <summary>
        /// listTestsForTarget
        /// </summary>
        /// <param name="piName">Product Instance name</param>
        /// <param name="name">Target name</param>
        /// <returns>NULL if target doesn't exist, Lists of Test objects</returns>
        public List<wlkTest> listTestsForTarget(string piName, string name)
        {
            List<wlkTest> Tests = null;

            checkForNotEmpty(objectType.lProject);
            try
            {
                ProductInstance pi = findProductInstanceByName(piName);
                Target trg = pi.GetTargets().FirstOrDefault(x => x.Machine.Name == name);
                if (!myControllersObjects[ControllerName].Targets.ContainsKey(name))
                    myControllersObjects[ControllerName].Targets.Add(name, new wlkTarget());
                if (trg != null)
                {
                    wlkTarget target = crackTarget(trg, true);
                    myControllersObjects[ControllerName].Targets[name] = target;
                    Tests = target.Tests;
                }
                else
                {
                    myControllersObjects[ControllerName].Targets[name] = new wlkTarget();
                }
            }
            finally
            {
                //int ac = myControllersObjects[ControllerName].releaseRO_ProjectManager(_projectManager);
                //logger.OutputLine(String.Format("Available Read Connect: {0}", ac), 3);
            }
            return Tests;
        }

        public List<wlkTest> listTestsForProjectAsync(AsyncCallback callBack)
        {
            List<wlkTest> Tests = new List<wlkTest>();
            checkForNotEmpty(objectType.lProject);
            if (myControllersObjects[ControllerName].prTests.ContainsKey(logoProject.Name))
            {
                Tests = myControllersObjects[ControllerName].prTests[logoProject.Name];
                ThreadPool.QueueUserWorkItem((p) =>
                {
                    try
                    {
                        string key_name = String.Format("listTestsForProject_{0}", logoProject.Name);
                        try
                        {
                            if (0 == createMethodKey(key_name))
                            {
                                myControllersObjects[ControllerName].Methods[key_name] = 1;
                                listTestsForProject();
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                        }
                        finally
                        {
                            myControllersObjects[ControllerName].Methods[key_name] = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                    }
                    finally
                    {
                        if (callBack != null)
                            callBack(null);
                    }

                });
            }
            else
            {
                Tests = listTestsForProject();
                if (callBack != null)
                    callBack(null);
            }
            return Tests;
        }

        public List<wlkTest> listTestsForProject()
        {
            List<wlkTest> Tests = new List<wlkTest>();
            checkForNotEmpty(objectType.lProject);
            if (!myControllersObjects[ControllerName].prTests.ContainsKey(logoProject.Name))
                myControllersObjects[ControllerName].prTests.Add(logoProject.Name, new List<wlkTest>());
            foreach (var test in logoProject.GetTests())
            {
                Tests.Add(crackTest(test));
            }
            myControllersObjects[ControllerName].prTests[logoProject.Name] = Tests;
            return Tests;
        }

        public List<wlkTest> listTests(Target tar)
        {
            List<wlkTest> Tests = new List<wlkTest>();
            foreach (Test test in tar.GetTests())
            {
                wlkTest wlktest = crackTest(test);
                wlktest.Results = new List<wlkTestResult>();
                foreach (var result in test.GetTestResults())
                {
                    wlktest.Results.Add(crackResults(result, true, wlktest));
                }
                Tests.Add(wlktest);
            } // end foreach test
            return Tests;
        }

        private wlkTest crackTest(Test test)
        {
            wlkTest wlktest = new wlkTest();
            wlktest.TestGUID = test.Id;
            wlktest.TestID = test.GetHashCode();
            wlktest.TestInstanceID=test.InstanceId;
            wlktest.Name = test.Name.ToString();
            wlktest.ScheduleOptions = test.ScheduleOptions.ToString();
            wlktest.EstimatedRuntime = test.EstimatedRuntime;
            wlktest.Description = test.Description;
            wlktest.Status = test.Status.ToString();
            wlktest.ExecutionState=test.ExecutionState.ToString();
            //wlktest.Type = test..TestType.ToString();
            wlktest.Requirments = new List<wlkRequirment>();
            foreach (Requirement req in test.GetRequirements())
            {
                 wlkRequirment wreq = new wlkRequirment();
                wreq.Name = req.Name;
                wreq.FullName = req.FullName;
                wreq.Feature = req.Feature.FullName;
                wlktest.Requirments.Add(wreq);
            }

            return wlktest;

        }



        public void RunaTest(string Testname)
        {
            checkForNotEmpty(objectType.lProject);

            foreach (ProductInstance pi in logoProject.GetProductInstances())
            {
                foreach (Target t in pi.GetTargets())
                {

                    foreach (Test test in t.GetTests())
                    {
                        if (test.Name.ToString().Equals(Testname, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            RunTest(test);
                            Thread.Sleep(1000); // switch bounce
                            while (logoProject.Info.Status == ProjectStatus.Running)
                            {
                                Console.WriteLine("\t\tTests " + logoProject.Info.Status.ToString());
                                Thread.Sleep(15000);
                            }
                            Console.WriteLine("\t\tTests " + logoProject.Info.Status.ToString());

                            // we don't want to do each, we want to do the latest.
                            TestResult tr = test.GetTestResults().Last();
                            //crackResults(tr);

                        }
                    }
                }
            }
        }

        public void RunAllTests()
        {
            checkForNotEmpty(objectType.lProject);
            Interlocked.Increment(ref myControllersObjects[ControllerName].openWRConnections);
            ThreadPool.QueueUserWorkItem((_p) => 
                {
                    try
                    {
                        Project pr = ((Project)_p);
                        foreach (Test test in pr.GetTests())
                        {
                            try
                            {
                                test.QueueTest();
                            }
                            catch (Exception ex)
                            {
                                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref myControllersObjects[ControllerName].openWRConnections);
                    }

                }, logoProject);
        }


        public void reScheduleQueuedOrNotRunTests(string PIname, bool reCreateTarget)
        {
            checkForNotEmpty(objectType.lProject);

            ProductInstance pi = findProductInstanceByName(PIname);
            if (reCreateTarget) {
                logger.OutputLine(String.Format("reCreateTargets in reScheduleQueuedOrNotRunTests"), 3);
                reCreateTargets(pi);
            }

            IEnumerable<Test> RescheduleList  = getTestsForReSchedule(pi,
                        new TestResultStatus[] { TestResultStatus.InQueue, TestResultStatus.NotRun } );

            logger.OutputLine(String.Format("reScheduleQueuedOrNotRunTests"), 3);
            reScheduleTestsAsync(pi, RescheduleList);
        }

        public void reScheduleTestList(string PIname, List<string> Tests, bool IncludeList, bool reCreateTarget) {
            checkForNotEmpty(objectType.lProject);

            ProductInstance pi = findProductInstanceByName(PIname);
            if (reCreateTarget) {
                logger.OutputLine(String.Format("reCreateTargets in reScheduleTestList"), 3);
                reCreateTargets(pi);
            }

            IEnumerable<Test> PInstanceTests = getTestsForReSchedule(pi,
                        new TestResultStatus[] { TestResultStatus.InQueue, TestResultStatus.NotRun } );
            IEnumerable<Test> RescheduleList = PInstanceTests.Where((x) => {
                    return Tests == null || (Tests.Contains(x.Name) == IncludeList);
                });

            logger.OutputLine(String.Format("reScheduleTestList"), 3);
            reScheduleTestsAsync(pi, RescheduleList);
        }

        public void reScheduleQueuedTests(string PIname, bool reCreateTarget) {
            checkForNotEmpty(objectType.lProject);

            ProductInstance pi = findProductInstanceByName(PIname);
            if (reCreateTarget) {
                logger.OutputLine(String.Format("reCreateTargets in reScheduleQueuedTests"), 3);
                reCreateTargets(pi);
            }
            IEnumerable<Test> RescheduleList = getTestsForReSchedule(pi,
                        new TestResultStatus[] { TestResultStatus.InQueue });

            logger.OutputLine(String.Format("reScheduleQueuedTests"), 3);
            reScheduleTestsAsync(pi, RescheduleList);
        }

        private void reScheduleTestsAsync(ProductInstance pi, IEnumerable<Test> ToReschedule)
        {
            if (1 == createMethodKey("reScheduleTests") || checkIsTargeting()) {
                releaseTargetLock();
                //addTestsForFutureScheduling(pi.Name);
                return;
            }
            Interlocked.Increment(ref myControllersObjects[ControllerName].openWRConnections);
            ThreadPool.QueueUserWorkItem((_pi) =>
            {
                try
                {
                    ProductInstance pi_inside = ((ProductInstance)_pi);
                    reScheduleTests(pi_inside, ToReschedule);
                }
                finally
                {
                    Interlocked.Decrement(ref myControllersObjects[ControllerName].openWRConnections);
                }
            }, pi);
        }

        private void reScheduleTests(ProductInstance pi_inside, IEnumerable<Test> ToReschedule) {
            myControllersObjects[ControllerName].Methods["reScheduleTests"] = 1;
            try {
                lock (myControllersObjects[ControllerName].TargetLoock) {
                    myControllersObjects[ControllerName].TargetLoockStartTime = DateTime.Now;
                    foreach (Test test in ToReschedule) {
                        try {
                            cancelDeleteQueueTest(test);
                        }
                        catch (Exception ex) {
                            logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                        }
                    }
                    myControllersObjects[ControllerName].TargetLoockStartTime = DateTime.MaxValue;
                }
            }
            catch (Exception ex) {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
            }
            finally {
                myControllersObjects[ControllerName].Methods["reScheduleTests"] = 0;
            }
        }

        private void cancelDeleteQueueTest(Test test)
        {
            logger.OutputLine(String.Format("Queueing Test: {0}", test.Name), 3);
            if (test.GetTestResults().Count == 0 || deleteTestInQueue(test))
            {
                var results=test.QueueTest();
                String resultIds = String.Empty;
                if (results != null)
                {
                    foreach (var res in results)
                        resultIds += " " + res.InstanceId;
                }
                else
                    resultIds = "Nothing returned from test.QueueTest()";
                logger.OutputLine(String.Format("Queued TestResults: {0}", resultIds), 3);
            }
        }

        private void addTestsForFutureScheduling(String PiName, String targetName="ANY", String testName="ALL")
        {
            var testsDic = myControllersObjects[ControllerName].TestsToRun;
            lock(myControllersObjects[ControllerName].EnvironmentLock)
            {
                if (!testsDic.ContainsKey(PiName))
                    testsDic.Add(PiName, new Dictionary<string, List<string>>());
                if (!testsDic[PiName].ContainsKey(targetName))
                    testsDic[PiName].Add(targetName, new List<string>());
                testsDic[PiName][targetName].Add(testName);
            }

        }

        private bool deleteTestInQueue(Test test)
        {
            if (test.Status != TestResultStatus.InQueue)
                return false;

            logger.OutputLine(String.Format("Canceling test: {0}", test.Name), 3);
            return deleteTest(test, false, TestResultStatus.InQueue);
            
        }

        private bool deleteTest(Test test, bool all=false, TestResultStatus status=TestResultStatus.InQueue)
        {
            bool result=false;
            foreach (TestResult tresult in test.GetTestResults())
            {
                if (!all && tresult.Status != status)
                    continue;

                try
                {
                    if(tresult.Status==TestResultStatus.InQueue || tresult.Status==TestResultStatus.Running)
                        tresult.Cancel();
                    logger.OutputLine(String.Format("Deleting result: {0}", tresult.GetHashCode()), 3);
                    test.DeleteTestResult(tresult);
                    result = true;
                }
                catch (Exception ex)
                {
                    logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                    logger.OutputLine(String.Format("Tests Status: {0}", test.Status), 1);
                    logger.OutputLine(String.Format("Result Status: {0}", tresult.Status), 1);
                }
            }
            return result;
        }

        private IEnumerable<Test> getTestsForReSchedule(ProductInstance pi, TestResultStatus[] Statuses)
        {
            //IEnumerable<Test> tests1 = logoProject.GetTests();
            //foreach (var test in tests1)
            //    if (test.Status == TestResultStatus.Canceled)
            //        Console.WriteLine(test.Name);
            IList<Test> PITests = pi.GetTests();
            IEnumerable<Test> tests = PITests.Where((x) => { return Statuses == null || Statuses.Contains(x.Status); });//(x.Status == TestResultStatus.InQueue) || (x.Status == TestResultStatus.NotRun)); /* || (x.Status == TestResultStatus.Canceled)*/
            return tests;
        }

        public void scheduleTestsFromDictionary(string PIname, Dictionary<string, Dictionary<string, int>> TestsToRun, List<string> RunnableClients)
        {
            ProductInstance pi = findProductInstanceByName(PIname);
            if (pi == null)
                throw new ArgumentNullException(String.Format("Product Instance {0} is NULL",PIname));

            // Passing PIName and pi to ensure the logger name is correct.
            // If pi.Name == PIName 100% of the time then PIName can be substituted.
            List<Machine> RunnableMachines = CreateMachineList(PIname, pi, RunnableClients);


            foreach (string pcName in TestsToRun.Keys)
            {
                try
                {
                    if (String.Compare(pcName, "ANY", true) == 0)
                    {
                        Machine[] machines = (RunnableClients == null) ? null : RunnableMachines.ToArray();
                        scheduleTestsForClient(TestsToRun[pcName], pcName, pi.GetTests(), machines);
                    }
                    else
                    {

                        Machine machine = pi.GetMachines().FirstOrDefault(x => x.Name == pcName);
                        if (machine != null)
                        {
                            Target tr = pi.GetTargets().FirstOrDefault(x => x.Machine == machine);
                            if (tr != null)
                            {
                                Machine[] machines = (machine == null) ? null : new Machine[] { machine };
                                scheduleTestsForClient(TestsToRun[pcName], pcName, tr.GetTests(), machines);
                            }
                            else
                            {
                                logger.OutputLine(String.Format("ERROR - Cannot Find Target in scheduleTestsFromDictionary: Machine: {0}", machine.Name), 1);
                                logger.OutputToTestSchedule(String.Format("Cannot Find Target for Machine: {0}", machine.Name), WHCKLog.SeverityLevel.Error);
                            }
                        }
                        else
                        {
                            logger.OutputLine(String.Format("ERROR - Cannot Find Machine in scheduleTestsFromDictionary: Machine: {0}", pcName), 1);
                            logger.OutputToTestSchedule(String.Format("Cannot Find Machine: {0}", pcName), WHCKLog.SeverityLevel.Error);
                        }
                    }
                }
                catch (Exception e)
                {
                    string Name = String.IsNullOrEmpty(pcName) ? "" : pcName;
                    logger.OutputLine(String.Format("Unexpected Error on Machine: {0} \n {1} {2}", Name, e.Message, e.StackTrace), 1);
                    logger.OutputToTestSchedule(String.Format("Unexpected Error, Alert the Automation Team."), WHCKLog.SeverityLevel.Error);



                }

            }

        }

        private List<Machine> CreateMachineList(string PIname, ProductInstance pi, List<string> RunnableClients)
        {
            List<Machine> RunnableMachines = null;
            try
            {
                if (RunnableClients != null && RunnableClients.Count > 0)
                {
                    RunnableMachines = new List<Machine>();
                    foreach (string ClientName in RunnableClients)
                    {
                        Machine machine = pi.GetMachines().FirstOrDefault(x => x.Name == ClientName);
                        if (machine != null)
                        {
                            RunnableMachines.Add(machine);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RunnableMachines = null;
                logger.OutputLine(String.Format("Error in ScheduleTestsFromDictionary, {0} {1}", e.Message, e.StackTrace), 1);
                logger.OutputToTestSchedule(String.Format("Could Not Create Machine List, Alert the Automation Team."), WHCKLog.SeverityLevel.Error);
            }
            return RunnableMachines;
        }


        private void scheduleTestsForClient(Dictionary<string, int> TestsToRun, string MachineName, IList<Test> tests, Machine[] RunnableClients = null)
        {



            foreach (string testName in TestsToRun.Keys)
            {
                try
                {
                    Test test = tests.FirstOrDefault(x => x.Name == testName);
                    if (test == null)
                    {
                        logger.OutputLine(String.Format("ERROR - Cannot Find Test {0} on: Machine: {1} in scheduleTestsForClient", testName, MachineName), 1);
                        logger.OutputToTestSchedule(String.Format("Cannot Find Test {0} on: Machine: {1}", testName, MachineName), WHCKLog.SeverityLevel.Error);



                        continue;
                    }
                    ScheduleTest(test, TestsToRun[testName], RunnableClients);

                }
                catch (Exception ex)
                {
                    logger.OutputLine(String.Format("ERROR - RERUN TESTS - ATTEMPTING RERUN ON ANY: Machine: {0}  Test:{1} \r\n {2} {3}", MachineName, testName, ex.Message, ex.StackTrace), 1);
                    logger.OutputToTestSchedule(String.Format("Failed to Schedule Test {0} on {1}, Attempting to Schedule on other clients", testName, MachineName), WHCKLog.SeverityLevel.Error);

                    AttemptRescheduleTest(testName, TestsToRun[testName], tests, RunnableClients);
                }
            }
        }


        private void AttemptRescheduleTest(string testName, int NumRuns, IList<Test> tests, Machine[] RunnableClients)
        {
            try
            {
                Test test = tests.FirstOrDefault(x => x.Name == testName);
                if (test != null)
                    ScheduleTest(test, NumRuns, RunnableClients);
                else
                {
                    logger.OutputLine(String.Format("ERROR - Cannot Find Test {0} in scheduleTestsForClient", testName), 1);
                    logger.OutputToTestSchedule(String.Format("ERROR - Cannot Find Test {0} in scheduleTestsForClient", testName), WHCKLog.SeverityLevel.Error);



                }
            }
            catch (Exception e)
            {
                logger.OutputLine(String.Format("ERROR - RERUN TESTS - CLIENT RERUN ATTEMPT FAILED:  Test:{0} \r\n {1} {2}", testName, e.Message, e.StackTrace), 1);
                logger.OutputToTestSchedule(String.Format("Failed to Scheduled Test {0} Again, I give up", testName), WHCKLog.SeverityLevel.Error);
            }

        }


        private void ScheduleTest(Test test, int numRuns, Machine[] machines = null)
        {
            if (test == null)
                throw new ArgumentNullException("test");

            for (int i = 0; i < numRuns; i++)
            {
                if (machines != null)
                {
                    string MachineName = (machines.Length == 1) ? machines.First().Name : "Any";

                    logger.OutputLine(String.Format("(stfc) Queueing Test on {0}: {1} ", MachineName, test.Name), 3);
                    logger.OutputToTestSchedule(String.Format("Queueing Test on {0}: {1}", test.Name, MachineName), WHCKLog.SeverityLevel.Information);


                    test.QueueTest(machines);
                }
                else
                {
                    logger.OutputLine(String.Format("(stfc) Queueing Test: {0} on Any", test.Name), 3);
                    logger.OutputToTestSchedule(String.Format("Queueing Test: {0} on Any", test.Name), WHCKLog.SeverityLevel.Information);

                    test.QueueTest();
                }
            }
        }


        public void getListofAvailableTests(string PIname, string TargetName)
        {
            List<Test> lTests = new List<Test>();
            checkForNotEmpty(objectType.lProject);
            ProductInstance pi = findProductInstanceByName(PIname);
            foreach (Test test in pi.GetTests().Where(x => x.Status == TestResultStatus.InQueue))
            {
                foreach (var t in test.GetTestTargets().Where(x => x.Machine.Name == TargetName))
                {
                    Console.WriteLine("Target: {0}", t.Machine.Name);
                    lTests.Add(test);
                    Console.WriteLine("Test: {0} \n status {1} ", test.Name, test.ExecutionState);
                }
            }
            Console.WriteLine("Tests: {0}", lTests.Count);
        }

        public void reRunFailed(string PIname)
        {
            ProductInstance pi = findProductInstanceByName(PIname);
            if (pi == null)
            {
                throw new ArgumentNullException("Product Instance is NULL");
            }

            foreach (Test test in pi.GetTests().Where(x => x.Status == TestResultStatus.Failed))
            {
                try
                {
                    test.QueueTest();
                }
                catch (Exception ex)
                {
                    logger.OutputLine(String.Format("ERROR - RERUN Failed TESTS: {2} \n {0} {1}", ex.Message, ex.StackTrace, test.Name), 1);
                }
            }

        }

        public void RunAllTestsAndWait()
        {
            checkForNotEmpty(objectType.lProject);

            logoProject.QueueTest();

            /*
        IRunTests projectTestRunner = p as IRunTests;
        if (projectTestRunner != null)
        {
            projectTestRunner.QueueTest();
        }
        */

            while (logoProject.Info.Status == ProjectStatus.Running)
            {
                Thread.Sleep(15000);
                foreach (ProductInstance pi in logoProject.GetProductInstances())
                {
                    foreach (Target t in pi.GetTargets())
                    {
                        foreach (TestResult tr in t.GetTestResults())
                        {
                            Console.WriteLine("\tTest " + tr.Test.Name + " Status " + tr.Status.ToString());
                        }
                    }
                }
            }
            // all done.
            foreach (ProductInstance pi in logoProject.GetProductInstances())
            {
                foreach (Target t in pi.GetTargets())
                {
                    Console.WriteLine("\tTest execution complete, gathering results...");
                    foreach (TestResult tr in t.GetTestResults())
                    {
                        //crackResults(tr);
                    }
                }
            }
        }

        public void RunTest(Test test)
        {
            checkForNotEmpty(objectType.lProject);

            {
                Console.WriteLine("\n\tTest Name: " + test.Name.ToString());
                Console.WriteLine("\tDescription:\n\n" + test.Description.ToString());
                Console.WriteLine("\n\tEstimated Runtime: " + test.EstimatedRuntime.ToString());
                Console.WriteLine("\tScheduled, waiting to complete...");


                IRunTests testRunner = test as IRunTests;
                if (testRunner != null)
                {
                    testRunner.QueueTest();
                }
            }
        }
        #endregion

        #region Results




        public List<wlkTestResult> listResults(int start, int count, bool limited)
        {
            List<wlkTestResult> res = new List<wlkTestResult>();
            checkForNotEmpty(objectType.lProject);

            IList<Test> tests = logoProject.GetTests();
            int t_count = tests.Count;
            if (start > t_count)
                return res;
            if(start<0)
                start=0;
            if (count <= 0)
                count = t_count;
            else
                count = start + count;
            if (count > t_count)
                count = t_count;

            for (int i = start; i < count; i++)
            {
                var test = tests[i];
                logger.OutputLine(String.Format("List Test: {0}", test.Name), 3);
                foreach (TestResult tr in test.GetTestResults().OrderBy(x => x.InstanceId))
                {
                    wlkTestResult wlktr = crackResults(tr, limited);
                    res.Add(wlktr);
                }
            }
            return res;
        }

        public List<wlkTest> listTestsWithResults(int start, int count, bool limited)
        {
            List<wlkTest> tests = new List<wlkTest>();
            checkForNotEmpty(objectType.lProject);

            IList<Test> ms_tests = logoProject.GetTests();
            int t_count = ms_tests.Count;
            if (start > t_count)
                return tests;
            if (start < 0)
                start = 0;
            if (count <= 0)
                count = t_count;
            else
                count = start + count;
            if (count > t_count)
                count = t_count;

            for (int i = start; i < count; i++)
            {
                var ms_test = ms_tests[i];
                //TODO : optimize to run it once and reuse for all results
                wlkTest test = crackTest(ms_test);
                test.Results = new List<wlkTestResult>();
                logger.OutputLine(String.Format("List Test: {0}", ms_test.Name), 3);
                foreach (TestResult tr in ms_test.GetTestResults().OrderBy(x => x.InstanceId))
                {
                    wlkTestResult wlktr = crackResults(tr, limited, test);
                    test.Results.Add(wlktr);
                }
                tests.Add(test);
            }
            return tests;
        }

        public wlkTestResult crackResults(TestResult tr, bool limited)
        {
            return crackResults(tr, limited, new wlkTest());
        }
        public wlkTestResult crackResults(TestResult tr, bool limited, wlkTest wlktest)
        {
            wlkTestResult wlkTR = new wlkTestResult();
            List<wlkTestParameter> _params = new List<wlkTestParameter>();
            try
            {
                //Console.WriteLine(String.Format("P0: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
                if (String.IsNullOrEmpty(wlktest.TestGUID))
                    wlkTR.Test = crackTest(tr.Test);
                //else
                //    wlkTR.Test = wlktest;
                wlkTR.ResultID = tr.GetHashCode();
                wlkTR.GUID = tr.InstanceId;
                wlkTR.StartTime = tr.StartTime;
                var aa = tr.GetParameters();
                //tr.
                //logger.OutputLine(String.Format(" --- Result: {0}", wlkTR.ResultID), 1);
                //Console.WriteLine(String.Format("{2} P1: {0} {1}", DateTime.Now, DateTime.Now.Millisecond, wlkTR.ResultID));
                try
                {
                    if (tr.Machine != null)
                        wlkTR.MachineName = tr.Machine.Name;
                }
                catch (Exception ex)
                {
                   // Console.WriteLine("ERROR: {0} {1}", ex.Message, ex.StackTrace);
                    logger.OutputLine(String.Format("ERROR: in tr.Machine TEST_Result: {0}-{1} {2} {3}", wlkTR.ResultID, wlkTR.Test.Name, ex.Message, ex.StackTrace), 1);
                }
                //Console.WriteLine(String.Format("P2: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));


                TestResultStatus trs;
                try
                {
                    trs = tr.Status;
                    wlkTR.Status = trs.ToString();
                }
                catch (Exception ex) 
                {
                    logger.OutputLine(String.Format("ERROR: TEST_Result: {0}-{1} {2} {3}", wlkTR.ResultID, wlkTR.Test.Name, ex.Message, ex.StackTrace), 1);
                    Console.WriteLine("ERROR: {0} {1}", ex.Message, ex.StackTrace);
                    trs = tr.Status;
                    wlkTR.Status = trs.ToString();
                }
                //return wlkTR;
                //Console.WriteLine(String.Format("{3} P3: {0} {1} {2}", DateTime.Now, DateTime.Now.Millisecond, wlkTR.MachineName, wlkTR.ResultID));
                
                if (trs != TestResultStatus.Running)
                    wlkTR.CompletionTime = tr.CompletionTime;
                //var ss = tr.GetAppliedFilters();
                foreach (TestParameter tp in tr.GetParameters())
                {
                    wlkTestParameter par = new wlkTestParameter();
                    par.Name = tp.Name.ToString();
                    par.DefaultValue = tp.DefaultValue.ToString();
                    par.ActualValue = tp.ActualValue;
                    _params.Add(par);
                }
                wlkTR.TestParameters = _params;
                //Console.WriteLine(String.Format("P4: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
                if (!limited)
                {
                    logger.OutputLine(String.Format(" --- Result: {0}-{1} machine {2} status {3}", wlkTR.ResultID, wlkTR.GUID, wlkTR.MachineName, wlkTR.Status), 3);
                    wlkTR.Filters = new List<wlkFilter>();
                    foreach (var filter in tr.GetAppliedFilters())
                    {
                        wlkTR.Filters.Add(crackFilter(filter));
                    }
                    wlkTR.Tasks = new List<wlkTask>();
                    foreach (var tsk in tr.GetTasks())
                    {
                        wlkTR.Tasks.Add(crackTask(tsk, !limited, wlkTR.Filters.Count>0));
                    }

                }
                //Console.WriteLine(String.Format("P5: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: {0} {1}", ex.Message, ex.StackTrace);
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                throw;
            }
            return wlkTR;
        }

        private wlkTask crackTask(Microsoft.Windows.Kits.Hardware.ObjectModel.Task msTask, bool savelog=false, bool getFilters=false)
        {
            wlkTask task = new wlkTask();
            task.Stage=msTask.Stage;
            task.TaskName = msTask.Name;
            task.TaskType = msTask.TaskType;
            task.Status = msTask.Status.ToString();


            //task.TaskCommandLine = WHCKHelper.getPrivateProperiesValue("TaskCommandLine", msTask) == null ? null : WHCKHelper.getPrivateFieldValue("taskCommandLine", msTask).ToString();           
            task.TaskCommandLine = getCMDForTask(msTask);
            if (savelog && (msTask.Status == TestResultStatus.Failed || msTask.Status == TestResultStatus.Canceled) && !String.IsNullOrWhiteSpace(_TestLogsSharePath))
            {
                //string path_stp = String.Format(@"{0}WHCK2_Logs\{1}\{2}\{3}\", _TestLogsSharePath, logoProject.Name, msTask.TestResult.Test.Name, msTask.TestResult.InstanceId);
                string path_stp = String.Format(@"{0}{1}\{2}\", _TestLogsSharePath, msTask.TestResult.Test.Name, msTask.TestResult.InstanceId);
                //Console.WriteLine(String.Format("P411: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
                    task.Logs=new List<string>();
                    foreach (TestLog tl in msTask.GetLogFiles())
                    {
                        try
                        {
                            //Console.WriteLine(String.Format("P4111_1: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
                            if (!Directory.Exists(path_stp))
                                Directory.CreateDirectory(path_stp);
                            task.Logs.Add(path_stp + tl.Name);
                            //Console.WriteLine(String.Format("P4111_2: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
                            if (File.Exists(path_stp + tl.Name))
                                File.Delete(path_stp + tl.Name);
                            //Console.WriteLine(String.Format("P4111_3: {0} {1} {2}", DateTime.Now, DateTime.Now.Millisecond, path_stp + tl.Name));
                            //path_stp = @"F:/Temp/";
                            tl.WriteLogTo(path_stp + tl.Name);
                        }
                        catch (Exception ex)
                        {
                            logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                            Console.WriteLine("ERROR: {0} {1}", ex.Message, ex.StackTrace);
                        }
                        //Console.WriteLine(String.Format("P4112: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
                    }

            }

            if (getFilters && msTask.AreFiltersApplied)
            {
                task.Filters = new List<wlkFilter>();
                foreach (var filter in msTask.GetAppliedFilters())
                {
                    task.Filters.Add(crackFilter(filter));
                }
            }


            //Console.WriteLine(String.Format("P412: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
            task.Tasks=new List<wlkTask>();
            foreach (var _tsk in msTask.GetChildTasks())
                task.Tasks.Add(crackTask(_tsk, savelog, getFilters));

            return task;
        }

        #endregion

        #region Submission
        public void CreateSubmission(string driverPath, string saveFileName)
        {
            String Message = String.Empty;

            checkForNotEmpty(objectType.lProject);

            if (packageWriter == null)
            {
                packageWriter = new PackageWriter(logoProject);
            }
            try
            {
                string savedir=new FileInfo(saveFileName).Directory.ToString();
                if (!Directory.Exists(savedir))
                    Directory.CreateDirectory(savedir);


                //TODO Remove in the next Build

                //foreach (Test test in p.GetTests())
                //{
                //    foreach (TestResult result in test.GetTestResults().Where(x => x.Status == TestResultStatus.Canceled || x.Status==TestResultStatus.NotRun))
                //    {
                //        result.Cancel();
                //    }
                //}


                // the steps needed to create a package are:
                // 1) create a LogoPackageWriter
                // 2) add drivers
                // 3) [optional] add supplemental content
                // 4) save to disk



                // Since packaging can be somewhat slow, as it may have a lot of files to read, compress and write
                // the packaging information does have an alerting mechanism
                // this will also be alerted for events when processing drivers
                packageWriter.SetProgressActionHandler((info) => { setPackStatus(String.Format("Package progress {0} of {1} : {2}", info.Current, info.Maximum, info.Message)); });


                // the AddDriver method has this definition.
                //  public bool AddDriver(
                //        string pathToDriver, 
                //        string pathToSymbols, 
                //        ReadOnlyCollection<Target> targets, 
                //        ReadOnlyCollection<string> locales, 
                //        out StringCollection errorMessages,
                //        out StringCollection warningMessages)
                // the path to symbols are optional, and can be null

                // each driver package can be associated with one or more targets, and can be from targets in different product instances.
                // the possible locales can be retrieved from LogoManager.GetLocaleList()
                // when adding a driver, the driver package is validated that it will be signalable, and additional checks will be performed
                // this means that if this task fails, the errorMessages and warningMessages will be filled out to provide information about any
                // problems encountered

                // for simplicity, we are going to add one driver package as gotten from the command line, 
                // and associate that with all of the targets in the project
                List<Target> targetList = new List<Target>();
                foreach (ProductInstance pi in logoProject.GetProductInstances())
                {
                    targetList.AddRange(pi.GetTargets());
                }

                // also for simplicity, we are going to use the first 3 locales returned by the GetLocaleList.
                List<string> localeList = new List<string>();
                foreach (string locale in ProjectManager.GetLocaleList())
                {
                    localeList.Add(locale);
                }

                StringCollection errorMessages;
                StringCollection warningMessages;

                // go ahead and call this API

                if (!String.IsNullOrEmpty(driverPath))
                {
                    if (packageWriter.AddDriver(driverPath, null, targetList.AsReadOnly(), localeList.AsReadOnly(), out errorMessages, out warningMessages) == false)
                    {
                        Message += String.Format("\nAdd driver failed to add this driver found at : {0}", driverPath);
                        foreach (string message in errorMessages)
                        {
                            Message += String.Format("\nError: {0}", message);
                        }
                    }

                    // warnings might not cause the method to fail, but may still be present
                    if (warningMessages.Count != 0)
                    {
                        Message += String.Format("\nAdd driver found warnings in the package found at : {0}", driverPath);
                        foreach (string message in warningMessages)
                        {
                            Message += String.Format("\nWarning: {0}", message);
                        }
                    }

                    if (!String.IsNullOrEmpty(Message))
                        setPackStatus(Message);
                }
                // and now call the save as
                // this save as does the bulk of the work
                packageWriter.Save(saveFileName);
            }
            catch (Exception ex)
            {
                logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                setPackStatus(Message+"\n"+ex.Message);
            }
            // now that we're done with writing the package, dispose it to make sure the file handle is closed
            packageWriter.Dispose();  
        }

        void QueryPackage(string packageName)
        {
            PackageManager pm = new PackageManager(packageName);
            Console.WriteLine("Package Manager Connection Type: " + pm.ConnectionType.ToString());
            foreach (ProjectInfo si in pm.GetProjectInfoList())
            {
                Console.WriteLine("\n\tName: " + si.Name.ToString());
                Console.WriteLine("\tTests passed: " + si.PassedCount);
                Console.WriteLine("\tTests failed: " + si.FailedCount);
                Console.WriteLine("\tTests NotRun: " + si.NotRunCount);
                Console.WriteLine("\tTests Total: " + si.TotalCount);
                Console.WriteLine("\tProject Status: " + si.Status);
                Console.WriteLine("\tProject Last Modified: " + si.ChangedDate.ToString());
            }
        }

        void ProjectAddDdriver(string pathToDriverDir)
        {
            throw new NotImplementedException();
            //w = new LogoPackageWriter(p, ConnectionType.SubmissionPackage);

            ////bad, bad code, as it assigns all platforms to just one driver package.
            //// each package should be be specifically set to the appropriate pi for platform.

            //foreach (ProductInstance pi in p.GetProductInstances())
            //{

            //    HashSet<OSPlatform> platforms = new HashSet<OSPlatform>();
            //    platforms.Add(pi.OSPlatform);
            //    w.AddDriver(  (p.Name.ToString(), pathToDriverDir, platforms);
            //}

        }

        void ProjectAddSymbols(string pathToSymbols)
        {
            throw new NotImplementedException();
            //if (w == null)
            //{
            //    w = new LogoPackageWriter(p, ConnectionType.SubmissionPackage);
            //}
            //w.AddSymbols(pathToSymbols);

        }

        
        public List<wlkProductType> listProductTypesEx()
        {
            List<wlkProductType> result = new List<wlkProductType>();
            checkForNotEmpty(objectType.prManager);

            foreach (ProductType pt in projectManager.GetProductTypes())
            {
                wlkProductType _pt = new wlkProductType();
                _pt.Name=pt.Name;
                _pt.Description=pt.Description;
                //TODO: finish
                foreach (Feature f in pt.GetFeatures())
                {
                    Console.WriteLine("\tFeature: " + f.Name.ToString());
                    foreach (Requirement r in f.GetRequirements())
                    {
                        Console.WriteLine("\t\tRequirement: " + r.Name.ToString());
                    }
                }
            }
            return result;
        }

        private void setPackStatus(String msg)
        {
            StreamWriter sw = new StreamWriter(@"C:\UI_LOGS\package.log",true);
            if (!packageStatus.ContainsKey(logoProject.Name))
                packageStatus.Add(logoProject.Name, "");
            packageStatus[logoProject.Name] = msg;
            sw.WriteLine(msg);
            sw.Close();
        }


        #endregion

        #region Filters

        public List<wlkFilter> listFilters()
        {
            List<wlkFilter> filters = new List<wlkFilter>();
            checkForNotEmpty(objectType.prManager);
            foreach (var filter in projectManager.GetFilters())
            {
                filters.Add(crackFilter(filter));
            }
            return filters;
        }

        private wlkFilter crackFilter(IFilter msfilter)
        {
            wlkFilter _filter = new wlkFilter();
            _filter.Title=msfilter.Title;
            _filter.ExpirationDate = msfilter.ExpirationDate??DateTime.MaxValue;
            _filter.FilterNumber = msfilter.FilterNumber;
            _filter.IsLogRequired = msfilter.IsLogRequired;
            _filter.IsResultRequired = msfilter.IsResultRequired;
            _filter.IssueDescription = msfilter.IssueDescription;
            _filter.IssueResolution = msfilter.IssueResolution;
            _filter.LastModifiedDate = msfilter.LastModifiedDate;
            _filter.ShouldFilterAllZeros = msfilter.ShouldFilterAllZeros;
            _filter.ShouldFilterNotRuns = msfilter.ShouldFilterNotRuns;
            _filter.Status = msfilter.Status.ToString();
            _filter.TestCommandLine = msfilter.TestCommandLine;
            _filter.Type = msfilter.Type.ToString();
            _filter.Version = msfilter.Version;
            _filter.Constraints = new List<string>();
            foreach (var constr in msfilter.Constraints)
                _filter.Constraints.Add(constr.Query);
            //_filter.LogNodes = new List<string>();
            //foreach (var lnode in msfilter.LogNodes)
            //    _filter.LogNodes.Add(lnode.);
            return _filter;
        }

        #endregion

        private string getCMDForTask(Microsoft.Windows.Kits.Hardware.ObjectModel.Task msTask)
        {
            string pattern = @"\[[^\]]*\]";
            String cmd = WHCKHelper.getPrivateProperiesValue("TaskCommandLine", msTask) == null ? null : WHCKHelper.getPrivateFieldValue("taskCommandLine", msTask).ToString();
            if (!String.IsNullOrWhiteSpace(cmd))
            {
                var pars = msTask.TestResult.GetParameters();
                MatchCollection matches = Regex.Matches(cmd, pattern);
                foreach (var match in matches)
                {
                    string cmdPar = match.ToString().Replace("[", "").Replace("]", "");
                    var par = pars.FirstOrDefault(x => x.Name.Equals(cmdPar, StringComparison.CurrentCultureIgnoreCase));
                    if (par != null)
                        cmd = cmd.Replace(match.ToString(), par.ActualValue);
                }

            }
            return cmd;
        }





        #region DEBUGING

        public List<String> getCmdForTest(String testName)
        {
            List<String> cmds = new List<string>();
            checkForNotEmpty(objectType.lProject);



            foreach (var test in logoProject.GetTests())
            {
                if (test.Name.ToUpper().Contains(testName.ToUpper()))
                {
                    foreach (var result in test.GetTestResults())
                    {
                        var Parameters = result.GetParameters();
                        foreach (var msTask in result.GetTasks())
                        {
                            Console.WriteLine(getCMDForTask(msTask));
                        }

                    }
                }
            }

            return cmds;
        }

        public void repro()
        {
            string testName = "DXGI GDI Interop D3D10.1";//"DXVA High Definition Video Processing - CreateVideoSurface";
            Test test = logoProject.GetTests().First(x => x.Name == testName);
            foreach (TestResult tr in test.GetTestResults())
            {
                var aa = tr.Machine;
                var bb = tr.Target;
                var cc = tr.Test;
                foreach (var tsk in tr.GetTasks())
                {
                    visitResultTask(tsk);

                }
            }
        }

        private void visitResultTask(Microsoft.Windows.Kits.Hardware.ObjectModel.Task tsk)
        {

            var tt = tsk.TestResult;
            tsk.GetAppliedFilters();
            //if (tsk.Status == TestResultStatus.Failed || tsk.Status == TestResultStatus.Canceled)
            //{
            Console.WriteLine(String.Format("P411: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
            foreach (TestLog tl in tsk.GetLogFiles())
            {
                try
                {
                    Console.WriteLine(String.Format("P4111_1: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
                    tl.WriteLogTo(@"F:/Temp/" + tl.Name);
                }
                catch (Exception ex)
                {
                    logger.OutputLine(String.Format("ERROR: {0} {1}", ex.Message, ex.StackTrace), 1);
                }
                Console.WriteLine(String.Format("P4112: {0} {1}", DateTime.Now, DateTime.Now.Millisecond));
            }
            foreach (var cTask in tsk.GetChildTasks())
            {
                visitResultTask(cTask);
            }
            //}


        }
        #endregion


        #region oldCode




        #endregion


    }


}
