# EuroCertClient
This application is a web client written in DotNetCore 7. It is a microservice for connection to EuroCert Signature Cloud.

# Configuration
Configuration is available through the *application.json* file.
```json
  "EuroCert": {
    "CertificateFilePath": "C:/certificate.crt",
    "Address": "https://ecqss.eurocert.pl/api/rsa/sign"
  },
```
*TaskId* and *ApiKey* should be provided in the request message.

# Usage
There is only one route action *[POST]http://localhost:5097/EuroCertSigner* and it accepts Form input:
```json
{
"EuroCertApiKey":"****",
"EuroCertTaskId":"*",
"SignatureFieldName":"test_signature",
"Appearance":{
  "PageNumber":1,
  "X":53.25,
  "Y":104.0,
  "Width":125.0,
  "Height":25.0,
  "Reason":"",
  "Location":""
  }
}
```
The response is the signed file stream.
If EuroCert returns with error, *InternalServerError* is returned by this service.

# License
This software uses [IText Core Library](https://wiki.itextsupport.com/home/it7kb/releases/release-itext-core-8-0-0 "iText's Homepage"),
and therefore the whole repository is available as open source under the same, [AGPLv3](https://itextpdf.com/how-buy/legal/agpl-gnu-affero-general-public-license "iText's AGPLv3 License") license.
