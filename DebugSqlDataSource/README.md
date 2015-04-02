# dotNET

## DebugSqlDataSource

 - A very simple to use custom Web SqlDataSource control that acts of a see-through to view what SQL statements are being used.

## Usage

 - Register the tag prefix at the start of your ASP.NET WebForm page
 
	``` aspx
	<%@ Register TagPrefix="dbgAuxDS" Namespace="DebugAuxDS" %>
	```
	
 - Specify the ASP.NET Web's SqlDataSource somewhere on your page
 
	```aspx
	<dbgAuxDS:dbgDataSource ID="..." runat="server" CanTrace="True" DurationCache="5" CacheEnable="False" ConnectionString="..." />
	```
	
 - The trace will appear in your output debug window within Visual Studio. 

