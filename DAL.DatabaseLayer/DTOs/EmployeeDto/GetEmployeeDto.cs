﻿namespace DAL.DatabaseLayer.DTOs.EmployeeDto;

public readonly record struct GetEmployeeDto(
    string Id,
    string UserName,
    string EmployeeName,
    int Age,
    decimal Salary,
    string Cv,
    string Image,
    string ApplicationUserId
);