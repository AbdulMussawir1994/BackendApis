using Cryptography.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DAL.DatabaseLayer.DataContext;

public class AdoContext
{
    private SqlDataAdapter DataAdapter { get; set; }
    private readonly SqlConnection connection;
    private readonly IConfiguration _configuration;
    public AdoContext(IConfiguration configuration, bool isLog = false)
    {
        connection = isLog ? new SqlConnection(new AesGcmEncryption(configuration).Decrypt(configuration.GetConnectionString("LogsConnection"))) : new SqlConnection(new RsaEncryption(configuration).Decrypt(configuration.GetConnectionString("DefaultConnection")));
        DataAdapter = new SqlDataAdapter();
        _configuration = configuration;
    }

    private async Task<SqlConnection> connectionState()
    {
        if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
        {
            await connection.OpenAsync();
        }
        return connection;
    }
    public async Task<bool> ExecuteInsertQuery(string commandText, SqlParameter[] parameters)
    {
        var isExecutable = true;
        var command = new SqlCommand();
        try
        {
            command.CommandText = commandText;
            using (SqlConnection con = await connectionState())
            {
                command.Connection = con;
                command.Parameters.AddRange(parameters);
                DataAdapter.InsertCommand = command;
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            List<string> paramterValues = new List<string>();
            for (int i = 0; i < parameters.Length; i++)
            {
                SqlParameter item = parameters[i];
                if (item.Value != null)
                    paramterValues.Add(item.Value.ToString());
                else
                    paramterValues.Add("");

            }

            isExecutable = false;
        }
        return isExecutable;
    }
    public async Task<bool> ExecuteUpdateQuery(string commandText, SqlParameter[] parameters)
    {
        var isExecutable = true;
        var command = new SqlCommand();
        try
        {
            command.CommandText = commandText;
            using (SqlConnection con = await connectionState())
            {
                command.Connection = con;
                command.Parameters.AddRange(parameters);
                DataAdapter.UpdateCommand = command;
                isExecutable = await command.ExecuteNonQueryAsync() > 0;
            }
        }
        catch (Exception ex)
        {
            List<string> paramterValues = new List<string>();
            for (int i = 0; i < parameters.Length; i++)
            {
                SqlParameter item = parameters[i];
                if (item.Value != null)
                    paramterValues.Add(item.Value.ToString());
                else
                    paramterValues.Add("");

            }

            isExecutable = false;
        }
        return isExecutable;
    }
    public async Task<object> ExecuteScalar(string commandText, SqlParameter[] parameters)
    {
        var command = new SqlCommand();
        try
        {
            command.CommandText = commandText;
            using (SqlConnection con = await connectionState())
            {
                command.Connection = con;
                command.Parameters.AddRange(parameters);
                DataAdapter.UpdateCommand = command;
                return await command.ExecuteScalarAsync();
            }
        }
        catch (Exception ex)
        {
            List<string> paramterValues = new List<string>();
            for (int i = 0; i < parameters.Length; i++)
            {
                SqlParameter item = parameters[i];
                if (item.Value != null)
                    paramterValues.Add(item.Value.ToString());
                else
                    paramterValues.Add("");
            }

        }
        return null;
    }

}
