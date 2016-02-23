using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

/* Present working Model Namespace */
using tsiewhckweb.Models;

namespace tsiewhckweb.Controllers
{
    public class ConfigurationController : Controller
    {
        /// <summary>
        /// ConfigurationModel private class reference
        /// </summary>
        private ConfigurationModel model;

        /// <summary>
        /// ConfigurationController class default constructor
        /// </summary>
        public ConfigurationController()
        {
            model = new ConfigurationModel();
        }

        /// <summary>
        /// GET: /Configuration/
        /// </summary>
        /// <returns>View result set.</returns>
        public ActionResult Index()
        {
            Controller con = (Controller)ControllerContext.Controller;
            List<Project_Group> configView = model.IndexHelper( ref con );
            return View( configView );
        }

        /// <summary>
        /// GET: /Configuration/Details/5
        /// </summary>
        /// <param name="id">Row ID primary key of which details to retrieve.</param>
        /// <returns>Greater data detail of the selected row.</returns>
        public ActionResult Details( int id = 0 )
        {
            Project_Group project_group = model.DetailsHelper( id );

            if( null == project_group )
            {
                return HttpNotFound();
            }
            return View( project_group );
        }

        /// <summary>
        /// GET: /Configuration/Create
        /// </summary>
        /// <returns>Incomplete view form to fill out.</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Overloaded POST: /Configuration/Create
        /// Solomon's browse file dialog control for an input test package results file.
        /// We need from this the actual test package results summary.
        /// </summary>
        /// <param name="file">Input file path of the package file.</param>
        /// <returns>Test package result text data/contents.</returns>
        [HttpPost]
        public ActionResult GetResults( HttpPostedFileBase file )
        {
            Controller con = (Controller)ControllerContext.Controller;
            model.GetResultsHelper( file, ref con );

            Response.Redirect( "~/Configuration/Create" );
            return View();
        }
        /// <summary>
        /// POST: /Configuration/Create
        /// </summary>
        /// <param name="test_config">Incomming row AND all related rows to insert to DB through the Test_Config table
        /// and all its connected relations.</param>
        /// <returns>Test_Config table row and all related table rows that were inserted to.</returns>
        [HttpPost]
        public ActionResult Create( Project_Group project_group )
        {
            Controller con = (Controller)ControllerContext.Controller;
            project_group = model.CreateHelper( project_group, ref con );
            model.SavePackageSubTestresults( model.Project_GroupDB.Test_Config.ConfigNumID, ref con );
            if( ModelState.IsValid )
                return RedirectToAction( "Index" );
            else
                return View( project_group );
        }
        /// <summary>
        /// GET: /Configuration/Edit/5
        /// </summary>
        /// <param name="id">Record ID provided by Project_Group table and ConfigNum field.</param>
        /// <returns>View page with the data record/row to edit.</returns>
        public ActionResult Edit( int id = 0 )
        {
            Project_Group project_group = model.EditHelper( id );

            if( null == project_group )
            {
                return HttpNotFound();
            }
            return View( project_group );
        }
        /// <summary>
        /// POST: /Configuration/Edit/5
        /// </summary>
        /// <param name="test_config">Data record/row of the Test_Config table plus relating tables to edit.</param>
        /// <returns>The modified Project_Group table plus all relating tables to the Edit View page.</returns>
        [HttpPost]
        public ActionResult Edit( Project_Group project_group )
        {
            if( ModelState.IsValid )
            {
                project_group = model.EditAndSaveHelper( project_group );
            }
            return View( project_group );
        }
        /// <summary>
        /// GET: /Configuration/Delete/5
        /// </summary>
        /// <param name="id">Record ID of row to delete.</param>
        /// <returns>The data record/row found to be deleted.</returns>
        public ActionResult Delete( int id = 0 )
        {
            Controller con = (Controller)ControllerContext.Controller;
            Project_Group project_group = model.DeleteHelper( ref con, id );

            if( null == project_group )
            {
                return HttpNotFound();
            }
            return View( project_group );
        }
        /// <summary>
        /// POST: /Configuration/Delete/5
        /// </summary>
        /// <param name="id">Record ID to delete from the tsiewhckweb database.</param>
        /// <returns>Back to View Index of the Configuration view page without anything else.</returns>
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed( int id )
        {
            Controller con = (Controller)ControllerContext.Controller;
            Project_Group project_group = model.DeleteConfirmedHelper( id, ref con );
            if( null == project_group )
                throw new ApplicationException( "The Project_Group row specified for deletion was not found." );
            return RedirectToAction( "Index" );
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