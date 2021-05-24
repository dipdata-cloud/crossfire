#!/bin/bash

set -o errexit   # abort on nonzero exitstatus
set -o nounset   # abort on unbound variable
set -o pipefail  # don't hide errors within pipes

usage() {
  echo "Usage: $0 -grstca --resource-group --region --subscription --tenant --deploy-agent-client-id --deploy-agent-secret" 
  echo "       $0 -g --resouce-group <Azure resource group>" 
  echo "       $0 -r --region <Azure region>" 
  echo "       $0 -s --subscription <Subscription to deploy>" 
  echo "       $0 -t --tenant <Tenant name>" 
  echo "       $0 -a --deploy-agent-client-id <agent id>" 
  echo "       $0 -c --deploy-agent--secret <secert> or environment variable DEPLOY_AGENT_SECRET" 
  exit 1
}


PARSED_ARGUMENTS=$(getopt -o grstcs --long "resource-group:,region:,subscription:,tenant:,deploy-agent-client-id:,deploy-agent-secret:" -- "$@")

VALID_ARGUMENTS=$?
if [[ "$VALID_ARGUMENTS" != "0" ]];
then
  usage
fi;

eval set -- "$PARSED_ARGUMENTS"
while :
do
  case "$1" in
    --resource-group)           APP_RESOURCE_GROUP=$2;       shift  2 ;;
    --region)                   APP_REGION="$2";             shift  2 ;;
    --subscription)             APP_SUBSCRIPTION="$2";       shift 2 ;;
    --tenant)                   APP_TENANT="$2";             shift 2 ;;
    --deploy-agent-client-id)   DEPLOY_AGENT_CLIENT_ID="$2"; shift 2 ;;
    --deploy-agent-secret)      DEPLOY_AGENT_SECRET="$2";    shift 2 ;;
    # -- means the end of the arguments; drop this, and break out of the while loop
    --) shift; break ;;
    # Some error occured in getopt. We already checked VALID_ARGUMENTS above and 
    # it should neven happed
    *) echo "Failed to parse unexpected option $1"
       usage ;;
  esac
done

if [[ -z "$APP_RESOURCE_GROUP" ]] \
  || [[ -z "$APP_REGION" ]] \
  || [[ -z "$APP_SUBSCRIPTION" ]] \
  || [[ -z "$APP_TENANT" ]] \
  || [[ -z "$DEPLOY_AGENT_CLIENT_ID" ]] \
  || [[ -z "$DEPLOY_AGENT_SECRET" ]];
then
  usage;
fi;

echo 'Logging to Azure...'
# Login to the cloud
az login --service-principal -u "$DEPLOY_AGENT_CLIENT_ID" -p "$DEPLOY_AGENT_SECRET" --tenant "$APP_TENANT"

echo "Setting subscription to $APP_SUBSCRIPTION..."
# Select subscription
az account set --subscription "$APP_SUBSCRIPTION"

echo "Provisioning resource group $APP_RESOURCE_GROUP..."
if ! az group exists -n "$APP_RESOURCE_GROUP" > /dev/null; 
then
  # Create test resource group
  az group create \
   --name "$APP_RESOURCE_GROUP" \
   --location "$APP_REGION"
  
  echo "$APP_RESOURCE_GROUP ready!"
else
  echo "$APP_RESOURCE_GROUP already exists. Skipping creation."
fi

# Deploy Key Vault

echo 'Provisioning Key Vault...'

az deployment group create \
  --name crossfireRolloutKV \
  --resource-group "$APP_RESOURCE_GROUP" \
  --template-file "./KeyVault/template.json" \
  --parameters "./KeyVault/parameters.json"

# Deploy Azure Storage

echo 'Provisioning Azure Storage Account...'

az deployment group create \
  --name crossfireRolloutAzureStorage \
  --resource-group "$APP_RESOURCE_GROUP" \
  --template-file "./StorageAccount/template.json"

# Deploy AzureAS

echo 'Provisioning AzureAnalysis Services...'

az deployment group create \
  --name crossfireRolloutAzureAS \
  --resource-group "$APP_RESOURCE_GROUP" \
  --template-file "./AnalysisServices/template.json" \
  --parameters "./AnalysisServices/parameters.json"

# Deploy AzureSignalR

echo 'Provisioning AzureSignalR...'

az deployment group create \
  --name crossfireRolloutAzureSignalR \
  --resource-group "$APP_RESOURCE_GROUP" \
  --template-file "./SignalR/template.json"

# Deploy Crossfire

echo 'Creating App Service Plan...'

az deployment group create \
  --name crossfireRolloutFarm \
  --resource-group "$APP_RESOURCE_GROUP" \
  --template-file "./CrossfireAppService/template.json" \
  --parameters "./CrossfireAppService/parameters.json"

echo 'Creating Crossfire...'

az deployment group create \
  --name crossfireRolloutApp \
  --resource-group "$APP_RESOURCE_GROUP" \
  --template-file "./Crossfire/template.json" \
  --parameters "./Crossfire/parameters.json"

echo 'Done!'

echo 'Prior to deployment, enable Managed Identity for Crossfire and give it access to Key Vault and Azure AS instance'
