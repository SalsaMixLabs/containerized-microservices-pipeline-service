# Using Helm for Configuration Values
Helm leverages configmaps in order to define configuration parameters. These values must be manually configured in the values.yaml file. The steps to obtain these values are shown below.

## Values to be Changed
1. aadAppId
  a. From App registrations in Azure Active Directory, select your application.
  b. In the Overview blade, copy the Application ID and store it in your application code.
2. appInsightsKey
  a. From your "Application Insights" resource, click on the overview blade, and copy the instrumentation key
3. jwtKey
4. omsWorkspaceId
  a. Run the following command, and the output is your ID:
  
```
WSID=$(az resource show --resource-group loganalyticsrg --resource-type Microsoft.OperationalInsights/workspaces --name containerized-loganalyticsWS | grep customerId | sed -e 's/.*://')
```
5. secretsVaultUrl
  a. Navigate to your Key Vault, and on the overview blade, copy the "DNS Name", and this is your secrets vault URL.
