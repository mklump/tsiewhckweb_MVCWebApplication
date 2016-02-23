using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Intel.WHQLCert.WHCKLog;

namespace Intel.WHQLCert
{

    public class WHCKLogger
    {
        public static DelegatedStateLogger PoolTestScheduleLogger;
        public static DelegatedStateLogger MachineTestScheduleLogger;

        public delegate void poolStateLoggerAddDelegate(String name, String text, int severity);

        public static poolStateLoggerAddDelegate poolStateLoggerAdd = null;
 

        private static System.IO.StreamWriter _Output = null;
        private static Object _classLock = new object();
        public string _LogFile = "c://UI_LOGS//WLK2_All.log";
        public int _LogLevel = 1;
        public string _LogName = "WLK2_All";

        public WHCKLogger()
        {

        }


        public void OutputToTestSchedule(string Msg, SeverityLevel Severity) {
            WHCKLog.EntryFilter Filter = new WHCKLog.EntryFilter();
            Filter.EntrySeverity = Severity;
            WHCKLogger.PoolTestScheduleLogger.AddDelegate.Invoke(
                _LogName, Msg, Filter, null);
        }

        public void OutputLine(string s, int severity, string file=null)
        {
            if (invokeExternalLogger(s, severity, file))
                return;

            Output(s + Environment.NewLine, severity, file);
        }

        public void Output(string s, int severity, string file=null)
        {


            if (invokeExternalLogger(s, severity, file))
                return;


            string logfile=_LogFile;
            try
            {
                if (severity <= _LogLevel)
                {
                    if (!String.IsNullOrEmpty(file))
                        logfile = file;

                    lock (_classLock)
                    {
                        if (_Output == null)
                            _Output = new System.IO.StreamWriter(logfile, true, System.Text.UnicodeEncoding.Default);

                        _Output.Write(System.DateTime.Now + " | " + severity + " | " + s, new object[0]);

                        if (_Output != null)
                        {
                            _Output.Close();
                            _Output = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, new object[0]);
            }
        }

        private bool invokeExternalLogger(string s, int severity, string file)
        {
            bool result = false;
            if (poolStateLoggerAdd == null)
                return false;

            try
            {

                string logfile = _LogFile;
                if (!String.IsNullOrEmpty(file))
                    logfile = file;
                FileInfo fi = new FileInfo(logfile);
                string name = fi.Name.Replace(".txt","");
                poolStateLoggerAdd(name, s, severity);
                result = true;
            }
            catch (Exception ex)
            {
                throw;
            }
            return result;

        }

    }
    /*
     * Region copy pasted from StateLogger.cs
     * We did not want to create a dependency on TestServiceModel.dll
     * Future TODO: move nondependent pieces from StateLogger.cs to Loggers own project and reference where needed.
     */
    #region Copy Pasted
    namespace WHCKLog {
        public class EntryFilter {
            private SeverityLevel? _EntrySeverity = null;
            private int? _EntryId = null;
            private IEnumerable<int> _EntrySourceIds = null;
            private DateTime? _StartDate = null;
            private DateTime? _EndDate = null;

            public SeverityLevel? EntrySeverity { get { return _EntrySeverity; } set { _EntrySeverity = value; } }
            public int? EntryId { get { return _EntryId; } set { _EntryId = value; } }
            public int? EntrySourceId {
                get { return _EntrySourceIds != null && _EntrySourceIds.Count() > 0 ? _EntrySourceIds.First() : new int?(); }
                set {
                    if (value.HasValue) {
                        List<int> _TempEntrySourceIds = new List<int>();
                        _TempEntrySourceIds.Add(value.Value);
                        _EntrySourceIds = _TempEntrySourceIds;
                    }
                    else {
                        _EntrySourceIds = null;
                    }
                }
            }
            public IEnumerable<int> EntrySourceIds { get { return _EntrySourceIds; } set { _EntrySourceIds = value; } }
            public DateTime? StartDate { get { return _StartDate; } set { _StartDate = value; } }
            public DateTime? EndDate { get { return _EndDate; } set { _EndDate = value; } }
        }

        public delegate void AddEntryDelegate(string EntryName, string Message, EntryFilter AdditionalInfo, List<string> LogsChecked);
        public delegate void DeleteEntryDelegate(string EntryName, List<string> LogsChecked);
        public delegate List<string> GetEntryDelegate(string EntryName, EntryFilter Filter, List<string> LogsChecked);
        public delegate List<string> GetBatchEntryDelegate(IEnumerable<string> EntryName, EntryFilter Filter, List<string> LogsChecked);

        public class DelegatedStateLogger {
            public AddEntryDelegate AddDelegate { get; private set; }
            public DeleteEntryDelegate DeleteDelegate { get; private set; }
            public GetEntryDelegate GetDelegate { get; private set; }
            public GetBatchEntryDelegate GetBatchDelegate { get; private set; }

            public DelegatedStateLogger(AddEntryDelegate AddDelegate, DeleteEntryDelegate DeleteDelegate,
                GetEntryDelegate GetDelegate, GetBatchEntryDelegate GetBatchDelegate) {
                this.AddDelegate = AddDelegate;
                this.DeleteDelegate = DeleteDelegate;
                this.GetDelegate = GetDelegate;
                this.GetBatchDelegate = GetBatchDelegate;
            }
        }

        public static class SeverityExtension {
            public static string EnglishErrorLevel(this SeverityLevel val) {
                if (val == SeverityLevel.Debug)
                    return "Debug";
                else if (val >= SeverityLevel.Information && val < SeverityLevel.Update)
                    return "Information";
                else if (val >= SeverityLevel.Update && val < SeverityLevel.Warning)
                    return "Update";
                else if (val >= SeverityLevel.Warning && val < SeverityLevel.Error)
                    return "Warning";
                else if (val >= SeverityLevel.Error && val < SeverityLevel.FatalError)
                    return "Error";
                else if (val >= SeverityLevel.FatalError)
                    return "Fatal Error";

                return "Invalid Severity Level";
            }

            public static SeverityLevel Parse(string SeverityName) {
                if (SeverityName == null)
                    throw new ArgumentNullException("Severity Name");

                string Temp = new string(SeverityName.ToCharArray());
                Temp.Trim();
                if (Temp.Equals("Debug", StringComparison.CurrentCultureIgnoreCase))
                    return SeverityLevel.Debug;
                else if (Temp.Equals("Information", StringComparison.CurrentCultureIgnoreCase))
                    return SeverityLevel.Information;
                else if (Temp.Equals("Update", StringComparison.CurrentCultureIgnoreCase))
                    return SeverityLevel.Update;
                else if (Temp.Equals("Warning", StringComparison.CurrentCultureIgnoreCase))
                    return SeverityLevel.Warning;
                else if (Temp.Equals("Error", StringComparison.CurrentCultureIgnoreCase))
                    return SeverityLevel.Error;
                else if (Temp.Equals("FatalError", StringComparison.CurrentCultureIgnoreCase) ||
                    Temp.Equals("Fatal Error", StringComparison.CurrentCultureIgnoreCase))
                    return SeverityLevel.FatalError;

                SeverityLevel Ret;
                if (Enum.TryParse(Temp, true, out Ret))
                    return Ret;

                throw new ArgumentException("SeverityName is not a SeverityLevel");
            }

        }
        public enum SeverityLevel : int {

            Information = 0,
            Update = 1,
            Warning = 3,
            Error = 5,
            FatalError = 9,

            Debug = -1

        };
    };

#endregion // End Copy Pasted
}
