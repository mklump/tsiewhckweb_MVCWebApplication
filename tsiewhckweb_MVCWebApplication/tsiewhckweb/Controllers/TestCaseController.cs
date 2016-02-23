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
    public class TestCaseController : Controller
    {
        private TestCaseModel testcase;

        /// <summary>
        /// TestCaseController default constructor.
        /// </summary>
        public TestCaseController()
        {
            testcase = new TestCaseModel();
        }
        /// <summary>
        /// GET: /TestCase/ Index() controller function
        /// </summary>
        /// <returns>View result set of the TestCase, Component, and Project tables.</returns>
        public ActionResult Index()
        {
            var viewResult = testcase.IndexHelper();
            return View( viewResult.ToList() );
        }
        /// <summary>
        /// GET: /TestCase/Details/5
        /// </summary>
        /// <param name="id">TestCaseID record ID to retrieve.</param>
        /// <returns>View/Result Set row for that single TestCaseID.</returns>
        public ActionResult Details( int id = 0 )
        {
            TestCase row = testcase.DetailsHelper( id );
            
            if( null == row )
            {
                return HttpNotFound();
            }
            return View( row );
        }
        /// <summary>
        /// GET: /TestCase/Create
        /// </summary>
        /// <returns>View with the resulting created TestCase row.</returns>
        public ActionResult Create()
        {
            return View();
        }
        /// <summary>
        /// POST: /TestCase/Create
        /// </summary>
        /// <param name="testcase">The TestCase data row to create.</param>
        /// <returns>The TestCase data row that was created.</returns>
        [HttpPost]
        public ActionResult Create( TestCase paramTestCase )
        {
            Controller con = (Controller)ControllerContext.Controller;
            paramTestCase = testcase.CreateHelper( paramTestCase, ref con );
            if( ModelState.IsValid )
                return RedirectToAction( "Index" );
            else
                return View( testcase );
        }

        ////
        //// GET: /TestCase/Edit/5

        //public ActionResult Edit(int id = 0)
        //{
        //    Project_Group project_group = db.Project_Group.Find(id);
        //    if (project_group == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    ViewBag.ConfigNumID = new SelectList(db.Test_Config, "ConfigNumID", "ConfigNumID", project_group.ConfigNumID);
        //    return View(project_group);
        //}

        ////
        //// POST: /TestCase/Edit/5

        //[HttpPost]
        //public ActionResult Edit(Project_Group project_group)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(project_group).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    ViewBag.ConfigNumID = new SelectList(db.Test_Config, "ConfigNumID", "ConfigNumID", project_group.ConfigNumID);
        //    return View(project_group);
        //}

        ////
        //// GET: /TestCase/Delete/5

        //public ActionResult Delete(int id = 0)
        //{
        //    Project_Group project_group = db.Project_Group.Find(id);
        //    if (project_group == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(project_group);
        //}

        ////
        //// POST: /TestCase/Delete/5

        //[HttpPost, ActionName("Delete")]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Project_Group project_group = db.Project_Group.Find(id);
        //    db.Project_Group.Remove(project_group);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}

        /// <summary>
        /// Overriden derived class Dispose() function from System.Web.Mvc.Controller Abstract Class.
        /// </summary>
        /// <param name="disposing">Dispose the DB connection either true or false.</param>
        protected override void Dispose( bool disposing )
        {
            testcase.DisposeHelper( disposing );
            base.Dispose( disposing );
        }
    }
}