#region Using directives
using FTOptix.Core;
using UAManagedCore;
using FTOptix.NetLogic;
using System.IO;
using System;
using FTOptix.UI;
using FTOptix.HMIProject;
using FilesystemBrowserHelper;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.Report;
#endregion

public class FileSystemBrowserDataGridLogic : BaseNetLogic
{
	public override void Start()
	{
		IUAVariable accessFullFilesystemVariable;

		pathVariable = Owner.Owner.Owner.Owner.GetVariable("StartingPath");
		if (pathVariable == null)
			throw new CoreConfigurationException("StartingPath variable not found in FilesystemBrowser");

		tmpFilePathVariable = Owner.Owner.Owner.GetVariable("TmpFile");
		if (tmpFilePathVariable == null)
			throw new CoreConfigurationException("TmpFile variable not found in FilesystemBrowser");

		accessFullFilesystemVariable = Owner.Owner.Owner.Owner.GetVariable("AccessFullFilesystem");
		if (accessFullFilesystemVariable == null)
			throw new CoreConfigurationException("AccessFullFilesystem variable not found");

		if (accessFullFilesystemVariable.Value && !PlatformConfigurationHelper.IsFreeNavigationSupported())
			return;

		locationsObject = Owner.Owner.GetObject("Locations");
		if (locationsObject == null)
			throw new CoreConfigurationException("Locations object not found");

		resourceUriHelper = new ResourceUriHelper(LogicObject.NodeId.NamespaceIndex)
		{
			LocationsObject = locationsObject
		};

		filesDatagrid = Owner as DataGrid;
		filesDatagrid.OnUserSelectionChanged += DataGrid_OnUserSelectionChanged;
	}

	public override void Stop()
	{
		filesDatagrid.OnUserSelectionChanged -= DataGrid_OnUserSelectionChanged;
	}

	private void DataGrid_OnUserSelectionChanged(object sender, UserSelectionChangedEvent e)
	{
		var selectedItemNodeId = e.SelectedItem;
		if (selectedItemNodeId == null || selectedItemNodeId.IsEmpty)
			return;

		var entry = (FileSelectorEntry)InformationModel.GetObject(selectedItemNodeId);
		if (entry == null)
			return;

		UpdateCurrentPath(entry.FileName);
	}

	private void UpdateCurrentPath(string lastPathToken)
	{
		// Necessary when FTOptixStudio placeholder path is configured with only %APPLICATIONDIR%\, %PROJECTDIR%\
		// i.e. at the start of the project
		var currentPath = resourceUriHelper.AddNamespacePrefixToFTOptixRuntimeFolder(pathVariable.Value);
		var currentPathResourceUri = new ResourceUri(currentPath);

		if (lastPathToken == "..")
			SetPathsToParentFolder(currentPathResourceUri);
		else
			SetPathsToSelectedFile(currentPathResourceUri, lastPathToken);
	}

	private void SetPathsToParentFolder(ResourceUri startingDirectoryResourceUri)
	{
		DirectoryInfo parentDirectory;
		try
		{
			parentDirectory = Directory.GetParent(startingDirectoryResourceUri.Uri);
		}
		catch (Exception exception)
		{
			Log.Error("FilesystemBrowserDatagridLogic", $"Unable to get parent folder: {exception.Message}");
			return;
		}

		if (parentDirectory == null)
			return;

		var parentDirectoryPath = parentDirectory.FullName;

		// E.g. %PROJECTDIR%/PKI
		pathVariable.Value = resourceUriHelper.GetFTOptixStudioFormattedPath(startingDirectoryResourceUri,
																	   parentDirectoryPath);

		tmpFilePathVariable.Value = ResourceUri.FromAbsoluteFilePath(parentDirectoryPath);
	}

	private void SetPathsToSelectedFile(ResourceUri currentDirectoryResourceUri, string targetFile)
	{
		string updatedPath;
		try
		{
			updatedPath = Path.Combine(currentDirectoryResourceUri.Uri, targetFile);
		}
		catch (Exception exception)
		{
			Log.Error("FilesystemBrowserDatagridLogic", $"Path not found {exception.Message}");
			return;
		}

		tmpFilePathVariable.Value = ResourceUri.FromAbsoluteFilePath(updatedPath);

		if (!IsDirectory(updatedPath))
			return;

		// E.g. %PROJECTDIR%/PKI
		pathVariable.Value = resourceUriHelper.GetFTOptixStudioFormattedPath(currentDirectoryResourceUri,
																	   updatedPath);
	}

	private bool IsDirectory(string path)
	{
		return Directory.Exists(path);
	}

	private IUAVariable pathVariable;
	private IUAVariable tmpFilePathVariable;
	private DataGrid filesDatagrid;
	private IUAObject locationsObject;

	private ResourceUriHelper resourceUriHelper;
}
