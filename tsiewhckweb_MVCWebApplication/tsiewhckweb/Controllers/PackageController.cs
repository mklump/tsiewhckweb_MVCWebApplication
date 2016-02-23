using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using tsiewhckweb.Models;

namespace tsiewhckweb.Controllers
{
    public class PackageController : Controller
    {
        private PackageModel package = new PackageModel();
        //
        // GET: /Package/

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index( HttpPostedFileBase file )
        {
            ViewBag.whckVersion = package.WhckVersion(file);
            ViewBag.TestResult = ViewBag.whckVersion;

            return View();
        }
    }
}
