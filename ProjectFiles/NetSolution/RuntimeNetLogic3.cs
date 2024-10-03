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
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.Arm;
using System.Collections.Generic;
#endregion

public class RuntimeNetLogic3 : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private const string DATASTORE_DATABASE = "DataStores/EmbeddedDatabase1";
    private const string TABLE_NAME = "RecipeSchema2";

    [ExportMethod]
    public void prova()
    {
        var myStore = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
        Object[,] ResultSet;
        String[] Header;

        // Esegui la query per ottenere la definizione della tabella
        myStore.Query("SELECT sql FROM sqlite_master WHERE type='table' AND name='RecipeSchema2'", out Header, out ResultSet);

        // Verifica se ci sono risultati
        if (ResultSet.GetLength(0) > 0)
        {
            // Recupera la definizione della tabella
            string tableDefinition = ResultSet[0, 0].ToString();

            // Dividi la definizione per righe
            string[] lines = tableDefinition.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Lista per memorizzare i nomi delle colonne
            List<string> columnNames = new List<string>();

            // Itera sulle righe della definizione
            foreach (var line in lines)
            {
                // Cerca le righe che rappresentano una colonna (quelle che contengono il tipo di dato, come TEXT, INTEGER, etc.)
                if (line.Contains("TEXT") || line.Contains("INTEGER") || line.Contains("DATETIME"))
                {
                    // Il nome della colonna è la prima parte della riga prima del tipo di dato
                    var parts = line.Split(new[] { ' ', '\t', '\"' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        // Aggiungi il nome della colonna alla lista
                        columnNames.Add(parts[0]);
                    }
                }
            }

            // Stampa i nomi delle colonne
            foreach (var columnName in columnNames)
            {
                Console.WriteLine($"Nome colonna: {columnName}");
            }
        }
    }

}
