## Installation (Sharepoint 2010)

1.  Download the wsp package to the server where SharePoint 2010 is installed.
2.  On the Start menu, click All Programs > Microsoft SharePoint 2010 Products > SharePoint 2010 Management Shell
3.  Type the following command:  

```
    Add-SPSolution -LiteralPath <SolutionPath>/ADUserEditorWebpart.wsp
```

4.  On the Central Administration Home page, click System Settings > Farm Management > Manage farm solutions.
5.  On the Solution Management page, click the "ADUserEditorWebpart.wsp", then click "Deploy Solution".
6.  Go the site collection where you installed it.
7.  Go to "Site Actions>Modify all parameters" and click on "Site Collection Features". If everything went well, you should find the feature you've just installed (AD User Editor Webpart).
8.  Push the button "Activate" in front of it.
9.  Create a new webpart page or modify the one you want and add the webpart "AD User Editor Webpart".

## Installation (MOSS 2007)

1.  Download and unzip the installation package to the server where MOSS 2007 is installed.
2.  Open a console (Start menu>execute>cmd) and type in the following instructions:  

```
    cd <path to installer> 
    stsadm -o addsolution -filename NomineSharePointTools.wsp
```

3.  Go to your SharePoint "Central Administration" and click on the tab "Operations".
4.  There, you should click on the link named "Solution Management" under the title "Global Configuration". If everything went well, you should find the solution you've just installed (NomineSharePointTools.wsp).
5.  Click it, choose "Deploy Solution", select the Web Application you want it to be deployed to and then click "OK".
6.  Go the site collection where you installed it.
7.  Go to "Site Actions>Modify all parameters" and click on "Site Collection Features". If everything went well, you should find the feature you've just installed (Nomine SharePoint Tools).
8.  Push the button "Activate" in front of it.
9.  Create a new webpart page or modify the one you want and add the webpart "AD User Editor".

## Configuration

### AD Controller(s)

You can click the three dots to view it better (and maybe copy-paste it to your favourite text editor).

This is for the webpart to understand which domain controller it should connect in order to edit an user.

Here is an example configuration for 2 domains:

```
<domains>
   <domain name="MYFIRSTDOMAIN" path="LDAP://dc1.myfirstdomain.com/DC=myfirstdomain,DC=com" usr="MYFIRSTDOMAIN\adminuser1" pwd="xxxxxxxxx" />
   <domain name="MYSECONDDOMAIN" path="LDAP://dc1.myseconddomain.com/DC=myseconddomain,DC=com" usr="MYSECONDDOMAIN\adminuser2" pwd="xxxxxxxxx" />
</domains>
```

Or just one :

```
<domains>
   <domain name="MYFIRSTDOMAIN" path="LDAP://dc1.myfirstdomain.com/DC=myfirstdomain,DC=com" usr="MYFIRSTDOMAIN\adminuser1" pwd="xxxxxxxxx" />
</domains>
```

There are 4 parameters:

*   **name**: It is the Netbios domain name (the netbios name is what you type before your login. eg: if you type **MYFIRSTDOMAIN**\user1, then the netbios name is MYFIRSTDOMAIN)
*   **path**: It's the path to connect to the corresponding Active Directory controller (eg: "LDAP://dc1.myfirstdomain.com/DC=myfirstdomain,DC=com").
*   **usr**: Username of an account that has **Read/Write** access to this Active Directory controller.
*   **pwd**: Password of this account.


### AD Properties

Here you can configure all the profile attributes that you want to appear in the edit form.

Here is an example configuration:

```
<properties>
   <property adname="displayName" name="Full Name" type="readonly" />
   <property adname="thumbnailPhoto" name="Photo" type="image" />
   <property adname="sn" name="Last Name" type="textbox" />
   <property adname="givenName" name="First Name" type="textbox" />
   <property adname="manager" name="Manager" type="person" />
   <property adname="assistant" name="Assistant" type="person" />
   <property adname="department" name="Service" type="listbox" values="IT;Human Resources;Bio;Security" />
   <property adname="title" name="Title" type="textbox" />
   <property adname="employeeType" name="Employee Type" type="dropdown" values="Technician,tech;Manager,mgr;Director,dir" />
   <property adname="telephoneNumber" name="Telephone number" type="textbox" />
   <property adname="otherTelephone" name="Other phone numbers" type="multitextbox" values="4" />
   <property adname="mobile" name="Mobile" type="textbox" />
   <property adname="facsimileTelephoneNumber" name="Fax" type="textbox" />
   <property adname="l" name="Town" type="dropdown" values="New-York;Washington" />
   <property adname="physicalDeliveryOfficeName" name="Office" type="textbox" />
   <property adname="company" name="Company" type="textbox" />
   <property adname="accountExpires" name="Departure Date" type="date" />
   <property adname="extensionCustomAttribute1" name="Certifications" type="checkboxlist" values="Cisco,CIS;Microsoft,MIC;Oracle,ORA" />
</properties>
```

A "property" field has 4 different properties:

*   **adname**: The LDAP name of the attribute you want to modify.  
    (You can find a list of all LDAP attribute names on [www.imibo.com](http://www.imibo.com/imius/User-Detailed-Tabs.htm#Detailed_User_Data))
*   **name**: The name you want to display in the edit form
*   **type**: Which type you want the field to be:
    *   _textbox_: Simple text-box. Common way of editing things.
    *   _dropdown_: Dropdown list with a choice of predefined values.
    *   _listbox_: If a dropdown list would be too long, this is the best solution. It works as "dropdown".
    *   _person_: People Picker to select any people that SharePoint will find in Active Directory.
    *   _date_: Date Picker with calendar.
    *   _readonly_: Read only field
    *   _photo_: Picture upload for the "thumbnailPhoto" attribute (photo will be resized & cropped to 128x128px)
    *   _checkboxlist_: Checkboxes with a choice of predefined values.
    *   _multitextbox_: Multiple textboxes (ex: values="4" indicates that you want 4 textboxes)
*   **values**:
    *   For ListBox, DropDownList and CheckBoxList:  
        values separated by semicolons(;)  
        OR  
        pair of displayed_title,inserted_value (comma (,) between title and value and semicolon(;) between items)
    *   For MultiTextBox:  
        indicates the number of fields (ex: values="4" indicates that you want 4 textboxes)

### Edit current user only

Check the box to activate "self-service mode": logged-in users can only edit their own profile.