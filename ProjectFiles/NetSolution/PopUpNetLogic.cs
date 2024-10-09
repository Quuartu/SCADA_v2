#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.S7TiaProfinet;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.SerialPort;
using FTOptix.System;
using System.Threading.Tasks;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Recipe;
using FTOptix.Report;
#endregion

public class PopUpNetLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    public void OpenPopUp(string messaggio_popup, int popupType)
    {
        //messaggio popup
        var messaggio = Project.Current.GetVariable(VariablePaths.PathTesto_PopUp);
        messaggio.Value = messaggio_popup;

        //configuro impostazioni per il popup desiderato
        switch (popupType)
        {
            //OK
            case 0:

                //nascondi pulsanti yes/no e mostra pulsante ok
                Project.Current.Get<Button>(VariablePaths.PathPopupButtonOK).Visible = true;
                Project.Current.Get<Button>(VariablePaths.PathPopupButtonYes).Visible = false;
                Project.Current.Get<Button>(VariablePaths.PathPopupButtonNo).Visible = false;

                break;

            //YES_NO
            case 4:

                //nascondi pulsante ok e mostra pulsante yes/no
                Project.Current.Get<Button>(VariablePaths.PathPopupButtonOK).Visible = false;
                Project.Current.Get<Button>(VariablePaths.PathPopupButtonYes).Visible = true;
                Project.Current.Get<Button>(VariablePaths.PathPopupButtonNo).Visible = true;

                break;

            default:

                break;
        }

        var open = Project.Current.GetVariable(VariablePaths.PathOpenPopup);
        open.Value = true;
    }

    public static async Task WaitForConditionAsync()
    {
        var popupYes = Project.Current.GetVariable(VariablePaths.PathPopupYes);
        var popupNo = Project.Current.GetVariable(VariablePaths.PathPopupNo);
        var popupOK = Project.Current.GetVariable(VariablePaths.PathPopupOK);

        while (!popupNo.Value && !popupYes.Value && !popupOK.Value)
        {
            // Attesa non bloccante per tot millisecondi
            await Task.Delay(200);
        }
    }
}
