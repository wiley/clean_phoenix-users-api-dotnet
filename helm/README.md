#Steps to install/configure company-api
*Note: Steps assume you are deploying to the namespace ${NAMESPACE} in EKS using ALB target groups*
### Create namespace
```shell
  kubectl create namespace ${NAMESPACE}
```
---
### Add required secrets
```shell
  # Update each --from-literal with values for your env
  kubectl create secret generic users-api-appsettings \
    --namespace ${NAMESPACE} \
    --from-literal=APPSETTINGS={"Your":{"Awesome":{"json","here"}}}
```
*Note:*
      Some secrets have many special characters which may be difficult to pass on the command line.
      These can sometimes be wrapped in single quotes or escaped using your command interpreter's escape characters.
      If you have issues, you may follow this alternate approach...
```shell
  # get base64 value of secrets. Copy this to be used later
  echo 'yes, this works!' | base64
  # produces base64 string 'eWVzLCB0aGlzIHdvcmtzIQo='
  
  # Create blank secret in your namespace
  kubectl create secret generic users-api-appsettings --namespace ${NAMESPACE}
  
  # Edit the secret and add base64 encoded version
  kubectl edit secret generic users-api-appsettings --namespace ${NAMESPACE}
```
This will open the blank secret and give the option to add the data section with the base64 values
```yaml
# Please edit the object below. Lines beginning with a '#' will be ignored,
# and an empty file will abort the edit. If an error occurs while saving this file will be
# reopened with the relevant failures.
#
apiVersion: v1
data:
  SOMESECRET: "eWVzLCB0aGlzIHdvcmtzIQo="
  OTHERSECRET: "eWVzLCB0aGlzIHdvcmtzIQo="
kind: Secret
metadata:
  creationTimestamp: "2021-11-01T15:07:58Z"
  name: test
  namespace: default
  resourceVersion: "2046178"
  uid: b4229835-c187-41b0-b954-f3d2284878ca
type: Opaque
```
Once complete, save and close your editor. kubectl will automatically upload the contents to EKS

### Verify Secrets
```shell
  # Get all base64 encoded values using kubectl
  kubectl get secret users-api-appsettings --namespace ${NAMESPACE} -o jsonpath='{.data}'
  # --OR--
  # Get a specific base64 encoded value using kubectl
  kubectl get secret users-api-appsettings --namespace ${NAMESPACE} -o jsonpath='{.data.YourSecret}'
  
  # Copy desired secret from output and paste in to echo
  echo 'eWVzLCB0aGlzIHdvcmtzIQo=' | base64 --decode
```

---
### Deploy
  ```shell
    # If your namespace was not created above, it will be auto-created in this step
    # Run from $/helm folder. If running from other folder, add path to Chart.yaml instead of '.'
    helm upgrade --install users-api . --namespace ${NAMESPACE} --create-namespace -f value_files\${ENV}.yaml
  ```