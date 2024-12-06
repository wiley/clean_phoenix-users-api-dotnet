# Setting up the development environment

:exclamation: Important: Now the project is using Docker and you must be connected on Wiley's VPN!

First connect to aws.

Execute the following commands after cloning the repository:

```
cd dev
# initialize .env
./setup.sh
docker-compose -f docker-compose.dev.yml up
```

After that the project should be running on [http://localhost:8080/](http://localhost:8080/).

Notes: 

* Actually there is no '/' route. So, you can check the health of your environment through the 'Health' endpoint (http://localhost:8080/api/v1.0/Health).

* Once the container is running, it may take a while for your environment to become available.


## If not using a Docker Container for UsersAPI Development

In order to use the shared URIs in the AppSettings.Development.json file simply add the following two lines to your c:/windows/system32/drivers/etc/hosts file:

```
127.0.0.1 usersapi-db
```

## Encryption of Federation Client Secret

All client secret values should be encrypted in the database as they provide a password to obtain any personally identifiable information about any user in the client’s organization and would be tied back to our organization.

To that end, there is now a solution where Staging and Production client secrets can be encrypted by our SREs and never be seen by either developers or other business stakeholders.  Clients will be instructed to provide the secret directly to our SREs.

For your local development environment, you can encrypt a client secret using the following command.  The OpenSSL used here is version 1.1.1a and was installed automatically with our instance of GIT.  Commands for OpenSSL v3 might be different, but the important part is “rsault -encrypt” is encrypting the string with RSA.  We also found that OpenSSL is using PKCS#1 v1.5 by default, and that C# enumeration RSAEncryptionPadding.Pkcs1 also refers to the v1.5 version.

```
echo abcdefg-0013-0023-1111222333|"\program files\git\usr\bin\openssl.exe" rsautl -encrypt -inkey usersapi.pem | "\program files\git\usr\bin\base64.exe" -w0
```

The above approach creates an encryption of the "guid + CR/LF" because the ECHO command sends the CRLF.  The UsersAPI code is designed to trim and remove extra CRLF just in case.  However, you could change this approach and use temporary files and ensure no CRLF in the "unencrypted.txt" input file.

```
"\program files\git\usr\bin\openssl.exe" rsautl -encrypt -inkey usersapi.pem -in unencrypted.txt -out encrypted.bin
type encrypted.bin | "\program files\git\usr\bin\base64.exe" -w0 > encrypted.txt
```

The resulting base64 encoded encrypted string should then be placed in the database OpenIdClientSecret field.

The usrsapi.pem file is now required by the application to exist in the web root folder (~/WLSUser folder where appsettings.config lives) to perform the decryption.  However, the developer usersapi.pem file is different than the one that should be deployed to staging and production and a dev copy is located in the wls-usersapi github root folder.  Developers will need to copy the usersapi.pem file from the root folder to the ~/WLSUsers folder to use the /users/loginsso method in DEV successfully (the /WLSUsers/usersapi.pem file location is excluded in .gitignore).
.

## Useful commands

Build project

```bash
dotnet build WLSUser
```

Run project

```bash
dotnet run build --project WLSUser
```

Generate migration with name UserModelRemoveId
[Entity framework migration files](https://www.learnentityframeworkcore.com/migrations/migration-files)

```bash
dotnet ef migrations add --project WLSUser.Infrastructure --startup-project WLSUser RemoveUsersUniqueIdUniqueIndex
```

Apply migrations (if issue, use -v flag to get more verbose logs + use eventually --no-build)

```bash
dotnet ef database update --startup-project WLSUser --no-build
```

List migrations

```bash
dotnet ef migrations list --startup-project WLSUser --no-build
```

Bundle migrations files

```bash
dotnet ef migrations bundle \
  --project WLSUser.Infrastructure \
  --startup-project WLSUser \
  --self-contained -r linux-x64 \
  --verbose -o dev/efbundle --force
```

Launch migration from bundle file overriding appsettings.json parameter:

```bash
export ConnectionStrings__UserDbContext='server=mysql;port=3306;database=usersapi-qa;user id=usersapi;password=users@pi!;default command timeout=360000;SslMode=none;'
 cd dev # efbundle needs appsettings.json file in the execution folder
 ./efbundle
```

Run Unit tests

```bash
dotnet test
```

Build prod docker image

```bash
ART_URL=https://crossknowledge-889859566884.d.codeartifact.us-east-1.amazonaws.com/nuget/phoenix/v3/index.json
ART_USER=fchastanet@wiley.com
ART_PASS="$(aws codeartifact get-authorization-token \
  --domain crossknowledge \
  --domain-owner 889859566884 \
  --region us-east-1 \
  --query authorizationToken \
  --output text)"
docker build \
  --build-arg "ART_USER=${ART_USER}" \
  --build-arg "ART_PASS=${ART_PASS}" \
  --build-arg "ART_URL=${ART_URL}" \
  -t "${IMAGE_NAME:-users-api-img}" \
  -f "${WORKSPACE:-.}/WLSUser/Dockerfile" \
  "${WORKSPACE:-.}"
```

# FAQ

## unable to copy errors during build

if you have the following errors:
```text
/usr/share/dotnet/sdk/6.0.418/Microsoft.Common.CurrentVersion.targets(5097,5): error MSB3027: Could not copy "/app/WLSUser/obj/Debug/net6.0/apphost" to "bin/Debug/net6.0/WLSUser". Exceeded retry count of 10. Failed.  [/app/WLSUser/WLSUser.csproj]
/usr/share/dotnet/sdk/6.0.418/Microsoft.Common.CurrentVersion.targets(5097,5): error MSB3021: Unable to copy file "/app/WLSUser/obj/Debug/net6.0/apphost" to "bin/Debug/net6.0/WLSUser". Text file busy : '/app/WLSUser/bin/Debug/net6.0/WLSUser' [/app/WLSUser/WLSUser.csproj]
```
run the following commands:
```bash
rm -Rf WLSUser.Infrastructure/{obj,out,bin} WLSUser/{obj,out,bin}
```

## verbose mode doesn't show messages

Apply this command, eg:

```bash
dotnet run build --project WLSUser --verbose | sed -r "s/\x1B\[([0-9]{1,3}(;[0-9]{1,2};?)?)?[mGK]//g"
```