#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using System.Runtime.CompilerServices;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.S7TiaProfinet;
using FTOptix.CommunicationDriver;
using FTOptix.Alarm;
using FTOptix.SQLiteStore;
using FTOptix.Recipe;
using System.Runtime.Intrinsics.Arm;
using System.Reflection.PortableExecutable;
using System.Timers;
using System.Threading;
#endregion

public class script_maincycle : BaseNetLogic
{
    private static System.Timers.Timer timer;

    private PeriodicTask myPeriodicTask;
    private RuntimeNetLogicClienteToRea _prod;
    private RuntimeNetLogicAnagrafica _art;

    private RuntimeNetLogicClienteToReaLocale _prodLocale;
    private RuntimeNetLogicAnagraficaLocale _artLocale;

    public override void Start()
    {
        timer = new System.Timers.Timer(10000); // 10000 millisecondi = 10 secondi
        // Fa partire il timer
        timer.AutoReset = true;  // Imposta AutoReset a true per eseguire ripetutamente
        timer.Enabled = true;

        _prod = new RuntimeNetLogicClienteToRea();
        _art = new RuntimeNetLogicAnagrafica();

        _prodLocale = new RuntimeNetLogicClienteToReaLocale();
        _artLocale = new RuntimeNetLogicAnagraficaLocale();

        myPeriodicTask = new PeriodicTask(Maincycle, 250, LogicObject);
        myPeriodicTask.Start();

    }

    public override void Stop()
    {
        myPeriodicTask.Dispose();
    }

    public void Maincycle()
    {
        StateMachine();
    }

    private void StateMachine()
    {
        //inizializzo variabili plc
        //script_inizializzazionitagPLC.PLC_AllineamentoVariabili_DB91();
        //script_inizializzazionitagPLC.PLC_AllineamentoVariabili_DB92();

        PopUpNetLogic popup             = new PopUpNetLogic();
        var popupOK                     = Project.Current.GetVariable(VariablePaths.PathPopupOK);
        var popupYes                    = Project.Current.GetVariable(VariablePaths.PathPopupYes);
        var popupNo                     = Project.Current.GetVariable(VariablePaths.PathPopupNo);

        var MachineStatusText           = Project.Current.GetVariable(VariablePaths.PathMachineStatusText);
        var MachineStatus               = Project.Current.GetVariable(VariablePaths.PathMachineStatus);
        var OdlStart                    = Project.Current.GetVariable(VariablePaths.PathOdlStart);
        long OdlStartLong               = OdlStart.Value; 
        var ap_start                    = Project.Current.GetVariable(VariablePaths.Pathap_start);
        var pr_ButtonTerminaSelected    = Project.Current.GetVariable(VariablePaths.Pathpr_ButtonTerminaSelected);
        var ResetProduction             = Project.Current.GetVariable(VariablePaths.PathResetProduction);
        var ProduzioneInCorso           = Project.Current.GetVariable(VariablePaths.PathProduzioneInCorso);

        var DB91_CambioProduzione       = Project.Current.GetVariable(VariablePaths.PathDB91_CambioProduzione);
        var DB91_TerminaProduzione      = Project.Current.GetVariable(VariablePaths.PathDB91_TerminaProduzione);
        var DB91_RiordinoProduzione     = Project.Current.GetVariable(VariablePaths.PathDB91_RiordinoProduzione);

        var DB92_ODP                    = Project.Current.GetVariable(VariablePaths.PathDB92_ODP);
        var DB92_CambioProduzioneOK     = Project.Current.GetVariable(VariablePaths.PathDB92_CambioProduzioneOK);
        var DB92_CambioProduzioneKO     = Project.Current.GetVariable(VariablePaths.PathDB92_CambioProduzioneKO);
        var DB92_AckTerminaProduzione   = Project.Current.GetVariable(VariablePaths.PathDB92_AckTerminaProduzione);
        var DB92_PezziDepositati        = Project.Current.GetVariable(VariablePaths.PathDB92_PezziDepositati);
        var DB92_PezziScarti            = Project.Current.GetVariable(VariablePaths.PathDB92_PezziScarti);
        var DB92_QtaRiordino            = Project.Current.GetVariable(VariablePaths.PathDB92_QtaRiordino);

        var Mem_PezziDepositati         = Project.Current.GetVariable(VariablePaths.Path_Mem_PezziDepositati); 
        var Mem_PezziScarti             = Project.Current.GetVariable(VariablePaths.Path_Mem_PezziScarti);
        var Mem_QtaRiordino             = Project.Current.GetVariable(VariablePaths.Path_Mem_QtaRiordino);

        var Extra_Produzione            = Project.Current.GetVariable(VariablePaths.Path_ExtraProduzione);
        var Quantity_ExtraProduzione    = Project.Current.GetVariable(VariablePaths.Path_QuantityExtraProduction);

        var DBExpress                   = Project.Current.GetVariable(VariablePaths.Path_DBEXpress);

        var LedSQLite                   = Project.Current.GetVariable(VariablePaths.Path_LedSQLite);
        //timer.Elapsed += OnTimedEvent;


        //casi macchina a stati
        switch ((int)MachineStatus.Value)
        {
            case 0:
                //------------------------------------------------
                MachineStatusText.Value = "Inizializzazione";
                //------------------------------------------------

                if (Project.Current.GetVariable("Model/fillBar").Value <15)
                {
                    Project.Current.GetVariable("Model/fillBar").Value += 1;
                }
                else
                {
                    MachineStatus.Value = 1;
                    
                    //cambia pagina
                    var myPanel = LogicObject.GetPanelLoader("PanelLoader1");
                    myPanel.ChangePanel("Main");

                    var mainPanel = LogicObject.GetPanelLoader("PanelLoaderScreens");
                    mainPanel.ChangePanel("Home");
                }

                break;
            case 1:
               
                //------------------------------------------------
                MachineStatusText.Value = "Stato iniziale (IDLE)";
                //------------------------------------------------

                DB91_CambioProduzione.Value = false;

                //Reset di tutte le variabili legate al caricamento programma su HMI
                ResetHMIProductVar();

                //Reset variabili PLC DB91
                ResetPLCProductVar();

                MachineStatus.Value = 10;

                break;

            case 10:
                //---------------------------------------------------------------
                MachineStatusText.Value = "Controllo allineamento con PLC-SCADA";
                //---------------------------------------------------------------

                if (DBExpress.Value)
                {
                    //sincronizzo il campo Status con lo stato corretto dell'ordine che sta girando sul PLC
                    _prod.pr_StatusSyncro(DB92_ODP.Value);

                    //se in plc sto lavorando con una ricetta allineo tabella in running
                    if (DB92_ODP.Value > 0)
                    {
                        //Sincronizzo OdlStart
                        OdlStart.Value = DB92_ODP.Value;

                        //mando prodotti al plc
                        SendProductDataToPLC(OdlStart.Value);

                        //mi metto in produzione in corso 
                        ProduzioneInCorso.Value = true;

                        //vado in produzione
                        MachineStatus.Value = 100;
                    }
                    else
                    {
                        MachineStatus.Value = 20;
                    }
                }
                else
                {
                    //sincronizzo il campo /Status con lo stato corretto dell'ordine che sta girando sul PLC
                    _prodLocale.pr_StatusSyncro_Locale(DB92_ODP.Value);

                    //se in plc sto lavorando con una ricetta allineo tabella in running
                    if (DB92_ODP.Value > 0)
                    {
                        //Sincronizzo OdlStart
                        OdlStartLong = DB92_ODP.Value;

                        //mando prodotti al plc
                        SendProductDataToPLCLocale(OdlStartLong);

                        //mi metto in produzione in corso 
                        ProduzioneInCorso.Value = true;

                        //vado in produzione
                        MachineStatus.Value = 100;
                    }
                    else
                    {
                        MachineStatus.Value = 20;
                    }
                }

                break;

            case 20:
                //--------------------------------------------------
                MachineStatusText.Value = "Attesa avvio produzione";
                //--------------------------------------------------
                if (DBExpress.Value)
                {
                    if (ap_start.Value && OdlStart.Value > 0)
                    {
                        //mando dati al plc
                        if (SendProductDataToPLC(OdlStart.Value))
                        {
                            //vado in produzione in corso
                            ProduzioneInCorso.Value = true;

                            //cambio stato
                            MachineStatus.Value = 25;
                        }
                        else
                        {
                            //Dati anagrafica non esistenti, popup e KO diretto
                            popup.OpenPopUp("Dati anagrafica non esistenti: crea prima l'articolo in Anagrafica", 0);
                            MachineStatus.Value = 21;
                        }
                    }
                }
                else
                {
                    if (ap_start.Value && OdlStartLong > 0)
                    {
                        //mando dati al plc
                        if (SendProductDataToPLCLocale(OdlStartLong))
                        {
                            //vado in produzione in corso
                            ProduzioneInCorso.Value = true;

                            //cambio stato
                            MachineStatus.Value = 25;
                        }
                        else
                        {
                            //Dati anagrafica non esistenti, popup e KO diretto
                            popup.OpenPopUp("Dati anagrafica non esistenti: crea prima l'articolo in Anagrafica", 0);
                            MachineStatus.Value = 21;
                        }
                    }
                }
                
                break;

            case 21:
                //--------------------------------------------------
                MachineStatusText.Value = "Dati di anagrafica non esistenti";
                //--------------------------------------------------

                if (popupOK.Value)
                {
                    //reset richiesta caricamento nuovo prodotto
                    ap_start.Value = false;
                    OdlStart.Value = 0;
                    
                    //
                    OdlStartLong = 0;

                    //reset popup
                    popupOK.Value = false;

                    //cambio stato
                    MachineStatus.Value = 20;
                }

                break;

            case 25:
                //---------------------------------------------------------
                MachineStatusText.Value = "Invio cambio produzione al PLC";
                //---------------------------------------------------------
                if (DBExpress.Value)
                {
                    //Handshake PLC - attendo risposta
                    DB91_CambioProduzione.Value = true;

                    //Aggiorno stato in db
                    _prod.pr_UpdateStatus(OdlStart.Value, MachineStatus.Value);

                    //Aggiorno pulsanti
                    _prod.pr_ManageProductionButtons(MachineStatus.Value);

                    //Cambio stato
                    MachineStatus.Value = 70;
                }
                else
                {
                    //Handshake PLC - attendo risposta
                    DB91_CambioProduzione.Value = true;

                    //Aggiorno stato in db
                    _prodLocale.pr_UpdateStatusLocale(OdlStartLong, MachineStatus.Value);

                    //Aggiorno pulsanti
                    _prodLocale.pr_ManageProductionButtonsLocale(MachineStatus.Value);

                    //Cambio stato
                    MachineStatus.Value = 70;
                }

                break;

            case 50:
                //------------------------------------------------------------------------------------
                MachineStatusText.Value = "Attesa richiesta caricamento progamma pressa da PLC/Robot";
                //------------------------------------------------------------------------------------

                //???

                break;

            case 55:
                //-----------------------------------------------------------------------
                MachineStatusText.Value = "Caricamento progamma in pressa e invio ok/ko al PLC";
                //-----------------------------------------------------------------------

                //???

                break;

            case 60:
                //------------------------------------------------------
                MachineStatusText.Value = "Attesa reset Richiesta caricamento pressa da PLC";
                //------------------------------------------------------
                
                //???
                
                break;

            case 70:
                //------------------------------------------------------
                MachineStatusText.Value = "Attesa esito da PLC - ok/ko";
                //------------------------------------------------------

                //Se ricevo OK
                if (DB92_CambioProduzioneOK.Value)
                {
                    //reset bit di cambio produzione
                    DB91_CambioProduzione.Value = false;

                    //reset richiesta caricamento nuovo prodotto
                    ap_start.Value = false;

                    //cambio stato
                    MachineStatus.Value = 80;
                }

                //Se ricevo KO
                if (DB92_CambioProduzioneKO.Value)
                {
                    //mostra popup
                    popup.OpenPopUp("KO da PLC: dati errati", 0);

                    //Cambio stato
                    MachineStatus.Value = 71;
                }

                break;

            case 71:
                //--------------------------------------------------
                MachineStatusText.Value = "KO da PLC: dati errati";
                //--------------------------------------------------

                if (popupOK.Value)
                {
                    //reset popup
                    popupOK.Value = false;

                    //Cambio stato
                    MachineStatus.Value = 180;
                }

                break;

            case 80:
                //-------------------------------------------------------------
                MachineStatusText.Value = "Attesa RESET segnali da PLC - ok/ko";
                //-------------------------------------------------------------
                if (DBExpress.Value)
                {
                    if (!DB92_CambioProduzioneOK.Value && !DB92_CambioProduzioneKO.Value)
                    {
                        //Cambio stato
                        MachineStatus.Value = 100;

                        //Aggiorno status in db
                        _prod.pr_UpdateStatus(OdlStart.Value, MachineStatus.Value);

                        //Aggiorno pulsanti
                        _prod.pr_ManageProductionButtons(MachineStatus.Value);
                    }
                }
                else
                {
                    if (!DB92_CambioProduzioneOK.Value && !DB92_CambioProduzioneKO.Value)
                    {
                        //Cambio stato
                        MachineStatus.Value = 100;

                        //Aggiorno status in db
                        _prodLocale.pr_UpdateStatusLocale(OdlStartLong, MachineStatus.Value);

                        //Aggiorno pulsanti
                        _prodLocale.pr_ManageProductionButtonsLocale(MachineStatus.Value);
                    }
                }
                
                break;

            case 100:
                //------------------------------------
                MachineStatusText.Value = "IN LAVORO";
                //------------------------------------
                // Seleziona il data store e i nomi dei campi in base a DBExpress.Value
                var myStore = DBExpress.Value
                    ? Project.Current.Get<Store>("DataStores/ODBC_PRG_OPTIX")
                    : Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");

                string tableName = DBExpress.Value ? "CLIENTE_TO_REA" : "RecipeSchema2";
                string idField = DBExpress.Value ? "ID" : "\"/ID\"";
                string quantityProducedField = DBExpress.Value ? "Quantity_Produced" : "\"/Quantity_Produced\"";
                string totalRejectField = DBExpress.Value ? "Total_Reject" : "\"/Total_Reject\"";
                string quantityRequestedField = DBExpress.Value ? "Quantity_Requested" : "\"/Quantità\"";
                string extraProductionField = DBExpress.Value ? "Extra_Production" : "\"/Extra_Production\"";

                // Aggiorna le quantità se necessario
                if (DB92_PezziDepositati.Value != Mem_PezziDepositati.Value ||
                    DB92_PezziScarti.Value != Mem_PezziScarti.Value ||
                    DB92_QtaRiordino.Value != Mem_QtaRiordino.Value)
                {
                    AggiornaQuantita(myStore, tableName, idField, quantityProducedField, totalRejectField);
                }

                // Verifica se è stata raggiunta la quantità richiesta più l'extra produzione
                ControllaExtraProduzione(myStore, tableName, idField, quantityRequestedField, extraProductionField);

                // Gestisce la richiesta di fine produzione
                if (pr_ButtonTerminaSelected.Value)
                {
                    pr_ButtonTerminaSelected.Value = false;
                    MachineStatus.Value = 148; // Codice di stato per "fine produzione"
                }

                // Funzione per aggiornare le quantità
                void AggiornaQuantita(
                    Store store,
                    string table,
                    string idFieldName,
                    string quantityProducedFieldName,
                    string totalRejectFieldName)
                {
                    Object[,] ResultSet;
                    String[] Header;

                    // Aggiorna quantità pezzi prodotti
                    if (DB92_PezziDepositati.Value != Mem_PezziDepositati.Value)
                    {
                        string query = $"UPDATE {table} SET {quantityProducedFieldName} = '{(uint)DB92_PezziDepositati.Value}' WHERE {idFieldName} = '{OdlStartLong}'";
                        store.Query(query, out Header, out ResultSet);
                        Mem_PezziDepositati.Value = DB92_PezziDepositati.Value;
                    }

                    // Aggiorna quantità scarti
                    if (DB92_PezziScarti.Value != Mem_PezziScarti.Value)
                    {
                        string query = $"UPDATE {table} SET {totalRejectFieldName} = '{(uint)DB92_PezziScarti.Value}' WHERE {idFieldName} = '{OdlStartLong}'";
                        store.Query(query, out Header, out ResultSet);
                        Mem_PezziScarti.Value = DB92_PezziScarti.Value;
                    }

                    // Aggiorna quantità di riordino
                    if (DB92_QtaRiordino.Value != Mem_QtaRiordino.Value)
                    {
                        string query = $"UPDATE {table} SET {quantityProducedFieldName} = '{(uint)DB92_PezziDepositati.Value}' WHERE {idFieldName} = '{OdlStartLong}'";
                        store.Query(query, out Header, out ResultSet);
                        Mem_QtaRiordino.Value = DB92_QtaRiordino.Value;
                    }
                }

                // Funzione per controllare l'extra produzione
                void ControllaExtraProduzione(
                    Store store,
                    string table,
                    string idFieldName,
                    string quantityRequestedFieldName,
                    string extraProductionFieldName)
                {
                    Object[,] ResultSetExtra;
                    String[] HeaderExtra;

                    string query = $"SELECT {quantityRequestedFieldName}, {extraProductionFieldName} FROM {table} WHERE {idFieldName} = '{OdlStartLong}'";
                    store.Query(query, out HeaderExtra, out ResultSetExtra);

                    int quantityRequested = Convert.ToInt32(ResultSetExtra[0, 0]);
                    int extraProduction = Convert.ToInt32(ResultSetExtra[0, 1]);

                    // Richiesta per extra-produzione
                    if (DB92_PezziDepositati.Value >= quantityRequested + extraProduction)
                    {
                        MachineStatus.Value = 110; // Codice di stato per "extra produzione raggiunta"
                    }
                }
                break;

            case 110:
                //------------------------------------------------------------
                MachineStatusText.Value = "Popup di conferma extra-produzione";
                //------------------------------------------------------------

                //popup di conferma chiusura ordine
                var openExtra = Project.Current.GetVariable(VariablePaths.PathOpenEXTRA);
                openExtra.Value = true;

                //Cambio stato
                MachineStatus.Value = 111;

                break;

            case 111:
                //-------------------------------------------------------------
                MachineStatusText.Value = "Attesa di conferma";
                //-------------------------------------------------------------
                // Seleziona il data store e i nomi dei campi in base a DBExpress.Value
                var myStoreE = DBExpress.Value
                    ? Project.Current.Get<Store>("DataStores/ODBC_PRG_OPTIX")
                    : Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");

                string tableNamePopup = DBExpress.Value ? "CLIENTE_TO_REA" : "RecipeSchema2";
                string idFieldPopup = DBExpress.Value ? "ID" : "\"/ID\"";
                string extraProductionFieldPopup = DBExpress.Value ? "Extra_Production" : "\"/Extra_Production\"";

                // Richiesta fine produzione
                if (pr_ButtonTerminaSelected.Value)
                {
                    pr_ButtonTerminaSelected.Value = false;
                    DB91_TerminaProduzione.Value = true;
                    MachineStatus.Value = 150;
                }

                // Ritorno in produzione
                if (Extra_Produzione.Value)
                {
                    Extra_Produzione.Value = false;
                    DB91_RiordinoProduzione.Value = true;

                    // Recupera il valore di Extra_Production
                    Object[,] ResultSetOldExtra;
                    String[] HeaderOldExtra;
                    string queryOldE = $"SELECT {extraProductionFieldPopup} FROM {tableNamePopup} WHERE {idFieldPopup} = '{OdlStartLong}'";
                    myStoreE.Query(queryOldE, out HeaderOldExtra, out ResultSetOldExtra);
                    int OldExtraProduction = Convert.ToInt32(ResultSetOldExtra[0, 0]);

                    // Aggiorna Quantity_ExtraProduzione
                    Quantity_ExtraProduzione.Value += OldExtraProduction;

                    // Aggiorna il database
                    Object[,] ResultSet;
                    String[] Header;
                    string query = $"UPDATE {tableNamePopup} SET {extraProductionFieldPopup} = '{(uint)Quantity_ExtraProduzione.Value}' WHERE {idFieldPopup} = '{OdlStartLong}'";
                    myStoreE.Query(query, out Header, out ResultSet);

                    // Cambio stato
                    MachineStatus.Value = 100;
                }
                break;

            case 148:
                //------------------------------------------------------------
                MachineStatusText.Value = "Popup di conferma chiusura ordine";
                //------------------------------------------------------------

                //popup di conferma chiusura ordine
                popup.OpenPopUp("Confermi di voler terminare la produzione in corso?", 4);

                //Cambio stato
                MachineStatus.Value = 149;

                break;

            case 149:

                if (popupYes.Value)
                {
                    //reset popup
                    popupYes.Value = false;

                    DB91_TerminaProduzione.Value = true;

                    //Cambio stato
                    MachineStatus.Value = 150;
                }
                else if (popupNo.Value)
                {
                    //reset popup
                    popupNo.Value = false;

                    //cambio stato
                    MachineStatus.Value = 100;
                }

                break;

            case 150:
                //-----------------------------------------------------------------
                MachineStatusText.Value = "Fine produzione + attesa handshake PLC";
                //-----------------------------------------------------------------

                //rimango in attesa dell'ack PLC
                if (DB92_AckTerminaProduzione.Value)
                {
                    DB91_TerminaProduzione.Value = false;

                    ProduzioneInCorso.Value = false;

                    MachineStatus.Value = 160;
                }

                break;

            case 160:
                //----------------------------------------------------------------------------
                MachineStatusText.Value = "Aggiornamento tabelle e spostamento nello storico";
                //----------------------------------------------------------------------------
                if (DBExpress.Value) {
                    //Aggiorno stato in db
                    _prod.pr_UpdateStatus(OdlStart.Value, MachineStatus.Value);

                    //Aggiorno pulsanti
                    _prod.pr_ManageProductionButtons(MachineStatus.Value);

                    //sposto su storico e elimino record
                    _prod.pr_TerminateAllRunning();
                    _prod.pr_Elimina(OdlStart.Value);

                    //reset variabili
                    ResetHMIProductVar();
                    ResetPLCProductVar();

                    MachineStatus.Value = 165;
                }
                else
                {
                    //Aggiorno stato in db
                    _prodLocale.pr_UpdateStatusLocale(OdlStartLong, MachineStatus.Value);

                    //Aggiorno pulsanti
                    _prodLocale.pr_ManageProductionButtonsLocale(MachineStatus.Value);

                    //sposto su storico e elimino record
                    _prodLocale.pr_TerminateAllRunningLocale();
                    _prodLocale.pr_EliminaLocale(OdlStart.Value);

                    //reset variabili
                    ResetHMIProductVar();
                    ResetPLCProductVar();

                    MachineStatus.Value = 165;
                }
                
                break;

            case 165:
                //----------------------------------------------------------------------------
                MachineStatusText.Value = "Attesa reset richiesta termina produzione dal PLC";
                //----------------------------------------------------------------------------

                if (!DB92_AckTerminaProduzione.Value)
                {
                    MachineStatus.Value = 199;
                }

                break;

            case 180:
                //---------------------------------------------------------------
                MachineStatusText.Value = "ERRORE CARICAMENTO RICETTA KO DA PLC";
                //---------------------------------------------------------------
                if (DBExpress.Value)
                {
                    //aggiorno stato su db
                    _prod.pr_UpdateStatus(OdlStart.Value, MachineStatus.Value);

                    //aggiorno pulsanti
                    _prod.pr_ManageProductionButtons(MachineStatus.Value);

                    //resetto tutte le variabili
                    ResetPLCProductVar();
                    ResetHMIProductVar();

                    MachineStatus.Value = 181;
                }
                else
                {
                    //aggiorno stato su db
                    _prodLocale.pr_UpdateStatusLocale(OdlStartLong, MachineStatus.Value);

                    //aggiorno pulsanti
                    _prodLocale.pr_ManageProductionButtonsLocale(MachineStatus.Value);

                    //resetto tutte le variabili
                    ResetPLCProductVar();
                    ResetHMIProductVar();

                    MachineStatus.Value = 181;
                }
                break;

            case 181:
                //-------------------------------------------------------------
                MachineStatusText.Value = "Attesa rimozione segnali KO da PLC";
                //-------------------------------------------------------------

                if (!DB92_CambioProduzioneKO.Value)
                {
                    MachineStatus.Value = 199;
                }

                break;

            case 190:
                //----------------------------------------------------------------
                MachineStatusText.Value = "ANNULLAMENTO CARICAMENTO DA OPERATORE";
                //----------------------------------------------------------------
                if (DBExpress.Value)
                {
                    //aggiorno status
                    _prod.pr_UpdateStatus(OdlStart.Value, MachineStatus.Value);

                    //aggiorno pulsanti
                    _prod.pr_ManageProductionButtons(MachineStatus.Value);

                    ResetHMIProductVar();
                    ResetPLCProductVar();

                    DB91_TerminaProduzione.Value = true;

                    MachineStatus.Value = 191;
                }
                else
                {
                    //aggiorno status
                    _prodLocale.pr_UpdateStatusLocale(OdlStartLong, MachineStatus.Value);

                    //aggiorno pulsanti
                    _prodLocale.pr_ManageProductionButtonsLocale(MachineStatus.Value);

                    ResetHMIProductVar();
                    ResetPLCProductVar();

                    DB91_TerminaProduzione.Value = true;

                    MachineStatus.Value = 191;
                }

                break;

            case 191:
                //-------------------------------------------------
                MachineStatusText.Value = "Attesa fine produzione da PLC";
                //-------------------------------------------------

                if (DB92_AckTerminaProduzione.Value)
                {
                    DB91_TerminaProduzione.Value = false;

                    MachineStatus.Value = 199;
                }

                break;

            case 199:
                //-------------------------------------------------------------------------------------------
                MachineStatusText.Value = "Termina produzione completata correttamente";
                //-------------------------------------------------------------------------------------------

                if (!DB92_AckTerminaProduzione.Value)
                {
                    MachineStatus.Value = 20;
                }

                break;

            default:
                break;
        }

        if (ResetProduction.Value)
        {
            MachineStatus.Value = 190;
        }
    }

    private void ResetHMIProductVar()
    {
        Project.Current.GetVariable(VariablePaths.PathOdlStart).Value = 0;
        Project.Current.GetVariable(VariablePaths.Pathpr_ButtonTerminaSelected).Value = false;
        Project.Current.GetVariable(VariablePaths.Pathap_start).Value = false;
        Project.Current.GetVariable(VariablePaths.PathResetProduction).Value = false;
        Project.Current.GetVariable(VariablePaths.PathProduzioneInCorso).Value = false;
    }

    private void ResetPLCProductVar()
    {
        Project.Current.GetVariable(VariablePaths.PathDB91_CambioProduzione).Value = false;
        Project.Current.GetVariable(VariablePaths.PathDB91_TerminaProduzione).Value = false;

        //azzera valori ricetta
        Project.Current.GetVariable(VariablePaths.PathDB91RicettaProduct_ID).Value = 0;
        Project.Current.GetVariable(VariablePaths.PathDB91RicettaDescrizione).Value = "";
        Project.Current.GetVariable(VariablePaths.PathDB91RicettaProgrammaRobot).Value = 0;
    }

    private bool SendProductDataToPLC(int odp)
    {
        //recupero il nome articolo
        ClienteToRea currentProd = new ClienteToRea();
        currentProd = _prod.pr_GetById(odp);

        if (currentProd.Production_Command == "" || currentProd.Production_Command is null)
            return false;

        //recupero i dati di anagrafica
        Anagrafica articolo = new Anagrafica();
        articolo = _art.aa_GetByProductID(currentProd.Product_ID);

        if (articolo.Product_ID == "" || articolo.Product_ID is null)
            return false;

        Project.Current.GetVariable(VariablePaths.PathDB91RicettaProduct_ID).Value = articolo.Product_ID;
        Project.Current.GetVariable(VariablePaths.PathDB91RicettaDescrizione).Value = articolo.Descr;
        Project.Current.GetVariable(VariablePaths.PathDB91RicettaProgrammaRobot).Value = articolo.Robot_Program;

        return true;
    }

    private bool SendProductDataToPLCLocale(long odp)
    {
        //recupero il nome articolo
        ClienteToReaLocale currentProd = new ClienteToReaLocale();
        currentProd = _prodLocale.pr_GetByIdLocale(odp);

        if (currentProd.Production_Command == "" || currentProd.Production_Command is null)
            return false;

        //recupero i dati di anagrafica
        AnagraficaLocale articolo = new AnagraficaLocale();
        articolo = _artLocale.aa_GetByProductIDLocale(currentProd.Product_ID);

        if (articolo.Product_ID == "" || articolo.Product_ID is null)
            return false;

        Project.Current.GetVariable(VariablePaths.PathDB91RicettaProduct_ID).Value = articolo.Product_ID;
        Project.Current.GetVariable(VariablePaths.PathDB91RicettaDescrizione).Value = articolo.Descr;
        Project.Current.GetVariable(VariablePaths.PathDB91RicettaProgrammaRobot).Value = articolo.Robot_Program;

        return true;
    }
/*
    private static void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        LedSQLite.Value = TestDBLocale();
    }

    private bool TestDBLocale()
    {
        object[,] TextConnection;
        var _store = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
        try
        {
            _store.Query($"SELECT 1", out _, out TextConnection);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
            return false;
        }
    }
 */
}
