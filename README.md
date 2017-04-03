**AD User Editor** is a WebPart for SharePoint 2010 & MOSS 2007 to easily edit any user profile attribute in Active Directory trough an entirely customizable form.

*   Nearly **Any Active Directory attribute** can be edited trough this webpart
*   **Edit form entirely customizable** via XML configuration (nothing "hard-coded")
*   **9 field types**: Photo (upload & resize), Read-Only, Single TextBox, Multiple TextBox, DropDownList (with preset values), ListBox (with preset values), People Picker, Date Picker, Checkboxes (with preset values, multivalued)
*   **Multi-domain** compatible
*   **SharePoint form "look & feel"**
*   **2 edit modes:** Self-service or user picker
*   Available for Sharepoint 2010 in [multilanguage version (EN, FR, ES, NL, DE)](https://nominesptools.codeplex.com/releases/view/107079)

Additional informations and installation manual are available [here](http://alexis.nomine.fr/en/ad-user-editor-webpart-sharepoint-2010-moss-2007/) (French & English).

_**Warning**: This tool modify data in Active Directory. Be careful setting the user access rights. It has been tested and verified, but I cannot be held responsible for any loss of data - use at your own risk._

![adusereditor.png](http://alexis.nomine.fr/en/files/2009/08/adusereditor.png "adusereditor.png")

_Feel free to send any constructive comment to help me making it even better._

## Sources

I picked a lot of infos and get inspirated by some bits of source code googling the web. My most important sources of inspiration and help were:

*   [ActiveDirectoryTools](http://adselfservice.codeplex.com/) of Burke Holland
*   SharePoint solution created by the [STSDEV](http://codeplex.com/stsdev) utility
*   Right way of coding webpart on [Ishai Sagi's blog](http://www.sharepoint-tips.com/2007/03/server-side-controls-and-data-binding.html)
*   Sharepoint Web Controls (User picker, Date picker) from [Karine Bosch's blog](http://karinebosch.wordpress.com/sharepoint-controls/)
*   Photo resizing & cropping [BRITISH DEVELOPER](http://www.britishdeveloper.co.uk/2011/05/image-resizing-cropping-and-compression.html)