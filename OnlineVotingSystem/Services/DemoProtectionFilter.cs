using System;
using System.Web.Mvc;
namespace OnlineVotingSystem.Services
{
    public sealed class DemoProtectionFilter:ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if(!DemoEnvironment.Enabled)return;
            try{using(DemoEnvironment.Open()) { }}catch(Exception){context.Result=new HttpNotFoundResult("This local DEMO installation is not authorized.");return;}
            var controller=Convert.ToString(context.RouteData.Values["controller"]);
            var action=Convert.ToString(context.RouteData.Values["action"]);
            if(controller=="Account"&&action=="Register") {context.Result=new HttpStatusCodeResult(403,"DEMO accounts must be provisioned locally; real student registration is disabled.");return;}
            if(controller=="Manage"&&context.HttpContext.Request.HttpMethod=="POST") {context.Result=new HttpStatusCodeResult(403,"Password and security changes are disabled in the demonstration.");return;}
            if(Array.Exists(new[]{"Candidates","CandidateStructures","Structures","VotingSessions","Users","Roles"},name=>name==controller))context.Result=new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary(new{controller="Demo",action="Index"}));
        }
    }
}
