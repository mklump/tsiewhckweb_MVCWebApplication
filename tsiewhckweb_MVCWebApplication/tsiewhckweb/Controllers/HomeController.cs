using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace tsiewhckweb.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Pronunciation: Wick Web. Declaration: WHCK Web.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "WHCK (Windows Hardware Certification Kit) for Windows 8+ is the successor of WHQL.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "solomon.kinard@intel.com";

            return View();
        }

        public ActionResult Configuration()
        {
            ViewBag.Message = "Please follow the instructions on this Page as shown.";

            return View();
        }
    }
}
