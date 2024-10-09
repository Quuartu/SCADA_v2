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
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http.Headers;
using FTOptix.SerialPort;
using FTOptix.System;
using System.Threading.Tasks;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Recipe;
using FTOptix.Report;

#endregion

public class RuntimeNetLogicAnagrafica : BaseNetLogic
{
    /// <summary>
    /// Percorso del nodo connettore ODBC.
    /// </summary>
    private const string DATASTORE_DATABASE = "DataStores/ODBC_PRG_OPTIX";
    /// <summary>
    /// Nome della tabella dalla quale leggere.
    /// </summary>
    private const string TABLE_NAME = "Product";
    /// <summary>
    /// Variabile di tipo Store che rappresenta un riferimento a database.
    /// </summary>
    private Store _store;
    /// <summary>
    /// Variabile di tipo Table che rappresenta un riferimento ad una tabella.
    /// </summary>
    private Table _table;


    /// <summary>
    /// Insert code to be executed when the user-defined logic is started.
    /// </summary>
    public override void Start()
    {
        try
        {
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _table = _store.Tables.Get<Table>(TABLE_NAME);

            Project.Current.GetVariable(VariablePaths.PathQueryAnagrafica).Value = $"SELECT * FROM {TABLE_NAME}";
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
    /// Pulsante Nuovo
    /// </summary>
    [ExportMethod]
    public void aa_Nuovo()
    {
        //ripulisci tutto
        aa_Undo();

        //abilita modalità inserimento, Salva e Annulla
        Project.Current.GetVariable(VariablePaths.PathAnagraficabInInsertMode).Value  = true;
        Project.Current.GetVariable(VariablePaths.PathAnagraficaSalvaEnabled).Value   = true;
        Project.Current.GetVariable(VariablePaths.PathAnagraficaAnnullaEnabled).Value = true;
    }

    /// <summary>
    /// Pulsante Modifica
    /// </summary>
    [ExportMethod]
    public void aa_Modifica()
    {
        //abilita modalità modifica, Salva e Annulla
        Project.Current.GetVariable(VariablePaths.PathAnagraficabInEditMode).Value    = true;
        Project.Current.GetVariable(VariablePaths.PathAnagraficaSalvaEnabled).Value   = true;
        Project.Current.GetVariable(VariablePaths.PathAnagraficaAnnullaEnabled).Value = true;
    }

    /// <summary>
    /// Pulsante Annulla
    /// </summary>
    [ExportMethod]
    public void aa_Annulla()
    {
        aa_Undo();
    }

    /// <summary>
    /// Pulsante Salva
    /// </summary>
    [ExportMethod]
    public void aa_Salva()
    {
        //esegui task asincrono (bisogna eseguire così perchè sui pulsanti in ft optix si possono usare solo funzioni che restituiscono un void)
        aa_SalvaAsincrono();
    }

    /// <summary>
    /// Pulsante elimina: prende in ingresso l'id articolo da eliminare
    /// </summary>
    /// 
    [ExportMethod]
    public void aa_Elimina(int id)
    {
        // Perform the query
        _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
        _store.Query($"DELETE FROM {TABLE_NAME} WHERE Id={id}", out _, out _);

        //ripulisci, solo pulsante Nuovo è abilitato
        aa_Undo();
    }

    /// <summary>
    /// Inserisce nuovo record in anagrafica
    /// </summary>
    [ExportMethod]
    public void aa_Inserisci(NodeId prodottoObjNodeId)
    {
        //esegui task asincrono (bisogna eseguire così perchè sui pulsanti in ft optix si possono usare solo funzioni che restituiscono un void)
        aa_InserisciAsincrono(prodottoObjNodeId);
    }

    private async Task aa_SalvaAsincrono()
    {
        bool result = await aa_CheckDataValueOK();
        if (!result)
        {
            PopUpNetLogic popup = new PopUpNetLogic();
            popup.OpenPopUp("INSERISCI VALORI VALIDI",0);
            await PopUpNetLogic.WaitForConditionAsync();

            //resetto popup
            Project.Current.GetVariable(VariablePaths.PathPopupOK).Value = false;

            return;
        }

        if (Project.Current.GetVariable(VariablePaths.PathAnagraficabInInsertMode).Value)
        {
            //se modalità inserimento, aggiungi una nuova produzione
            aa_Inserisci(Project.Current.GetObject(VariablePaths.PathAnagraficaProdotto).NodeId);
        }
        else if (Project.Current.GetVariable(VariablePaths.PathAnagraficabInEditMode).Value)
        {
            //se modalità modifica, aggiorna la produzione
            var rowSelected = Project.Current.GetVariable(VariablePaths.PathrowIdAnagraficaSelected).Value;
            aa_Update(rowSelected, Project.Current.GetObject(VariablePaths.PathAnagraficaProdotto).NodeId);
        }

        //ripulisci variabili
        aa_Undo();
    }

    private async Task aa_InserisciAsincrono(NodeId prodottoObjNodeId)
    {
        var prodottoObj = InformationModel.GetObject(prodottoObjNodeId);

        string[] columns = { "Product_ID", "Descr", "Robot_Program" };

        string nomeProdotto = prodottoObj.GetVariable("Product_ID").Value.Value.ToString();
        string descrizione = prodottoObj.GetVariable("Descr").Value.Value.ToString();
        int programmaRobot = (int)prodottoObj.GetVariable("Robot_Program").Value.Value;

        if (nomeProdotto == "" || programmaRobot == 0)
        {
            PopUpNetLogic popup = new PopUpNetLogic();
            popup.OpenPopUp("INSERISCI VALORI VALIDI",0);

            await PopUpNetLogic.WaitForConditionAsync();

            //reset popup
            Project.Current.GetVariable(VariablePaths.PathPopupOK).Value = false;

            return;
        }

        var values = new object[1, 3];
        values[0, 0] = nomeProdotto;
        values[0, 1] = descrizione;
        values[0, 2] = programmaRobot;

        _table = _store.Tables.Get<Table>(TABLE_NAME);
        _table.Insert(columns, values);
    }

    /// <summary>
    /// Aggiorna i dati di anagrafica
    /// </summary>
    [ExportMethod]
    public void aa_Update(int selected_rowid, NodeId datiDaAggiornareNode)
    {
        var datiDaAggiornareObj = InformationModel.GetObject(datiDaAggiornareNode);
        string nomeProdotto = datiDaAggiornareObj.GetVariable("Product_ID").Value.Value.ToString();
        string descrizione = datiDaAggiornareObj.GetVariable("Descr").Value.Value.ToString();
        int programmaRobot = (int)datiDaAggiornareObj.GetVariable("Robot_Program").Value.Value;

        _store = Project.Current.Get<Store>("DataStores/ODBC_PRG_OPTIX");
        if (_store == null)
        {
            Log.Error("Run Query", "Missing Store Object");
        }

        //esegui query
        _store.Query($"UPDATE Product SET Product_ID = '{nomeProdotto}', Descr = '{descrizione}', Robot_Program = {programmaRobot} WHERE ID = {selected_rowid}", out _, out _);
    }

    /// <summary>
    /// Resetta le variabili della pagina
    /// </summary>
    private void aa_Undo()
    {
        //reset dei pulsanti della pagina di anagrafica
        Project.Current.GetVariable(VariablePaths.PathAnagraficaSalvaEnabled).Value = false;
        Project.Current.GetVariable(VariablePaths.PathAnagraficaEliminaEnabled).Value = false;
        Project.Current.GetVariable(VariablePaths.PathAnagraficaAnnullaEnabled).Value = false;
        Project.Current.GetVariable(VariablePaths.PathAnagraficaModificaEnabled).Value = false;
        Project.Current.GetVariable(VariablePaths.PathAnagraficabInEditMode).Value = false;
        Project.Current.GetVariable(VariablePaths.PathAnagraficabInInsertMode).Value = false;

        //reset delle text box
        Project.Current.GetVariable(VariablePaths.PathAnagraficaProdottoID).Value = 0;
        Project.Current.GetVariable(VariablePaths.PathAnagraficaProdottoProduct_ID).Value = "";
        Project.Current.GetVariable(VariablePaths.PathAnagraficaProdottoDescr).Value = "";
        Project.Current.GetVariable(VariablePaths.PathAnagraficaProdottoRobot_Program).Value = 0;
    }

    /// <summary>
    /// Controlla che i valori inseriti a pannello siano validi
    /// </summary>
    private async Task<bool> aa_CheckDataValueOK()
    {
        if (Project.Current.GetVariable(VariablePaths.PathAnagraficaProdottoProduct_ID).Value == "")
        {
            PopUpNetLogic popup = new PopUpNetLogic();
            popup.OpenPopUp("INSERISCI Product_ID",0);
            await PopUpNetLogic.WaitForConditionAsync();

            //resetto popup
            Project.Current.GetVariable(VariablePaths.PathPopupOK).Value = false;

            return false;
        }

        if (Project.Current.GetVariable(VariablePaths.PathAnagraficaProdottoRobot_Program).Value == 0)
        {
            PopUpNetLogic popup = new PopUpNetLogic();
            popup.OpenPopUp("INSERISCI Robot_Program", 0);
            await PopUpNetLogic.WaitForConditionAsync();

            //resetto popup
            Project.Current.GetVariable(VariablePaths.PathPopupOK).Value = false;

            return false;
        }

        return true;
    }

    /// <summary>
    /// Recupera i dati dell'articolo nomeArticolo
    /// </summary>
    public Anagrafica aa_GetByProductID(string nomeArticolo)
    {
        Anagrafica art = new Anagrafica();

        try
        {
            object[,] result;
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            _store.Query($"SELECT * FROM {TABLE_NAME} WHERE Product_ID='{nomeArticolo}'", out _, out result);

            if (result.GetLength(0) == 0 || result.GetLength(1) == 0)
            {
                Log.Warning("pr_GetById no record found");
                return art;
            }

            art.Id = (int)result[0,0];
            art.Product_ID = (string)result[0, 1];
            art.Descr = (string)result[0, 2];
            art.Robot_Program = (int)result[0, 3];
            art.Active = (bool)result[0, 4];

            return art;
        }
        catch (Exception ex)
        {
            Log.Warning($"Error pr_GetById: {ex}");
            return art;
        }
    }

    /// <summary>
    /// Filtra la query
    /// </summary>
    [ExportMethod]
    public void aa_FilterQuery(string filter)
    {
        string query = "";

        if (Project.Current.GetVariable(VariablePaths.PathAnagraficaFilterActive).Value)
        {
            _store = Project.Current.Get<Store>(DATASTORE_DATABASE);
            query = $"SELECT * FROM {TABLE_NAME} WHERE Product_ID LIKE '%{filter}%' OR Descr LIKE '%{filter}%'";
            _store.Query(query, out _, out _);
            Project.Current.GetVariable(VariablePaths.PathQueryAnagrafica).Value = query;
        }
        else
        {
            query = $"SELECT * FROM {TABLE_NAME}";
            Project.Current.GetVariable(VariablePaths.PathAnagraficaTextFilter).Value = "";
            Project.Current.GetVariable(VariablePaths.PathQueryAnagrafica).Value = query;
        }
    }
}


public class Anagrafica
{
    public int Id { get; set; }
    public string Product_ID { get; set; }
    public string Descr { get; set; }
    public int Robot_Program { get; set; }
    public bool Active { get; set; }
}
