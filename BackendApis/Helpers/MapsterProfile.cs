using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.Models;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using Mapster;

namespace BackendApis.Helpers;

public sealed class MapsterProfile : TypeAdapterConfig
{
    public MapsterProfile()
    {
        RegisterEmployeeMappings();
        RegisterUserMappings();
    }

    private void RegisterEmployeeMappings()
    {
        TypeAdapterConfig<Employee, GetEmployeeDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.EmployeeName, src => src.Name)
            .Map(dest => dest.Age, src => src.Age)
            .Map(dest => dest.Salary, src => src.Salary)
            .Map(dest => dest.Cv, src => src.CvUrl ?? string.Empty) // ensure empty value if null
            .Map(dest => dest.Image, src => src.ImageUrl ?? string.Empty)
            .Map(dest => dest.UserName, src => src.ApplicationUser.UserName)
            .IgnoreNullValues(true);

        TypeAdapterConfig<CreateEmployeeViewModel, Employee>.NewConfig()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Age, src => src.Age)
            .Map(dest => dest.Salary, src => src.Salary)
            .Map(dest => dest.ApplicationUserId, src => src.ApplicationUserId)
            .Ignore(dest => dest.CvUrl)
            .Ignore(dest => dest.ImageUrl)
            .Ignore(dest => dest.ApplicationUser)
            .IgnoreNullValues(true);
    }

    private void RegisterUserMappings()
    {
        // Example: RegisterViewModel -> RegisterDto (extend as needed)
        // TypeAdapterConfig<RegisterViewModel, RegisterDto>.NewConfig()
        //    .Map(dest => dest.ICNum, src => src.IcNumber);
    }
}