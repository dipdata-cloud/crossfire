[![Build Status](https://dipdata.visualstudio.com/Pyros/_apis/build/status/Crossfire/Crossfire-Github?branchName=main&jobName=Build%20and%20Publish)](https://dipdata.visualstudio.com/Pyros/_build/latest?definitionId=34&branchName=main)
[![Coverage](https://img.shields.io/azure-devops/coverage/dipdata/Pyros/34?style=plastic)](https://img.shields.io/azure-devops/coverage/dipdata/Pyros/34?style=plastic)
# Introduction
Crossfire is an open-source message-queue, cache and request/response pre-process server for (various) data exploration backends. Currently supported: 
- Azure Analysis Services

Support for SQL and more advanced backends is planned for the future.
Crossfire provides a simple HTTP interface to send a query to a supported backend and receive a result in asynchronous manner via server-side push over websocket. Crossfire supports the following query languages and dialects:
- MDX

Support for SQL, DAX and other languages is planned for the future

# Features

Crossfire is a Web API application. All queries are accepted in a form of POST requests which are then compiled to one of the supported languages. Crossfire provides:

- Short-term response caching based on query text and target server. Thus, prod and non-prod servers will get separate caches
- OAUTH2.0 (`Bearer`) token-based authentication. A simple HMAC-based authentication is also available if needed. User identity is proxied to the connected backend
- Support for multiple active backends. You can run 10 different Azure AS instances all connected to a single Crossfire domain
- Processing of queries as background jobs via [Hangfire](https://www.hangfire.io/) library
- Convertion of backend-specific response, for example XMLA, to JSON
- Server-side result push via SignalR hub. Azure SignalR or load balancing is required to correctly run Crossfire on a horizontally scalable cluster. Various [SignalR Clients](https://docs.microsoft.com/en-us/aspnet/core/signalr/client-features?view=aspnetcore-5.0) can be used to receive query results from Crossfire
- Health checks on connected backend, for example launching a connected Azure AS instance

# Getting Started
Crossfire can be built for Linux or Windows OS. Under `Cloud/Azure/ci-build.yml` you can find an example of Azure DevOps pipeline that produces artifacts for Azure App Service. You can also simply use netcore CLI:
```shell
  dotnet publish --configuration Release --output /tmp/crossfire.zip --framework netcoreapp3.1 --runtime linux-x64
```

When deploying to Azure App Service using Code, you must modify the Web App `Startup command` like below:
```shell
apt-get update -y && apt-get install -y libsecret-1-0 && dotnet Crossfire.dll --urls "http://0.0.0.0:5000"
```

Any port that is different from Kestrel standard 5000 can be used.

Crossfire also relies on the following Azure services:

- Key Vault
- Azure Table Storage
- Azure Graph API

Hangfire library requires a storage. By default, Crossfire uses Azure SQL (SQL Server) storage, but any other storage supported by Hangfire can be used.

## Build and Test
In order to build simply do `dotnet build --configuration <Debug|Release> --framework netcoreapp3.1 --runtime <linux-x64|windows-x64|...>`

Testing requires a backend that supports Azure Table Storage: Storage Emulator, Azurite or Table Storage in the cloud, as well as a functioning Azure AS instance with Adventure Works sample model deployed there. Once these conditions are met, run `dotnet test --runtime <linux-x64|windows-x64|...> --collect:"XPlat Code Coverage"` to run all tests with a coverage report

## Real Life Usage

**DISCLAIMER**: Currently there is no way to use Crossfire as a proxy between Excel or PowerBI and actual server. This is not impossible to do, thus if you have time, please submit your idea in a PR/issue!

Crossfire fundamentally changes the way your client interacts with Azure AS:

- All queries become asynchronous
- No MSOLAP or other driver is required
- You submit queries over HTTP, but you receive results over WebSocket from a server-initiated push.

First you must deploy the application to Azure in order to use it. This can be done either by hand or by running `deploy.sh` script we included under `test/CrossfireXUnit/Resources`. Before doing anything, make sure that you have the following:

- Azure Resource Group to host app components
- Azure Subscription Id that will run all app components
- Azure Tenant Id
- Client Application or Azure AD user account that will be used to deploy app components. It must have contributor rights on your Resource Group and Subscription

Now, either by hand or via `deploy.sh` create resources listed below. You might want to reuse ones you already have active in your cloud:

- Azure Key Vault
- Azure Storage Account
- Azure Analysis Services (this is only if you do not have any real ones or are creating infra for tests)
- Azure SignalR
- App Service Plan with B1 tier minimum
- Web App on the App Service Plan above

Running `deploy.sh` requires Azure CLI to be installed on your system. If you are running from Windows, use Git Bash binary usually found under `C:\Program Files\Git\bin` to run this script.

Some resources are not created by `deploy.sh`, add them manually:

- Azure SQL Database (Basic tier is enough to try)

Now you have resources ready, a few configuration steps:

- Enable System Assigned Identity in **Identity** blade of your Web App.

Once the identity is provisioned, retrieve the object id from Azure AD using App Registrations Blade and your Web App name. Create an access policy in Key Vault for it:

```shell
  az keyvault set-policy -n MyVault --secret-permissions get list --object-id {GUID}
```

Add a new app registration to Azure AD that will be used by your Web App to access Azure AS:

```shell
 az ad app create --display-name 'crossfire-aas' --available-to-other-tenants false --credential-description 'crossfire-password' --password '<secure password>' --native-app false
```

Note the application id of a newly created app. Connect to your Azure AS instance and add this app as instance administrator by supplying the following username:

```
 app:<application id>@<tenant id>
```

Finally, create two cache tables in your Azure Storage Table account:

- CrossfireTokenStore
- CrossfireConnectionInfoStore

You only need to create tables themselves.

If you made it this far, you are almost ready to go. Go to your Web App and edit it's config to override `appsettings.json`. You can use the one below as an example:

```
[
  {
    "name": "ApplicationInsights__InstrumentationKey",
    "value": "<some key>",
    "slotSetting": true
  },
  {
    "name": "Authentication__AzureAdClientId",
    "value": "<Application id in Azure AD of your client application that wants to use Crossfire>",
    "slotSetting": true
  },
  {
    "name": "Authentication__AzureAdPolicy",
    "value": "<If you use Azure B2C. For normal Azure AD tenants just set this to oauth2>",
    "slotSetting": true
  },
  {
    "name": "Authentication__AzureAdTenant",
    "value": "<Azure AD tenant name like myorg.onmicrosoft.com>. If you use custom domain names, use the Primary one here",
    "slotSetting": true
  },
  {
    "name": "Authentication__AzureGraphApiTenant",
    "value": "<Azure AD tenant name like myorg.onmicrosoft.com>",
    "slotSetting": true
  },
  {
    "name": "Authentication__AzureGraphClientId",
    "value": "<Application id used to access Azure Graph>. Simply grant Graph permissions to read user profiles to Crossfire application id created on the previous step",
    "slotSetting": true
  },
  {
    "name": "Authentication__AzureSignalREndpoint",
    "value": "https://<my-app>.service.signalr.net",
    "slotSetting": true
  },
  {
    "name": "Authentication__AzureSignalRKey",
    "value": "<Your key must be stored in Azure Key Vault. Only put a secret name here>",
    "slotSetting": true
  },
  {
    "name": "Authentication__KeyVaultUri",
    "value": "https://<my-keyvault>.vault.azure.net/",
    "slotSetting": true
  },
  {
    "name": "Authentication__Storage",
    "value": "<Key Vault secret name that contains Azure Storage connection string>",
    "slotSetting": true
  },
  {
    "name": "ConnectedServers__0__ApplicationId",
    "value": "<Application id of an Azure AS admin created in the previous step>",
    "slotSetting": true
  },
  {
    "name": "ConnectedServers__0__Region",
    "value": "<Azure AS Server region>",
    "slotSetting": true
  },
  {
    "name": "ConnectedServers__0__ResourceGroup",
    "value": "<Azure AS Server resource group>",
    "slotSetting": true
  },
  {
    "name": "ConnectedServers__0__ServerName",
    "value": "<Azure AS Server name>",
    "slotSetting": true
  },
  {
    "name": "ConnectedServers__0__ServerServiceUri",
    "value": "asazure://<region>.asazure.windows.net/<server>",
    "slotSetting": true
  },
  {
    "name": "ConnectedServers__0__ServerUri",
    "value": "https://<region>.asazure.windows.net",
    "slotSetting": true
  },
  {
    "name": "ConnectedServers__0__SubscriptionId",
    "value": "<Azure Subscription id that hosts Azure AS server>",
    "slotSetting": true
  },
  {
    "name": "ConnectedServers__0__TenantId",
    "value": "<Azure AD tenant that hosts Azure AS Server>",
    "slotSetting": true
  },
  // if more than one, add them here
  {
    "name": "ConnectedServers__1__...",
    ...
  },
  // servers defined
  {
    "name": "DistributedCache__ConnectionInfoStore",
    "value": "CrossfireConnectionInfoStore",
    "slotSetting": true
  },
  {
    "name": "DistributedCache__TokenStore",
    "value": "CrossfireTokenStore",
    "slotSetting": true
  },
  {
    "name": "Logging__LogLevel__Default",
    "value": "Information",
    "slotSetting": true
  },
  {
    "name": "PORT",
    "value": "5000",
    "slotSetting": false
  },
  {
    "name": "QueryProcessor__CacheMaxSize",
    "value": "1048576",
    "slotSetting": true
  },
  {
    "name": "QueryProcessor__HangfireWorkers",
    "value": "20",
    "slotSetting": true
  },
  {
    "name": "WEBSITE_HEALTHCHECK_MAXPINGFAILURES",
    "value": "10",
    "slotSetting": false
  },
  {
    "name": "WEBSITE_HTTPLOGGING_RETENTION_DAYS",
    "value": "2",
    "slotSetting": false
  }
]
```

Once your app is restarted, try to access `https://my-crossfire-installation.azurewebsites.net`. It should open a Swagger UI.

## Submitting queries

First, you should connect to a SignalR hub from your client application. SignalR has many client libraries, below is a simple example for typescript/javascript.

```typescript
import * as signalR from '@aspnet/signalr';

@Injectable()
export class CrossfireSampleService {
  ...

  // define connection
  private connection: signalR.HubConnection;

  // define unique connection id you will receive from Crossfire
  private clientUniqueIdentifier: string;
  constructor(

     // your application config
     @Inject(AppConfig) private config: EnvironmentConfig,

     // your frontend id/access token provider
     private tokenService: SampleIdTokenService
     ...
  ) {

    // create singlalR hub connection
    // JS client does not support custom headers
    // this.config.crossfireSignalrEndpoint is https://my-crossfire-installation.azurewebsites.net/<hubName>
    // <hubName> is modelMessages by default
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.config.crossfireSignalrEndpoint, {
        accessTokenFactory: () => tokenService.toPromise()
      })
      .build();
  }

  // connection start helper
  private startConnection(userId: string, startCallback: () => void) {
    
    // first activate this SignalR connection
    this.connection.start()
      .then(() => {

        // join service channel and receive a unique id to be used with all requests
        this.connection.invoke('JoinServiceChannelJsClient', userId);
      })
      .then(() => startCallback)
      .catch(err => {
        // any error handler
        ...
      });
  }

  // this notifies the hub that we would like to receive query results for the specified Azure user
  private joinQueryChannel(userId: string): void {
    this.connection.invoke('JoinModelQueryChannelJsClient', userId, this.clientUniqueIdentifier);
  }

  // connection initializer - to be called when your app is loaded in a browser tab
  public initializeHubConnection(user: MyAzureUser) {

    // register events
    // upon receiving a clientId from Crossfire, join channels and get ready to shoot
    this.connection.on(
      'clientIdentifier',
      (userPrincipalName, clientUid) => {
        this.clientUniqueIdentifier = clientUid;
        this.joinQueryChannel(userPrincipalName);
      });
    });

    // register error message handlers
    this.connection.on('errorMessage', (errorMessage: any) => {
      console.log({ severity: 'error', summary: 'Error executing request', detail: errorMessage.payload });
    });

    this.connection.onclose(() => {

      // any handlers you need in case connection was dropped
      ...
    });
    this.startConnection(user.azureUserId, null);
  }

  // use this method in your view to activate receiving messages back from Crossfire
  public registerQueryHandler(callerId: string, handler: (response) => void) {
    console.log($`Registering Crossfire query handler from {callerId}`);
    this.connection.on('queryResult', handler);
  }

  // use this method in your view when it is closed/destroyed to deactivate receiving messages
  public unregisterQueryHandler(callerId: string, handler: (response) => void) {
    console.log($`Removing Crossfire query handler from {callerId}`);
    this.connection.off('queryResult', handler);
  }
}
```

Once you have the service setup, it can be used in your apps views/components. Example below is for Angular:

```typescript
export class ViewWithCrossfireComponent implements OnInit, OnDestroy {

  constructor(
    private crossfireService: CrossfireService,
    private zone: NgZone,
  ) {
  }

  // cleanup handler trigger
  ngOnDestroy(): void {
    this.crossfireService.unregisterMessageHandler('my-view-selector', 'QUERY_RESPONSE_MESSAGE', this.crossfireQueryHandler);
  }

  // register handler
  ngOnInit(): void {
    this.crossfireService.registerMessageHandler('my-view-selector', 'QUERY_RESPONSE_MESSAGE', this.crossfireQueryHandler);
    ...
  }

  private crossfireQueryHandler = (response: any) => {
    this.zone.run(() => {

      // process query response from Crossfire here
      ...
    });
  };

}
```

Now you can POST queries to an http endpoint `https://my-crossfire-installation.azurewebsites.net/model/query` and `crossfireQueryHandler` will trigger once data is returned to you. 

Remember that when constructing a `QueryRequest` you need to provide a `uniqueClientIdentifier` received from SignalR hub on connection start:

```json
{
  TargetDatabase = "adventureworks",
  Region = "my-azure-as-region",
  ResourceGroup = "my-azure-as-resource-group",
  TargetServer = "my-azure-as-server",
  UniqueClientIdentifier = this.crossfireService.clientUniqueIdentifier,
  CompilationTarget = "MDX",
  OutputFormat = 0,
  QueryValues = [ "[Measures].[Internet Total Sales]" ],
  QuerySlices = [ "[Date].[Fiscal Year].[All].children" ],
  RequestMetadata = "Arbitrary payload to pass along your request. Can be used to identify a response on a client, or for any other purpose",
}
```

## FAQ

- Q: Can Crossfire accept free text queries?
- A: Yes, we just didn't add an endpoint for this. Submit a PR, we'll happily integrate it

- Q: This SignalR stuff is really a lot of work. Can't I just go with simply receiving results via HTTP?
- A: You can, but there is a serious downside to that approach. SignalR allows Crossfire to _recieve_ a query and _asynchronously_ send you a result. Since queries are backed by Hangfire queues, there can be some waiting time associated with them, and you will hold a connection while waiting for HTTP to return results.
This is not very scalable once you start getting hundreds and thousands of requests. Important bit here is, you can generate quite a lot of queries from a single big dashboard with a lot of visuals, and if you load a dozen of those in different tabs,
there is a high chance your Azure AS will drop dead. Then you have an option of upscaling the server farm by adding more instances, but in the end your Azure AS will die anyway, so you'll end up with a very expensive upscaling of Azure AS as well. Crossfire allows you to use _several tiers_ lower instance than your normal production one
thanks to Hangfire queues and SignalR pushes. Keep in mind that data transfer over SignalR is usually more efficient that over raw HTTP, especially due to the way Azure SignalR handles big messages, splitting them into smaller chunks and thus ensuring your browser will not choke when receiving results.

- Q: Can I use PHP/NET/Ruby/etc clients instead of Javascript?
- A: Yes, as long as there is such a client. There are 3 official ones: .NET, Java and Javascript. https://docs.microsoft.com/en-us/aspnet/core/signalr/client-features?view=aspnetcore-5.0

- Q: Why even bother using Crossfire, when I have PowerBI?
- A: Crossfire does not replace PowerBI and is not aiming to do that. Crossfire is designed to help people (like ourselves), who use Azure AS directly and not via PowerBI. The most common use case for Crossfire is your organization having its own frontend over Azure AS, or
any frontend apps that may need to interact with Azure AS. We are, however, looking into options of making Crossfire a data proxy for various data processing engines. First thing that comes to mind, is a PowerBI connector to Azure AS that utilizes Crossfire. But this is not decided anywhere as of now.

- Q: Can Crossfire speed up my hanging Excel worksheets?
- A: Not without an Office plugin (we think). Due to SignalR and Websockets in general still being rare beasts in most popular enterprise applications like Excel, only way to make this work with current architecture is to write a specialized plugin/connection driver. If you disagree, please tell us!

- Q: I can't make this work. Could you help?
- A: Yes, we can help. Ideally submit and _Issue_ describing your problem: startup errors, application errors and where exactly you got stuck. We would be glad to make this work for you
