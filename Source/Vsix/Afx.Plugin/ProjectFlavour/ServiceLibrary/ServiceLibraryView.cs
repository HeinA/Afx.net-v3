using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Afx.vsix.ProjectFlavour.PropertyPageBase;

namespace Afx.vsix.ProjectFlavour.ServiceLibrary
{
  public partial class ServiceLibraryView : PageView
  {
    public const string StringPropertyTag = "StringProperty";
    public const string BooleanPropertyTag = "BooleanProperty";

    public ServiceLibraryView(IPageViewSite site) : base(site)
    {
      InitializeComponent();
    }

    private PropertyControlTable propertyControlTable;

    /// <summary>
    /// This property is used to map the control on a PageView object to a property
    /// in PropertyStore object.
    /// 
    /// This property will be called in the base class's constructor, which means that
    /// the InitializeComponent has not been called and the Controls have not been
    /// initialized.
    /// </summary>
    protected override PropertyControlTable PropertyControlTable
    {
      get
      {
        if (propertyControlTable == null)
        {
          // This is the list of properties that will be persisted and their
          // assciation to the controls.
          propertyControlTable = new PropertyControlTable();

          // This means that this CustomPropertyPageView object has not been
          // initialized.
          if (string.IsNullOrEmpty(base.Name))
          {
            this.InitializeComponent();
          }

          // Add two Property Name / Control KeyValuePairs. 
          //propertyControlTable.Add(StringPropertyTag, tbStringProperty);
         propertyControlTable.Add(BooleanPropertyTag, checkBox1);
        }
        return propertyControlTable;
      }
    }
  }
}
