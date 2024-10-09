#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.DataLogger;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.Core;
#endregion

public class FileSelectorButtonsLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void ExecuteCallback()
    {
        var fileSelectorDialog = Owner.Owner.Owner.Owner as FTOptix.UI.Dialog;
        Owner.Owner.Owner.Owner.GetVariable("FullPath").Value = Owner.Owner.Owner.GetVariable("TmpFile").Value;
        fileSelectorDialog.Get<MethodInvocation>("FileSelectedCallback").Invoke();
        fileSelectorDialog.Close();
    }
}
