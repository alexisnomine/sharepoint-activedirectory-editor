/* --- AD User Editor Webpart ---
 * Version: 2.1.4
 * Author: Alexis NOMINE
 * Website: http://alexis.nomine.fr/
 * Licence: MIT
 * ------------------------
 * Changelog:
 * ------------------------
 * 
 * 2.1.4 :  - Self service authentication change from "System.Security.Principal.WindowsIdentity.GetCurrent().Name" to "SPContext.Current.Web.CurrentUser.LoginName" 
 *          - Fix typo in default german and spanish default configuration causing errors with xml parsing
 * 
 * 2.1.3 :  - German translation, thanks to Danuueel
 * 
 * 2.1.2 :  - Dutch translation, thanks to maarteng
 * 
 * 2.1.1 :  - displayName instead of distinguishedName (CN=xxx,DC=xxx) in read only fields like "manager"
 *          - read only field can now display multiple values
 *          - no more exception trying to display errors (sic)
 *          - message displayed when webpart hasn't yet been configured
 *          - having multiple multitextbox fields is now possible
 * 
 * 2.1.0 :  - thumbnailPhoto attribute modification with image upload resizing & croping (jpg only)
 *          - new field types for multivaluated properties: checkboxlist, multitextbox
 *          - autosize listbox
 * 
 * 2.0.1 :  - compatibility with SP2010 "claims based authentication" (strips 'i:0#.w|' from the username)
 *          - meaningful error message if required domain missing from xml config (instead of keyNotFoundException)
 *          - auto uppercase domain NetBios name (-> less config errors)
 *          - better "person field" handling ('DOMAIN\username' instead of just 'username' when filling the field)
 *          - edit form now with the current theme style
 * 
 * 2.0.0 : - Initial release for SharePoint 2010
 * 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.Linq;
using Microsoft.SharePoint;

namespace ADUserEditorWebpart.ADUserEditorWebpart
{
    #region custom types

    public struct userProperty
    {
        public string ADName;
        public string Name;
        public string Values;
        public string Type;
    };

    public struct domainConnexion
    {
        public string path;
        public string login;
        public string password;
    }

    public enum messageType { error, info };

    #endregion

    [ToolboxItemAttribute(false), Serializable]
    public class ADUserEditorWebpart : Microsoft.SharePoint.WebPartPages.WebPart
    {
        #region Webpart Parameters Declaration

        private string _ADDomains = "<domains><domain name='NOMINE' path='LDAP://srvad/DC=nomine,DC=fr' usr='NOMINE\\admdom' pwd='xxxx' /></domains>";
        [WebBrowsable(true)]
        [ResourcesAttribute(Category = "lgConfiguration", Name = "lgDomainControllers")]
        [Personalizable(PersonalizationScope.Shared)]
        public string ADDomains
        {
            get { return _ADDomains; }
            set { _ADDomains = value; }
        }

        private string _ADProperties = "";
        [WebBrowsable(true)]
        [ResourcesAttribute(Category = "lgConfiguration", Name = "lgPropertiesDefinition")]
        [Personalizable(PersonalizationScope.Shared)]
        public string ADProperties
        {
            get {
                if (_ADProperties.Equals(""))
                    return Utilities.getLocalizedValue("lgDefaultPropertiesConf"); 
                else 
                    return _ADProperties;
            }
            set { _ADProperties = value; }
        }

        private bool _SelfService = true;
        [WebBrowsable(true)]
        [ResourcesAttribute(Category = "lgConfiguration", Name = "lgSelfService")]
        [Personalizable(PersonalizationScope.Shared)]
        public bool SelfService
        {
            get { return _SelfService; }
            set { _SelfService = value; }
        }

        #endregion

        #region Internal Variables Declaration

        private List<userProperty> userProperties;
        private Dictionary<string, domainConnexion> domains;
        private DirectoryEntry ADConnexion;
        private DirectorySearcher search;
        private SearchResult CurrentUser;

        private PeopleEditor userSelect;
        private Label state;
        private Table editForm;
        private Dictionary<string, Control> EditFields;

        #endregion

        protected override void CreateChildControls()
        {
            this.UseDefaultStyles = false; // No default styles
            CssRegistration.Register("/_layouts/1036/styles/Themable/forms.css");

            #region Info Zone

            state = new Label();
            state.CssClass = "ms-informationbar";
            state.Style.Add("display", "block");
            state.Style.Add("padding", "5px 5px 5px 25px");
            state.Style.Add("margin", "10px");
            state.Visible = false;
            this.Controls.Add(state);

            #endregion

            // check if default config still in place
            if (!_ADDomains.Contains("<domains><domain name='NOMINE' path='LDAP://srvad/DC=nomine,DC=fr' usr='NOMINE\\admdom' pwd='xxxx' /></domains>"))
            {
                try
                {
                    userProperties = Utilities.getUserPropertiesFromXML(ADProperties);
                    domains = Utilities.getDomainsFromXML(_ADDomains);
                    ADConnexion = new DirectoryEntry();
                    search = Utilities.initDirectorySearcher(ADConnexion, userProperties);

                    // no user picker if self service
                    if (!_SelfService)
                    {
                        info(Utilities.getLocalizedValue("lgSelectUserToEdit"));
                        #region User picker

                        Table userSelection = new Table();
                        //userSelection.CssClass = "ms-toolbar";
                        userSelection.Style.Add("background", "white url(/_layouts/images/bgximg.png) repeat-x 0px -882px");
                        //userSelection.CssClass = "ms-formbody";
                        userSelection.Style.Add("border", "1px solid #a5a5a5;");
                        userSelection.Style.Add("margin", "10px auto");

                        TableRow tr1 = new TableRow();
                        TableRow tr2 = new TableRow();
                        TableRow tr3 = new TableRow();
                        userSelection.Controls.Add(tr1);
                        userSelection.Controls.Add(tr2);
                        userSelection.Controls.Add(tr3);
                        TableCell tc1 = new TableCell();
                        tr1.Cells.Add(tc1);
                        TableCell tc2 = new TableCell();
                        tr2.Cells.Add(tc2);
                        TableCell tc3 = new TableCell();
                        tr3.Cells.Add(tc3);
                        tc1.HorizontalAlign = tc3.HorizontalAlign = HorizontalAlign.Center;

                        Label userSelectLabel = new Label();
                        userSelectLabel.Text = Utilities.getLocalizedValue("lgUserSelection");
                        tc1.Controls.Add(userSelectLabel);

                        userSelect = new PeopleEditor();
                        userSelect.MultiSelect = false;
                        userSelect.AllowEmpty = false;
                        tc2.Controls.Add(userSelect);

                        Button selectUser = new Button();
                        selectUser.Text = Utilities.getLocalizedValue("lgValidate");
                        selectUser.Click += new EventHandler(selectUser_Click);
                        tc3.Controls.Add(selectUser);

                        this.Controls.Add(userSelection);

                        #endregion
                    }

                    #region Edit form

                    EditFields = new Dictionary<string, Control>();
                    editForm = new Table();
                    //appearance
                    editForm.CssClass = "ms-formtable";
                    editForm.Width = Unit.Percentage(100);
                    editForm.CellSpacing = 0;
                    editForm.EnableViewState = true;

                    #region fields

                    foreach (userProperty prop in userProperties)
                    {
                        // Add the table row
                        TableRow oRow = new TableRow();
                        editForm.Rows.Add(oRow);

                        // Add the cells
                        TableCell oCellLabel = new TableCell();
                        oCellLabel.Width = Unit.Pixel(190);//.Percentage(40);
                        oRow.Cells.Add(oCellLabel);
                        TableCell oCellControl = new TableCell();
                        //oCellControl.Width = Unit.Percentage(60);
                        oRow.Cells.Add(oCellControl);

                        // Create the label field
                        Label l = new Label();
                        l.Text = prop.Name;

                        // Add the label to the table cell
                        oCellLabel.Controls.Add(l);

                        // Set the css class of the cell for the SharePoint styles
                        oCellLabel.CssClass = "ms-formlabel";
                        oCellControl.CssClass = "ms-formbody";

                        switch (prop.Type)
                        {
                            case "textbox":
                                // Create the form field
                                TextBox t = new TextBox();
                                t.CssClass = "ms-long";
                                t.ID = prop.ADName;
                                //t.EnableViewState = true;

                                // Add the field to the table cell and to field references
                                oCellControl.Controls.Add(t);
                                EditFields.Add(prop.ADName, t);
                                break;

                            case "listbox":
                                ListBox lb = new ListBox();
                                lb.ID = prop.ADName;
                                lb.SelectionMode = ListSelectionMode.Single; // one selection only
                                lb.CssClass = "ms-long";
                                lb.Items.Add(""); // add empty item

                                string[] List2 = null;
                                if (prop.Values != null)
                                {
                                    List2 = prop.Values.Split(';');
                                    foreach (string listItem in List2)
                                    {
                                        if (listItem.Contains(",")) // Pairs name/value
                                        {
                                            lb.Items.Add(new ListItem(listItem.Split(',')[0].Trim(), listItem.Split(',')[1].Trim()));
                                        }
                                        else // Only value
                                        {
                                            lb.Items.Add(listItem.Trim());
                                        }
                                    }
                                }

                                lb.Rows = List2.Length + 1;

                                // Add the field to the table cell and editFields list
                                oCellControl.Controls.Add(lb);
                                EditFields.Add(prop.ADName, lb);
                                break;

                            case "checkboxlist":
                                CheckBoxList cbl = new CheckBoxList();
                                cbl.ID = prop.ADName;

                                string[] List3 = null;
                                if (prop.Values != null)
                                {
                                    List3 = prop.Values.Split(';');
                                    foreach (string listItem in List3)
                                    {
                                        if (listItem.Contains(",")) // Pairs name/value
                                        {
                                            cbl.Items.Add(new ListItem(listItem.Split(',')[0].Trim(), listItem.Split(',')[1].Trim()));
                                        }
                                        else // Only value
                                        {
                                            cbl.Items.Add(listItem.Trim());
                                        }
                                    }
                                }

                                // Add the field to the table cell and editFields list
                                oCellControl.Controls.Add(cbl);
                                EditFields.Add(prop.ADName, cbl);
                                break;

                            case "dropdown":
                                DropDownList dl = new DropDownList();
                                dl.ID = prop.ADName;
                                dl.CssClass = "ms-long";
                                dl.Items.Add("");

                                //generates each list item
                                string[] List;
                                if (prop.Values != null)
                                {
                                    List = prop.Values.Split(';');

                                    foreach (string listItem in List)
                                    {
                                        if (listItem.Contains(",")) // Pair name/value
                                        {
                                            dl.Items.Add(new ListItem(listItem.Split(',')[0].Trim(), listItem.Split(',')[1].Trim()));
                                        }
                                        else // only value
                                        {
                                            dl.Items.Add(listItem.Trim());
                                        }
                                    }
                                }
                                // Add the field to the table cell and editFields list
                                oCellControl.Controls.Add(dl);
                                EditFields.Add(prop.ADName, dl);
                                break;

                            case "person":
                                PeopleEditor p = new PeopleEditor();
                                p.MultiSelect = false; // only one user
                                p.ID = prop.ADName;
                                // Add the field to the table cell and editFields list
                                oCellControl.Controls.Add(p);
                                EditFields.Add(prop.ADName, p);
                                break;
                            case "date":
                                DateTimeControl dtc = new DateTimeControl();
                                dtc.DateOnly = true;
                                dtc.ID = prop.ADName;
                                // Add the field to the table cell and editFields list
                                oCellControl.Controls.Add(dtc);
                                EditFields.Add(prop.ADName, dtc);
                                break;
                            case "readonly":
                                Label lbl = new Label();
                                lbl.ID = prop.ADName;
                                // Add the field to the table cell and editFields list
                                oCellControl.Controls.Add(lbl);
                                EditFields.Add(prop.ADName, lbl);
                                break;
                            case "multitextbox":
                                // main panel
                                Panel pa = new Panel();
                                pa.ID = prop.ADName;

                                /*
                                // store the number of text fields
                                HiddenField tbNum = new HiddenField();
                                tbNum.ID = "tbNum";
                                tbNum.Value = "1";
                                pa.Controls.Add(tbNum);
                                //TODO: don't forget to clear this hidden row in clearFields()

                                int numTextBox = Int32.Parse(tbNum.Value);*/

                                int numTextBox = 1;
                                if (prop.Values != null)
                                    numTextBox = Int32.Parse(prop.Values);

                                for (int i = 0; i < numTextBox; i++)
                                {
                                    TextBox tb = new TextBox();
                                    tb.CssClass = "ms-long";
                                    //tb.ID = "tb" + i;
                                    pa.Controls.Add(tb);
                                    pa.Controls.Add(new LiteralControl("<br />"));
                                }

                                // Add the field to the table cell and editFields list
                                oCellControl.Controls.Add(pa);
                                EditFields.Add(prop.ADName, pa);
                                break;
                            case "image":
                                FileUpload fu = new FileUpload();
                                fu.ID = prop.ADName;

                                // default image in base64
                                Image img = new Image();
                                img.ID = "previewImg";
                                img.ImageUrl = "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEASABIAAD//gA7Q1JFQVRPUjogZ2QtanBlZyB2MS4wICh1c2luZyBJSkcgSlBFRyB2NjIpLCBxdWFsaXR5ID0gOTAK/9sAQwAEAgMDAwIEAwMDBAQEBAUJBgUFBQULCAgGCQ0LDQ0NCwwMDhAUEQ4PEw8MDBIYEhMVFhcXFw4RGRsZFhoUFhcW/9sAQwEEBAQFBQUKBgYKFg8MDxYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYW/8AAEQgAgACAAwEiAAIRAQMRAf/EABsAAQEAAwEBAQAAAAAAAAAAAAAHAwQGBQII/8QAOBAAAgIBAgIHBAkDBQAAAAAAAAECAwQFEQYhBxITMUFRYXGBkaEUFSIyUrHB4fAjM7JCY4KS0f/EABQBAQAAAAAAAAAAAAAAAAAAAAD/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwD9FAAAAAAAAAAAAAAAAAAAAAAAAAAAAZMWi7IyIUUVystsfVjGK3bYGMHf8NcC0VQV+rvtbGt+whLaEfa1zb9nL2nWYWBhYcVHGxKKUvwVqIEUBbczDxMqPVycWm5PwsrUvzOW4j4Fw8iDu0qX0a7v7KTbrl+q/L0AnYM2djX4eVPHya5V21vaUX4GEAAAAAAAAAAABTujzh+GmaeszIgvpl63e651RfdFevn8PA4jgbBjn8T41M1vXCXaT9VHnt73sveV0AAAAAA5/jnQa9Y06U6oJZlMW6peMl+B+j8PJ+8lkk4ycZJxaezT70XMlXSXgRwuKbXCO0MmKuSXm91L5pv3geAAAAAAAAAAAOr6IEnxPdulusSW3/eBSSUdHGXHE4ux+u9o39apv1a5fNIq4AAAAAAJ70yJfWmE9ufYy5/8ihEv6VMtZPFUqovdYtUa3t585P8Ay29wHNgAAAAAAAAAD6rnKFkZwk4yi04yXen5lc4R1erWdIryOsu2glG+K/0z/wDH3r9iQno8M6ln6bqUbMBSsnP7MqUnJWryaQFjBp6RkW5eFC+7EtxZy+9Vbt1l+3w9huAADX1LIni4c74Y1uRKK5VVpOUgNbiTU6NI0uzMuabitq4b85y8F/PDckGVdZk5NmRdLrWWzcpy823uz0eLdV1DVNSlLOhKlV7qGO00q17H4+bPKAAAAAAAAAAHRcAcOvWMx5GSmsOiS6/+5L8K/X9wPjhDhbL1qSvsbow0+djXOfpFfr3e0oui6Rp+lUKvCxlB7bSm1vKXtf8AEblVcKq1XXFRhBJRilskl4IyAAAAAAGhrOk6fqtHZZuNCzZfZl3Tj7Jd6J3xhwrlaO3kUt34e/8Ac2+1X6SX6/kVMx21wtrddkVKE01KMlumn4MCHg6PpA4c+qMpZWLFvDulsl39lL8L9PL+b84AAAAAAbGlYd2oajThULey6ait+5ebfolu/cWHSMKjTtOqwseO1dUeqn4t+Lfq3zOM6H9PU78nU5x3Vf8ARqfq+cvlt8Wd8AAAAAAAAAAAGrquHTn4FuHfFOu6PVfmvJr1T5ke1jCt07U78K/79M3Hfb7y8H71sy1nB9MGnJSxtUrjt1v6NrXxi/8AL5AcOAAAAArPR5irF4SxFts7Yu2T8+s918tj2zW0mpU6VjUpbKumEfhFI2QAAAAAAAAAAAHi8e4v0zhPMhtzrr7WL8nH7T+Sa957Rg1CCuwb6nzU6pR+KaAiQAA//9k=";

                                Label imgInfo = new Label();
                                imgInfo.Text = Utilities.getLocalizedValue("lgImgConstraints");

                                Label imgAlert = new Label();
                                imgAlert.ForeColor = System.Drawing.Color.Red;
                                imgAlert.Visible = false;

                                Button clearImgBtn = new Button();
                                clearImgBtn.Text = Utilities.getLocalizedValue("lgClear");
                                clearImgBtn.Click += new EventHandler(clearImgBtn_Click);

                                HiddenField clearImg = new HiddenField();
                                clearImg.ID = "clearImg";
                                clearImg.Value = "0";

                                // Add the field to the table cell and editFields list
                                oCellControl.Controls.Add(img);
                                oCellControl.Controls.Add(new LiteralControl("<br />"));
                                oCellControl.Controls.Add(clearImg);
                                oCellControl.Controls.Add(clearImgBtn);
                                oCellControl.Controls.Add(new LiteralControl("<br />"));
                                oCellControl.Controls.Add(fu);
                                oCellControl.Controls.Add(new LiteralControl("<br />"));
                                oCellControl.Controls.Add(imgInfo);
                                oCellControl.Controls.Add(new LiteralControl("<br />"));
                                oCellControl.Controls.Add(imgAlert);

                                EditFields.Add(prop.ADName + "Preview", img);
                                EditFields.Add(prop.ADName + "Clear", clearImg);
                                EditFields.Add(prop.ADName + "Alert", imgAlert);
                                EditFields.Add(prop.ADName, fu);
                                break;
                            default: break;
                        }
                    }

                    #endregion

                    #region buttons apply and cancel

                    Button apply = new Button();
                    apply.Text = Utilities.getLocalizedValue("lgApply");
                    apply.Click += new EventHandler(apply_Click);

                    Button reset = new Button();
                    reset.Text = Utilities.getLocalizedValue("lgCancel");
                    reset.Click += new EventHandler(reset_Click);

                    Label spacer = new Label();
                    spacer.Text = "&nbsp;&nbsp;&nbsp;&nbsp;";

                    TableRow trButtons = new TableRow();
                    TableCell tcButtons = new TableCell();
                    //tcButtons.HorizontalAlign = HorizontalAlign.Right;
                    trButtons.Cells.Add(new TableCell());
                    trButtons.Cells.Add(tcButtons);
                    tcButtons.Controls.Add(reset);
                    tcButtons.Controls.Add(spacer);
                    tcButtons.Controls.Add(apply);
                    trButtons.CssClass = "ms-formbody";

                    //editForm.Rows.AddAt(0, trButtons);
                    editForm.Rows.Add(trButtons);
                    editForm.Visible = false;

                    this.Controls.Add(editForm);
                    #endregion

                    #endregion
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    error(ex.ToString());
                }
            }
            else
            {
                error(Utilities.getLocalizedValue("lgConfigurationNeeded"));
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            this.UseDefaultStyles = false; // No default styles
            CssRegistration.Register("/_layouts/1036/styles/Themable/forms.css");
            if (_SelfService && !Page.IsPostBack)
                selectUser(); // if self service select the current user
        }

        public override string LoadResource(string resName)
        {
            return SPUtility.GetLocalizedString("$Resources:" + resName, "ADUserEditorWebpart", (uint)CultureInfo.CurrentUICulture.LCID);
        }

        /* Reordering to render custom properties first */
        public override ToolPart[] GetToolParts()
        {
            ToolPart[] toolparts = new ToolPart[2];
            CustomPropertyToolPart custom = new CustomPropertyToolPart();
            WebPartToolPart wptp = new WebPartToolPart();
            toolparts[0] = custom;
            toolparts[1] = wptp;
            return toolparts;
        }

        protected override void Render(HtmlTextWriter output)
        {
            //output.Write("\n<div id='adusereditor'>\n");
            output.Write("<style  type='text/css'>.ms-inputuserfield{border:1px solid #a5a5a5;}</style>");
            base.Render(output);
            //output.Write("\n</div>\n");
            //output.Write("<script type='text/javascript'>alert('ok');</script>");
        }


        /*protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            // No default styles
            if (this.Zone != null && this.Zone.WebPartChrome != null)
            {
                SPChromeSettings chromeSettings = ((SPChrome)this.Zone.WebPartChrome).GetSPChromeSettings(this);
                chromeSettings.UseDefaultStyles = false;
            }
        }*/

        #region Events

        private void clearImgBtn_Click(object sender, EventArgs e)
        {
            Button b = (Button)sender;

            HiddenField clearImg = (HiddenField)b.Parent.FindControl("clearImg");
            clearImg.Value = "1";

            Image previewImg = (Image)b.Parent.FindControl("previewImg");
            previewImg.ImageUrl = "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEASABIAAD//gA7Q1JFQVRPUjogZ2QtanBlZyB2MS4wICh1c2luZyBJSkcgSlBFRyB2NjIpLCBxdWFsaXR5ID0gOTAK/9sAQwAEAgMDAwIEAwMDBAQEBAUJBgUFBQULCAgGCQ0LDQ0NCwwMDhAUEQ4PEw8MDBIYEhMVFhcXFw4RGRsZFhoUFhcW/9sAQwEEBAQFBQUKBgYKFg8MDxYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYW/8AAEQgAgACAAwEiAAIRAQMRAf/EABsAAQEAAwEBAQAAAAAAAAAAAAAHAwQGBQII/8QAOBAAAgIBAgIHBAkDBQAAAAAAAAECAwQFEQYhBxITMUFRYXGBkaEUFSIyUrHB4fAjM7JCY4KS0f/EABQBAQAAAAAAAAAAAAAAAAAAAAD/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwD9FAAAAAAAAAAAAAAAAAAAAAAAAAAAAZMWi7IyIUUVystsfVjGK3bYGMHf8NcC0VQV+rvtbGt+whLaEfa1zb9nL2nWYWBhYcVHGxKKUvwVqIEUBbczDxMqPVycWm5PwsrUvzOW4j4Fw8iDu0qX0a7v7KTbrl+q/L0AnYM2djX4eVPHya5V21vaUX4GEAAAAAAAAAAABTujzh+GmaeszIgvpl63e651RfdFevn8PA4jgbBjn8T41M1vXCXaT9VHnt73sveV0AAAAAA5/jnQa9Y06U6oJZlMW6peMl+B+j8PJ+8lkk4ycZJxaezT70XMlXSXgRwuKbXCO0MmKuSXm91L5pv3geAAAAAAAAAAAOr6IEnxPdulusSW3/eBSSUdHGXHE4ux+u9o39apv1a5fNIq4AAAAAAJ70yJfWmE9ufYy5/8ihEv6VMtZPFUqovdYtUa3t585P8Ay29wHNgAAAAAAAAAD6rnKFkZwk4yi04yXen5lc4R1erWdIryOsu2glG+K/0z/wDH3r9iQno8M6ln6bqUbMBSsnP7MqUnJWryaQFjBp6RkW5eFC+7EtxZy+9Vbt1l+3w9huAADX1LIni4c74Y1uRKK5VVpOUgNbiTU6NI0uzMuabitq4b85y8F/PDckGVdZk5NmRdLrWWzcpy823uz0eLdV1DVNSlLOhKlV7qGO00q17H4+bPKAAAAAAAAAAHRcAcOvWMx5GSmsOiS6/+5L8K/X9wPjhDhbL1qSvsbow0+djXOfpFfr3e0oui6Rp+lUKvCxlB7bSm1vKXtf8AEblVcKq1XXFRhBJRilskl4IyAAAAAAGhrOk6fqtHZZuNCzZfZl3Tj7Jd6J3xhwrlaO3kUt34e/8Ac2+1X6SX6/kVMx21wtrddkVKE01KMlumn4MCHg6PpA4c+qMpZWLFvDulsl39lL8L9PL+b84AAAAAAbGlYd2oajThULey6ait+5ebfolu/cWHSMKjTtOqwseO1dUeqn4t+Lfq3zOM6H9PU78nU5x3Vf8ARqfq+cvlt8Wd8AAAAAAAAAAAGrquHTn4FuHfFOu6PVfmvJr1T5ke1jCt07U78K/79M3Hfb7y8H71sy1nB9MGnJSxtUrjt1v6NrXxi/8AL5AcOAAAAArPR5irF4SxFts7Yu2T8+s918tj2zW0mpU6VjUpbKumEfhFI2QAAAAAAAAAAAHi8e4v0zhPMhtzrr7WL8nH7T+Sa957Rg1CCuwb6nzU6pR+KaAiQAA//9k=";
        }

        private void selectUser_Click(object sender, EventArgs e)
        {
            selectUser();
        }

        private void apply_Click(object sender, EventArgs e)
        {
            try
            {
                CurrentUser = findFromAccountName((String)ViewState["EditedUserLogin"]);

                DirectoryEntry de = CurrentUser.GetDirectoryEntry();

                foreach (userProperty prop in userProperties)
                {
                    switch (prop.Type)
                    {
                        case "textbox":
                            TextBox t = (TextBox)EditFields[prop.ADName];
                            if (t.Text.Trim() == "") // if no value set
                            {
                                //if the property exists, empty it
                                if (de.Properties.Contains(prop.ADName))
                                    de.Properties[prop.ADName].Clear();
                                //else it doesn't matter: do nothing :-)
                            }
                            else
                            {
                                de.Properties[prop.ADName].Value = t.Text.Trim();
                            }
                            break;
                        case "listbox":
                            ListBox lb = (ListBox)EditFields[prop.ADName];
                            if (lb.SelectedValue.Trim() == "")
                            {
                                if (de.Properties.Contains(prop.ADName))
                                    de.Properties[prop.ADName].Clear();
                            }
                            else
                            {
                                de.Properties[prop.ADName].Value = lb.SelectedValue.Trim();
                            }
                            break;
                        case "checkboxlist":
                            CheckBoxList cbl = (CheckBoxList)EditFields[prop.ADName];

                            //clear before inserting
                            if (de.Properties.Contains(prop.ADName))
                                de.Properties[prop.ADName].Clear();

                            int i = 0;
                            foreach (ListItem li in cbl.Items)
                            {
                                if (li.Selected)
                                {
                                    de.Properties[prop.ADName].Insert(i, li.Value);
                                    i++;
                                }
                            }
                            break;
                        case "dropdown":
                            DropDownList dl = (DropDownList)EditFields[prop.ADName];
                            if (dl.SelectedValue.Trim() == "")
                            {
                                if (de.Properties.Contains(prop.ADName))
                                    de.Properties[prop.ADName].Clear();
                            }
                            else
                            {
                                de.Properties[prop.ADName].Value = dl.SelectedValue.Trim();
                            }
                            break;
                        case "person":
                            PeopleEditor p = (PeopleEditor)EditFields[prop.ADName];

                            if (p.ResolvedEntities.Count > 0)
                            {
                                PickerEntity ppp = (PickerEntity)p.ResolvedEntities[0];

                                // search for the distinguished name of the person and put it in the user property
                                SearchResult person = findFromAccountName(ppp.Key);
                                if (person != null)
                                {
                                    de.Properties[prop.ADName].Value = person.Properties["distinguishedName"][0].ToString().Trim();
                                }
                            }
                            else
                            {
                                if (de.Properties.Contains(prop.ADName))
                                    de.Properties[prop.ADName].Clear();
                            }
                            break;
                        case "date":
                            DateTimeControl dtc = (DateTimeControl)EditFields[prop.ADName];
                            dtc.Validate();
                            if (dtc.IsDateEmpty)
                            {
                                if (de.Properties.Contains(prop.ADName))
                                    de.Properties[prop.ADName].Value = ((long)0).ToString(); // set value to null
                            }
                            else if (dtc.IsValid)
                            {
                                //hack:addHours(23) to have the same date in AD console and webpart (France)
                                de.Properties[prop.ADName].Value = dtc.SelectedDate.AddHours(23).ToFileTimeUtc().ToString();
                            }
                            break;
                        case "multitextbox":
                            Panel pa = (Panel)EditFields[prop.ADName];

                            //clear before inserting
                            if (de.Properties.Contains(prop.ADName))
                                de.Properties[prop.ADName].Clear();

                            foreach (TextBox tb in pa.Controls.OfType<TextBox>().Reverse())
                            {
                                if (tb.Text != "")
                                    de.Properties[prop.ADName].Add(tb.Text);
                            }
                            break;
                        case "image":
                            int maxUpload = 2*1024*1024; // //TODO: allow change
                            FileUpload fu = (FileUpload)EditFields[prop.ADName];
                            Image img = (Image)EditFields[prop.ADName + "Preview"];
                            HiddenField imgClear = (HiddenField)EditFields[prop.ADName + "Clear"];
                            if (!fu.HasFile)
                            {
                                // if image removed, clear
                                if (imgClear.Value.Equals("1") && de.Properties.Contains(prop.ADName))
                                {
                                    de.Properties[prop.ADName].Clear();
                                    imgClear.Value = "0"; // clear only once :)
                                }
                                // if no change to existing image, no action
                            }
                            else
                            {
                                Label imgAlert = (Label)EditFields[prop.ADName + "Alert"];

                                if (fu.PostedFile.ContentLength > maxUpload)
                                {
                                    imgAlert.Visible = true;
                                    imgAlert.Text = Utilities.getLocalizedValue("lgImgTooLarge");
                                }
                                else if (fu.PostedFile.ContentType != "image/jpeg" && fu.PostedFile.ContentType != "image/pjpeg")
                                {
                                    imgAlert.Visible = true;
                                    imgAlert.Text = Utilities.getLocalizedValue("lgImgWrongFormat") + fu.PostedFile.ContentType;
                                }
                                else
                                {
                                    imgAlert.Visible = false;
                                    imgAlert.Text = "";

                                    byte[] resizedImage = Utilities.GetCroppedImage(fu.FileBytes, 128, 128);
                                    String resizedImage64 = System.Convert.ToBase64String(resizedImage, 0, resizedImage.Length); // to put in preview
                                    img.ImageUrl = "data:image/jpg;base64," + resizedImage64;
                                    de.Properties[prop.ADName].Value = resizedImage;
                                }
                            }
                            break;
                        default: break;
                    }
                }

                de.CommitChanges();

                confirm(Utilities.getLocalizedValue("lgUserUpdated"));
                //selectUser();
                //clearFields();
                //editForm.Visible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                error(ex.ToString());
            }
        }

        private void reset_Click(object sender, EventArgs e)
        {

            if (_SelfService)
            {
                selectUser();
            }
            else
            {
                CurrentUser = null;
                editForm.Visible = false;
                info(Utilities.getLocalizedValue("lgEditCancelled"));
            }
        }

        #endregion

        #region PrivateMethods

        private void error(string message)
        {
            state.Visible = true;
            state.Text = message;
            state.Style.Remove("background");
            state.Style.Add("background", "url('_layouts/images/exclaim.gif') no-repeat left");
        }

        private void info(string message)
        {
            state.Visible = true;
            state.Text = message;
            state.Style.Remove("background");
            state.Style.Add("background", "url('_layouts/images/cnsinf16.gif') no-repeat left");
        }

        private void confirm(string message)
        {
            state.Visible = true;
            state.Text = message;
            state.Style.Remove("background");
            state.Style.Add("background", "url('_layouts/images/CNSAPP16.GIF') no-repeat left");
        }

        private SearchResult findFromAccountName(string Name)
        {
            string _accountName = Name.Replace("i:0#.w|", ""); // fix claims based auth
            string _domain = _accountName.Split('\\')[0].ToUpperInvariant();
            string _username = _accountName.Split('\\')[1];
            if (domains.ContainsKey(_domain))
            {
                ADConnexion.Path = domains[_domain].path;
                ADConnexion.Username = domains[_domain].login;
                ADConnexion.Password = domains[_domain].password;
                search.Filter = "(samAccountName=" + _username + ")";
                return search.FindOne();
            }
            else
            {
                string errmsg = "Domain not found in the webpart config for user '" + Name + "'. Please update it! (Existing domains: ";
                int i = 0;
                foreach (String dom in domains.Keys)
                {
                    errmsg += dom;
                    if(i < domains.Keys.Count - 1)
                        errmsg += " , ";
                    i++;
                }
                errmsg += ")";
                throw (new Exception(errmsg));
            }
        }

        private string getNTName(string Name)
        {
            search.Filter = "(distinguishedName=" + Name + ")";

            //try to find the user in every domain
            foreach (KeyValuePair<string, domainConnexion> _domain in domains)
            {
                ADConnexion.Path = _domain.Value.path;
                ADConnexion.Username = _domain.Value.login;
                ADConnexion.Password = _domain.Value.password;
                SearchResult sr = search.FindOne();
                if (sr != null)
                {
                    return _domain.Key + "\\" + sr.Properties["samAccountName"][0].ToString();
                }
            }
            return null; // if nothing found
        }

        private string getDisplayName(string Name)
        {
            search.Filter = "(distinguishedName=" + Name + ")";

            //try to find the user in every domain
            foreach (KeyValuePair<string, domainConnexion> _domain in domains)
            {
                ADConnexion.Path = _domain.Value.path;
                ADConnexion.Username = _domain.Value.login;
                ADConnexion.Password = _domain.Value.password;
                SearchResult sr = search.FindOne();
                if (sr != null)
                {
                    return sr.Properties["displayName"][0].ToString();
                }
            }
            return null; // if nothing found
        }

        private void selectUser()
        {
            try
            {
                clearFields(); // clear all data

                if (!_SelfService) // find selected user
                {
                    if (userSelect.ResolvedEntities.Count > 0)
                    {
                        PickerEntity ppp = (PickerEntity)userSelect.ResolvedEntities[0];
                        try
                        {
                            string usr = ppp.Key;
                            CurrentUser = findFromAccountName(usr);
                            if (CurrentUser != null)
                            {
                                ViewState["EditedUserLogin"] = usr;
                                populateFields();
                                editForm.Visible = true;
                                info(Utilities.getLocalizedValue("lgUserFound"));
                            }
                            else
                            {
                                error(Utilities.getLocalizedValue("lgUserNotFound"));
                                editForm.Visible = false;
                            }
                        }
                        catch (Exception e)
                        {
                            error(e.Message);
                            editForm.Visible = false;
                        }
                    }
                    else
                    {
                        error(Utilities.getLocalizedValue("lgUserNotSelected"));
                        editForm.Visible = false;
                    }
                }
                else // self service: select current windows user
                {
                    string accountName = "";
                    using (SPSite site = new SPSite(SPContext.Current.Site.ID))
                    {
                        using (SPWeb web = site.OpenWeb(SPContext.Current.Web.ID))
                        {
                            accountName = web.CurrentUser.LoginName;
                        }
                    }
                    if (accountName == "SHAREPOINT\\system")
                        accountName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                    try
                    {
                        CurrentUser = findFromAccountName(accountName);
                        if (CurrentUser != null)
                        {
                            ViewState["EditedUserLogin"] = accountName;
                            populateFields();
                            editForm.Visible = true;
                            //info(lgUserFound);
                        }
                    }
                    catch (Exception e)
                    {
                        error(e.Message);
                        editForm.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                error(ex.ToString());
            }
        }

        private void populateFields()
        {
            try
            {
                if (CurrentUser != null)
                {
                    foreach (userProperty prop in userProperties)
                    {
                        if (CurrentUser.Properties.Contains(prop.ADName))
                        {
                            switch (prop.Type)
                            {
                                case "textbox":
                                    TextBox t = (TextBox)EditFields[prop.ADName];
                                    t.Text = CurrentUser.Properties[prop.ADName][0].ToString();
                                    break;
                                case "listbox":
                                    ListBox lb = (ListBox)EditFields[prop.ADName];
                                    ListItem li = lb.Items.FindByValue(CurrentUser.Properties[prop.ADName][0].ToString());
                                    if (li != null) li.Selected = true;
                                    break;
                                case "checkboxlist":
                                    CheckBoxList cbl = (CheckBoxList)EditFields[prop.ADName];
                                    foreach(String value in CurrentUser.Properties[prop.ADName]){
                                        cbl.Items.FindByValue(value).Selected = true;
                                    }
                                    break;
                                case "dropdown":
                                    DropDownList dl = (DropDownList)EditFields[prop.ADName];
                                    ListItem li2 = dl.Items.FindByValue(CurrentUser.Properties[prop.ADName][0].ToString());
                                    if (li2 != null) li2.Selected = true;
                                    break;
                                case "person":
                                    PeopleEditor p = (PeopleEditor)EditFields[prop.ADName];
                                    string DN = CurrentUser.Properties[prop.ADName][0].ToString();
                                    p.CommaSeparatedAccounts = getNTName(DN);
                                    //p.CommaSeparatedAccounts = _usr.Properties["userPrincipalName"][0].ToString();
                                    p.Validate();
                                    break;
                                case "date":
                                    DateTimeControl dtc = (DateTimeControl)EditFields[prop.ADName];
                                    long date = (long)CurrentUser.Properties[prop.ADName][0];
                                    DateTime dt;
                                    if (date != 0 && date != 9223372036854775807) // eq none or never
                                    {
                                        dt = DateTime.FromFileTimeUtc(date);
                                        dtc.SelectedDate = dt;
                                    }
                                    break;
                                case "readonly":
                                    Label lbl = (Label)EditFields[prop.ADName];
                                    int j = 0;
                                    foreach (String value in CurrentUser.Properties[prop.ADName])
                                    {
                                        String txt = value;

                                        //line break between results
                                        if (j > 0)
                                            lbl.Text += "<br />";

                                        // display "display name" if string is a "distinguished name"
                                        if (!txt.Contains("CN="))
                                            lbl.Text += txt;
                                        else
                                            lbl.Text += getDisplayName(txt);

                                        j++;
                                    }
                                    break;
                                case "multitextbox":
                                    Panel pa = (Panel)EditFields[prop.ADName];

                                    TextBox[] tbs = pa.Controls.OfType<TextBox>().ToArray<TextBox>();
                                    // fill in textboxes
                                    int i = 0;
                                    foreach (String value in CurrentUser.Properties[prop.ADName])
                                    {
                                        if (i < tbs.Length)
                                        {
                                            tbs[i].Text = value; // +tbs[i].ID;
                                            i++;
                                        }
                                    }
                                    break;
                                case "image":
                                    byte[] imgAD = (byte[])CurrentUser.Properties[prop.ADName][0];
                                    Image img = (Image)EditFields[prop.ADName + "Preview"];
                                    img.ImageUrl = "data:image/jpg;base64," + System.Convert.ToBase64String(imgAD, 0, imgAD.Length); 
                                    break;
                                default: break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                error(ex.ToString());
            }

        }

        private void clearFields()
        {
            try
            {
                foreach (userProperty prop in userProperties)
                {
                    switch (prop.Type)
                    {
                        case "textbox":
                            TextBox t = (TextBox)EditFields[prop.ADName];
                            t.Text = ""; // clear textbox
                            break;
                        case "listbox":
                            ListBox lb = (ListBox)EditFields[prop.ADName];
                            lb.ClearSelection(); // clear selection
                            break;
                        case "checkboxlist":
                            CheckBoxList cbl = (CheckBoxList)EditFields[prop.ADName];
                            cbl.ClearSelection(); // clear selection
                            break;
                        case "dropdown":
                            DropDownList dl = (DropDownList)EditFields[prop.ADName];
                            dl.ClearSelection(); // clear selection
                            break;
                        case "person":
                            PeopleEditor p = (PeopleEditor)EditFields[prop.ADName];
                            p.Accounts.Clear();
                            p.Entities.Clear();
                            p.ResolvedEntities.Clear();
                            //p.CommaSeparatedAccounts = ""; // clear name
                            break;
                        case "date":
                            DateTimeControl dtc = (DateTimeControl)EditFields[prop.ADName];
                            dtc.SelectedDate = new DateTime(); // clear date
                            break;
                        case "readonly":
                            Label lbl = (Label)EditFields[prop.ADName];
                            lbl.Text = ""; // clear label
                            break;
                        case "multitextbox":
                            Panel pa = (Panel)EditFields[prop.ADName];

                            foreach (TextBox tb in pa.Controls.OfType<TextBox>())
                            {
                                tb.Text = "";
                            }
                            break;
                        case "image":
                            FileUpload fu = (FileUpload)EditFields[prop.ADName];
                            fu = new FileUpload();
                            fu.ID = prop.ADName;

                            Image img = (Image)EditFields[prop.ADName + "Preview"];
                            img.ImageUrl = "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEASABIAAD//gA7Q1JFQVRPUjogZ2QtanBlZyB2MS4wICh1c2luZyBJSkcgSlBFRyB2NjIpLCBxdWFsaXR5ID0gOTAK/9sAQwAEAgMDAwIEAwMDBAQEBAUJBgUFBQULCAgGCQ0LDQ0NCwwMDhAUEQ4PEw8MDBIYEhMVFhcXFw4RGRsZFhoUFhcW/9sAQwEEBAQFBQUKBgYKFg8MDxYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYW/8AAEQgAgACAAwEiAAIRAQMRAf/EABsAAQEAAwEBAQAAAAAAAAAAAAAHAwQGBQII/8QAOBAAAgIBAgIHBAkDBQAAAAAAAAECAwQFEQYhBxITMUFRYXGBkaEUFSIyUrHB4fAjM7JCY4KS0f/EABQBAQAAAAAAAAAAAAAAAAAAAAD/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwD9FAAAAAAAAAAAAAAAAAAAAAAAAAAAAZMWi7IyIUUVystsfVjGK3bYGMHf8NcC0VQV+rvtbGt+whLaEfa1zb9nL2nWYWBhYcVHGxKKUvwVqIEUBbczDxMqPVycWm5PwsrUvzOW4j4Fw8iDu0qX0a7v7KTbrl+q/L0AnYM2djX4eVPHya5V21vaUX4GEAAAAAAAAAAABTujzh+GmaeszIgvpl63e651RfdFevn8PA4jgbBjn8T41M1vXCXaT9VHnt73sveV0AAAAAA5/jnQa9Y06U6oJZlMW6peMl+B+j8PJ+8lkk4ycZJxaezT70XMlXSXgRwuKbXCO0MmKuSXm91L5pv3geAAAAAAAAAAAOr6IEnxPdulusSW3/eBSSUdHGXHE4ux+u9o39apv1a5fNIq4AAAAAAJ70yJfWmE9ufYy5/8ihEv6VMtZPFUqovdYtUa3t585P8Ay29wHNgAAAAAAAAAD6rnKFkZwk4yi04yXen5lc4R1erWdIryOsu2glG+K/0z/wDH3r9iQno8M6ln6bqUbMBSsnP7MqUnJWryaQFjBp6RkW5eFC+7EtxZy+9Vbt1l+3w9huAADX1LIni4c74Y1uRKK5VVpOUgNbiTU6NI0uzMuabitq4b85y8F/PDckGVdZk5NmRdLrWWzcpy823uz0eLdV1DVNSlLOhKlV7qGO00q17H4+bPKAAAAAAAAAAHRcAcOvWMx5GSmsOiS6/+5L8K/X9wPjhDhbL1qSvsbow0+djXOfpFfr3e0oui6Rp+lUKvCxlB7bSm1vKXtf8AEblVcKq1XXFRhBJRilskl4IyAAAAAAGhrOk6fqtHZZuNCzZfZl3Tj7Jd6J3xhwrlaO3kUt34e/8Ac2+1X6SX6/kVMx21wtrddkVKE01KMlumn4MCHg6PpA4c+qMpZWLFvDulsl39lL8L9PL+b84AAAAAAbGlYd2oajThULey6ait+5ebfolu/cWHSMKjTtOqwseO1dUeqn4t+Lfq3zOM6H9PU78nU5x3Vf8ARqfq+cvlt8Wd8AAAAAAAAAAAGrquHTn4FuHfFOu6PVfmvJr1T5ke1jCt07U78K/79M3Hfb7y8H71sy1nB9MGnJSxtUrjt1v6NrXxi/8AL5AcOAAAAArPR5irF4SxFts7Yu2T8+s918tj2zW0mpU6VjUpbKumEfhFI2QAAAAAAAAAAAHi8e4v0zhPMhtzrr7WL8nH7T+Sa957Rg1CCuwb6nzU6pR+KaAiQAA//9k=";

                            HiddenField imgClear = (HiddenField)EditFields[prop.ADName + "Clear"];
                            imgClear.Value = "0";

                            Label imgAlert = (Label)EditFields[prop.ADName + "Alert"];
                            imgAlert.Visible = false;
                            imgAlert.Text = "";
                            
                            break;
                        default: break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                error(ex.ToString());
            }
        }

        #endregion
    }

    public static class Utilities
    {
        public static List<userProperty> getUserPropertiesFromXML(string XML)
        {
            if (!XML.Contains("<?xml"))
                XML = "<?xml version='1.0' encoding='UTF-8'?>" + XML;

            List<userProperty> userProperties = new List<userProperty>();

            XmlTextReader config_xml = new XmlTextReader(new System.IO.StringReader(XML));
            config_xml.WhitespaceHandling = WhitespaceHandling.None;

            while (config_xml.Read())
            {
                if (config_xml.NodeType == XmlNodeType.Element && config_xml.Name == "property")
                {
                    userProperty p;
                    p.ADName = config_xml.GetAttribute("adname");
                    p.Type = config_xml.GetAttribute("type");
                    p.Name = config_xml.GetAttribute("name");
                    p.Values = config_xml.GetAttribute("values");
                    userProperties.Add(p);
                }
            }
            return userProperties;
        }

        public static Dictionary<string, domainConnexion> getDomainsFromXML(string XML)
        {
            if (!XML.Contains("<?xml"))
                XML = "<?xml version='1.0' encoding='UTF-8'?>" + XML;

            Dictionary<string, domainConnexion> domains = new Dictionary<string, domainConnexion>();

            XmlTextReader config_xml2 = new XmlTextReader(new System.IO.StringReader(XML));
            config_xml2.WhitespaceHandling = WhitespaceHandling.None;

            while (config_xml2.Read())
            {
                if (config_xml2.NodeType == XmlNodeType.Element && config_xml2.Name == "domain")
                {
                    domainConnexion d;
                    d.path = config_xml2.GetAttribute("path");
                    d.login = config_xml2.GetAttribute("usr");
                    d.password = config_xml2.GetAttribute("pwd");
                    domains.Add(config_xml2.GetAttribute("name").ToUpperInvariant(), d);
                }
            }
            return domains;
        }

        public static DirectorySearcher initDirectorySearcher(DirectoryEntry ADConnexion, List<userProperty> userProperties)
        {
            DirectorySearcher search = new DirectorySearcher(ADConnexion);
            search.PropertiesToLoad.Add("distinguishedName"); //necessary but not shown
            search.PropertiesToLoad.Add("samAccountName"); //necessary but not shown
            search.PropertiesToLoad.Add("userPrincipalName"); //necessary but not shown
            foreach (userProperty prop in userProperties)
            {
                search.PropertiesToLoad.Add(prop.ADName);
            }
            return search;
        }

        public static string getLocalizedValue(string resName)
        {
            return SPUtility.GetLocalizedString("$Resources:" + resName, "ADUserEditorWebpart", (uint)CultureInfo.CurrentUICulture.LCID);
        }

        public static byte[] GetCroppedImage(byte[] originalBytes, int targetWidth, int targetHeight)
        {
            // source : http://www.britishdeveloper.co.uk/2011/05/image-resizing-cropping-and-compression.html

            using (var streamOriginal = new System.IO.MemoryStream(originalBytes))
            using (var imgOriginal = System.Drawing.Image.FromStream(streamOriginal))
            {
                //get original width and height of the incoming image
                var originalWidth = imgOriginal.Width; // 1000
                var originalHeight = imgOriginal.Height; // 800

                //get the percentage difference in size of the dimension that will change the least
                var percWidth = ((float)targetWidth / (float)originalWidth); // 0.2
                var percHeight = ((float)targetHeight / (float)originalHeight); // 0.25
                var percentage = Math.Max(percHeight, percWidth); // 0.25

                //get the ideal width and height for the resize (to the next whole number)
                var width = (int)Math.Max(originalWidth * percentage, targetWidth); // 250
                var height = (int)Math.Max(originalHeight * percentage, targetHeight); // 200

                //actually resize it
                using (var resizedBmp = new System.Drawing.Bitmap(width, height))
                {
                    using (var graphics = System.Drawing.Graphics.FromImage((System.Drawing.Image)resizedBmp))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                        graphics.DrawImage(imgOriginal, 0, 0, width, height);
                    }

                    //work out the coordinates of the top left pixel for cropping
                    var x = (width - targetWidth) / 2; // 25
                    var y = (height - targetHeight) / 2; // 0

                    //create the cropping rectangle
                    var rectangle = new System.Drawing.Rectangle(x, y, targetWidth, targetHeight); // 25, 0, 200, 200

                    //crop
                    using (var croppedBmp = resizedBmp.Clone(rectangle, resizedBmp.PixelFormat))
                    using (var ms = new System.IO.MemoryStream())
                    {
                        //get the codec needed
                        var imgCodec = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);

                        //make a paramater to adjust quality
                        var codecParams = new System.Drawing.Imaging.EncoderParameters(1);

                        //reduce to quality of 80 (from range of 0 (max compression) to 100 (no compression))
                        codecParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);

                        //save to the memorystream - convert it to an array and send it back as a byte[]
                        croppedBmp.Save(ms, imgCodec, codecParams);
                        return ms.ToArray();
                    }
                }
            }
        }
    }
}