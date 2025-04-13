using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace DAL.ServiceLayer.BaseController;

public class WebBaseController : ControllerBase
{
    private readonly ConfigHandler _configHandler;
    public WebBaseController(ConfigHandler configHandler)
    {
        _configHandler = configHandler;
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [NonAction]
    [HttpGet("api/[controller]/{ServiceObject}/{ServiceMethod}/{model}")]
    public MobileResponse<CustomResponse> ModelValidator(object model)
    {
        MobileResponse<CustomResponse> response = new MobileResponse<CustomResponse>(_configHandler, "modelvalidator") { Content = new CustomResponse(), LogId = Guid.NewGuid().ToString(), RequestDateTime = DateTime.Now.ToString(), Status = { Code = "", StatusMessage = "", IsSuccess = false, StatusType = StatusType.Error } };

        if (model != null && !TryValidateModel(model))
        {
            response.Content.Message = "Invalid Request Model";
            response.Content.IsSuccess = false;
            response.Status.Code = "Error-4000";
            response.Status.IsSuccess = false;
            response.Status.StatusType = StatusType.Error;
            response.Status.StatusMessage = string.Join(" ", ModelState.Values
                                              .SelectMany(x => x.Errors)
                                              .Select(x => x.ErrorMessage));

        }
        else if (model == null)
        {
            response.Content.Message = "Invalid Request Model";
            response.Content.IsSuccess = false;
            response.Status.Code = "Error-4000";
            response.Status.IsSuccess = false;
            response.Status.StatusType = StatusType.Error;
            response.Status.StatusMessage = string.Join(" ", ModelState.Values
                                              .SelectMany(x => x.Errors)
                                              .Select(x => x.ErrorMessage));
        }
        else
        {
            response.Content.Message = "Valid Request Model";
            response.Content.IsSuccess = true;
            response.Status.Code = "MSG-4000";
            response.Status.IsSuccess = true;
            response.Status.StatusType = StatusType.Success;
            response.Status.StatusMessage = "Valid Request Model";
        }
        return response;
    }
}
