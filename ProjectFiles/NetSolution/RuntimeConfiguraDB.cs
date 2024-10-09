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
using FTOptix.Core;
using System.IO;
using System.Collections.Generic;
using System.Data.SqlClient;
using FTOptix.SQLiteStore;
using FTOptix.Recipe;
using FTOptix.Report;
#endregion

public class RuntimeConfiguraDB : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        string sourcePath = string.Empty;
        string destinationPath = string.Empty;
        string RuntimePath = Path.Combine(Path.GetFullPath(Project.Current.ProjectDirectory));
        sourcePath = Path.Combine(RuntimePath, "sni.dll");
        destinationPath = Path.Combine(RuntimePath, "NetSolution", "bin", "sni.dll");
        System.IO.File.Copy(sourcePath, destinationPath, true);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void Configura_file(NodeId db_configurazioni)
    {
        var db_configurazioniObj = InformationModel.GetObject(db_configurazioni);

        List<string> tables = new List<string>();
        List<string> columns = new List<string>();

        tables = GetTablesList(db_configurazioniObj);

        foreach (var table in tables)
        {
            columns = GetColumnsList(db_configurazioniObj, table);

            CreateGrid(table, columns);
        }

    }

    /// <summary>
    /// Recupera tutte le tabelle associate al db
    /// </summary>
    private List<string> GetTablesList(IUAObject db_configurazioniObj)
    {
        List<string> tableList = new List<string>();

        try
        {
            var connectionString = GetSQLConnectionString(db_configurazioniObj);

            using (SqlConnection connection = new SqlConnection(connectionString))
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
            Log.Warning("Errore: GetTablesList ->" + ex.Message);
        }

        return tableList;
    }

    /// <summary>
    /// Aggiunge le tabelle con le relative colonne 
    /// </summary>
    private List<string> GetColumnsList(IUAObject db_configurazioniObj, string tableName)
    {
        List<string> columnList = new List<string>();

        try
        {
            var connectionString = GetSQLConnectionString(db_configurazioniObj);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
                using (SqlCommand queryCommand = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = queryCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columnList.Add(reader.GetString(0));
                        }
                    }
                }

            }
        }
        catch (Exception ex)
        {
            Log.Warning("Errore: GetColumnsList ->" + ex.Message);
        }

        return columnList;
    }
    /// <summary>
    /// crea la tabella
    /// </summary>
    private void CreateGrid(string table, List<string> columns)
    {
        var myGrid = InformationModel.Make<DataGrid>("MyGrid");
        myGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        myGrid.VerticalAlignment = VerticalAlignment.Stretch;
        myGrid.TopMargin = 30;
        myGrid.LeftMargin = 30;
        myGrid.RightMargin = 30;
        myGrid.BottomMargin = 30;

        var dataBase = Project.Current.Get<Store>("DataStores/ODBC_PRG_OPTIX");

        myGrid.Model = dataBase.NodeId;
        myGrid.Query = "SELECT * FROM \"" + table + "\"";

        myGrid.AutoRefreshTime = 1000;

        for (int i = 0; i < columns.Count; i++)
        {
            // aggiungere colonne
            var myColonna = InformationModel.Make<DataGridColumn>(columns[i]);
            myColonna.Title = columns[i];
            myColonna.OrderBy = "{Item}/" + columns[i];



            var myDataItemTemplate = InformationModel.Make<DataGridLabelItemTemplate>("DataItemTemplate");
            IUAVariable link = InformationModel.MakeVariable<DynamicLink>("TextLink", FTOptix.Core.DataTypes.NodePath);
            link.Value = "{Item}/" + columns[i];
            myDataItemTemplate.GetVariable("Text").Refs.AddReference(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink, link);

            myColonna.DataItemTemplate = myDataItemTemplate;
            myGrid.Columns.Add(myColonna);

            Owner.Add(myGrid);
        }
    }
    //stringa di connessione
    private string GetSQLConnectionString(IUAObject config)
    {
        if (config.GetVariable("UsaUtenzaSQL").Value)
        {
            return $"Server={config.GetVariable("Server").Value.Value};Database={config.GetVariable("NomeDB").Value.Value};User Id={config.GetVariable("UtenteSQL").Value.Value};Password={config.GetVariable("PasswordSQL").Value.Value};";
        }
        else
        {
            return $"Server={config.GetVariable("Server").Value.Value};Database={config.GetVariable("NomeDB").Value.Value};Integrated Security=SSPI;";
        }
    }
}
