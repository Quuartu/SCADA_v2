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

public class RuntimeNetLogicAnagraficaLocale : BaseNetLogic
{
    /// <summary>
    /// Percorso del nodo DB Locale.
    /// </summary>
    private const string DATASTORE_DATABASE = "DataStores/EmbeddedDatabase1";
    /// <summary>
    /// Nome della tabella dalla quale leggere.
    /// </summary>
    private const string TABLE_NAME = "RecipeSchema1";
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
        try
        {
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _table = _store.Tables.Get<Table>(TABLE_NAME);
        }
        catch (Exception ex)
        {
            Log.Error(ex.StackTrace);
        }
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    /// <summary>
    /// Recupera i dati dell'articolo Name
    /// </summary>
    public AnagraficaLocale aa_GetByProductIDLocale(string nomeArticolo)
    {
        AnagraficaLocale art = new AnagraficaLocale();

        try
        {
            object[,] result;
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _store.Query($"SELECT * FROM {TABLE_NAME} WHERE Name='{nomeArticolo}'", out _, out result);

            if (result.GetLength(0) == 0 || result.GetLength(1) == 0)
            {
                Log.Warning("pr_GetByIdLocale no record found");
                return art;
            }

            art.Id = (long)result[0, 1];
            art.Product_ID = (string)result[0, 0];
            art.Descr = (string)result[0, 2];
            art.Robot_Program = (int)result[0, 3];
            //art.Active = (bool)result[0, 4];

            return art;
        }
        catch (Exception ex)
        {
            Log.Warning($"Error pr_GetByIdLocale: {ex}");
            return art;
        }
    }
}

public class AnagraficaLocale
{
    public long Id { get; set; }
    public string Product_ID { get; set; }
    public string Descr { get; set; }
    public int Robot_Program { get; set; }
    public bool Active { get; set; }
}
