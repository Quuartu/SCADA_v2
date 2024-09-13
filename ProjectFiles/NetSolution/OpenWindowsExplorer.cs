#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Recipe;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Modbus;
using FTOptix.CommunicationDriver;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.Diagnostics;
#endregion

public class OpenWindowsExplorer : BaseNetLogic
{

    [ExportMethod]
    public void OpenExplorerToPath(string PathWithFilename)
    {
        string PathWithFileName_URI = new ResourceUri(PathWithFilename).Uri;
        string PathWithoutFilename = PathWithFileName_URI.Substring(0, PathWithFileName_URI.LastIndexOf("\\"));       
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = PathWithoutFilename,
            UseShellExecute = true,
            Verb = "open"
        });
        
    }
}
