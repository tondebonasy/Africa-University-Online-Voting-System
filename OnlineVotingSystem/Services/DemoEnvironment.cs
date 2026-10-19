using System;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;

namespace OnlineVotingSystem.Services
{
    public static class DemoEnvironment
    {
        public static bool Enabled { get { return ConfigurationManager.AppSettings["Demo:Enabled"] == "true"; } }
        public static string DatabaseName { get { return ConfigurationManager.AppSettings["Demo:Database"]; } }
        public static string AdminId { get { return ConfigurationManager.AppSettings["Demo:AdminUserId"]; } }
        public static string BackupDirectory { get { return ConfigurationManager.AppSettings["Demo:BackupDirectory"]; } }
        public static SqlConnection Open()
        {
            if (!Enabled || ConfigurationManager.AppSettings["AppEnvironment"] != "Development") throw new InvalidOperationException("The demo is disabled outside its explicitly configured Development installation.");
            if (HttpContext.Current != null && !HttpContext.Current.Request.IsLocal) throw new InvalidOperationException("Demo operations are restricted to this local computer.");
            var plain = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
            var entity = new EntityConnectionStringBuilder(ConfigurationManager.ConnectionStrings["OnlineVotingSystemEntities"].ConnectionString);
            var mapped = new SqlConnectionStringBuilder(entity.ProviderConnectionString);
            if (plain.DataSource != @"(localdb)\MSSQLLocalDB" || mapped.DataSource != plain.DataSource || plain.InitialCatalog != DatabaseName || mapped.InitialCatalog != DatabaseName || !plain.IntegratedSecurity || !mapped.IntegratedSecurity || !(DatabaseName == "online_voting_system_demo" || System.Text.RegularExpressions.Regex.IsMatch(DatabaseName ?? "", @"^online_voting_system_demo_test_[a-f0-9]{10}$"))) throw new InvalidOperationException("Unsafe database target. Demo operations require the isolated LocalDB database and matching Identity/EDMX connections.");
            var connection = new SqlConnection(plain.ConnectionString);
            try
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM dbo.DemoInstallation WHERE Id=1 AND InstanceId=@instance AND AdminUserId=@admin";
                    Guid instance;
                    if (!Guid.TryParse(ConfigurationManager.AppSettings["Demo:InstanceId"], out instance)) throw new InvalidOperationException("Missing demo installation marker.");
                    cmd.Parameters.AddWithValue("@instance", instance); cmd.Parameters.AddWithValue("@admin", AdminId ?? "");
                    if (Convert.ToInt32(cmd.ExecuteScalar()) != 1) throw new InvalidOperationException("The database is not this authorized demo installation.");
                }
                return connection;
            }
            catch { connection.Dispose(); throw; }
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class DemoOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try { using (DemoEnvironment.Open()) { } }
            catch (Exception) { context.Result = new HttpNotFoundResult("This local development demonstration is not enabled."); }
        }
    }
}
