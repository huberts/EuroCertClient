# EuroCertClient
This application is a web client written in DotNetCore 7. It is a microservice for connection to EuroCert Signature Cloud.

# Configuration
Configuration is available through the *application.json* file.
```json
  "EuroCert": {
    "CertificateFilePath": "C:/certificate.crt",
    "Address": "https://ecqss.eurocert.pl/api/rsa/sign",
    "TaskId": "0",
    "ApiKey": "API_KEY"
  },
```

# Usage
There is only one route action *[POST]http://localhost:5097/EuroCertSigner* and it accepts JSON input:
```json
{
  "Base64EncodedSourceFilePath": "",
  "Base64EncodedDestinationFilePath": "",
  "SignatureFieldName": "Signed by EuroCert",
  "Appearance": {
    "PageNumber": 0,
    "Rectangle": [0, 0, 0, 0],
    "Reason": "",
    "Location": ""
  }
}
```
The response is even simplier:
```json
{
 "Signature": ""
}
```

# License
This software uses [IText Core Library](https://wiki.itextsupport.com/home/it7kb/releases/release-itext-core-8-0-0 "iText's Homepage"),
and therefore the whole repository is available as open source under the same, [AGPLv3](https://itextpdf.com/how-buy/legal/agpl-gnu-affero-general-public-license "iText's AGPLv3 License") license.
