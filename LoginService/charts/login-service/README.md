# Containerizer Microservice Pipeline Service

Deploy this service to Kubernetes using [Helm](https://helm.sh/).

## Install Helm Client (helm)

[Installation Details for Windows and Linux](https://docs.helm.sh/using_helm/#installing-helm)

## Install Helm Server (Tiller)

1. Run ```helm init --upgrade```

    This will validate that helm is correctly installed on your local environment and the cluster ```kubectl``` is connected to.
2. Run ```kubectl get pods --namespace kube-system``` to validate tiller is running

```bash
NAME                                            READY     STATUS    RESTARTS   AGE
...
tiller-deploy-677436516-cq73w                   1/1       Running   0          21h
...
```

## Update Helm Values

Certain values must be manually added to the [values.yaml](values.yaml) file so that helm can deploy the deployment, service, and configmap properly. The steps to obtain these values are shown below.

## App-specific values

These values should be changed in the ```app``` section:

- **app.image.repository**
- **app.image.imageName**
- **app.image.tag**
- **app.imagePullSecrets**

## Hexadite values

These values should be changed in the ```hexadite``` section:

- **hexadite.image.repository**
- **hexadite.keyVault**

## Configmap values

This service uses Kubernetes configmaps in order to define configuration parameters. The following variables should be changed in the ```configmap``` section of the [values.yaml](values.yaml) file:

**configmap.aadAppId**

1. From App registrations in Azure Active Directory, select your application.
2. In the Overview blade, the Application ID is the value you need.
  
**configmap.appInsightsKey**

1. From your "Application Insights" resource, click on the overview blade, and use the Instrumentation Key.
  
**configmap.omsWorkspaceId**

1. Run the following command in your command prompt, and the output is your ID:

```bash
WSID=$(az resource show --resource-group loganalyticsrg --resource-type Microsoft.OperationalInsights/workspaces --name containerized-loganalyticsWS | grep customerId | sed -e 's/.*://')
```

**configmap.secretsVaultUrl**

1. Navigate to your Key Vault in the Azure portal, and on the overview blade, copy the "DNS Name", and this is your secrets vault URL.

## Deploy Helm Chart onto Cluster

- Ensure Azure Container Registry Credentials are deployed onto your cluster as a Kubernetes secret with the same name that you set for ```app.imagePullSecrets``` in [values.yaml](values.yaml)

  - [Deploy container registry Kubernetes secret](https://kubernetes-v1-4.github.io/docs/user-guide/kubectl/kubectl_create_secret_docker-registry/)

- Install the helm chart
    ```bash
    helm install . -n login-service
    ```
    **Note**: Use the relative path if you are running from the root directory.
    ```bash
    helm install /LoginService/charts/loginservice -n login-service
    ```
