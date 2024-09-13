#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.S7TiaProfinet;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
using FTOptix.Core;
using System.Data.SqlClient;
using System.Data;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using FTOptix.SQLiteStore;
using FTOptix.Recipe;
#endregion

public class DesignTimeConfiguraDB : BaseNetLogic
{
    const string DB_SERVER = ".\\SQLEXPRESS";
    const string DB_NAME = "PRG_OPTIX";
    const string DB_USER = "sa";
    const string DB_PASSWORD = "reaSQL";

    private string _connectionString = $"Server={DB_SERVER};Database={DB_NAME};User Id={DB_USER};Password={DB_PASSWORD};";


    [ExportMethod]
    public void ConfigureDB()
    {

        string sourcePath = string.Empty;
        string destinationPath = string.Empty;
        string RuntimePath = Path.Combine(Path.GetFullPath(Project.Current.ProjectDirectory));
        sourcePath = Path.Combine(RuntimePath, "sni.dll");
        destinationPath = Path.Combine(RuntimePath, "NetSolution", "bin", "sni.dll");
        System.IO.File.Copy(sourcePath, destinationPath, true);



        //prendo il datastore (rimarrà sempre fisso)
        ODBCStore db = Project.Current.Get<ODBCStore>("DataStores/ODBC_PRG_OPTIX");

        //compilo le info necessarie per l'accesso al db
        db.Server = DB_SERVER;
        db.Database = DB_NAME;
        db.Username = DB_USER;
        db.Password = DB_PASSWORD;

        //cancello le tabelle presenti
        //db.Tables.Clear();
        
        //aggiungo le tabelle necessarie con le relative colonne
        List<string> tables = new List<string>();
        tables = ListTables();

        if (tables.Count>0 )
        {
            foreach (string tbl in tables)
            {
                AddNewTable(db, tbl);
            }
        }

    }

    /// <summary>
    /// Recupera tutte le tabelle associate al db
    /// </summary>
    private List<string> ListTables()
    {
        List<string> tableList = new List<string>();

        try
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

                using (SqlCommand queryCommand = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = queryCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tableList.Add(reader["TABLE_NAME"].ToString());
                        }
                    }
                }

            }
        }
        catch (Exception ex)
        {
            Log.Warning("Errore: ListTables ->" + ex.Message);

        }

        return tableList;
    }

    /// <summary>
    /// Aggiunge le tabelle con le relative colonne 
    /// </summary>
    private void AddNewTable(ODBCStore db, string tableName)
    {
        var newTable = InformationModel.Make<Table>(tableName);
        db.Tables.Add(newTable);

        try
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = {tableName}";
                using (SqlCommand queryCommand = new SqlCommand(query,connection))
                {
                    using (SqlDataReader reader = queryCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            UAManagedCore.NodeId type = (NodeId)InformationModel.MakeObjectType(reader.GetString(1));

                            db.Tables[tableName].AddColumn(reader.GetString(0),type);
                            //db.AddColumn(table, reader.GetString(0), MapSqlType(reader.GetString(1)));
                            Log.Info(reader.GetString(0) + "   " + reader.GetString(1));   
                        }
                    }
                }

            }
        }
        catch (Exception ex)
        {
            Log.Warning("Errore: AddNewTable ->" + ex.Message);
        }
    }


    static Type MapSqlType(string sqlType)
    {
        switch (sqlType.ToLower())
        {
            case "int":
                return typeof(int);
            case "varchar":
            case "nvarchar":
            case "char":
            case "nchar":
                return typeof(string);
            // Aggiungi altri casi secondo necessità
            default:
                return typeof(object); // Tipo generico per altri tipi non gestiti
        }
    }
}
