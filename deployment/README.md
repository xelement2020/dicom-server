# Deployment Templates

This directory contains Azure Resource Manager JSON Deployment Templates for the Dicom Server and Azure Bulk Importer Function.

## How to use the Templates

To deploy, create a new resource group on Azure. From within the resource group select 'Add new resource'.

This will take you to the Azure marketplace where you can search for and select `Template deployment (deploy using custom templates)`.

Next select `Create` followed by `Build your own template in the editor`.

Then you can copy the JSON code for one of the templates into the editor.

Note: In both JSON templates there are comments which will give red errors in the code editor. Make sure you remove these comments. They contain code to deploy from the repositories, but this will not work until the repositories are made public. For now you'll need to deploy the code manually.

Select 'Save', enter settings information (see details below), agree to the terms and conditions, and `purchase`.

Once deployed you'll need to actually deploy the source code.

### Dicom Server Deployment Template

To deploy the Dicom Server complete the steps above using the JSON from the `dicom-server-deployment.json` file.  

The template covers the following:  
- Creates App Service Plan  
- Creates the Web App for the Dicom Server  
- Creates CosmosDb database  
- Creates Storage Account for the Blob Storage Containers  
- Sets up Application Insights  
- Adds KeyVault Store which the Web App can access  
- Adds blob and cosmos connection strings to the KeyVault Store

When entering the settings for the Dicom Server, you'll need to enter a 'Service Name' which will be used in the naming of a few of the resources above.

Also, note that the resource templates do not create the Blob Storage containers or create the CosmosDb database and set up the partitioning. This is intentional, as the server is meant to handle this.

Once you've deployed the template, you'll need to actually deploy the dicom server (since the template can't deploy code from private repositories). The easiest way is to open the solution in Visual Studio, and right click on the `Microsoft.Health.Dicom.Web` project in the Solution Explorer. Select `Publish` from the contect menu.  
Use the `Select Existing` option and you can specify the Azure Web App you want to deploy to (make sure you are logged into your Microsoft Account).

### Bulk Importer Azure Function Deployment Template

The Bulk Importer Azure Function can be found in branch `personal/james-clements/dicom-bulk-importer-azure-function'. It has a documentation file [here](https://github.com/microsoft/dicom-server/tree/personal/james-clements/dicom-bulk-importer-azure-function/samples).

To deploy the Azure Function complete the steps above using the JSON from the `importer-function-deployment.json` file.  

In the template settings you'll need to enter the `Dicom Server URL`. This is the URL which your Dicom Server Web App got deployed to. If you select the server's Web App resource in your resource group, the url will be in the top right corner.
It should look something like `https://servicename.azurewebsites.net`

Again, you'll need to deploy the code separately. This can be done in Visual Studio. Checkout the branch `personal/james-clements/dicom-bulk-importer-azure-function` and open the solution in Visual Studio. In the Solution Explorer right click on the `DicomImporter` project in the `samples` folder. Select 'Publish', 'Select Existing' and find the Azure Function to deploy to from your resource group.

### Deployment Buttons

The button below can be used to deploy the Fhir Server to Azure via a deployment template. For Azure to load the template correctly, it needs to be publically accessible so it cannot be done with the templates in this repository until after the repository is made public.

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FMicrosoft%2Ffhir-server%2Fmaster%2Fsamples%2Ftemplates%2Fdefault-azuredeploy.json" target="_blank">
    <img src="https://azuredeploy.net/deploybutton.png"/>
</a>
