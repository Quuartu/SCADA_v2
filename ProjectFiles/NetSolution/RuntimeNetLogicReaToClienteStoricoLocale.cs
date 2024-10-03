#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Recipe;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.SQLiteStore;
using FTOptix.S7TiaProfinet;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
#endregion

public class RuntimeNetLogicReaToClienteStoricoLocale : BaseNetLogic
{
    /// <summary>
    /// Percorso del nodo connettore ODBC.
    /// </summary>
    private const string DATASTORE_DATABASE = "DataStores/EmbeddedDatabase1";
    /// <summary>
    /// Nome della tabella dalla quale leggere.
    /// </summary>
    private const string TABLE_NAME = "RecipeSchema3";
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
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    /// <summary>
    /// Inserisce un record nello storico
    /// </summary>
    public void sp_InsertLocale(ReaToClienteLocale prod)
    {
        try
        {
            string[] columns = { "/Production_Command", "/Product_ID", "/dt_start", "/dt_end", "/Quantity_Requested", "/Quantity_Produced", "/Total_Reject", "/Extra_Production" };

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
            Log.Warning($"ERROR: sp_InsertLocale {ex.Message}");
        }
    }

}

public class ReaToClienteLocale
{
    public int Id { get; set; }
    public string Production_Command { get; set; }
    public string Product_ID { get; set; }
    public DateTime? dt_start { get; set; }
    public DateTime? dt_end { get; set; }
    public long Quantity_Requested { get; set; }
    public long Quantity_Produced { get; set; }
    public long Total_Reject { get; set; }
    public long Extra_Production { get; set; }
}