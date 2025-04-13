using DAL.DatabaseLayer.DataContext;
using DAL.ServiceLayer.Models.LogsModels;
using DAL.ServiceLayer.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace DAL.ServiceLayer.LogsHelper;

public class LogService
{
    private bool _alreadyDisposed = false;
    private readonly IConfiguration _config;
    public LogService(ConfigHandler configHandler)
    {
        _config = configHandler._config;
    }
    public async Task<bool> InsertLogsAsync(Log model)
    {

        //if (string.IsNullOrEmpty(model.Exception))
        //{
        //    model.Exception = "Exception";
        //}

        //if (string.IsNullOrEmpty(model.QueryString))
        //{
        //    model.QueryString = "QueryString";
        //}

        try
        {
            model.StartTime = model.StartTime != DateTime.MinValue ? model.StartTime : DateTime.Now;
            model.EndTime = DateTime.Now;
            string query = "Insert Into Logs (LogId,Controller,Action,Path,Method,QueryString,RequestBody,ResponseBody,UserId,EndTime,IsException,Exception,RequestHeaderJson,StartTime,Status) " +
                "Values (@LogId,@Controller,@Action,@Path,@Method,@QueryString,@RequestBody,@ResponseBody,@UserId,@EndTime,@IsException,@Exception,@RequestHeaderJson,@StartTime,@Status)";
            SqlParameter[] parameter = new SqlParameter[15];
            parameter[0] = new SqlParameter("@LogId", model.LogId);
            parameter[1] = new SqlParameter("@Controller", model.Controller);
            parameter[2] = new SqlParameter("@Action", model.Action);
            parameter[3] = new SqlParameter("@Path", model.Path);
            parameter[4] = new SqlParameter("@Method", model.Method);
            parameter[5] = new SqlParameter("@QueryString", model.QueryString);
            parameter[6] = new SqlParameter("@RequestBody", model.RequestBody);
            parameter[7] = new SqlParameter("@ResponseBody", model.ResponseBody);
            parameter[8] = new SqlParameter("@UserId", model.UserId);
            parameter[9] = new SqlParameter("@EndTime", model.EndTime);
            parameter[10] = new SqlParameter("@IsException", model.IsException);
            parameter[11] = new SqlParameter("@Exception", model.Exception);
            parameter[12] = new SqlParameter("@RequestHeaderJson", model.RequestHeaderJson);
            parameter[13] = new SqlParameter("@StartTime", model.StartTime);
            parameter[14] = new SqlParameter("@Status", model.Status);
            return await new AdoContext(_config, true).ExecuteInsertQuery(query, parameter);//
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
    }

    #region Dispose
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (_alreadyDisposed)
            return;
        _alreadyDisposed = true;
    }

    #endregion
}
