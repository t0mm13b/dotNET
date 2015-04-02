# dotNET

## SecHttp

 - A very simple to use Http Module that intercepts IP addresses and allows access to the web site by configuring what IP addresses are allowed.
 - Use NuGET to pull down the third-party source, as specified below or, if deemed so, pull in the files `Bits.cs` and `IPAddressRange.cs` from that project and add it in to the website project.

## Usage

 - Specify the section _**modules**_ within the _**system.webServer**_ section
 
	``` aspx
	<system.webServer>
		<modules>
			<add name="SecHttp" type="SecurityNS.SecHttp" />
		</modules>
	</system.webServer>
	```
	
 - Specify the keys used within the _*appSettings*_ section
 
	``` aspx
	<add key="IsSecured" value="true" />
    <add key="SecuredIPs" value="192.168.1.100-192.168.1.120, 192.168.1.1/24, 127.0.0.1" />
    <add key="Custom403Message" value="This is SPARTAAAAAAAAAA!" />
	```
	
 - IP address ranges can be specified, or CIDR notation can be used.
  
 - Any IP address that is outside of the listed IP addresses will not be able to access the site in question.  

## Third party source included

 - [IP Address Range](https://github.com/jsakamoto/ipaddressrange)