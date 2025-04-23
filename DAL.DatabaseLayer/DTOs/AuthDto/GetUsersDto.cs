namespace DAL.DatabaseLayer.DTOs.AuthDto;

public readonly record struct GetUsersDto(
    string Id,
    string UserName
);
