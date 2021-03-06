# watters/gelf4net

## Overview

This fork introduces a handful of breaking changes from the original [gelf4net](https://github.com/jjchiw/gelf4net), including:

 * separate appenders for UDP and AMQP
 * simplified class structure
 * disallows UDP messages with more than 128 chunks (per [GELF spec](https://github.com/Graylog2/graylog2-docs/wiki/GELF))
 * always sends file/line number if it's in the loggingEvent
 * renamed a couple of configuration properties

(NOTE: I've done less work on AMQP since I don't use it, but I'd expect it to work)
 
## Usage

**Properties**

***All Appenders***

* string AdditionalFields //Key:Value CSV ex: app:MyApp,version:1.0
* string Facility
* string Host
* string RemoteAddress
* int RemotePort

***UDF Appender***

* int ChunkSize

***AMQP Appender***

* int GrayLogServerAmqpPort
* string GrayLogServerAmqpUser
* string GrayLogServerAmqpPassword
* string GrayLogServerAmqpVirtualHost
* string GrayLogServerAmqpQueue

***Per-message custom fields***

Any properties returned from LoggingEvent.GetProperties() will be appended as custom properties. This includes properties set on GlobalContext and ThreadContext (see: http://logging.apache.org/log4net/release/manual/contexts.html).

Additionally, the appender will reflect over the LoggingEvent.MessageObject looking for properties of a type that implements IDictionary. It will then include all of these values as well. If name collisions are detected, the appender will prefer the most-specific values:

 1. Fields retrieved from MessageObject
 2. Fields retrieved from LoggingEvent.GetProperties()
 3. Fields specified in the appender configuration
 
The core GELF field "short_message" can be specified per-message by adding a string property to your custom message object (LoggingEvent.MessageObject) named "short_message" or "ShortMessage".

**log4net Xml Configuration**

	<?xml version="1.0"?>
	<configuration>
		<configSections>
			<section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler,Log4net"/>
		</configSections>

		<log4net>
			<root>
				<level value="DEBUG"/>
				<appender-ref ref="UdpGelfAppender"/>
			</root>

			<appender name="UdpGelfAppender" type="Esilog.Gelf4net.UdpGelfAppender, Esilog.Gelf4net">
				<param name="RemoteAddress" value="public-graylog2.taulia.com" />
				<param name="Facility" value="RandomPhrases" />
				<param name="AdditionalFields" value="app:RandomSentence,version:1.0" />

				<layout type="log4net.Layout.PatternLayout">
					<param name="ConversionPattern" value="%-5p%d{yyyy-MM-dd hh:mm:ss}%m%n"/>
				</layout>
			</appender>
		</log4net>

		<startup>
			<supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.0"/>
		</startup>
	</configuration>

## Copyright and License

watters/gelf4net 

forked from:
jjchiw/gelf4net created by Juan J. Chiw - Copyright 2011

based on:
gelf4j created by Philip Stehlik - Copyright 2011

See LICENSE for license details