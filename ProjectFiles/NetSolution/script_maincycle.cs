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
#endregion

public class script_maincycle : BaseNetLogic
{
    private PeriodicTask myPeriodicTask;
    private RuntimeNetLogicClienteToRea _prod;
    private RuntimeNetLogicAnagrafica _art;

    public override void Start()
    {
        _prod = new RuntimeNetLogicClienteToRea();
        _art = new RuntimeNetLogicAnagrafica();
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
        var ap_start                    = Project.Current.GetVariable(VariablePaths.Pathap_start);
        var pr_ButtonTerminaSelected    = Project.Current.GetVariable(VariablePaths.Pathpr_ButtonTerminaSelected);
        var ResetProduction             = Project.Current.GetVariable(VariablePaths.PathResetProduction);
        var ProduzioneInCorso           = Project.Current.GetVariable(VariablePaths.PathProduzioneInCorso);

        var DB91_CambioProduzione       = Project.Current.GetVariable(VariablePaths.PathDB91_CambioProduzione);
        var DB91_TerminaProduzione      = Project.Current.GetVariable(VariablePaths.PathDB91_TerminaProduzione);
        var DB92_ODP                    = Project.Current.GetVariable(VariablePaths.PathDB92_ODP);
        var DB92_CambioProduzioneOK     = Project.Current.GetVariable(VariablePaths.PathDB92_CambioProduzioneOK);
        var DB92_CambioProduzioneKO     = Project.Current.GetVariable(VariablePaths.PathDB92_CambioProduzioneKO);
        var DB92_AckTerminaProduzione   = Project.Current.GetVariable(VariablePaths.PathDB92_AckTerminaProduzione);

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

                //sincronizzo il campo status con lo stato corretto dell'ordine che sta girando sul PLC
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

                break;

            case 20:
                //--------------------------------------------------
                MachineStatusText.Value = "Attesa avvio produzione";
                //--------------------------------------------------

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

                //Handshake PLC - attendo risposta
                DB91_CambioProduzione.Value = true;

                //Aggiorno stato in db
                _prod.pr_UpdateStatus(OdlStart.Value, MachineStatus.Value);

                //Aggiorno pulsanti
                _prod.pr_ManageProductionButtons(MachineStatus.Value);

                //Cambio stato
                MachineStatus.Value = 70;

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

                if (!DB92_CambioProduzioneOK.Value && !DB92_CambioProduzioneKO.Value)
                {
                    //Cambio stato
                    MachineStatus.Value = 100;

                    //Aggiorno status in db
                    _prod.pr_UpdateStatus(OdlStart.Value, MachineStatus.Value);

                    //Aggiorno pulsanti
                    _prod.pr_ManageProductionButtons(MachineStatus.Value);
                }

                break;

            case 100:
                //------------------------------------
                MachineStatusText.Value = "IN LAVORO";
                //------------------------------------

                //Aggiorno quantità pezzi
                //TO DO...

                //Richiesta fine produzione
                if (pr_ButtonTerminaSelected.Value)
                {
                    pr_ButtonTerminaSelected.Value = false;

                    //Cambio stato
                    MachineStatus.Value = 148;
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

                //aggiorno stato su db
                _prod.pr_UpdateStatus(OdlStart.Value, MachineStatus.Value);

                //aggiorno pulsanti
                _prod.pr_ManageProductionButtons(MachineStatus.Value);

                //resetto tutte le variabili
                ResetPLCProductVar();
                ResetHMIProductVar();

                MachineStatus.Value = 181;

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

                //aggiorno status
                _prod.pr_UpdateStatus(OdlStart.Value, MachineStatus.Value);

                //aggiorno pulsanti
                _prod.pr_ManageProductionButtons(MachineStatus.Value);

                ResetHMIProductVar();
                ResetPLCProductVar();

                DB91_TerminaProduzione.Value = true;

                MachineStatus.Value = 191;

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
        Project.Current.GetVariable(VariablePaths.PathDB91RicettaProduct_ID).Value = "";
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
}
