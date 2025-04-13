using DAL.ServiceLayer.Models.LogsModels;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Http;
using ServiceStack.Text;
using System.Text.Json;
using System.Web;

namespace DAL.ServiceLayer.LogsHelper;

public class AppUserLogsHelper : LogService
{
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly string _LogId;
    public AppUserLogsHelper(ConfigHandler configHandler) : base(configHandler)
    {
        _LogId = configHandler.LogId;
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

    }
    public async Task<string> GetLogResponse(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;
        await using var responseBody = _recyclableMemoryStreamManager.GetStream();
        try
        {
            context.Response.Body = responseBody;
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var data = !text.Contains("DOCTYPE") ? text : "PAGE HTML RESPONSE";
            await responseBody.CopyToAsync(originalBodyStream);
            return data;
        }

        catch (Exception ex)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            return $"Exception : {ex.Message}";
        }
    }
    public async Task<string> GetLogRequest(HttpContext context)
    {
        if (context.Request.Method.ToUpper() == "GET")
        {
            return "PAGE GET REQUEST";
        }
        else
        {
            context.Request.EnableBuffering();
            await using var requestStream = _recyclableMemoryStreamManager.GetStream();
            try
            {
                await context.Request.Body.CopyToAsync(requestStream);
                context.Request.Body.Position = 0;
                return await ReadStreamInChunks(requestStream);
            }
            catch (Exception ex)
            {
                await context.Request.Body.CopyToAsync(requestStream);
                context.Request.Body.Position = 0;
                return $"Exception : {ex.Message}";
            }
        }
    }
    public string GetRequestHeaders(IHeaderDictionary keyValues)
    {
        Headers headers = new Headers();
        foreach (var item in keyValues)
        {
            if (item.Key.ToLower() == "accept")
                headers.Accept = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "host")
                headers.Host = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "ip")
                headers.IP = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "deviceid")
                headers.DeviceId = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "latitude")
                headers.Latitude = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "longitude")
                headers.Longitude = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "platform")
                headers.Platform = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "mobilemodel")
                headers.MobileModel = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "appversion")
                headers.AppVersion = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "isrefreshtoken")
                headers.IsRefreshToken = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "osversion")
                headers.OSVersion = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "language")
                headers.Language = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "x-forwarded-for")
                headers.XForwardedFor = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
            else if (item.Key.ToLower() == "x-real-ip")
                headers.XRealIP = string.IsNullOrEmpty(item.Value) ? "" : item.Value;
        }

        return System.Text.Json.JsonSerializer.Serialize(headers);
    }
    public Log GetLogModel(LogModel model)
    {
        bool sts = false;
        //if (!string.IsNullOrWhiteSpace(model.ResBody))
        //{
        //    using (JsonDocument doc = JsonDocument.Parse(model.ResBody))
        //    {
        //        JsonElement root = doc.RootElement;

        //        if (root.ValueKind == JsonValueKind.Object)
        //        {
        //            if (root.TryGetProperty("status", out JsonElement statusElement) &&
        //                statusElement.TryGetProperty("isSuccess", out JsonElement isSuccessElement))
        //            {
        //                sts = isSuccessElement.GetBoolean();
        //            }
        //        }
        //        else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        //        {
        //            var firstItem = root[0];
        //            if (firstItem.TryGetProperty("status", out JsonElement statusElement) &&
        //                statusElement.TryGetProperty("isSuccess", out JsonElement isSuccessElement))
        //            {
        //                sts = isSuccessElement.GetBoolean();
        //            }
        //        }
        //    }
        //}

        if (!model.IsExceptionFromResponse && !model.IsExceptionFromRequest)
        {
            if (!string.IsNullOrWhiteSpace(model.ResBody))
            {
                using (JsonDocument doc = JsonDocument.Parse(model.ResBody))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("status", out JsonElement statusElement) &&
                        statusElement.TryGetProperty("isSuccess", out JsonElement isSuccessElement))
                    {
                        sts = isSuccessElement.GetBoolean();
                    }
                }
            }
        }

        Log u = new Log();
        u.Method = model.Method;
        u.Path = model.Path;
        u.QueryString = model.QueryString;
        u.UserId = model.UserId;
        u.Action = model.Action;
        u.RequestBody = model.ReqBody;
        u.ResponseBody = model.ResBody;
        u.StartTime = model.StartTime;
        u.EndTime = DateTime.Now;
        u.Controller = model.Controller;
        u.IsException = model.IsExceptionFromRequest || model.IsExceptionFromResponse;
        if (model.IsExceptionFromRequest)
            u.Exception = model.ReqBody;
        else if (model.IsExceptionFromResponse)
            u.Exception = model.ResBody;
        else
            u.Exception = "";
        u.RequestHeaderJson = model.RequestHeaders;
        u.LogId = _LogId;
        u.Status = sts;
        return u;
    }

    public async Task<bool> SaveAppUserLogs(Log log)
    {
        return await InsertLogsAsync(log);
    }
    private async Task<string> ReadStreamInChunks(Stream stream)
    {
        const int readChunkBufferLength = 4096;

        stream.Seek(0, SeekOrigin.Begin);

        using var textWriter = new StringWriter();
        using var reader = new StreamReader(stream);

        var readChunk = new char[readChunkBufferLength];
        int readChunkLength;

        do
        {
            readChunkLength = await reader.ReadBlockAsync(readChunk, 0, readChunkBufferLength);
            await textWriter.WriteAsync(readChunk, 0, readChunkLength);
        } while (readChunkLength > 0);

        string val = HttpUtility.UrlDecode(textWriter.ToString());
        if (!string.IsNullOrEmpty(val))
            return System.Text.Json.JsonSerializer.Serialize(val);
        else
            return "PAGE GET REQUEST";
    }


}