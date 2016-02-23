using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using tsiewhckweb.Models;

namespace tsiewhckweb.Controllers
{
    public class TestingStatusController : Controller
    {
        /// <summary>
        /// ConfigurationModel private class reference
        /// </summary>
        private TestingStatusModel model;

        /// <summary>
        /// TestingStatusController class default constructor.
        /// </summary>
        public TestingStatusController()
        {
            model = new TestingStatusModel();
        }
        /// <summary>
        /// GET: /TestingStatus/
        /// </summary>
        /// <returns>TestingStatus view.</returns>
        public ActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// Handler funtion for POST: /TestingStatus
        /// TestingStatusController POST helper fuction responsible for retreiving, 
        /// calculationg, and displaying the Pass and Fail percentages of each
        /// Project Group by Name.
        /// </summary>
        /// <returns>View back to the current running TestingStatus view page with the
        /// Pass/Fail per Project_Group results stat percentages.</returns>
        [HttpPost]
        public ActionResult GetProjectPassFail()
        {
            Controller con = ( Controller ) ControllerContext.Controller;
            model.PassFailStats = model.GetProjectPassing( ref con );
            Response.Redirect( "~/TestingStatus/Index" );
            return View( model.PassFailStats.ToList() );
        }
        /// <summary>
        /// Handler funtion for POST: /TestingStatus
        /// TestingStatusController POST helper fuction responsible for retreiving, 
        /// calculationg, and displaying the Pass and Fail percentages of each
        /// Test Case by Name.
        /// </summary>
        /// <returns>View back to the current running TestingStatus view page with the
        /// Pass/Fail per TestCase results stat percentages.</returns>
        [HttpPost]
        public ActionResult GetTestCasePassFail()
        {
            Controller con = ( Controller ) ControllerContext.Controller;
            model.PassFailStats = model.GetTestCasePassing( ref con );
            Response.Redirect( "~/TestingStatus/Index" );
            return View( model.PassFailStats.ToList() );
        }
        /// <summary>
        /// Handler funtion for POST: /TestingStatus
        /// TestingStatusController POST helper fuction responsible for retreiving, 
        /// calculationg, and displaying the Pass and Fail percentages of each
        /// Driver by Name.
        /// </summary>
        /// <returns>View back to the current running TestingStatus view page with the
        /// Pass/Fail per Driver Name results stat percentages.</returns>
        [HttpPost]
        public ActionResult GetDriverPassFail()
        {
            Controller con = ( Controller ) ControllerContext.Controller;
            model.PassFailStats = model.GetDriverPassing( ref con );
            Response.Redirect("~/TestingStatus/Index");
            return View( model.PassFailStats.ToList() );
        }
        /// <summary>
        /// GET: /TestingStatus/Details/5
        /// </summary>
        /// <param name="id">Project_GroupID row identification value.</param>
        /// <returns>The idientified Project_Group row.</returns>
        public ActionResult Details( int id = 0 )
        {
            Project_Group project_group = model.DB.Project_Group.Find( id );
            if( project_group == null )
            {
                return HttpNotFound();
            }
            return View(project_group);
        }
        /// <summary>
        /// Overriden derived class Dispose() function from System.Web.Mvc.Controller Abstract Class.
        /// </summary>
        /// <param name="disposing">Dispose the DB connection either true or false.</param>
        protected override void Dispose( bool disposing )
        {
            model.DisposeHelper( disposing );
            base.Dispose( disposing );
        }
    }
}