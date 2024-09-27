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

public class AssociazioneVarModelloAPLC : BaseNetLogic
{
    [ExportMethod]
    public void Method1()
    {
        // Insert code to be executed by the method

        var modello = Project.Current.Get("Model").FindVariable("DB91_CambioProduzione");
        var varPLC = Project.Current.Get("CommDrivers/S7TIAPROFINETDriver2/S7TIAPROFINETStation1/Tags/51D20_PLC").FindVariable("b_Rcp_CambioProd"); 
        modello.SetDynamicLink(varPLC, DynamicLinkMode.ReadWrite);
       
    }
}
