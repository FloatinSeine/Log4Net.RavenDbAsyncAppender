# Log4Net RavenDbAsyncAppender

A simple <a href="http://logging.apache.org/log4net/">Log4Net</a> appender to allow logging to <a href="http://ravendb.net/">RavenDB</a>. 

Based on top of the <a href="http://logging.apache.org/log4net/release/sdk/log4net.Appender.BufferingAppenderSkeleton.html">BufferedAppenderSkeleton</a> was developed to use async logging to RavenDB to maximise the throughput of logging from the calling application.

## Appender Properties

In addition to the standard BufferedAppenderSkeleton properties the following updates are included:

ConnectionString the name of the connection string to use to connect to the RavenDB server

SlidingFlush the number of seconds to wait until the next automatic flush occurs after the last Buffer Flush. If set to 0 then no timer is created.

BufferSize is used to change the default requests that is used on a IDocumentSession.


##Configuring the Appender

The following section provides Sample configuration section. 

```
  <configSections>
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" requirePermission="false"/>
  </configSections>
  <connectionStrings>
    <add name="ApplicationLogs" connectionString="URL=http://localhost:8080;Database=ApplicationLogs"/>
  </connectionStrings>

  <log4net>
    <appender name="RavenDbBufferedAsyncAppender" type="Log4Net.AsyncAppender.RavenDbBufferedAsyncAppender, Log4Net.AsyncAppender">
      <bufferSize value="100" />
      <connectionString value="ApplicationLogs" />
      <slidingFlush value="60" />
    </appender>

    <root>
      <appender-ref ref="RavenDbBufferedAsyncAppender" />
    </root>
  </log4net>
```



--------

## About Stephen Williams

Writes at his blog [Agog in the Ether](http://agogintheether.blogspot.co.uk/).