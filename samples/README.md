# Dicom Server Samples 

## Dicom Bulk Importer Azure Function

The Bulk Importer is an Azure Function which gets triggered when a file gets uploaded to a `'dicomimport'` blob container. The function checks the file is a valid Dicom file then attempts to post the file to a Store endpoint of the Dicom Server.

The aim of this project is to bring convenience to bulk importing dicom files and will be especially useful when testing the server. All you need to do is upload files to a blob container then check them again later.

The project also acts as a sample showing how the Store endpoint can be used.


If all files have been removed from the `'dicomimport'` container, it means they have been injested successfully. If a file was invalid, or there were other processing errors, the file gets transferred from `'dicomimport'` into a second container called `'dicomrejectedimports'`.

Currently it assumes there is no authentication required by the Dicom Server.

### Important

You should be aware that while testing locally you might encounter Storage Exceptions from the Dicom Server's Store endpoint. If these exceptions contain some 500 internal server errors from Blob Storage, this is a known bug with the Azure Storage Emulator.

The Azurite emulator doesn't have these problems, but it's currently not compatible with the Azure Functions Core Tools so will not work as an alternative.

### Deploying

You can deploy using the Azure Resource Manager Deployment Template which is temporarily on the branch `personal/james-clements/deployment-templates` with [documentation here](https://github.com/microsoft/dicom-server/tree/personal/james-clements/deployment-templates/deployment).

#### Deploying Manually

( *TODO:* These instuctions for deploying manually can be completed at a later date and used in documentation for the Dicom Server in a similar way to FHIR Server documentation )

The Azure Function can be deployed using the [Azure Portal](https://portal.azure.com/).

When deploying, the following environment variables should be set:  
```
AzureWebJobsStorage=<STORAGE ACCOUNT CONNECTION STRING>
DicomServerUrl=<URL OF DICOM SERVER>
```

Or alternatively you can use the Azure CLI for a single command line deployment of the container instance:
```
az container create --resource-group <RESOURCE GROUP NAME> --image reponame/dicomimporter --name dicomimporter1 --cpu 2 --memory 2 --environment-variables AzureWebJobsStorage='<STORAGE ACCOUNT CONNECTION STRING>' DicomServerUrl='<URL OF DICOM SERVER>' MaxDegreeOfParallelism=16
```

### Local Testing

To test the Azure Function locally you first need to be running the Dicom Server locally using the Azure Cosmos and Storage Emulators.

You also need to have the [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) installed, ideally version 2.7 or later.

You can change the environment variables listed above in the `localsettings.json` file. The default values should work out-of-the-box providing your Dicom Server instance is running on the default port `63837`.

You'll need a way of loading blobs into the Blob Store of the Storage Emulator. The easiest way to do this is using the [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/).

Using the Storage Explorer you can navigate to the Emulator's Blob Store and add a Blob Container called `'dicomimport'`. With the container selected you can upload files or an entire folder. The names of the files you upload do not matter.

You can run the Azure function in Visual Studio. Visual Studio will startup the Azure Functions Core Tools and open a log.

While running, the function will attempt to injest any dicom files in the `'dicomimport'` container of your Azure Emulator and any additional files which get added to the container.