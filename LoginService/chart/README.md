# Containerizer Microservice Pipeline Service

# Install Helm Client (helm)
1. Run ```brew install kubernetes-helm```

[Installation Details for Windows and Linux](https://docs.helm.sh/using_helm/#installing-helm)

# Install Helm Server (Tiller)
1. Run ```helm init```

    This will validate that helm is correctly installed on your local environment and the cluster ```kubectl``` is connected to.
2. Run ```kubectl get pods --namespace kube-system``` to validate tiller is running

```
NAME                                            READY     STATUS    RESTARTS   AGE
...
tiller-deploy-677436516-cq73w                   1/1       Running   0          21h
...
```

# Update Helm Values
Certain configuration values must be manually added to the [values.yaml](values.yaml) file so that helm can deploy the deployments, services, and configmaps properly. The steps to obtain these values are shown below.

## App-specific values
These values should be changed in the app section:

**image.repository**
**image.imageName**
**image.tag**
**imagePullSecrets**

## Hexadite values
These values should be changed in the hexadite section:

**image.repository**
**keyVault**

## Configmap values
This service uses Kubernetes configmaps in order to define configuration parameters. The following variables should be changed in the configmap section:

## Configmap values
These values should be changed in the configs section of the [values.yaml](values.yaml) file.

**aadAppId**
1. From App registrations in Azure Active Directory, select your application.
2. In the Overview blade, the Application ID is the value you need.
  
**appInsightsKey**
1. From your "Application Insights" resource, click on the overview blade, and use the Instrumentation Key.
  
**omsWorkspaceId**
1. Run the following command in your command prompt, and the output is your ID:
  
```
WSID=$(az resource show --resource-group loganalyticsrg --resource-type Microsoft.OperationalInsights/workspaces --name containerized-loganalyticsWS | grep customerId | sed -e 's/.*://')
```

**secretsVaultUrl**
1. Navigate to your Key Vault in the Azure portal, and on the overview blade, copy the "DNS Name", and this is your secrets vault URL.

# Deploy App Helm Chart onto Cluster
1. Ensure Azure Container Registry Credentials are deployed onto your cluster
2. Install the helm chart
    ```
    helm install . -n <app-chart-name>
    ```
    **Note**: Use the relative path if you are running from the root directory.
    ```
    helm install /LoginService/chart -n login-service
    ```
