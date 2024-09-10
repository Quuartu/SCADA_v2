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
using FTOptix.SerialPort;
using FTOptix.System;
using FTOptix.EventLogger;
#endregion

public class RuntimeNetLogicReaToClienteStorico : BaseNetLogic
{
    /// <summary>
    /// Percorso del nodo connettore ODBC.
    /// </summary>
    private const string DATASTORE_DATABASE = "DataStores/ODBC_PRG_OPTIX";
    /// <summary>
    /// Nome della tabella dalla quale leggere.
    /// </summary>
    private const string TABLE_NAME = "REA_TO_CLIENTE";
    /// <summary>
    /// Variabile di tipo Store che rappresenta un riferimento a database.
    /// </summary>
    private Store _store;
    /// <summary>
    /// Variabile di tipo Table che rappresenta un riferimento ad una tabella.
    /// </summary>
    private Table _table;

    public override void Start()
    {
        _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
        _table = _store.Tables.Get<Table>(TABLE_NAME);

        Project.Current.GetVariable(VariablePaths.PathQueryStorico).Value = $"SELECT * FROM {TABLE_NAME}";
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    /// <summary>
    /// Inserisce un record nello storico
    /// </summary>
    public void sp_Insert(ReaToCliente prod)
    {
        try
        {
            string[] columns = { "Production_Command", "Product_ID", "dt_start", "dt_end", "Quantity_Requested", "Quantity_Produced", "Total_Reject", "Extra_Production" };

            var values = new object[1, 8];
            values[0, 0] = prod.Production_Command;
            values[0, 1] = prod.Product_ID;
            values[0, 2] = prod.dt_start;
            values[0, 3] = prod.dt_end;
            values[0, 4] = prod.Quantity_Requested;
            values[0, 5] = prod.Quantity_Produced;
            values[0, 6] = prod.Total_Reject;
            values[0, 7] = prod.Extra_Production;

            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _table = _store.Tables.Get<Table>(TABLE_NAME);
            _table.Insert(columns, values);

        }
        catch (Exception ex)
        {
            Log.Warning($"ERROR: sp_Insert {ex.Message}");
        }
    }

    /// <summary>
    /// Filtra la query dello Storico (ReaToCliente)
    /// </summary>
    [ExportMethod]
    public void sp_FilterQuery(string filter)
    {
        string start_date;
        string end_date;
        string query;

        query = $"SELECT * FROM {TABLE_NAME} WHERE";

        if (Project.Current.GetVariable(VariablePaths.PathStoricobdateFilter).Value)
        {
            //recupero data inizio e fine.
            start_date = FormatDate(Project.Current.GetVariable(VariablePaths.PathStoricoDateFrom).Value);
            end_date = FormatDate(Project.Current.GetVariable(VariablePaths.PathStoricoDateTo).Value);

            query = $"{query} dt_end BETWEEN '{start_date} 00:00:00' AND '{end_date} 23:59:59' AND";
        }
        else 
        {
            start_date = "";
            end_date = "";
        }

        query = $"{query} (Production_Command LIKE '%{filter}%' OR Product_ID LIKE '%{filter}%')";

        _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
        _store.Query(query, out _, out _);
        Project.Current.GetVariable(VariablePaths.PathQueryStorico).Value = query;
    }

    /// <summary>
    /// trasforma il parametro in una data
    /// </summary>
    private string FormatDate(DateTime? date)
    {
        if (date.HasValue)
            return date.Value.ToString("yyyy-MM-dd");
        else
            return "null";
    }
}

public class ReaToCliente
{
    public int Id { get; set; }
    public string Production_Command { get; set; }
    public string Product_ID { get; set; }
    public DateTime? dt_start { get; set; }
    public DateTime? dt_end { get; set; }
    public int Quantity_Requested { get; set; }
    public int Quantity_Produced { get; set; }
    public int Total_Reject { get; set; }
    public int Extra_Production { get; set; }
}
