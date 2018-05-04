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

# Using Helm for Configuration Values
Helm leverages configmaps in order to define configuration parameters. These values must be manually configured in the values.yaml file. The steps to obtain these values are shown below.

## Values to be Changed
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
