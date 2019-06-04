# uScale Loadbalancer
                           __
    __  ________________ _/ /__ 
   / / / / ___/ ___/ __ `/ / _ \
  / /_/ (__  ) /__/ /_/ / /  __/
  \__,_/____/\___/\__,_/_/\___/ 

The uScale loadbalancer is a simple high-availability loadbalancer written in C# using Watson and can be used with any HTTP/HTTPS application.

![alt tag](https://github.com/jchristn/uscale/blob/master/assets/diagram_uscale.png)

## Setup

Run the app with ```setup``` in the command line arguments to run the setup script and create a ```system.json``` file.

## Definitions

- A 'Host' is defined as a virtual resource accessible by hostname as found in the HTTP request's ```Host``` header.  ```Hosts``` is an array, so you can have multiple managed virtual resources

- A 'Node' is a physical resource mapped to a host by its hostname and port.  ```Nodes``` is an array, so you can have multiple nodes mapping to a host

- Nodes are polled at an interval according to their configuration, and removed from rotation when the maximum number of failures are reached

- Node polling continues while a node is failed to detect return-to-service conditions

- The ```HeartbeatUrl``` for a Node must be a full URL including the protocol, i.e. http://10.1.1.1:80/loopback.  This URL must always return a 200 to indicate that the Node is online and available

- The ```HandlingMode``` should either be ```Proxy``` or ```Redirect```:
  - Proxy: uscale will submit a request on behalf of the requestor and marshal the response back to the requestor.  
  - Redirect: uscale will send an HTTP redirect according to the configuration

- The ```BalancingScheme``` should always be set to ```RoundRobin``` (for now)
 
## Performance and Scale

It is recommended that you use ```redirect``` for ```HandlingMode``` as this will unburden uscale from having to proxy each connection.

## Sample Configuration

```
{
  "EnableConsole": true,
  "RedirectStatusCode": 302,
  "RedirectStatusString": "Moved Temporarily",
  "Hosts": [
    {
      "Name": "MyApp",
      "HttpHostNames": [
        "www.myapp.com",
        "myapp.com"
      ],
      "Nodes": [
        {
          "Hostname": "10.1.1.1",
          "Port": 80,
          "Ssl": false,
          "HeartbeatUrl": "http://10.1.1.1:80/loopback",
          "PollingIntervalMsec": 2500,
          "MaxFailures": 4,
          "Failed": false
        },
        {
          "Hostname": "10.1.1.2",
          "Port": 80,
          "Ssl": false,
          "HeartbeatUrl": "http://10.1.1.2:80/loopback",
          "PollingIntervalMsec": 2500,
          "MaxFailures": 4,
          "Failed": false
        }
      ],
      "LastIndex": 0,
      "BalancingScheme": "RoundRobin",
      "HandlingMode": "Redirect",
      "AcceptInvalidCerts": true
    }
  ],
  "Server": {
    "DnsHostname": "+",
    "Port": 9000,
    "Ssl": false
  },
  "Auth": {
    "AdminApiKeyHeader": "x-api-key",
    "AdminApiKey": "admin"
  },
  "Logging": {
    "SyslogServerIp": "127.0.0.1",
    "SyslogServerPort": 514,
    "MinimumSeverityLevel": 1,
    "LogRequests": false,
    "LogResponses": false,
    "ConsoleLogging": true
  },
  "Rest": {
    "UseWebProxy": false,
    "WebProxyUrl": "",
    "AcceptInvalidCerts": true
  }
}

```

## Admin APIs

Using the admin API key, a set of RESTful APIs can be used to gather visibility into the loadbalancer during runtime.  The admin API key header defined in the ```Auth``` section of the config can be included as a header or as a querystring key-value pair.

```
GET /_loadbalancer/config?x-api-key=admin
GET /_loadbalancer/connections?x-api-key=admin
GET /_loadbalancer/hosts?x-api-key=admin
```

## Multiplatform Support

uscale supports both .NET Framework as well as .NET Core.  .NET Core is recommended for cross-platform deployments.

uscale works well in Mono environments to the extent that we have tested it. It is recommended that when running under Mono, you execute the containing EXE using --server and after using the Mono Ahead-of-Time Compiler (AOT).

NOTE: Windows accepts '0.0.0.0' and '+' as an IP address representing any interface.  On Mac and Linux with Mono you must supply a specific IP address ('127.0.0.1' is also acceptable, but '0.0.0.0' is NOT).

```
mono --aot=nrgctx-trampolines=8096,nimt-trampolines=8096,ntrampolines=4048 --server uscale.exe
mono --server uscale.exe
```
