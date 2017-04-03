*   **2.1.4**:
    *   Self service authentication change from "System.Security.Principal.WindowsIdentity.GetCurrent().Name" to "SPContext.Current.Web.CurrentUser.LoginName"
    *   Fix typo in default german and spanish default configuration causing errors with xml parsing
*   **2.1.3**:
    *   German translation, thanks to Danuueel
*   **2.1.2**:
    *   Dutch translation, thanks to maarteng
*   **2.1.1**:
    *   displayName instead of distinguishedName (CN=xxx,DC=xxx) in read only fields like "manager"
    *   read only field can now display multiple values
    *   no more exception trying to display errors (sic)
    *   message displayed when webpart hasn't yet been configured
    *   having multiple multitextbox fields is now possible
*   **2.1.0**:
    *   **image field type** (image upload resizing & cropping to 128x128px, jpg only)
    *   **new field types for multivalued attributes: checkboxlist, multi-textbox**
*   **2.0.1**:
    *   compatibility with SP2010 "claims based authentication" (strips 'i:0#.w|' from the username)
    *   meaningful error message if required domain missing from xml config (instead of keyNotFoundException)
    *   auto uppercase domain NetBios name (-> less config errors)
    *   better "person field" handling ('DOMAIN\username' instead of just 'username' when filling the field)
    *   edit form now with the current theme style
*   **2.0.0**:
    *   **Now for SharePoint 2010** !
    *   **Multilingual**: English, French and Spanish.
*   **1.1**:
    *   _NEW_: "readonly" field type added
    *   _bugfix_: no more editing config in personal view
    *   _bugfix_: no more need to add empty "values" attribute in xml config
    *   _bugfix_: no more "user found" message in self-service mode
*   **1.0.1**:
    *   _bugfix_: solved issue with "person" field type
    *   now available in Spanish, thanks to Gaston Schab!