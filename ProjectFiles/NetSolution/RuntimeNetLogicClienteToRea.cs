#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.S7TiaProfinet;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Alarm;
using FTOptix.Core;
using System.Reflection.Emit;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography.X509Certificates;
using FTOptix.SerialPort;
using FTOptix.System;
using System.Threading.Tasks;
using FTOptix.EventLogger;
#endregion

public class RuntimeNetLogicClienteToRea : BaseNetLogic
{
    /// <summary>
    /// Percorso del nodo connettore ODBC.
    /// </summary>
    private const string DATASTORE_DATABASE = "DataStores/ODBC_PRG_OPTIX";
    /// <summary>
    /// Nome della tabella dalla quale leggere.
    /// </summary>
    private const string TABLE_NAME = "CLIENTE_TO_REA";
    /// <summary>
    /// Variabile di tipo Store che rappresenta un riferimento a database.
    /// </summary>
    private Store _store;
    /// <summary>
    /// Variabile di tipo Table che rappresenta un riferimento ad una tabella.
    /// </summary>
    private Table _table;

    private RuntimeNetLogicReaToClienteStorico _storicoprod;
     
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        try
        {
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _table = _store.Tables.Get<Table>(TABLE_NAME);

            Project.Current.GetVariable(VariablePaths.PathQueryProduzione).Value = $"SELECT * FROM {TABLE_NAME}";
            //Project.Current.GetVariable("Model/Produzione/QueryProduzione").Value = $"SELECT t.*, CASE t.Status when 0 THEN 'DA ELABORARE' WHEN 25 then 'IN AVVIO' WHEN 100 then 'IN LAVORO' when 180 then 'ERRORE KO PLC' when 190 then 'ANNULLAMENTO DA USER' end FROM {TABLE_NAME} t";
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
    /// Pulsante Elimina: prende in ingresso l'id prodotto da eliminare
    /// </summary>
    [ExportMethod]
    public void pr_Elimina(int id)
    {
        _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
        // Perform the query
        _store.Query($"DELETE FROM {TABLE_NAME} WHERE Id={id}", out _, out _);
        Project.Current.GetVariable(VariablePaths.PathQueryProduzione).Value = $"SELECT * FROM {TABLE_NAME}";
        //Project.Current.GetVariable("Model/Produzione/QueryProduzione").Value = $"SELECT t.*, CASE t.Status when 0 THEN 'DA ELABORARE' WHEN 25 then 'IN AVVIO' WHEN 100 then 'IN LAVORO' when 180 then 'ERRORE KO PLC' when 190 then 'ANNULLAMENTO DA USER' end FROM {TABLE_NAME} t";

        //ripulisci, solo pulsanti Nuovo e Storico Produzione sono abilitati
        pr_Undo();

    }

    /// <summary>
    /// Inserisci nuova produzione da pannello (prende in input Production_Command e Product_ID)
    /// </summary>
    [ExportMethod]
    public void pr_Inserisci(NodeId prodottoObjNodeId)
    {
        pr_InserisciAsincrono(prodottoObjNodeId);
    }

    private async Task pr_InserisciAsincrono(NodeId prodottoObjNodeId)
    {
        var prodottoObj = InformationModel.GetObject(prodottoObjNodeId);

        string[] columns = { "Production_Command", "Product_ID" };

        string odp = prodottoObj.GetVariable("Odp").Value.Value.ToString();
        string nomeArticolo = prodottoObj.GetVariable("NomeArticolo").Value.Value.ToString();

        if (odp == "" || nomeArticolo == "")
        {
            PopUpNetLogic popup = new PopUpNetLogic();
            popup.OpenPopUp("INSERISCI VALORI VALIDI", 0);

            await PopUpNetLogic.WaitForConditionAsync();

            //reset popup
            Project.Current.GetVariable(VariablePaths.PathPopupOK).Value = false;

            return;
        }

        var values = new object[1, 2];
        values[0, 0] = odp;
        values[0, 1] = nomeArticolo;

        _table = _store.Tables.Get<Table>(TABLE_NAME);
        _table.Insert(columns, values);
        Project.Current.GetVariable(VariablePaths.PathQueryProduzione).Value = $"SELECT * FROM {TABLE_NAME}";
        //Project.Current.GetVariable("Model/Produzione/QueryProduzione").Value = $"SELECT t.*, CASE t.Status when 0 THEN 'DA ELABORARE' WHEN 25 then 'IN AVVIO' WHEN 100 then 'IN LAVORO' when 180 then 'ERRORE KO PLC' when 190 then 'ANNULLAMENTO DA USER' end FROM {TABLE_NAME} t";

        pr_Undo();
    }

    /// <summary>
    /// Annulla inserimento
    /// </summary>
    [ExportMethod]
    public void pr_Annulla()
    {
        pr_Undo();
    }

    /// <summary>
    /// Avvia produzione
    /// </summary>
    [ExportMethod]
    public void pr_Avvia(int id)
    {
        Project.Current.GetVariable(VariablePaths.PathOdlStart).Value = id;
        Project.Current.GetVariable(VariablePaths.Pathap_start).Value = true;
    }

    /// <summary>
    /// Resetta le variabili della pagina
    /// </summary>
    private void pr_Undo()
    {
        //reset dei pulsanti della pagina di produzione
        Project.Current.GetVariable(VariablePaths.PathProduzioneAvviaEnabled).Value = false;
        Project.Current.GetVariable(VariablePaths.PathProduzioneEliminaEnabled).Value = false;
        Project.Current.GetVariable(VariablePaths.PathProduzioneTerminaEnabled).Value = false;

        //reset delle text box
        Project.Current.GetVariable(VariablePaths.PathProduzioneNuovaProduzioneOdp).Value = "";
        Project.Current.GetVariable(VariablePaths.PathProduzioneNuovaProduzioneNomeArticolo).Value = "";
    }

    /// <summary>
    /// Controlla l'abilitazione dei pulsanti a seconda dello stato
    /// </summary>
    [ExportMethod]
    public void pr_ManageProductionButtons(int status)
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
                TerminaEnabled.Value= false;
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
    /// Esegue la query filtrata nella tabella Produzione (ClienteToRea)
    /// </summary>
    [ExportMethod]
    public void pr_FilterQuery(string filter)
    {
        if (Project.Current.GetVariable(VariablePaths.PathProduzioneFilterActive).Value)
        {
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _store.Query($"SELECT * FROM {TABLE_NAME} WHERE Production_Command LIKE '%{filter}%'",out _, out _);
            Project.Current.GetVariable(VariablePaths.PathQueryProduzione).Value = $"SELECT * FROM {TABLE_NAME} WHERE Production_Command LIKE '%{filter}%'";
        }
        else
        {
            Project.Current.GetVariable(VariablePaths.PathProduzioneTextFilter).Value = "";
            Project.Current.GetVariable(VariablePaths.PathQueryProduzione).Value = $"SELECT * FROM {TABLE_NAME}";
            //Project.Current.GetVariable("Model/Produzione/QueryProduzione").Value = $"SELECT t.*, CASE t.Status when 0 THEN 'DA ELABORARE' WHEN 25 then 'IN AVVIO' WHEN 100 then 'IN LAVORO' when 180 then 'ERRORE KO PLC' when 190 then 'ANNULLAMENTO DA USER' end FROM {TABLE_NAME} t";
        }
    }

    /// <summary>
    /// Sincronizza tutti gli stati dei record di produzione all'avvio della macchina a stati
    /// </summary>
    public void pr_StatusSyncro(int id)
    {
        try
        {
            object[,] result;
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _store.Query($"SELECT * FROM {TABLE_NAME} WHERE Status = 100 or Status=25 or ID={id}", out _, out result);

            for (int i = 0; i < result.GetLength(0); i++)
            {
                if (result[i, 0].Equals(id))
                {
                    //esegui query
                    _store.Query($"UPDATE {TABLE_NAME} SET Status = 100 WHERE ID = {id}", out _, out _);
                }
                else
                {
                    short status_running = 100;
                    if (result[i, (result.GetLength(1)-1)].Equals(status_running))
                    {
                        //esegui query
                        _store.Query($"UPDATE {TABLE_NAME} SET Status = 0 WHERE ID = {result[i,0]}", out _, out _);
                    }

                    status_running = 25;
                    if (result[i, (result.GetLength(1) - 1)].Equals(status_running))
                    {
                        //esegui query
                        _store.Query($"UPDATE {TABLE_NAME} SET Status = 0 WHERE ID = {result[i, 0]}", out _, out _);
                    }
                } 
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Errore pr_StatusSyncro: {ex.Message}");
        }   
    }

    /// <summary>
    /// Aggiorna lo stato della produzione al valore desiderato
    /// </summary>
    public void pr_UpdateStatus(int id, int status)
    {
        try
        {
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _store.Query($"UPDATE {TABLE_NAME} SET Status ={status} WHERE ID={id}", out _, out _);
        }
        catch (Exception ex)
        {
            Log.Warning($"Errore pr_UpdateStatus: {ex.Message}");
        }
    }

    /// <summary>
    /// Ritorna i dati relativi al record con ID=id
    /// </summary>
    public ClienteToRea pr_GetById(int id)
    {
        ClienteToRea prod = new ClienteToRea();

        try
        {
            object[,] result;
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _store.Query($"SELECT * FROM {TABLE_NAME} WHERE ID={id}", out _, out result);

            if (result.GetLength(0) == 0 || result.GetLength(1) == 0)
            {
                Log.Warning("pr_GetById no record found");
                return prod;
            }

            prod.Id = id;
            prod.Production_Command = (string)result[0, 1];
            prod.Product_ID = (string)result[0, 2];
            prod.Date_Insert = ((DateTime?)result[0, 3]);
            prod.Date_Elab = ((DateTime?)result[0, 4]);
            prod.Date_Update = ((DateTime?)result[0, 5]);
            prod.Date_End = ((DateTime?)result[0, 6]);
            prod.Quantity_Requested = (int)result[0, 7];
            prod.Quantity_Produced = (int)result[0, 8];
            prod.Extra_Production = (int)result[0, 9];
            prod.Total_Reject = (int)result[0, 10];
            prod.Status = (Int16)result[0, 11];

            return prod;
        }
        catch (Exception ex)
        {
            Log.Warning($"Error pr_GetById: {ex}");
            return prod;
        }
    }

    /// <summary>
    /// Recupera i dati delle produzioni terminate e li sposta nello storico
    /// </summary>
    public void pr_TerminateAllRunning()
    {
        object[,] result;
        _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
        _store.Query($"SELECT * FROM {TABLE_NAME} WHERE Status=160",out _,out result);

        if (result.GetLength(0) == 0 || result.GetLength(1) == 0)
        {
            Log.Warning("pr_TerminateAllRunning no record found");
            return;
        }

        ReaToCliente prod = new ReaToCliente();
        prod.Production_Command = (string)result[0, 1];
        prod.Product_ID = (string)result[0, 2];
        prod.dt_start = (DateTime?)result[0, 3];
        prod.dt_end = DateTime.Now;
        prod.Quantity_Requested = (int)result[0, 7];
        prod.Quantity_Produced = (int)result[0, 8];
        prod.Total_Reject = (int)result[0, 9];
        prod.Extra_Production = (int)result[0, 10];

        _storicoprod = new RuntimeNetLogicReaToClienteStorico();
        _storicoprod.sp_Insert(prod);

    }
}


public class ClienteToRea
{ 
    public int Id { get; set; }
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
