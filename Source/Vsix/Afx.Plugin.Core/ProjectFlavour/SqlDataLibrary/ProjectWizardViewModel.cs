using Afx.Plugin.Commands;
using Afx.Plugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Afx.Plugin.ProjectFlavour.SqlDataLibrary
{
  public class ProjectWizardViewModel : ViewModel
  {
    public IEnumerable<AfxProject> ClassLibraries
    {
      get { return ApplicationStructure.Instance.Projects.OfType<AfxProjectClassLibrary>(); }
    }

    public AfxProject SelectedClassLibrary { get; set; }

    public bool? mDialogResult;
    public bool? DialogResult
    {
      get { return mDialogResult; }
      set { SetProperty<bool?>(ref mDialogResult, value); }
    }

    DelegateCommand mOkCommand;
    public DelegateCommand OkCommand
    {
      get { return mOkCommand ?? (mOkCommand = new DelegateCommand(ExecuteOK)); }
    }

    void ExecuteOK()
    {
      DialogResult = true;
    }
  }
}
