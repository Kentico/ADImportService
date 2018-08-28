# Kentico AD Import Service

[![Build status](https://ci.appveyor.com/api/projects/status/jin5kt2gx4co2gre?svg=true)](https://ci.appveyor.com/project/kentico/adimportservice)
[![first-timers-only](https://img.shields.io/badge/first--timers--only-friendly-blue.svg)](http://www.firsttimersonly.com/)

Kentico Active Directory Import Service provides real-time import of users and groups from the Active Directory database to users and roles in Kentico. The service is fully configurable through a configuration file.


## Installation

Assuming that you have Kentico version 8.x installed, follow these steps:

1. Enable REST service in Kentico settings with basic authentication
2. Download the [ADImportService.exe](https://github.com/Kentico/ADImportService/releases/latest) executable from releases (You might need to unblock it. Right click on `ADImportService.exe`, select properties and click unblock ([screenshot](https://i.imgur.com/ov0mksL.png)).
3. Open the command line and find the ```InstallUtil``` utility (most likely in ```C:\Windows\Microsoft.NET\Framework64\v4.0.x```
4. Execute the ```InstallUtil.exe <path to the ADImportService.exe>``` (e.g.: ```InstallUtil.exe C:\ADImportService\ADImportService.exe```) command
5. Create the ```C:\ProgramData\Kentico AD Import Service\configuration.xml``` file and copy the sample [configuration](#configuration) there
6. Open the configuration file and enter all required values
7. Open Microsoft Management Console and start the ```Kentico AD Import Service```

Immediately after starting, it gets the current users and groups and adds them to Kentico. Then it enables processing of asynchronous changes. If the application fails, it informs about the event in the Windows Event Log.


### Configuration

Here is a sample configuration which you can copy to the ```configuration.xml``` file. 

```xml
<ServiceConfiguration>
	<Listener DomainController="FQDN or IP of Domain Controller" 
	UseSsl="false" SslCertificateLocation="Path to .cer file">
		<Credentials>
			<UserName>UserName</UserName>
			<Password>Password</Password>
			<Domain>Domain</Domain>
		</Credentials>
	</Listener>
	<Rest UserName="Kentico user name" Password="Kentico password" 
	Encoding="utf-8" BaseUrl="http://localhost/Kentico8 (use https to ebnable SSL)" 
	SslCertificateLocation="Path to .cer file" />
	<UserAttributesBindings>
		<Binding Cms="FullName" Ldap="sAMAccountName" />
		<Binding Cms="UserGUID" Ldap="objectGUID" />
	</UserAttributesBindings>
	<GroupAttributesBindings>
		<Binding Cms="RoleDisplayName" Ldap="sAMAccountName" />
		<Binding Cms="RoleDescription" Ldap="description" />
		<Binding Cms="RoleGUID" Ldap="objectGUID" />
	</GroupAttributesBindings>
</ServiceConfiguration>
```

### Common installation issues

If you're not able to run the service, make sure that 

- LDAP server is accessible
- REST service is accessible (try to open it in your browser ```www.yourdomain.com/rest/cms.user```)
- Credentials are valid
- Kentico user is able to modify users and roles
- Windows user is able to read from AD database
- Check the Windows Event log and Kentico Event log for error messages


## Acknowledgement

The project is based on code developed by [Tomas Hruby](https://github.com/TomHruby) for his [bachelor thesis](https://is.muni.cz/th/396080/fi_b/?furl=%2Fth%2F396080%2Ffi_b%2F;so=nx;lang=en) (full text of the [thesis in pdf](https://is.muni.cz/th/396080/fi_b/thesis.pdf)).


## Contributing
Want to improve the AD Import Service? Great! But make sure you read the [contributing guidelines](https://github.com/Kentico/KInspector/blob/master/CONTRIBUTING.md) first.

If anything feels wrong or incomplete, please let us know. Create a new [issue](https://github.com/Kentico/ADImportService/issues/new) or submit a [pull request](https://help.github.com/articles/using-pull-requests/).

![Analytics](https://kentico-ga-beacon.azurewebsites.net/api/UA-69014260-4/Kentico/ADImportService?pixel)
