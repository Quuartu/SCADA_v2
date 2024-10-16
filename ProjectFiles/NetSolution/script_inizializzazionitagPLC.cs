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
using FTOptix.EventLogger;
using FTOptix.Core;
using FTOptix.SQLiteStore;
using FTOptix.Recipe;
using FTOptix.Report;
#endregion

public class script_inizializzazionitagPLC : BaseNetLogic
{
   
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
}

public static class VariablePaths
{
    //database
    //public const string PathDataStore = "DataStores/ODBC_PRG_OPTIX"; //???? ha senso?

    //popup
    public const string PathOpenPopup                               = "Model/Popup/OpenPopUp";
    public const string PathTesto_PopUp                             = "Model/Popup/Testo_PopUp";
    public const string PathPopupButtonOK                           = "UI/POPUP_PRG/DialogBoxPopUp/Rectangle1/ButtonOK";
    public const string PathPopupButtonYes                          = "UI/POPUP_PRG/DialogBoxPopUp/Rectangle1/ButtonYes";
    public const string PathPopupButtonNo                           = "UI/POPUP_PRG/DialogBoxPopUp/Rectangle1/ButtonNo";
    public const string PathPopupOK                                 = "Model/Popup/PopupOK";
    public const string PathPopupYes                                = "Model/Popup/PopupYes";
    public const string PathPopupNo                                 = "Model/Popup/PopupNo";

    public const string PathOpenEXTRA                               = "Model/Popup/OpenPopUpExtraProduzione";

    public const string PathMachineStatusText                       = "Model/MacchinaStati/MachineStatusText";
    public const string PathMachineStatus                           = "Model/MacchinaStati/MachineStatus";
    public const string PathOdlStart                                = "Model/Produzione/OdlStart"; 
    public const string Pathap_start                                = "Model/Produzione/ap_start";
    public const string Pathpr_ButtonTerminaSelected                = "Model/Produzione/pr_ButtonTerminaSelected";
    public const string PathResetProduction                         = "Model/Produzione/ResetProduction";

    public const string PathDB91_CambioProduzione                   = "Model/Drivers/DB91/DB91_CambioProduzione";
    public const string PathDB91_TerminaProduzione                  = "Model/Drivers/DB91/DB91_TerminaProduzione";
    public const string PathDB91_RiordinoProduzione                 = "Model/Drivers/DB91/DB91_RiordinoProduzione";
    public const string PathDB91_AckInvioProgrammaPressa            = "Model/Drivers/DB91/DB91_AckInvioProgrammaPressa";
    public const string PathDB91_AckProgrammaPressaInviatoOK        = "Model/Drivers/DB91/DB91_AckProgrammaPressaInviatoOK";
    public const string PathDB91_AckProgrammaPressaInviatoKO = "Model/Drivers/DB91/DB91_AckProgrammaPressaInviatoKO";

    public const string PathDB92_ODP                                = "Model/Drivers/DB92/DB92_ODP";
    public const string PathDB92_CambioProduzioneOK                 = "Model/Drivers/DB92/DB92_CambioProduzioneOK";
    public const string PathDB92_CambioProduzioneKO                 = "Model/Drivers/DB92/DB92_CambioProduzioneKO";
    public const string PathDB92_AckTerminaProduzione               = "Model/Drivers/DB92/DB92_AckTerminaProduzione";
    public const string PathDB92_ProduzioneInCorso                  = "Model/Drivers/DB92/DB92_ProduzioneInCorso";
    public const string PathDB92_PezziDepositati                    = "Model/Drivers/DB92/DB92_PezziDepositati";
    public const string PathDB92_PezziScarti                        = "Model/Drivers/DB92/DB92_PezziScarti";
    public const string PathDB92_QtaRiordino                        = "Model/Drivers/DB92/DB92_QtaRiordino";
    public const string PathDB92_InviaProgrammaAPressa              = "Model/Drivers/DB92/DB92_InviaProgrammaAPressa";

    //ricetta
    public const string PathDB91RicettaProduct_ID                   = "Model/Drivers/DB91/Ricetta/Product_ID";
    public const string PathDB91RicettaDescrizione                  = "Model/Drivers/DB91/Ricetta/Descrizione";
    public const string PathDB91RicettaProgrammaRobot               = "Model/Drivers/DB91/Ricetta/ProgrammaRobot";

    //cliente_to_rea -> produzione
    public const string PathQueryProduzione                         = "Model/Produzione/QueryProduzione";
    public const string PathProduzioneFilterActive                  = "Model/Produzione/FiltroProduzione/FilterActive";
    public const string PathProduzioneTextFilter                    = "Model/Produzione/FiltroProduzione/TextFilter";
    public const string PathProduzioneAvviaEnabled                  = "Model/Produzione/ButtonProduzioneEnabled/AvviaEnabled";
    public const string PathProduzioneEliminaEnabled                = "Model/Produzione/ButtonProduzioneEnabled/EliminaEnabled";
    public const string PathProduzioneTerminaEnabled                = "Model/Produzione/ButtonProduzioneEnabled/TerminaEnabled";
    public const string PathProduzioneNuovaProduzioneOdp            = "Model/Produzione/NuovaProduzione/Odp";
    public const string PathProduzioneNuovaProduzioneNomeArticolo   = "Model/Produzione/NuovaProduzione/NomeArticolo";
    public const string PathProduzioneInCorso                       = "Model/Produzione/ProduzioneInCorso";

    //rea_to_cliente -> storico
    public const string PathQueryStorico                            = "Model/Storico/QueryStorico";
    public const string PathStoricobdateFilter                      = "Model/Storico/bdateFilter";
    public const string PathStoricoDateFrom                         = "Model/Storico/DateFrom";
    public const string PathStoricoDateTo                           = "Model/Storico/DateTo";

    //anagrafica
    public const string PathQueryAnagrafica                         = "Model/Anagrafica/QueryAnagrafica";
    public const string PathAnagraficaFilterActive                  = "Model/Anagrafica/FiltroAnagrafica/FilterActive";
    public const string PathAnagraficaTextFilter                    = "Model/Anagrafica/FiltroAnagrafica/TextFilter";
    public const string PathAnagraficabInInsertMode                 = "Model/Anagrafica/ButtonAnagraficaEnabled/bInInsertMode";
    public const string PathAnagraficabInEditMode                   = "Model/Anagrafica/ButtonAnagraficaEnabled/bInEditMode";
    public const string PathrowIdAnagraficaSelected                 = "Model/Anagrafica/rowIdAnagraficaSelected";
    public const string PathAnagraficaSalvaEnabled                  = "Model/Anagrafica/ButtonAnagraficaEnabled/SalvaEnabled";
    public const string PathAnagraficaAnnullaEnabled                = "Model/Anagrafica/ButtonAnagraficaEnabled/AnnullaEnabled";
    public const string PathAnagraficaEliminaEnabled                = "Model/Anagrafica/ButtonAnagraficaEnabled/EliminaEnabled";
    public const string PathAnagraficaModificaEnabled               = "Model/Anagrafica/ButtonAnagraficaEnabled/ModificaEnabled";
    public const string PathAnagraficaProdotto                      = "Model/Anagrafica/Prodotto";
    public const string PathAnagraficaProdottoID                    = "Model/Anagrafica/Prodotto/ID";
    public const string PathAnagraficaProdottoProduct_ID            = "Model/Anagrafica/Prodotto/Product_ID";
    public const string PathAnagraficaProdottoDescr                 = "Model/Anagrafica/Prodotto/Descr";
    public const string PathAnagraficaProdottoRobot_Program         = "Model/Anagrafica/Prodotto/Robot_Program";

    //popup_selezionaprogramma
    public const string Pathsp_ProgramSelected                      = "Model/popup_selezionaprogramma/sp_ProgramSelected";
    public const string Pathsp_ProgramList                          = "Model/popup_selezionaprogramma/sp_ProgramList";

    public const string Path_DBEXpress                              = "Model/DB";
    public const string Path_PressaError                            = "Model/PressaError";
    public const string Path_sPressProgramName                      = "Model/Configurazioni/FTPpressa/ftp_sPressProgramName";
    public const string Path_sPressGroup                            = "Model/Configurazioni/FTPpressa/ftp_sPressGroup";
    public const string Path_config_pressPathIn                     = "Model/Configurazioni/FTPpressa/ftp_config_pressPathIn";
    public const string Path_config_pressPathOut                    = "Model/Configurazioni/FTPpressa/ftp_config_pressPathOut";
    public const string Path_config_pressExt                        = "Model/Configurazioni/FTPpressa/ftp_pressExt";
    public const string Path_config_pressUser                       = "Model/Configurazioni/FTPpressa/ftp_pressUser";
    public const string Path_config_pressPswd                       = "Model/Configurazioni/FTPpressa/ftp_pressPswd";

    public const string Path_Mem_PezziDepositati                    = "Model/Mem_PezziDepositati";
    public const string Path_Mem_PezziScarti                        = "Model/Mem_PezziScarti";
    public const string Path_Mem_QtaRiordino                        = "Model/Mem_QtaRiordino";
    public const string Path_ExtraProduzione                        = "Model/Drivers/Extra_Produzione";
    public const string Path_QuantityExtraProduction                = "Model/Produzione/Quantity_ExtraProduction";

    public const string Path_LedSQLite                              = "Model/TestConnection/LedSQLite";
    public const string Path_LedSQLexpress                          = "Model/TestConnection/LedSQLexpress";

    public const string Path_FolderSelected                         = "Model/Configurazioni/FolderSelection";
    public const string Path_PathFolderTemp                         = "Model/SelectPdf/SelectedPath";

    public const string Path_ftp_NomeCartella                       = "Model/Configurazioni/FTPRobot/ftp_NomeCartella";
    public const string Path_ftp_IndirizzoIP                        = "Model/Configurazioni/FTPRobot/ftp_IndirizzoIP";
}
