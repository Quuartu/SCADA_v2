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
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Net;
using FluentFTP;
using FTOptix.Report;
#endregion

public class script_maincycle : BaseNetLogic
{
    private PeriodicTask myPeriodicTask;
    private RuntimeNetLogicClienteToRea _prod;
    private RuntimeNetLogicAnagrafica _art;

    private RuntimeNetLogicClienteToReaLocale _prodLocale;
    private RuntimeNetLogicAnagraficaLocale _artLocale;

    public override void Start()
    {
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
        PopUpNetLogic popup                             = new PopUpNetLogic();
        var popupOK                                     = Project.Current.GetVariable(VariablePaths.PathPopupOK);
        var popupYes                                    = Project.Current.GetVariable(VariablePaths.PathPopupYes);
        var popupNo                                     = Project.Current.GetVariable(VariablePaths.PathPopupNo);

        var MachineStatusText                           = Project.Current.GetVariable(VariablePaths.PathMachineStatusText);
        var MachineStatus                               = Project.Current.GetVariable(VariablePaths.PathMachineStatus);
        var OdlStart                                    = Project.Current.GetVariable(VariablePaths.PathOdlStart);
        long OdlStartLong                               = OdlStart.Value; 
        var ap_start                                    = Project.Current.GetVariable(VariablePaths.Pathap_start);
        var pr_ButtonTerminaSelected                    = Project.Current.GetVariable(VariablePaths.Pathpr_ButtonTerminaSelected);
        var ResetProduction                             = Project.Current.GetVariable(VariablePaths.PathResetProduction);
        var ProduzioneInCorso                           = Project.Current.GetVariable(VariablePaths.PathProduzioneInCorso);

        var DB91_CambioProduzione                       = Project.Current.GetVariable(VariablePaths.PathDB91_CambioProduzione);
        var DB91_TerminaProduzione                      = Project.Current.GetVariable(VariablePaths.PathDB91_TerminaProduzione);
        var DB91_RiordinoProduzione                     = Project.Current.GetVariable(VariablePaths.PathDB91_RiordinoProduzione);
        var DB91_AckInvioProgrammaPressa                = Project.Current.GetVariable(VariablePaths.PathDB91_AckInvioProgrammaPressa);
        var DB91_AckProgrammaPressaInviatoOK            = Project.Current.GetVariable(VariablePaths.PathDB91_AckProgrammaPressaInviatoOK);
        var DB91_AckProgrammaPressaInviatoKO            = Project.Current.GetVariable(VariablePaths.PathDB91_AckProgrammaPressaInviatoKO);

        var DB92_ODP                                    = Project.Current.GetVariable(VariablePaths.PathDB92_ODP);
        var DB92_CambioProduzioneOK                     = Project.Current.GetVariable(VariablePaths.PathDB92_CambioProduzioneOK);
        var DB92_CambioProduzioneKO                     = Project.Current.GetVariable(VariablePaths.PathDB92_CambioProduzioneKO);
        var DB92_AckTerminaProduzione                   = Project.Current.GetVariable(VariablePaths.PathDB92_AckTerminaProduzione);
        var DB92_PezziDepositati                        = Project.Current.GetVariable(VariablePaths.PathDB92_PezziDepositati);
        var DB92_PezziScarti                            = Project.Current.GetVariable(VariablePaths.PathDB92_PezziScarti);
        var DB92_QtaRiordino                            = Project.Current.GetVariable(VariablePaths.PathDB92_QtaRiordino);
        var DB92_InviaProgrammaAPressa                  = Project.Current.GetVariable(VariablePaths.PathDB92_InviaProgrammaAPressa);

        var PressaError                                 = Project.Current.GetVariable(VariablePaths.Path_PressaError);

        var Mem_PezziDepositati                         = Project.Current.GetVariable(VariablePaths.Path_Mem_PezziDepositati); 
        var Mem_PezziScarti                             = Project.Current.GetVariable(VariablePaths.Path_Mem_PezziScarti);
        var Mem_QtaRiordino                             = Project.Current.GetVariable(VariablePaths.Path_Mem_QtaRiordino);

        var Extra_Produzione                            = Project.Current.GetVariable(VariablePaths.Path_ExtraProduzione);
        var Quantity_ExtraProduzione                    = Project.Current.GetVariable(VariablePaths.Path_QuantityExtraProduction);

        var DBExpress                                   = Project.Current.GetVariable(VariablePaths.Path_DBEXpress);
        var FolderSelected                              = Project.Current.GetVariable(VariablePaths.Path_FolderSelected);
        var PathFolderTemp                              = Project.Current.GetVariable(VariablePaths.Path_PathFolderTemp);

        var ftp_NomeCartella = Project.Current.GetVariable(VariablePaths.Path_ftp_NomeCartella);
        var ftp_IndirizzoIP = Project.Current.GetVariable(VariablePaths.Path_ftp_IndirizzoIP);
        var sPressProgramName = Project.Current.GetVariable(VariablePaths.Path_sPressProgramName);     // Nome del programma della pressa 
        var sPressGroup = Project.Current.GetVariable(VariablePaths.Path_sPressGroup);           // Gruppo del programma della pressa 
        var config_pressPathIn = Project.Current.GetVariable(VariablePaths.Path_config_pressPathIn);    // Percorso di input per la pressa 
        var config_pressPathOut = Project.Current.GetVariable(VariablePaths.Path_config_pressPathOut);   // Percorso di output per la pressa 
        var config_pressExt = Project.Current.GetVariable(VariablePaths.Path_config_pressExt);       // Estensione del file della pressa *
        var config_pressUser = Project.Current.GetVariable(VariablePaths.Path_config_pressUser);      // Username per la connessione FTP *
        var config_pressPswd = Project.Current.GetVariable(VariablePaths.Path_config_pressPswd);      // Password per la connessione FTP *
        //private bool config_pressToMount;                                                                                // Flag che indica se montare il volume


        //casi macchina a stati
        switch ((int)MachineStatus.Value)
        {
            case 0:
                //-------------------------------------------
                MachineStatusText.Value = "Inizializzazione";
                //-------------------------------------------

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
                //-----------------------------------------------------------
                MachineStatusText.Value = "Dati di anagrafica non esistenti";
                //-----------------------------------------------------------

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
                    MachineStatus.Value = 50;
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
                    MachineStatus.Value = 50;
                }

                break;

            case 50:
                //-------------------------------------------------------------------------------------
                MachineStatusText.Value = "Attesa richiesta caricamento programma pressa da PLC/Robot";
                //-------------------------------------------------------------------------------------

                //attesa che dal robot/PLC arrivi richiesta di caricare dati in pressa
                if (DB92_InviaProgrammaAPressa.Value == true)
                {
                    MachineStatus.Value = 55;
                    PressaError.Value = false;
                }
                if (DB92_CambioProduzioneKO.Value)
                {
                    MachineStatus.Value = 180;
                }

                break;

            case 55:
                //------------------------------------------------------------------------------
                MachineStatusText.Value = "Caricamento progamma in pressa e invio ok/ko al PLC";
                //------------------------------------------------------------------------------

                //se caricamento dati in pressa eseguito correttamente / a PLC
                if (GestioneCaricamentoPressa(OdlStartLong, sPressProgramName.Value, sPressGroup.Value, config_pressPathIn.Value, config_pressPathOut.Value, config_pressExt.Value, config_pressUser.Value, config_pressPswd.Value)) {
                    DB91_AckInvioProgrammaPressa.Value = true;
                    DB91_AckProgrammaPressaInviatoOK.Value = true;
                    DB91_AckProgrammaPressaInviatoKO.Value = false;
                } else
                {
                    //caricamento non andato a buon fine / a PLC
                    DB91_AckInvioProgrammaPressa.Value = true;
                    DB91_AckProgrammaPressaInviatoOK.Value = false;
                    DB91_AckProgrammaPressaInviatoKO.Value = true;
                    PressaError.Value = true;
                }

                //cambio stato
                MachineStatus.Value = 60;

                break;

            case 60:
                //---------------------------------------------------------------------------
                MachineStatusText.Value = "Attesa reset Richiesta caricamento pressa da PLC";
                //---------------------------------------------------------------------------
                if ( !DB92_InviaProgrammaAPressa.Value ) {
                    // se non ho caricato correttamente file in pressa
                    if (PressaError.Value) {
                        //cambio stato
                        MachineStatus.Value = 190;
                        PressaError.Value = false;
                    }
                    else
                    {
                        MachineStatus.Value = 70;
                    }
                    //Azzeramento comandi pressa
                    DB91_AckInvioProgrammaPressa.Value = false;
                    DB91_AckProgrammaPressaInviatoKO.Value = false;
                    DB91_AckProgrammaPressaInviatoOK.Value = false;
                }

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
                //-------------------------------------------------
                MachineStatusText.Value = "KO da PLC: dati errati";
                //-------------------------------------------------

                if (popupOK.Value)
                {
                    //reset popup
                    popupOK.Value = false;

                    //Cambio stato
                    MachineStatus.Value = 180;
                }

                break;

            case 80:
                //--------------------------------------------------------------
                MachineStatusText.Value = "Attesa RESET segnali da PLC - ok/ko";
                //--------------------------------------------------------------
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
                //-------------------------------------------------------------
                MachineStatusText.Value = "Popup di conferma extra-produzione";
                //-------------------------------------------------------------

                //popup di conferma chiusura ordine
                var openExtra = Project.Current.GetVariable(VariablePaths.PathOpenEXTRA);
                openExtra.Value = true;

                //Cambio stato
                MachineStatus.Value = 111;

                break;

            case 111:
                //---------------------------------------------
                MachineStatusText.Value = "Attesa di conferma";
                //---------------------------------------------
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
                //--------------------------------------------------------
                MachineStatusText.Value = "Attesa fine produzione da PLC";
                //--------------------------------------------------------

                if (DB92_AckTerminaProduzione.Value)
                {
                    DB91_TerminaProduzione.Value = false;

                    MachineStatus.Value = 199;
                }

                break;

            case 199:
                //----------------------------------------------------------------------
                MachineStatusText.Value = "Termina produzione completata correttamente";
                //----------------------------------------------------------------------

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

        if ((short)FolderSelected.Value != 0)
        {
            if (PathFolderTemp.Value != "")
            {
                switch ((short)FolderSelected.Value) {

                case (short)1:
                    ftp_NomeCartella.Value = PathFolderTemp.Value;
                    break;

                case (short)2:
                    ftp_IndirizzoIP.Value = PathFolderTemp.Value;
                    break;

                case (short)3:
                    sPressProgramName.Value = PathFolderTemp.Value;
                    break;

                case (short)4:
                    sPressGroup.Value = PathFolderTemp.Value;
                    break;

                case (short)5:
                    config_pressPathIn.Value = PathFolderTemp.Value;
                    break;

                case (short)6:
                    config_pressPathOut.Value = PathFolderTemp.Value;
                    break;
                }
                FolderSelected.Value = 0;
                PathFolderTemp.Value = "";
            }
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

    //GESTIONE PROGRAMMA PRESSA
    public bool GestioneCaricamentoPressa(long OdlStart, string sPressProgramName, string sPressGroup, string config_pressPathIn, string config_pressPathOut, string config_pressExt, string config_pressUser, string config_pressPswd)
    {
        try
        {
            // Verifica preliminare sull'ID prodotto (OdlStart è una variabile globale)
            long ProdID = OdlStart;
            if (ProdID == 0)
            {
                // Mostra errore se l'ID del prodotto è 0
                ShowError("Errore", "Impossibile caricare l'ordine zero.");
                return false;
            }

            // Recupera il gruppo e il nome del file da caricare
            string FileGroup = sPressGroup;
            string FileSrc = sPressProgramName + config_pressExt; // Nome del file con estensione

            // Costruisce il percorso completo della sorgente del file
            string FullPath = Path.Combine(config_pressPathIn, FileGroup);

            // Verifica se è possibile montare il volume di rete (connessione FTP)
            if (!MountNetworkDrive(FullPath, config_pressUser, config_pressPswd))
            {
                // Mostra errore se non è possibile connettersi
                ShowError("Errore", $"Volume non raggiungibile: {FullPath}");
                //config_pressToMount = true;
                return false;
            }

            // Verifica se il file esiste nel percorso indicato
            if (!File.Exists(Path.Combine(FullPath, FileSrc)))
            {
                // Mostra errore se il file non è trovato
                ShowError("Errore", $"File non trovato! Percorso: {FullPath}\\{FileSrc}");
                return false;
            }

            // Copia il file nella destinazione (pressa)
            if (!CopyFile(FileSrc, "0", config_pressExt, FullPath, config_pressPathOut))
            {
                // Mostra errore se la copia fallisce
                ShowError("Errore", "Errore durante la copia del file sulla pressa.");
                return false;
            }

            // Verifica che la lunghezza del file sorgente e del file copiato siano uguali
            if (!CheckFileConsistency(FullPath, FileSrc, config_pressPathOut, "0", config_pressExt))
            {
                // Mostra errore se i file non sono consistenti
                ShowError("Errore", "File non esistente sulla pressa.");
                return false;
            }

            // Se tutto va bene, ritorna true
            return true;
        }
        catch (Exception ex)
        {
            // Gestione generale degli errori
            ShowError("Errore", "Errore caricamento pressa: " + ex.Message);
            return false;
        }
    }

    // Funzione che si occupa di montare il volume di rete (connessione FTP)
    private bool MountNetworkDrive(string path, string config_pressUser, string config_pressPswd)
    {
        try
        {
            // Estrazione dell'unità e del percorso
            string drive = path.Substring(0, 2);
            string folder = path.Substring(2).Replace("\\", "/"); // Per FluentFTP usa "/" come separatore

            // Creazione del client FTP
            using (var ftpClient = new FtpClient("ftp://" + drive)) // L'indirizzo del server FTP
            {
                ftpClient.Credentials = new System.Net.NetworkCredential(config_pressUser, config_pressPswd);

                // Connessione al server FTP
                ftpClient.Connect();

                // Verifica se la directory esiste
                if (ftpClient.DirectoryExists(folder))
                {
                    Console.WriteLine("Volume montato correttamente.");
                    return true;
                }
                else
                {
                    Console.WriteLine("Directory non trovata.");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            // Gestione errori di connessione
            Console.WriteLine($"Errore connessione FTP: {ex.Message}");
            return false;
        }
    }

    // Funzione che copia un file da una sorgente a una destinazione
    private bool CopyFile(string src, string dest, string ext, string srcPath, string destPath)
    {
        try
        {
            // Costruisce i percorsi completi per il file sorgente e destinazione
            string srcFile = Path.Combine(srcPath, src + ext);
            string destFile = Path.Combine(destPath, dest + ext);

            // Se il file sorgente esiste, lo copia nella destinazione
            if (File.Exists(srcFile))
            {
                File.Copy(srcFile, destFile, true);  // True per sovrascrivere eventuali file esistenti
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            // Gestione degli errori durante la copia del file
            Console.WriteLine($"Errore copia file: {ex.Message}");
            return false;
        }
    }

    // Funzione che controlla se la lunghezza del file sorgente e quello copiato coincidono
    private bool CheckFileConsistency(string srcPath, string srcFile, string destPath, string destFile, string config_pressExt)
    {
        try
        {
            // Ottiene la lunghezza del file sorgente e di quello copiato
            long lenIn = new FileInfo(Path.Combine(srcPath, srcFile)).Length;
            long lenOut = new FileInfo(Path.Combine(destPath, destFile + config_pressExt)).Length;
            return lenIn == lenOut;  // Verifica se le lunghezze sono uguali
        }
        catch (Exception ex)
        {
            // Gestione degli errori durante la verifica della lunghezza dei file
            Console.WriteLine($"Errore verifica file: {ex.Message}");
            return false;
        }
    }

    // Funzione che mostra un errore sulla UI (simulato con Console.WriteLine)
    private void ShowError(string title, string message)
    {
        // Simula una finestra pop-up che mostra l'errore
        Console.WriteLine($"{title}: {message}");
    }

    public int SaveProgramPressBrake(string sPressProgramName, string sPressGroup, string config_pressPathIn, string config_pressPathOut, string config_pressExt)
    {
        try
        {
            // Verifica che il nome del programma della pressa non sia vuoto
            if (string.IsNullOrEmpty(sPressProgramName))
            {
                ShowError("Errore", "Manca il nome del file pressa da salvare!");
                return 3; // 3 = Errore salvataggio
            }

            //// Verifica che il codice dell'articolo non sia vuoto
            //if (string.IsNullOrEmpty(sCodiceArticolo))
            //{
            //    ShowError("Errore", "Errore: CodArticolo vuoto!");
            //    return 3; // 3 = Errore salvataggio
            //}

            // Chiede conferma all'operatore per salvare il programma
            var result = ShowConfirmation("Conferma", $"Salvare eventuali modifiche apportate al programma pressa '{sPressProgramName}'?");

            // Se l'operatore conferma il salvataggio
            if (result == true)
            {
                string Src = "0";
                string Dest = sPressProgramName;
                string PathOutSave = Path.Combine(config_pressPathIn, sPressGroup);
                string PathSrcRead = config_pressPathOut;

                // Copia il programma dalla cartella di output a quella di input
                if (CopyFile(Src, Dest, config_pressExt, PathSrcRead, PathOutSave))
                {
                    return 1; // 1 = Salvato con successo
                }
                else
                {
                    return 3; // 3 = Errore salvataggio
                }
            }
            else
            {
                return 2; // 2 = Non salvare
            }
        }
        catch (Exception ex)
        {
            // Gestione degli errori
            ShowError("Errore", "Errore salvataggio programma pressa: " + ex.Message);
            return 3; // 3 = Errore salvataggio
        }
    }

    // Funzione che simula la richiesta di conferma all'operatore
    private bool ShowConfirmation(string title, string message)
    {
        // Simula una finestra di conferma (sì/no) per l'operatore
        Console.WriteLine($"{title}: {message}");
        // Per semplicità, restituisce sempre true (conferma)
        return true;
    }
}
