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

public class RuntimeNetLogicClienteToReaLocale : BaseNetLogic
{
    /// <summary>
    /// Percorso del DB Locale.
    /// </summary>
    private const string DATASTORE_DATABASE = "DataStores/EmbeddedDatabase1";
    /// <summary>
    /// Nome della tabella dalla quale leggere.
    /// </summary>
    private const string TABLE_NAME = "RecipeSchema2";
    /// <summary>
    /// Variabile di tipo Store che rappresenta un riferimento a database.
    /// </summary>
    private Store _store;
    /// <summary>
    /// Variabile di tipo Table che rappresenta un riferimento ad una tabella.
    /// </summary>
    private Table _table;

    //private RuntimeNetLogicReaToClienteStorico _storicoprod;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    /// <summary>
    /// Avvia produzione
    /// </summary>
    [ExportMethod]
    public void pr_AvviaLocale(long id)
    {
        Project.Current.GetVariable(VariablePaths.PathOdlStart).Value = id;
        Project.Current.GetVariable(VariablePaths.Pathap_start).Value = true;
    }

    /// <summary>
    /// Controlla l'abilitazione dei pulsanti a seconda dello stato
    /// </summary>
    [ExportMethod]
    public void pr_ManageProductionButtonsLocale(int status)
    {
        var AvviaEnabled = Project.Current.GetVariable(VariablePaths.PathProduzioneAvviaEnabled);
        var EliminaEnabled = Project.Current.GetVariable(VariablePaths.PathProduzioneEliminaEnabled);
        var TerminaEnabled = Project.Current.GetVariable(VariablePaths.PathProduzioneTerminaEnabled);

        switch (status)
        {
            //IDLE - DA ELABORARE
            case 0:
                AvviaEnabled.Value = true;
                EliminaEnabled.Value = true;
                TerminaEnabled.Value = false;
                break;
            //DA ELABORARE
            case 20:
                AvviaEnabled.Value = true;
                EliminaEnabled.Value = true;
                TerminaEnabled.Value = false;
                break;
            //IN AVVIO
            case 25:
                AvviaEnabled.Value = false;
                EliminaEnabled.Value = false;
                TerminaEnabled.Value = false;
                break;
            //IN LAVORO
            case 100:
                AvviaEnabled.Value = false;
                EliminaEnabled.Value = false;
                TerminaEnabled.Value = true;
                break;
            //TERMINATA
            case 160:
                AvviaEnabled.Value = false;
                EliminaEnabled.Value = true;
                TerminaEnabled.Value = false;
                break;
            //KO DA PLC
            case 180:
                AvviaEnabled.Value = true;
                EliminaEnabled.Value = true;
                TerminaEnabled.Value = false;
                break;
            //ESCAPE DA OPERATORE
            case 190:
                AvviaEnabled.Value = true;
                EliminaEnabled.Value = true;
                TerminaEnabled.Value = false;
                break;
            //STATO NON RICONOSCIUTO
            default:
                AvviaEnabled.Value = false;
                EliminaEnabled.Value = false;
                TerminaEnabled.Value = false;
                break;
        }

        //indipendentemente da tutto, se c'è produzione in corso, pulsante Avvia deve essere disabilitato 
        if (Project.Current.GetVariable(VariablePaths.PathProduzioneInCorso).Value)
        {
            AvviaEnabled.Value = false;
        }
    }

    /// <summary>
    /// Sincronizza tutti gli stati dei record di produzione all'avvio della macchina a stati
    /// </summary>
    public void pr_StatusSyncro_Locale(long id)
    {
        try
        {
            object[,] result;
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE); 
            _store.Query($"SELECT * FROM {TABLE_NAME} WHERE \"/Status\"=100 or \"/Status\"=25 or \"/ID\"={id}", out _, out result);

            for (int i = 0; i < result.GetLength(0); i++)
            {
                if (result[i, 0].Equals(id))
                {
                    //esegui query
                    _store.Query($"UPDATE {TABLE_NAME} SET \"/Status\"=100 WHERE \"/ID\"={id}", out _, out _);
                }
                else
                {
                    short status_running = 100;
                    if (result[i, (result.GetLength(1) - 1)].Equals(status_running))
                    {
                        //esegui query
                        _store.Query($"UPDATE {TABLE_NAME} SET \"/Status\"= 0 WHERE \"/ID\"={result[i, 0]}", out _, out _);
                    }

                    status_running = 25;
                    if (result[i, (result.GetLength(1) - 1)].Equals(status_running))
                    {
                        //esegui query
                        _store.Query($"UPDATE {TABLE_NAME} SET \"/Status\"= 0 WHERE \"/ID\"= {result[i, 0]}", out _, out _);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Errore pr_StatusSyncro_Locale: {ex.Message}");
        }
    }

    /// <summary>
    /// Aggiorna lo stato della produzione al valore desiderato
    /// </summary>
    public void pr_UpdateStatusLocale(long id, int status)
    {
        try
        {
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _store.Query($"UPDATE {TABLE_NAME} SET \"/Status\" ={status} WHERE \"/ID\"={id}", out _, out _);
        }
        catch (Exception ex)
        {
            Log.Warning($"Errore pr_UpdateStatusLocale: {ex.Message}");
        }
    }

    /// <summary>
    /// Ritorna i dati relativi al record con /ID=id
    /// </summary>
    public ClienteToReaLocale pr_GetByIdLocale(long id)
    {
        ClienteToReaLocale prod = new ClienteToReaLocale();

        try
        {
            object[,] result;
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _store.Query($"SELECT * FROM {TABLE_NAME} WHERE \"/ID\"={id}", out _, out result);

            if (result.GetLength(0) == 0 || result.GetLength(1) == 0)
            {
                Log.Warning("pr_GetByIdLocale no record found");
                return prod;
            }

            prod.Id = id;
            prod.Production_Command = (string)result[0, 0];
            prod.Product_ID = (string)result[0, 3];
            prod.Date_Insert = ((DateTime?)result[0, 4]);
            prod.Date_Elab = ((DateTime?)result[0, 5]);
            prod.Date_Update = ((DateTime?)result[0, 6]);
            prod.Date_End = ((DateTime?)result[0, 7]);
            prod.Quantity_Requested = (int)result[0, 8];
            prod.Quantity_Produced = (int)result[0, 9];
            prod.Extra_Production = (int)result[0, 10];
            prod.Total_Reject = (int)result[0, 11];
            prod.Status = (Int16)result[0, 12];

            return prod;
        }
        catch (Exception ex)
        {
            Log.Warning($"Error pr_GetByIdLocale: {ex}");
            return prod;
        }
    }

}

public class ClienteToReaLocale
{
    public long Id { get; set; }
    public string Production_Command { get; set; }
    public string Product_ID { get; set; }
    public DateTime? Date_Insert { get; set; }
    public DateTime? Date_Elab { get; set; }
    public DateTime? Date_Update { get; set; }
    public DateTime? Date_End { get; set; }
    public int Quantity_Requested { get; set; }
    public int Quantity_Produced { get; set; }
    public int Extra_Production { get; set; }
    public int Total_Reject { get; set; }
    public int Status { get; set; }
}