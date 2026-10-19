using System.Web;
using System.Web.Mvc;

namespace OnlineVotingSystem
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new OnlineVotingSystem.Services.DemoProtectionFilter());
        }
    }
}

