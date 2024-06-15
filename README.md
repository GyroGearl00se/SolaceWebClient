# SolaceWebClient

## Description
The Solace Web Client is using the .NET (C#) library and offers Queue browsing, Publish & Subscribe.

![](docs/SolaceWebClient-Pub_Sub.png)
![](docs/SolaceWebClient-Queue_Browser.png)

## Secure Connection (TLS)
To validate secure connections (tcps://) mount the certificate(s) from your desired endpoint(s) in the "/app/trustedca" directory.

By default the checkbox for TCPS connection is checked. To use it for nonSSL endpoints simply uncheck it.

NOT RECOMMENDED - You can also uncheck "SSL Verify" to skip SSL validation.


### Docker

```
docker run -d -p 8080:8080 gyrogearl00se/solacewebclient:latest

For TCPS connections
docker run -d -p 8080:8080 -v $(pwd)/certs:/app/trustedca gyrogearl00se/solacewebclient:latest
```
