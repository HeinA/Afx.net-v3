using Afx.vsix.ProjectFlavour.PropertyPageBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Afx.vsix.ProjectFlavour.ServiceLibrary
{
  [Guid(PropertyPageGuidString)]
  public class PropertyPage : PropertyPageBase.PropertyPage
  {
    public const string PropertyPageGuidString = "B569C590-D30A-402B-9FE3-064EC1913E22";

    #region Overriden Properties and Methods

    /// <summary>
    /// Help keyword that should be associated with the page
    /// </summary>
    protected override string HelpKeyword
    {
      // TODO: Put your help keyword here
      get { return String.Empty; }
    }

    /// <summary>
    /// Title of the property page.
    /// </summary>
    public override string Title
    {
      get { return "Custom"; }
    }

    /// <summary>
    /// Provide the view of our properties.
    /// </summary>
    /// <returns></returns>
    protected override IPageView GetNewPageView()
    {
      return new ServiceLibraryView(this);
    }

    /// <summary>
    /// Use a store implementation designed for flavors.
    /// </summary>
    /// <returns>Store for our properties</returns>
    protected override IPropertyStore GetNewPropertyStore()
    {
      return new ServiceLibraryPropertyStore();
    }

    #endregion
  }
}
