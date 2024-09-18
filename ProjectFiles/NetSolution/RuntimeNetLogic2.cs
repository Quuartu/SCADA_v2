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

public class RuntimeNetLogic2 : BaseNetLogic
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
    public void GetValue(string Odp)
    {
        // Get the Database from the current project
        var myStore = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
        // Create the output to get the result (mandatory)
        Object[,] ResultSet;
        String[] Header;
        // Perform the query
        myStore.Query("SELECT * FROM RecipeSchema2 WHERE Name = '" + Odp + "'", out Header, out ResultSet);
        //Applico Production Order
        Project.Current.GetVariable("Recipes/RecipeSchema2/EditModel/Odp").Value = Odp;
    }
}
