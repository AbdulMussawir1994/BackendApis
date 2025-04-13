using DAL.ServiceLayer.Utilities;

namespace DAL.ServiceLayer.Models;

public class MobileResponse<T>
{
    public readonly ConfigHandler _configHandler;
    public readonly string _serviceName;

    public MobileResponse(ConfigHandler configHandler, string serviceName)
    {
        _configHandler = configHandler;
        _serviceName = serviceName;
        LogId = configHandler?.LogId;
        RequestDateTime = configHandler?.RequestedDateTime;
    }

    public string LogId { get; set; }
    public Status Status { get; set; } = Status.Error();
    public T Content { get; set; }
    public string RequestDateTime { get; set; } = DateTime.UtcNow.ToString("o");

    public MobileResponse<T> SetError(string code, string message, T content = default)
    {
        Status = Status.Error(code, message);
        Content = content;
        return this;
    }

    public MobileResponse<T> SetSuccess(string code, string message, T content = default)
    {
        Status = Status.Success(code, message);
        Content = content;
        return this;
    }

    //public bool IsSuccess => Status.IsSuccess;
}

public class Status
{
    public bool IsSuccess { get; set; }
    public string Code { get; set; }
    public StatusType StatusType { get; set; }
    public string StatusMessage { get; set; }

    private Status(bool isSuccess, string code, StatusType statusType, string message)
    {
        IsSuccess = isSuccess;
        Code = code;
        StatusType = statusType;
        StatusMessage = message;
    }

    public static Status Success(string code, string message) =>
        new Status(true, code, StatusType.Success, message);

    public static Status Error(string code = "ERROR-0000", string message = "ERROR") =>
        new Status(false, code, StatusType.Error, message);
}

public enum StatusType
{
    Info,
    Success,
    Error,
    Exception
}