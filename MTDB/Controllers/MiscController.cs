using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MTDB.Controllers
{
    public class MiscController : Controller
    {
        [Route("legal")]
        [Route("misc/legal")]
        public ActionResult Legal()
        {
            return View();
        }
    }
}